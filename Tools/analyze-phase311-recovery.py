"""Analyse completed, hash-bound recovery cohorts; partial prefixes stay partial."""
import argparse, collections, csv, hashlib, json, math
from pathlib import Path
ROOT=Path(__file__).resolve().parents[1]
p=argparse.ArgumentParser();p.add_argument('--input-root',default='Artifacts/Phase311/exp1-ablations');p.add_argument('--prefix',default='phase3_1_1_exp1');args=p.parse_args()
folder=ROOT/args.input_root;tables={k:[] for k in ['ablation','revive','skill-economy','paired-seeds','resource-economy']};complete={}
def wilson(success,n):
    if not n:return (0,0)
    z=1.96;den=1+z*z/n;center=(success/n+z*z/(2*n))/den;half=z*math.sqrt(success/n*(1-success/n)/n+z*z/(4*n*n))/den
    return (100*(center-half),100*(center+half))
for d in sorted(folder.iterdir()):
    if not (d/'summary.txt').exists():continue
    manifest=json.loads((d/'manifest.json').read_text(encoding='utf-8-sig'));cfg=manifest['config'];ss=[json.loads(l) for l in (d/'results.jsonl').read_text(encoding='utf-8-sig').splitlines()]
    assert [s['seed'] for s in ss]==list(range(cfg['firstSeed'],cfg['firstSeed']+len(ss)))
    rows=[r for s in ss for r in s['encounters']];ten=[r for r in rows if r['floor']==10];resolved=[r for r in ten if r['result']!='Active'];wins=sum(r['result']=='Clear' for r in resolved);low,high=wilson(wins,len(resolved))
    identity=dict(label=d.name,balance_version=manifest['balanceVersion'],runtime_hash=manifest['runtimeHash'],config_hash=manifest['configHash'],executable_hash=manifest.get('executableHash',''),assembly_hash=manifest.get('managedAssemblyHash',''))
    tables['ablation'].append(dict(**identity,seeds=len(ss),requested=cfg['count'],complete=len(ss)==cfg['count'],floor10_encounters=len(ten),floor10_censored=len(ten)-len(resolved),floor10_clear=wins,clear_percent=100*wins/max(1,len(resolved)),wilson_low=low,wilson_high=high,death_count=sum(r['deaths'] for r in ten),mean_clear_seconds=sum(r['clearTime'] for r in ten if r['result']=='Clear')/max(1,wins),potions_floor10=sum(r['potions'] for r in ten),mp_spent_floor10=sum(u['mpSpent'] for r in ten for u in r['team']),mp_drained_floor10=sum(u['mpDrained'] for r in ten for u in r['team']),nonterminal=sum(s['result']=='Nonterminal' for s in ss),rules=json.dumps(cfg['rules'],sort_keys=True),raw_sha256=hashlib.sha256((d/'results.jsonl').read_bytes()).hexdigest()))
    economy=collections.defaultdict(collections.Counter);revive=collections.defaultdict(collections.Counter)
    for r in rows:
        for skill in r.get('skillEconomy',[]):
            for k,v in skill.items():
                if k!='skill':economy[skill['skill']][k]+=v
        for observation in r.get('reviveOpportunities',[]):
            v=revive[observation['reasonNotCast']];v['samples']+=observation['samples'];v['seconds']+=observation['seconds']
            if all(observation[k] for k in ['deadTargetExists','healerAlive','hasSkill','equipped','cooldownReady','manaSufficient','distanceValid','losValid','dangerAcceptable','targetRevivable']):v['joint_eligible_seconds']+=observation['seconds']
    for reason,counts in revive.items():tables['revive'].append(dict(**identity,reason_not_cast=reason,**{k:counts[k] for k in ['samples','seconds','joint_eligible_seconds']}))
    for skill,v in economy.items():
        mana=v['manaCost'];casts=v['casts'];raw=v['rawHealing'];eff=v['effectiveHealing']
        tables['skill-economy'].append(dict(**identity,skill=skill,casts=casts,effective_damage=v['effectiveDamage'],raw_healing=raw,effective_healing=eff,overheal=raw-eff,overheal_fraction=(raw-eff)/raw if raw else '',mana_cost=mana,damage_per_mana=v['effectiveDamage']/mana if mana else '',hp_per_mana=eff/mana if mana else '',mean_mana=mana/max(1,casts),cooldown=v['cooldownSum']/max(1,casts),cast_time=v['castTime']/max(1,casts),travel_seconds=v['travelSeconds'],immediate_heal_per_cooldown=(eff/max(1,casts))/(v['cooldownSum']/max(1,casts)) if v['cooldownSum'] else '',scope='instant direct effects; Regen/DoT separately recorded; travel includes attempts'))
    # Conservation captures generated/salvaged materials, including inventory overflow.
    # This is valid only for the newer explicit reservation/kit-spend instrumentation.
    if ss and 'inventory' in ss[0]:
        inventories=[v for s in ss for v in s['inventory']];units=[u for r in rows for u in r['team']];spent=sum(u['materialsSpent'] for u in units)
        recovery=collections.Counter()
        for r in rows:
            for v in r['recovery']:recovery[v['id']]+=v['count']
        tables['resource-economy'].append(dict(**identity,seeds=len(ss),materials_spent=spent,materials_generated=spent+sum(v['materials']-v['initialMaterials'] for v in inventories),materials_remaining=sum(v['materials'] for v in inventories),living_materials_remaining=sum(v['materials'] for v in inventories if v['alive']),hp_potions_remaining=sum(v['hpPotions'] for v in inventories),mp_potions_remaining=sum(v['mpPotions'] for v in inventories),living_hp_potions=sum(v['hpPotions'] for v in inventories if v['alive']),living_mp_potions=sum(v['mpPotions'] for v in inventories if v['alive']),rest_agent_seconds=sum(u['restSeconds'] for u in units),hp_potions_used=recovery['Potion:hp'],mp_potions_used=recovery['Potion:mp'],true_revives=recovery['Revive'],revive_heals_on_living=recovery['revive'],encounters=len(rows),potions_per_encounter=sum(r['potions'] for r in rows)/max(1,len(rows))))
    if len(ss)==cfg['count']:complete[d.name]={s['seed']:any(r['floor']==10 and r['result']=='Clear' for r in s['encounters']) for s in ss}
if 'control-baseline' in complete:
    baseline=complete['control-baseline']
    for label,cohort in complete.items():
        assert set(baseline)==set(cohort)
        tables['paired-seeds'].append(dict(label=label,baseline_only=sum(baseline[s] and not cohort[s] for s in cohort),variant_only=sum(cohort[s] and not baseline[s] for s in cohort),both_clear=sum(baseline[s] and cohort[s] for s in cohort),neither_clear=sum(not baseline[s] and not cohort[s] for s in cohort)))
for name,rows in tables.items():
    if rows:
        with (ROOT/'Artifacts'/(args.prefix+'_'+name+'.csv')).open('w',encoding='utf-8-sig',newline='') as out:
            writer=csv.DictWriter(out,fieldnames=list(rows[0]));writer.writeheader();writer.writerows(rows)
for row in tables['ablation']:print(row['label'],row['seeds'],row['floor10_clear'],row['floor10_encounters'],row['floor10_censored'],round(row['clear_percent'],2))

