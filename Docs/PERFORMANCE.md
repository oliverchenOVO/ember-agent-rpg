# Phase 3 效能與原生配置診斷

測試平台：Windows、Unity 6000.2.0f1、Direct3D 12、NVIDIA RTX 3070 Ti Laptop GPU（8 GB VRAM）。這裡僅報告已保存的實測資料，不把規劃時長當成測試時長。

## 實際時長與數據

`Artifacts/Phase3/soak-120m` 的名稱是原先目標，實際有渲染資料約 **31.4 分鐘**。這是最後微調之前的 Development Build。1–30 分鐘 1 Hz 取樣的 frame p95 12.12 ms、CPU p95 8.48 ms、GPU p95 1.54 ms；場景物件最多 801、Mesh 55、材質 42、音源 3。Used Memory 855.3 → 1056.4 MiB，30 分鐘後短窗口 1054.4 → 1053.4 MiB。原生用量暖機後增加，尚不能從這個時長證明完全沒有緩慢洩漏。

最後版本 `soak-frozen-120m` 實際約 **296.4 秒**，31.4 分鐘資料不能冒充最後版本長測。暖機後 frame p95 6.07 ms、CPU p95 5.28 ms、GPU p95 1.45 ms；物件最多 802、Mesh 56、材質 42、音源 3。退出清理後物件降至 3 的一筆，連同啟動資料排除。Player log 顯示正常關閉流程，不能把該筆當成場景運作期間的空畫面。

**60 / 120 分鐘驗證尚未完成。** 使用者要求結束本輪並準備搬移專案，磁碟空間不足，因此沒有再次啟動長測。

完整表見 `Artifacts/phase3-soak-120m-performance.md` 與 `Artifacts/phase3-soak-frozen-120m-performance.md`。窗口標籤代表取樣區間；是否達到完整時長須看 last rendered elapsed 與 complete 欄位。1 Hz 百分位不是逐幀 percentile。GC mean 約 63–73 KB / sampled frame，IMGUI 與診斷 CSV/資源枚舉仍有配置；不是零 GC 宣告。ai_ms 包含整段 simulation tick loop，save_ms / telemetry_ms 保留最近一次維護成本，不是每幀持續成本。無法取得的 counter 為 NA。

## 已採取的措施

- 觀測狀態重用，減少完整 JSON 複製。
- 角色特效最多 64 個；五種工坊 Boss / 場景快取；控制區最多 8 個。
- 自建 Mesh、Material、AudioClip 的生命週期明確；關閉時清空粒子、銷毀世界與資產並等待清理。
- IMGUI 與高階決策有 profiler marker；長測使用實際渲染、分鐘存讀檔、輪替觀察及死亡/坍塌情境。

這些是有界資源與量測基礎，沒有完成完整 allocation call stack 分析，也沒有最終音畫內容效能保證。

## JobTempAlloc 隔離結果

專案搜尋沒有自行配置 NativeArray、NativeList、TempJob 或排程 JobHandle。最小 Player 對照：空場景、Camera、Camera + Mesh 各 0 警告；加入 ParticleSystem 後 Development 退出有 2 次警告。相同 Release 的兩次對照，未清理粒子均 1 次，StopEmittingAndClear、銷毀後等待兩秒均 0 次。正式 UI 測試兩種解析度退出均 0 次。原始計數見 `Artifacts/jobtempalloc-tests.txt`。

這足以將可重現路徑縮至 **Unity ParticleSystem 退出生命週期**，不是證明取得引擎內部精確配置呼叫點。[Unity UUM-113839](https://issuetracker.unity3d.com/issues/memory-leak-warnings-are-thrown-when-creating-a-particle-system-gameobject-2) 記錄 6000.2.0f1 的 ParticleSystem 洩漏警告，6000.2.6f1 修正，與隔離結果一致。未升級 Unity，也沒有聲稱修正封閉引擎本體；本專案提供明確清理 workaround，後續應升級並重跑對照與長測。舊診斷版本關閉仍有警告，文件保留失敗紀錄。

## 搬移後待補

最後版本的 30 / 60 / 120 分鐘完整渲染長測、JobTempAlloc runtime 與退出檢查、逐幀 GC call stack、單機不與 Editor 回歸競爭的獨立 CPU/GPU 比較。先重跑既有命令，避免以 headless 模擬取代 rendering profile。

## 短程修改前參考

`before-pooling` 有 179 秒有效渲染；暖機後 frame p95 6.07 ms、CPU 3.41 ms、GPU 0.82 ms、GC 約 50.6 KB / sampled frame。它的樓層/情境與最後版本不同，不能用兩者 CPU 差值宣稱優化比例。退出曾有 OnGUI 對已清理 Camera 的例外，後續以停用遊戲更新再清理的退出流程處理；原始失敗 log 保留。後續應建立固定樓層、固定鏡頭與相同硬體負載的比較。
