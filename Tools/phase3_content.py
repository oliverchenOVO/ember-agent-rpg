"""Theme B authored identity; stable Phase 2 boss IDs are preserved for saves."""
def apply(data):
    data['environments'].append(dict(id='astral_foundry',r=.14,g=.25,b=.34,fog=.015,lighting='foundry_cold_rim',vfx='foundry_circuit',music='foundry_pulse',ambience='foundry_machine'))
    specs=[
        ('furnace_lane',0,46,5.2,2.3,1.8,1,0,0,0,'Burn','Fire',False),
        ('rail_sweep',2,48,3.4,1.65,1.2,1,0,2.5,0,'Slow','Physical',False),
        ('rail_cross',0,38,4,1.85,1,2,0,2,0,'','Physical',False),
        ('hatch',3,0,5.5,1.6,2,0,0,0,0,'','Dark',True),
        ('extract',5,30,4.5,1.8,2,0,0,0,16,'Weaken','Dark',True),
        ('pulse_ring',0,40,2.8,1.55,7.5,3,3.3,0,0,'','Arcane',False),
        ('clock_line',1,36,2.3,1.35,1.1,1,0,0,0,'Slow','Arcane',True),
        ('core_shield',6,0,5,1.8,2,0,0,0,0,'','Ice',True),
        ('core_cross',0,56,3.3,1.8,1.4,2,0,2.5,6,'Burn','Fire',True),
    ]
    for id,mechanic,damage,interval,windup,radius,shape,inner,push,drain,status,element,interrupt in specs:
        data['abilities'].append(dict(id=id,nameKey='p3.ability.'+id,mechanic=mechanic,target=2 if 'rail' in id else 0,damage=damage,interval=interval,windup=windup,radius=radius,shape=shape,length=18,innerRadius=inner,push=push,resourceDrain=drain,status=status,element=element,duration=5,interruptible=interrupt,interruptWindow=.9))
    hp_curve=[900,1300,1800,2400,3200,6500,7000,7800,8500,12000,9500,11000,12500,14500,18000,16000,18500,21000,23500,27000,28000,31000,34000,37000,44000]
    for i,b in enumerate(data['bosses']):
        b['hp']=hp_curve[i]
        if i>=10:b['ambientPressure']=1.3+(i-10)*.13
        for j,phase in enumerate(b['phases']):phase['enrageAfter']=130 if i<10 else 150;phase['afterSeconds']=55 if i<5 and j==1 else 0
    identities=[('stoker','loading',5500,3,'',4,[['furnace_lane'],['furnace_lane','bolt']]),
                ('railjudge','rails',6500,3.5,'Rail',4.4,[['rail_sweep'],['rail_cross','rail_sweep']]),
                ('weavemother','hatchery',7500,4,'',4.8,[['hatch','extract'],['hatch','extract','core_shield']]),
                ('metronome','pressure',8500,4,'Pendulum',5.2,[['clock_line','pulse_ring'],['clock_line','rail_cross','pulse_ring']]),
                ('armillary','armillary',12000,5,'',5.8,[['furnace_lane'],['hatch','core_shield','extract'],['pulse_ring','core_cross','clock_line']])]
    for i,(identity,layout,hp,armor,movement,pressure,sets) in enumerate(identities):
        f=data['floors'][i+5];b=data['bosses'][i+5];n=i+6
        b.update(nameKey='p3.boss.'+identity,archetype=identity,presentation=identity,movement=movement,hp=hp,armor=armor,ambientPressure=pressure,placeholder=False,weaknessElement='Ice' if i in (0,3,4) else 'Fire',deathMechanic='None')
        b['phases']=[dict(nameKey='p3.phase.'+str(j+1),hpBelow=[1,.62,.28][j],afterSeconds=[0,42,84][j],enrageAfter=110+i*8,dpsDeadline=0,requiredDamage=0,abilities=abilities,resistance=.12,weakness=.3) for j,abilities in enumerate(sets)]
        f.update(theme='astral_foundry',placeholder=False,nameKey='p3.floor.'+str(n),arenaLayout=layout,introKey='p3.intro.'+identity,deathKey='p3.death.'+identity,intelKey='p3.intel.'+identity,lootIdentity=['Scorch','Mobility','Recovery','Break','Astral'][i],lootTables=['foundry_'+str(n)],music='foundry_pulse',ambience='foundry_machine',lighting='foundry_cold_rim',vfx='foundry_circuit',hazard='FurnaceLane' if i==0 else 'RailSweep' if i==1 else 'CrystalAdds' if i==2 else 'Resonance',restPool=['foundry_service'])
        acts=[next(a for a in data['abilities'] if a['id']==id) for seq in sets for id in seq]
        f['difficulty']=dict(effectiveHp=hp*(1+armor*.08),incomingDamage=sum(a['damage'] for a in acts)/len(acts),attackFrequency=sum(1/a['interval'] for a in acts)/len(acts),aoePressure=[.3,.45,.35,.65,.8][i],positioningPressure=[.3,.7,.45,.8,.85][i],reactionWindow=min(a['windup'] for a in acts),mechanicComplexity=len(set(a['id'] for a in acts)),resourceAttrition=pressure,buildDependency=[.2,.3,.6,.55,.7][i],coordinationDependency=[.2,.4,.7,.55,.8][i])
        data['loot'].append(dict(id='foundry_'+str(n),items=['greatsword','bow','staff','holy_staff','hp','mp'],rolls=2,materials=3,affix=f['lootIdentity'],infusion='enchant' if i==0 else 'holy' if i==4 else ''))
    rest=dict(data['rests'][0]);rest.update(id='foundry_service',collapseAfter=29,collapseSpeed=2.3);data['rests'].append(rest)
