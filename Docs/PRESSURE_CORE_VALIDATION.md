# 持續扣血改為可摧毀壓力核心

日期：2026-10-04。基線：6d0fc3f。專案：F:\CodeX開發小東東\Agent RPG。

這輪修改實際玩法：移除 Boss 對全隊的 ambientPressure 持續傷害與 240 秒後按生命比例遞增的全場耗損。有 ambientPressure 的 Boss 改為三座可攻擊的場地核心，位置為 (-6, 3)、(6, 3)、(0, -4)。沒有基礎壓力的 Boss 不生成核心。

每座核心先等待 3／9／15 秒，再完整蓄能 3 秒（橘色圈），接著對半徑 3 內地面脈衝 5 秒（紅色圈）。圈外不受核心傷害。正常冷卻 12 秒，開戰 240 秒後新一輪冷卻縮為 6 秒；已進行中的預警與脈衝不會跳階段，因此完整週期由 20 秒變為 14 秒。傷害使用原 Boss 的基礎壓力值，狂暴 ×1.5，恢復空檔降至 20%，之後套用現有護甲／防禦／護盾。傷害按每步真正處於脈衝的時間積分，避免跨階段時漏算或多算。

核心生命為 clamp(60 + Boss HP ×0.004, 60, 140)。Agent 在預警與傷害區優先尋找安全位置，接著用普攻摧毀最近的核心；已有治療技能且有人低於 35% 生命時，原治療決策優先。摧毀後該核心在當場戰鬥永久停用，不重生；保留殘骸，移除它的範圍圈，並記錄事件和角色記憶。技能動畫使用實際核心目標，沒有把核心傷害混入對 Boss 的傷害統計。

模型為基座、支架、懸浮晶體與範圍圈。紅色內圈持續向外流動，實際傷害邊界保持半徑 3，不跟著動畫縮放。HUD 顯示三座核心的生命、冷卻／蓄能／脈衝／摧毀狀態與倒數。Boss 資料頁顯示新規則。灼燒、死亡倒數、召喚物及招式殘留危險區仍依既有機制運作，其持續威脅資訊沿用上一版；摧毀核心不會消除其他來源。

核心生命、階段與倒數加入 schema 2 的可選擴充 pressureVersion=1。讀取舊版資料時初始化三座具有完整首次預警的核心；新版讀取保留摧毀狀態與計時。保存技術 ID，不更名。舊施法事件缺少 pressureAttack 時不會被錯認為核心攻擊。BalanceVersion 改為 P3-PressureCores-1，不能把既有平衡數據當作新版結果。

## 驗證

21 項受控檢查：首次完整預警、預警無傷、脈衝傷害、圈外無傷、跨 240 秒不跳階段、跨階段傷害積分、摧毀後預測傷害為零、治療預測不修改狀態、核心存檔與 30 秒計時 replay、舊存檔初始化、舊施法事件兼容、拒絕非法階段、死亡／撤離角色不受核心傷害、核心全滅後沒有超時全場耗損、Agent 實際攻擊與動畫事件。

兩個受控 AI 樣本（1729、1730），第 8 層、初始四職業，40 秒內核心總生命 270 → 0。這些樣本抑制 Boss 排程招式，專門驗證拆除與閃避整合，不代表自然闖關、勝率或難度 Gate。

最終 `pressure-build-pair-final` 的 Development／Release Build 成功，exit 0，合計 147.640 秒（含 21 項機制檢查、字串／字型驗證及兩份 Build）。628 個雙語鍵與 886 種所需字元覆蓋通過。

| Player | 語言／解析度 | 情境 | 結果 |
| --- | --- | --- | --- |
| Development 核心 | zh-TW／1600×900 | 12 | exit 0、issues 0 |
| Development 核心 | en／1280×720 | 12 | exit 0、issues 0 |
| Release 核心 | zh-TW／1600×900 | 12 | exit 0、issues 0 |
| Release 遺書／Boss／技能樹 | zh-TW／1280×720 | 13 | exit 0、issues 0 |
| Release 威脅／作者篩選 | zh-TW／1280×720 | 12 | exit 0、issues 0 |
| Release 背包／記憶／相機 | zh-TW／1280×720 | 14 | exit 0、issues 0 |

最終成品共 75 張截圖，六份完整 Player log 沒有例外或 JobTempAlloc 警告。核心情境包含冷卻、蓄能、脈衝、圈外、超時、單座摧毀、全部摧毀、多種傷害、核心攻擊、存檔、休息層與 Boss 資料。逐情境檢查模型與預警／脈衝數量對應模擬狀態、舊全場光環不可見、觀察不修改模擬 JSON。人工抽查繁中脈衝與英文複合威脅，核心和紅圈可見，字型與換行正常；威脅面板移到右側，避免擋住中央戰鬥。

UI galleries 曾同時執行多個獨立 Player，各自使用 artifacts／測試存檔，因此不作效能認證，也沒有硬體滑鼠操作的新認證。沒有停止或改寫使用者自己的 Player／存檔。

前三次受控驗證的失敗紀錄保留：第一次測試誤用無基礎壓力的第 1 層；第二次揭露舊存檔的空清單需要版本旗標判定並修正；第三次非法資料測試忘記還原新版旗標。第四次完整 Build／初始 12 張短測成功，之後移動面板並加入內圈波動，重新產出最終雙 Build；早期成品不混入上述 75 張驗證。

原始紀錄保留於 `Artifacts/Phase311/pressure-*`；成品與分析以 `Artifacts/pressure-core-build-manifest.json` 和 `Artifacts/pressure-core-validation.json` 綁定。初次成品另存 `Artifacts/pressure-core-build-manifest-initial.json`。代表截圖與聯絡表位於 `Docs/Media/PressureCores`。重建分析：`python Tools/analyze-pressure-cores.py`。

Development assembly SHA256：`c698cc171367a4edb9615b6693c91373c276e45fd34c132f2eadde0af0d8d255`。
Release assembly SHA256：`3e49d30b1a55e35cdb17554f87421bdffae5b4753f92d8199378f529652784c9`。所有最終 Player 測試的 managed assembly hash 與相應出貨成品一致。

新版執行檔：`Builds/PressureCores/Windows/Ember.exe`（Release）及 `Builds/PressureCores/Development/Ember.exe`。

未啟動新的 5000-seed simulation 或 120-minute soak。新版平衡與長時間穩定性未認證；歷史 Balance Gate FAIL 沒有被本輪短測推翻。
