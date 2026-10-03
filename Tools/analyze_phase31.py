"""Read complete checkpointed seeds only; never treat a partial run as a passed gate."""
import argparse, collections, csv, hashlib, json
from pathlib import Path

ROOT = Path(__file__).resolve().parents[1]
SOURCES = ['Other','Pressure','Enrage','Status','Summon','Hazard','Ability','FinalPulse','Collapse','RestRisk']
CAUSES = ['BURST','ATTRITION','MANA_COLLAPSE','HAZARD','FAILED_DODGE','FAILED_INTERRUPT','NO_RECOVERY','AI_PRIORITY','COLLAPSE','OTHER']

def read_run(label):
    folder = ROOT/'Artifacts/Phase31'/label
    manifest = json.loads((folder/'manifest.json').read_text(encoding='utf-8-sig'))
    seeds = [json.loads(line) for line in (folder/'results.jsonl').read_text(encoding='utf-8-sig').splitlines()]
    assert [s['seed'] for s in seeds] == list(range(manifest['config']['firstSeed'],manifest['config']['firstSeed']+len(seeds)))
    return manifest,seeds

def write_csv(name,rows):
    if not rows:return
    with (ROOT/'Artifacts'/name).open('w',encoding='utf-8-sig',newline='') as output:
        writer=csv.DictWriter(output,fieldnames=list(rows[0]));writer.writeheader();writer.writerows(rows)

def main():
    parser=argparse.ArgumentParser();parser.add_argument('labels',nargs='+');parser.add_argument('--prefix',default='phase3_1');args=parser.parse_args()
    breakdown=[];compositions=[];inventory=[];text=[]
    for label in args.labels:
        manifest,seeds=read_run(label);cfg=manifest['config'];rows=[r for s in seeds for r in s['encounters']];ten=[r for r in rows if r['floor']==10]
        sha=hashlib.sha256((ROOT/'Artifacts/Phase31'/label/'results.jsonl').read_bytes()).hexdigest()
        inventory.append(dict(label=label,completed=len(seeds),requested=cfg['count'],complete=len(seeds)==cfg['count'],composition=cfg['composition'],first_seed=cfg['firstSeed'],last_seed=seeds[-1]['seed'] if seeds else '',runtime_hash=manifest['runtimeHash'],config_hash=manifest['configHash'],raw_sha256=sha,balance_version=manifest['balanceVersion'],started_utc=manifest['startedUtc']))
        recovery=collections.defaultdict(lambda:collections.Counter());skills=collections.Counter();loot=collections.Counter();weapons=collections.Counter();opportunities=collections.defaultdict(lambda:collections.Counter())
        for r in rows:
            for s in r['skills']:skills[s['id']]+=s['count']
            for s in r['lootItems']:loot[s['id']]+=s['count']
            for u in r['team']:
                weapons[u['profession']+':'+u['weapon']]+=1
                for s in u.get('opportunities',[]):
                    for k,field in [('unlocked_seconds','raw'),('equipped_seconds','effective'),('ready_seconds','mana')]:opportunities[s['id']][k]+=s[field]
            for s in r.get('recovery',[]):
                for k in ['raw','effective','overheal','mana','count']:recovery[s['id']][k]+=s[k]
        for phase in range(3):
            ps=[p for r in ten for p in r.get('phases',[]) if p['phase']==phase];deaths=[d for r in ten for d in r.get('deathEvents',[]) if d['phase']==phase]
            damage=collections.Counter();killers=collections.Counter();causes=collections.Counter()
            for p in ps:
                for s in p['damageSources']:damage[s['id']]+=s['effective']
            for d in deaths:killers[SOURCES[d['source']]+':'+d['ability']]+=1;causes[CAUSES[d['cause']]]+=1
            seconds=sum(p['agentSeconds'] for p in ps)
            breakdown.append(dict(label=label,runtime_hash=manifest['runtimeHash'],composition=cfg['composition'],phase=phase+1,encounters=len(ps),deaths=len(deaths),mean_phase_seconds=sum(p['duration'] for p in ps)/max(1,len(ps)),damage_taken=sum(v for k,v in damage.items() if k!='RestRisk'),rest_damage_excluded=damage.get('RestRisk',0),mean_remaining_mp=sum(p['mpSum'] for p in ps)/max(1,seconds),potions=sum(p['potions'] for p in ps),skill_ready_fraction=sum(p['skillReadySeconds'] for p in ps)/max(1,seconds),heal_ready_fraction=sum(p['healReadySeconds'] for p in ps)/max(1,seconds),defense_ready_fraction=sum(p['defenseReadySeconds'] for p in ps)/max(1,seconds),potion_ready_fraction=sum(p['potionReadySeconds'] for p in ps)/max(1,seconds),low_mp_deaths=sum(d['mpFraction']<.2 for d in deaths),potion_available_at_death=sum(d['potionAvailable'] for d in deaths),heal_available_at_death=sum(d['healingAvailable'] for d in deaths),damage_sources=json.dumps(damage,sort_keys=True),lethal_sources=json.dumps(killers,sort_keys=True),primary_causes=json.dumps(causes,sort_keys=True)))
        units=[u for r in rows for u in r['team']];death_floors=collections.Counter({})
        for r in rows:
            for d in r.get('deathEvents',[]):death_floors[r['floor']]+=1
        combat_recovery={k:v for k,v in recovery.items() if not k.startswith('Rest:') and k!='Potion:mp' and k!='RestRescue'}
        compositions.append(dict(label=label,composition=cfg['composition'],seeds=len(seeds),requested=cfg['count'],reached_11F=sum(s['floor']>10 for s in seeds),wipes=sum(s['result']=='Wipe' for s in seeds),nonterminal=sum(s['result']=='Nonterminal' for s in seeds),floor10_encounters=len(ten),floor10_clears=sum(r['result']=='Clear' for r in ten),mean_damage=sum(r['damage'] for r in rows)/max(1,len(seeds)),mean_damage_taken=sum(r['damageTaken'] for r in rows)/max(1,len(seeds)),mean_skill_healing=sum(r['healing'] for r in rows)/max(1,len(seeds)),mean_recovery_raw=sum(v['raw'] for v in combat_recovery.values())/max(1,len(seeds)),mean_recovery_effective=sum(v['effective'] for v in combat_recovery.values())/max(1,len(seeds)),mean_overheal=sum(v['overheal'] for v in combat_recovery.values())/max(1,len(seeds)),mean_mp_spent=sum(u.get('mpSpent',0) for u in units)/max(1,len(seeds)),mean_mp_drained=sum(u.get('mpDrained',0) for u in units)/max(1,len(seeds)),potions=sum(r['potions'] for r in rows),revive_casts=skills['revive'],taunt_casts=skills['taunt'],mean_boss_clear_seconds=sum(r['clearTime'] for r in rows if r['result']=='Clear')/max(1,sum(r['result']=='Clear' for r in rows)),mean_run_seconds=sum(s['seconds'] for s in seeds)/max(1,len(seeds)),rest_actions=sum(r['restActions'] for r in rows),crafts=sum(r['crafts'] for r in rows),shield_absorbed=sum(u.get('shieldAbsorbed',0) for u in units),death_floors=json.dumps(death_floors,sort_keys=True),recovery_sources=json.dumps(recovery,sort_keys=True),skills=json.dumps(skills,sort_keys=True),weapon_exposures=json.dumps(weapons,sort_keys=True),loot=json.dumps(loot,sort_keys=True),support_opportunities=json.dumps(opportunities,sort_keys=True)))
        text.append(f"- `{label}`：{len(seeds)}/{cfg['count']} seeds；runtime `{manifest['runtimeHash']}`；10F {sum(r['result']=='Clear' for r in ten)}/{len(ten)} encounters 擊敗。")
    write_csv(args.prefix+'_floor10_breakdown.csv',breakdown);write_csv(args.prefix+'_composition-analysis.csv',compositions);write_csv(args.prefix+'_runtime-manifest.csv',inventory)
    (ROOT/'Artifacts'/(args.prefix+'_composition-analysis.md')).write_text('# Phase 3.1 結構化診斷資料\n\n'+'\n'.join(text)+'\n\n所有數值依 label/runtime/config 分開。Natural 含自由選職與分隊，不能與固定編成視為等價。固定編成需 floorLimit=10；25F 自然樣本的總量涵蓋 1–25F，不能直接比較其總量。\n\n恢復來源 raw/effective/overheal 分開；MP 藥水不計入 HP 恢復。death source 表示最後一擊，並不單獨證明全部致死因素。ready 是資源/冷卻/配裝候選，未保證施法距離或能救回目標。盾吸收為實際抵銷傷害；church 只提供 guard，不是直接治療。技能機會欄 raw/effective/mana 分別是 unlocked/equipped/ready 秒數。weapon_exposures 是 encounter 暴露次數。loot 與 craft 是觀察，不是掉落依賴因果證明。\n',encoding='utf8')
    print(json.dumps(inventory,ensure_ascii=True))

if __name__=='__main__':main()
