"""Generate Marble Sort level JSONs (mirror of the in-game rules).

Rules enforced here (must match LevelDefinition.ValidateTotals):
- per-colour marble totals == per-colour sum of destinationQueue capacities
- trucks are just containers; the tile grid flattens all marbles by colour
"""
import json, os

OUT = r"d:\Marble-Sort-Container\Assets\_Project\Resources\Levels"

def interleave_queue(counts, cap):
    """counts: dict color->box count. Emit boxes round-robin across colours."""
    remaining = dict(counts)
    queue = []
    while any(v > 0 for v in remaining.values()):
        for c in counts:
            if remaining[c] > 0:
                queue.append({"color": c, "capacity": cap})
                remaining[c] -= 1
    return queue

def make_level(level_id, belt_cap, active, marbles, box_counts, box_cap, per_truck=16):
    # marbles: dict color -> total count (== box_counts[color] * box_cap)
    for c, n in marbles.items():
        assert n == box_counts[c] * box_cap, f"L{level_id} {c}: {n} != {box_counts[c]}*{box_cap}"
    # flatten interleaved so trucks carry a mix
    pool = []
    left = dict(marbles)
    while any(v > 0 for v in left.values()):
        for c in marbles:
            if left[c] > 0:
                pool.append(c)
                left[c] -= 1
    trucks = []
    for i in range(0, len(pool), per_truck):
        trucks.append({"id": f"truck_{len(trucks)+1:02d}", "balls": pool[i:i+per_truck]})
    data = {
        "levelId": level_id,
        "conveyorCapacity": belt_cap,
        "activeDestinationCount": active,
        "trucks": trucks,
        "destinationQueue": interleave_queue(box_counts, box_cap),
    }
    path = os.path.join(OUT, f"level_{level_id:03d}.json")
    with open(path, "w") as f:
        json.dump(data, f, indent=2)
    total = sum(marbles.values())
    print(f"level_{level_id:03d}: {total} marbles, {len(trucks)} trucks, "
          f"{len(data['destinationQueue'])} boxes x{box_cap}, {total // 4} stacks-of-4")

# L1 — gentle intro: 4 colours, 8 stacks
make_level(1, 15, 4,
           {"red": 8, "blue": 8, "green": 8, "yellow": 8},
           {"red": 2, "blue": 2, "green": 2, "yellow": 2}, 4)

# L2 — the reference-density board: 6 colours, 16 stacks
make_level(2, 18, 4,
           {"red": 12, "blue": 12, "green": 12, "purple": 12, "yellow": 8, "orange": 8},
           {"red": 3, "blue": 3, "green": 3, "purple": 3, "yellow": 2, "orange": 2}, 4)

# L3 — 6 colours, 18 stacks
make_level(3, 18, 4,
           {"red": 12, "blue": 12, "green": 12, "purple": 12, "yellow": 12, "orange": 12},
           {"red": 3, "blue": 3, "green": 3, "purple": 3, "yellow": 3, "orange": 3}, 4)

# L4 — hardest: 20 stacks
make_level(4, 20, 4,
           {"red": 16, "blue": 16, "green": 12, "yellow": 12, "purple": 12, "orange": 12},
           {"red": 4, "blue": 4, "green": 3, "yellow": 3, "purple": 3, "orange": 3}, 4)
