# 物品與製作
ItemDefinition：id、kind、weaponType、power、quality、skill、profession、materialCost。
ItemInstance：instanceId、definitionId、quality、infusedSkill、upgrade。
所有人能裝備任何武器；武器技能來自實體，不能憑空學會別職技能。
Recipe 驗證材料數量、工坊、已學灌注技能、時間與生命；開始時預留材料，完成時產出。
製作有 4 秒工作時間，黑smith 品質 +0.25，智慧 +0.01/WIS，基準品質 1.2，高於一般掉落 1.0。
逃離或死亡會取消未完工作，預留材料不退，避免免費洗製作；明確顯示成本。
Loot table 全職業共用，先抽掉落再由 Agent 比較配裝/拆解。庫存有限 24；溢出轉材料。
可用 HP/MP potion、EXP book、職業技能書、裝備；缺材料不製作，非本職技能書拆解。
武器強化消耗材料，提升 power；防具降低傷害。正式内容會擴充配方、稀有材料、飾品與暫時 Buff。
