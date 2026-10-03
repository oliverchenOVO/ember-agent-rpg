"""Create immutable first-stage configs bound to a successfully built runtime.

No runs start here. Editor simulation and the reference Windows Player are
explicitly distinct; the bound hashes prevent resuming against another build.
"""
import argparse, hashlib, json, re
from pathlib import Path
ROOT=Path(__file__).resolve().parents[1]
parser=argparse.ArgumentParser();parser.add_argument('--build-log',required=True);parser.add_argument('--exe',required=True);parser.add_argument('--output',default='Artifacts/Phase311/exp1-ablations');args=parser.parse_args()
log=(ROOT/args.build_log).read_text(encoding='utf8',errors='replace')
assert 'EMBER WINDOWS BUILD PASSED' in log
runtime=re.search(r'SOURCE RUNTIME HASH / ([a-f0-9]{64})',log)[1]
exe=(ROOT/args.exe).resolve();assembly=exe.parent/'Ember_Data/Managed/Assembly-CSharp.dll'
sha=lambda p:hashlib.sha256(p.read_bytes()).hexdigest()
switches=['phaseWindows','combatRecovery','clinicSupplies','holyRecoveryCost','recoveryWindow','windowManaRecovery','reducedManaDrain','potionBeforeMovement','manaReserve','defensiveAI','shieldPriority','retryInvalidCasts','healTiming','roleLoadout']
base={k:False for k in switches};base.update(phaseWindows=True,combatRecovery=True)
groups={'control-baseline':[], 'A-window':['recoveryWindow'], 'B-drain':['reducedManaDrain'], 'C-window-drain':['recoveryWindow','reducedManaDrain'], 'D-potions':['potionBeforeMovement'], 'E-reserve':['manaReserve'], 'F-supply':['clinicSupplies'], 'G-defense':['defensiveAI'], 'I-window-mana':['recoveryWindow','windowManaRecovery'], 'J-loadout':['roleLoadout'], 'control-candidate-A':switches}
out=ROOT/args.output;out.mkdir(parents=True,exist_ok=True)
binding={'balanceVersion':'P3.1.1-EXP1','runtimeHash':runtime,'executableHash':sha(exe),'managedAssemblyHash':sha(assembly),'runner':'Unity Editor / pure simulation','referencePlayer':str(exe),'notPlayerSimulation':True}
(out/'build-binding.json').write_text(json.dumps(binding,indent=2),encoding='utf8')
for label,enabled in groups.items():
    rules={**base,**{k:True for k in enabled}}
    if label=='control-baseline':rules={k:False for k in switches}
    config=dict(label=label,composition='Natural',rules=rules,firstSeed=1,count=100,floorLimit=10,maxTicks=50000,maxWallSeconds=900,executablePath=str(exe),expectedRuntimeHash=runtime)
    dest=out/(label+'-config.json')
    if dest.exists():assert json.loads(dest.read_text(encoding='utf8'))==config,'Immutable config differs'
    else:dest.write_text(json.dumps(config,indent=2),encoding='utf8')
print(json.dumps(binding,ensure_ascii=True))
