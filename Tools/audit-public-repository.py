"""Local publication inventory. Values are never included in findings.

Run Gitleaks separately; this supplements it with Git-object sizes, personal
paths, metadata and ignored-file inventory. It is not a safety certification.
"""
from pathlib import Path
import collections, hashlib, json, os, re, subprocess

ROOT = Path(__file__).resolve().parents[1]
OUT = ROOT / 'Artifacts/PublicAudit'
OUT.mkdir(parents=True, exist_ok=True)

def git(*args):
    return subprocess.check_output(['git', '-c', 'core.quotepath=false', *args], cwd=ROOT)

tracked = set(git('ls-files', '-z').decode('utf-8').split('\0')) - {''}
commits = git('rev-list', '--all').decode().splitlines()
objects = {}
for line in git('rev-list', '--objects', '--all').decode('utf-8').splitlines():
    oid, _, path = line.partition(' ')
    objects[oid] = path
batch = subprocess.run(['git', 'cat-file', '--batch-check=%(objectname) %(objecttype) %(objectsize)'],
                       input=('\n'.join(objects)+'\n').encode(), capture_output=True, cwd=ROOT, check=True)
blobs = [(oid, int(size), objects[oid]) for oid, kind, size in
         (line.split() for line in batch.stdout.decode().splitlines()) if kind == 'blob']
rules = {
    'absolute_local_path': re.compile(r'(?i)(?:(?<![A-Z0-9_])[A-Z]:[\\/]|/(?:Users|home)/)[^\s"<>\r\n]{2,}'),
    'user_profile_path': re.compile(r'(?i)(?:[A-Z]:[\\/]+Users[\\/]+[^\s"<>\\/]+|/(?:Users|home)/[^/\s"<>]+)'),
    'email': re.compile(r'(?i)\b[A-Z0-9._%+-]+@[A-Z0-9.-]+\.[A-Z]{2,}\b'),
    'credentialed_url': re.compile(r'(?i)https?://[^\s/:]+:[^\s/@]+@[^\s]+'),
    'private_key': re.compile(r'-----BEGIN (?:RSA |EC |DSA |OPENSSH |ENCRYPTED )?PRIVATE KEY-----'),
    'unity_account_field': re.compile(r'(?i)(?:access_token|refresh_token|licenseKey|serialNumber)\s*[=:]\s*["\']?[^\s"\',}]{8,}'),
}
text_ext = {'.md','.txt','.cs','.json','.jsonl','.yaml','.yml','.xml','.config','.env','.ini','.log','.ps1','.py','.csv','.tsv','.toml','.sh','.bat','.cmd','.meta','.unity','.asset','.html','.rsp','.browser'}
findings, current, special, errors, local_large = [], [], [], [], []
def scan(data, path, scope, oid=None, line_offset=0):
    text = data.decode('utf-8', errors='replace')
    for rule, pattern in rules.items():
        # Fast literal prechecks prevent expensive email backtracking on large
        # telemetry numbers; they do not filter any matching value out.
        if rule=='email' and '@' not in text:continue
        if rule=='credentialed_url' and '://' not in text:continue
        if rule=='private_key' and 'PRIVATE KEY' not in text:continue
        if rule in {'absolute_local_path','user_profile_path'} and '\\' not in text and ':/' not in text and '/Users/' not in text and '/users/' not in text and '/home/' not in text:continue
        if rule=='unity_account_field' and not re.search(r'access_token|refresh_token|licensekey|serialnumber',text,re.I):continue
        matches=list(pattern.finditer(text))
        if matches:
            findings.append({'scope':scope,'path':path,'blob':oid,'rule':rule,'count':len(matches),
                             'lines':[line_offset+text.count('\n',0,m.start())+1 for m in matches[:12]]})

def scan_file(p, rel, scope):
    # Raw telemetry can exceed available RAM. Scan every byte with bounded
    # overlapping windows; offsets are file line numbers. Very long matches
    # beyond the 4 KiB overlap remain a documented scanner limitation.
    offset=0;tail=b''
    with p.open('rb') as stream:
        while True:
            block=stream.read(1024*1024)
            if not block:break
            scan(tail+block,rel,scope,line_offset=offset-tail.count(b'\n'))
            offset+=block.count(b'\n');tail=(tail+block)[-4096:]

reader = subprocess.Popen(['git','cat-file','--batch'], stdin=subprocess.PIPE, stdout=subprocess.PIPE, cwd=ROOT)
for oid, size, path in blobs:
    reader.stdin.write((oid+'\n').encode());reader.stdin.flush()
    header=reader.stdout.readline();data=reader.stdout.read(size);reader.stdout.read(1)
    # Also inspect binary object strings for key/credential indicators.
    scan(data, path, 'history', oid)
reader.stdin.close();reader.wait()

local_counts=collections.Counter();local_bytes=collections.Counter();scanned=collections.Counter()
for base, dirs, files in os.walk(ROOT):
    dirs[:]=[d for d in dirs if d!='.git' and not (Path(base)/d).is_symlink()]
    for name in files:
        p=Path(base)/name;rel=p.relative_to(ROOT).as_posix()
        # Scanner output is not input. Unity/Build/Artifacts inputs are included.
        if rel.startswith('Artifacts/PublicAudit/'):continue
        try:
            size=p.stat().st_size;top=rel.split('/')[0];local_counts[top]+=1;local_bytes[top]+=size
            if size>10*1024**2:local_large.append({'path':rel,'bytes':size,'tracked':rel in tracked})
            sensitive=(name.lower().startswith('.env') or name.lower() in {'id_rsa','id_ed25519','credentials','config.json'} or p.suffix.lower() in {'.pem','.key','.p12','.pfx','.keystore','.jks','.unitypackage'})
            if sensitive:special.append({'path':rel,'tracked':rel in tracked,'bytes':size})
            if p.suffix.lower() in text_ext or name.startswith('.') or sensitive:
                scan_file(p,rel,'working_tracked' if rel in tracked else 'working_untracked_or_ignored');scanned[top]+=1
        except (OSError,ValueError) as e:errors.append({'path':rel,'error_type':type(e).__name__})

locations={}
for c in commits:
    for entry in git('ls-tree','-r','-z',c).decode('utf-8').split('\0'):
        if not entry:continue
        info,path=entry.split('\t',1);oid=info.split()[2]
        locations.setdefault((oid,path),c)
for f in findings:
    if f['scope']=='history':f['example_commit']=locations.get((f['blob'],f['path']))

authors=git('log','--all','--format=%an%x00%ae%x00%cn%x00%ce').decode('utf-8').splitlines()
identity_pairs=set()
for line in authors:
    parts=line.split('\0');identity_pairs.update([tuple(parts[:2]),tuple(parts[2:])])
metadata_scan=git('log','--all','--format=%B')
scan(metadata_scan,'<commit messages>','history_metadata')
large=[{'blob':oid,'path':path,'bytes':size,'tracked_now':path in tracked,'in_history':True,
        'over_10_MiB':size>10*1024**2,'over_25_MiB':size>25*1024**2,
        'over_50_MiB':size>50*1024**2,'over_100_MiB':size>100*1024**2} for oid,size,path in blobs if size>10*1024**2]
asset_ext={'.otf','.ttf','.fbx','.blend','.obj','.png','.jpg','.jpeg','.wav','.mp3','.ogg','.shader','.dll','.unitypackage'}
asset_paths=sorted({path for _,_,path in blobs if Path(path).suffix.lower() in asset_ext})
summary={'audited_head':git('rev-parse','HEAD').decode().strip(),'commits':len(commits),'refs':git('for-each-ref','--format=%(refname)').decode().splitlines(),
         'tracked_files':len(tracked),'unique_blobs':len(blobs),'history_blob_bytes':sum(size for _,size,_ in blobs),
         'remote_configured':bool(git('remote').strip()),'local_files':dict(local_counts),'local_bytes':dict(local_bytes),
         'local_text_files_scanned':dict(scanned),'read_errors':errors,'identity_pair_count':len(identity_pairs),
         'identity_values_sha256':[hashlib.sha256(('\0'.join(x)).encode()).hexdigest() for x in sorted(identity_pairs)],
         'findings_by_rule':dict(collections.Counter(f['rule'] for f in findings)),
         'sensitive_named_files':special,'history_large_files':large,'asset_paths_in_history':asset_paths}
(OUT/'inventory.json').write_text(json.dumps(summary,ensure_ascii=False,indent=2)+'\n',encoding='utf-8')
(OUT/'privacy-findings.json').write_text(json.dumps(findings,ensure_ascii=False,indent=2)+'\n',encoding='utf-8')
(OUT/'local-large-files.json').write_text(json.dumps(local_large,ensure_ascii=False,indent=2)+'\n',encoding='utf-8')
print(json.dumps({k:summary[k] for k in ['commits','tracked_files','unique_blobs','history_blob_bytes','findings_by_rule','read_errors','identity_pair_count']},ensure_ascii=True))
