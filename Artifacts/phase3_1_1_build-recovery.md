# Phase 3.1.1 Build Recovery

開始：2026-10-03。基線 d7a82ac；Unity 6000.2.0f1（eed1c594c913）。平衡先凍結，尚未執行新的 simulation。

## 操作前證據

沒有 Unity / Hub / ShaderCompiler / dotnet / ILPP / Bee / Burst / PackageManager 程序殘留；F 槽約 1.47 TB 可用，C 槽約 17.8 GB 可用。Packages 為 Unity built-in modules；沒有 Burst 與專案 PackageCache / obj。

Library/Bee 666 檔／43,305,674 bytes；ScriptAssemblies 4 檔／404,776 bytes；PackageManager 19,777 bytes；Temp 8 bytes 與零位元 UnityLockfile；Logs 271 bytes。沒有刪除整個 Library。

Bee 歷史 Player input/DAG 存有搬移前 C 槽絕對路徑。這是快取搬移風險的實際證據，尚不能單獨證明 ILPP hang 根因。先用現有快取做一次最小 compile 觀測；如停滯，再隔離最小編譯快取，保留 ShaderCache、資產 import cache 及全部舊 log。

重新完整讀取舊 log 後修正前輪判讀：`candidate1-build-development/editor.log` 末段實際已有 Csc success、Assembly-CSharp.dll 更新及成功 reload，編譯總時間 314.57 秒、Csc 約 8.71 秒。仍沒有 Player Build 成功。`short-gate.log` 後段也有正常退出訊息；wrapper 未取得 exit 0 仍不能算可靠退出。前輪「未進入編譯」敘述過於絕對，應改為長時間等待後曾完成編譯，但建置未完成／程序退出觀測不可靠。

## 新 watchdog

`Tools/watch-unity.ps1` 每 5 秒留 CPU delta、log growth、mtime、stage、Unity/ILPP PID、記憶體、priority 與 named-pipe 存在狀態；每 30 秒輸出。IDLE 120 秒先警示，連續 CPU/log/stage 都無進度 420 秒才 STALLED 並終止指定 process tree；整次有 timeout。IPC pipe 存在不是成功連線證明。保留 Editor.log、stdout、stderr、process metadata、samples 與 result。

CompileOnly 不產生 Player，檢查 assembly 已載入、import 完成且 catalog/content test 能啟動。必須連續三次 exit 0 + GATE marker，才進入 validation/Development Build。

新執行結果待補。原始資料存 `Artifacts/Phase311`，不覆寫前輪。

## 觀測結果

| Gate | PID | 結果 | elapsed 秒 |
|---|---:|---|---:|
| Compile 1（含 Csc/reload） | 51304 | exit 0 + marker | 5.61 |
| Compile 2（warm import） | 45292 | exit 0 + marker | 約 5.6 |
| Compile 3（修改 Editor comment，強制 Csc） | 15216 | exit 0 + marker | 5.61 |
| 原候選 Editor validation | 74624 | PASS | 約 9 |
| P3.1.1-A validation | 62596 | PASS | 11.39 |
| Development Build | 25652 | PASS | 23.28 |
| Release Build | 69936 | PASS | 27.02 |

沒有清除快取、升級 Unity 或更動 worker priority。ILPP 50344 等 worker 在 BelowNormal 下仍有 CPU、記憶體約 48 MB、named pipe PRESENT，編譯成功。`UnityAutoQuitter.exe` 位於 Unity 官方安裝的 Data/Tools，命令列指向 Editor 與 ILPP PID，是該工具鏈生命週期程序，沒有把它當成外部干擾程序。

前轮 ILPP 的精確停滯觸發條件這次沒有重現，因此 **root cause 未證明**。可驗證改善是 launch/watchdog：独立 stdout/stderr 檔案、保留父程序、同專案互斥、exit code + method marker 雙重判定、CPU/log/IPC 觀測及有界退出；不是宣稱已修復 Unity 引擎。沒有為求成功任意刪 Library。

Development smoke：25 個 Phase 2、21 個 Phase 3 fixture，自動 layout 0 issues、exit 0；另有 300.0065 秒正常 1x Player、48993 幀、maintenance 0、exit 0，JobTempAlloc 0。fixture 提供 1F/rest/9F/10F/save/load 與雙語切換，長度 smoke 是實際連續模擬，兩種證據分開。manual 四組矩陣仍待完成。

成品隔離於 `Builds/Phase311/Development` 與 `Builds/Phase311/Windows`，沒有覆寫舊 Phase 3 成品。完整 executable／managed／data 檔案 SHA 及來源 runtime SHA 記錄於 `Artifacts/phase3_1_1_build-manifest.json`。SOURCE runtime 為 `1ecb08e858c003475e6d84dfd9fc5583d9ca3be2dee480e47236b2c55294ac0f`；balance P3.1.1-A 只更名版本標記，尚未再調數值。
