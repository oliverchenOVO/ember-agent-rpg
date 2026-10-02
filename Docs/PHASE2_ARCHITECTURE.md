# Phase 2 架構審查與實作方向

## 審查結論

已閱讀現有設計與 Core / Presentation / Editor 程式。Agent 的成長、技能合法性、物品實例、數值與種子 RNG 可直接沿用；Loc token 與中英表也可擴充。原切片的 Simulation 同時負責戰鬥、休息、掉落、關係與重啟，World 只有單一 Boss/phase/floor，不能將同一份狀態分享給不同樓層的分隊。UtilityBrain 混合高階目的與戰術執行；記憶只有字串，關係缺乏事件來源。

保留 Simulation 與 schema-1 存檔、242 項基準測試和原切片模式。新增 TowerSimulation 作為正式爬塔排程器，ExpeditionState 擁有所有 Agent、有限事件/記憶、各 Group 的獨立 Boss / Rest / Travel。玩家觀察 World 是投影，不是另一套權威狀態；切換觀察目標不能停掉其他隊伍。舊存檔以明確遷移建立 1F Expedition，不將 SliceComplete 誤認為 25F 勝利。

## 資料與內容擴充

版本化 tower.json 包含 25 個 FloorDefinition、BossDefinition、AbilityDefinition、PhaseDefinition、LootTable、RestDefinition、EnvironmentProfile。1–5 森林、6–10 冰原、11–15 城堡、16–20 深淵、21–24 為四環境終末版本、25 為塔心。五種可執行 archetype：近戰守護者、遠距施法者、衝鋒野獸、召喚者、多階段環境 Boss。其他專屬美術/機制採明確 placeholder，以組合能力驗證資料流程，不宣稱 25 隻最終內容已製作。

資料的穩定 ID 連接 loot / rest / environment / abilities。內容產生工具只是 authoring，不參與每幀決策。新增既有 mechanic 類型的 Boss/Floor/Item/Skill 不需改排程器；新 mechanic 類型仍需增加執行模組及測試。所有玩家可見名稱與事件使用 Loc 表。

## 決策與 LLM 邊界

AgentDecisionContext 是高階快照，含生命比例、風險、出口時間、可用資源、Boss 情報、同組隊友、帶来源的記憶與關係摘要。RuleBasedReasoner 以效用權重決定目標，每 1.5 秒或狀態轉換觸發；LowLevelExecutor 每個固定 tick 執行移動/閃避/技能/目標，不呼叫 LLM。人格與關係改變候選分數，不能強制劇情結果。

IAgentReasoner 僅輸出 HighDecision DTO。LLMReasoner 接受可替換傳輸，非同步、限時、有限重試、頻率與 token 上限、驗證、回退及有限 replay 紀錄；未知 ID/越權/無效 JSON 不能控制遊戲物件。API key 僅從環境讀取，不序列化。無 API 時預設 RuleBasedReasoner，模擬不等待網路。外部服務的回覆時序不承諾決定性，replay 使用已驗證決策紀錄。

## 分隊、死亡與通關

Group 擁有成員 ID、自己的樓層、Boss 實例、休息時計、策略與移動時間；Agent 的裝備/庫存始終個人所有，掉落只發給擊敗該 Boss 的 Group。不同 Group 即使在同層也不共用 Boss 血量。Rejoin 只允許同層休息/移動安全點且沒有未完成工作，合併休息時計取較危險值，不藉合隊重置 Boss/坍塌。離隊會帶走人與其進度快照，戰鬥中不複製掉落。

死者停止行動並留下有限遺書。當所有 Group 都死亡或抵達 25F 結束，才結算 Expedition；至少一名真正完成 25F 的生還者才有 TowerClear。普通失敗清除所有私人 Run / Working / Relationship 記憶，Book 持續保留；勝利僅真正完成且活著的人保留 LongTerm / Survivor 與關係。死人及未完成者重置。輪迴成長/裝備重置，有限歷史與 Book 保留。

## 風險與驗證策略

重點風險是分隊時角色重複所有權、共用 Boss、存檔半套狀態、非同步回覆跨輪迴污染、休息時間/轉場造成停滯，以及高層成長/戰鬥失衡。以資料參照驗證、存檔結構驗證、generation/decision ID、有限紀錄與進度 watchdog 檢查；watchdog 只記錄並使測試失敗，不能偽造通關。

原 242 斷言不刪減。新增機制單測、各種分隊/合隊、跨層排程、記憶與儲存往返、LLM 故障回退，並跑 1000 固定種子統計。實際 Windows Player 分別驗證五 archetype、25F、分隊、多語系與主要操作；程序核心 soak 和實際畫面測試分開報告。
