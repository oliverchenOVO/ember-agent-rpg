"""Analyze encounter exposures, not independent parties as independent runs. No third-party dependencies."""
import argparse, collections, csv, json, statistics
from pathlib import Path

ROOT = Path(__file__).resolve().parents[1]
def analyze(source, dest):
    floors=collections.defaultdict(list); classes=collections.defaultdict(lambda:[0,0,0]); skills=collections.Counter(); weapons=collections.defaultdict(lambda:[0,0])
    with source.open(encoding='utf-8-sig') as f:
        for line in f:
            r=json.loads(line)
            # The first baseline instrument also opened bookkeeping rows for rest-only splits.
            # No baseline agent died: non-Clear rows are these extra bookkeeping rows, not encounters.
            if source.stem=='baseline' and r['result']!='Clear':continue
            floors[r['floor']].append(r)
            for u in r['team']:
                v=classes[u['profession']];v[0]+=u['damage'];v[1]+=u['healing'];v[2]+=1
                v=weapons[u['weapon']];v[0]+=u['damage'];v[1]+=1
            skills.update({v['id']:v['count'] for v in r['skills']})
    dest.parent.mkdir(parents=True,exist_ok=True)
    rawkeys=['run_id','seed','run','group','floor','clearTime','boss','team','deaths','damage','damageTaken','healing','potions','skills','loot','lootItems','crafts','splits','rejoins','restActions','restSites','collapses','result','finalResult']
    with dest.with_name(dest.name+'-encounters.csv').open('w',encoding='utf-8-sig',newline='') as f:
        w=csv.DictWriter(f,fieldnames=rawkeys);w.writeheader()
        for rows in floors.values():
            for r in rows:
                row={k:r.get(k,'') for k in rawkeys};row['run_id']=str(r['seed'])+':'+str(r['run'])
                for k in ['team','skills','lootItems','restSites']:row[k]=json.dumps(row[k],ensure_ascii=False)
                w.writerow(row)
    keys=['floor','encounters','runs_reached','runs_cleared','run_clear_rate','encounter_clear_rate','average_clear_time','deaths','damage_taken','healing','potions','average_survivors','split_encounter_rate','average_gear_quality','crafts','rest_actions','collapses']
    summary=[]
    for floor,rows in sorted(floors.items()):
        clear=[r for r in rows if r['result']=='Clear'];reached={r['seed'] for r in rows};cleared={r['seed'] for r in clear}
        avg=lambda key,items=rows:statistics.mean(r[key] for r in items) if items else 0
        summary.append(dict(zip(keys,[floor,len(rows),len(reached),len(cleared),len(cleared)/len(reached),len(clear)/len(rows),avg('clearTime',clear),sum(r['deaths'] for r in rows),avg('damageTaken'),avg('healing'),avg('potions'),avg('survivors'),sum(r['splits']>0 for r in rows)/len(rows),avg('gearQuality'),sum(r['crafts'] for r in rows),sum(r['restActions'] for r in rows),sum(r['collapses'] for r in rows)])))
    with dest.with_suffix('.csv').open('w',encoding='utf-8-sig',newline='') as f:
        w=csv.DictWriter(f,fieldnames=keys);w.writeheader();w.writerows(summary)
    result=dict(floors=summary,skills=dict(skills),classes={k:dict(damage_per_exposure=v[0]/v[2],healing_per_exposure=v[1]/v[2],exposures=v[2]) for k,v in classes.items()},weapons={k:dict(damage_per_exposure=v[0]/v[1],exposures=v[1]) for k,v in weapons.items()})
    dest.with_suffix('.json').write_text(json.dumps(result,ensure_ascii=False,indent=2),encoding='utf-8')
    lines=['# Telemetry analysis','', '單位是分隊樓層 encounter；run_clear_rate 依 seed 去重。死亡含戰鬥/休息；技能與物品輸出以 exposure 正規化，不能推斷因果或單件強度。','', '| Floor | Reached seeds | Clear % | Time | Deaths | Taken | Heal | Potions | Survivors | Split % | Quality |','|---|---:|---:|---:|---:|---:|---:|---:|---:|---:|---:|']
    for r in summary:lines.append('| {floor} | {runs_reached} | {run_clear_rate:.1%} | {average_clear_time:.1f} | {deaths} | {damage_taken:.1f} | {healing:.1f} | {potions:.2f} | {average_survivors:.2f} | {split_encounter_rate:.1%} | {average_gear_quality:.2f} |'.format(**r))
    flags=[]
    for i,r in enumerate(summary):
        if r['deaths']==0:flags.append(f"{r['floor']}F: no recorded deaths")
        if i and r['run_clear_rate']<summary[i-1]['run_clear_rate']-.15:flags.append(f"{r['floor']}F: conditional clear-rate spike")
    lines+=['','## Flags']+['- '+x for x in flags]+['','## Skill usage',json.dumps(dict(skills),ensure_ascii=False),'','## Class exposure',json.dumps(result['classes'],ensure_ascii=False),'','## Weapon exposure',json.dumps(result['weapons'],ensure_ascii=False)]
    dest.with_suffix('.md').write_text('\n'.join(lines)+'\n',encoding='utf-8');print(dest.with_suffix('.md'))
if __name__=='__main__':
    p=argparse.ArgumentParser();p.add_argument('label');args=p.parse_args()
    analyze(ROOT/'Artifacts/Phase3'/f'{args.label}.jsonl',ROOT/'Artifacts'/f'phase3-{args.label}-analysis')
