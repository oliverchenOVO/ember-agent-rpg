# 記憶與存檔
角色 id 固定，人格跨輪穩定；完整 memory、關係記憶與成長分開保存。
一般失敗/SliceComplete：清除當輪 memory、關係、成長、背包。下輪只在讀死者之書動作中取得限長遺書。
只有 outcome=TowerClear 且 alive 的個體保留完整 memory 與有向關係；死亡者不保留。
每人周目結束可寫最多 80 字元，內容由最後狀態、人格、事件決定，可為遺憾或提醒。
死者之書保留最近 64 條；歷史保留最近 100 周目，保存 survivors、組成、時間、裝備、傷害、治療。
真正最快通關紀錄只納入 TowerClear；切片紀錄另標記，不污染 Hall of Fame。
WorldState 可序列化含 schemaVersion=1、rng uint state、clock、timer、agents、boss、telegraph、messages、history。
存檔寫入 tmp，File.Replace 保留 bak；首次 File.Move。讀檔檢查版本與基本 invariant，損壞回讀 bak。
自動存於結算/新周目；觀察者提供 Save/Load。測試中比較 round-trip 後繼續模擬一致性。
