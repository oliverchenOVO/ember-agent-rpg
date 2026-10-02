import json
from pathlib import Path
r=Path(__file__).resolve().parents[1]/'UnityProject/Assets/Resources'
stats=[[10,5,3,10,4,4,3],[5,12,4,6,5,5,5],[3,5,12,5,8,10,3],[4,4,9,7,12,9,3]]
weapons=['greatsword','bow','staff','holy_staff']
classes=[dict(profession=i,stats=dict(zip(['str','dex','intel','vit','wis','mana','luck'],s)),starter=['slash','shot','fireball','holy'][i],weapon=weapons[i]) for i,s in enumerate(stats)]
# id, label, effect, power, mana, cd, range, weapon, prerequisite
trees=[
 [('slash','Crescent slash','Damage',22,4,2.2,3,'Sword',''),('wave','Sword wave','Damage',38,10,6,8,'Sword','slash'),('rage','Blood rage','Enchant',18,8,10,0,'','wave'),('guard','Iron guard','Guard',.6,4,5,0,'',''),('taunt','Challenge','Taunt',5,5,5,8,'','guard'),('counter','Counter','Damage',40,10,7,3,'Sword','taunt')],
 [('shot','Piercing shot','Damage',20,3,2.4,10,'Bow',''),('poison','Venom arrow','Damage',32,8,5,10,'Bow','shot'),('rain','Arrow rain','Damage',52,14,9,10,'Bow','poison'),('trap','Snare trap','Guard',.45,4,6,0,'',''),('pierce','Armor piercer','Damage',42,10,6,12,'Bow','trap'),('snipe','Starfall snipe','Damage',65,18,12,14,'Bow','pierce')],
 [('fireball','Ember orb','Damage',26,7,3,10,'',''),('frost','Frost spear','Damage',38,10,5,11,'','fireball'),('meteor','Meteor','Damage',70,22,10,13,'','frost'),('shield','Arcane shield','Guard',.55,8,6,0,'',''),('enchant','Flame infusion','Enchant',16,9,9,0,'','shield'),('lightning','Lightning','Damage',55,15,7,12,'','enchant')],
 [('holy','Holy light','Damage',18,4,3,9,'',''),('heal','Mend','Heal',50,8,4,12,'','holy'),('groupheal','Sanctuary','Heal',80,18,8,14,'','heal'),('ward','Divine ward','Guard',.65,7,6,0,'',''),('cleanse','Purifying light','Heal',35,6,5,12,'','ward'),('revive','Renewal','Heal',95,22,12,12,'','cleanse')]]
skills=[]
for p,tree in enumerate(trees):
 for t in tree:
  skills.append(dict(zip(['id','name','effect','power','mana','cooldown','range','weapon','prerequisite'],t),profession=p,cost=1))
items=[]
for i,(id,w) in enumerate(zip(weapons,['Greatsword','Bow','Staff','Staff'])):
 items.append(dict(id=id,name=['Oathblade','Ash bow','Cinder staff','Pilgrim staff'][i],kind='Weapon',weapon=w,power=[12,10,8,7][i],skill='',profession=i))
items += [dict(id='spear',name='Wayfarer spear',kind='Weapon',weapon='Neutral',power=11,skill='',profession=0),dict(id='armor',name='Runic cuirass',kind='Armor',power=3),dict(id='hp',name='Life draught',kind='Potion',power=60),dict(id='mp',name='Ether draught',kind='Potion',power=50),dict(id='exp',name='Field journal',kind='Experience',power=1)]
for i in range(4): items.append(dict(id='book'+str(i),name=['Sword codex','Hunters codex','Arcane codex','Prayer codex'][i],kind='SkillBook',profession=i))
recipes=[dict(id='forge'+str(i),item=id,materials=3,seconds=4,quality=1.2) for i,id in enumerate(weapons+['spear'])]
(r/'catalog.json').write_text(json.dumps(dict(classes=classes,skills=skills,items=items,recipes=recipes,boss=dict(id='rootcrown',name='THE ROOTCROWN',hp=1300,damage=33,interval=3.6,windup=1.2,radius=3.2)),indent=2),encoding='utf-8')
