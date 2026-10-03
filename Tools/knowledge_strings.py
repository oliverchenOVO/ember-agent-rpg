import json
from pathlib import Path
pairs={
"boss":("Boss 資料","Boss dossier"),"skills":("技能樹","Skill tree"),"close":("關閉","Close"),"open_book":("閱讀完整遺書","Read full legacy"),
"observer":("觀察資料完整公開；Agent 仍須親眼見證或閱讀遺書才能取得招式記憶。檢視期間暫停。","Observer data is public. Agents learn moves through witnessing or reading legacies. Inspection pauses play."),
"survived":("輪迴結束時仍生還","Survived the end of this life"),"list":("{0} / {1}","{0} / {1}"),
"epitaph":("{0}\n我停在第 {1} 層，面對{2}。我的結局：{3}。\n{4}","{0}\nI reached floor {1}, facing {2}. My ending: {3}.\n{4}"),
"witnessed":("我親眼見過的招式：{0}。詳細記錄與應對線索留在下方。","Moves I personally witnessed: {0}. Details and responses follow below."),
"no_observation":("我沒有親眼見到可確認的招式；別把猜測當成情報。","I witnessed no confirmed move. Do not mistake guesses for intelligence."),
"observed":("我親眼看見{0}準備施放{1}。","I saw {0} telegraph {1}."),"inherited":("{0}的遺書記載了{1}：{2}","{0}'s legacy recorded {1}: {2}"),
"prepared":("根據遺書中對{1}的記錄，我準備了{0}。","The legacy's account of {1} led me to prepare {0}."),
"tone.risk":("我總想再往前一步；下一個我，別拿命去試那些標記。","I always wanted one step farther. Next time, do not test those markers with your life."),
"tone.curiosity":("我最放不下那些沒弄懂的招式。把我看見的記下來，下一次就有答案。","The moves I never understood haunt me. Record what I saw so next time we have answers."),
"tone.empathy":("我掛念的是同伴。看清攻擊落在哪裡，再決定怎麼救人。","I worry about my companions. Watch where attacks land before trying to save anyone."),
"tone.greed":("我還想多帶走一件東西。下次先留住命，戰利品才帶得出去。","I wanted one more treasure. Stay alive first, or you carry nothing out."),
"tone.loyalty":("我不想讓隊伍少一個人。留下這些線索，是我最後能做的事。","I never wanted our party to lose anyone. These clues are the last thing I can leave."),
"tone.aggression":("我太急著打倒眼前的敵人。記住出招的空檔，別只顧著追擊。","I was too eager to strike. Remember the openings instead of chasing every attack."),
"book_meta":("第 {0} 輪 / {1} / {2} / 等級 {3}","Life {0} / {1} / {2} / Level {3}"),
"book_build":("留下的配置：{0}；技能：{1}","Final build: {0}; skills: {1}"),"legacy":("舊版遺書未保存招式證據，保留原文供閱讀。","This older legacy contains no move evidence. Its original text is preserved."),
"book_help":("在有死者之書的避難層閱讀，才能把線索帶進本輪記憶；保留最近 8 筆相關遺書。","Read at a refuge with the Book of the Dead to learn from up to 8 recent relevant legacies."),
"known":("親眼見過","Witnessed"),"inherited_badge":("遺書線索","Legacy knowledge"),"unknown":("尚未見證","Not witnessed"),
"boss_stats":("生命 {0} / {1}　護甲 {2}　弱點 {3}　抗性 {4}","HP {0} / {1} | Armor {2} | Weak: {3} | Resist: {4}"),
"boss_state":("目前階段：{0}　狂暴：{1}　護盾減傷：{2}%　召喚物：{3}","Current phase: {0} | Enraged: {1} | Shield reduction: {2}% | Adds: {3}"),
"yes":("是","Yes"),"no":("否","No"),
"phase":("{0}｜生命 ≤ {1}% 或開戰 {2} 秒後（0 表示無時間條件）；狂暴 {3} 秒；弱點增傷 {4}%／抗性減傷 {5}%","{0} | HP ≤ {1}% or {2}s elapsed (0 = no time trigger); enrage {3}s; weakness +{4}% / resistance -{5}%"),
"phase_start":("開戰階段","Opening phase"),"dps":("階段限時：{0} 秒內造成至少最大生命 {1}% 傷害，否則狂暴。","Phase check: deal at least {1}% max HP within {0}s or trigger enrage."),
"pressure":("持續壓力：基礎每秒 {0} 傷害，隨狂暴時間增加；恢復空檔降至 20%。開戰超過 240 秒另有遞增耗損。","Ambient pressure: base {0} damage/s, increasing after enrage; 20% during recovery windows. Additional escalating attrition after 240s."),
"death_pulse":("擊敗後仍會爆發最後脈衝：半徑 2.5，基礎傷害 18。","Death pulse: radius 2.5, base damage 18 even after defeat."),
"boss_note":("以下為基礎數值。狂暴時招式傷害 ×1.2、間隔 ×0.8；實際傷害另計護甲與防禦。間隔從結算後起算，另加預警時間。","Base values below. Enrage: move damage ×1.2, interval ×0.8. Armor and defense affect actual damage. Intervals start after resolution and exclude windup."),
"timing":("預警 {0} 秒　結算後間隔 {1} 秒　範圍 {2}（半徑／半寬）　長度 {3}　內圈 {4}","Windup {0}s | Post-resolution interval {1}s | Radius/half-width {2} | Length {3} | Inner radius {4}"),
"target":("形狀：{0}　鎖定：{1}　可打斷：{2}　打斷窗口：最後 {3} 秒","Shape: {0} | Target: {1} | Interruptible: {2} | Final interrupt window: {3}s"),
"damage":("基礎傷害 {0}（{1}）","Base damage {0} ({1})"),"drain":("命中時合計扣除魔力 {0}","Total drain {0} MP on hit"),
"status":("附加{0}，持續 {1} 秒：{2}","Applies {0} for {1}s: {2}"),"push":("擊退距離 {0}","Knockback distance {0}"),
"hazard":("危險區持續 {0} 秒，半徑 3，每秒基礎傷害 7。","Hazard lasts {0}s, radius 3, base damage 7/s."),
"skill_header":("{0} / {1}　等級 {2}　技能點 {3}　已裝備 {4} / 4","{0} / {1} | Level {2} | Points {3} | Equipped {4} / 4"),
"skill_help":("點選節點查看效果。連線代表實際前置技能；裝備與解鎖由 Agent 自主決定。灌注技能可跨職業使用。","Select a node for details. Connections show prerequisites. Agents choose unlocks and loadouts autonomously. Infusions can cross classes."),
"skill_cost":("魔力 {0}　冷卻 {1} 秒　距離 {2}　技能點 {3}","MP {0} | Cooldown {1}s | Range {2} | Points {3}"),
"prerequisite":("前置：{0}","Requires: {0}"),"equipped":("已裝備","Equipped"),"unlocked":("已解鎖","Unlocked"),"locked":("未解鎖","Locked"),"infused":("武器灌注","Weapon infusion"),
"other_class":("其他職業；需透過武器灌注使用。","Other class; available through weapon infusion."),
"unlockable":("前置與技能點已足夠，等待 Agent 決定。","Prerequisites and points ready; awaiting Agent choice."),
"not_ready":("目前缺少前置技能或技能點。","Missing prerequisite or skill points."),
"weapon":("武器需求：{0}（武器灌注施放免除此限制）","Weapon requirement: {0} (waived for infused casts)"),
"skill_note":("以下依目前角色屬性計算，為減傷前數值。治療受生命上限限制；持續傷害受抗性影響。防禦值每秒衰減 4 個百分點。","Values use this Agent's current stats before mitigation. Healing is capped by max HP; DOT is affected by resistance. Guard decays by 4 percentage points/s."),
"formula":("技能強度 = 基礎值 {0} + 力量×0.4 + {1}×0.8 = {2}","Skill power = base {0} + STR×0.4 + {1}×0.8 = {2}"),
"int":("智力","INT"),"wis":("智慧","WIS"),"personal":("自身","Self"),"no_weapon":("不限","Any"),
}
extra={
"element.Physical":("物理","Physical"),"element.Fire":("火焰","Fire"),"element.Ice":("冰霜","Ice"),"element.Light":("聖光","Light"),"element.Dark":("暗影","Dark"),"element.Arcane":("奧術","Arcane"),"element.":("無","None"),
"shape.Circle":("圓形","Circle"),"shape.Lane":("直線","Lane"),"shape.Cross":("十字","Cross"),"shape.Annulus":("環形","Annulus"),
"target.Random":("隨機存活角色","Random living Agent"),"target.LowestHealth":("生命比例最低","Lowest HP ratio"),"target.Farthest":("距離最遠","Farthest Agent"),"target.HighestDamage":("累積傷害最高","Highest damage dealt"),
"status_effect.Burn":("每秒基礎傷害 2","Base damage 2/s"),"status_effect.Slow":("移動速度降為 55%","Movement speed 55%"),"status_effect.Stun":("移動速度降為 20%","Movement speed 20%"),"status_effect.Weaken":("狀態標記；目前未額外降低傷害","Status marker; no additional damage reduction currently"),"status_effect.Doom":("結束時承受最大生命 120% 的基礎傷害","On expiration, base damage equal to 120% max HP"),
"mechanic.Slam":("對預警區內角色造成傷害。","Damages Agents inside the telegraph."),"mechanic.Projectile":("朝鎖定區域發射攻擊。","Fires an attack into the targeted area."),"mechanic.Charge":("衝至鎖定位置，攻擊預警區。","Charges to the target and hits the telegraph area."),"mechanic.Summon":("增加 2 個召喚物，上限 6；每個每秒造成基礎傷害 2。攻擊先清除召喚物，再傷害 Boss。","Adds 2 summons, capped at 6; each deals base damage 2/s. Attacks remove summons before damaging the Boss."),"mechanic.Storm":("攻擊後在目標位置留下危險區。","Attacks and leaves a hazard at the target."),"mechanic.Drain":("直接命中鎖定角色，不看地面範圍；額外吸取 10 魔力，Boss 恢復 8 生命。","Hits the locked target regardless of ground area; drains an extra 10 MP and restores 8 Boss HP."),"mechanic.Shield":("獲得 55% 減傷護盾，每秒衰減 10 個百分點。","Grants 55% damage reduction, decaying by 10 percentage points/s."),"mechanic.Survival":("Boss 在持續期間免疫傷害，並留下危險區。","Boss is immune during the duration and creates a hazard."),"mechanic.Doom":("命中後施加狀態，注意結束時的致命傷害。","Applies a status on hit; beware lethal damage on expiry."),
"advice.Slam":("預警時離開標記；可打斷的招式留意最後窗口。","Leave the marker during windup; interrupt in the final window when allowed."),"advice.Projectile":("先避開鎖定區，再回頭攻擊。","Leave the targeted area before resuming attacks."),"advice.Charge":("別站在衝刺路線上，衝刺後再追擊。","Stay off the charge path; attack after the charge."),"advice.Summon":("保留清場技能，先處理召喚物。","Keep add-clearing skills ready and remove summons first."),"advice.Storm":("離開持續危險區，別為了追擊停留。","Leave persistent hazards instead of staying to attack."),"advice.Drain":("地面閃避無法躲開鎖定吸取；保留護盾與魔力補給。","Ground dodging cannot avoid targeted drain; reserve shields and MP supplies."),"advice.Shield":("利用護盾衰退的時間恢復資源，避免空耗技能。","Recover resources while the shield decays; avoid wasting skills."),"advice.Survival":("免疫期間專心移動，等危險區結束再輸出。","Move during immunity; resume damage after the hazard ends."),"advice.Doom":("預警時離開範圍，受影響後優先淨化。","Leave during windup; prioritize cleansing if afflicted."),
}
pairs.update(extra)
pairs["node_cost"]=("魔力 {0}　冷卻 {1} 秒\n距離 {2}　技能點 {3}","MP {0} | Cooldown {1}s\nRange {2} | Points {3}")
pairs["edge"]=("›","›")
pairs["agent_stats"]=("力量 {0}　敏捷 {1}　智力 {2}　體質 {3}　智慧 {4}　魔力屬性 {5}　幸運 {6}","STR {0} | DEX {1} | INT {2} | VIT {3} | WIS {4} | Mana stat {5} | Luck {6}")
pairs.update({"weapon.Sword":("劍","Sword"),"weapon.Bow":("弓","Bow"),"weapon.Staff":("法杖","Staff")})
skills={
"slash":("物理傷害 {0}；可打斷。將 Boss 沿遠離角色的方向推移（差向量×0.08）。","Physical damage {0}; can interrupt. Pushes Boss away by offset×0.08."),
"wave":("物理傷害 {0}；可打斷。易傷 5 秒，所受傷害增加 25%。","Physical damage {0}; can interrupt. Vulnerable for 5s, damage taken +25%."),
"rage":("持續 10 秒的火焰強化：普通攻擊額外基礎傷害 12；並每秒恢復 3 生命，持續 6 秒。","Fire empowerment for 10s: basic attacks gain base damage 12; regenerate 3 HP/s for 6s."),
"guard":("防禦減傷 65%；護盾吸收 {7} 傷害，持續 5 秒。","Guard reduction 65%; shield absorbs {7} damage for 5s."),
"taunt":("將目前招式改鎖定自身；圓形預警同步移動。防禦減傷 50%。","Retargets current attack to self; circle telegraph moves with the target. Guard reduction 50%."),
"counter":("立即造成物理傷害 {1}；可打斷。反擊效果 5 秒，受到基礎傷害大於 3 的攻擊時反擊 {0}。防禦減傷 50%。","Immediate physical damage {1}; can interrupt. For 5s, hits with base damage >3 trigger counter damage {0}. Guard reduction 50%."),
"shot":("物理傷害 {0}；距離大於 5 時提高至 {5}。移動速度增加 30%，持續 2 秒。","Physical damage {0}, increased to {5} beyond distance 5. Movement speed +30% for 2s."),
"poison":("立即物理傷害 {2}；中毒每秒基礎傷害 6，持續 8 秒。","Immediate physical damage {2}; poison deals base damage 6/s for 8s."),
"rain":("物理傷害 {0}；可打斷；先移除最多 3 個召喚物。","Physical damage {0}; can interrupt; removes up to 3 summons first."),
"trap":("放置半徑 3 的陷阱，持續 5 秒；Boss 位於區內時定身，區內每秒基礎物理傷害 5。可在打斷窗口干擾招式。","Radius 3 trap for 5s; roots Boss inside and deals base physical damage 5/s. Can disrupt casts in their interrupt window."),
"pierce":("物理傷害 {0}；可打斷。易傷 5 秒，所受傷害增加 25%。","Physical damage {0}; can interrupt. Vulnerable for 5s, damage taken +25%."),
"snipe":("物理傷害 {4}；施放後普通攻擊等待 1.2 秒。","Physical damage {4}; sets basic attack wait to 1.2s."),
"fireball":("火焰傷害 {0}；可打斷。燃燒每秒基礎火焰傷害 4，持續 4 秒。","Fire damage {0}; can interrupt. Burn deals base fire damage 4/s for 4s."),
"frost":("冰霜傷害 {0}；可打斷。定身 3 秒（半徑 3），Boss 下一次攻擊等待增加 0.8 秒。","Ice damage {0}; can interrupt. Roots within radius 3 for 3s; adds 0.8s to Boss attack wait."),
"meteor":("火焰傷害 {0}；可打斷。留下半徑 3 的區域，每秒基礎火焰傷害 8，持續 4 秒。","Fire damage {0}; can interrupt. Radius 3 zone deals base fire damage 8/s for 4s."),
"shield":("自身護盾吸收 {6} 傷害，持續 8 秒。","Self shield absorbs {6} damage for 8s."),
"enchant":("火焰強化 12 秒；普通攻擊額外基礎伤害 12；附魔狀態持續 12 秒。","Fire empowerment for 12s; basic attacks gain base damage 12; enchant status lasts 12s."),
"lightning":("奧術傷害 {0}；可打斷；先移除最多 3 個召喚物。","Arcane damage {0}; can interrupt; removes up to 3 summons first."),
"holy":("聖光傷害 {0}；可打斷。治療生命比例最低的同伴 {9}；依實際治療比例額外耗魔，最多 {10}。","Light damage {0}; can interrupt. Heals lowest-HP-ratio ally for {9}; extra MP proportional to effective healing, up to {10}."),
"heal":("單體治療 {0}；另每秒恢復 3 生命，持續 4 秒。","Single-target heal {0}; regenerates 3 HP/s for 4s."),
"groupheal":("全隊存活角色各治療 {3}，並獲得吸收 12 傷害的護盾，持續 3 秒。","Heals each living party member for {3}; grants shields absorbing 12 damage for 3s."),
"ward":("全隊存活角色各獲得吸收 {8} 傷害的護盾，持續 8 秒。","Shields each living party member for {8} damage, lasting 8s."),
"cleanse":("移除目標身上所有 Boss 狀態，並治療 {0}。","Removes all Boss statuses from the target and heals {0}."),
"revive":("復活死亡同伴，恢復其最大生命的 35%；每位同伴每輪限復活一次。對存活同伴改為治療 {0}。","Revives a dead ally at 35% max HP, once per ally per life. Living targets are healed for {0}."),
}
for k,v in skills.items():pairs['skill.'+k]=v
for i,locale in enumerate(['zh-TW','en']):
 p=Path('UnityProject/Assets/Resources/Localization')/(locale+'.json');table=json.loads(p.read_text(encoding='utf8'));existing={e['key']:e for e in table['entries']}
 for key,values in pairs.items():
  key='codex.'+key
  if key in existing:existing[key]['value']=values[i].replace('伤害','傷害')
  else:table['entries'].append({'key':key,'value':values[i].replace('伤害','傷害')})
 p.write_text(json.dumps(table,ensure_ascii=False,indent=2)+'\n',encoding='utf8')
print(len(pairs),'bilingual keys')
