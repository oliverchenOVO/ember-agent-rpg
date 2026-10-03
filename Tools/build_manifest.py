"""Record source identity separately from the files actually shipped in each Player."""
import hashlib,json,re
from pathlib import Path

ROOT=Path(__file__).resolve().parents[1]
def sha(path):return hashlib.sha256(path.read_bytes()).hexdigest()
log=(ROOT/'Artifacts/Phase311/candidate-A-release/Editor.log').read_text(encoding='utf-8-sig',errors='replace')
runtime=re.search(r'SOURCE RUNTIME HASH / ([a-f0-9]{64})',log).group(1)
manifest={'candidate':'candidate-A','balance_version':'P3.1.1-A','source_runtime_hash':runtime,'runtime_hash_scope':'Core text excluding .meta + Resources JSON; independent of Player assemblies and Presentation','builds':{}}
for kind,folder,label in [('development','Development','candidate-A-development'),('release','Windows','candidate-A-release')]:
    directory=ROOT/'Builds/Phase311'/folder
    result=json.loads((ROOT/'Artifacts/Phase311'/label/'result.json').read_text(encoding='utf-8-sig'))
    assert result['state']=='COMPLETE' and result['exitCode']==0
    files={str(p.relative_to(directory)).replace('\\','/'):sha(p) for p in sorted(directory.rglob('*')) if p.is_file() and p.suffix.lower() not in ['.pdb','.log']}
    manifest['builds'][kind]={'elapsed_seconds':result['elapsed'],'completed_utc':result['utc'],'executable':str(directory/'Ember.exe'),'executable_hash':files['Ember.exe'],'assembly_hash':files['Ember_Data/Managed/Assembly-CSharp.dll'],'files':files}
(ROOT/'Artifacts/phase3_1_1_build-manifest.json').write_text(json.dumps(manifest,ensure_ascii=False,indent=2)+'\n',encoding='utf8')
print(json.dumps({k:{f:v[f] for f in ['executable_hash','assembly_hash','elapsed_seconds']} for k,v in manifest['builds'].items()}))
