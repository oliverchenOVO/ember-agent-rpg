"""Validate the bounded portfolio clip; never launches a game or simulation.

Requires ffmpeg/ffprobe on PATH (or --ffmpeg-dir). The visual/privacy review is
manual: this script only checks decoding, motion, links, and asset identity.
"""
import argparse
import hashlib
import json
import re
import subprocess
from pathlib import Path

parser = argparse.ArgumentParser(description=__doc__)
parser.add_argument('--ffmpeg-dir', default='')
args = parser.parse_args()
root = Path(__file__).resolve().parents[1]
clip = root / 'Media/Portfolio/refuge-demo.mp4'
gif = root / 'Media/Portfolio/refuge-demo.gif'

def run(name, *arguments):
    executable = str(Path(args.ffmpeg_dir) / (name + '.exe')) if args.ffmpeg_dir else name
    return subprocess.check_output([executable, *map(str, arguments)], text=True)

probe = json.loads(run('ffprobe', '-v', 'error', '-count_frames', '-show_entries',
                       'stream=codec_type,width,height,nb_read_frames:format=duration',
                       '-of', 'json', clip))
video = [s for s in probe['streams'] if s['codec_type'] == 'video']
assert len(video) == 1 and len(probe['streams']) == 1, 'Expected silent video'
stream = video[0]
assert float(probe['format']['duration']) == 8 and int(stream['nb_read_frames']) == 120
assert (stream['width'], stream['height']) == (1068, 584)

def frame_hashes(crop=None):
    options = ['-vf', crop] if crop else []
    result = run('ffmpeg', '-v', 'error', '-i', clip, *options, '-f', 'framemd5', '-')
    return [line.split(',')[-1].strip() for line in result.splitlines()
            if line and not line.startswith('#')]

frames = frame_hashes()
scene = frame_hashes('crop=680:360:300:180')
assert len(set(frames)) > 100 and len(set(scene)) > 100, 'Frozen recording suspected'

documents = ['README.md', 'README.zh-TW.md', 'README.en.md', 'Docs/REVIEW_GUIDE.md',
             'Docs/INDEX.md', 'Docs/SCREENSHOTS.md']
count = 0
for document in documents:
    path = root / document
    for target in re.findall(r'!?\[[^\]]*\]\(([^)]+)\)', path.read_text(encoding='utf-8')):
        if target.startswith(('https://', 'http://', '#')):
            continue
        destination = (path.parent / target.split('#')[0]).resolve()
        receipt_path = (root / 'Evidence/portfolio-demo-validation.json').resolve()
        assert destination.exists() or destination == receipt_path, f'Broken link: {document}: {target}'
        count += 1
assert (root / 'README.md').read_bytes() == (root / 'README.zh-TW.md').read_bytes()

assets = [{'file': str(p.relative_to(root)).replace('\\', '/'),
           'bytes': p.stat().st_size, 'sha256': hashlib.sha256(p.read_bytes()).hexdigest()}
          for p in (clip, gif)]
receipt = {
    'date': '2026-10-11', 'gameplay_revision': '43a2f70',
    'player': 'Officially patched v0.3.0-portfolio Windows Player',
    'duration_seconds': 8, 'fps': 15, 'decoded_frames': len(frames),
    'unique_frames': len(set(frames)), 'unique_scene_frames': len(set(scene)),
    'scene_crop': [300, 180, 680, 360], 'size': [1068, 584],
    'audio': False, 'playback_speed': 1, 'capture': 'Continuous desktop-region capture',
    'edits': 'First 8 seconds retained; titlebar/edge cropped; MP4 encoded and GIF downsampled',
    'manual_review': 'Eight one-second samples reviewed: moving Agents, advancing clock, no other app visible',
    'limitations': 'Partial viewport; no combat clip, full UI review, balance Gate, 5000-seed or 120-minute soak',
    'local_links_checked': count, 'assets': assets,
}
(root / 'Evidence/portfolio-demo-validation.json').write_text(
    json.dumps(receipt, ensure_ascii=False, indent=2) + '\n', encoding='utf-8')
print(json.dumps(receipt, ensure_ascii=False, indent=2))
