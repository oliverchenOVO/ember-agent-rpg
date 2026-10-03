# Phase 3.1 Native 配置調查（進行中）

舊 120 分鐘紀錄有 6 次 runtime lifespan warning 與 2 次退出警告。不能把 runtime 警告歸因於退出；每次舊 warning 的精確時間未被記錄，不能事後補造。

## 引擎函式定位

Unity 安裝附帶 `UnityPlayer_Win64_player_development_mono_x64.pdb`（60,821,504 bytes）。舊 Development DLL 與安裝目錄 DLL SHA-256 相同：`86a01ca0ccd81e70d7e14de57c039daf6f6e1bdc74ad9cafcde0cba74375fc75`。DLL RSDS 指向該 PDB，版本對應 6000.2.0f1。

舊 stack 的 `UnityMain` 位址為 `0x7ffb6c86a0ab`；PE export RVA `0x10fa0a0`，以載入模組 64 KiB 對齊推得 base `0x7ffb6b770000`，回查 PDB 為 `UnityMain + 0xb`，其餘 16 層也形成一致呼叫鏈。這是由舊 stack 推回 base；舊 process 已退出，無法再讀其 module table。

```
GeometryJobTasks::ScheduleSharedGeometryJobs + 0x2dd
GfxDevice::ScheduleSharedGeometryJobsInternal + 0x8c
GfxDeviceClient::ScheduleSharedGeometryJobsInternal + 0x256
ParticleSystemGeometryJob::ScheduleJobs + 0x1153
DispatchGeometryJobs + 0x92
Camera::CustomRender + 0x45d
Camera::Render + 0x53
RenderManager::RenderCameras + 0x4c6
PlayerRender + 0x2df
PostLateUpdateFinishFrameRenderingRegistrator::Forward + 0x52
ExecutePlayerLoop → PlayerLoop → PerformMainLoop
MainMessageLoop → UnityMainImpl → UnityMain
```

配置來源已定位至粒子幾何 job 的 shared geometry 排程。**確切是哪個引擎內部工作未釋放、以及觸發條件，仍須隔離重現；尚未宣稱修復。** 六個 runtime dump 的 48-byte block 當下 age=0/3、state=OK；退出 dump 則有 age=5–15、EXPIRED。警告與 block 描述不能混為一個確定的洩漏事件。

專案腳本搜尋沒有 NativeArray、NativeList、Allocator.Temp/TempJob、IJob、JobHandle。這排除專案自行建立的這些容器，不排除引擎配置。ProfilerRecorder 原先只在 OnDestroy 釋放；本輪量測工具將補 OnDisable 與正常完成前釋放，屬生命週期完善，不能據此宣稱解決粒子 allocation。

## 已有隔離證據與限制

Phase 3 保留的短測：empty / camera / camera+mesh 沒有警告；particle component 在退出時可重現，明確 Stop/Clear/Destroy 並等待後短測可消除退出警告。這些短測不涵蓋六次 runtime lifespan warning。新版 5–15 分鐘隔離測試、timestamp/event 對照、功能排除矩陣與結果待補。

原始 logs / symbols / base scan / 17 層解析輸出保留於 `Artifacts/Phase31/`；解析工具為 `Tools/symbolize_native.ps1`。base scan 只是探索，不能以任意位址解出的函式名稱當作實際 caller；最終鏈使用 PE export/PDB 相互驗證的 base。

## Phase 3.1 收尾狀態

新版 Development Build 的 Editor 在建置前停於 ILPP 連線，未產生新 Player，因此新版 5–15 分鐘隔離與功能排除矩陣未完成。保留既有 Development exe，但不以它冒充候選版本。乾淨原版 300 秒 baseline 未觀察到 JobTempAlloc，不能以短測無警告否定舊 runtime 六次警告。

新增 `--no-particles` 與 `--mesh-embers` 選用實驗，預設仍是原粒子。mesh 方案只供排除該引擎 job path，沒有宣稱外觀或效能驗證通過。ProfilerRecorder 正常完成前與 OnDisable 釋放只屬生命週期補強，不是配置來源的已證明修復。完整 Gate 狀態見 `Docs/PHASE3_1_VALIDATION.md`。

## Phase 3.1.1 新版重現

EXP2.1 runtime `593eb2559149e06de985e51d08e50a138c1e4354bd2ed2dbfc2d2eb5b17412bf`、Development assembly `14091e0cfd350761df20a117a62db9bb3692b30057370fd935d098b991de9363`，正常粒子／1x／無 headless／無故障注入的 10-minute baseline 已完成。執行中觀察到一次 lifespan warning，log 成長在 watchdog process elapsed 364.48–394.90 秒之間；沒有引擎精確時間戳，不把此範圍補造成精確 event time。complete marker 601.008 秒、91,019 frames，正常退出。

新 UnityPlayer DLL SHA 仍為 `86a01ca0ccd81e70d7e14de57c039daf6f6e1bdc74ad9cafcde0cba74375fc75`。以同一 PE export RVA `0x10fa0a0` 與 stack `UnityMain+0xb` 推得本次 ASLR base `0x7ffb765e0000`，解析 33 個唯一位址。48-byte stack 和上面的 ParticleSystem geometry 鏈一致；兩筆 128-byte stack 另外包含 profiler callback vector grow、GfxDevice async job、RenderShadowMaps 與 D3D12 ExecuteCommandBuffersAsync。allocation dump 的 age=0/state=OK，不能把每筆 dump 都稱為超齡泄漏。

證據在 `Artifacts/Phase311/exp21-baseline-on-native-A/player.log`、`native-symbols.txt`、`process-heartbeat.txt`、`events.csv` 與逐幀 CSV。`Tools/symbolize_native.ps1` 新增任意 log 位址解析，仍要求獨立驗證的 module base。後續 Player wrapper 用 bounded log snapshot 記錄首次發現 warning 的 UTC／process elapsed，精度約一秒＋flush 延遲；不冒充 engine callback 時間。

五分鐘對照短於本次 warning 出現範圍；B/C 是預設 mesh embers 未啟用／不存在 Boss 專屬 ParticleSystem 的 no-op controls，不能作為 causal isolation。完整八組與新版效能／GC 結果以 `Docs/PHASE3_1_1_VALIDATION.md` 為準。本次重現證明先前五分鐘零警告不足以宣稱修復；Native Gate 仍須真正穩定的重現差異或正常特效 warning=0。
