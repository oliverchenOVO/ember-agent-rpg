# EMBER — The Witness Tower
四名自主 Agent 的 3D Roguelite 觀察型 RPG，Unity 6000.2.0f1。

最新追加修正：休息區會抽選可用設施（可完全沒有設施），並修正死亡來源持續傷害造成的假彈幕。可執行版本在 `Builds/RestInteraction/Windows/Ember.exe`，功能、存檔相容性與畫面驗證見 [休息區與彈幕驗證](Docs/REST_INTERACTION_VALIDATION.md)。這次局部修正沒有重跑完整平衡 Gate；下方 Phase 3.1.1 結果保留為該版紀錄。

Phase 3.1.1 **Gate FAIL，不進入 Phase 4**。Build pipeline 已恢復，Development／Release 成功，3,500 個診斷 seed-runs、治療改善、雙語畫面及存讀檔證據見 [最新驗證報告](Docs/PHASE3_1_1_VALIDATION.md)。H3 法師／弓箭手編成仍不合格，沒有 RC／FINAL；500／1000／5000 正式批次未啟動。本輪成品另存 `Builds/Phase311/EXP2.1-R1`，完整 SHA 見 `Artifacts/phase3_1_1_exp21_r1_build-manifest.json`；原 `Builds/Windows` 是歷史成品。舊 700 樣本與當時停滯記錄保留在 [Phase 3.1 報告](Docs/PHASE3_1_VALIDATION.md)。`Tools/build.ps1 -TestsOnly` 包含 5000 seeds，非本輪收尾短測指令。

目前預設執行 Phase 3：25 層流程、原五種 Boss 與 6–10F「廢棄星鑄工坊」五個專屬 Boss、分隊爬塔、來源記憶及可選 LLM gateway。技能效果、跨職業灌注、觀察鏡頭、聲音事件與難度 telemetry 已深化；11–25F 仍有佔位內容，並非 25 套最終製作。
設計與內容 authoring 文件在 Docs；執行與測試命令在 Tools。架構審查見 Docs/PHASE2_ARCHITECTURE.md；內容與 LLM 設定見 Docs/PHASE2_CONTENT_PIPELINE.md。

用 Unity Hub 開啟 UnityProject，開啟 Assets/Scenes/Witness.unity，按 Play。
Windows 成品：Builds/Windows/Ember.exe。空白鍵暫停，1/2/3 切換速度，方向鍵環繞鏡頭。
HUD 可查看角色、遺書、歷史、遠征分隊、Agent 觀察與存讀檔。點選角色可追蹤其分隊，其他隊伍會繼續模擬。F8 在 Agent 觀察頁顯示推理來源。
預設自主選職；Ember.exe --showcase 可在首輪展示四種職業。Ember.exe --vertical-slice 保留原第一階段模式。

重新建置：powershell -ExecutionPolicy Bypass -File Tools/build.ps1
只跑測試：powershell -ExecutionPolicy Bypass -File Tools/build.ps1 -TestsOnly
實際畫面驗證：powershell -ExecutionPolicy Bypass -File Tools/playtest.ps1 -Visual
坍塌畫面情境：powershell -ExecutionPolicy Bypass -File Tools/playtest.ps1 -Visual -Collapse
Blender 原創面具：blender --background --python Tools/create_mask.py

smoke 使用 Artifacts 內獨立存檔，不覆蓋一般遊戲存檔。一般存檔位於 LocalLow/WitnessWorks/Ember - The Witness Tower。

Phase 2 實測與限制請見 Docs/PHASE2_VALIDATION.md；第一階段紀錄保留於 Artifacts/VALIDATION.md。
Phase 2 畫面檢查：powershell -ExecutionPolicy Bypass -File Tools/phase2_playtest.ps1
Phase 2 完整實際輪迴：powershell -ExecutionPolicy Bypass -File Tools/phase2_playtest.ps1 -Lifecycle

Phase 3 設計：Docs/THEME_B_DESIGN.md；難度：Docs/DIFFICULTY_CURVE.md；內容與重現：Docs/PHASE3_CONTENT_PIPELINE.md；驗證：Docs/PHASE3_VALIDATION.md；效能：Docs/PERFORMANCE.md。
Phase 3 畫面：powershell -ExecutionPolicy Bypass -File Tools/phase3_playtest.ps1
Phase 3 TestsOnly 包含既有回歸、新增斷言與 5000 seeds。Development Build 及長程 profiling 指令見內容指南。

遊戲預設繁體中文（zh-TW），右下角語言按鈕可即時切換英文。完整字串盤點見 Docs/LOCALIZATION_INVENTORY.md；語系、舊存檔及字型架構見 Docs/LOCALIZATION.md。
中文化畫面測試：powershell -ExecutionPolicy Bypass -File Tools/localization_playtest.ps1
較小視窗測試：powershell -ExecutionPolicy Bypass -File Tools/localization_playtest.ps1 -Width 1280 -Height 720

Phase 3 最後收尾（2026-10-03）：已在 F 槽完成 Release、5000 組種子與 120 分鐘實際渲染；編成比較與數據報告已保存。原生警告、UI 像素字元缺漏、壓力負載尖峰與 10F 難度仍有待修項，不宣稱所有完成條件通過。詳見 Docs/PHASE3_VALIDATION.md。
