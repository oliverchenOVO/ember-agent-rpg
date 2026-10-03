# Phase 3.1 結構化診斷資料

- `baseline100`：100/100 seeds；runtime `702b74e16206d937493b6964d5fe62b5e6fd057fc278c954a592722fb57b7476`；10F 78/99 encounters 擊敗。
- `candidate1-100`：100/100 seeds；runtime `ddd1dc2a3d11c61dfa6c20a431a4351722d58cdbc13bbf0fa0bead06a2b10a58`；10F 96/100 encounters 擊敗。
- `baseline50-Healer`：50/50 seeds；runtime `702b74e16206d937493b6964d5fe62b5e6fd057fc278c954a592722fb57b7476`；10F 50/51 encounters 擊敗。
- `candidate1-50-Healer`：50/50 seeds；runtime `ddd1dc2a3d11c61dfa6c20a431a4351722d58cdbc13bbf0fa0bead06a2b10a58`；10F 50/50 encounters 擊敗。

所有數值依 label/runtime/config 分開。Natural 含自由選職與分隊，不能與固定編成視為等價。固定編成需 floorLimit=10；25F 自然樣本的總量涵蓋 1–25F，不能直接比較其總量。

恢復來源 raw/effective/overheal 分開；MP 藥水不計入 HP 恢復。death source 表示最後一擊，並不單獨證明全部致死因素。ready 是資源/冷卻/配裝候選，未保證施法距離或能救回目標。盾吸收為實際抵銷傷害；church 只提供 guard，不是直接治療。技能機會欄 raw/effective/mana 分別是 unlocked/equipped/ready 秒數。weapon_exposures 是 encounter 暴露次數。loot 與 craft 是觀察，不是掉落依賴因果證明。
