# Phase 3.1 — Stabilization / Balance / Performance Gate

開始日期：2026-10-03。工作目錄 `F:\CodeX開發小東東\Agent RPG`，基線 `43be33a`。本輪只處理既有 1–10F、AI、核心穩定性、UI 與效能；不製作 Phase 4 或 11–25F 新內容。

## 已完整讀取的證據

PHASE3_VALIDATION、PERFORMANCE、phase3-final-analysis、phase3-compositions、phase3-final-audit、phase3-visual-review、phase3-soak-relocated-120m-performance、phase3-runtime-manifest，另讀 native warning stacks 與相關 Runtime 原始碼。基線資料與原始 JSONL/圖片/log 全數保留，不能被本輪輸出覆寫。

| 問題 | 現有證據 | 根因假設（尚待量測） | 驗證方法 | 修正方向與验收 |
|---|---|---|---|---|
| 10F cliff | 9F 96.4% → 10F 72.2%，10F 4636 死亡事件、平均成功戰鬥 94.3 秒 | 持續熱壓、時間轉階段、跨階段 adds/shield/status 疊加、MP 抽取與幾何避險佔用恢復 | 原平衡版本 100 自然 seeds、五編成控制，逐階段 HP/MP、傷害來源、冷卻/恢復、死亡快照；相同種子消融 | 優先減少重疊與提供恢復窗口，不先降 HP；最終條件 Boss 擊敗率 80–90% 為診斷區間，10F 仍有較高時間/資源成本 |
| 編成失衡 | 四戰士 2/50、射手 1/50、法師 0/50、補師及混合各 50/50 抵達 11F | holy 每次攻擊附帶低成本治療；非補師消耗品/休息 AI 不足；裝備或打斷效率差異 | 每種編成 damage/taken/raw/effective/overheal/MP/potion/rest/regen/shield/revive、技能裝備與掉落打造資料；相同 config/seed 比較 | 非補師不新增完整自補；恢復來源可靠，支持職業有施法/資源代價。控制樣本非補師各至少 20% 抵達 11F 作警戒值，四補師不以無代價 100% 勝出；混合保有優勢，不能只用勝率判定 |
| AI 恢復與技能 | taunt 68、revive 8 次；ExecuteCombat 避險分支在喝藥前 return | 藥水被危險移動跳過；嘲諷輸出分數低/未配裝；復活只在死者存在時配裝，戰鬥中死亡來不及換裝 | 記錄技能 unlock/equipped/ready/MP 不足/實際施放機會；死亡時恢復可用但未使用；避險與護盾狀態行為對照 | 保留職業差異，喝藥在移動/避險前評估；召喚/護盾/轉階段重排優先級；用行為斷言及自然樣本驗證 |
| UI 像素缺漏 | 英文頂列、繁中 9F 卡片缺字，自動邊界/HasCharacter=0 issues | 動態 atlas、rich text、GUI matrix/scale、快取或語系切換中的渲染狀態 | 最小固定文字多字級/雙語/兩解析度重現，記錄 font texture rebuild 與逐幀截圖；AB 隔離，不先認定 atlas | 根因證據後修正；1600×900、1280×720 × zh-TW/en，主要介面與語言切換、9/10F人工檢視無已知缺漏 |
| JobTempAlloc | 6 runtime lifespan + 2 cleanup 訊息；48-byte native stack；退出 MemoryLeaks 150443 bytes | ParticleSystem 退出已有對照；runtime 可能是粒子、FrameTiming/ProfilerRecorder、存讀檔/重啟等其他路徑 | Development full stack，時間/事件紀錄；5–15 分鐘有上限的重現；先量測後逐項 no-particles/no-timing/no-save/no-telemetry/no-split/no-restart/no-injection | 優先零警告；否則必須有可重現的子系統根因、排除矩陣與明確 unresolved。不可只標成退出問題或假裝修好 |
| 幀時間與 GC | 16x 共享負載 frame p95 139.31/60.59/42.41 ms；GC 91–121 KB/取樣幀 | 模擬 tick、高階/戰術決策、UI formatting、診斷枚舉、同步 JSON/IO 或粒子 | 分離 Baseline 1x、Gameplay Stress、Extreme；逐幀 p50/p95/p99、GC/sec、phase/event markers、allocation counters；無並行 headless/Build | 固定 workload 前後比較，只修量測 hot path；baseline p95 ≤16.67 ms、p99 ≤33.33 ms 作 60 FPS 診斷門檻，記錄硬體/解析度/VSync與NA；不得用共享壓力值冒充基準 |

## Telemetry 2.0 與版本

新增結構化 enum/source id 的 Death Cause Taxonomy：BURST、ATTRITION、MANA_COLLAPSE、HAZARD、FAILED_DODGE、FAILED_INTERRUPT、NO_RECOVERY、AI_PRIORITY、COLLAPSE、OTHER。資料分類依呼叫來源與數值狀態，不解析翻譯文字；保留原文字供玩家顯示。

每次 death 保存來源、能力、phase/hazard、hit 前 HP、MP、治療/藥水/防禦 readiness、危险距離、救援意圖、隊伍人數。記錄階段 duration/taken/MP/potion/skill availability，以及 raw/effective/overheal、恢復来源、mana/loot/craft。舊資料缺少欄位標示 unavailable，不回填猜測值。

先以 `phase3-baseline` 平衡版本啟用診斷；gameplay 修改才遞增 balance version。schema 2 存檔維持可讀，新增診斷有預設值與上限。每個 stage 固定 seed/config、Runtime+Resources SHA-256、版本及 UTC timestamp；資料只合併同一 manifest/config，修改後保留前版。

## 執行順序與有界工作

1. 提交本計畫；補診斷與版本化 runner，不改平衡，100 seeds 與原五編成重現。
2. 證明 10F/恢復/技能使用根因，進行局部對照；小批調整及行為斷言。同步建立 UI 最小重現。
3. 在沒有 seed runner 同時執行時做 native 短測隔離及三種乾淨 benchmark；每個測試有期限（單次最多 15 分鐘）、進度/heartbeat/完成標記。native 短測不以無限 soak 找錯。
4. 修正後 100 → 500 → 1000 regression，驗證死亡/存讀檔/決定性、原有斷言、雙語與 Release。每階段失敗先處理，不直接升級樣本量。
5. 候選版本穩定才執行 5000 final telemetry；per-seed flush/checkpoint，可續跑，心跳包含 checkpoint/CPU/時間。不得中斷後從 seed 1 重跑完整樣本。
6. 小 commit 提交診斷、平衡、AI、UI、native、benchmark、已量測 GC 修正與測試；整理完整 Gate 報告、commit 清單，確認 clean 且無測試殘留。

## Gate 檢查

10F 曲線與編成具合理生存路径、補師仍具支援身份但非無代價 dominant；1000-seed crash/softlock/impossible/infinite/split deadlock/save corruption 各 0；固定版本/config/seed 決定性一致；combat/rest/split/before-boss/after-death/before-10F/next-life 存讀檔通過；UI 人工檢查無已知缺漏、雙語完整；native 零警告或可重現根因及 unresolved 記錄；乾淨 benchmark、幀尖峰與 GC 改善有量化證據；Windows Release 成功、工作目錄 clean。

若任何 Gate 未通過，停在 Phase 3.1，列出 blocker 與工具/時間/磁碟限制，等待使用者下一步；不得開始 Phase 4。最終文件分開呈現已完成測試、通過項目與 unresolved，不把部分樣本寫成全量通過。
