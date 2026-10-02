# 職業與技能
属性 STR/DEX/INT/VIT/WIS/MANA/LUCK，每職固定初始數值 + 5 自由點。升級 +3 屬性 +1 技能點。
每職 6 個節點、prerequisite 與 costs，由 catalog 管理。最多 4 個裝備職業主動技能，另外 1 個武器技能。
Warrior：Slash → SwordWave → BloodRage；Guard → Taunt → Counter。
Archer：Shot → PoisonArrow → ArrowRain；Trap → PiercingArrow → Snipe。
Mage：Fireball → FrostSpear → Meteor；Shield → Enchant → Lightning。
Healer：HolyLight → Heal → GroupHeal；Ward → Cleanse → Revive。
切片技能 effect 類型：Damage、Heal、Guard、Enchant、Taunt；後續獨立毒/控場/復活效果待擴充。
劍技要求 Sword/Greatsword；弓技要求 Bow；法術與輔助不硬綁武器。
專精加成 1.2，跨職可持有但無加成，Neutral 無懲罰。火焰附魔加到實際武器普攻。
冷卻依角色每技能記錄；同名武器/角色技能共享 cooldown 防止重複施放漏洞。
死亡者不能施法、施法檢查 Mana、距離、解鎖、slot 與武器。灌注武器技能免除製作者職業限制，成本由持有者付。
