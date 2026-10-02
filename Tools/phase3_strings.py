entries={}
def add(k,en,zh):entries['p3.'+k]=(en,zh)
for n,en,zh in [(6,'Foundry threshold','星鑄工坊入口'),(7,'Rail tribunal','雙軌審判台'),(8,'Crystal hatchery','星晶孵化工位'),(9,'Pressure chamber','失序加壓平台'),(10,'Armillary crucible','天球鑄心熔爐')]:add('floor.'+str(n),en,zh)
bosses=[('stoker','The Ember Stoker','餘燼司爐','Step aside from the furnace lanes.','側移避開爐道，冷卻時再接近。'),('railjudge','The Rail Judge','軌道裁決者','Move across the tracks, not along them.','橫越軌道，別沿著掃掠方向奔跑。'),('weavemother','The Crystal Weavemother','星晶織母','Clear hatchlings and interrupt extraction.','先清除晶偶，再打斷抽取。'),('metronome','The Broken Metronome','失序節拍器','The ring leaves a safe inner pocket.','共振環中央有安全區，留意接續的直線。'),('armillary','The Armillary Heart','天球鑄心','Watch phase openings and conserve recovery.','善用階段空檔，保留恢復資源。')]
for id,en,zh,intel,zintel in bosses:
    add('boss.'+id,en,zh);add('intel.'+id,intel,zintel);add('intro.'+id,en+' awakens. The assembly begins.',zh+'甦醒，失落的鑄造程序再度啟動。');add('death.'+id,en+' falls silent.',zh+'的星光逐漸熄滅。')
for id,en,zh in [('furnace_lane','Furnace lane','熔爐灼道'),('rail_sweep','Rail sweep','軌道掃掠'),('rail_cross','Crosscut','十字裁切'),('hatch','Crystal hatch','晶偶孵化'),('extract','Ether extraction','法力抽取'),('pulse_ring','Resonance ring','共振環'),('clock_line','Pendulum strike','鐘擺射線'),('core_shield','Core insulation','晶核隔熱'),('core_cross','Astral cross','星鑄十字')]:add('ability.'+id,en,zh)
for id,en,zh in [('1','Ignition','第一階段／點火'),('2','Overload','第二階段／過載'),('3','Stellar collapse','第三階段／星核崩解')]:add('phase.'+id,en,zh)
add('cause.pressure','foundry heat pressure','工坊熱壓')
add('cue','{0} · {1:0.0}s','{0} · {1:0.0} 秒')
add('intel','Boss intel: {0}','Boss 情報：{0}')
add('infusion','Infusion: {0} · {1} MP · {2:0.0}s cooldown','灌注：{0} · 法力 {1} · 冷卻 {2:0.0} 秒')
add('affix','{0} · {1}','{0} · {1}')
for id,en,zh in [('Scorch','Scorch mark','灼印'),('Mobility','Rail bearings','軌行'),('Recovery','Ether condenser','凝能'),('Break','Fracture teeth','破甲'),('Astral','Astral core','星核'),('Custom','Calibrated','校準')]:add('affix.'+id,en,zh)
for id,en,zh in [('Poison','Poison','中毒'),('Vulnerable','Vulnerable','易傷'),('Root','Rooted','定身'),('Shield','Shield','護盾'),('Regen','Regeneration','持續恢復'),('Counter','Counter ready','反擊待命'),('Haste','Haste','迅捷'),('Enchant','Fire enchant','火焰附魔'),('Zone','Control zone','控場區'),('Summon','Echo summon','召喚殘影')]:add('status.'+id,en,zh)
for id,en,zh in [('slash','Cleave and interrupt','斬擊並打斷'),('wave','Ranged break and vulnerability','遠距破甲並施加易傷'),('rage','Damage boost with regeneration','強化傷害並持續恢復'),('guard','Absorb damage','吸收傷害'),('taunt','Draw attention and protect allies','吸引攻擊，保護隊友'),('counter','Retaliate after a guarded hit','格擋受擊後反擊'),('shot','Precision projectile','精準射擊'),('poison','Damage over time','持續毒傷'),('rain','Clear multiple summons','清除多個召喚物'),('trap','Root and interrupt in a zone','區域定身並打斷'),('pierce','Expose armor weakness','揭露護甲弱點'),('snipe','Long-range burst with recovery delay','遠距爆發，需較長恢復'),('fireball','Fire damage and burning','火焰傷害並灼燒'),('frost','Slow and control','緩速控場'),('meteor','Area burst and lingering zone','範圍爆發與持續區域'),('shield','Barrier protection','屏障保護'),('enchant','Weapon-dependent fire strike','依武器呈現火焰攻擊'),('lightning','Chain through summons','連鎖清除召喚物'),('holy','Light damage and allied recovery','光傷並恢復隊友'),('heal','Focused heal and regeneration','單體治療與持續恢復'),('groupheal','Group heal and protection','群體治療與保護'),('ward','Group barrier','團隊屏障'),('cleanse','Remove harmful effects','解除負面狀態'),('revive','One revival per life','每輪生命可復活一次')]:add('skill.'+id,en,zh)
add('effects','Effect: {0}','效果：{0}')
add('camera','Spectator camera · arrows override','觀察鏡頭 · 方向鍵手動接管')
add('profiling','Profiling {0} min · run {1}','效能紀錄 {0} 分鐘 · 第 {1} 輪')
