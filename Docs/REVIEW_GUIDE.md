# 作品審閱指南

這份指南提供快速理解 EMBER 的閱讀順序。遊戲目前是可遊玩的研究／作品集版本，完整開發歷程保留在原倉庫。

## 三分鐘認識作品

1. 閱讀 [README](../README.md) 的作品亮點，先了解玩家是觀察自主 Agent，而非直接下達戰鬥指令。
2. 觀看[休息層動態短片](../Media/Portfolio/refuge-demo.mp4)，觀察搜尋、移動與倒數的關係，再查看[實機圖集](SCREENSHOTS.md)的戰鬥、技能樹、背包與記憶介面。
3. 閱讀[系統架構](ARCHITECTURE.md)與[角色行動邏輯](AGENT_DECISION_LOGIC.md)，理解高階目標、低階戰術、引擎檢查與畫面呈現如何分工。
4. 查看[工程證據](ENGINEERING_EVIDENCE.md)與[已知限制](../KNOWN_LIMITATIONS.md)，區分已驗證的機制、受控樣本結果與尚未完成的驗收。

## 想深入了解哪一部分？

| 問題 | 建議閱讀 |
| --- | --- |
| Agent 為什麼選擇這個行動？ | [行動邏輯](AGENT_DECISION_LOGIC.md)、[效用與戰術演算法](ALGORITHMS.md) |
| 記憶如何影響下一輪？ | [記憶來源與有向關係](MEMORY_RELATIONSHIPS.md) |
| 有哪些實際改善與量測？ | [工程案例](PORTFOLIO_CASE_STUDY.md)、[工程證據](ENGINEERING_EVIDENCE.md) |
| 如何確認下載成品與原始碼版本？ | [散布與雜湊驗證](DISTRIBUTION.md)、[圖片來源](../Media/Portfolio/PROVENANCE.json) |
| 哪些內容還沒完成？ | [已知限制](../KNOWN_LIMITATIONS.md)、[公開狀態](PUBLIC_REPOSITORY_SETTINGS.md) |

## 親自觀察

從 [Windows Release](https://github.com/oliverchenOVO/ember-agent-rpg/releases/tag/v0.3.0-portfolio) 下載並完整解壓縮，執行 `Ember.exe`。點選右側角色可切換觀察對象；查看背包、配置內容、Boss 資料與技能樹，並比較思緒、記憶和實際行動。休息層支援滾輪縮放、左鍵拖曳旋轉、右鍵拖曳平移。

動態短片是 2026-10-11 從已修補 Windows 展示版擷取的 **8 秒、原速、無聲、局部畫面**。它展示休息層的實際連續移動，未重排時間或生成遊戲內容；不是完整介面展示、戰鬥影片或通關證據。較完整的 15–30 秒戰鬥示範與 GitHub 社群預覽設定仍待補齊。

最新遊戲機制驗證對應 `43a2f70`。本次只新增展示素材與文件，沒有執行新的完整平衡驗收、5000 個種子模擬或 120 分鐘穩定性測試；歷史驗收未通過的結果仍保留。
