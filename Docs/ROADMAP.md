# 開發里程碑
M0：環境、9 份設計、內容 schema、本機 Git。
M1：決定性模擬、合法性驗證、技能/物品/關係/存檔測試。
M2：首 Boss 3D 觀察者、完整休息區、製作、坍塌、死亡遺書與輪迴，Windows 可執行檔。
M3：Playtest 與多種子 soak，校正效用與視覺可讀性；此階段交付垂直切片。
M4：2–5F 各獨立 Boss、地圖路徑、裝備內容、完整 split-party scheduler。
M5：6–20F 四環境、完整技能效果、控制狀態、音樂/動畫/美術資產 pipeline。
M6：21–25F、完整記憶敘事、速度榜、可選 LLM provider 與成本上限。
M7：長時間穩定性、效能、可及性、完整音畫製作与 release。
每階段 commit，僅已執行的驗證可標記通過。不把規劃内容標為已製作。

## Phase 2 進度更新（2026-10-02）

已完成 25 層資料/進度框架、五種可執行 Boss archetype、獨立分隊與會合、高階決策、可選 LLM gateway、來源記憶、九種休息設施與 schema 2 存檔。上列 M4–M6 的系統基礎已有實作；後二十層獨立內容、完整技能效果與最終音畫仍待製作，不能把框架完成視為全部內容完成。測試數據、限制與下一階段建議見 [Phase 2 驗證](PHASE2_VALIDATION.md)。

## Phase 3 收尾（2026-10-03）

6–10F 工坊、五 Boss、24 技能效果、灌注回饋、掉落/打造估價、鏡頭、音效事件與 telemetry 已實作。F 槽完成 Release、5000-seed 完整資料、五編成各 50 組及 120 分鐘真實 rendering 長測。433 累積斷言通過；仍有 runtime JobTempAlloc、UI 像素字元缺漏、共享壓力負載幀時間尖峰與 10F 難度突增，**不能標記所有 Phase 3 完成條件通過**。

報告與證據見 [最終驗證](PHASE3_VALIDATION.md)、[效能](PERFORMANCE.md)、[難度](DIFFICULTY_CURVE.md)。工作目錄已搬到 F 槽，必要資料與未來搬移流程見 [PROJECT_MOVE.md](PROJECT_MOVE.md)。本輪收尾後等待下一步，未開始 Phase 4。

## Phase 3.1.1 收尾（2026-10-03）

Build pipeline 已恢復；新 Development／Release 與成品 SHA 完整保存。3,500 個診斷 seed-runs 顯示預測治療降低 overheal，但 H3 的法師 7/100、弓箭手 15/100、補師 100/100 突破 10F，組成平衡仍 FAIL。雙語主要 UI／遊戲中切換與七種 Save/Load replay 有通過證據；Native 根因與完整效能 Gate 仍有量測限制。沒有 RC／FINAL，沒有啟動新版正式 500／1000／5000 或 120 分鐘 soak；不進入 Phase 4。最新資料與限制見 [Phase 3.1.1 驗證](PHASE3_1_1_VALIDATION.md)。


## 持續扣血玩法改版（2026-10-04）

全場壓力與超時耗損改為可預警、可離開、可摧毀的壓力核心；整合 Agent 閃避、普攻目標、預測治療與存檔。僅進行受控短測與畫面回歸，不重跑長時間測試，不更新歷史 Balance Gate 為通過。見 [驗證紀錄](PRESSURE_CORE_VALIDATION.md)。


## 職業與隊伍平衡改版（2026-10-04）

修正技能主要屬性成長與武器估價，調整補師攻守取捨、法師耗魔與隊伍支援協作。使用受控前後短測、行為檢查及 Player 介面回歸收尾；沒有重啟長測，沒有將既有平衡 Gate 改為通過。見 [驗證與數據](TEAM_BALANCE_VALIDATION.md)。
