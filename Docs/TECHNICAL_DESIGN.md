# 技術設計
## 已確認環境
Unity 6000.2.0f1：D:/unity/6000.2.0f1/Editor/Unity.exe；Windows Mono Build 模組可用。
Blender 3.1.2；Git 2.54.0；Python 可用；.NET Runtime 8 有安裝但無 SDK。
工作目錄初始為空，無既有遠端。以本機 Git 建立里程碑，不建立外部帳號或購買資產。
## 架構
UnityProject/Assets/Scripts/Core：純資料、種子 RNG、事件、戰鬥、效用決策、周目與存檔。
Presentation：3D 世界、角色視覺、VFX、觀察者鏡頭、HUD 與音效；只讀 simulation state。
Editor：產生場景、執行驗證、Windows BuildPipeline。Resources/catalog.json：版本化內容。
固定 0.1 秒 simulation step，低層移動與避招在該步更新。高層每 1.5 秒及狀態轉換評估。
單位位置使用平面 x/z；3D 呈現與邏輯分離。首輪圓形開放場地不需要 NavMesh；複雜地圖再導入。
採 Built-in render pipeline，避免無需使用的套件與網路依賴。使用 PBR 材質、霧、陰影、程序化幾何與光效。
## 品質與限制
先證明單 Boss + 休息探索 + 周目結算迴圈；不宣稱完成 25 層或最終美術。
無外部 LLM 呼叫、憑證或費用。IBrain 提供可替換介面；LLM 未實作。
JsonUtility schema v1 存檔含 RNG 狀態、角色、關係、技能、庫存、事件、周目歷史；原子替換與備份。
發佈存檔位於 Application.persistentDataPath，與專案檔案分離。
參考：https://docs.unity.com/en-us/engine/6000.0/manual/unity-editor/command-line-arguments/editor
