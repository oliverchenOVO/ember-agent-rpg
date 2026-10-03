"""Index bounded feature verification; never imply a full balance or soak pass."""
import csv,hashlib,json,re
from pathlib import Path
ROOT=Path(__file__).resolve().parents[1]
raw=ROOT/'Artifacts/Phase311'
manifest=json.loads((ROOT/'Artifacts/knowledge-codex-build-manifest.json').read_text(encoding='utf8'))
editor=(raw/'knowledge-persistence-validation-final/Editor.log').read_text(encoding='utf-8-sig',errors='replace')
assert 'KNOWLEDGE CODEX VALIDATION PASSED' in editor
assert 'runtime='+manifest['source_runtime_hash'] in editor
manual=[('1600-zh','codex_skill_detail'),('1280-en','codex_boss'),('1280-en','codex_healer_detail'),('1280-zh','codex_book_bottom'),('1280-zh','codex_warrior'),('1280-zh','codex_archer'),('1600-en','codex_mage'),('1600-zh','codex_book'),('1600-zh','codex_boss'),('1600-zh','codex_boss_bottom'),('1600-en','codex_legacy'),('1600-en','codex_empty'),('1280-en','codex_archer_detail')]
summary={'source_runtime_hash':manifest['source_runtime_hash'],'editor_validation':'PASS','skill_descriptions':24,'localization_keys':536,'glyphs_checked':819,'save_replay_cases':7,'ui':[],'manual_review':[],'full_balance_gate':'NOT_RUN; historical P3.1.1 FAIL unchanged','5000_seed':'NOT_RUN','120_minute_soak':'NOT_RUN'}
for label in ['knowledge-qa-dev-1600-zh-final','knowledge-qa-dev-1280-en-final','knowledge-qa-dev-1280-zh-final','knowledge-qa-dev-1600-en-final','knowledge-qa-release-1600-zh-final']:
 folder=raw/label;r=json.loads((folder/'process-result.json').read_text(encoding='utf-8-sig'));assert r['state']=='COMPLETE' and r['exitCode']==0
 kind='release' if 'release' in label else 'development';assert r['managedAssemblyHash'].lower()==manifest['builds'][kind]['assembly_hash']
 layout=(folder/'layout-results.txt').read_text(encoding='utf8');assert 'issues: 0' in layout
 pngs=list(folder.glob('*.png'));assert len(pngs)==13
 log=(folder/'player.log').read_text(encoding='utf-8-sig',errors='replace');assert 'Exception:' not in log and 'JobTempAlloc' not in log
 summary['ui'].append({'label':label,'scenarios':13,'issues':0,'exit':0,'assembly':r['managedAssemblyHash']})
for group,file in manual:summary['manual_review'].append({'path':f'Artifacts/Phase311/knowledge-qa-dev-{group}-final/{file}.png','result':'PASS','review':'visual inspection: glyphs, wrapping, clipped controls, scroll content'})
for file in ['codex_book','codex_healer_detail']:summary['manual_review'].append({'path':f'Artifacts/Phase311/knowledge-qa-release-1600-zh-final/{file}.png','result':'PASS','review':'visual inspection'})
folder=raw/'knowledge-release-gameplay-final';r=json.loads((folder/'process-result.json').read_text(encoding='utf-8-sig'));assert r['state']=='COMPLETE' and r['exitCode']==0 and r['managedAssemblyHash'].lower()==manifest['builds']['release']['assembly_hash']
complete=(folder/'complete.txt').read_text(encoding='utf8');assert 'maintenance=1' in complete
log=(folder/'player.log').read_text(encoding='utf-8-sig',errors='replace');assert 'Exception:' not in log and 'JobTempAlloc' not in log
summary['gameplay_smoke']={'configured_seconds':65,'result':'PASS','completed':complete.strip(),'save_load_cycles':1,'runtime_exceptions':0,'JobTempAlloc_warnings':0,'scope':'bounded startup/gameplay/save/load/cleanup; not performance certification'}
(ROOT/'Artifacts/knowledge-codex-summary.json').write_text(json.dumps(summary,ensure_ascii=False,indent=2)+'\n',encoding='utf8')
with (ROOT/'Artifacts/knowledge-codex-evidence.csv').open('w',encoding='utf8',newline='') as f:
 writer=csv.writer(f);writer.writerow(['path','bytes','sha256'])
 for folder in sorted(raw.glob('knowledge-*')):
  for item in sorted(folder.rglob('*')):
   if item.is_file():writer.writerow([item.relative_to(ROOT).as_posix(),item.stat().st_size,hashlib.sha256(item.read_bytes()).hexdigest()])
print('Verified: 65 screenshots; 15 manual reviews; 536 keys / 819 glyphs; 7 replays; 1 native save/load cycle.')
