# Telemetry analysis

單位是分隊樓層 encounter；run_clear_rate 依 seed 去重。死亡含戰鬥/休息；技能與物品輸出以 exposure 正規化，不能推斷因果或單件強度。

| Floor | Reached seeds | Clear % | Time | Deaths | Taken | Heal | Potions | Survivors | Split % | Quality |
|---|---:|---:|---:|---:|---:|---:|---:|---:|---:|---:|
| 1 | 1000 | 100.0% | 3.9 | 0 | 1.1 | 0.0 | 0.00 | 4.00 | 99.9% | 1.00 |
| 2 | 1000 | 100.0% | 14.5 | 0 | 0.2 | 0.0 | 0.00 | 1.09 | 35.7% | 1.34 |
| 3 | 1000 | 100.0% | 13.1 | 0 | 0.2 | 0.0 | 0.12 | 1.01 | 29.8% | 1.43 |
| 4 | 1000 | 100.0% | 16.1 | 0 | 23.7 | 7.4 | 0.58 | 1.01 | 29.2% | 1.43 |
| 5 | 1000 | 100.0% | 20.0 | 0 | 9.3 | 0.1 | 0.41 | 1.01 | 25.8% | 1.46 |
| 6 | 1000 | 100.0% | 15.1 | 0 | 0.2 | 0.1 | 0.17 | 1.01 | 25.3% | 1.46 |
| 7 | 1000 | 100.0% | 15.7 | 0 | 0.2 | 0.0 | 0.17 | 1.01 | 23.1% | 1.48 |
| 8 | 1000 | 100.0% | 15.4 | 0 | 0.2 | 0.0 | 0.15 | 1.00 | 22.7% | 1.49 |
| 9 | 1000 | 100.0% | 19.0 | 0 | 26.5 | 3.5 | 0.14 | 1.00 | 20.4% | 1.50 |
| 10 | 1000 | 100.0% | 22.3 | 0 | 6.8 | 0.3 | 0.13 | 1.00 | 20.9% | 1.50 |
| 11 | 1000 | 100.0% | 16.8 | 0 | 0.2 | 0.0 | 0.12 | 1.00 | 20.3% | 1.51 |
| 12 | 1000 | 100.0% | 17.3 | 0 | 0.1 | 0.1 | 0.12 | 1.01 | 20.3% | 1.52 |
| 13 | 1000 | 100.0% | 16.4 | 0 | 0.2 | 0.1 | 0.12 | 1.00 | 19.5% | 1.53 |
| 14 | 1000 | 100.0% | 20.5 | 0 | 26.4 | 2.1 | 0.12 | 1.00 | 17.6% | 1.53 |
| 15 | 1000 | 100.0% | 23.4 | 0 | 5.7 | 0.1 | 0.10 | 1.00 | 17.9% | 1.53 |
| 16 | 1000 | 100.0% | 18.0 | 0 | 0.1 | 0.1 | 0.09 | 1.00 | 17.5% | 1.54 |
| 17 | 1000 | 100.0% | 18.3 | 0 | 0.2 | 0.0 | 0.08 | 1.00 | 17.0% | 1.55 |
| 18 | 1000 | 100.0% | 17.7 | 0 | 0.1 | 0.1 | 0.08 | 1.00 | 16.3% | 1.55 |
| 19 | 1000 | 100.0% | 21.9 | 0 | 27.9 | 1.9 | 0.07 | 1.00 | 14.8% | 1.56 |
| 20 | 1000 | 100.0% | 24.7 | 0 | 6.1 | 0.2 | 0.06 | 1.00 | 15.1% | 1.56 |
| 21 | 1000 | 100.0% | 19.0 | 0 | 0.2 | 0.0 | 0.05 | 1.00 | 15.0% | 1.56 |
| 22 | 1000 | 100.0% | 19.5 | 0 | 0.1 | 0.1 | 0.04 | 1.00 | 14.8% | 1.56 |
| 23 | 1000 | 100.0% | 18.7 | 0 | 0.2 | 0.0 | 0.03 | 1.00 | 14.7% | 1.57 |
| 24 | 1000 | 100.0% | 23.4 | 0 | 30.2 | 1.8 | 0.03 | 1.00 | 14.3% | 1.57 |
| 25 | 1000 | 100.0% | 25.5 | 0 | 8.0 | 0.1 | 0.02 | 1.00 | 11.5% | 1.58 |

## Flags
- 1F: no recorded deaths
- 2F: no recorded deaths
- 3F: no recorded deaths
- 4F: no recorded deaths
- 5F: no recorded deaths
- 6F: no recorded deaths
- 7F: no recorded deaths
- 8F: no recorded deaths
- 9F: no recorded deaths
- 10F: no recorded deaths
- 11F: no recorded deaths
- 12F: no recorded deaths
- 13F: no recorded deaths
- 14F: no recorded deaths
- 15F: no recorded deaths
- 16F: no recorded deaths
- 17F: no recorded deaths
- 18F: no recorded deaths
- 19F: no recorded deaths
- 20F: no recorded deaths
- 21F: no recorded deaths
- 22F: no recorded deaths
- 23F: no recorded deaths
- 24F: no recorded deaths
- 25F: no recorded deaths

## Skill usage
{"fireball": 73677, "holy": 157565, "slash": 141309, "wave": 56141, "rage": 11690, "frost": 52090, "ward": 557, "meteor": 26262, "counter": 45033, "guard": 428, "lightning": 24320, "shot": 36658, "poison": 41195, "rain": 26450, "pierce": 37198, "snipe": 22826, "cleanse": 767, "heal": 749, "enchant": 221, "taunt": 19, "trap": 161, "shield": 309}

## Class exposure
{"Mage": {"damage_per_exposure": 903.6271798343499, "healing_per_exposure": 0.0, "exposures": 24646}, "Healer": {"damage_per_exposure": 901.4267681017773, "healing_per_exposure": 3.1383542061800243, "exposures": 23133}, "Warrior": {"damage_per_exposure": 900.047870348636, "healing_per_exposure": 0.0, "exposures": 27281}, "Archer": {"damage_per_exposure": 898.1669670058046, "healing_per_exposure": 0.0, "exposures": 25521}}

## Weapon exposure
{"staff": {"damage_per_exposure": 904.5892983601989, "exposures": 22123}, "holy_staff": {"damage_per_exposure": 902.0593629960694, "exposures": 20931}, "greatsword": {"damage_per_exposure": 899.7860197611727, "exposures": 29423}, "bow": {"damage_per_exposure": 898.2267810485299, "exposures": 26083}, "spear": {"damage_per_exposure": 892.4983931800506, "exposures": 2021}}
