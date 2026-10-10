# 文件索引

從[繁體中文 README](../README.md)開始，或閱讀 [English README](../README.en.md)。第一次認識作品可參考[作品審閱指南](REVIEW_GUIDE.md)。

## 作品展示與實作

- [實機圖集](SCREENSHOTS.md)：註明各版本與受控測試情境，包含休息層動態短片。
- [系統架構](ARCHITECTURE.md)、[角色行動邏輯](AGENT_DECISION_LOGIC.md)、[演算法](ALGORITHMS.md)、[記憶與關係](MEMORY_RELATIONSHIPS.md)。
- [工程證據](ENGINEERING_EVIDENCE.md)、[工程案例](PORTFOLIO_CASE_STUDY.md)、[網站與推甄介紹](PORTFOLIO_COPY.md)。
- [已知限制](../KNOWN_LIMITATIONS.md)、[散布狀態](DISTRIBUTION.md)、[公開安全檢查](PUBLIC_REPOSITORY_SAFETY_AUDIT.md)、[倉庫設定](PUBLIC_REPOSITORY_SETTINGS.md)。

## 最新觀察介面改善

[觀察紀事與挑戰紀錄](OBSERVER_JOURNAL_VALIDATION.md)：角色篩選、內容搜尋、完整捲動閱讀與歷史配置顯示。

## 最新遊戲機制驗證

[隊伍平衡驗證](TEAM_BALANCE_VALIDATION.md)對應遊戲版本 `43a2f70`。後續作品集提交沒有新增遊戲機制，也不代表重新取得完整平衡認證。最新 5000 個種子模擬與 120 分鐘穩定性測試未執行。

[動態素材檢查紀錄](../Evidence/portfolio-demo-validation.json)僅驗證 8 秒休息層短片的解碼、影格變化、檔案雜湊與文件連結；它不取代遊戲機制測試或完整介面驗證。

## 歷史開發證據

早期設計與驗證文件保留在 Git 中。各文件的建置路徑、測試數量與通過／失敗結果只適用於當時版本；其中的本機路徑是歷史紀錄，不是 GitHub 下載連結。

- [Phase 2](PHASE2_VALIDATION.md)、[Phase 3](PHASE3_VALIDATION.md)、[Phase 3.1](PHASE3_1_VALIDATION.md)、[Phase 3.1.1](PHASE3_1_1_VALIDATION.md)。Phase 3.1.1 完整驗收未通過。
- [休息層互動](REST_INTERACTION_VALIDATION.md)、[知識與圖鑑](KNOWLEDGE_CODEX_VALIDATION.md)、[戰鬥演出](COMBAT_ART_VALIDATION.md)、[避難層探索](REFUGE_EXPLORATION_VALIDATION.md)。

Unity 原始碼位於 `UnityProject`；建置、短程 Player 檢查與歷史模擬腳本位於 `Tools`。**執行前先閱讀腳本：`build.ps1 -TestsOnly` 包含 5000 個種子樣本批次。**
