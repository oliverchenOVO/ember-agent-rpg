"""Analyze paired bounded fixtures; do not treat them as a full balance gate."""
import json,re,hashlib,statistics,shutil,csv
from pathlib import Path
root=Path('Artifacts/Phase311');dest=Path('Artifacts');manifest=json.loads((dest/'team-balance-build-manifest.json').read_text(encoding='utf-8'))
sets={}
for label,folder in [('baseline','team-balance-baseline'),('candidate','team-balance-build-pair-verified')]:
 source=root/folder/'diagnostics'/('team-'+label+'.jsonl');rows=[json.loads(line) for line in source.read_text(encoding='utf-8-sig').splitlines()];assert len(rows)==36
 assert len({(r['seed'],r['floor'],r['composition']) for r in rows})==36
 metadata=(source.parent/('team-'+label+'-manifest.txt')).read_text(encoding='utf-8-sig');hash_value=re.search(r'RuntimeHash=(\w+)',metadata).group(1)
 expected=manifest['source_runtime_hash'] if label=='candidate' else json.loads((dest/'pressure-core-build-manifest.json').read_text(encoding='utf-8'))['source_runtime_hash'];assert hash_value==expected
 shutil.copy2(source,dest/('team-balance-'+label+'.jsonl'));shutil.copy2(source.parent/('team-'+label+'-manifest.txt'),dest/('team-balance-'+label+'-manifest.txt'));sets[label]=rows
assert {(r['seed'],r['floor'],r['composition']) for r in sets['baseline']}=={(r['seed'],r['floor'],r['composition']) for r in sets['candidate']}
summary=[]
for comp in ['Warrior','Archer','Mage','Healer','Mixed','NoHealer']:
 row={'composition':comp,'samples_per_version':6}
 for label,rows in sets.items():
  sample=[r for r in rows if r['composition']==comp]
  row[label]={'clear':sum(r['clear'] for r in sample),'mean_dps':statistics.mean(r['damage']/r['seconds'] for r in sample),'mean_mana_fraction':statistics.mean(r['manaRemaining'] for r in sample),'mean_seconds':statistics.mean(r['seconds'] for r in sample),'mean_living':statistics.mean(r['living'] for r in sample),'mean_overheal':statistics.mean(r['overheal'] for r in sample)}
 row['dps_change_percent']=(row['candidate']['mean_dps']/row['baseline']['mean_dps']-1)*100;summary.append(row)
galleries=[]
for label,kind,count in [('team-gallery-dev-zh-verified','development',13),('team-gallery-release-en-verified','release',13),('team-pressure-release-verified','release',12),('team-inspection-release-verified','release',14)]:
 p=root/label;process=json.loads((p/'process-result.json').read_text(encoding='utf-8-sig'));layout=(p/'layout-results.txt').read_text(encoding='utf-8');log=(p/'player.log').read_text(encoding='utf-8',errors='replace')
 assert process['exitCode']==0 and 'issues: 0' in layout and len(list(p.glob('*.png')))==count
 assert process['managedAssemblyHash'].lower()==manifest['builds'][kind]['assembly_hash']
 assert not re.search(r'\b\w*Exception\b',log) and 'JobTempAlloc' not in log
 galleries.append({'label':label,'screenshots':count,'layout':layout.strip(),'process':process,'exceptions':0,'job_temp_warnings':0})
log=(root/'team-balance-build-pair-verified/Editor.log').read_text(encoding='utf-8',errors='replace');assert 'TEAM BALANCE UNIT PASSED / checks=22' in log and 'PRESSURE VALIDATION PASSED / checks=21' in log and log.count('EMBER WINDOWS BUILD PASSED')==2
h=hashlib.sha256()
for p in sorted(Path('UnityProject/Assets/Scripts/Presentation').glob('*.cs')):h.update(p.as_posix().encode());h.update(p.read_bytes())
out={'candidate':'team-balance','base_commit':'bd4c9ac','summary':summary,'paired_encounters':36,'analyzed_encounters':72,'fixture':'Two seeds, floors 6/8/10, identical controlled attributes, gear and four-skill loadouts; actual Boss abilities enabled; no progression/rest validation in the benchmark. Each encounter capped at 180 simulated seconds; each invocation capped at 60 wall seconds.', 'unit_checks':{'team':22,'pressure_regression':21},'galleries':galleries,'screenshots':sum(r['screenshots'] for r in galleries),'presentation_hash':h.hexdigest(),'build_manifest':'Artifacts/team-balance-build-manifest.json','limits':{'balance_gate':'historical FAIL; current full gate not run','5000_seed':'not run','120_minute_soak':'not run','performance':'not certified; UI galleries overlap'},'intermediate_benchmarks':['team-balance-candidate-1','team-balance-build-pair-1','team-balance-build-pair-final'],'overheal_note':'Much of the decrease removes zero-effective HolyAttack recovery events at full health; it is not equivalent to mana savings.'}
(dest/'team-balance-validation.json').write_text(json.dumps(out,ensure_ascii=False,indent=2)+'\n',encoding='utf-8')
with (dest/'team-balance-comparison.csv').open('w',encoding='utf-8-sig',newline='') as f:
 writer=csv.writer(f);writer.writerow(['composition','baseline_dps','candidate_dps','change_percent','baseline_mana_fraction','candidate_mana_fraction','baseline_clear','candidate_clear','samples_per_version'])
 for r in summary:writer.writerow([r['composition'],r['baseline']['mean_dps'],r['candidate']['mean_dps'],r['dps_change_percent'],r['baseline']['mean_mana_fraction'],r['candidate']['mean_mana_fraction'],r['baseline']['clear'],r['candidate']['clear'],6])
media=Path('Docs/Media/TeamBalance');media.mkdir(parents=True,exist_ok=True)
for name in ['codex_mage','codex_archer_detail','codex_healer_detail']:shutil.copy2(root/'team-gallery-dev-zh-verified'/(name+'.png'),media/(name+'.png'))
print(json.dumps({'screenshots':out['screenshots'],'encounters':72,'summary':summary},ensure_ascii=True))
