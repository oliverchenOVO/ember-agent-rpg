# Phase 2 內容製作與執行

## 內容來源

Resources/catalog.json 仍擁有職業、技能、物品與配方的既有穩定 ID。Resources/tower.json 是 schema version 2 的塔資料，包含 25 層、25 個 Boss 定義、9 種能力 mechanic、9 種休息設施與 9 種環境 profile。前五層使用五種不同可執行 archetype，其他樓層重組機制，`placeholder: true` 明確表示尚未製作各自完整內容。

Tools/write_phase2_content.py 是目前的 authoring 來源；修改後重產 tower.json。Tools/phase2_strings.py 擁有新增中英文案，執行 Tools/write_localization.py 產生字串表與盤點。不要只改生成檔，否則下次產生會被覆寫。

Floor 的 id/floor/nameKey/theme/bossId/difficultyTier、arenaRules、hazard、lootTables、restPool、threatTags、intelTags、music/ambience、lighting/vfx 都是資料。主題順序為森林、冰原、城堡、深淵、四種終末版本、獨立塔心。音樂/環境音參照目前標為 placeholder；環境與 Boss 輪廓以可重用程序幾何呈現，尚非最終素材。

## 新增流程

- Boss：給穩定 ID/nameKey，指定 archetype、生命/防禦、元素弱點/抵抗、死亡機制及 phases。每個 phase 有條件、技能 ID 清單、狂暴時間、DPS 時限與弱點倍率。使用既有 mechanic 可以只改資料；新 mechanic 要增加 BossRuntime 的 resolver 並附測試。
- Floor：建立 FloorDefinition，連結存在的 Boss、LootTable、RestDefinition、EnvironmentProfile，補上中英 nameKey。25F 只有擊敗 Boss 並撤離休息層，才能取得完成資格。
- Item/Skill：在既有 catalog authoring 工具新增 ID 與數值/需求，補中英名稱與對應 loot/recipe。核心依資料讀取，不新增每個物品的 if 分支；全新技能效果仍要增加執行模組。現有六節點技能樹沿用原 effect 類型，淨化與群補另有 Phase 2 行為；完整復活/毒/控場樹仍需後續深化。
- Rest：site 的 effect、位置、秒數、材料、風險與獎勵由資料控制。腦袋估算移動+任務+撤離時間；開始工作預留成本，死亡取消，沒有免費工作。床鋪最多兩次並按生命上限恢復，避免長期成長讓固定恢復量失效。

TowerContent.Validate 及 LocalizationValidation 在 Build 前檢查參照、25 層範圍、重複 ID、字串/字型與格式。新增內容後必須執行完整 Build，並檢視新情境的實際畫面。

## 執行模式與存檔

一般啟動是 Phase 2 遠征模式，選擇見證者會追蹤所屬分隊；所有其他分隊繼續固定 tick 模擬。下方新增遠征分隊、Agent 觀察。F8 切換推理來源資訊，只在 Agent 觀察頁呈現。原切片可用 `--vertical-slice`，原 `--smoke` / `--localization-smoke` 等驗證模式也保留。

Phase 2 使用獨立 `ember-expedition-save.json`，schema 2 保存 RNG、各隊 Boss/預警/召喚/狀態、轉場時間、角色庫存、目標與工作、來源記憶、人格 profile、關係事件及 replay。原 witness-save.json 保留；第一次讀取若沒有新版檔，會明確將原切片匯入新 1F 遠征，保留人物/遺書/歷史，不能假造已通過的樓層。backup 回復與原子替換沿用原做法。

同層的不同隊有不同 Boss 實例。只能在同層休息安全點且沒有未完成工作時會合，時計取最大值；不能合隊重置坍塌或戰鬥。物品 uid 為持有者內的識別，移交時分配接收者的新 uid，不複製物品。所有人必須持有於一個且只有一個 Group，即使死亡仍保有死亡前組別資料。

## Optional LLM gateway

預設沒有任何外部呼叫。設定 `EMBER_REASONER_URL`（HTTPS，或 loopback HTTP）及可選 `EMBER_REASONER_KEY` 後，以 `--llm` 開啟。使用環境變數，絕不寫入資源、存檔、Git 或 replay。這是 provider-neutral JSON gateway，不直接呼叫某家商業 API；gateway 自行連接其 provider 並負責計費/真正 tokenizer 限額。

POST 內容是 LLMRequest：schema=`ember.high-decision.v1`、context、instructions 與 maxOutputTokens=256。回覆純 HighDecision JSON：intent、targetGoal、reasoningTags、riskLevel、proposedAction、dialogueIntent、confidence。intent 是 Goal 名稱；proposedAction 是 allowedSites 內 ID 或空字串。數字 0–1，tag 最多 8 個，字串長度有限。LLM 只能提出高階目標，不能移動物件、施放任意技能、改寫資源或控制每幀戰鬥。

預設 timeout 1800 ms、最多 1 次 retry、請求間隔 2 秒、每輪最多 64 次請求，另有 8192 個保守 byte/token 單位的單次預留額與 65536 個每輪預留額。計算以 UTF-8 bytes 加輸出預留作為保守上限，不宣稱等同特定模型 tokenizer。失敗、取消、超時、超額或格式非法即用本機效用 AI。網路等待期間本機 AI 繼續評估，絕不暫停模擬。較舊輪迴/組別/phase/revision 的晚到回覆丟棄；非同步 LLM 執行不保證跨機時序一致，離線 replay 使用已驗證紀錄。

最多保存 128 筆 interaction/replay 紀錄，含快照、狀態、嘗試次數、已驗證決策，不包含憑證或任意模型原文。Tools/mock_reasoner.py 是 loopback 測試 gateway，交替回傳合法/非法/延遲結果，無外部服務費用。

## 驗證命令

```powershell
python Tools/write_phase2_content.py
python Tools/write_localization.py
powershell -ExecutionPolicy Bypass -File Tools/build.ps1
powershell -ExecutionPolicy Bypass -File Tools/phase2_playtest.ps1
powershell -ExecutionPolicy Bypass -File Tools/phase2_playtest.ps1 -Width 1280 -Height 720
powershell -ExecutionPolicy Bypass -File Tools/phase2_playtest.ps1 -Lifecycle
```

畫面 fixtures 與實際自然爬塔不同：`--phase2-qa` 預置主要狀態供 UI 回歸；`--phase2-smoke` 使用真實決策、戰鬥與進度，16 倍模擬以加速整輪驗證。一般遊戲速度不變。詳細結果與未完成內容見 PHASE2_VALIDATION.md。
