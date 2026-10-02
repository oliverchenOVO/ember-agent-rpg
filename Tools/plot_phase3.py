"""Create repository report figures from completed telemetry and rendering samples."""
import argparse,csv,math,statistics
from pathlib import Path
import matplotlib
matplotlib.use('Agg')
import matplotlib.pyplot as plt
from matplotlib import font_manager
ROOT=Path(__file__).resolve().parents[1]
font_manager.fontManager.addfont(str(ROOT/'UnityProject/Assets/Resources/Fonts/NotoSansCJKtc-Regular.otf'))
plt.rcParams.update({'font.family':'Noto Sans CJK TC','axes.unicode_minus':False,'font.size':10})
OUT=ROOT/'Docs/Images';OUT.mkdir(exist_ok=True)

def difficulty(label):
 with (ROOT/f'Artifacts/phase3-{label}-analysis.csv').open(encoding='utf-8-sig') as f:rows=list(csv.DictReader(f))
 n=int(rows[0]['runs_reached']);x=[int(r['floor']) for r in rows]
 fig,axes=plt.subplots(2,1,figsize=(11,7),sharex=True,layout='constrained')
 axes[0].plot(x,[float(r['run_clear_rate'])*100 for r in rows],'o-',label='抵達後擊敗 Boss 的 seed 比例')
 axes[0].plot(x,[int(r['runs_cleared'])/n*100 for r in rows],'s-',label='占所有起始 seeds 的比例')
 axes[0].set(ylabel='比例（%）',ylim=(0,105),title=f'EMBER Phase 3｜{n} 組種子難度統計');axes[0].legend(loc='lower left')
 axes[1].plot(x,[float(r['average_clear_time']) for r in rows],'o-',color='#b65f11')
 axes[1].set(xlabel='樓層',ylabel='通關隊伍平均戰鬥時間（秒）',xticks=range(1,26))
 for ax in axes:ax.axvspan(5.5,10.5,color='#dcaf69',alpha=.18);ax.grid(alpha=.22)
 fig.savefig(OUT/'phase3-difficulty.png',dpi=160);plt.close(fig)

def performance(label):
 with (ROOT/f'Artifacts/Phase3/{label}/frames.csv').open(encoding='utf-8-sig') as f:rows=[r for r in csv.DictReader(f) if int(r['objects'])>=100 and int(r['draw_calls'])>1 and float(r['elapsed'])>=60]
 x=[float(r['elapsed'])/60 for r in rows]
 val=lambda k,scale=1:[float(r[k])/scale if r.get(k) not in (None,'NA','') else math.nan for r in rows]
 fig,axes=plt.subplots(3,1,figsize=(11,9),sharex=True,layout='constrained')
 for key,scale,name in [('cpu_ns',1e6,'CPU 主執行緒 counter'),('gpu_timing_ms',1,'GPU FrameTiming')]:
  values=val(key,scale);axes[0].plot(x,[v if v>0 else math.nan for v in values],label=name,linewidth=.6,alpha=.8)
 axes[0].set(yscale='log',ylabel='時間（ms，對數軸）',title='EMBER｜實際渲染長程測試（1 Hz 取樣）');axes[0].legend()
 axes[1].plot(x,val('total_used',2**20),color='#276a91',label='Total Used Memory');axes[1].set(ylabel='引擎 Used Memory（MiB）',ylim=(0,max(val('total_used',2**20))*1.15));axes[1].legend(loc='upper left')
 secondary=axes[1].twinx();secondary.plot(x,val('gc_used',2**20),color='#bb6d19',linewidth=.6);secondary.set_ylabel('GC Used（MiB）',color='#bb6d19')
 axes[2].plot(x,val('gc_alloc',1024),linewidth=.5,color='#7d519c');axes[2].set(xlabel='實際經過時間（分鐘）',ylabel='GC 配置（KiB／取樣幀）',xticks=range(0,121,15))
 for ax in axes:ax.grid(alpha=.2);ax.set_xlim(0,120)
 fig.savefig(OUT/'phase3-performance.png',dpi=160);plt.close(fig)

if __name__=='__main__':
 p=argparse.ArgumentParser();p.add_argument('--telemetry',default='final');p.add_argument('--profile',default='soak-relocated-120m');a=p.parse_args();difficulty(a.telemetry);performance(a.profile)
