# Agent 背包、記憶與休息層觀察

日期：2026-10-04。基線：ac66729。專案：`F:\CodeX開發小東東\Agent RPG`。

配置框下方新增「背包」「記憶內容」按鈕。檢視視窗暫停遊戲，關閉恢復原暫停狀態；可切換四名 Agent，長內容可捲動。背包分開列出目前裝備、材料數量及最多 24 件攜帶物品，同名物品逐件顯示，包含品質、強化、詞綴、武器基礎威力、灌注與效果。基礎威力不等於實際傷害，不含屬性、熟練度、抗性及戰鬥加成。

記憶依短期、本輪、關係、長期、生還者分類，顯示保存內容、來源、轉述者、輪迴、原始信心、記憶年齡與重要性；印象深刻的互動另列。此介面只讀取資料，不替 Agent 增加情報。舊版單層模式仍可閱讀其文字記憶。

休息層中央場景支援滾輪縮放、左鍵拖曳旋轉、右鍵拖曳平移。縮放距離限制 8–100、俯仰 20–80 度、平移中心限制在 60 × 40 地圖內。操作後進入自由觀察；底部「回到跟隨視角」恢復角色跟隨。側邊 UI、小地圖與檢視視窗不啟動場景拖曳／縮放，暫停時仍可觀察。

隕石提示原本以整片品質、屬性、技能矩形作為觸發區，沒有灌注時還會取第一個技能，並使用「灌注」標題。改成逐技能文字區域；只有非空灌注會產生灌注提示。技能提示使用實際技能效果資料。

所有新增文案集中於 zh-TW/en 字串表，沿用動態 Noto Sans CJK TC。遊戲仍為 IMGUI，無 TextMeshPro 介面。

## 驗證

最終 `inspection-build-pair-final`：Development 與 Release 同一 Editor 流程完成，exit 0，154.384 秒（含字串驗證與兩份 Build，不是單次純 Build 時間）。兩種語言各 599 鍵，placeholder parity 通過；872 種所需字元全部覆蓋。

| Player 矩陣 | 語言／解析度 | 情境 | 結果 |
| --- | --- | --- | --- |
| Development 新介面 | zh-TW／1600×900 | 14 | exit 0、issues 0 |
| Development 新介面 | en／1280×720 | 14 | exit 0、issues 0 |
| Release 新介面 | zh-TW／1280×720 | 14 | exit 0、issues 0 |
| Release 舊資料視窗回歸 | zh-TW／1600×900 | 13 | exit 0、issues 0 |

共 55 張截圖。四次完整 Player log 無例外與 JobTempAlloc 警告。涵蓋滿背包／底部／空背包、長記憶／底部／空記憶、縮放／旋轉／平移／重設、配置框空白／技能／灌注 hover，及既有死者之書、Boss 資料與四職業技能樹。抽查代表截圖，未見方框字、按鈕截斷或文字擠壓；IMGUI 不使用 TMP Auto Size。

相機斷言通過：正方向滾輪縮短距離、旋轉改變角度、平移改變中心、上下限有效、重設退出自由觀察，操作前後模擬 JSON 相同。提示斷言通過：空白處無提示、技能區是技能提示、非空灌注區是灌注提示。

Development assembly SHA256：`86b4d90e92edc95a708dbcecba08a123b9af0df7032033502793a28df5509a63`。
Release assembly SHA256：`1e68e67b6c79931fa21e8ca2863548a3b6e7f8dbb545a0679a8728c7b6e1a673`。每次 Player 測試記錄與出貨 assembly 一致。

證據：`Artifacts/agent-inspection-build-manifest.json`、`Artifacts/agent-inspection-validation.json`。原始資料保留於各 `Artifacts/Phase311/inspection-*`；代表畫面及聯絡表位於 `Docs/Media/AgentInspection`。可用 `python Tools/analyze-agent-inspection.py` 重建分析與聯絡表。

失敗紀錄保留：第一次 Build 達 600 秒上限（監控收尾共 640.221 秒）；第二次 UPM 本機連線啟動失敗；第三次 Build 成功，但首次中文 gallery 有 8 issues：操作說明高度不足、兩個武器類型翻譯缺漏及測試 fixture 的錯誤技能 ID。修正後重建雙 Build，以上最終矩陣全部通過。未刪快取或原始失敗 artifacts。

使用者原本執行中的舊版 Player 保留，QA 使用獨立 artifacts、測試存檔與程序；沒有結束或修改其遊玩進度。`watch-player.ps1` 新增選用的 `AllowExistingPlayer`，預設仍要求隔離；此選項只用於介面 gallery，不可作效能測量。這次未作效能認證。

新版執行檔：`Builds/AgentInspection/Windows/Ember.exe`（Release）與 `Builds/AgentInspection/Development/Ember.exe`。

相機 QA 使用與輸入轉接相同的控制函式，不能視為實體滑鼠硬體測試。截圖中的滿背包及長記憶為受控測試資料，不代表自然遊玩的掉落或記憶頻率。

不啟動新 5000-seed 或 120-minute soak；舊 Balance Gate FAIL 不因觀察介面更新而改判。
