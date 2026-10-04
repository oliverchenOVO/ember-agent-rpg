# Algorithms · 演算法與實例

These are implementation-grounded explanations and readable pseudocode, not a source-code release. Heuristic coefficients are design choices; they have not been claimed as optimal or learned policies.

## Utility scoring

一般形式為 `selected = argmax U(action, context)`，但會先產生合法候選，並在執行前加入安全與資源檢查。高階休息決策的實際 urgency 為：

```text
urgency = clamp01(1 - (remaining - escapeSeconds - 3) / 12)
exitUtility = 12 + urgency * (90 + caution*20 - risk*12 + bookConfidence*8)
exploreUtility = 26 + curiosity*22 + greed*10 - urgency*(75 - risk*22)
```

**受控計算例，非實測紀錄：** remaining=12、escapeSeconds=6、caution=0.5、risk=0.4、curiosity=0.7、greed=0.3、bookConfidence=0。urgency=0.75，Exit=83.4，Explore=−5.25，因此在這兩個候選中選撤離。其他合法候選仍需一起比較，不能把兩項例子當成完整決策。

設施額外扣分會納入 `travel + work + escape + safety margin`。在空間版休息層，估計總時間先含 3 秒保留，再以 `total + 2 > remaining` 判定不足，扣除 `90 + caution*30`。風險另依設施 risk、caution 和冒險傾向扣分。這是軟性懲罰，不是強制禁止冒險。

## Navigation

休息層為 60×40，導航使用間距 2 的 31×21 網格、四向鄰居及障礙膨脹（角色半徑 0.38）。若起終點直線可通行，直接返回終點；否則用 BFS 搜尋連通格，再做合法視線平滑。

```text
if direct segment is collision-free: return [destination]
start, goal = nearest reachable walkable nodes
queue = [start]; previous[start] = start
while queue is not empty and goal is not visited:
    node = pop_front(queue)
    for next in four_neighbors(node):
        if walkable(next) and unvisited(next) and clear(node, next):
            previous[next] = node
            push_back(queue, next)
if goal is unvisited: return no_path
route = trace_previous(goal) + destination
remove intermediate points only if the shortcut is collision-free
```

BFS 核心複雜度為 O(V+E)；最近可達節點搜尋、碰撞檢查和平滑另有成本。它是等權網格最短步數，再平滑成世界座標路線，**不是** A*、Unity NavMesh 或全域最短連續幾何路徑。查無可達路徑時距離為無限大，不能拿直線距離假裝可撤離。

## Predictive healing

```text
arrivalHorizon = clamp(time_to_cast_range, 0, 1.5 seconds)
need = clamp(maxHP - HP + incomingDamage(arrivalHorizon)
             - expectedRegeneration - committedOtherHealing, 0, maxHP)
effective = min(healPower, need)
overhealFraction = (healPower - effective) / healPower
utility = effective/healPower + need/maxHP + urgencyBonus
          - overhealFraction - travelSeconds*0.15
```

IncomingDamage 合併核心預測、可能落下的招式、狀態、危險地面和召喚物，再考慮護甲、防禦與盾。已完成施法進入冷卻的補師不再保留同一份需求；準備就緒且距施法範圍不超過 1.5 秒的合法治療才納入預約。群補威力為 0.65 倍，需至少兩人需求達到群補威力的 25%，且有急救需求或總有效需求達 55%。

**示例：** 生命 40/100、估計受傷 10、恢復 0、另一補師即將補 30，缺血需求由 70 變成 40。若本次治療威力 50，有效 40、過量比例 0.2。瀕死檢查另使用短期傷害與 35% 生命門檻，不會僅因有預約就放棄急救。

## Directed relationships

目前 Attachment 的實際組合：

```text
A(i → j) = 0.22*trust + 0.25*friendship + 0.30*love
           + 0.23*loyalty - 0.45*resentment
```

關係事件有方向與來源；i 對 j 的評價不一定等於 j 對 i。人格、自己生命、隊友危險與關係共同參與保護／支援評分；沒有「感情超過某個值就必須救人」的單一劇情開關。

## Seeded simulation and replay

RNG 使用儲存於狀態的 uint xorshift 序列：

```text
r = rng
r ^= r << 13
r ^= r >> 17
r ^= r << 5
rng = (r == 0 ? 1729 : r)
sample = (rng & 0xFFFFFF) / 16777216.0
```

unsigned 32-bit 運算、固定內容／規則／seed／輸入與固定步長支持重現。存檔驗證不只比較 JSON 能否讀回，也比較還原後繼續模擬的結果。遠端 LLM 時序與跨平台浮點差異不在無條件決定性承諾內；測試證據依實際環境與版本解讀。

## Class scaling and evaluation

技能的主要屬性分別是力量、敏捷、智力、智慧；系數為 1.2／0.6／1.1／1.0。補師傷害技能乘 0.75，治療保留完整威力。裝備估價使用實際角色屬性、專精、品質、升級、附魔與技能武器限制，而不是只看武器基礎 power。

數值與閾值是可調參數。最新比較採相同 seed、樓層、屬性與裝備做前後配對；僅改策略後看到 DPS 改變，不足以證明完整爬塔勝率提高。詳見 [evidence](ENGINEERING_EVIDENCE.md)。
