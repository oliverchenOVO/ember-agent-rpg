import json,re,hashlib,shutil
from pathlib import Path
from PIL import Image,ImageDraw
base=Path('Artifacts/Phase311')
labels=['inspection-gallery-dev-zh-final','inspection-gallery-dev-en-final','inspection-gallery-release-zh-final','inspection-codex-release-final']
rows=[]
for name in labels:
 p=base/name;result=json.loads((p/'process-result.json').read_text(encoding='utf-8-sig'));layout=(p/'layout-results.txt').read_text(encoding='utf8');log=(p/'player.log').read_text(encoding='utf8',errors='replace');imgs=list(p.glob('*.png'))
 assert result['exitCode']==0 and 'issues: 0' in layout
 assert len(imgs)==(13 if 'codex' in name else 14)
 expected='1e68e67b6c79931fa21e8ca2863548a3b6e7f8dbb545a0679a8728c7b6e1a673' if 'release' in name else '86b4d90e92edc95a708dbcecba08a123b9af0df7032033502793a28df5509a63'
 assert result['managedAssemblyHash'].lower()==expected
 rows.append({'label':name,'layout':layout.strip(),'screenshots':len(imgs),'process':result,'exceptions':len(re.findall(r'\b(?:\w*Exception|AssertionException)\b',log)),'job_temp_warnings':log.count('JobTempAlloc'),'camera_check':(p/'camera-check.txt').read_text(encoding='utf8') if (p/'camera-check.txt').exists() else None})
 assert rows[-1]['exceptions']==0 and rows[-1]['job_temp_warnings']==0
h=hashlib.sha256()
for p in sorted(Path('UnityProject/Assets/Scripts/Presentation').glob('*.cs')):h.update(p.as_posix().encode());h.update(p.read_bytes())
out={'candidate':'agent-inspection','base_commit':'ac66729','presentation_source_hash':h.hexdigest(),'build_manifest':'Artifacts/agent-inspection-build-manifest.json','final_build':json.loads((base/'inspection-build-pair-final/result.json').read_text(encoding='utf-8-sig')),'galleries':rows,'screenshots':sum(r['screenshots'] for r in rows),'physical_mouse_test':False,'performance_certification':False,'concurrency':'User-owned older Player preserved; UI QA only, no performance claims.','prior_failures':['inspection-build-pair-1 timeout','inspection-build-pair-2 UPM startup failure','inspection-gallery-dev-zh-1 8 corrected issues'],'long_tests':{'5000_seed':'not run for this candidate','120_minute_soak':'not run for this candidate','balance_gate':'prior FAIL unchanged'}}
Path('Artifacts/agent-inspection-validation.json').write_text(json.dumps(out,ensure_ascii=False,indent=2)+'\n',encoding='utf8')
dest=Path('Docs/Media/AgentInspection');dest.mkdir(parents=True,exist_ok=True);source=base/labels[0]
for stem in ['inspect_bag','inspect_memory','inspect_camera_orbit','inspect_hover_blank','inspect_hover_skill']:shutil.copy2(source/(stem+'.png'),dest/(stem+'.png'))
images=sorted(source.glob('*.png'));sheet=Image.new('RGB',(960,((len(images)+2)//3)*205),(15,23,28));draw=ImageDraw.Draw(sheet)
for i,p in enumerate(images):
 img=Image.open(p).convert('RGB');img.thumbnail((312,176));x=(i%3)*320;y=(i//3)*205;sheet.paste(img,(x,y));draw.text((x+4,y+178),p.stem,fill=(220,225,220))
sheet.save(dest/'inspection-contact-sheet.jpg',quality=90)
print(json.dumps({'screenshots':out['screenshots'],'source_hash':h.hexdigest(),'results':'all passed'}))
