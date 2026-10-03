# 死者之書、Boss 資料與技能樹驗證

日期：2026-10-03。工作目錄：`F:\CodeX開發小東東\Agent RPG`。基線 commit：`d4b32b6`。本次是有界功能修正；原 Phase 3.1.1 Balance Gate FAIL 維持，沒有以短測宣稱完整 Gate／5000 seeds／120-minute soak 通過。

## 問題與完成行為

原本 `TowerSimulation.Die` 固定寫入 `epitaph.death`；閱讀設施只取得同作者最後一筆文字。沒有招式證據，內容也未影響實際應對。

現在遺書包含輪迴、樓層、Boss／階段、職業／等級、武器／裝備技能、實際結局與本人見過的招式。語氣依六項個性中的最高值選擇冒險、好奇、同理、貪欲、忠誠或積極攻擊模板，與實際遭遇組合；這是可追溯的模板敘事，不是 LLM 自由生成。死亡原因沿用實際傷害事件。生還者在輪迴結束時也可留下見證。

招式只在該分隊戰鬥中、本人存活且未撤離、Boss 確實顯示預警時記錄。在 Boss 結算前記錄既有預警，結算後記錄新預警，避免致命招式的見證遺失。未見過的未來階段招式不會自動寫入；舊遺書沒有證據時保留原文，明確標示舊版資料。書仍最多 64 筆；角色每次讀書參考最多 8 筆，優先下一層相關 Boss，其次最近輪迴，同招式採排序中的第一筆來源，避免後續記錄覆蓋來源。

閱讀可取得其他作者的證據，保留 `BookOfDead` 來源、作者與信心值，加入本輪有限記憶及推理 context。真正換層時，Agent 依已讀過的招式，優先裝備自己已解鎖的護盾、淨化、清場或控制技能，保留最多四個裝備槽及基本輸出。已從遺書學過的招式，避開預警時使用 1.1 的邊界餘裕（原本 0.6）。不會免費解鎖技能；非生還者不會跳過讀書而自動繼承招式。休息區缺少死者之書時也不能假裝讀到資料。

新增可捲動的觀察資料視窗：

- **Boss 資料**：目前生命／最大生命、護甲、弱點／抗性、目前階段／狂暴／護盾／召喚物；全部階段條件、時間檢查；招式傷害、預警與間隔、形狀與範圍、鎖定規則、狀態及持續時間、耗魔／擊退、打斷窗口、持續危險區及應對建議。觀察者可看完整內容，每個招式另標示該 Agent 是否親眼見過／已讀過；這不會把完整資料直接授予 Agent。
- **技能樹**：四名角色與四職業可切換檢視，共 24 個實際節點。連線使用 catalog 前置關係；顯示解鎖、裝備、武器灌注、技能點、角色全部屬性。點選節點查看魔力、冷卻、距離、解鎖成本／前置、武器限制、目前屬性計算後的傷害／治療／護盾與次要效果。
- **死者之書**：底部顯示最新語氣摘要，「閱讀完整遺書」開啟所有保留記錄、招式數值與應對線索。舊資料與空書均有明確呈現。

視窗開啟時暫停，關閉或按 Esc 恢復開啟前的暫停狀態。技能節點只供觀察，不由玩家直接解鎖或改配置。

## 數值與語系

`CodexText.Power` 與實際 `SpecialSkill` 共用技能強度公式。護盾／防禦／附魔等使用實際效果數值，沒有把 catalog 的 power 一律誤當傷害。例如法師護盾為 `35 + 智力×2`、戰士守勢是 65% 防禦及 `15 + 體質` 護盾，補師全隊治療是技能強度×0.65。Boss 的基礎傷害與抗性／狂暴修正分開說明；吸取魔力顯示合計值。`Weaken` 如實顯示目前是狀態標記，沒有虛構額外減傷。

新增 129 組繁中／英文 string-table keys；全表 536 keys，兩語系占位符一致。遺書及記憶存語意 token，切換語言仍可翻譯。介面使用既有內嵌 Noto Sans CJK TC；819 個不同字元檢查均覆蓋。當前介面是 IMGUI，沒有宣稱不存在的 TextMeshPro fallback／Auto Size 測試。長內容以實際字型量測行高並捲動，節點數值分成兩行，視窗標題高度調整至 43。

## 驗證結果

- Editor `knowledge-persistence-validation-final` 正常退出，`KNOWLEDGE CODEX VALIDATION PASSED`。涵蓋個性／死因差異、撤離者不取得見證、遺書不包含未見招式、其他作者閱讀、來源保存、真實舊 JSON 缺少新欄位、招式證據與閱讀記憶存讀檔、實際 `NextFloor` 準備、四槽限制、法師護盾實際施放值、非生還者記憶有限及 24 個技能描述。
- 既有 104 項 Phase 3 技能／診斷檢查、7 項 Phase 3.1 檢查及七種 Save/Load replay（各 300 ticks）PASS。這是 fixture／重播驗證，不是平衡樣本。
- Development：`1280×720 / 1600×900 × zh-TW / en`，每組 13 情境，合計 52 screenshots，均正常退出；新視窗及既有 HUD 的量測／字元／按鈕檢查 issues=0。新捲動文字另檢查 glyph／高度／未展開 token，不把合法的捲動裁切誤報成 viewport 溢出。
- Release：額外 13 情境，issues=0、正常退出。合計 65 張截圖，人工檢視其中 15 張（Development 13、Release 2），涵蓋四職業、長遺書、Boss 上下內容、技能數值、復活、中毒、舊書／空書及四種解析度／語系組合；未見缺字、方框、非預期換行、控制項截斷或擠壓。其餘 50 張未人工檢視，不宣稱全數 manual review。捲動區邊界裁切是正常行為。
- Release 正常 gameplay smoke 設定 65 秒，含 cleanup 實際 `66.00974` 秒、10,370 frames、一次真實儲存／讀取 maintenance、正常退出；無 runtime exception／JobTempAlloc。這不是長期 soak 或效能認證。
- 最新 Development Build `27.042` 秒、Release Build `15.234` 秒，均成功。
- 第一版資料來源測試發現重複招式在閱讀時改寫作者來源，已以讀取去重固定來源優先順序。第一版兩組 Player QA 各回報 13 個標題高度不足；修正後上述五組均零問題。失敗 logs／screenshots 保留，沒有覆寫成成功。

## 成品、資料與重現

最新 Release：`Builds/KnowledgeCodex/Windows/Ember.exe`。Development：`Builds/KnowledgeCodex/Development/Ember.exe`。先前 RestInteraction／Phase311 成品均保留。

- Runtime source SHA：`733b3b381e923dd7316aac3d48104ff6deff527dd1345194e0609ecabad48300`（Core＋Resources；不含 Presentation）。
- Development assembly SHA：`1e9568319b89ea4e829d7e8d81f0dde09a5690928f6d00e8e766a4a3350dd6a1`。
- Release assembly SHA：`b25b9b4742563a751e3bd4cc23270d60015e8e718a7770f2d896001199f56a8a`。
- 全部出貨檔案 SHA：`Artifacts/knowledge-codex-build-manifest.json`。
- 分析與人工檢視清單：`Artifacts/knowledge-codex-summary.json`。
- Raw logs／screenshots／存檔／CSV：`Artifacts/Phase311/knowledge-*`；大小及 SHA 索引：`Artifacts/knowledge-codex-evidence.csv`。

僅重現有界檢查，可使用新 Label 執行：

```powershell
./Tools/watch-unity.ps1 -Method Ember.Editor.KnowledgeCodexValidation.Run -Label knowledge-check-new -TimeoutSeconds 180
./Tools/watch-player.ps1 -Label knowledge-ui-new -Executable Builds/KnowledgeCodex/Windows/Ember.exe -TimeoutSeconds 60 -Width 1600 -Height 900 -PlayerArgs @('--knowledge-qa','--qa-locale','zh-TW')
python Tools/analyze-knowledge-codex.py
```

沒有重跑 500／1000／5000-seed 或 120-minute soak，沒有新增 Phase 4／11–25F 內容。加入遺書準備與較早避開預警會改變戰鬥行為，舊平衡數據不能視為新版本結果；本次僅功能驗證 PASS，完整平衡驗證仍未完成。
