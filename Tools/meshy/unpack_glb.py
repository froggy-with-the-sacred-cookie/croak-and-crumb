"""Split Meshy GLBs into .gltf + .bin + external textures resized to MAX px.

Embedded GLB textures reach Unity uncompressed (16 MB per prop). External images go through
Unity's TextureImporter instead, so they get block-compressed and size-capped.
Usage: uv run --with pillow python unpack_glb.py <glb>...   (originals are moved to Tools/meshy/glb_original/, ignored by git)
From Glouton (October 2026): build went from 399 MB to 99 MB.
"""
import io, json, os, shutil, struct, sys
from PIL import Image

MAX = 1024
ROOT = os.path.dirname(os.path.abspath(__file__))
BACKUP = os.path.join(ROOT, "glb_original")


def unpack(path):
    data = open(path, "rb").read()
    magic, version, length = struct.unpack_from("<4sII", data, 0)
    assert magic == b"glTF", path
    off, chunks = 12, {}
    while off < length:
        clen, ctype = struct.unpack_from("<I4s", data, off)
        chunks[ctype] = data[off + 8: off + 8 + clen]
        off += 8 + clen
    gltf = json.loads(chunks[b"JSON"])
    binary = chunks[b"BIN\x00"]

    folder, base = os.path.split(path)
    name = os.path.splitext(base)[0]
    for i, img in enumerate(gltf.get("images", [])):
        view = gltf["bufferViews"][img.pop("bufferView")]
        raw = binary[view.get("byteOffset", 0): view.get("byteOffset", 0) + view["byteLength"]]
        im = Image.open(io.BytesIO(raw))
        if max(im.size) > MAX:
            im = im.resize((MAX, MAX * im.size[1] // im.size[0]), Image.LANCZOS)
        out = f"{name}_tex{i}.png"
        im.save(os.path.join(folder, out), optimize=True)
        img.pop("mimeType", None)
        img["uri"] = out

    gltf["buffers"] = [{"byteLength": len(binary), "uri": f"{name}.bin"}]
    open(os.path.join(folder, f"{name}.bin"), "wb").write(binary)
    json.dump(gltf, open(os.path.join(folder, f"{name}.gltf"), "w"))

    os.makedirs(BACKUP, exist_ok=True)
    shutil.move(path, os.path.join(BACKUP, base))
    if os.path.exists(path + ".meta"):
        os.remove(path + ".meta")
    print(name, "->", len(gltf.get("images", [])), "texture(s)")


if __name__ == "__main__":
    for p in sys.argv[1:]:
        unpack(p)
