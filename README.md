# EMBER — The Witness Tower
四名自主 Agent 的 3D Roguelite 觀察型 RPG，Unity 6000.2.0f1。

目前預設執行 Phase 2：資料驅動的 25 層流程、五種 Boss 戰鬥 archetype、分隊獨立爬塔、來源記憶與 optional LLM gateway。其他 Boss 專屬內容、音樂及完整美術仍採 placeholder，並非 25 套最終內容。
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

遊戲預設繁體中文（zh-TW），右下角語言按鈕可即時切換英文。完整字串盤點見 Docs/LOCALIZATION_INVENTORY.md；語系、舊存檔及字型架構見 Docs/LOCALIZATION.md。
中文化畫面測試：powershell -ExecutionPolicy Bypass -File Tools/localization_playtest.ps1
較小視窗測試：powershell -ExecutionPolicy Bypass -File Tools/localization_playtest.ps1 -Width 1280 -Height 720
