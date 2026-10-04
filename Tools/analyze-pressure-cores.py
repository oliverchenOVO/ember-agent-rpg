"""Verify bounded pressure galleries against the actual shipped assemblies."""
import json,re,hashlib,shutil
from pathlib import Path
from PIL import Image,ImageDraw
base=Path('Artifacts/Phase311')
manifest=json.loads(Path('Artifacts/pressure-core-build-manifest.json').read_text(encoding='utf-8'))
labels=[('pressure-gallery-dev-zh-final',12,'development'),('pressure-gallery-dev-en-final',12,'development'),('pressure-gallery-release-zh-final',12,'release'),('pressure-codex-release',13,'release'),('pressure-readability-release',12,'release'),('pressure-inspection-release',14,'release')]
rows=[]
for name,count,kind in labels:
 p=base/name;result=json.loads((p/'process-result.json').read_text(encoding='utf-8-sig'));layout=(p/'layout-results.txt').read_text(encoding='utf-8');log=(p/'player.log').read_text(encoding='utf-8',errors='replace');imgs=list(p.glob('*.png'))
 assert result['exitCode']==0 and 'issues: 0' in layout and len(imgs)==count,name
 assert result['managedAssemblyHash'].lower()==manifest['builds'][kind]['assembly_hash'],name
 exceptions=len(re.findall(r'\b(?:\w*Exception|AssertionException)\b',log));warnings=log.count('JobTempAlloc');assert exceptions==warnings==0,name
 check=(p/'pressure-checks.txt').read_text(encoding='utf-8') if (p/'pressure-checks.txt').exists() else None
 if name.startswith('pressure-gallery'):assert check.count('/ PASS /')==12
 rows.append({'label':name,'screenshots':count,'layout':layout.strip(),'process':result,'exceptions':exceptions,'job_temp_warnings':warnings,'checks':check})
log=(base/'pressure-build-pair-final/Editor.log').read_text(encoding='utf-8',errors='replace')
assert 'PRESSURE VALIDATION PASSED / checks=21' in log and log.count('EMBER WINDOWS BUILD PASSED')==2
h=hashlib.sha256()
for p in sorted(Path('UnityProject/Assets/Scripts/Presentation').glob('*.cs')):h.update(p.as_posix().encode());h.update(p.read_bytes())
out={'candidate':'pressure-cores','base_commit':'6d0fc3f','build_manifest':'Artifacts/pressure-core-build-manifest.json','presentation_source_hash':h.hexdigest(),'unit_checks':21,'ai_fixtures':[{'seed':1729,'seconds':40,'initial_core_hp':270,'remaining_core_hp':0},{'seed':1730,'seconds':40,'initial_core_hp':270,'remaining_core_hp':0}], 'fixture_limit':'Boss scheduled abilities suppressed in AI source-disabling fixtures; not a progression or balance gate.', 'galleries':rows,'screenshots':sum(r['screenshots'] for r in rows),'long_tests':{'5000_seed':'not run for this candidate','120_minute_soak':'not run for this candidate','balance_gate':'historical FAIL; new balance not certified'},'performance_certification':False,'prior_attempts':['pressure-build-pair-1: test used zero-pressure floor','pressure-build-pair-2: legacy list normalization exposed','pressure-build-pair-3: invalid-source fixture left legacy version flag','pressure-build-pair-4: successful initial candidate; panel moved aside and inner wave animation added before final build']}
Path('Artifacts/pressure-core-validation.json').write_text(json.dumps(out,ensure_ascii=False,indent=2)+'\n',encoding='utf-8')
dest=Path('Docs/Media/PressureCores');dest.mkdir(parents=True,exist_ok=True);source=base/'pressure-gallery-release-zh-final'
for stem in ['pressure_warning','pressure_active','pressure_disabled','pressure_combined','pressure_dossier']:shutil.copy2(source/(stem+'.png'),dest/(stem+'.png'))
imgs=sorted(source.glob('*.png'));sheet=Image.new('RGB',(960,((len(imgs)+2)//3)*205),(15,23,28));draw=ImageDraw.Draw(sheet)
for i,p in enumerate(imgs):
 img=Image.open(p).convert('RGB');img.thumbnail((312,176));x=i%3*320;y=i//3*205;sheet.paste(img,(x,y));draw.text((x+4,y+178),p.stem,fill=(220,225,220))
sheet.save(dest/'pressure-contact-sheet.jpg',quality=90)
print(json.dumps({'screenshots':out['screenshots'],'checks':21,'results':'all passed'}))
