# Ember Phase 2 驗證報告

驗證日期：2026-10-02。環境：Windows、Unity 6000.2.0f1、Windows Player。一般啟動進入 Phase 2 遠征；原第一階段切片保留為 `--vertical-slice`。執行檔：`Builds/Windows/Ember.exe`。

## 完成範圍

先完成 [架構審查](PHASE2_ARCHITECTURE.md)，再依資料、Boss、決策、記憶、分隊、休息層、進度、介面及測試順序實作。[內容製作與執行指南](PHASE2_CONTENT_PIPELINE.md) 說明資料擴充、存檔與 LLM gateway。

- 25 層資料與正式通關資格：四種主題、21–24 層終末變體、25 層塔心；樓層連結 Boss、能力、階段、掉落、休息設施與環境參照。前五層提供守護者、施法者、衝鋒者、召喚者、環境型五種實際不同的戰鬥行為；後二十層明確標示內容佔位。
- Boss 能力模組含預警、目標規則、打斷、狀態、召喚、護盾、生存階段、元素弱點、狂暴與輸出壓力。階段可由生命或時間切換。
- 高階效用決策與固定 tick 戰術執行分離，涵蓋戰鬥、支援、恢復、製作、情報、遺書、掠奪、撤離、離隊與會合。11 種人格傾向與八維關係事件會影響決策；情報具有來源、可信度與時效。
- 可選 LLM 高階推理具嚴格 schema、非同步 timeout/retry、頻率/每輪預算、合法性檢查、晚到回覆淘汰、本機 fallback 與有界 replay。網路等待不停止戰鬥；憑證僅由環境變數讀取。
- 分隊有各自的 Boss、樓層、休息時計與進度；未觀察的隊伍持續模擬。離隊不複製人物/物品；同層安全休息點可會合，時計取最大值。完成判定等待所有分隊終止。
- 記憶分作用域與來源。普通失敗、死亡與手動中止會重置不合資格的私人記憶；只有真正完成 25 層且存活的個體可保留勝者記憶。死者之書與歷史另外保存。
- 九種休息設施提供實際成本、任務時間、風險與效果，決策預留移動、工作及撤離時間。schema 2 存檔保存分隊、戰鬥、角色、記憶、關係、RNG 與決策；原存檔可匯入新 1F 遠征。
- 玩家介面增加分隊與 Agent 觀察頁、目標、情報與記憶來源、關係事件；F8 顯示推理診斷。程序模型呈現五種 Boss 輪廓、環境、預警與狀態。中英各 313 個字串鍵，技術 ID 保留。

## 自動測試與大量種子

原核心測試 242 項斷言保持通過，新增 Phase 2 87 項，共 329 項。完整 Build 前執行測試，Windows Build 成功。

新增斷言覆蓋 1–25 層、生命/時間階段、九種機制、打斷/元素、分隊分割型態、獨立進度/會合時計、記憶重置/勝者/死亡/手動中止、書籍可信度、關係事件、物品移交、複雜存檔後續一致性、backup/非法 ownership、舊存檔遷移，以及 LLM 合法/非法/缺欄位/超時/重試/頻率/預算/replay。

1000 組本機 deterministic seeds（1–1000）使用實際 AI、戰鬥與休息進度，沒有強制勝利。每組另檢查存檔 JSON 往返；續玩一致性由獨立單元測試驗證。

| 統計 | 結果 |
| --- | ---: |
| 通關 / 全滅 | 1000 / 0 |
| Crash | 0 |
| Softlock | 0 |
| Impossible progression | 0 |
| Infinite combat | 0 |
| Split party deadlock | 0 |
| Save corruption | 0 |
| 離隊 / 會合 | 24708 / 21715 |
| 最高樓層 | 25 |
| 最長單輪模擬時間 | 1717.8 秒 |

結果保存於 [phase2-tests.txt](../Artifacts/phase2-tests.txt)。全部通關代表目前原型難度偏友善，不能當成正式平衡或死亡率驗證；死亡與重置另用指定情境測試。

## Windows 實際執行與中文化

| 驗證 | 結果 |
| --- | --- |
| Phase 2 畫面 | 25 情境 × 1600×900 / 1280×720，共 50 次，issues 0 |
| 原切片中文化畫面 | 22 情境 × 兩種解析度，共 44 次，issues 0 |
| 字串/字型 | 中英各 313 鍵、格式參數一致；595 個不同字元有字形 |
| 本機規則 AI 完整輪迴 | 1–25 層 → TowerClear → 第 2 輪；退出碼 0 |
| 可選 LLM 完整輪迴 | loopback gateway 13 次請求 → TowerClear → 第 2 輪；退出碼 0 |
| 原切片 smoke | 戰鬥 → 避難層 → 結局 → 第 2 輪；退出碼 0 |

本機規則完整輪迴模擬時間 823.0476 秒，離隊 14 次、會合 11 次，四人取得完成資格。LLM 測試輪迴 864.2360 秒，離隊 10 次、會合 7 次。自動完整輪迴使用 16 倍模擬加速；一般玩家控制維持原速度設定。

loopback 測試服務交替提供合法、非法及延遲回覆，驗證實際 Player 持續運作；沒有連接商業 provider。replay 上限 128 筆，結局資料的早期請求已被後續頻率/預算 fallback 紀錄取代；各 accepted/timeout/schema 狀態由單元測試獨立驗證。

UI 測試實際渲染主要戰鬥、五種 Boss、階段、主題、分隊、記憶、關係、休息、死亡/結局、診斷與語系情境，檢查字形、所需文字高度、按鈕寬度、邊界、缺鍵與未解析 token。另直接檢視代表截圖與完整輪迴結局，未見方框字、裁切或中文擠壓。測試透過與按鈕相同的 handler，不代表所有滑鼠/鍵盤操作都經人工逐項驗證。

現有 UI 使用 IMGUI，嵌入完整 Noto Sans CJK TC；專案沒有 TextMeshPro，TMP fallback / Auto Size 不適用。結果見 [畫面摘要](../Artifacts/phase2-layout-tests.txt)、[字串與字型結果](../Artifacts/localization-tests.txt)。實際截圖與 Player logs 位於未納入 Git 的 `Artifacts/Phase2`；原切片結果位於 `Artifacts/Localization` 與既有 playtest 目錄。

## 已知限制

- 25 層流程完整，但後二十個 Boss 是五種模組的重組佔位，尚非二十個獨立完整 Boss 製作。美術仍以程序幾何重用為主；音樂/環境音參照是 placeholder。
- 分隊/會合限定安全休息點；競技場沒有複雜地圖與 NavMesh，死亡表現仍為原型。
- 原技能樹沿用；全套技能各自的復活、毒、陷阱與控場效果尚未完整深化。記憶與對話採型別化資料、效用權重及情境模板。
- LLM 是通用 JSON gateway，商業 provider adapter、真正 tokenizer/費用計算需另實作。非同步 LLM 時序不保證跨機決定性；有界 replay 不是完整商業服務會話錄製。
- 測試沒有偵測到 managed exception，但原有 Unity `JobTempAlloc` 退出警告仍存在，來源尚未確認。尚未完成數小時實際渲染 soak、memory profiler 或其他平台/解析度驗證。

## 下一階段建議

1. 先製作 6–10 層各自的 Boss、場景、美術與音樂，沿用現有資料與機制模組。
2. 調整難度、分隊凝聚力、風險與掉落分配；補齊技能效果，增加真正會失敗的自然種子測試。
3. 完成長時間渲染/記憶體效能診斷，再接入商業 LLM adapter、tokenizer、費用可觀察性與實際 provider 情境測試。

## Git 里程碑

| Commit | 內容 |
| --- | --- |
| `6de0ddc` | feat: add phase 2 data architecture |
| `dc68c16` | feat: expand boss framework |
| `131c1f8` | feat: add high level agent brain |
| `1cb325a` | feat: add optional llm reasoner |
| `5446062` | feat: add advanced memory model |
| `74fcfad` | feat: add party split progression |
| `622f18f` | feat: expand rest area simulation |
| `f34825b` | feat: add 25 floor progression framework |
| `63d3799` | feat: integrate phase 2 observer UI and reusable visuals |
| `e95ff05` | fix: enforce survivor retention and expose typed memories |
| `d44a875` | test: add phase 2 simulation coverage |

本報告、內容指南、README 與既有驗證文件以最後的 `docs: complete phase 2 validation` 提交。所有提交保存在本機 Git，未推送遠端。
