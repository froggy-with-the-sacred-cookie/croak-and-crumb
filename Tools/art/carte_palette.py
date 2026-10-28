"""Carte de palette d'une vision d'arène : des bandes de couleur proportionnelles a leur part, sans texte.

Jointe au deuxieme message d'un prompt de vision (Captures/directions/, skill direction-artistique) comme seule reference
de couleur : Gemini suit mieux une palette vue qu'une liste de codes. Les familles se lisent dans l'ordre donne
(dominante, secondaire, accent) ; un « | » dans la liste laisse un petit blanc entre deux familles.
Image generee : refuse d'ecraser sans --remplacer.

Lancer :
  uv run --with pillow --with numpy python -I Tools/art/carte_palette.py Captures/directions/x/palette_V1.png \
    F4E9D3:20 BFEDE3:20 A9B7E8:20 "|" E9C88A:10 7B6FA8:10 3E3566:10 "|" F08A7E:4 F7B7A3:3 5FE6E0:3
"""
import sys
from pathlib import Path

from PIL import Image, ImageDraw

WIDTH, HEIGHT = 1920, 240
GAP = 16  # blanc entre deux familles, en pixels
GAP_COLOR = (255, 255, 255)


def main() -> None:
    args = [a for a in sys.argv[1:] if a != "--remplacer"]
    if len(args) < 2:
        sys.exit(__doc__)
    out = Path(args[0])
    if out.exists() and "--remplacer" not in sys.argv:
        sys.exit("carte_palette : %s existe deja (--remplacer pour l'ecraser)" % out)
    bands = []
    for item in args[1:]:
        if item == "|":
            bands.append(None)
            continue
        hexa, weight = item.split(":")
        bands.append((tuple(int(hexa[i:i + 2], 16) for i in (0, 2, 4)), float(weight)))
    gaps = sum(1 for b in bands if b is None)
    total = sum(b[1] for b in bands if b is not None)
    usable = WIDTH - gaps * GAP
    image = Image.new("RGB", (WIDTH, HEIGHT), GAP_COLOR)
    draw = ImageDraw.Draw(image)
    x = 0.0
    for b in bands:
        if b is None:
            x += GAP
            continue
        w = usable * b[1] / total
        draw.rectangle([round(x), 0, round(x + w) - 1, HEIGHT - 1], fill=b[0])
        x += w
    out.parent.mkdir(parents=True, exist_ok=True)
    image.save(out)
    print("Carte de palette : %s" % out)


if __name__ == "__main__":
    main()
