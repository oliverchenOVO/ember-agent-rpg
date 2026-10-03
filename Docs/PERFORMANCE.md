# Phase 3 效能與原生配置診斷

## Phase 3.1.1 收尾與資料範圍

最新資料為 EXP2.1-R1 Development，runtime `593eb2559149e06de985e51d08e50a138c1e4354bd2ed2dbfc2d2eb5b17412bf`、assembly `f7ba3899fa61a5c05a1d0666ce4b7414a89acb3fbb2aba5c74da23d0a1af9890`、H3 rules file SHA `311db79af73ee907ce39b6ae256cda570c6aa7c977b66af12006efe7710124ce`。本輪不重啟 120-minute soak，也不把下方舊版共享負載資料冒充新版。

R1 修正的是測試收尾期間仍觸發 Save/Load 的 observer 生命週期錯誤，沒有更改平衡 Core、字型或正常特效。原 EXP2.1 曾在十分鐘 baseline 出現一次 runtime JobTempAlloc；新版零警告窗口與原版警告均保留，尚未證明精確觸發條件。完整結論見 [Phase 3.1.1 驗證](PHASE3_1_1_VALIDATION.md)。

量測以 Stopwatch 逐幀 wall interval 為主，排除前十秒暖機與 cleanup 後的 frames；cleanup 仍保留在 raw CSV 和每個 ≥p99 frame 的事件分析。CPU/GPU counters 為不同取樣點，事件時間鄰近不等於因果。OFF 停止詳細 scope 與物件盤點，但 CSV、counters 仍啟用，只能估計這些額外診斷的增量，不能稱作零 observer 的產品 GC。Mono 區域 allocation API 校準失敗，欄位 NA；`Total Used Memory` 與 OS Working Set 都不能冒充 native-only memory。逐幀 phase 欄位缺失，checkpoint 的 Battle/Rest 只能證實離散覆蓋，不能算出連續分階段百分位。

### R1 完整 workload 數據

四組均正常退出、cleanup 完成、exception=0、JobTempAlloc=0。完整數值與 identity 見 `Artifacts/phase3_1_1_exp21_r1_performance-summary.json`／`performance-metrics.csv`／`workload-coverage.csv`（後兩者亦使用同一完整 prefix）。

| workload | CPU p50/p95/p99 ms | GPU p50/p95/p99 ms | wall p50/p95/p99 ms |
|---|---|---|---|
| baseline ON 10m | 2.135 / 3.077 / 3.984 | 0.846 / 1.524 / 1.861 | 6.053 / 6.464 / 6.796 |
| baseline OFF 10m | 2.344 / 3.759 / 5.611 | 0.729 / 0.926 / 1.772 | 6.055 / 6.745 / 8.447 |
| gameplay 10m | 3.340 / 5.362 / 6.897 | 0.668 / 1.315 / 1.763 | 6.059 / 6.911 / 8.351 |
| extreme 20m | 3.385 / 5.416 / 7.225 | 0.930 / 1.729 / 2.132 | 6.062 / 7.101 / 11.302 |

| workload | GC MB/s | GC bytes/frame mean | managed heap p50/max MiB | engine total-used max MiB |
|---|---:|---:|---|---:|
| baseline ON 10m | 7.904 | 48018.0 | 3.246 / 3.680 | 594.272 |
| baseline OFF 10m | 7.791 | 47591.4 | 3.238 / 3.684 | 594.844 |
| gameplay 10m | 7.916 | 48487.5 | 3.242 / 4.285 | 594.055 |
| extreme 20m | 11.047 | 68817.8 | 4.453 / 6.992 | 932.146 |

| workload | UI p50/p95/p99 ms | Step p50/p95/p99 ms | View p50/p95/p99 ms | localization render p50/p95/p99 ms |
|---|---|---|---|---|
| baseline ON 10m | 0.897 / 1.466 / 1.908 | 0.003 / 0.041 / 0.127 | 0.020 / 0.031 / 0.042 | 0.070 / 0.105 / 0.274 |
| baseline OFF 10m | NA | NA | NA | NA |
| gameplay 10m | 1.295 / 2.655 / 3.894 | 0.005 / 0.076 / 0.213 | 0.030 / 0.051 / 0.086 | 0.105 / 0.236 / 0.553 |
| extreme 20m | 1.278 / 2.402 / 3.493 | 0.112 / 0.418 / 1.183 | 0.051 / 0.095 / 0.141 | 0.088 / 0.172 / 0.306 |

| workload | particles peak | draw calls p50/p95/p99 | batches p50/p95/p99 |
|---|---:|---|---|
| baseline ON 10m | 36.0 | 325.000 / 797.000 / 845.000 | 325.000 / 797.000 / 845.000 |
| baseline OFF 10m | NA | 328.000 / 820.000 / 871.000 | 328.000 / 820.000 / 870.000 |
| gameplay 10m | 36.0 | 304.000 / 702.000 / 842.000 | 304.000 / 702.000 / 841.000 |
| extreme 20m | 32.0 | 775.000 / 1172.000 / 1447.000 | 775.000 / 1171.000 / 1443.000 |

ON／OFF 差異約 0.113 MB/s（相對 OFF 1.45%）；單次比較沒有充分隔離全 observer 成本，也不能將 CPU／wall 差異歸因於唯一開關。GC 仍持續配置，未證實產品 GC 改善。

gameplay 有自然分隊、兩組並行、9 次 Save/Load；extreme 有 9 輪、最高 25F、19 次 Save/Load／telemetry flush。分鐘 checkpoint 有 Battle／Rest，gameplay 另有 Ended；逐幀沒有 phase，因此不能計算連續分階段百分位。300／600／900 秒附近有死亡／重啟證據，但沒有專用注入 marker，逐次故障注入歸因仍 NOT VERIFIED。

全部 ≥p99 spikes 分別為 ON 974／OFF 966／gameplay 967／extreme 1,911 列，包含 cleanup；記錄 UI／Step／View／localization cost 及事件時間窗。最大非 cleanup wall spike 分別 198.932／987.719／201.145／1,485.112 ms。extreme 最大值附近同時有 VFX、IO、telemetry、inventory；單靠相鄰時間不能宣稱因果。CSV 的 save_ms 會保留前次值，不能作每幀存檔成本。

**Performance Gate NOT VERIFIED；Native 精確根因未證明。本輪停止測試，整體平衡 Gate FAIL，不進入 Phase 4。**

下方內容保留為 Phase 3／3.1 歷史紀錄；其當時的 Build／符號狀態不代表 Phase 3.1.1 的最新狀態。

Phase 3.1 新增乾淨 300 秒 1x baseline（CPU p95 6.455 ms、p99 8.168 ms）及逐幀 wall interval／事件／配置量隔離工具。新版 Build 停滯，三 workload 前後測與 GC 改善仍未完成；Gate 未通過。限制見 [Phase 3.1 驗證](PHASE3_1_VALIDATION.md)，不要把下面舊共享負載 soak 當成正常遊玩基準。

2026-10-03，在 F 槽完成 **120 分鐘實際渲染壓力測試**。Windows / Unity 6000.2.0f1 / D3D12 / RTX 3070 Ti Laptop（8 GB）。Development Player 使用凍結 Runtime，16 倍速、分鐘存讀檔、輪替觀察、五分鐘死亡/坍塌；同機同時執行 headless seed runner，前段也有 Release 建置/畫面 QA。這不是單獨的一倍速效能基準，數字不能直接當作正常遊玩幀率。

## 完整量測

complete marker：`elapsed=7200.005; rows=6975; runs=60; maintenance=119`。session 起始時間、30/60/90 分鐘存檔、frames.csv 及 Player log 保存於 `Artifacts/Phase3/soak-relocated-120m`。原始資料不納入 Git，搬移時必須保留。有效渲染最後秒數、排除列數及完成標記見 `Artifacts/phase3-soak-relocated-120m-performance.md`；統計工具只接受物件≥100、draw calls>1 的取樣，排除暖機前 60 秒。counter 偶爾為 0/NA 不等於已證實黑畫面，共排除 36 筆無工作負載/無效 counter 列，不全是啟動或清理；最大有效取樣間隔 22.17 秒，不能宣稱逐幀連續無缺畫面。另保存 coverage 稽核。

| 實際窗口 | 取樣數 | Frame p95 ms | CPU p95 ms | GPU counter p95 ms | GPU FrameTiming p95 ms | GC 平均 bytes/取樣幀 | Used Memory 首→末 MiB |
|---|---:|---:|---:|---:|---:|---:|---|
| 1–30 分 | 1655 | 139.31 | 86.98 | 3.00 | 3.22 | 120878 | 986.9→1057.3 |
| 30–60 分 | 1746 | 60.59 | 17.68 | 5.39 | 5.53 | 102663 | 1058.4→1055.9 |
| 60–120 分 | 3479 | 42.41 | 19.74 | 4.09 | 4.64 | 91621 | 1057.3→1065.5 |

1 Hz 百分位描述取樣幀，不是所有幀。CPU counter 與 GPU FrameTiming 的擷取點不同，不能逐列相減推算瓶頸。NA 是無法取得，沒有改寫成 0；GPU counter 的 0 仍是原始 counter 值，另列 FrameTiming 作參照。ai_ms 包含整段 tick loop，save_ms/telemetry_ms 保留最近一次維護成本，不是每一幀都執行存檔。更完整均值、最大值與資源/IO 數據保存在 JSON 摘要。

資源物件有上限，但 GC 仍持續配置；Used Memory 是引擎總 used counter，不全是 native、也不是 OS Working Set。暖機之後約 1 GiB 的平台不能證明零慢速洩漏。CPU/frame p95 尖峰尚未達到穩定低延遲目標；後續需要固定場景、同速、獨立負載與 allocation call stack，比較才可宣稱優化比例。

![完整渲染資料](Images/phase3-performance.png)

## 執行期間警告仍未解決

本次完整 log 共 **8 次 JobTempAlloc 警告**，退出前已確定至少 6 次。native allocation diagnostic 輸出包含 48-byte allocation 與 UnityPlayer 位址，但未取得引擎完整符號/精確配置呼叫點。原始 stack 及退出前後觀察保存於 `Artifacts/phase3-jobtemp-longrun.txt`。退出清理另有 remaining-allocation 警告，引擎 MemoryLeaks 診斷 allocatedMemory=150443 bytes；未歸因到專案的精確呼叫點。log 缺少逐警告 wall-clock timestamp；frameIndex/age 不能用來推定警告發生在第幾分鐘。

下面的最小 ParticleSystem 對照只證實可重現的退出路徑與 workaround，**不能解釋所有長測 runtime 警告或宣稱全部解決**。未升級 Unity，也未擅自移除所有粒子。完整長測執行完成，不等於原生配置驗證無警告通過。

## 已採取的措施

- 觀測狀態重用，減少完整 JSON 複製。
- 角色特效最多 64 個；五種工坊 Boss / 場景快取；控制區最多 8 個。
- 自建 Mesh、Material、AudioClip 的生命週期明確；關閉時清空粒子、銷毀世界與資產並等待清理。
- IMGUI 與高階決策有 profiler marker；長測使用實際渲染、分鐘存讀檔、輪替觀察及死亡/坍塌情境。

這些是有界資源與量測基礎，沒有完成完整 allocation call stack 分析，也沒有最終音畫內容效能保證。

## JobTempAlloc 隔離結果

專案搜尋沒有自行配置 NativeArray、NativeList、TempJob 或排程 JobHandle。最小 Player 對照：空場景、Camera、Camera + Mesh 各 0 警告；加入 ParticleSystem 後 Development 退出有 2 次警告。相同 Release 的兩次對照，未清理粒子均 1 次，StopEmittingAndClear、銷毀後等待兩秒均 0 次。正式 UI 測試兩種解析度退出均 0 次。原始計數見 `Artifacts/jobtempalloc-tests.txt`。

這足以將可重現路徑縮至 **Unity ParticleSystem 退出生命週期**，不是證明取得引擎內部精確配置呼叫點。[Unity UUM-113839](https://issuetracker.unity3d.com/issues/memory-leak-warnings-are-thrown-when-creating-a-particle-system-gameobject-2) 記錄 6000.2.0f1 的 ParticleSystem 洩漏警告，6000.2.6f1 修正，與隔離結果一致。未升級 Unity，也沒有聲稱修正封閉引擎本體；本專案提供明確清理 workaround，後續應升級並重跑對照與長測。舊診斷版本關閉仍有警告，文件保留失敗紀錄。


## 歷史診斷與後續

中間 Build `soak-120m` 實際約 31.4 分鐘，最後 Runtime 的先前短測 `soak-frozen-120m` 約 296 秒；這些原始 log/CSV 和摘要保留，沒有冒充完整時長。修改前 `before-pooling` 約 179 秒，情境不同，不能用 CPU 差值宣稱優化比例。舊 OnGUI 清理後 Camera 例外保留於歷史 log；退出流程後續以停止更新、清理、延後離開改善。

優先處理 runtime JobTempAlloc、UI 動態字型像素缺漏及尖峰；評估 Unity 已修正 patch 後再以相同 workload 對照。逐幀 GC stack、獨立單機 CPU/GPU benchmark 和正式美術音畫成本仍待驗證。本輪到此收尾，不啟動新的長測。
