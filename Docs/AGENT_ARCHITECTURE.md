# Agent 架構
IBrain.Decide(WorldState, AgentState, Catalog) -> Intent。合法動作由引擎驗證，Brain 不直接修改世界。
人格包含 risk、greed、curiosity、empathy、loyalty、aggression；Goals 以 intent 與理由呈現。
選職業為加權抽樣，允許重複與奇怪加點；正式啟動自主選職，--showcase / smoke 測試首輪展示四職業。
戰鬥效用：對低血盟友治療/保護、威脅與存活、Mana 保留、傷害、個性、關係、預警迫近。
移動執行器依 intent 接近、遠離預警/目標，冷卻到期才施法；避免 LLM 每幀控制。
休息效用：血量與資源需要、探索渴望、製作收益、剩餘時間与離出口距離共同評分。
關係為有向 Trust/Respect/Friendship/Love/Fear/Resentment/Loyalty/Rivalry。
施救、分配物品與爭執更新關係；各值有限範圍。不採 Love 門檻強制救援。
對話由 speaker、intent、事件與關係 context 組成；目前為本機文字生成模板，可觀察因果，未接 LLM。
分隊需未來獨立 PartyProgress scheduler；本切片僅記錄衝突與個別逃离，不虛構完整分裂爬塔。
Brain 擴展契約：LLM 回傳有限 action schema，限時、快取、validator、fallback UtilityBrain，事件節流。
