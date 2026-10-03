# 持續扣血可讀性與遺書選取

日期：2026-10-04。基線：c01a6b4。專案：F:\CodeX開發小東東\Agent RPG。

BossRuntime 原本就會在沒有招式預警時造成傷害：ambientPressure 每步扣血；開戰超過 240 秒另有遞增耗損；灼燒每秒扣血；召喚物攻擊第一名存活且未撤離的隊員；殘留危險區在半徑 3 內每秒造成 7 點減傷前傷害。這些持續來源不會新增單次施法動畫事件。第 8 層的基礎場地壓力為每秒 4.8，超過該階段狂暴時間後提高，恢復空檔降至 20%。不能僅憑使用者截圖判定當時每一筆傷害來源。

新增只讀的「持續威脅」面板，顯示選中角色的有效來源與減傷前數值。合併多個灼燒的當前威力，顯示最長剩餘時間；死亡倒數顯示最早觸發時間。召喚物只對實際被選中的第一名存活隊員顯示。場地壓力以 Boss 周圍持續脈動的紫色光環表現；灼燒以角色附近紅色活動標記表現。這些標記不是可閃避招式預警，不改變原傷害、AI、數值或隨機序列。

底部死者之書原本固定讀取 world.book 最後一筆，因此選中別的 Agent 仍顯示上一名死者。改為依 selected 篩選作者，預設最新一篇，有多篇可用「較新一篇／較舊一篇」翻頁；無該角色遺書時明確顯示空資料，不借用他人遺書。完整視窗增加四角色篩選與「全部角色」，開啟時預設選中角色。

## 驗證紀錄

最終 `readability-build-pair-final`：Development／Release Build 成功，exit 0；整個流程 221.582 秒（含雙語驗證和兩份 Build，非單次純 Build）。兩種語言各 612 鍵，879 種所需字元覆蓋，placeholder parity 通過。

| Player 驗證 | 語言／解析度 | 情境 | 結果 |
| --- | --- | --- | --- |
| Development 新情境 | zh-TW／1600×900 | 12 | exit 0、issues 0 |
| Development 新情境 | en／1280×720 | 12 | exit 0、issues 0 |
| Release 新情境 | zh-TW／1600×900 | 12 | exit 0、issues 0 |
| Release 既有資料視窗 | zh-TW／1280×720 | 13 | exit 0、issues 0 |

共 49 張截圖，四次完整 Player log 無例外或 JobTempAlloc 警告。人工抽查綜合威脅、LYRA 遺書、SERA 空資料等畫面，無方框字、文字溢出或按鈕截斷。

無施法的受控 Boss Tick 實際讓角色掉血，且沒有增加 casts 或 telegraph；場地壓力光環可見。持續威脅資料讀取前後模擬 JSON 相同；超時來源依目前階段與經過時間檢查。遺書情境涵蓋 KAEL 兩篇、LYRA 一篇、SERA 無遺書、較舊篇、角色篩選與全部作者。既有完整遺書、Boss 資料與四職業技能樹回歸通過。

Development assembly SHA256：`66750a1930c48181792e4f8712bd60b58ba80b5aca6cd8eb462286c87852fd11`。
Release assembly SHA256：`e98341a273b9662203670243e9cf26946030ab4b14a387c11468de8c076e622f`。所有 Player 測試的 assembly hash 與出貨檔案一致。

首次 `readability-build-pair-1` 在 UnityLinker 發生原生 access violation（exit -1073741819），沒有產出完整候選。原始失敗紀錄保留；重試前修正 QA 對狂暴壓力遞增的期望公式，最終重新雙 Build 通過。沒有刪除 Library 或測試 artifacts。

資料：`Artifacts/combat-readability-build-manifest.json`、`Artifacts/combat-readability-validation.json`；原始 logs 和圖片位於各 `Artifacts/Phase311/readability-*`。代表畫面及聯絡表位於 `Docs/Media/CombatReadability`。重建分析：`python Tools/analyze-combat-readability.py`。

新版執行檔：`Builds/CombatReadability/Windows/Ember.exe`（Release）與 `Builds/CombatReadability/Development/Ember.exe`。使用者原有 Player 保留，QA 使用獨立 artifacts 與測試存檔；允許其他 Player 共存，因此不作效能認證。相機與背包功能沿用上一版。

受控 QA 使用無招式、灼燒、超時、召喚物、危險地面及多作者遺書等情境，不代表自然遊玩的頻率。未啟動 5000-seed 或 120-minute soak；舊 Balance Gate FAIL 維持。
