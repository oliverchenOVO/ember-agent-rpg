from pathlib import Path
root = Path(__file__).resolve().parents[1]
docs = {
'TECHNICAL_DESIGN': '''# 技術設計
## 已確認環境
Unity 6000.2.0f1：D:/unity/6000.2.0f1/Editor/Unity.exe；Windows Mono Build 模組可用。
Blender 3.1.2；Git 2.54.0；Python 可用；.NET Runtime 8 有安裝但無 SDK。
工作目錄初始為空，無既有遠端。以本機 Git 建立里程碑，不建立外部帳號或購買資產。
## 架構
UnityProject/Assets/Scripts/Core：純資料、種子 RNG、事件、戰鬥、效用決策、周目與存檔。
Presentation：3D 世界、角色視覺、VFX、觀察者鏡頭、HUD 與音效；只讀 simulation state。
Editor：產生場景、執行驗證、Windows BuildPipeline。Resources/catalog.json：版本化內容。
固定 0.1 秒 simulation step，低層移動與避招在該步更新。高層每 1.5 秒及狀態轉換評估。
單位位置使用平面 x/z；3D 呈現與邏輯分離。首輪圓形開放場地不需要 NavMesh；複雜地圖再導入。
採 Built-in render pipeline，避免無需使用的套件與網路依賴。使用 PBR 材質、霧、陰影、程序化幾何與光效。
## 品質與限制
先證明單 Boss + 休息探索 + 周目結算迴圈；不宣稱完成 25 層或最終美術。
無外部 LLM 呼叫、憑證或費用。IBrain 提供可替換介面；LLM 未實作。
JsonUtility schema v1 存檔含 RNG 狀態、角色、關係、技能、庫存、事件、周目歷史；原子替換與備份。
發佈存檔位於 Application.persistentDataPath，與專案檔案分離。
參考：https://docs.unity.com/en-us/engine/6000.0/manual/unity-editor/command-line-arguments/editor
''',
'GAME_DESIGN': '''# 遊戲設計
「EMBER / The Witness Tower」：觀察四個性格穩定但每周目重新選職業的挑戰者。
七支柱：自主、自由 Build、關係、獨立 Boss、製作灌注、死亡遺產、速度紀錄。
玩家僅暫停、改觀察速度、選擇觀察角色、查看遺書/歷史與重新開始，不直接指揮戰鬥。
垂直切片：選職業與 5 點 → 根冠巨獸 → 戰利品與升級 → 廢墟營地 → 逃離/坍塌死亡 → 結算 → 重生。
休息區是本切片出口；逃離只算 SliceComplete，不是 25F Clear，不能取得勝者完整記憶權。
個體死亡即停止行動，剩餘人繼續；全滅立即結算。重啟重置等級(0)、成長、技能、裝備與背包。
戰鬥讀取技能 Mana/CD/武器要求、仇恨、距離、預警範圍、性格與救援關係權重。
休息倒數 26 秒後坍塌从入口逐步推到出口；治療/製作/讀書/探索/逃離均耗費實際模擬時間。
隨機掉落不按隊伍配裝，產生職業不適用書、武器、材料與藥水。可拆解、裝備、使用、製作。
美術方向：冷青遺跡、琥珀火光、巨獸鹿角、流動符文；低面數輪廓搭配精緻光照。
''',
'AGENT_ARCHITECTURE': '''# Agent 架構
IBrain.Decide(WorldState, AgentState, Catalog) -> Intent。合法動作由引擎驗證，Brain 不直接修改世界。
人格包含 risk、greed、curiosity、empathy、loyalty、aggression；Goals 以 intent 與理由呈現。
選職業為加權抽樣，允許重複與奇怪加點；切片預設首輪四職業展示，可開啟完全自主組成。
戰鬥效用：對低血盟友治療/保護、威脅與存活、Mana 保留、傷害、個性、關係、預警迫近。
移動執行器依 intent 接近、遠離預警/目標，冷卻到期才施法；避免 LLM 每幀控制。
休息效用：血量與資源需要、探索渴望、製作收益、剩餘時間与離出口距離共同評分。
關係為有向 Trust/Respect/Friendship/Love/Fear/Resentment/Loyalty/Rivalry。
施救、分配物品與爭執更新關係；各值有限範圍。不採 Love 門檻強制救援。
對話由 speaker、intent、事件與關係 context 組成；目前為本機文字生成模板，可觀察因果，未接 LLM。
分隊需未來獨立 PartyProgress scheduler；本切片僅記錄衝突與個別逃离，不虛構完整分裂爬塔。
Brain 擴展契約：LLM 回傳有限 action schema，限時、快取、validator、fallback UtilityBrain，事件節流。
''',
'BOSS_DESIGN': '''# 25 Boss 內容規劃
每個正式 Boss 必須有独立 silhouette、動畫、攻擊 pattern、抵抗、機制、音效與 phase。以下為規劃，僅 1F 已實作。
|層|環境 / Boss|核心機制|
|--|--|--|
|1|森林 / 根冠巨獸|落點預警、範圍重擊、半血狂暴|
|2|森林 / 荊棘蛛后|蛛網束縛與卵囊|
|3|森林 / 蝕月狼|側翼突進、追獵標記|
|4|森林 / 毒沼蛇|毒池、淨化|
|5|森林 / 千年樹心|根節打斷、燃燒互動|
|6|冰原 / 霜甲巨人|破甲、脆弱部位|
|7|冰原 / 鏡雪妖|鏡像辨識|
|8|冰原 / 凍潮鯨|冰面與潮汐|
|9|冰原 / 零度構裝|元素切換|
|10|冰原 / 白龍|吐息、遮蔽物|
|11|城堡 / 無首騎士|格擋反擊|
|12|城堡 / 亡書師|召喚、施法打斷|
|13|城堡 / 鐘樓機械|時間窗口|
|14|城堡 / 偽聖祭司|祭壇風險與資訊誤導|
|15|城堡 / 墜星君王|多 phase、DPS check|
|16|深淵 / 熔岩獸|平台與熱度|
|17|深淵 / 裂地魔|中央致命陷阱|
|18|深淵 / 奇美拉|三頭元素交互|
|19|深淵 / 吞魂者|Mana 壓力、生命交換|
|20|深淵 / 地獄門|生存與小怪波次|
|21|焚盡森林 / 灰燼樹心|根節 + 火海|
|22|永凍冰原 / 靜止之龍|時間停滯 + 冰鏡|
|23|虛空城堡 / 失墜君王|碎片平台 + 反擊|
|24|無底深淵 / 終末奇美拉|元素 + 無地面|
|25|白色塔心 / 記錄者|封技、複製、遺書回聲|
首 Boss：鹿角型植物巨獸、程序化分節肢體、呼吸/重擊動畫。3 秒一次預警，1.2 秒躲避窗口；半血縮短間隔並擴大半徑。
後續以 BossDefinition + mechanic modules 擴充，不能單靠數值增長複製。
''',
'CLASS_AND_SKILL_DESIGN': '''# 職業與技能
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
''',
'ITEM_AND_CRAFTING_DESIGN': '''# 物品與製作
ItemDefinition：id、kind、weaponType、power、quality、skill、profession、materialCost。
ItemInstance：instanceId、definitionId、quality、infusedSkill、upgrade。
所有人能裝備任何武器；武器技能來自實體，不能憑空學會別職技能。
Recipe 驗證材料數量、工坊、已學灌注技能、時間與生命；開始時預留材料，完成時產出。
製作有 4 秒工作時間，黑smith 品質 +0.25，智慧 +0.01/WIS，基準品質 1.2，高於一般掉落 1.0。
逃離或死亡會取消未完工作，預留材料不退，避免免費洗製作；明確顯示成本。
Loot table 全職業共用，先抽掉落再由 Agent 比較配裝/拆解。庫存有限 24；溢出轉材料。
可用 HP/MP potion、EXP book、職業技能書、裝備；缺材料不製作，非本職技能書拆解。
武器強化消耗材料，提升 power；防具降低傷害。正式内容會擴充配方、稀有材料、飾品與暫時 Buff。
''',
'MEMORY_SYSTEM': '''# 記憶與存檔
角色 id 固定，人格跨輪穩定；完整 memory、關係記憶與成長分開保存。
一般失敗/SliceComplete：清除當輪 memory、關係、成長、背包。下輪只在讀死者之書動作中取得限長遺書。
只有 outcome=TowerClear 且 alive 的個體保留完整 memory 與有向關係；死亡者不保留。
每人周目結束可寫最多 80 字元，內容由最後狀態、人格、事件決定，可為遺憾或提醒。
死者之書保留最近 64 條；歷史保留最近 100 周目，保存 survivors、組成、時間、裝備、傷害、治療。
真正最快通關紀錄只納入 TowerClear；切片紀錄另標記，不污染 Hall of Fame。
WorldState 可序列化含 schemaVersion=1、rng uint state、clock、timer、agents、boss、telegraph、messages、history。
存檔寫入 tmp，File.Replace 保留 bak；首次 File.Move。讀檔檢查版本與基本 invariant，損壞回讀 bak。
自動存於結算/新周目；觀察者提供 Save/Load。測試中比較 round-trip 後繼續模擬一致性。
''',
'ROADMAP': '''# 開發里程碑
M0：環境、9 份設計、內容 schema、本機 Git。
M1：決定性模擬、合法性驗證、技能/物品/關係/存檔測試。
M2：首 Boss 3D 觀察者、完整休息區、製作、坍塌、死亡遺書與輪迴，Windows 可執行檔。
M3：Playtest 與多種子 soak，校正效用與視覺可讀性；此階段交付垂直切片。
M4：2–5F 各獨立 Boss、地圖路徑、裝備內容、完整 split-party scheduler。
M5：6–20F 四環境、完整技能效果、控制狀態、音樂/動畫/美術資產 pipeline。
M6：21–25F、完整記憶敘事、速度榜、可選 LLM provider 與成本上限。
M7：長時間穩定性、效能、可及性、完整音畫製作与 release。
每階段 commit，僅已執行的驗證可標記通過。不把規劃内容標為已製作。
''',
'TEST_PLAN': '''# 品質驗證
Unity batch compiler -> Editor Validation.Run -> BuildWindows -> Windows player smoke -> screenshot review。
Core checks：傷害/防禦、自由加點、武器專精與要求、slot 上限、Mana/CD、死亡施法、製作資源与灌注成本、loot 無組成偏差、庫存上限。
Memory checks：失敗清除、真正 TowerClear 只保留存活者、切片不取得勝者資格、遺書字數與讀取動作。
Save checks：JSON round-trip、RNG 可重現、繼續執行一致、損壞 primary 的 backup 復原、版本拒絕。
Simulation checks：多種 seed 都在時間上限內結束且能重新開始；孤存者继续；休息區倒數与坍塌致死。
UI/Player checks：可見四角色、Boss/血量/預警、事件理由、視角、pause/speed、存讀檔、遺書/歷史；無 exception。
先實作可直接 batch 呼叫的 assertion harness，避免 NUnit package 下載影響第一次建置。
未完成完整 25F、獨立分隊爬塔與 LLM 的驗證應列入後續 milestone，不可以用切片通過代稱。
'''
}
for name, body in docs.items():
    (root / 'Docs' / (name + '.md')).write_text(body, encoding='utf-8')
(root / 'README.md').write_text('''# EMBER — The Witness Tower
四名自主 Agent 的 3D Roguelite 觀察型 RPG，Unity 6000.2.0f1。

目前目標：一個 Boss + 危險休息區 + 遺書 + 自動重新開始的可驗證垂直切片。
設計文件在 Docs；執行與測試命令在 Tools。25 層、LLM 與完整美術為後續里程碑。

用 Unity Hub 開啟 UnityProject，開啟 Assets/Scenes/Witness.unity，按 Play。
Windows 成品：Builds/Windows/Ember.exe。空白鍵暫停，1/2/3 切換速度，方向鍵環繞鏡頭。
HUD 可查看角色、遺書、歷史与存讀檔。預設四職業展示；Autonomous composition 可允許任何職業組合。

詳細實測與限制請見 Artifacts/VALIDATION.md。
''', encoding='utf-8')
