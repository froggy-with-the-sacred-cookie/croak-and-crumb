"""Remove the flat display base Meshy sometimes adds under a model (a wide disc at the very bottom).

The width profile is scanned from the bottom up: a base is a wide slab that suddenly narrows.
Triangles lying entirely inside that slab are dropped.
Usage: python3 trim_base.py [--apply] [--cut 0.03] <file.gltf>...   (dry run by default)
"""
import json, math, os, struct, sys

SCAN = 0.3       # only look at the bottom 30 % of the model
NARROW = 0.55    # the part above the base is narrower than this fraction of the base


def accessor_data(gltf, binary, idx):
    a = gltf["accessors"][idx]
    v = gltf["bufferViews"][a["bufferView"]]
    start = v.get("byteOffset", 0) + a.get("byteOffset", 0)
    comps = {"SCALAR": 1, "VEC2": 2, "VEC3": 3, "VEC4": 4}[a["type"]]
    fmt = {5126: "f", 5125: "I", 5123: "H", 5121: "B"}[a["componentType"]]
    size = struct.calcsize(fmt)
    stride = v.get("byteStride", size * comps)
    out = []
    for i in range(a["count"]):
        vals = struct.unpack_from("<" + fmt * comps, binary, start + i * stride)
        out.append(vals if comps > 1 else vals[0])
    return out


def append(gltf, binary, data, target):
    while len(binary) % 4:
        binary.append(0)
    gltf["bufferViews"].append({"buffer": 0, "byteOffset": len(binary), "byteLength": len(data), "target": target})
    binary.extend(data)
    return len(gltf["bufferViews"]) - 1


def find_cut(pos):
    ys = [p[1] for p in pos]
    y0, y1 = min(ys), max(ys)
    h = y1 - y0
    cx = (min(p[0] for p in pos) + max(p[0] for p in pos)) / 2
    cz = (min(p[2] for p in pos) + max(p[2] for p in pos)) / 2
    bins = 60
    width = [0.0] * bins
    for p in pos:
        t = (p[1] - y0) / h
        if t >= SCAN:
            continue
        b = int(t / SCAN * bins)
        width[b] = max(width[b], math.hypot(p[0] - cx, p[2] - cz))
    base = max(width[:3])
    if base <= 0:
        return None
    for b in range(2, bins):
        if 0 < width[b] < base * NARROW:
            # the slab must be thin: a base, not the lower half of the object
            cut = y0 + (b / bins) * SCAN * h
            return cut if (cut - y0) / h < 0.2 else None
    return None


def trim(path, apply):
    folder = os.path.dirname(path)
    gltf = json.load(open(path))
    bin_path = os.path.join(folder, gltf["buffers"][0]["uri"])
    binary = bytearray(open(bin_path, "rb").read())
    changed = False
    for mesh in gltf["meshes"]:
        for prim in mesh["primitives"]:
            pos = accessor_data(gltf, binary, prim["attributes"]["POSITION"])
            ys = [p[1] for p in pos]
            cut = find_cut(pos) if FORCE is None else min(ys) + FORCE * (max(ys) - min(ys))
            if cut is None:
                print(f"{os.path.basename(path)}: no base")
                continue
            idx = accessor_data(gltf, binary, prim["indices"])
            keep = []
            for i in range(0, len(idx), 3):
                tri = idx[i:i + 3]
                if max(pos[j][1] for j in tri) > cut + 1e-4:
                    keep.extend(tri)
            print(f"{os.path.basename(path)}: base below {100 * (cut - min(ys)) / (max(ys) - min(ys)):.1f}% "
                  f"of height, {len(idx) // 3 - len(keep) // 3} of {len(idx) // 3} triangles removed")
            if not apply:
                continue
            # compact: only keep vertices still used, so bounds match what is drawn
            used = sorted(set(keep))
            remap = {old: new for new, old in enumerate(used)}
            for attr, acc in list(prim["attributes"].items()):
                values = accessor_data(gltf, binary, acc)
                kept = [values[i] for i in used]
                comps = len(kept[0]) if isinstance(kept[0], tuple) else 1
                flat = [c for v in kept for c in (v if comps > 1 else (v,))]
                entry = {"bufferView": append(gltf, binary, struct.pack("<%df" % len(flat), *flat), 34962),
                         "componentType": 5126, "count": len(kept), "type": gltf["accessors"][acc]["type"]}
                if attr == "POSITION":
                    entry["min"] = [min(v[k] for v in kept) for k in range(3)]
                    entry["max"] = [max(v[k] for v in kept) for k in range(3)]
                gltf["accessors"].append(entry)
                prim["attributes"][attr] = len(gltf["accessors"]) - 1
            new_idx = [remap[i] for i in keep]
            gltf["accessors"].append({"bufferView": append(gltf, binary, struct.pack("<%dI" % len(new_idx), *new_idx), 34963),
                                      "componentType": 5125, "count": len(new_idx), "type": "SCALAR"})
            prim["indices"] = len(gltf["accessors"]) - 1
            changed = True
    if apply and changed:
        gltf["buffers"][0]["byteLength"] = len(binary)
        open(bin_path, "wb").write(binary)
        json.dump(gltf, open(path, "w"))


FORCE = None   # --cut 0.03 forces the cut at 3 % of the height

if __name__ == "__main__":
    apply = "--apply" in sys.argv
    if "--cut" in sys.argv:
        FORCE = float(sys.argv[sys.argv.index("--cut") + 1])
    for p in [a for a in sys.argv[1:] if a.endswith(".gltf")]:
        trim(p, apply)
