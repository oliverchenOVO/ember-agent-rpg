# 品質驗證
Unity batch compiler -> Editor Validation.Run -> BuildWindows -> Windows player smoke -> screenshot review。
Core checks：傷害/防禦、自由加點、武器專精與要求、slot 上限、Mana/CD、死亡施法、製作資源与灌注成本、loot 無組成偏差、庫存上限。
Memory checks：失敗清除、真正 TowerClear 只保留存活者、切片不取得勝者資格、遺書字數與讀取動作。
Save checks：JSON round-trip、RNG 可重現、繼續執行一致、損壞 primary 的 backup 復原、版本拒絕。
Simulation checks：多種 seed 都在時間上限內結束且能重新開始；孤存者继续；休息區倒數与坍塌致死。
UI/Player checks：可見四角色、Boss/血量/預警、事件理由、視角、pause/speed、存讀檔、遺書/歷史；無 exception。
先實作可直接 batch 呼叫的 assertion harness，避免 NUnit package 下載影響第一次建置。
未完成完整 25F、獨立分隊爬塔與 LLM 的驗證應列入後續 milestone，不可以用切片通過代稱。
