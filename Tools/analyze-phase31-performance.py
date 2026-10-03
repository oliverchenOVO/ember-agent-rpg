import argparse,csv,json,math
from pathlib import Path

root=Path(__file__).resolve().parents[1]
def number(value):
    try:
        result=float(value)
        return result if math.isfinite(result) else None
    except (ValueError,TypeError):return None
def percentile(values,p):
    values=sorted(values)
    if not values:return None
    position=(len(values)-1)*p;lo=int(position);hi=math.ceil(position)
    return values[lo]+(values[hi]-values[lo])*(position-lo)
parser=argparse.ArgumentParser();parser.add_argument('labels',nargs='+');args=parser.parse_args()
summaries=[];spikes=[]
for label in args.labels:
    folder=root/'Artifacts/Phase31'/label
    rows=list(csv.DictReader((folder/'frames.csv').open(encoding='utf-8-sig')))
    if not rows:continue
    usable=[r for r in rows if number(r['elapsed']) is not None and float(r['elapsed'])>=10]
    duration=float(rows[-1]['elapsed'])-float(usable[0]['elapsed']) if usable else 0
    wall_clock='unity_delta_ms' in rows[0]
    summary={'label':label,'complete':(folder/'complete.txt').exists(),'rows':len(rows),'after_warmup_rows':len(usable),'elapsed_seconds':float(rows[-1]['elapsed']),'warmup_seconds':10,'frame_measurement':'Stopwatch wall interval' if wall_clock else 'legacy Unity deltaTime; unreliable for wall interval','metrics':{}}
    for key,scale in [('frame_ms',1),('cpu_ns',1e-6),('gpu_ns',1e-6),('gpu_timing_ms',1),('ui_ns',1e-6),('decision_ns',1e-6),('gc_alloc',1),('managed_heap',1),('total_used',1),('draw_calls',1),('batches',1),('particles',1),('simulation_ms',1),('ui_alloc',1),('step_alloc',1),('view_alloc',1),('render_alloc',1)]:
        vs=[number(r.get(key)) for r in usable];vs=[v*scale for v in vs if v is not None and (v>0 or key not in ['cpu_ns','gpu_ns','gpu_timing_ms'])]
        summary['metrics'][key]={'valid':len(vs),'unavailable_or_zero':len(usable)-len(vs),'p50':percentile(vs,.5),'p95':percentile(vs,.95),'p99':percentile(vs,.99),'max':max(vs) if vs else None,'mean':sum(vs)/len(vs) if vs else None}
    summary['gc_bytes_per_second']=sum(number(r.get('gc_alloc')) or 0 for r in usable)/max(1,duration)
    events=list(csv.DictReader((folder/'events.csv').open(encoding='utf-8-sig'))) if (folder/'events.csv').exists() else []
    for row in sorted(usable,key=lambda r:number(r.get('frame_ms')) or 0,reverse=True)[:20]:
        t=float(row['elapsed']);near=[e for e in events if abs(float(e['elapsed'])-t)<.1]
        spikes.append({'label':label,'elapsed':t,'recorded_frame_ms':row['frame_ms'],'frame_measurement':summary['frame_measurement'],'cpu_ns':row.get('cpu_ns'),'gc_alloc':row.get('gc_alloc'),'simulation_ms':row.get('simulation_ms'),'save_ms':row.get('save_ms'),'events_within_100ms':json.dumps(near,ensure_ascii=False)})
    summaries.append(summary)
(root/'Artifacts/phase3_1_performance-summary.json').write_text(json.dumps(summaries,ensure_ascii=False,indent=2),encoding='utf8')
if spikes:
    with (root/'Artifacts/phase3_1_frame-spikes.csv').open('w',encoding='utf-8-sig',newline='') as out:
        writer=csv.DictWriter(out,fieldnames=list(spikes[0]));writer.writeheader();writer.writerows(spikes)
for summary in summaries:print(summary['label'],summary['complete'],summary['metrics']['cpu_ns'],summary['gc_bytes_per_second'])
