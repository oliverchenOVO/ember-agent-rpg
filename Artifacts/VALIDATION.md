# 第一階段驗證 — 2026-10-02

已交付 Unity 6000.2.0f1 專案與 Windows Mono 執行檔，位置為 `Builds/Windows/Ember.exe`。
Blender 3.1.2 已實際執行原創面具生成工具，FBX 已載入遊戲；不含外部下載資產。

## 實作範圍

四名固定人格 Agent，自主加權選職與加點，允許重複職業。以 `--showcase` 可在首輪展示四職業。
本機 UtilityBrain 以情境、人格與有向關係評分；每 1.5 秒重新決策，固定 0.1 秒低層模擬。
一隻完整 Rootcrown Boss，有範圍预警、避招、範圍重擊、荊棘波與半血狂暴。
完整串接戰鬥、非陣容導向掉落、背包、藥水、升級、技能、裝備、製作、灌注與跨職業武器贈與。
休息區含治療、遺書閱讀、工坊、寶箱與出口；26 秒後坍塌前線由入口推進並殺死落後者。
死亡不終止其他人的戰鬥；全滅或所有存活者逃離後寫入遺書、紀錄周目，8 秒後重啟。
觀察介面顯示意圖原因、人物屬性、武器、技能、血魔、關係、遺書與歷史；提供暫停、速度、鏡頭及存讀檔。

## 已執行的驗證

- Unity 編譯與 Windows Release Build 成功，退出碼 0。
- 核心 harness：**242 項斷言全部通過**，詳見 `core-tests.txt`。
- 100 組種子自主陣容全部在 250 秒模擬上限內完成切片並重啟；100 次完成、0 次自然全滅、356 次製作。
- 固定單一 simulation 连續執行 60,000 tick（6,000 秒模擬時間），超過 100 次周目切換，歷史/遺書/庫存/當輪訊息保持設定上限。
- 獨立驗證孤存者繼續、全滅、坍塌死亡、死者不能施法、四技能槽、前置技能、武器需求、專精、Mana/CD、防禦。
- 製作資源與時間、已解鎖灌注、跨職物品轉交、禁止重複轉交、持有者支付治療 Mana、關係更新皆通過。
- 記憶失敗重置、真正 25F 勝利的存活者記憶保留規則，使用明確測試 fixture 驗證；**遊戲目前不提供 25F 通關**。
- 存檔 JSON 往返、RNG 恢復後續跑一致、損毀 primary 回讀備份、拒絕未知 schema 都通過。
- Windows Player 實際執行完整一輪，產生 battle/refuge/end 截圖，寫出 player-run.json，並啟動第 2 輪；smoke 退出碼 0。
- 另執行預置的坍塌呈現情境；此情境為視覺檢查，與自然決策 soak 分開。
- 實際檢視截圖，修正輔助文字對比、姓名牌重疊、FBX 面具比例、配裝視覺更新。

## 限制與待驗證項目

目前是一 Boss 垂直切片。25 Boss 規劃已寫入設計文件，其他 24 Boss 尚未實作。
切片完成標記 SliceComplete，不能取得正式塔通關最快紀錄或勝者記憶資格。
LLM provider、完整分隊各自爬塔、完整技能效果（復活/毒/淨化等）、數百裝備、最終音樂與專業動畫仍為下一階段。
目前對話由情境模板組成，並非 LLM 自由語言；關係數值与情境会影響決策。
UI 畫面已檢查，所有按鈕和鍵盤組合尚未逐一做人工交互驗證。
連續測試為核心模擬，尚未完成數小時的實際渲染 soak / memory profiler 驗證。
Unity 在一般 Player 退出時出現 `JobTempAlloc` 內部分配警告；D3D11/12 均曾重現。
加上 `-diag-job-temp-memory-leak-validation` 的短程診斷未產生可歸因的堆疊，因此來源尚未確認。
遊戲邏輯無偵測到 managed exception，smoke 正常退出，但不能據此宣稱長時間渲染無記憶體問題。
背景隱藏視窗會產生黑色 ScreenCapture，視覺驗證需使用 `Tools/playtest.ps1 -Visual`。
smoke 使用 Artifacts 下獨立存檔，不覆蓋一般遊戲資料。

## 重現

```powershell
powershell -ExecutionPolicy Bypass -File Tools/build.ps1
powershell -ExecutionPolicy Bypass -File Tools/build.ps1 -TestsOnly
powershell -ExecutionPolicy Bypass -File Tools/playtest.ps1 -Visual
powershell -ExecutionPolicy Bypass -File Tools/playtest.ps1 -Visual -Collapse
```

一般遊戲：直接執行 Ember.exe。Unity：開啟 UnityProject，載入 Assets/Scenes/Witness.unity 並 Play。
Git 使用本機 main 分階段提交，無遠端；未建立外部帳號、付費服務或提交憑證。

## zh-TW 全專案中文化驗證（2026-10-02）

- 已先搜尋整個 Assets、Scene、ProjectSettings 與內容產生工具，建立 Docs/LOCALIZATION_INVENTORY.md；中英字串表各 169 鍵，包含所有現有玩家可見 UI、動態紀錄、Agent 對話、技能/物品名稱、死亡與歷史資訊、系統提示及 Windows 視窗標題。技術識別與存檔 schema 未改名。
- 中英鍵與格式參數一致、catalog/enum 覆蓋、語系切換、巢狀參數、舊版英文紀錄轉換、token JSON 往返全部通過。結果見 localization-tests.txt。
- 嵌入 Noto Sans CJK TC Regular，字串表與固定角色名/數字使用的 470 個不同字元全部有字形。TMP 未安裝，現有介面使用 IMGUI；TMP fallback / Auto Size 不適用。
- Windows Player 重新 Build 成功；原有 242 項核心斷言通過，100 組種子完成並重啟、356 次製作，既有測試未受翻譯影響。
- 在 1600×900 與 1280×720 各執行 22 個實際渲染情境，共 44 次：戰鬥、Boss 預警與第二階段、避難層、四技能槽、坍塌、空/有資料的死者之書和挑戰紀錄、關係、補師資訊、暫停、倍速、儲存/讀取成功與實際失敗、新輪迴、英文切換、恢復繁中、舊存檔紀錄。兩種解析度皆 Issues: 0，Player 退出碼 0。
- 每次渲染檢查字形、文字所需高度、按鈕寬度、UI 邊界、未解析 token 與缺鍵。直接檢視代表性截圖，包括繁中戰鬥、四技能槽、死者之書、歷史、關係、英文、1280×720 死者之書、舊英文訊息轉換與儲存失敗提示，未見方框字、裁切、換行或版面擠壓。已修正 Noto 行高造成的舊版 padding/高度不足。
- 另以一般 zh-TW Player 完整執行戰鬥 → 休息層 → 結局 → 自動第 2 輪，smoke 退出碼 0；battle/refuge/end 截圖及 player-run.json 已更新。無 managed exception，原有退出時 JobTempAlloc 警告仍存在。
- 自動測試呼叫與按鈕相同的 handler；沒有宣稱完成所有滑鼠/鍵盤操作的人工逐項測試。其餘解析度、平台及長時間渲染仍需另行驗證。未知舊版自由英文文本保留原始資料，繁中顯示替代提示，限制詳見 Docs/LOCALIZATION.md。

完整畫面測試輸出與截圖位於 Artifacts/Localization（不納入 Git），兩種解析度的摘要保存於 localization-layout-tests.txt。
