"""Découpe une planche d'icônes Gemini sur fond magenta (refonte du HUD, 19/11/2026).

Alpha par « magenta-itude » (min(R, B) - G), couleur démêlée du fond pour les bords et les parties transparentes ;
un seul facteur d'échelle pour toutes les icônes (leurs poids restent ceux de la planche), 512 px.

    uv run --with pillow --with numpy python -I Tools/art/decoupe_icones.py <planche.jpg> <dossier> <colonnes> <rangées> cle1,cle2,…

Les icônes du jeu vont ensuite en 256 px dans Assets/Froggy/Interface/Resources/Hud/Icones/ ; contrôle :
Tools/art/controle_icones.py (fond sombre, crème, herbe, 64 et 40 px, niveaux de gris)."""
import sys, os
import numpy as np
from PIL import Image
src, out, cols, rows = sys.argv[1], sys.argv[2], int(sys.argv[3]), int(sys.argv[4])
cles = sys.argv[5].split(",")
os.makedirs(out, exist_ok=True)
im = np.asarray(Image.open(src).convert("RGB")).astype(np.float32)
R, G, B = im[..., 0], im[..., 1], im[..., 2]
m = np.minimum(R, B) - G                      # 255 sur magenta pur, négatif sur le dessin
T0 = 40.0
a = 1.0 - np.clip((m - T0) / (255.0 - T0), 0, 1)
a[a < 0.08] = 0.0
mag = np.array([255.0, 0.0, 255.0])
aa = np.maximum(a, 1e-3)[..., None]
fg = np.clip((im - (1 - a[..., None]) * mag) / aa, 0, 255)   # démêle la couleur du fond
rgba = np.dstack([fg, a * 255]).astype(np.uint8)
H, W = a.shape; ch, cw = H / rows, W / cols
boites = []
for i, cle in enumerate(cles):
    r, c = divmod(i, cols)
    y0, y1, x0, x1 = int(r * ch), int((r + 1) * ch), int(c * cw), int((c + 1) * cw)
    cel = a[y0:y1, x0:x1]
    ys, xs = np.where(cel > 0.3)
    bx0, bx1, by0, by1 = xs.min(), xs.max() + 1, ys.min(), ys.max() + 1
    bord = (bx0 <= 2 or by0 <= 2 or bx1 >= cel.shape[1] - 2 or by1 >= cel.shape[0] - 2)
    boites.append((cle, x0 + bx0, y0 + by0, x0 + bx1, y0 + by1, bord))
plus_grand = max(max(b[3] - b[1], b[4] - b[2]) for b in boites)
TAILLE, MARGE = 512, 16
f = (TAILLE - 2 * MARGE) / plus_grand        # un seul facteur pour toutes les icônes
for cle, X0, Y0, X1, Y1, bord in boites:
    piece = Image.fromarray(rgba[Y0:Y1, X0:X1], "RGBA")
    piece = piece.resize((max(1, round((X1 - X0) * f)), max(1, round((Y1 - Y0) * f))), Image.LANCZOS)
    toile = Image.new("RGBA", (TAILLE, TAILLE), (0, 0, 0, 0))
    toile.alpha_composite(piece, ((TAILLE - piece.width) // 2, (TAILLE - piece.height) // 2))
    toile.save(os.path.join(out, cle + ".png"))
    print(cle, X1 - X0, "x", Y1 - Y0, "touche le bord de sa case" if bord else "")
