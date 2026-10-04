# Portfolio case study · 用遊戲研究自主系統

EMBER uses a game as an observable experimental environment for autonomous decisions, finite memory, group interaction and reproducible engineering. The useful story includes failures and bounded conclusions, not just a feature list.

## Problem / Original idea

一般 RPG 讓玩家替角色做決定；這個專案把「決定」本身變成觀察對象：四名角色能否各自追求生存、好奇心、收益與關係，卻仍形成可理解的隊伍行為？死亡之後又該傳遞多少資訊，才能讓下一次挑戰有意義，而不變成全知策略？

## Design goals

把高塔拆成戰鬥、休息、轉場與獨立分隊；把角色拆成狀態、人格、關係、記憶、裝備、目的與行動。玩家應能看見原因，而不是只看到角色移動。規則與展示分離，讓數值比較可以重現，也讓 UI 能說明行為。

## Technical approach

高階快照和效用分數決定目的，低階處理閃避與施法。引擎驗證合法性、剩餘魔力、冷卻、距離及時間預算。資料表驅動技能與 Boss；自訂導航用共享障礙足跡避免「畫得到牆卻穿過去」。記憶區分親歷、遺書與轉述；關係以有向事件更新。

## What failed, and what changed?

| Problem | Diagnosis | Change | Remaining limit |
| --- | --- | --- | --- |
| 10F difficulty cliff | 將樓層／編成與資源耗損拆開比較，避免以整體平均掩蓋特殊樓層 | 迭代回復窗口、資源與施法政策 | 歷史 H3 編成 Gate 仍 FAIL，最新版完整 Gate 未重跑 |
| Healer dominance | 四補師輸出接近四法師，卻保留約 94% MP；技能共用錯誤屬性成長 | 共用職業縮放、補師輸出定位、治療預約與四格分工 | 無補師短測仍較快，完整爬塔價值待驗證 |
| Invisible attrition | 扣血來源與畫面不一致，觀察者無法判斷對策 | 可摧毀壓力核心、獨立預警／脈衝與來源說明 | 不同 DoT／召喚物仍需各自可辨識的演出 |
| JobTempAlloc warnings | 保留原 log、版本與 native stack；定位到引擎粒子幾何排程鏈 | 改善觀察與資源清理，檢查正常退出與短窗口 | 原始 runtime 警告精確根因未證明；短期零警告不等於長期修復 |
| Unity ILPP / Build stalls | 同時查看 CPU、log 成長、stage、子程序與 exit marker；舊 log 曾完成 Csc，不能稱完全沒編譯 | 有界 watchdog、分離 stdout/stderr、同專案互斥、雜湊綁定成品 | 未重現原始停滯觸發條件，不能宣稱修復 Unity 引擎 |
| CJK glyph / UI issues | 盤點完整字串與字型語料，而不只翻譯單張截圖 | zh-TW／en 字串表、嵌入 Noto CJK、實機小尺寸檢查 | 最新語料與畫面樣本通過，不代表所有未取樣組合都人工檢查 |

## Experimental thinking

每次比較限定同一版本和樣本。最新 Team Balance 使用兩個 seed、三個樓層與六種編成做 36 組前後配對；同時保存資料、成品雜湊與畫面。它回答「受控戰鬥輸出如何變化」，不能回答「二十五層自然進度已全面平衡」。中斷的長測不補寫為成功。

## My role and AI-assisted development

Oliver Chen 主導問題拆解、玩法目標、架構方向、驗證標準、整合與迭代。程式與文件產出使用 AI 協作，作者負責需求取捨與結果檢查。這份作品強調可追查的工程判斷，而不以「全部手寫」作為不實賣點。

## Current limitations / Next steps

持續改善後段內容與 rig／音效；以完整自然進度重新評估補師價值及 10F 門檻；補齊最新版完整 Gate 與長時間 soak；設計可重現的 LLM 決策紀錄；持續擴充 Windows 封裝版本的相容性驗證。這些是後續方向，並非本次展示已完成事項。

[Architecture](ARCHITECTURE.md) · [Algorithms](ALGORITHMS.md) · [Evidence](ENGINEERING_EVIDENCE.md) · [Limitations](../KNOWN_LIMITATIONS.md).
