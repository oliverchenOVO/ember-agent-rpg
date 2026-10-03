# 休息區變化與死亡來源彈幕修正

2026-10-03，工作目錄 `F:\CodeX開發小東東\Agent RPG`。

每次進入休息區以模擬 RNG 抽選設施。RestDefinition 可設定 `emptyChance`（預設 0.15）及 `facilityChance`（預設 0.65）；先抽完全空白的休息區，其餘情況逐項抽選，不保證床、診療所、鍛造台或任何其他設施。即使沒有設施，出口與坍塌仍正常運作。這是設施可用性變化，既有互動效果仍依設施定義執行。

GroupState 保存抽選標記及可用設施 ID，分隊複製同一結果，存讀檔不重抽。保留 schema 2，缺少新欄位的舊存檔維持原有完整配置，下一次進入休息區才抽選。技術 ID 沒有改名。Agent context／執行檢查與一般、工坊場景都使用相同可用性；新增診療所恢復選項，沒有床時仍可考慮現有診療所。缺席設施沒有模型或互動環，HUD 顯示設施數／「沒有可用設施」，兩種語言均透過 string table。

死亡角色彈幕的原因：WorldView 原先以累積 damage／healing 增加推測發射事件。既有 Burn、Poison、Zone、Summon 等效果可在來源死亡後繼續結算，因此造成遺體發射新特效的假象。現在新特效與技能 presentation cue 均要求來源存活、未撤離且位於觀察中的戰鬥分隊；池化特效記錄來源，死亡／撤離／切換分隊／離開戰鬥時清除其彈幕。既有持續效果仍正常結算，沒有用取消所有持續傷害來掩蓋畫面問題。

## 驗證結果

- 最新 Editor `rest-interaction-validation-final` 正常退出。64 個有界 layout fixtures：11 空白、52 部分、1 完整；驗證每筆新存檔保留結果、分隊保留結果、缺席鍛造不能工作／扣材料、缺少新欄位的真實舊 JSON 可讀取。這是功能 fixture，不是平衡 seed regression。
- 實際 Core fixture 證明來源死亡後既存 Burn 仍增加累積傷害，來源保持死亡。
- Phase 3 技能／診斷檢查、407 個 localization keys／701 glyphs，以及 combat／rest／split／before boss／after death／before 10F／next life 七種 Save/Load replay 均 PASS。
- Development 四組 `1280×720 / 1600×900 × zh-TW / en`：每組五場景（一般／工坊的零設施與兩設施，以及死亡法師），合計 20 screenshots；全組 layout issues=0、正常退出。人工檢視其中 14 張，涵蓋四組解析度／語言、兩種休息場景與死亡法師；未見新增文字缺字／截斷或缺席設施殘留。其餘六張未人工檢視，不宣稱全數 manual review。
- 四組實際 Player renderer assertions 均 PASS：活人增加傷害會建立特效，死亡清除特效，之後兩次死亡來源傷害增量均不建立新彈幕。每組 `dead-source-check.txt` 保存結果。
- Release 額外五場景 QA 正常退出、layout issues=0、同一 renderer assertion PASS；這五張額外截圖未人工檢視。
- Release 60 秒／1x 正常 gameplay smoke：61.028 秒含 cleanup、9,179 frames、正常退出，無例外／JobTempAlloc。時間到期前沒有 maintenance，不能冒充 Player Save/Load 長測。
- Development Build 29.887 秒，Release Build 47.818 秒，均成功。前兩次編譯錯誤的 log 仍保留，未覆寫成成功。

完整 source／出貨檔案 SHA 為 `Artifacts/rest-interaction-build-manifest.json`；raw logs／screenshots／fixture 結果在 `Artifacts/Phase311/rest-interaction-*`，大小與 SHA 索引為 `Artifacts/rest-interaction-evidence.csv`。新 runtime hash `23adc38f542cd126f291f6ab258a9e28fe025cbcf111d07299b18b0ffac73c55`。Development assembly `5f01fc62a787b9901ed49b72e61713a47b054df95e5e1698e1b048a07f50aa5a`；Release assembly `add63ff0338bf54cb74869820609f7bdad465f0ddc048a4fd95dde2d99938d8d`。

最新成品：`Builds/RestInteraction/Windows/Ember.exe`；Development：`Builds/RestInteraction/Development/Ember.exe`。沒有覆寫凍結的 Phase 3.1.1 成品／既有資料，沒有新長測或 500／1000／5000 批次。新增設施變化會影響恢復資源，舊版平衡數據不能當成新版校準；原 Phase 3.1.1 Balance Gate FAIL 維持，這份局部修正不宣稱整體 Gate 通過。
