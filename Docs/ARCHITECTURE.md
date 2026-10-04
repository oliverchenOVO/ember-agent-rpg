# System architecture · 系統架構

EMBER separates authoritative simulation state, bounded decision proposals, local execution and observable presentation. The diagrams describe the current implementation, not a planned neural-network architecture.

![Architecture](../Media/Diagrams/architecture.png)

## 狀態由誰擁有？

`ExpeditionState` 擁有所有 Agent、個人背包、記憶、關係與 Group。每個 Group 有自己的樓層、Boss、休息時計和轉場進度。`TowerSimulation` 更新權威狀態；畫面顯示的是投影，切換觀察角色不會停止其他分隊。

| 層 | 責任 | 邊界 |
| --- | --- | --- |
| Content | JSON 定義樓層、Boss、技能、掉落、休息層與文字 | 新機制仍需新增執行程式，不能宣稱一切只靠 JSON |
| State | 角色、Group、RNG、記憶、裝備、關係 | 避免分隊共享 Boss HP 或重複所有權 |
| Context / Reasoner | 用合法選項、資源、時間、人格與關係評分高階目標 | 回傳有限 DTO，不直接改 Transform |
| Combat / Rest execution | 施法、移動、閃避、搜尋、互動、撤離 | 檢查生命、魔力、冷卻、距離和障礙 |
| Presentation | UI、模型、技能演出、鏡頭、雙語文字 | 不另建一套會與模擬分岔的遊戲狀態 |
| Persistence / evidence | 版本化存檔、RNG 狀態、事件與 telemetry | 驗證結果必須綁定正確版本 |

## 更新節奏

固定模擬步長預設為 0.1 秒。高階決策一般以 1.5 秒為間隔，並受工作、討論與非同步回覆狀態影響；本機戰術候選約每 0.6 秒重新評估。危險判定與低階移動在固定 tick 執行，不能等待遠端網路。

## 可選 LLM 的受限介面

預設使用 `RuleBasedReasoner`，遊戲不需要 API。`LLMReasoner` 是可選的高階 JSON gateway，有 timeout、重試、頻率與預算上限。回覆必須通過 schema、允許目標／設施及 generation/run/revision 檢查，才能採用；跨輪迴、過期或非法回覆被拒絕。

LLM 不控制逐幀操作。外部回覆時序不保證可重現，決定性主張限於固定本機規則、內容、seed 與相同輸入；外部決策需另記錄才能重播。

## 分隊與合隊

離隊建立獨立 Group，帶走指定成員及進度快照。個人裝備不複製成公共背包；不同隊即使同層也有獨立 Boss。合隊限制在相容的休息狀態，不能用合隊重置坍塌時間；休息時計取兩隊較危險的值。

## Why this design?

This separation makes decision failures observable and testable. A screenshot can explain what an Agent chose; a seeded fixture can test whether that action was legal; telemetry can compare the resulting damage, healing and resources. The intended research question is how bounded autonomous policies behave together—not whether an LLM can operate a game controller.

[Action logic](AGENT_DECISION_LOGIC.md) · [Algorithms](ALGORITHMS.md) · [Memory](MEMORY_RELATIONSHIPS.md) · [Evidence](ENGINEERING_EVIDENCE.md).
