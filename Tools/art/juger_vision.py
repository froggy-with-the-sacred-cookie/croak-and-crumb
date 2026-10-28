"""Juge les tirages d'une vision d'arène (Captures/directions/) avant l'avis de l'équipe.

Pour chaque image : part de la teinte dominante (sur 12 tranches de 30 degres), part de chaud, nombre de tranches
presentes, saturation moyenne, valeurs basses et hautes, ecart des valeurs. Ecrit a cote, dans un dossier
« _tests/ », la version en gris (les masses de valeur et le point focal tiennent-ils sans la couleur ?) et la version
floue (voit-on trois masses lisibles, ou une mosaique ?). N'ecrase jamais les tirages.

Lancer (PIL et numpy) :
  uv run --with pillow --with numpy python -I Tools/art/juger_vision.py Captures/directions/etape1b_visions
Reperes du premier tour (19 octobre 2026), juges monotones (Eku) : dominante 32 a 56 % sur une seule
tranche, 3 a 7 tranches, chaud 0 a 6 %.
"""
import sys
from pathlib import Path

import numpy as np
from PIL import Image, ImageFilter, ImageOps

COLORED = 0.15  # saturation sous laquelle un pixel compte comme gris
EXTS = {".png", ".jpg", ".jpeg", ".webp"}


def measure(path: Path) -> dict:
    im = Image.open(path).convert("RGB").resize((480, 270))
    hsv = np.array(im.convert("HSV")).reshape(-1, 3).astype(float)
    h, s, v = hsv[:, 0] * 360 / 255, hsv[:, 1] / 255, hsv[:, 2] / 255
    col = s > COLORED
    hist, _ = np.histogram(h[col], bins=12, range=(0, 360))
    share = hist / max(1, col.sum())
    dom = int(share.argmax())
    warm = (((h < 60) | (h > 330)) & col).sum() / max(1, col.sum())
    return {
        "dominante": share[dom] * 100, "tranche": "%d-%d" % (dom * 30, dom * 30 + 30), "chaud": warm * 100,
        "tranches": int((share > 0.02).sum()), "sat": s.mean(), "v5": np.percentile(v, 5),
        "v95": np.percentile(v, 95), "vstd": v.std(),
    }


def tests(path: Path, out: Path) -> None:
    im = Image.open(path).convert("RGB")
    out.mkdir(exist_ok=True)
    ImageOps.grayscale(im).save(out / (path.stem + "_gris.png"))
    small = im.resize((im.width // 8, im.height // 8)).filter(ImageFilter.GaussianBlur(3))
    small.resize(im.size).save(out / (path.stem + "_flou.png"))


def main() -> None:
    if len(sys.argv) < 2:
        sys.exit(__doc__)
    folder = Path(sys.argv[1])
    files = sorted(p for p in folder.iterdir() if p.suffix.lower() in EXTS and not p.stem.startswith("joindre"))
    print("%-16s %9s %8s %6s %8s %5s %5s %5s %5s" % (
        "tirage", "dominante", "tranche", "chaud", "tranches", "sat", "v5", "v95", "vstd"))
    for p in files:
        m = measure(p)
        tests(p, folder / "_tests")
        print("%-16s %8.0f%% %8s %5.1f%% %8d %5.2f %5.2f %5.2f %5.2f" % (
            p.name, m["dominante"], m["tranche"], m["chaud"], m["tranches"], m["sat"], m["v5"], m["v95"], m["vstd"]))
    print("Tests en gris et en flou : %s" % (folder / "_tests"))


if __name__ == "__main__":
    main()
