# Phase 3 內容與重現

## 資料與程序模型

`Tools/write_phase2_content.py` 產生完整塔資料，再套用 `Tools/phase3_content.py` 的專屬 6–10 層。沿用原 Boss ID，改用專屬顯示名稱與 archetype，舊 schema 2 存檔可以續讀。資料描述 attack shape、長度、內圈、推移、抽取、順序、phase、移動方式、熱壓與難度向量；不是單純 Floor × constant。

`FoundryView.cs` 是模組組裝的 Unity 程序模型流程。梁、工作台、爐口、軌道、輪、吊臂、晶巢、鐘錘與天球軌環依組裝拓樸區分五個 Boss。Ring 建立 Mesh，Part 使用共享 Unity primitive Mesh；模型與場景依身份快取，特效有上限的重用池。WorldView 擁有並釋放自己建立的 Mesh/Material。現有 Blender 原創面具流程保留在 `Tools/create_mask.py`；Phase 3 新工坊使用 Unity 模組，沒有聲稱已製作 Blender 動畫或外部專業美術。

後續擴充：新增資料與兩種語系字串，組裝不同拓樸；新增 shape/mechanic 必須同時支援 CombatCue、戰術避讓、地面顯示與測試。Cue 判定與畫面使用同一份位置/尺寸，不能只改 VFX。場景裝飾位於移動區外，不作為未建模的碰撞障礙。

## 戰鬥、灌注與掉落

24 個既有技能 ID 保留，Phase 3 執行器提供不同效果；正式 UI 的滑鼠提示顯示成本、冷卻與效果。灌注從持有武器決定施放原點，使用施放者法力與自己的技能冷卻，跨職業及近遠程組合可施放。換武器立即失去舊武器灌注，存檔保存附魔、灌注及效果。復活每位 Agent 每輪最多一次，會記錄死亡事件與遺書；完成資格仍要求通過 25 層且結算時存活。

Boss 工坊掉落保證一把當前職業可立即使用的武器，具有區域詞綴；其他掉落包含武器與恢復品。灼印/星核提供跨職業灌注。打造品質高於一般掉落，可指定自己的技能灌注；AI 同時估價詞綴、法力需求、職業適性與灌注。沒有新增數百種裝備或完整裝備 UI。

持續傷害按 dt 計算，避免每 0.1 秒都套用「最少一點傷害」而放大熱壓。超過 240 秒的戰鬥逐漸提高狂暴壓力，阻止純恢復策略無限拖延；這是明確的 prototype 輸出時限，不代表最終平衡。

## Telemetry

`TelemetryEnabled` 預設關閉。啟用後依 seed/run/group/floor 保存 encounter；休息層分割不建立假的戰鬥 encounter，而將事件歸入原戰鬥的記錄。每輪資料在下一輪重置，保存/讀取可延續本輪資料。

JSONL 提供逐筆記錄，分析工具另外產生 raw encounter CSV、逐層 CSV/JSON/Markdown、技能使用與職業/武器 exposure。run ID 是 seed:run。樓層 clear rate 依 seed 去重，不能把分隊當獨立 run。

傷害統計是實際 Boss HP 傷害（召喚物使用數量抽象，沒有獨立 HP）；治療是直接/持續恢復的有效量，復活由技能使用記錄辨識。survivors/gear quality 取戰鬥結束快照，死亡、掉落、製作及休息事件可在之後更新。同一把武器的 exposure 平均不能證明因果強度，還要參照控制組 composition trials。原基線 telemetry 最初也產生休息層 bookkeeping rows；基線沒有死亡，分析工具明確排除這些非 Clear 列，原始檔保留供稽核。

## 命令

```powershell
python Tools/write_phase2_content.py
python Tools/write_localization.py
# 原基線需使用 Tools/Fixtures/phase2-tower.json 與原基線引擎版本；不要以新技能冒充舊基線。
python Tools/analyze_telemetry.py final
powershell -ExecutionPolicy Bypass -File Tools/build.ps1
powershell -ExecutionPolicy Bypass -File Tools/phase3_playtest.ps1
powershell -ExecutionPolicy Bypass -File Tools/phase3_playtest.ps1 -Width 1280 -Height 720
powershell -ExecutionPolicy Bypass -File Tools/phase3_profile.ps1 -Seconds 7200 -Label soak -Development
python Tools/analyze_performance.py soak
```

Editor batch 方法：`Phase3Validation.Unit`、`Phase3Validation.Full`（原回歸、Phase 3 斷言、5000 seeds）、`Phase3Validation.Compositions`（五種四人編成各 50 seeds）；Development Build 用 `ProjectBuilder.BuildDevelopment`。診斷用 `BuildReleaseDiagnostics` 跳過測試，不能單獨當作正式驗證。最終 Windows Build 的 ProjectBuilder.Build 仍先執行既有完整回歸。

長程測試每分鐘存讀檔、輪流觀察分隊，每五分鐘加入一次死亡或坍塌情境。這是 rendering stress workload，不能拿來計算自然難度勝率。30/60/120 分鐘 checkpoint、1 Hz counters 與逐次 Player logs 都保存於 Artifacts/Phase3（大型原始資料不納入 Git）。

JobTempAlloc 對照用 `--native-diag camera-particles` 與 `camera-particles-cleanup`；後者停止/清空粒子、銷毀並等待兩秒再退出。正式退出流程先停止粒子、釋放自建資產，再等待一秒，包含一般關閉視窗。詳細原生隔離結果與限制見 PERFORMANCE.md。
