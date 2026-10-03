import json,re,hashlib,shutil
from pathlib import Path
from PIL import Image,ImageDraw
base=Path('Artifacts/Phase311')
labels=['readability-gallery-dev-zh-1','readability-gallery-dev-en-final','readability-gallery-release-zh-final','readability-codex-release-final']
rows=[]
for name in labels:
 p=base/name;result=json.loads((p/'process-result.json').read_text(encoding='utf-8-sig'));layout=(p/'layout-results.txt').read_text(encoding='utf8');log=(p/'player.log').read_text(encoding='utf8',errors='replace');imgs=list(p.glob('*.png'))
 assert result['exitCode']==0 and 'issues: 0' in layout
 assert len(imgs)==(13 if 'codex' in name else 12)
 expected='e98341a273b9662203670243e9cf26946030ab4b14a387c11468de8c076e622f' if 'release' in name else '66750a1930c48181792e4f8712bd60b58ba80b5aca6cd8eb462286c87852fd11'
 assert result['managedAssemblyHash'].lower()==expected
 rows.append({'label':name,'layout':layout.strip(),'screenshots':len(imgs),'process':result,'exceptions':len(re.findall(r'\b(?:\w*Exception|AssertionException)\b',log)),'job_temp_warnings':log.count('JobTempAlloc'),'readability_check':(p/'readability-check.txt').read_text(encoding='utf8') if (p/'readability-check.txt').exists() else None})
 assert rows[-1]['exceptions']==0 and rows[-1]['job_temp_warnings']==0
h=hashlib.sha256()
for p in sorted(Path('UnityProject/Assets/Scripts/Presentation').glob('*.cs')):h.update(p.as_posix().encode());h.update(p.read_bytes())
out={'candidate':'combat-readability','base_commit':'c01a6b4','presentation_source_hash':h.hexdigest(),'build_manifest':'Artifacts/combat-readability-build-manifest.json','final_build':json.loads((base/'readability-build-pair-final/result.json').read_text(encoding='utf-8-sig')),'galleries':rows,'screenshots':sum(r['screenshots'] for r in rows),'performance_certification':False,'concurrency':'User-owned older Player preserved; UI QA only, no performance claims.','prior_failures':['readability-build-pair-1 UnityLinker native access violation'],'long_tests':{'5000_seed':'not run for this candidate','120_minute_soak':'not run for this candidate','balance_gate':'prior FAIL unchanged'}}
Path('Artifacts/combat-readability-validation.json').write_text(json.dumps(out,ensure_ascii=False,indent=2)+'\n',encoding='utf8')
dest=Path('Docs/Media/CombatReadability');dest.mkdir(parents=True,exist_ok=True);source=base/labels[0]
for stem in ['read_pressure','read_combined','read_book_lyra','read_book_missing','read_book_full']:shutil.copy2(source/(stem+'.png'),dest/(stem+'.png'))
images=sorted(source.glob('*.png'));sheet=Image.new('RGB',(960,((len(images)+2)//3)*205),(15,23,28));draw=ImageDraw.Draw(sheet)
for i,p in enumerate(images):
 img=Image.open(p).convert('RGB');img.thumbnail((312,176));x=(i%3)*320;y=(i//3)*205;sheet.paste(img,(x,y));draw.text((x+4,y+178),p.stem,fill=(220,225,220))
sheet.save(dest/'readability-contact-sheet.jpg',quality=90)
print(json.dumps({'screenshots':out['screenshots'],'source_hash':h.hexdigest(),'results':'all passed'}))
