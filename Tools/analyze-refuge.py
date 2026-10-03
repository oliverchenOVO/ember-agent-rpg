"""Inspect existing bounded refuge evidence; never start Unity or simulations."""
import csv,hashlib,json,re,statistics
from pathlib import Path
ROOT=Path(__file__).resolve().parents[1];RAW=ROOT/'Artifacts/Phase311'
manifest=json.loads((ROOT/'Artifacts/refuge-exploration-build-manifest.json').read_text(encoding='utf-8-sig'))
editor=(RAW/'refuge-build-pair-final2/Editor.log').read_text(encoding='utf-8-sig',errors='replace')
assert 'REFUGE EXPLORATION VALIDATION PASSED' in editor
assert 'REFUGE RUNTIME / '+manifest['source_runtime_hash'] in editor
assert len(re.findall(r'REFUGE REPLAY / .* / PASSED',editor))==7
assert 'REST INTERACTION VALIDATION PASSED' in editor
assert 'EMBER WINDOWS BUILD PASSED' in editor
loc=re.search(r'LOCALIZATION VALIDATION PASSED / (\d+) keys / (\d+) glyphs',editor)
summary={'scope':'bounded refuge exploration/model/navigation checks; no balance or soak certification',
 'runtime_hash':manifest['source_runtime_hash'],'localization_keys':int(loc[1]),'glyphs':int(loc[2]),
 'save_replays':7,'navigation_variants':6,'rooms':6,'area':[60,40],
 'autonomous_checks':re.findall(r'REFUGE AUTONOMOUS / ([^\r\n]+)',editor),
 'oversearch_fixture':re.search(r'REFUGE OVERSEARCH / ([^\r\n]+)',editor)[1],
 'players':[],'balance_gate':'NOT_RUN; historical FAIL unchanged','5000_seed':'INCOMPLETE for this candidate','120_minute_soak':'INCOMPLETE for this candidate'}
checks=[('refuge-gallery-dev-zh-final5','development',23),('refuge-gallery-dev-en-final2','development',23),('refuge-gallery-release-final','release',23),('refuge-codex-release-final','release',13)]
for label,kind,count in checks:
 folder=RAW/label;result=json.loads((folder/'process-result.json').read_text(encoding='utf-8-sig'))
 assert result['state']=='COMPLETE' and result['exitCode']==0
 assert result['managedAssemblyHash'].lower()==manifest['builds'][kind]['assembly_hash']
 assert f'scenarios: {count} /' in (folder/'layout-results.txt').read_text(encoding='utf-8') and 'issues: 0' in (folder/'layout-results.txt').read_text(encoding='utf-8')
 assert len(list(folder.glob('*.png')))==count
 log=(folder/'player.log').read_text(encoding='utf-8-sig',errors='replace');assert 'Exception:' not in log
 warnings=log.count('Internal: JobTempAlloc')+log.count('Internal: There are remaining Allocations on the JobTempAlloc')
 if count==23:
  rows=list(csv.reader((folder/'refuge-mesh-checks.csv').open(encoding='utf-8')));assert len(rows)==23
  assert all(int(r[1])>=200 for r in rows)
  assert next(r for r in rows if r[0]=='refuge_empty')[2]=='0'
  assert next(r for r in rows if r[0]=='refuge_partial')[2]=='3'
 summary['players'].append({'label':label,'scenarios':count,'seconds':result['elapsed'],'layout_issues':0,'JobTempAlloc_messages':warnings,'native_status':'unresolved warning' if warnings else 'none observed in this bounded run'})
folder=RAW/'refuge-release-gameplay-final';result=json.loads((folder/'process-result.json').read_text(encoding='utf-8-sig'));assert result['state']=='COMPLETE' and result['exitCode']==0
assert result['managedAssemblyHash'].lower()==manifest['builds']['release']['assembly_hash']
complete=(folder/'complete.txt').read_text(encoding='utf-8');assert 'maintenance=1' in complete
log=(folder/'player.log').read_text(encoding='utf-8-sig',errors='replace');assert 'Exception:' not in log
summary['gameplay']={'configured_seconds':65,'completion':complete.strip(),'JobTempAlloc_messages':log.count('Internal: JobTempAlloc')+log.count('Internal: There are remaining Allocations on the JobTempAlloc')}
checkpoint=json.loads((folder/'checkpoint-1m.json').read_text(encoding='utf-8'))
summary['gameplay']['checkpoint_groups']=[{'id':g['id'],'floor':g['floor'],'phase':g['phase'],'refuge_variant':g['refugeVariant'],'available_facilities':len(g['availableRestSites'])} for g in checkpoint['groups']]
summary['gameplay']['checkpoint_room_counts']=[len(p['knownRooms']) for p in checkpoint['plans']]
frame_rows=list(csv.DictReader((folder/'frames.csv').open(encoding='utf-8')))
frame_ms=sorted(float(r['frame_ms']) for r in frame_rows if 5<=float(r['elapsed'])<65)
summary['gameplay']['frame_ms_excluding_first_5s_and_cleanup']={'samples':len(frame_ms),'median':statistics.median(frame_ms),'p95':frame_ms[int(len(frame_ms)*.95)],'p99':frame_ms[int(len(frame_ms)*.99)],'max':max(frame_ms),'scope':'one bounded run; spikes remain; no performance certification'}
summary['retained_failed_attempts']=[]
for folder in sorted(RAW.glob('refuge-*')):
 for name in ['result.json','process-result.json']:
  p=folder/name
  if p.exists():
   result=json.loads(p.read_text(encoding='utf-8-sig'))
   if result['state']!='COMPLETE':summary['retained_failed_attempts'].append({'label':folder.name,'result':result})

summary['manual_review']={'contact_sheet':'refuge-gallery-dev-zh-final5/refuge-contact-sheet.jpg','scope':'23 thumbnails plus selected full-size model/environment/HUD/collapse images; remaining full-size images automated only'}
summary['presentation_source_hash']=hashlib.sha256(b''.join(p.name.encode()+b'\0'+p.read_bytes() for p in sorted((ROOT/'UnityProject/Assets/Scripts/Presentation').glob('*.cs')))).hexdigest()
with (ROOT/'Artifacts/refuge-exploration-evidence.csv').open('w',encoding='utf-8',newline='') as stream:
 writer=csv.writer(stream);writer.writerow(['path','bytes','sha256'])
 for folder in sorted(RAW.glob('refuge-*')):
  for p in sorted(folder.rglob('*')):
   if p.is_file() and p.suffix in {'.png','.jpg','.json','.csv','.log','.txt'}:writer.writerow([p.relative_to(ROOT).as_posix(),p.stat().st_size,hashlib.sha256(p.read_bytes()).hexdigest()])
(ROOT/'Artifacts/refuge-exploration-summary.json').write_text(json.dumps(summary,ensure_ascii=False,indent=2)+'\n',encoding='utf-8')
print(json.dumps({k:summary[k] for k in ['localization_keys','glyphs','autonomous_checks','oversearch_fixture','players','gameplay']},ensure_ascii=True,indent=2))
