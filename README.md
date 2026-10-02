# EMBER — The Witness Tower
四名自主 Agent 的 3D Roguelite 觀察型 RPG，Unity 6000.2.0f1。

目前目標：一個 Boss + 危險休息區 + 遺書 + 自動重新開始的可驗證垂直切片。
設計文件在 Docs；執行與測試命令在 Tools。25 層、LLM 與完整美術為後續里程碑。

用 Unity Hub 開啟 UnityProject，開啟 Assets/Scenes/Witness.unity，按 Play。
Windows 成品：Builds/Windows/Ember.exe。空白鍵暫停，1/2/3 切換速度，方向鍵環繞鏡頭。
HUD 可查看角色、遺書、歷史与存讀檔。預設自主選職；Ember.exe --showcase 可在首輪展示四種職業。

重新建置：powershell -ExecutionPolicy Bypass -File Tools/build.ps1
只跑測試：powershell -ExecutionPolicy Bypass -File Tools/build.ps1 -TestsOnly
實際畫面驗證：powershell -ExecutionPolicy Bypass -File Tools/playtest.ps1 -Visual
坍塌畫面情境：powershell -ExecutionPolicy Bypass -File Tools/playtest.ps1 -Visual -Collapse
Blender 原創面具：blender --background --python Tools/create_mask.py

smoke 使用 Artifacts 內獨立存檔，不覆蓋一般遊戲存檔。一般存檔位於 LocalLow/WitnessWorks/Ember - The Witness Tower。

詳細實測與限制請見 Artifacts/VALIDATION.md。

遊戲預設繁體中文（zh-TW），右下角語言按鈕可即時切換英文。完整字串盤點見 Docs/LOCALIZATION_INVENTORY.md；語系、舊存檔及字型架構見 Docs/LOCALIZATION.md。
中文化畫面測試：powershell -ExecutionPolicy Bypass -File Tools/localization_playtest.ps1
較小視窗測試：powershell -ExecutionPolicy Bypass -File Tools/localization_playtest.ps1 -Width 1280 -Height 720
