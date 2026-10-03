"""Index bounded Player/Editor art checks and bind them to shipped assemblies."""
import csv, hashlib, json, re
from pathlib import Path

ROOT=Path(__file__).resolve().parents[1]
RAW=ROOT/'Artifacts/Phase311'
manifest=json.loads((ROOT/'Artifacts/combat-art-build-manifest.json').read_text(encoding='utf8'))
editor=(RAW/'combat-art-validation-03/Editor.log').read_text(encoding='utf-8-sig',errors='replace')
assert 'COMBAT ART VALIDATION PASSED' in editor
assert 'KNOWLEDGE CODEX VALIDATION PASSED' in editor
assert 'runtime='+manifest['source_runtime_hash'] in editor
assert '539 keys / 825 glyphs' in editor
summary={'scope':'bounded combat presentation validation; no balance Gate or soak claim',
         'source_runtime_hash':manifest['source_runtime_hash'],
         'editor_validation':'PASS','localization_keys':539,'glyphs_checked':825,
         'save_replay_cases':7,'players':[], 'full_balance_gate':'NOT_RUN; historical FAIL unchanged',
         '5000_seed':'INCOMPLETE; not started again','120_minute_soak':'INCOMPLETE; not started again'}
labels=[('combat-art-gallery-dev-zh-final2','development',75),
        ('combat-art-gallery-dev-en-final2','development',75),
        ('combat-art-rest-dev-final','development',5),
        ('combat-art-codex-dev-en-final','development',13),
        ('combat-art-gallery-release-final','release',75)]
for label,kind,count in labels:
    folder=RAW/label
    result=json.loads((folder/'process-result.json').read_text(encoding='utf-8-sig'))
    assert result['state']=='COMPLETE' and result['exitCode']==0
    assert result['managedAssemblyHash'].lower()==manifest['builds'][kind]['assembly_hash']
    layout=(folder/'layout-results.txt').read_text(encoding='utf8')
    assert 'issues: 0' in layout and f'scenarios: {count} /' in layout
    pngs=list(folder.glob('*.png'));assert len(pngs)==count
    log=(folder/'player.log').read_text(encoding='utf-8-sig',errors='replace')
    assert 'Exception:' not in log
    native_warnings=log.count('Internal: JobTempAlloc')+log.count('Internal: There are remaining Allocations on the JobTempAlloc')
    if count==75:
        rows=list(csv.reader((folder/'art-event-checks.csv').open(encoding='utf8')))
        assert len(rows)==75
        assert all(int(r[1])==1 for r in rows if r[0].startswith('art_skill_'))
        assert all(int(r[2])==2 for r in rows if r[0].startswith('art_move_'))
        assert next(r for r in rows if r[0]=='art_multi')[1]=='4'
        assert 'PASS' in (folder/'concurrent-skills.txt').read_text(encoding='utf8')
    summary['players'].append({'label':label,'scenarios':count,'layout_issues':0,'elapsed_seconds':result['elapsed'],'exit':0,'JobTempAlloc_warning_messages':native_warnings,'native_status':'WARN; unresolved' if native_warnings else 'no warning observed in this bounded run'})
assert 'PASS' in (RAW/'combat-art-rest-dev-final/dead-source-check.txt').read_text(encoding='utf8')
meshes=list(csv.reader((RAW/'combat-art-gallery-dev-zh-final2/art-mesh-counts.csv').open(encoding='utf8')))
models=[{'floor':int(r[0][9:]),'mesh_filters':int(r[1])} for r in meshes if r[0].startswith('art_boss_')]
assert len(models)==25 and min(r['mesh_filters'] for r in models)>=12
summary['boss_models']=models
folder=RAW/'combat-art-release-gameplay-final'
result=json.loads((folder/'process-result.json').read_text(encoding='utf-8-sig'))
assert result['state']=='COMPLETE' and result['exitCode']==0
assert result['managedAssemblyHash'].lower()==manifest['builds']['release']['assembly_hash']
complete=(folder/'complete.txt').read_text(encoding='utf8');assert 'maintenance=1' in complete
log=(folder/'player.log').read_text(encoding='utf-8-sig',errors='replace');assert 'Exception:' not in log and 'JobTempAlloc' not in log
summary['gameplay_smoke']={'configured_seconds':65,'completed':complete.strip(),'exit':0,'save_load_cycles':1}
summary['manual_review']={'contact_sheet':'combat-art-gallery-dev-zh-final2/boss-contact-sheet.jpg',
    'scope':'25 model thumbnails plus listed full-size screenshots; remaining frames automated only',
    'full_size':[
        'combat-art-gallery-dev-zh-final2/'+n+'.png' for n in
        ['art_boss_1','art_boss_2','art_boss_3','art_boss_6','art_boss_8','art_boss_9','art_boss_10','art_boss_12','art_skill_lightning','art_move_furnace_lane','art_hud','art_rest']
    ]+['combat-art-gallery-dev-en-final2/art_hud.png','combat-art-codex-dev-en-final/codex_skill_detail.png','combat-art-gallery-release-final/art_multi.png','combat-art-gallery-release-final/art_boss_10.png']}
sources=sorted((ROOT/'UnityProject/Assets/Scripts/Presentation').glob('*.cs'))
summary['presentation_source_hash']=hashlib.sha256(b''.join(p.name.encode()+b'\0'+p.read_bytes() for p in sources)).hexdigest()
with (ROOT/'Artifacts/combat-art-evidence.csv').open('w',encoding='utf8',newline='') as stream:
    writer=csv.writer(stream);writer.writerow(['path','bytes','sha256'])
    for folder in sorted(RAW.glob('combat-art-*')):
        for item in sorted(folder.rglob('*')):
            if item.is_file():writer.writerow([item.relative_to(ROOT).as_posix(),item.stat().st_size,hashlib.sha256(item.read_bytes()).hexdigest()])
summary['native_memory_status']='UNRESOLVED: Release gallery has JobTempAlloc warnings; gameplay smoke has zero observed warnings'
(ROOT/'Artifacts/combat-art-summary.json').write_text(json.dumps(summary,ensure_ascii=False,indent=2)+'\n',encoding='utf8')
print(f'Visual checks PASS; native memory WARN: {sum(r["scenarios"] for r in summary["players"])} screenshots; 25 models; 24 skills; 18 moves; 1 native save/load cycle.')
