# Phase 3.1 驗證與 Gate 報告

日期：2026-10-03；工作目錄 `F:\CodeX開發小東東\Agent RPG`；基線 `43be33a`。

**Gate 未通過；不建議進入 Phase 4。** 本輪保留診斷、候選修正與完整樣本；不能把候選版稱為正式通過版。

## 1. Summary

完成結構化死亡／恢復 telemetry、版本化可續跑 runner、原版與候選各 100 自然 seeds、原版與候選各五編成 × 50 seeds，共 700 個完成樣本。資料按 runtime/config 分開，不能合併成同版本 700-seed regression。

通過診斷一致性 7 assertions、Phase 3 技能 104 assertions、雙語 405 keys／700 glyphs 檢查、七種存讀檔重播。候選自然 10F 96% 超過目標，UI 缺字及 runtime 原生 warning 的完整觸發條件仍未證明。舊 5000 與 120 分鐘測試不計入本輪通過項目。

## 2–3. 10F 原因與前後比較

舊 5000 全量 9F 96.4%、10F 72.2%。本輪 seeds 1–100：原版 10F 78/99（78.8%）、候選 96/100（96%）。小樣本不能取代全量結論；遭遇數、條件 Boss 通關率與 run 抵達率分開。

原版 P1/P2/P3 死亡次數為 0/27/49；76 次死亡全部 MP <20%，沒有 HP 藥水或可用治療候選。主要最後一擊是 Pressure：P2 25 Pressure、2 MP 抽取能力；P3 49 Pressure。這支持持續熱壓與 MP 耗竭疊加的診斷，不能據此宣稱死亡時仍有藥卻不喝，也不能以最後一擊單獨證明完整因果。

候選保留 Boss HP、補師直接治療量：10F 轉階段增加 5 秒低壓恢復窗口及 MP 回復，減少 MP 抽取；移動前使用有限藥水、保留 MP、使用既有防禦技能與診療所有限補給。P2 死亡降至 0，P3 25 次，但這些同時改動可能過強，未完成消融前不判通過。

逐階段 CSV：`Artifacts/phase3_1_floor10_breakdown.csv`。RestRisk 從戰鬥傷害總量排除、另列 `rest_damage_excluded`；ready 的分母是 agent-seconds，不代表已確認施法距離或有效救援目標。

## 4. 固定編成對照

| 編成 | 原版抵達 11F | 候選抵達 11F | 原版／候選平均 MP 消耗 | 原版／候選藥水總數 |
|---|---:|---:|---:|---:|
| 戰士 | 2/50 | 49/50 | 2748.9 / 3092.9 | 973 / 2418 |
| 弓箭手 | 1/50 | 24/50 | 2748.9 / 3243.9 | 938 / 2798 |
| 法師 | 0/50 | 48/50 | 3495.5 / 4798.0 | 907 / 1827 |
| 補師 | 50/50 | 50/50 | 3444.0 / 3397.5 | 91 / 67 |
| 混合 | 50/50 | 50/50 | 3202.5 / 3285.5 | 384 / 1332 |

固定 seeds 1–50、showcase、floorLimit=10，全部 0 nonterminal。50/50 不代表真實勝率必定 100%。非補師已有生存路徑，補師與混合仍滿樣本通關，最終資源代價與平衡未建立。自然 25F run 的總量不能直接比較固定 10F 編成總量。

詳見 `Artifacts/phase3_1_composition-analysis.csv` 與對應 MD。

## 5. 恢復經濟

原版四補師 HolyAttack：raw 343522、effective 215868、overheal 127654，約占技能有效治療 77.7%；一般 Heal 4400 次、有效 18829、過量 326268（94.5%）。候選 Heal 1412 次、有效 21876、過量 93217（81.0%），仍需改善；HolyAttack 有效 217951，仍是主要續航來源。不能只因施放次數下降就判定 AI 通過。

非補師依賴有限 HP/MP 藥水、床與診療所；法師使用既有護盾，戰士使用既有防禦。候選補給額外消耗 1 材料，加上既有診療所材料／行動時間，單次補最多各 1 瓶至庫存 2，限定 1–10F，未提供全職永久自補。

CSV 分列 raw/effective/overheal、床／診療所／regen、實際盾吸收、打造／掉落、武器遭遇暴露。`recovery.mana` 尚未接上逐來源 MP，0 表示未收集，不能當作免費恢復；角色總量 `mpSpent/mpDrained` 已實際量測。逐來源 MP 及完整掉落／打造依賴因果仍缺失。

seed 1 屬性快照顯示射手／法師在受傷後偏向 Vit，而補師 Wis 持續成長；這是單種子診斷，未泛化也未據此修改屬性 AI。

## 6. AI 決策

原版 taunt/revive 已解鎖許多秒，但配裝與 ready 秒數為 0，直接證明配裝阻礙使用。候選保留職業的 taunt/guard/shield/revive：戰士 taunt 3502 次、混合 693 次；revive 仍 0。四補師及混合各發生 2 次死亡，仍缺有效目標／距離／存活施法者的完整機會追蹤，不能宣稱復活 AI 已驗證。

候選讓移動與避險前評估藥水、避免盾／adds 下昂貴爆發、保留 12% MP，施法前重查缺血量。一般治療仍 81% 過量，列為未完成項目。

## 7. UI glyph

405 keys、700 glyphs 雙語表／HasCharacter 檢查通過。實際 UI 使用 Noto Sans CJK TC 的 IMGUI，沒有 TMP UI，TMP Auto Size/fallback 不適用；字型包含字元不代表像素一定正確。

原版 1280×720 可見 Player 的 20 個 Theme B 截圖未再次重現已知缺字；隱藏 Player 黑圖無效。第一個獨立 font probe 沒有 Camera 清除 buffer，文字疊影屬測試工具錯誤，已判無效並補 Camera。`Font.textureRebuilt` 只追蹤該 API，不能據零事件排除所有文字後端 atlas。

未證明缺字是 atlas、rich text、matrix 或快取問題，沒有提交臆測修復。加入固定 QA 語系 override、Holy 成本 tooltip fixture；新版 1280×720 / 1600×900 × zh-TW / en 全介面人工矩陣未完成，是 blocker。

## 8. JobTempAlloc

比對相同 UnityPlayer DLL SHA-256、RSDS/PDB、PE UnityMain export，解析舊 17 層呼叫鏈至 `ParticleSystemGeometryJob::ScheduleJobs` → GfxDevice shared geometry jobs。配置來源已定位至粒子幾何排程；runtime 的確切觸發條件仍未證明。

退出 block age 5–15 expired、runtime block age 0/3 state OK，不能混稱所有警告都是退出問題。詳見 `Artifacts/jobtempalloc-investigation.md`。

ProfilerRecorder 增加完成與 OnDisable 釋放；`--mesh-embers` 是選用隔離實驗，預設保留原有粒子，沒有以更換外觀冒充已修復 runtime warning。新版 5–15 分鐘隔離與功能排除矩陣未完成。

## 9–12. 效能、尖峰與 GC

乾淨原版 baseline：1x、1600×900、D3D12、RTX 3070 Ti Laptop、Development，300 秒、38151 逐幀資料，無同期 headless/Build、無故障注入，maintenance=0。

前 10 秒暖機後：CPU p50 3.792、p95 6.455、p99 8.168 ms；GPU p95 約 1.58 ms，UI p95 約 3.50 ms，GC p95 約 52910 bytes/frame，約 5.91 MB/sec（十進位、含診斷工具）。這是目前 baseline，不是已改善的數值。逐幀資料和完成標記保留在 `Artifacts/Phase31/performance-before-baseline`。

舊 `frame_ms` 使用 Unity unscaledDeltaTime，出現 10–37 秒離群值，但相鄰 realtime 僅 47–127 ms，不能當作真正停頓。本輪工具改用 Stopwatch wall interval，同存 unity_delta_ms／realtime_gap_ms，加入樓層／轉階段／存讀檔／分隊／資源盤點事件。

增加 UI / simulation / view / Loc.Render 的配置量隔離，尚未取得新 Player 對照；沒有實作或宣稱未量測的 GC hot path 優化。新版 baseline／gameplay stress／extreme、尖峰事件關聯與前後 GC/sec 改善未完成，不能通過效能 Gate。

`Artifacts/phase3_1_performance-summary.json` 的舊 frame_ms 分布與 `phase3_1_frame-spikes.csv` 只保存原始觀測，不能當 wall interval 使用。CPU/GPU counter 0/NA 分列，不冒充有效樣本。

## 13–14. Seeds 與測試

- 原版自然 100：56 clear / 44 wipe / 0 nonterminal，640.62 秒；結局與舊相同種子資料一致。
- 候選自然 100：55 clear / 45 wipe / 0 nonterminal，710.82 秒；25F clear 並非 10F clear。
- 原版與候選固定五隊各 50：共 500；全部完成，0 nonterminal。
- 診斷 7、Phase 3 技能 104、localization 405 keys / 700 glyphs 通過。
- 七種 save replay 通過：combat/rest/split/before-boss/after-death/before-10/next-life，各重播 300 ticks 比對 world 與 groups；舊 schema 2 fixture 可讀。
- 500 tuning 消融未完成：第一次前 Editor 尚關閉，撞到專案鎖；第二次 ILPP connectivity 停滯，均 0 seeds。完整 logs 保留，不計入樣本。
- 新版 1000 regression、5000 final、本輪 120-minute soak 未完成／未啟動；不得宣稱 crash/softlock/impossible battle/infinite loop/split deadlock/save corruption 全量為零。
- 每個完整 run 有 manifest runtime/config SHA、版本、UTC、原始 JSONL SHA、checkpoint/heartbeat。resume 拒絕 hash 改變、非連續前綴，不覆寫重跑。
- runtimeHash 範圍為 Core + Resources JSON，不含 Presentation/Editor；不能以此當作 Player assembly hash。完整版本矩陣見 `Artifacts/phase3_1_runtime-manifest.csv`。

## 15. Git

`c33f55d` 計畫；`b29bf81` 結構化診斷與 runner；`f09fa6c` 候選平衡；`1ed29cd` 存讀檔重播、UI/native/profile 隔離工具；`b5b2a52` cohort 分析與 native 呼叫鏈證據。文件收尾 commit 以 `git log` 為準。候選沒有標成正式通過版。

產生器 parity 再檢查：405 keys 的中英內容與已存 JSON 完全一致，沒有改動 runtime 資產；PowerShell wrapper 語法解析通過。Unity source 最後一次成功編譯是 ShortGate；後续 WorldView 的 opt-in flag 改名與 probe 整理未經新 Player Build。不能用 Editor assertions 冒充 Windows 實機新版驗證。

## 16–18. 限制、Gate 與 Phase 4

**Gate 未通過，停在 Phase 3.1，不開始 Phase 4。** Blockers：10F 過度修正／消融未完成；補師恢復代價及 revive 機會追蹤不足；缺字根因與新版 UI 矩陣未完成；runtime 原生 warning 觸發條件／排除矩陣未完成；三 workload 效能／GC 改善證據缺失；1000/5000 未完成。

Unity ILPP worker 多次 CPU 近 0 且未建立 IPC；Normal priority 曾伴隨恢復，後續仍停滯，不能稱為已證明修復。ShortGate 已寫完通過結果，Editor 關閉階段無進度，保留資料後終止。兩次消融異常都在模擬前。wrapper 有期限／CPU／log bytes 心跳且拒絕同專案 Editor 重疊。

最新 Development Build 的 Editor 在進入建置前也停滯，未產生新 Player；新版 Release 未完成。現有 exe 是舊已驗證版本，不能冒充本候選。磁碟已搬至 F 且空間充足；限制是工具停滯與尚未達 Gate，不是本輪磁碟容量。不再啟動新長測，整理 commit／clean 後等待使用者下一步。
