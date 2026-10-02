"""Audit completed final telemetry, retained prefixes and frozen Runtime provenance."""
import argparse, collections, csv, hashlib, json, re
from pathlib import Path

ROOT = Path(__file__).resolve().parents[1]
RAW = ROOT / 'Artifacts/Phase3'

def prefix_digest(path, last_seed):
    digest = hashlib.sha256()
    count = 0
    with path.open('rb') as source:
        for line in source:
            try:
                row = json.loads(line)
            except json.JSONDecodeError:
                break  # The preserved interruption can end in an incomplete row.
            if row['seed'] > last_seed:
                break
            digest.update(line)
            count += 1
    return count, digest.hexdigest()

def audit_profile():
    folder = RAW / 'soak-relocated-120m'
    complete = (folder / 'complete.txt').read_text(encoding='utf-8-sig')
    elapsed = float(re.search(r'elapsed=([\d.]+)', complete).group(1))
    assert elapsed >= 7200, complete
    for minute in (30, 60, 90):
        assert (folder / f'checkpoint-{minute}m.json').exists(), minute
    with (folder / 'frames.csv').open(encoding='utf-8-sig') as source:
        rows = list(csv.DictReader(source))
    valid = [r for r in rows if int(r['objects']) >= 100 and int(r['draw_calls']) > 1]
    assert float(valid[-1]['elapsed']) >= 7190, 'Rendering did not span the full session'
    times = [float(r['elapsed']) for r in valid]
    report = [f'120-minute profile evidence: {complete.strip()}',
              f'Raw rows={len(rows)}; rendered workload rows={len(valid)}; excluded={len(rows)-len(valid)}',
              f'Valid elapsed first={times[0]:.3f}, last={times[-1]:.3f}; maximum valid-sample gap={max(b-a for a,b in zip(times,times[1:])):.3f}s',
              '30/60/90-minute checkpoint files present. Counter filtering is not pixel-level screenshot validation.']
    for start, end in ((60, 1800), (1800, 3600), (3600, 7200)):
        samples = [r for r in valid if start <= float(r['elapsed']) < end]
        report.append(f'{start/60:g}–{end/60:g}min: {len(samples)} valid samples; objects max={max(int(r["objects"]) for r in samples)}, meshes max={max(int(r["meshes"]) for r in samples)}, materials max={max(int(r["materials"]) for r in samples)}')
    log = (folder / 'player.log').read_text(encoding='utf-8-sig')
    exceptions = re.findall(r'^.*(?:Exception:|NullReferenceException|MissingReferenceException).*$', log, re.MULTILINE)
    report.append(f'Managed exception lines found by log scan: {len(exceptions)}. See original log for full diagnostics.')
    rejected = [r for r in rows if int(r['objects']) < 100 or int(r['draw_calls']) <= 1]
    report.append('Excluded row elapsed values (not all startup/cleanup): ' + ', '.join(r['elapsed'] for r in rejected))
    leaks = [json.loads(line.split('##utp:', 1)[1]) for line in log.splitlines() if line.startswith('##utp:') and 'MemoryLeaks' in line]
    for leak in leaks:
        report.append('Engine exit MemoryLeaks diagnostic (not attributed to project code): ' + json.dumps(leak, ensure_ascii=False))
    (ROOT / 'Artifacts/phase3-profile-audit.txt').write_text('\n'.join(report) + '\n', encoding='utf-8')
    warnings = list(re.finditer(r'^.*JobTempAlloc.*$', log, re.MULTILINE))
    observed = json.loads((folder / 'runtime-warning-observation.json').read_text(encoding='utf-8-sig'))
    native = [f'Full 120-minute Player log JobTempAlloc warnings={len(warnings)}.',
              f'At least {observed["warningsBeforeCompletion"]} were observed while Player PID {observed["pid"]} was still rendering, before complete marker, at {observed["observedAt"]}.',
              f'Lifespan warnings={log.count("JobTempAlloc has allocations")}; remaining-allocation cleanup warnings={log.count("remaining Allocations on the JobTempAlloc")}.',
              'Remaining-allocation messages are in the PlayerConnection cleanup block. The log does not timestamp each individual warning; exact wall-clock timing is unavailable.',
              'UnityPlayer native addresses are available; full engine symbols and exact allocation caller are unavailable. Particle shutdown isolation does not prove this runtime cause.']
    native += ['Engine exit diagnostic: ' + json.dumps(leak, ensure_ascii=False) for leak in leaks]
    lines = log.splitlines()
    for i, line in enumerate(lines):
        if 'JobTempAlloc' in line:
            native.extend(['', f'Warning at original log line {i+1}:'] + lines[i:i+25])
    (ROOT / 'Artifacts/phase3-jobtemp-longrun.txt').write_text('\n'.join(native) + '\n', encoding='utf-8')
    print('\n'.join(report))

def main():
    outcomes = {}
    identities = set()
    previous = 0
    rows = 0
    with (RAW / 'final.jsonl').open(encoding='utf-8-sig') as source:
        for line in source:
            row = json.loads(line)
            seed = row['seed']
            assert previous <= seed <= previous + 1, ('seed order', previous, seed)
            assert seed not in outcomes or outcomes[seed] == row['finalResult'], ('outcome', seed)
            identity = (seed, row['run'], row['group'], row['floor'])
            assert identity not in identities, ('duplicate encounter', identity)
            identities.add(identity)
            outcomes[seed] = row['finalResult']
            previous = seed
            rows += 1
    assert set(outcomes) == set(range(1, 5001)), 'Expected exactly seeds 1–5000'
    counts = collections.Counter(outcomes.values())
    assert set(counts) <= {'TowerClear', 'Wipe'}, counts
    summary = (RAW / 'final-summary.txt').read_text(encoding='utf-8-sig')
    for key, value in [('Seeds', 5000), ('clears', counts['TowerClear']), ('wipes', counts['Wipe']), ('nonterminal', 0)]:
        assert re.search(r'\b' + key + '=' + str(value) + r'\b', summary), (key, summary)
    report = ['Final telemetry audit', f'Unique seeds: {len(outcomes)}; unique encounters: {rows}', f'Outcomes: {dict(counts)}; summary matches; no duplicate encounter identities.']
    for filename, last in [('final-interrupted.jsonl', 1949), ('final-before-streamed-resume.jsonl', 2201)]:
        original = prefix_digest(RAW / filename, last)
        retained = prefix_digest(RAW / 'final.jsonl', last)
        assert original == retained, ('retained bytes differ', filename)
        report.append(f'Retained seeds 1–{last}: exact raw bytes match {filename}; rows={retained[0]}; SHA256={retained[1]}')
    with (ROOT / 'Artifacts/phase3-runtime-manifest.csv').open(encoding='utf-8-sig') as source:
        assets = list(csv.DictReader(source))
    for asset in assets:
        path = ROOT / asset['path']
        assert path.stat().st_size == int(asset['bytes']), asset['path']
        assert hashlib.sha256(path.read_bytes()).hexdigest() == asset['sha256'].lower(), asset['path']
    report.append(f'Frozen Runtime manifest: {len(assets)} files, 0 differences.')
    (ROOT / 'Artifacts/phase3-final-audit.txt').write_text('\n'.join(report) + '\n', encoding='utf-8')
    print('\n'.join(report))

if __name__ == '__main__':
    parser = argparse.ArgumentParser()
    parser.add_argument('--profile-only', action='store_true')
    args = parser.parse_args()
    if not args.profile_only:
        main()
    audit_profile()
