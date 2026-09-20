#!/usr/bin/env python3
"""Widen landing platforms at route hops that leave too little margin.

The difficulty pass measured every hop on the completion route as
margin = 1 - gap/max-jump-for-that-rise. Hops under 10% are exact-max
jumps: the player gets one solution and no error tolerance. This script
grows the LANDING platform of each flagged hop (a bigger target is what
actually helps someone who under-jumps), by the amount needed to reach
the target margin, leaving position, height, route and pacing alone.

Dry-run by default; pass --apply to write.
"""
import re
import subprocess
import sys
from pathlib import Path

ROOT = Path(__file__).resolve().parents[2]
SCRIPTS = ROOT / "Assets" / "Scripts"
TARGET = 0.15          # aim a little above the 10% floor
# Only small islands get widened. Growing a big hub/arena platform by 3-5
# units would visibly reshape a level's silhouette, and the instrument
# cannot tell a deliberate hard jump from an accidental one — so the pass
# stays with the case that is unambiguous: a narrow island a child is
# being asked to land on. Platforms already this wide are reported, not
# touched.
MAX_TOUCH_WIDTH = 8.0
GRAVITY = 9.81
JUMP_V = 9.5
RUN = 8.0
SPEED_SAFETY = 0.95
LANDS = 0.3
TAKEOFF = 0.8


def jump_range(rise):
    apex = 0.5 * JUMP_V * JUMP_V / GRAVITY
    if rise > apex - 0.15:
        return None
    disc = JUMP_V * JUMP_V - 2 * GRAVITY * rise
    if disc < 0:
        return None
    t = (JUMP_V + disc ** 0.5) / GRAVITY
    return SPEED_SAFETY * RUN * min(t, 3.2)


def margin(ax, az, aw, ad, bx, bz, bw, bd, rise):
    """XZ rect distance between platform footprints, plus margin."""
    ha, hb = aw / 2, bw / 2
    da, db = ad / 2, bd / 2
    dx = abs(ax - bx) - (ha + hb) - TAKEOFF - LANDS
    dz = abs(az - bz) - (da + db) - TAKEOFF - LANDS
    gap = ((max(dx, 0) ** 2 + max(dz, 0) ** 2) ** 0.5) if (dx > 0 or dz > 0) else 0.0
    rng = jump_range(rise)
    if rng is None or rng <= 0:
        return None
    return 1.0 - gap / rng


def main():
    apply = "--apply" in sys.argv
    out = subprocess.run(
        ["sh", str(ROOT / "tools" / "LevelAudit" / "run.sh"), "--margins"],
        capture_output=True, text=True, cwd=ROOT)
    lines = out.stdout.splitlines()

    # Parse: "platform@(x, y, z) -> platform@(x, y, z)" from TIGHT lines
    hops = []
    for ln in lines:
        m = re.search(r"TIGHT (\d+)%.*?platform@\(([-\d.]+), ([-\d.]+), ([-\d.]+)\)"
                      r" -> platform@\(([-\d.]+), ([-\d.]+), ([-\d.]+)\)", ln)
        if not m:
            continue
        pct = int(m.group(1))
        ax_, ay, az = float(m.group(2)), float(m.group(3)), float(m.group(4))
        bx, by, bz = float(m.group(5)), float(m.group(6)), float(m.group(7))
        hops.append((pct, ax_, ay, az, bx, by, bz))

    print(f"parsed {len(hops)} tight hops under the band")
    if not hops:
        print("nothing to widen")
        return 0

    # Map every platform spec occurrence so we can edit by exact coords.
    # The engine reports TOP-FACE positions; specs hold CENTER positions
    # (top = center.y + size.y/2), so index by center and look up by
    # top-face minus half height.
    spec_re = re.compile(
        r"new PlatformSpec\((-?[\d.]+)f, (-?[\d.]+)f, (-?[\d.]+)f, "
        r"([\d.]+)f, ([\d.]+)f, ([\d.]+)f\)")
    files = sorted(SCRIPTS.glob("Level*.cs"))
    index = {}   # (x, y_center, z) -> list of (path, lineno, w, h, d, text)
    for path in files:
        for i, line in enumerate(path.read_text(encoding="utf-8").splitlines(), 1):
            mm = spec_re.search(line)
            if not mm:
                continue
            key = (float(mm.group(1)), float(mm.group(2)), float(mm.group(3)))
            index.setdefault(key, []).append(
                (path, i, float(mm.group(4)), float(mm.group(5)),
                 float(mm.group(6)), line))

    def find_topface(x, y_top, z):
        """Locate the spec whose top face matches an engine position."""
        for (cx, cy, cz), hits in index.items():
            if abs(cx - x) > 0.01 or abs(cz - z) > 0.01:
                continue
            for hit in hits:
                if abs((cy + hit[3] / 2) - y_top) < 0.05:
                    return hit
        return None

    edits = {}   # (path, lineno) -> (old_w, old_d, new_w, new_d, note)
    for pct, ax_, ay, az, bx, by, bz in hops:
        hit = find_topface(bx, by, bz)
        if hit is None:
            print(f"  !! landing platform not found at ({bx}, {by}, {bz})")
            continue
        path, lineno, w, h, d, text = hit
        src = find_topface(ax_, ay, az)
        sw = src[2] if src else 4.0
        sd = src[4] if src else 4.0
        rise = by - ay
        # Grow in 1-unit steps until the target margin is walked out.
        nw, nd = w, d
        for _ in range(8):
            mg = margin(ax_, az, sw, sd, bx, bz, nw, nd, rise)
            if mg is None or mg >= TARGET:
                break
            nw += 1
            nd += 1
        if (nw, nd) == (w, d):
            print(f"  - {path.name}:{lineno} already has margin once measured")
            continue
        if min(w, d) > MAX_TOUCH_WIDTH:
            print(f"  * {path.name}:{lineno} {w:g}x{d:g} is a wide hub "
                  f"({pct}%) — left alone, flagged for review")
            continue
        edits[(path, lineno)] = (w, d, nw, nd,
                                 f"{pct}% -> target {int(TARGET*100)}%")

    print(f"\n{len(edits)} platform(s) to widen")
    by_file = {}
    for (path, lineno), (w, d, nw, nd, note) in edits.items():
        by_file.setdefault(path, []).append((lineno, w, d, nw, nd, note))
        print(f"  {path.name}:{lineno}  {w:g}x{d:g} -> {nw:g}x{nd:g}   {note}")

    if not apply:
        print("\n(dry run — pass --apply to write)")
        return 0

    for path, items in by_file.items():
        text = path.read_text(encoding="utf-8")
        lines = text.splitlines()
        for lineno, w, d, nw, nd, note in items:
            old = lines[lineno - 1]
            new = old.replace(f"{w:g}f, 1f, {d:g}f)",
                              f"{nw:g}f, 1f, {nd:g}f)")
            if new == old:
                print(f"  !! no change applied at {path.name}:{lineno}")
                continue
            lines[lineno - 1] = new
        path.write_text("\n".join(lines) + "\n", encoding="utf-8")
        print(f"  wrote {path.name} ({len(items)} edit(s))")
    return 0


if __name__ == "__main__":
    sys.exit(main())
