"""Summarize bounded Portfolio RC Player receipts; raw logs stay local.
Run after the single observation session exits. It never launches a Player.
"""
from pathlib import Path
import collections, csv, hashlib, json, re, statistics

ROOT = Path(__file__).resolve().parents[1]
ART = ROOT / "Artifacts/Phase311"
ASSEMBLY = "bc6b54e40d4b549cb4a8476c974feed1d5e443041bb04e7138fac1e8e5c3e083"

def load(path):
    return json.loads(path.read_text(encoding="utf-8-sig"))

def log_counts(path):
    text = path.read_text(encoding="utf-8-sig", errors="replace")
    return {"exception_occurrences": len(re.findall(r"\b\w*Exception(?::|\b)", text)),
            "job_temp_alloc_occurrences": text.count("JobTempAlloc"),
            "error_lines": [line for line in text.splitlines() if re.search(r"\berror\b|state corruption", line, re.I)]}

ui = []
for folder in sorted(ART.glob("portfolio-rc-*-20261011")):
    layout = folder / "layout-results.txt"
    if not layout.exists():
        continue
    text = layout.read_text(encoding="utf-8-sig")
    match = re.search(r"scenarios: (\d+) / (\d+)x(\d+) / issues: (\d+)", text)
    receipt = load(folder / "process-result.json")
    logs = log_counts(folder / "player.log")
    ui.append({"label": folder.name, "scenarios": int(match[1]), "resolution": [int(match[2]), int(match[3])],
               "issues": int(match[4]), "exit_code": receipt["exitCode"], "wall_seconds": receipt["elapsed"],
               "assembly_matches": receipt["managedAssemblyHash"].lower() == ASSEMBLY,
               **{key: value for key, value in logs.items() if key != "error_lines"},
               "error_line_count": len(logs["error_lines"])})

folder = ART / "portfolio-rc-stability-30m-20261011"
receipt = load(folder / "process-result.json")
assert receipt["exitCode"] == 0 and (folder / "complete.txt").exists(), "Session did not complete; do not infer PASS"
with (folder / "frames.csv").open() as stream:
    frames = list(csv.DictReader(stream))
with (folder / "events.csv").open() as stream:
    events = list(csv.DictReader(stream))

reads = []
opened = None
for event in events:
    if event["event"] == "observer_open":
        opened = float(event["elapsed"])
    elif event["event"] == "observer_close_restore" and opened is not None:
        reads.append((opened, float(event["elapsed"])))
        opened = None

def window(first, last):
    rows = [row for row in frames if first <= float(row["elapsed"]) < last]
    intervals = [float(row["frame_ms"]) for row in rows if float(row["frame_ms"]) > 0]
    playing = [float(row["frame_ms"]) for row in rows if float(row["frame_ms"]) > 0 and
               not any(start <= float(row["elapsed"]) < end for start, end in reads)]
    return {"seconds": [first, last], "frames": len(rows), "mean_fps": 1000 / statistics.mean(intervals),
            "playing_mean_fps": 1000 / statistics.mean(playing), "median_frame_ms": statistics.median(intervals),
            "p95_frame_ms": sorted(intervals)[int(len(intervals) * .95)],
            "median_total_used_mib": statistics.median(float(row["total_used"]) / 1048576 for row in rows),
            "median_gc_used_mib": statistics.median(float(row["gc_used"]) / 1048576 for row in rows)}

windows = [window(300, 480), window(1620, 1800)]
logs = log_counts(folder / "player.log")
counts = dict(collections.Counter(event["event"] for event in events))
process_memory = [int(value) for value in re.findall(r"memory=(\d+)", (folder / "process-heartbeat.txt").read_text(encoding="utf-8-sig"))]
zip_path = ROOT / "Builds/PortfolioRC/EMBER-Windows-x64.zip"
summary = {"candidate": "0.3.1", "runtime_source_commit": "fca13ac", "metadata_commit": "ddce826",
    "assembly_sha256": ASSEMBLY, "development": False, "ui": ui,
    "ui_scenario_total": sum(row["scenarios"] for row in ui),
    "stability": {"exit_code": receipt["exitCode"], "process_wall_seconds": receipt["elapsed"],
        "sampled_seconds": float(frames[-1]["elapsed"]), "frames": len(frames), "event_counts": counts,
        "observed_runs": sorted(set(int(row["run"]) for row in frames)),
        "observed_floors": sorted(set(int(row["floor"]) for row in frames)),
        "performance_windows": windows, "peak_process_working_set_mib": max(process_memory)/1048576,
        "last_process_working_set_mib": process_memory[-1]/1048576,
        "exception_occurrences": logs["exception_occurrences"], "job_temp_alloc_occurrences": logs["job_temp_alloc_occurrences"],
        "error_line_count": len(logs["error_lines"]), "complete_receipt": (folder/"complete.txt").read_text()},
    "package": {"bytes": zip_path.stat().st_size, "sha256": hashlib.sha256(zip_path.read_bytes()).hexdigest()},
    "scope": "One 1x natural Release observation with periodic UI inspection; not a balance cohort or 120-minute soak",
    "ime": {"unicode_text_input": "PASS", "hotkey_dispatch_during_composition": "PASS", "os_candidate_window": "UNVERIFIED"}}
assert all(row["exit_code"] == 0 and row["issues"] == 0 and row["assembly_matches"]
           and row["exception_occurrences"] == 0 and row["job_temp_alloc_occurrences"] == 0
           and row["error_line_count"] == 0 for row in ui), "A UI receipt failed validation"
summary["release_gate"] = {"status": "BLOCKED", "blockers": ["Real Windows IME composition/candidate selection remains unverified"],
                           "public_release_updated": False}
output = ROOT / "Evidence/portfolio-rc-validation.json"
output.write_text(json.dumps(summary, ensure_ascii=False, indent=2)+"\n", encoding="utf-8")
print(json.dumps(summary, ensure_ascii=False, indent=2))
