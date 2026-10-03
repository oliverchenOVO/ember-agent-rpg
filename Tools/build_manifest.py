"""Record source identity separately from the files actually shipped in each Player."""
import argparse,hashlib,json,re
from pathlib import Path

ROOT=Path(__file__).resolve().parents[1]
def sha(path):return hashlib.sha256(path.read_bytes()).hexdigest()
parser=argparse.ArgumentParser();parser.add_argument('--candidate',default='candidate-A');parser.add_argument('--build-root',default='Builds/Phase311');parser.add_argument('--development-label',default='candidate-A-development');parser.add_argument('--release-label',default='candidate-A-release');parser.add_argument('--output',default='Artifacts/phase3_1_1_build-manifest.json');args=parser.parse_args()
log=(ROOT/'Artifacts/Phase311'/args.release_label/'Editor.log').read_text(encoding='utf-8-sig',errors='replace')
runtime=re.search(r'SOURCE RUNTIME HASH / ([a-f0-9]{64})',log).group(1)
manifest={'candidate':args.candidate,'balance_version':re.search(r'/ balance=(\S+)',log).group(1),'source_runtime_hash':runtime,'runtime_hash_scope':'Core text excluding .meta + Resources JSON; independent of Player assemblies and Presentation','builds':{}}
for kind,folder,label in [('development','Development',args.development_label),('release','Windows',args.release_label)]:
    directory=ROOT/args.build_root/folder
    source_log=(ROOT/'Artifacts/Phase311'/label/'Editor.log').read_text(encoding='utf-8-sig',errors='replace')
    assert re.search(r'SOURCE RUNTIME HASH / ([a-f0-9]{64})',source_log).group(1)==runtime,'Build runtime mismatch'
    result=json.loads((ROOT/'Artifacts/Phase311'/label/'result.json').read_text(encoding='utf-8-sig'))
    assert result['state']=='COMPLETE' and result['exitCode']==0
    files={str(p.relative_to(directory)).replace('\\','/'):sha(p) for p in sorted(directory.rglob('*')) if p.is_file() and p.suffix.lower() not in ['.pdb','.log']}
    manifest['builds'][kind]={'elapsed_seconds':result['elapsed'],'completed_utc':result['utc'],'executable':str(directory/'Ember.exe'),'executable_hash':files['Ember.exe'],'assembly_hash':files['Ember_Data/Managed/Assembly-CSharp.dll'],'files':files}
output=ROOT/args.output
if output.exists():
    prior=json.loads(output.read_text(encoding='utf8'))
    assert prior==manifest,'Preserve prior manifest; choose a fresh output name'
else:output.write_text(json.dumps(manifest,ensure_ascii=False,indent=2)+'\n',encoding='utf8')
print(json.dumps({k:{f:v[f] for f in ['executable_hash','assembly_hash','elapsed_seconds']} for k,v in manifest['builds'].items()}))
