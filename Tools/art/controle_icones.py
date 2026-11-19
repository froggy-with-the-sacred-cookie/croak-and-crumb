"""Planche de contrôle d'icônes du HUD : chaque icône sur fond sombre, crème et herbe, à 64 et 40 px, et en gris.

    uv run --with pillow python -I Tools/art/controle_icones.py <dossier> <sortie.jpg> cle1,cle2,…"""
import sys, os, glob
from PIL import Image, ImageDraw, ImageFont, ImageOps
dossier, out = sys.argv[1], sys.argv[2]
cles = sys.argv[3].split(",")
f = ImageFont.truetype("/System/Library/Fonts/Supplemental/Arial Bold.ttf", 16)
G, P1, P2 = 150, 64, 40
fonds = [(33, 44, 40), (236, 230, 214), (122, 170, 82)]   # sombre, crème, herbe du jardin
col_w = G * 3 + P1 + P2 + G // 2 + 60
img = Image.new("RGB", (14 + 2 * col_w, 14 + ((len(cles) + 1) // 2) * (G + 34)), (24, 30, 36))
d = ImageDraw.Draw(img)
for i, cle in enumerate(cles):
    ic = Image.open(os.path.join(dossier, cle + ".png")).convert("RGBA")
    x0 = 14 + (i % 2) * col_w; y0 = 14 + (i // 2) * (G + 34)
    d.text((x0, y0), cle, fill=(240, 235, 220), font=f); y = y0 + 22; x = x0
    for c in fonds:
        case = Image.new("RGBA", (G, G), c + (255,)); case.alpha_composite(ic.resize((G, G), Image.LANCZOS))
        img.paste(case.convert("RGB"), (x, y)); x += G
    for t in (P1, P2):
        case = Image.new("RGBA", (t, t), fonds[2] + (255,)); case.alpha_composite(ic.resize((t, t), Image.LANCZOS))
        img.paste(case.convert("RGB"), (x + 6, y + G - t)); x += t + 6
    gris = Image.new("RGBA", (G // 2, G // 2), (128, 128, 128, 255)); gris.alpha_composite(ic.resize((G // 2, G // 2), Image.LANCZOS))
    img.paste(ImageOps.grayscale(gris.convert("RGB")).convert("RGB"), (x + 12, y + G // 2))
img.save(out, quality=90); print(out, img.size)
