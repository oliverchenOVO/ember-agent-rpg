"""Versioned tower authoring: references, reusable mechanics, explicit placeholder flags."""
import json
from pathlib import Path
ROOT=Path(__file__).resolve().parents[1]
abilities=[]
for id,mechanic,damage,interval,windup,radius,status,element,target in [
 ('slam',0,22,4,1.4,3,'','Physical',0),('bolt',1,16,3,1,1.8,'Slow','Ice',2),
 ('charge',2,26,4,1.8,1.6,'Stun','Physical',2),('summon',3,8,6,1.8,2,'','Dark',0),
 ('storm',4,14,5,2,4,'Burn','Fire',0),('drain',5,12,5,1.5,3,'Weaken','Dark',1),
 ('shield',6,0,8,1.8,2,'','Ice',3),('survival',7,9,5,1.5,4,'Slow','Ice',0),
 ('doom',8,22,7,2,2,'Doom','Dark',1)]:
 abilities.append(dict(id=id,nameKey='p2.ability.'+id,mechanic=mechanic,damage=damage,interval=interval,windup=windup,radius=radius,status=status,element=element,target=target,duration=4,interruptible=mechanic in (1,3,5,6,8),interruptWindow=.7))
names=['Rootcrown','Thornweaver','Moonfang','Mirecoil','Heartwood','Frostplate','Snowmirror','Tidewhale','Zeroform','Whitewing','Headless','Grimoire','Clockwork','FalseSaint','Starfall','Lavabeast','Riftfiend','Chimera','Souleater','Hellgate','Ashheart','Stilldragon','Voidking','Endchimera','Witness']
themes=['forest','ice','castle','abyss','ashforest','eternalice','voidcastle','endabyss','final']
colors=[(.13,.3,.24),(.2,.4,.55),(.25,.2,.4),(.38,.14,.12),(.35,.2,.1),(.16,.25,.5),(.2,.08,.3),(.28,.03,.09),(.55,.57,.55)]
envs=[dict(id=t,r=c[0],g=c[1],b=c[2],fog=.023,lighting=t+'_light',vfx=t+'_vfx',music=t+'_music_placeholder',ambience=t+'_ambience_placeholder') for t,c in zip(themes,colors)]
archetypes=['guardian','caster','charger','summoner','environment']
sets=[['slam'],['bolt','shield'],['charge'],['summon','drain'],['storm','survival']]
bosses=[]; floors=[]
for i,name in enumerate(names):
 n=i+1; arch=i%5; theme=themes[i//5] if n<=20 else themes[n-17] if n<=24 else 'final'
 # five real reusable archetypes; later floors combine modules while bespoke content remains placeholder.
 phases=[dict(nameKey='p2.phase.opening',hpBelow=1,afterSeconds=0,enrageAfter=100,dpsDeadline=0,requiredDamage=0,abilities=sets[arch],resistance=.1,weakness=.25),
         dict(nameKey='p2.phase.intense',hpBelow=.5,afterSeconds=55,enrageAfter=85,dpsDeadline=75 if arch==4 else 0,requiredDamage=.2 if arch==4 else 0,abilities=sets[arch]+(['doom'] if n==25 else ['storm'] if arch==4 else []),resistance=.15,weakness=.35)]
 bosses.append(dict(id=name.lower(),nameKey='p2.boss.'+name.lower(),archetype=archetypes[arch],hp=430+n*38,armor=1+n*.12,weaknessElement='Fire' if arch in (0,1) else 'Ice',resistElement='Dark',deathMechanic='FinalPulse' if arch==4 else 'None',phases=phases,placeholder=n>5))
 floors.append(dict(floor=n,id='floor_%02d'%n,nameKey='p2.floor.%d'%n,theme=theme,bossId=name.lower(),difficultyTier=(n-1)//5+1,arenaRules=['BoundedArena','IndependentInstance'],hazard='FallingEmbers' if arch==4 else 'TelegraphedGround',lootTables=['shared'],restPool=['crossing'],threatTags=[archetypes[arch]],intelTags=[abilities[arch]['id'],'element_weakness'],music=theme+'_music_placeholder',ambience=theme+'_ambience_placeholder',lighting=theme+'_light',vfx=theme+'_vfx',placeholder=n>5))
sites=[]
for i,(id,effect,seconds,risk,reward,cost) in enumerate([
 ('bed','Heal',3,0,40,0),('forge','Craft',4,0,1.5,3),('church','Bless',3,.05,.35,1),('clinic','Cleanse',3,0,55,1),
 ('library','Intel',3,.02,.8,0),('corpse','Loot',3,.3,2,0),('cache','Loot',3,.15,2,0),('resource','Materials',5,.45,5,0),('book','Read',3,0,.6,0)]):
 sites.append(dict(id=id,nameKey='p2.site.'+id,effect=effect,x=-7+(i%3)*4,z=-3+(i//3)*3,seconds=seconds,risk=risk,reward=reward,materialCost=cost))
data=dict(version=2,floors=floors,bosses=bosses,abilities=abilities,loot=[dict(id='shared',items=['greatsword','bow','staff','holy_staff','armor','hp','mp','exp','book0','book1','book2','book3'],rolls=2,materials=3)],rests=[dict(id='crossing',collapseAfter=26,collapseSpeed=2.3,sites=sites)],environments=envs)
# Catalog owns stable item IDs, not this generator.
catalog=json.loads((ROOT/'UnityProject/Assets/Resources/catalog.json').read_text(encoding='utf-8'))
ids={x['id'] for x in catalog['items']}
data['loot'][0]['items']=[x['id'] for x in catalog['items']]
from phase3_content import apply
apply(data)
(ROOT/'UnityProject/Assets/Resources/tower.json').write_text(json.dumps(data,ensure_ascii=False,indent=2)+'\n',encoding='utf-8')
print('Authored 25 floors, 25 definitions, 5 executable archetypes, 9 rest sites')
