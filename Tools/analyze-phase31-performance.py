import argparse,csv,json,math
from collections import Counter
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
parser=argparse.ArgumentParser();parser.add_argument('labels',nargs='+');parser.add_argument('--input-root',default='Artifacts/Phase31');parser.add_argument('--prefix',default='phase3_1');args=parser.parse_args()
summaries=[];spikes=[]
for label in args.labels:
    folder=root/args.input_root/label
    rows=list(csv.DictReader((folder/'frames.csv').open(encoding='utf-8-sig')))
    if not rows:continue
    detailed=not((folder/'session.txt').exists() and 'detailedDiagnostics=False' in (folder/'session.txt').read_text(encoding='utf8'))
    events=list(csv.DictReader((folder/'events.csv').open(encoding='utf-8-sig'))) if (folder/'events.csv').exists() else []
    cleanup=min((float(e['elapsed']) for e in events if e['event']=='scene_cleanup_begin'),default=float('inf'))
    all_warm=[r for r in rows if number(r['elapsed']) is not None and float(r['elapsed'])>=10]
    usable=[r for r in all_warm if float(r['elapsed'])<cleanup]
    duration=float(usable[-1]['elapsed'])-float(usable[0]['elapsed']) if usable else 0
    wall_clock='unity_delta_ms' in rows[0]
    summary={'label':label,'complete':(folder/'complete.txt').exists(),'rows':len(rows),'after_warmup_rows':len(usable),'elapsed_seconds':float(rows[-1]['elapsed']),'warmup_seconds':10,'cleanup_excluded_rows':len(all_warm)-len(usable),'frame_measurement':'Stopwatch wall interval' if wall_clock else 'legacy Unity deltaTime; unreliable for wall interval','metrics':{}}
    for key,scale in [('frame_ms',1),('cpu_ns',1e-6),('gpu_ns',1e-6),('gpu_timing_ms',1),('ui_ns',1e-6),('decision_ns',1e-6),('gc_alloc',1),('managed_heap',1),('total_used',1),('draw_calls',1),('batches',1),('particles',1),('simulation_ms',1),('ui_alloc',1),('step_alloc',1),('view_alloc',1),('render_alloc',1),('ui_ms',1),('step_scope_ms',1),('view_ms',1),('localization_ms',1)]:
        vs=[number(r.get(key)) for r in usable] if detailed or key!='particles' else [];vs=[v*scale for v in vs if v is not None and (v>0 or key not in ['cpu_ns','gpu_ns','gpu_timing_ms'])]
        summary['metrics'][key]={'valid':len(vs),'unavailable_or_zero':len(usable)-len(vs),'p50':percentile(vs,.5),'p95':percentile(vs,.95),'p99':percentile(vs,.99),'max':max(vs) if vs else None,'mean':sum(vs)/len(vs) if vs else None}
    summary['gc_bytes_per_second']=sum(number(r.get('gc_alloc')) or 0 for r in usable)/max(1,duration)
    if not summary['metrics']['gc_alloc']['valid']:summary['gc_bytes_per_second']=None
    summary['detailed_diagnostics']=detailed
    process_path=folder/'process-result.json'
    summary['process_identity']=json.loads(process_path.read_text(encoding='utf-8-sig')) if process_path.exists() else None
    summary['event_counts']=dict(Counter(e['event'] for e in events))
    summary['observed_group_floors']=sorted({int(e['detail']) for e in events if e['event']=='floor'})
    summary['observed_group_counts']=sorted({int(r['groups']) for r in usable})
    summary['checkpoint_phase_observations']=[]
    for checkpoint in sorted(folder.glob('checkpoint-*m.json')):
        state=json.loads(checkpoint.read_text(encoding='utf-8-sig'))
        summary['checkpoint_phase_observations'].append({'file':checkpoint.name,'groups':[{'id':g.get('id'),'floor':g.get('floor'),'phase':g.get('phase'),'terminal':g.get('terminal')} for g in state.get('groups',[])]})
    summary['phase_timing_scope']='Checkpoint phases are discrete observations; continuous battle/rest percentile partition is NOT VERIFIED because frame rows have no phase field'
    summary['native_memory']='NOT AVAILABLE; total_used is engine total used, not a native-only counter'
    summary['observer_scope']='OFF disables detailed scopes and resource enumeration; per-frame CSV/counters remain, so this is not a zero-observer product measurement'
    threshold=summary['metrics']['frame_ms']['p99']
    summary['spike_threshold_ms']=threshold
    for row in sorted((r for r in all_warm if number(r.get('frame_ms')) is not None and float(r['frame_ms'])>=threshold),key=lambda r:number(r.get('frame_ms')) or 0,reverse=True):
        t=float(row['elapsed']);start=t-float(row['frame_ms'])/1000
        near=[e for e in events if start-.1<=float(e['elapsed'])<=t+.1]
        spikes.append({'label':label,'elapsed':t,'interval_start':start,'cleanup_interval':t>=cleanup,'recorded_frame_ms':row['frame_ms'],'frame_measurement':summary['frame_measurement'],'cpu_ns':row.get('cpu_ns'),'gc_alloc':row.get('gc_alloc'),'simulation_ms':row.get('simulation_ms'),'save_ms':row.get('save_ms'),'events_in_interval_with_100ms_margin':json.dumps(near,ensure_ascii=False),'correlation_scope':'wall interval with timestamp margin; not proof of cause; save_ms can retain prior sample'})
        spikes[-1].update({k:row.get(k) if detailed or k!='particles' else None for k in ['ui_ms','step_scope_ms','view_ms','localization_ms','draw_calls','particles']})
    summaries.append(summary)
(root/('Artifacts/'+args.prefix+'_performance-summary.json')).write_text(json.dumps(summaries,ensure_ascii=False,indent=2),encoding='utf8')
if spikes:
    with (root/('Artifacts/'+args.prefix+'_frame-spikes.csv')).open('w',encoding='utf-8-sig',newline='') as out:
        writer=csv.DictWriter(out,fieldnames=list(spikes[0]));writer.writeheader();writer.writerows(spikes)
for summary in summaries:print(summary['label'],summary['complete'],summary['metrics']['cpu_ns'],summary['gc_bytes_per_second'])
