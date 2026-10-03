"""Plot measured diagnostic comparisons; no regression or final-gate claim."""
import csv,re,json
from pathlib import Path
import matplotlib
matplotlib.use('Agg')
import matplotlib.pyplot as plt
from matplotlib import font_manager
import numpy as np
root=Path(__file__).resolve().parents[1]
font_manager.fontManager.addfont(str(root/'UnityProject/Assets/Resources/Fonts/NotoSansCJKtc-Regular.otf'))
plt.rcParams.update({'font.family':'Noto Sans CJK TC','axes.spines.top':False,'axes.spines.right':False,'figure.facecolor':'white','axes.facecolor':'#f8fafc','font.size':10})
def rows(name):return list(csv.DictReader((root/'Artifacts'/name).open(encoding='utf-8-sig')))
fig,ax=plt.subplots(1,3,figsize=(16,5.8),layout='constrained')
classes=['Warrior','Archer','Mage','Healer','Mixed'];x=np.arange(5)
for i,(folder,prefix,name,color) in enumerate([('exp1-compositions','H100-','H：防禦＋Holy 成本','#94a3b8'),('exp21-N-compositions','N100-','N：配裝＋預測治療','#0d9488'),('exp21-H3-compositions','H3-','H3：有限補給 cost=3','#f59e0b')]):
 values=[]
 for cls in classes:
  # EXP1 naming is read from the stored composition config, never guessed.
  config=next(json.loads(p.read_text(encoding='utf-8-sig')) for p in (root/'Artifacts/Phase311'/folder).glob('*-config.json') if json.loads(p.read_text(encoding='utf-8-sig'))['composition']==cls)
  text=(root/'Artifacts/Phase311'/folder/config['label']/'summary.txt').read_text(encoding='utf8')
  values.append(int(re.search(r'clears=(\d+)',text).group(1)))
 ax[0].bar(x+(i-1)*.25,values,.24,label=name,color=color)
 for j,v in enumerate(values):ax[0].text(j+(i-1)*.25,v+1,str(v),ha='center',fontsize=8)
ax[0].set(xticks=x,xticklabels=['戰士','弓箭手','法師','補師','混合'],ylim=(0,116),ylabel='突破 10F 的 seed 數／100',title='固定編成：職業差距仍未收斂')
ax[0].legend(loc='upper left',fontsize=8)
natural=rows('phase3_1_1_exp21_ablation.csv')+rows('phase3_1_1_exp21_price_ablation.csv')
labels=['H','H2','K','L','M','N','H3','P'];vals=[float(r['clear_percent']) for r in natural]
ax[1].axhspan(82,90,color='#bbf7d0',alpha=.6,label='診斷區間 82–90%')
ax[1].bar(labels,vals,color=['#0d9488' if 82<=v<=90 else '#f59e0b' for v in vals],width=.6)
for i,v in enumerate(vals):ax[1].text(i,v+.4,f'{v:.1f}',ha='center',fontsize=8)
ax[1].set(ylim=(70,102),ylabel='已解決 10F encounter 通關率（%）',title='Natural：不能只看平均通關率')
ax[1].legend(fontsize=8)
economy=rows('phase3_1_1_exp21_skill-economy.csv')+rows('phase3_1_1_exp21_price_skill-economy.csv')
heal=[r for r in economy if r['skill']=='heal' and r['label']!='K-finite-supply-only' and r['label']!='P-supply-cost3']
names=['H','H2','L','M','N','H3'];overheal=[100*float(r['overheal_fraction']) for r in heal]
ax[2].bar(names,overheal,color=['#94a3b8']+['#0d9488']*5)
for i,v in enumerate(overheal):ax[2].text(i,v+1,f'{v:.1f}',ha='center',fontsize=8)
ax[2].set(ylim=(0,102),ylabel='直接 Heal 溢補比例（%）',title='預測治療改善溢補，尚未解決平衡')
fig.suptitle('Phase 3.1.1 診斷結果｜Gate FAIL，沒有 RC／FINAL',fontsize=17)
fig.text(.5,-.015,'各組 100 seeds，floorLimit=10；相同 seed 跨設定重複比較。編成 seed 成功率與 Natural encounter 成功率分母不同。H2 排除 1 個 censored encounter。',ha='center',fontsize=9)
output=root/'Docs/Images/phase3_1_1-recovery.png';output.parent.mkdir(exist_ok=True);fig.savefig(output,dpi=150,bbox_inches='tight');plt.close(fig)
print('Diagnostic chart saved')
