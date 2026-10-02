"""Summarize 1 Hz Player counters. NA is unavailable, never zero."""
import csv, json, math, statistics, argparse
from pathlib import Path
ROOT=Path(__file__).resolve().parents[1]
def summarize(label):
    folder=ROOT/'Artifacts/Phase3'/label
    with (folder/'frames.csv').open(encoding='utf-8-sig') as f:raw=list(csv.DictReader(f))
    rows=[r for r in raw if int(r['objects'])>=100 and int(r['draw_calls'])>1]
    if not rows:raise ValueError('No rendered workload samples')
    complete=(folder/'complete.txt').exists() and float(rows[-1]['elapsed'])>=7190
    rejected=len(raw)-len(rows)
    windows=[(60,1800),(1800,3600),(3600,7200)]
    results=[]
    for start,end in windows:
        sample=[r for r in rows if start<=float(r['elapsed'])<end]
        if not sample:continue
        result={'from_seconds':start,'to_seconds':end,'samples':len(sample),'last_elapsed':float(sample[-1]['elapsed'])}
        for key in ['frame_ms','cpu_ns','gpu_ns','gpu_timing_ms','gc_alloc','gc_used','gc_reserved','total_used','draw_calls','batches','managed_heap','objects','meshes','materials','particles','audio_sources','ai_ms','save_ms','telemetry_ms']:
            ordered=[float(r[key]) for r in sample if r.get(key) not in (None,'NA','')]
            values=sorted(ordered)
            if values:result[key]={'mean':statistics.mean(values),'p95':values[math.ceil(len(values)*.95)-1],'max':max(values),'first':ordered[0],'last':ordered[-1]}
            else:result[key]=None
        results.append(result)
    out=ROOT/'Artifacts'/('phase3-'+label+'-performance.json');out.write_text(json.dumps(results,indent=2),encoding='utf-8')
    text=['# Player counter summary','',f'Session: {label}; rendered samples: {len(rows)}; last rendered elapsed: {rows[-1]["elapsed"]} s; excluded startup/cleanup samples: {rejected}; complete 120 min: {complete}.', '1 Hz counter samples; warmup 60 s excluded. Percentiles describe samples, not every frame. ai_ms includes the complete simulation tick loop; serialization/telemetry columns retain the last maintenance cost. Unavailable metrics remain NA.','', '| Window | Samples | Frame p95 ms | CPU p95 ms | GPU p95 ms | GC bytes/frame mean | Used memory first→last MiB | Objects | Meshes | Materials | Audio |','|---|---:|---:|---:|---:|---:|---|---:|---:|---:|---:|']
    for r in results:
        v=lambda k,stat='p95':r[k][stat] if r[k] else float('nan')
        text.append(f'| {r["from_seconds"]/60:g}–{r["to_seconds"]/60:g} min | {r["samples"]} | {v("frame_ms"):.2f} | {v("cpu_ns")/1e6:.2f} | {v("gpu_ns")/1e6:.2f} | {v("gc_alloc","mean"):.0f} | {v("total_used","first")/2**20:.1f}→{v("total_used","last")/2**20:.1f} | {v("objects","max"):.0f} | {v("meshes","max"):.0f} | {v("materials","max"):.0f} | {v("audio_sources","max"):.0f} |')
    (out.with_suffix('.md')).write_text('\n'.join(text)+'\n',encoding='utf-8');print(out.with_suffix('.md'))
if __name__=='__main__':
    p=argparse.ArgumentParser();p.add_argument('label');a=p.parse_args();summarize(a.label)
