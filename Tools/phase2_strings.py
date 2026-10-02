"""Phase 2 localized content/UI. Stable IDs remain English in runtime data."""
entries={}
def put(key,en,zh):entries['p2.'+key]=(en,zh)
for id,en,zh in [
 ('groups','Expeditions','遠征分隊'),('group_row','Group {0} · F{1} · {2}','第 {0} 隊 · {1} 樓 · {2}'),
 ('goal','Goal: {0}','目前目標：{0}'),('membership','Group {0} / F{1}','第 {0} 隊 / {1} 樓'),
 ('intel','Boss intel: {0}','Boss 情報：{0}'),('intel_unknown','Not studied yet','尚未研究'),('memory','{0}: {1}','{0}：{1}'),
 ('details','Agent insight','Agent 觀察'),('split_status','{0} groups · {1} splits · {2} reunions','{0} 支分隊 · {1} 次分隊 · {2} 次會合'),
 ('debug','Reasoner debug','推理除錯'),('provider','Provider: {0} · revision {1}','推理來源：{0} · 決策 {1}'),
 ('rule','Local rules','本機規則'),('fallback','Local fallback','本機備援'),('llm','LLM','LLM'),
 ('boss_state','{0} / Adds {1} / Interrupts {2}','{0} / 召喚物 {1} / 打斷 {2}'),
 ('status','Status: {0}','狀態：{0}'),('none','None','無'),('group_battle','Combat','戰鬥中'),('group_rest','Refuge','休息探索'),('group_travel','Travelling','前往下一層'),('group_complete','Completed','已完成'),('group_dead','Fallen','已全滅'),
 ('observation','Observe a witness to follow their group.','選擇見證者，即可觀看所屬分隊。'),
 ('footer','AUTONOMOUS LIVES. TWENTY-FIVE FLOORS.','自主生命，二十五層見證。'),
 ('event.split','Group {0} splits; group {1} continues independently.','第 {0} 隊分裂，第 {1} 隊將獨自前進。'),
 ('event.rejoin','Group {0} meets its companions again.','第 {0} 隊與同伴再次會合。'),
 ('event.goal','My current goal is {0}.','我目前的目標是{0}。'),
 ('event.victory','We defeated {0}. Group {1} enters the refuge.','我們擊敗了{0}，第 {1} 隊進入避難層。'),
 ('event.phase','{0}: {1}.','{0}：{1}。'),('event.telegraph','{0} prepares {1}.','{0}正在準備{1}。'),
 ('event.floor','Group {0} enters F{1}: {2}.','第 {0} 隊進入 {1} 樓：{2}。'),
 ('event.work','I will spend time at {0}.','我準備在{0}花些時間。'),('event.work_done','I finished at {0}.','我完成了{0}的工作。'),
 ('event.clear','The twenty-five floors have been witnessed.','二十五層的旅程，已被見證。'),('event.group_clear','Group {0} reached the tower summit.','第 {0} 隊抵達了塔頂。'),
 ('cause.status','{0}','{0}'),('cause.ability','{0}','{0}'),('cause.adds','summoned creatures','召喚生物'),('cause.hazard','arena hazard','競技場危險區域'),('cause.final_pulse','the final pulse','臨終衝擊'),('cause.resource','a dangerous resource room','危險資源室'),
 ('memory.victory','I survived floor {0}.','我活著通過了第 {0} 層。'),('memory.intel','I studied the threats on floor {0}.','我研究了第 {0} 層的威脅。'),('memory.told','{0} told me about floor {1}.','{0} 告訴我第 {1} 層的情報。'),('memory.clear','I reached the summit in my own life.','這是我的親身記憶：我抵達了塔頂。'),
 ('phase.opening','First phase','第一階段'),('phase.intense','Second phase / Enraged','第二階段 / 狂暴'),
 ]:put(id,en,zh)
for id,en,zh in [('Fight','defeat the guardian','擊敗守護者'),('Support','support my group','支援隊友'),('Recover','recover strength','恢復體力'),('Forge','improve my build','改善配置'),('Intel','learn the next boss','研究下一層的 Boss'),('ReadBook','read the Book of the Dead','閱讀死者之書'),('Loot','search for resources','搜尋資源'),('Exit','leave before collapse','在坍塌前撤離'),('Rescue','rescue a companion','救援同伴'),('LeaveParty','continue independently','離隊獨自前進'),('Rejoin','meet my companions','與同伴會合')]:put('goal.'+id,en,zh)
for id,en,zh in [('slam','Ground break','裂地重擊'),('bolt','Frost volley','寒霜連射'),('charge','Moonfang charge','月牙衝鋒'),('summon','Call the forgotten','召喚遺忘者'),('storm','Ember storm','餘燼風暴'),('drain','Soul siphon','靈魂汲取'),('shield','Mirror ward','鏡面護盾'),('survival','Frozen sanctuary','冰封試煉'),('doom','Witness of doom','終末見證')]:put('ability.'+id,en,zh)
for id,en,zh in [('bed','bed','床鋪'),('forge','forge','鍛造台'),('church','church','教堂'),('clinic','clinic','醫療站'),('library','library','圖書館'),('corpse','fallen adventurer','冒險者遺骸'),('cache','hidden cache','隱藏寶箱'),('resource','dangerous resource room','危險資源室'),('book','Book of the Dead','死者之書')]:put('site.'+id,en,zh)
for id,zh in [('Burn','燃燒'),('Slow','緩速'),('Stun','暈眩'),('Weaken','虛弱'),('Doom','死亡倒數')]:put('status.'+id,id,zh)
for id,en,zh in [('OwnExperience','My own experience','親身經歷'),('BookOfDead','Written in the Book of the Dead','死者之書記載'),('ToldByAgent','Told by a companion','同伴轉述')]:put('source.'+id,en,zh)
for id,en,zh in [('Healed','{0} healed me.','{0} 治療了我。'),('Rescued','{0} protected me.','{0} 保護了我。'),('Abandoned','{0} left me behind.','{0} 把我留在身後。'),('LootStolen','{0} took my loot.','{0} 拿走了我的戰利品。'),('TacticSuccess','Our strategy with {0} succeeded.','我與 {0} 的戰術成功了。'),('TacticFailure','Our strategy with {0} failed.','我與 {0} 的戰術失敗了。'),('Conversation','I talked with {0}.','我與 {0} 交談過。'),('Death','I witnessed {0} die.','我目睹了 {0} 的死亡。'),('RiskyRescue','{0} risked their life to save me.','{0} 冒著生命危險救了我。'),('Rejoined','I met {0} again.','我再次遇見了 {0}。')]:put('relationship.'+id,en,zh)
names=['Rootcrown','Thornweaver','Moonfang','Mirecoil','Heartwood','Frostplate','Snowmirror','Tidewhale','Zeroform','Whitewing','Headless','Grimoire','Clockwork','FalseSaint','Starfall','Lavabeast','Riftfiend','Chimera','Souleater','Hellgate','Ashheart','Stilldragon','Voidking','Endchimera','Witness']
zh=['根冠之主','荊棘蛛后','蝕月狼','毒沼蛇','千年樹心','霜甲巨人','鏡雪妖','凍潮鯨','零度構裝','白龍','無首騎士','亡書師','鐘樓機械','偽聖祭司','墜星君王','熔岩獸','裂地魔','奇美拉','吞魂者','地獄門','灰燼樹心','靜止之龍','失墜君王','終末奇美拉','記錄者']
for i,(en,ch) in enumerate(zip(names,zh),1):put('boss.'+en.lower(),en,ch);put('floor.'+str(i),f'{en} sanctuary',ch+'之境')
