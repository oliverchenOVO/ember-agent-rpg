# 專案搬移

尚未指定目的地。本輪收尾時既有 Unity batch process 仍正常執行；後續 runner 已取消。先讓既有 process 結束，或明確授權終止，再搬移，避免寫入原始資料期間搬移。

## 必須保留

整個 `.git`、`UnityProject/Assets`（含所有 `.meta`、字型與授權）、`UnityProject/Packages`、`UnityProject/ProjectSettings`、`Tools`、`Docs`、`README.md`。`Artifacts` 的摘要納入 Git，但 `Artifacts/Phase3` 等原始 JSONL、Player log、profile CSV、截圖被忽略，**必須額外保留**，否則會失去驗證證據。

`Builds/Windows` 可保留方便立即執行；Builds 不是原始碼。`UnityProject/Library`、`Temp`、`obj`、`Logs` 是可重建快取，不是必須搬移資料。移除快取會令第一次開啟 Unity 重新匯入，耗時且需要新磁碟空間。尚未自動刪除任何資料。

## 流程

1. 完成本輪測試、文件與 Git commit，退出 Player / Editor。
2. 指定目的地完整路徑，確認可用空間及解析後路徑。
3. 複製上述必要資料與原始驗證證據，先保留原始專案。
4. 對照來源/目的地檔案數、大小與重要檔案 SHA-256，確認 `.git` 完整。
5. 在目的地執行 `git status`、開啟 UnityProject、重新 Build / Run。
6. 驗證成功後才清理原位置；原始碼搬移與刪除原始資料分開處理。

## 路徑與設定

Tools 以 `$PSScriptRoot` / `__file__` 推導專案根目錄，多數命令不依賴目前絕對路徑。Unity Editor 預設位於 `D:\unity\6000.2.0f1\Editor\Unity.exe`，必要時傳入 `Tools/build.ps1 -Unity`。Windows 存檔仍在 `%USERPROFILE%\AppData\LocalLow\WitnessWorks\Ember - The Witness Tower`，搬移專案不會搬移玩家存檔；可另外備份該資料夾。Codex 工作目錄與 Unity Hub 的專案清單要指向新位置。
