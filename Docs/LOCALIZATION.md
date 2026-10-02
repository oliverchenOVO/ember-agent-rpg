# EMBER 語系架構

預設語系為 `zh-TW`。右下角「語言 / 繁體中文」可即時切換英文，選擇會存入 PlayerPrefs；啟動參數 `--locale zh-TW` 或 `--locale en` 可指定此次語系。品牌 EMBER、KAEL / LYRA / ORIN / SERA 及 Agent RPG 保留原名。

## 字串表與新增文案

`UnityProject/Assets/Resources/Localization/zh-TW.json` 與 `en.json` 是獨立的執行期字串表，Phase 3 共 399 個語意鍵，保留 Phase 2 的 313 鍵與原切片的 169 鍵。`Core/Localization.cs` 的 `Loc.T` 讀取字串表、格式化參數及處理缺鍵；缺鍵會記入 `MissingKeys`，並回退至英文表。UI 不自行保存翻譯內容。

`Tools/write_localization.py` 集中維護中英對照，匯入 `Tools/phase2_strings.py` 與 `Tools/phase3_strings.py`，產生兩份表及 `Docs/LOCALIZATION_INVENTORY.md`。新增文案時增加穩定的語意鍵，再執行此工具；新增技能或物品也要補入對照，不要改動 catalog ID。盤點清單列出完整英文來源、中文與語意鍵，包含視窗標題、HUD、提示、按鈕、職業、行動、結局、技能、物品、Boss、休息層、Agent 意圖與對話、戰鬥紀錄、記憶和遺言，以及分隊、記憶來源與推理除錯文案。

當場顯示使用 `Loc.T("ui.save")`；要儲存的訊息使用 `Loc.Token("event.…", arguments)`，顯示時才呼叫 `Loc.Render`。參數可用 `Loc.Ref("skill", id)` 等巢狀 token，避免切換語系後句子中的技能名稱仍停留在舊語系。類別、enum、資料欄位、檔名及資源 ID 維持原有識別字。

## 舊存檔

遠征存檔 schema 為 2，原切片存檔 schema 為 1，token 寫入原有文字欄位；遊戲產品識別與原有存檔位置不變。原版英文模板、技能/物品名稱與 ID、職業及結局，在顯示時透過精確對照與參數化模板轉換，包含歷史紀錄與遺言。未知的舊版自由英文內容無法可靠自動翻譯，繁中介面會顯示集中定義的中文替代提示，原始存檔文字仍保留；切換英文可讀取原文。原有中文與允許的角色名維持可讀。

## 字型與排版

隨專案提供完整的 `NotoSansCJKtc-Regular.otf`，載入為 Dynamic Font 並嵌入字型資料，不依賴玩家電腦安裝字型。來源為 Noto 官方 `notofonts/noto-cjk` 的 Traditional Chinese Regular；授權為 SIL Open Font License 1.1，全文隨字型放在 `Resources/Fonts/OFL.txt`。

字型 SHA-256：`DCE08BD4FD91AA8AA76ED8FEA4B694C2DFB8550F67871E326843212DDBEB88B4`。

目前所有文字由 IMGUI 繪製。專案未安裝 TextMeshPro，沒有 TMP 元件、fallback chain 或 Auto Size 設定，因此這些項目不適用。使用同一份完整 TC 字型，驗證字串表及玩家畫面的字形覆蓋。若日後改用 TMP，需另外建立涵蓋相同字元的 TMP Font Asset、設定 TC fallback 並重跑畫面檢查，不能直接假設此結果適用。

配合 Noto 行高，已調整標籤 padding、字級、標題/數值區高度、按鈕寬度與高度、四技能槽區域、配置品質資訊及底部說明區域。UI 依 1600×900 邏輯座標縮放。

## 驗證

完整 Build 先執行 `LocalizationValidation.Run`，檢查中英鍵與格式參數一致、catalog/enum 覆蓋、字型字形、巢狀語系切換、舊記錄轉換及 token JSON 往返，再執行原有核心測試。

```powershell
powershell -ExecutionPolicy Bypass -File Tools/build.ps1
powershell -ExecutionPolicy Bypass -File Tools/localization_playtest.ps1
powershell -ExecutionPolicy Bypass -File Tools/localization_playtest.ps1 -Width 1280 -Height 720
powershell -ExecutionPolicy Bypass -File Tools/playtest.ps1 -Visual
```

中文化 Player 測試使用 `--localization-smoke`，截圖及獨立存檔位於 `Artifacts/Localization/<解析度>`。每次 22 個情境涵蓋主要介面、空資料、四技能槽、死亡/坍塌、存讀檔成功與失敗、英文切換與舊記錄。它使用 UI 相同的操作 handler，並在實際渲染時檢查高度、按鈕文字寬度、視窗邊界、字形與缺鍵；這是自動操作及畫面檢查，並非人工逐一點擊所有按鈕。結果與限制見 `Artifacts/VALIDATION.md`。

2026-10-03 F 槽最後 Release：兩語系各 399 鍵、685 字形檢查通過；兩解析度中文化 44 情境與 Theme B 40 情境的自動測量均為 0 issues。然而人工檢視在英文切換的頂列，以及一張繁中 9F 角色列截圖，發現數字或前導字元未完整渲染。切回繁中截圖正常。這是尚未解決的像素呈現問題，自動 `HasCharacter` / 邊界檢查無法涵蓋；不能宣稱 84 張畫面全部無缺字。動態字型 atlas / IMGUI 是待查方向，尚未證實根因。本輪維持凍結 Runtime，證據與限制見 `Artifacts/phase3-visual-review.txt`。
