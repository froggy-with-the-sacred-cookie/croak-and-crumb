"""Vitrine du jardin japonais : un petit terrain (étang en deux bassins, butte, sentiers) et une disposition des éléments
du kit, pour les juger en jeu. Ce n'est PAS l'arène : une proposition à remanier dans l'éditeur par l'équipe.
Faits appliqués : lanterne yukimi au bord de l'eau, iris au bord de l'eau, tsukubai au bout d'un sentier de pierres,
gravier ratissé en cercles autour des pierres dressées, beaucoup de nénuphars (page Notion « V1 »).
Se lance après jardin_japonais.py, dans la même session de Blender (il en reprend les maillages pour l'aperçu) :
    FROGGY_ROOT=$PWD /Applications/Blender.app/Contents/MacOS/Blender -b -P Tools/blender/jardin_japonais.py -P Tools/blender/vitrine_jardin.py
Écrit Vitrine_Sol.fbx et Vitrine_Eau.fbx à côté du kit, Assets/Froggy/Vitrine/disposition_jardin.json (coordonnées de
Unity : x = x de Blender, y = z, z = y ; le lacet de Unity est l'opposé de la rotation de Blender autour de z ; vérifié
par le contrôle de VitrineJardin.cs), et des aperçus dans Captures/modeles/vitrine/.
VITRINE=chine (28 octobre 2026, nuit) : la vitrine agrandie au fond d'une vallée : le sol monte vers une crête qui
fait le tour (0,6 m au col de la porte, au sud, jusqu'à ~5 m au nord), et la muraille de Chine (muraille_chine.py) suit
cette crête, comme elle suit les arêtes des montagnes à Simatai ou Jinshanling ; fichiers en _chine / _Chine. La crête
est une hauteur par joint de module (4 m), en pas de 0,6 m, plate sous les tours et la porte : elle part dans la
disposition (« crete », « tours », « porte ») pour que la muraille s'y pose.
VITRINE=grande (28 octobre 2026, soir) : la vitrine agrandie, 52 × 40 m au lieu de 36 × 28 (des multiples des 4 m de la
muraille), autour du même jardin, qui ne bouge pas. Elle ajoute le quatrième lot (jardin_japonais_lot4.py, à lancer
avant) : un marais aux iris traversé par un yatsuhashi à l'ouest, un plateau et son azumaya à l'est, la porte au sud
(muraille_jardin.py), des karikomi le long des murs, des lanternes kasuga près de la porte (on en pose près des portes
des sanctuaires) et au bout des sentiers. Elle écrit Vitrine_Sol_Grande.fbx, Vitrine_Eau_Grande.fbx et
disposition_jardin_grande.json : la vitrine d'origine reste telle quelle, pour comparer."""
import os, json, ast
exec(open(os.environ["FROGGY_ROOT"] + "/Tools/blender/blender_lib.py").read())
from mathutils import Matrix

CHINE = os.environ.get("VITRINE") == "chine"
GRANDE = os.environ.get("VITRINE") == "grande" or CHINE
NOM_V = "chine" if CHINE else "grande" if GRANDE else ""
DOSSIER = os.path.join(MODELES, "JardinJaponais")
DISPO = os.path.join(RACINE, "Assets", "Froggy", "Vitrine", f"disposition_jardin{'_' + NOM_V if NOM_V else ''}.json")
APERCUS = os.path.join(RENDUS, f"vitrine_{NOM_V}" if NOM_V else "vitrine")
X0, X1, Y0, Y1 = (-26.0, 26.0, -20.0, 20.0) if GRANDE else (-18.0, 18.0, -14.0, 14.0)
PAS = .4
MARGE = 9.0                                # vitrine chine : le sol au-delà des bornes, sous la muraille et dehors
NX_M, NY_M = round((X1 - X0) / 4), round((Y1 - Y0) / 4)     # modules de muraille par côté
N_M = 2 * (NX_M + NY_M)
PORTE_C = NX_M // 2                                         # la porte : au milieu du sud, au col
TOURS_C = sorted({2, NX_M - 3, NX_M + 2, NX_M + NY_M - 3, NX_M + NY_M + 2, NX_M + NY_M + NX_M // 2, 2 * NX_M + NY_M - 3,
                  2 * NX_M + NY_M + 2, N_M - 3})


def crete_cible(i):
    """La hauteur visée au joint i (le contour part du coin sud-ouest vers l'est) : un col à la porte, la crête la
    plus haute à l'opposé, trois bosses entre les deux."""
    th, tg = 2 * math.pi * i / N_M, 2 * math.pi * (PORTE_C + .5) / N_M
    return .6 + 1.9 * (1 - math.cos(th - tg)) + .35 * (1 - math.cos(3 * (th - tg)))


CRETE = [round(crete_cible(i) / .6) * .6 for i in range(N_M)]
for k in TOURS_C + [PORTE_C]: CRETE[(k + 1) % N_M] = CRETE[k]          # plat sous les tours et la porte
assert all(abs(CRETE[(i + 1) % N_M] - CRETE[i]) <= 1.2 + 1e-6 for i in range(N_M)), "crête trop raide pour les modules"
M = (-21.8, .5, 2.9, 7.6)          # le marais aux iris (vitrine agrandie) : centre x, y, demi-axes
MARAIS_FOND = -.42                 # 0 à 30 cm d'eau : les iris y ont pied
PLATEAU = (22.0, -3.0, 3.0, 5.0, .45)   # centre x, y, rayon du plat, rayon du pied, hauteur
FOND, EAU = -.6, -.12
A = (-4.5, 1.0, 6.2, 4.6)          # bassin ouest : centre x, y, demi-axes
B = [4.0, -1.8, 4.2, 3.4]          # bassin est : son x se règle pour que le pont enjambe l'étranglement
BUTTE = (-11.0, -8.5, 4.2, 1.1)    # centre x, y, rayon, hauteur
PONT = 4.4                          # portée du taiko-bashi
RAYONS = {"Nenuphar_Grand": 1.3, "Nenuphar_Moyen": .9, "Nenuphar_Petit": .6}
HERBE = ((140, 184, 96), (104, 150, 78), (64, 108, 64))         # assez sourd : sous le soleil de Unity, il sortait fluo
HERBE_JAUNE = ((164, 188, 102), (124, 156, 82), (82, 112, 66))
HERBE_SOMBRE = (78, 128, 76)
BERGE = ((158, 130, 98), (124, 100, 76), (84, 68, 56))
VASE = ((86, 112, 100), (66, 88, 82), (46, 62, 60))
SENTIER = ((224, 210, 180), (192, 176, 148), (134, 122, 102))
EAU_COUL = (110, 170, 186)
rnd = random.Random(31)


# ---------- le terrain ----------
def ellipse(x, y, e):
    return (math.hypot((x - e[0]) / e[2], (y - e[1]) / e[3]) - 1) * min(e[2], e[3])


def smin(a, b, k=1.2):
    h = max(k - abs(a - b), 0) / k
    return min(a, b) - h * h * k / 4


def etang(x, y):
    """Distance signée au bord de l'étang (négative dedans)."""
    return smin(ellipse(x, y, A), ellipse(x, y, B))


def marais(x, y): return ellipse(x, y, M) if GRANDE else 9.0


def eau(x, y): return min(etang(x, y), marais(x, y))


def crete_a(f):
    """La hauteur de la crête à l'indice de joint f (fractionnaire), entre deux joints en ligne droite."""
    i = math.floor(f); t = f - i
    return CRETE[i % N_M] * (1 - t) + CRETE[(i + 1) % N_M] * t


def talus(x, y):
    """Vitrine chine : vu de chaque côté, le sol monte vers la crête (d'autant plus loin qu'elle est haute), reste à
    sa hauteur sous la muraille, puis redescend dehors ; on garde le plus haut des quatre côtés."""
    if not CHINE: return 0.0
    cotes = ((0, NX_M, (x - X0) / (X1 - X0), y - Y0), (NX_M, NY_M, (y - Y0) / (Y1 - Y0), X1 - x),
             (NX_M + NY_M, NX_M, (X1 - x) / (X1 - X0), Y1 - y), (2 * NX_M + NY_M, NY_M, (Y1 - y) / (Y1 - Y0), x - X0))
    h = 0.0
    for i0, n, u, d in cotes:
        c = crete_a(i0 + n * min(1.0, max(0.0, u))); W = 2.5 + 1.0 * c
        if d >= W: continue
        if d >= 0: t = 1 - d / W
        elif d > -5.0: t = 1.0
        else: t = max(0.0, 1 - (-d - 5.0) / (MARGE - 5.0))
        h = max(h, c * t * t * (3 - 2 * t))
    return h


def sol(x, y):
    h = sol_jardin(x, y)
    if not CHINE: return h
    e = eau(x, y)                                     # dans l'eau, le talus ne monte qu'au ras de la rive
    return h + talus(x, y) * (1.0 if e >= 0 else max(0.0, 1 + e / .8))


def sol_jardin(x, y):
    s = etang(x, y)
    if s < 0: return FOND * min(1, -s / .7)
    m = marais(x, y)
    if m < 0: return MARAIS_FOND * min(1, -m / .6)
    if GRANDE:
        d = math.hypot(x - PLATEAU[0], y - PLATEAU[1])
        if d < PLATEAU[3]:
            t = min(1, (PLATEAU[3] - d) / (PLATEAU[3] - PLATEAU[2])); return PLATEAU[4] * t * t * (3 - 2 * t)
    d = math.hypot(x - BUTTE[0], y - BUTTE[1])
    return BUTTE[3] * (1 - (d / BUTTE[2]) ** 2) ** 2 if d < BUTTE[2] else 0.0


def largeur(x):
    dedans = [j / 20 for j in range(int(Y0 * 20), int(Y1 * 20)) if etang(x, j / 20) < 0]
    return (min(dedans), max(dedans)) if dedans else None


def etranglement():
    """La colonne la plus étroite de l'étang entre les deux bassins : (x, (y bas, y haut)), ou None si l'étang est coupé."""
    meilleur = None
    for i in range(61):
        x = A[0] + (B[0] - A[0]) * i / 60; l = largeur(x)
        if l is None: return None
        if meilleur is None or l[1] - l[0] < meilleur[1][1] - meilleur[1][0]: meilleur = (x, l)
    return meilleur


# le bassin est s'éloigne jusqu'à ce que l'étranglement laisse au pont 0,45 m de terre ferme à chaque bout
for i in range(60):
    B[0] = 4.0 + .05 * i; e = etranglement()
    if e and e[1][1] - e[1][0] <= PONT - .9: break
XP, (ybas, yhaut) = e; YP = (ybas + yhaut) / 2
print(f"pont : x={XP:.2f}, étang de {ybas:.2f} à {yhaut:.2f} ({yhaut - ybas:.2f} m), bouts au sol à "
      f"{sol(XP, YP - PONT / 2):.2f} et {sol(XP, YP + PONT / 2):.2f} m")


def bezier(p0, p1, p2, pas=.25):
    n = max(2, int(((p1 - p0).length + (p2 - p1).length) / pas))
    return [p0 * (1 - t) ** 2 + p1 * 2 * t * (1 - t) + p2 * t * t for t in (i / n for i in range(n + 1))]


def bord(e, angle, vise, distance=None):
    """Le point dans la direction `angle` (degrés) depuis le centre du bassin e, où distance() (etang par défaut)
    atteint `vise`."""
    a = math.radians(angle); distance = distance or etang
    for k in range(600):
        x, y = e[0] + math.cos(a) * k * .03, e[1] + math.sin(a) * k * .03
        if distance(x, y) >= vise: return x, y


# ---------- la disposition ----------
elements = []


def poser(nom, x, y, rot=None, ech=1.0, z=None):
    elements.append(dict(nom=nom, x=x, y=y, z=sol(x, y) if z is None else z, rot=rnd.uniform(0, 360) if rot is None else rot, ech=ech))


depart = Vector((XP, -12.0, 0))
bout_sud, bout_nord = Vector((XP, YP - PONT / 2 - .4, 0)), Vector((XP, YP + PONT / 2 + .4, 0))
T = Vector((-4.0, 8.6, 0))
while etang(T.x, T.y) < 1.2: T.y += .2
G = Vector((10.0, 8.6, 0))
sentiers = [bezier(depart, Vector((XP - 3, (depart.y + bout_sud.y) / 2, 0)), bout_sud),
            bezier(bout_nord, Vector((XP - 1, T.y - 1.2, 0)), T + Vector((0, -1.2, 0))),
            bezier(bout_nord, Vector((XP + 3, bout_nord.y + 1.5, 0)), Vector((G.x - 3.7, G.y, 0)))]
points_sentier = [p for s in sentiers for p in s]


def loin_du_sentier(x, y, d=.9): return all((p.x - x) ** 2 + (p.y - y) ** 2 > d * d for p in points_sentier)


poser("Pont_Taiko_Rouge", XP, YP, 90, z=0)
lx, ly = bord(A, 140, -.15); poser("Lanterne_Yukimi", lx, ly, z=max(sol(lx, ly), -.1))
poser("Tsukubai", T.x, T.y, 0)
sx, sy = bord(B, 0, .35); poser("ShishiOdoshi", sx, sy, 90)   # la bouche du tube (+y) tournée vers l'étang (ouest)
for ang, vise, ech in ((60, .25, .9), (235, .3, 1.1)): poser("Rocher_Mousse", *bord(A, ang, vise), ech=ech)
poser("Rocher_Mousse", *bord(B, 110, .3), ech=.7)
ILOT = Vector((A[0] - 1.8, A[1] + 1.0, 0)); poser("Rocher_Mousse", ILOT.x, ILOT.y, ech=1.25, z=FOND + .05)
for e_, ang in ((A, 285), (A, 305), (B, 250)): poser("Galets", *bord(e_, ang, .05))
for e_, angs in ((B, (205, 235, 265, 300)), (A, (195, 215, 250))):
    for ang in angs:
        x, y = bord(e_, ang, .15)
        if loin_du_sentier(x, y, 1.0) and abs(x - XP) > 1.2: poser("Iris", x, y, ech=rnd.uniform(.95, 1.25))
poser("Pin_Niwaki", *bord(A, 180, .9), 0)          # le nuage le plus long (+x) au-dessus de l'eau
poser("Pin_Niwaki", 14.6, 2.0, 200)
poser("Erable_Momiji", 12.8, -6.5); poser("Erable_Momiji", -1.2, 11.0)
for nom, x, y, ech in (("Cerisier_1", BUTTE[0], BUTTE[1], 1.0), ("Cerisier_2", 4.8, -8.8, 1.0), ("Cerisier_1", -15.2, -2.0, .85)):
    poser(nom, x, y, ech=ech); poser("Petales_Sol", x + .3, y - .4, ech=ech)
for x, y in ((-15.6, 12.4), (-11.2, 12.7), (-6.6, 12.9), (15.8, 12.3), (16.3, 7.2)): poser("Bambous", x, y)
for k in range(7): poser("Cloture_Bambou", -15.0 + 2.4 * k, 11.3, 0)
poser("Gravier_Ratisse", G.x, G.y, 0)
centres = ast.literal_eval(bpy.data.objects["Gravier_Ratisse"]["centres"])
for (cx, cy, r) in centres: poser("Rocher_Dresse", G.x + cx, G.y + cy, ech=r * .95, z=0)

# les nénuphars : beaucoup, de trois tailles, jamais sur le chemin du pont ni sur l'îlot
nenuphars = []
for essai in range(6000):
    if len(nenuphars) >= 24: break
    nom = rnd.choices(list(RAYONS), (2, 4, 4))[0]; r = RAYONS[nom]
    x, y = rnd.uniform(A[0] - A[2], B[0] + B[2]), rnd.uniform(B[1] - B[3], A[1] + A[3])
    if etang(x, y) > -(r * .8 + .2): continue
    if abs(x - XP) < r + 1.0 and abs(y - YP) < PONT / 2 + r: continue
    if math.hypot(x - ILOT.x, y - ILOT.y) < r + 1.5: continue
    if any(math.hypot(x - n[0], y - n[1]) < r + n[2] + .3 for n in nenuphars): continue
    nenuphars.append((x, y, r)); poser(nom, x, y, z=EAU - .015)
fleurs = 0
for x, y, r in rnd.sample(nenuphars, len(nenuphars)):
    a = rnd.uniform(0, 2 * math.pi); fx, fy = x + math.cos(a) * (r + .3), y + math.sin(a) * (r + .3)
    if etang(fx, fy) < -.4 and all(math.hypot(fx - n[0], fy - n[1]) > n[2] + .2 for n in nenuphars):
        poser("Fleur_Nenuphar", fx, fy, z=EAU); fleurs += 1
    if fleurs >= 7: break

def gue(chemins):
    """Des pierres de gué le long des chemins, en quinconce."""
    for s in chemins:
        dist = 0.0; cote = 1
        for p, q in zip(s, s[1:]):
            dist += (q - p).length
            if dist < .8: continue
            dist = 0.0; cote = -cote; d = (q - p).normalized(); n = Vector((-d.y, d.x, 0))
            x, y = (q + n * .18 * cote).x, (q + n * .18 * cote).y
            if eau(x, y) > .4: poser(f"PierreGue_{rnd.randint(1, 3)}", x, y, z=sol(x, y))


gue(sentiers[:2])   # le troisième reste en terre battue
print(f"{len(elements)} éléments, dont {len(nenuphars)} nénuphars et {fleurs} fleurs")

# ---------- la vitrine agrandie : ajouts au même jardin ----------
if GRANDE:
    PORTE = Vector(((X0 + X1) / 2, Y0 + 1.6, 0)); P = Vector(PLATEAU[:2] + (0,))
    # chaque nouveau sentier part d'un sentier existant : de la porte au départ d'origine, puis vers l'azumaya par le
    # sud de l'étang (en passant au large de l'érable)
    nouveaux = [bezier(PORTE, Vector((PORTE.x + 1.5, (PORTE.y + depart.y) / 2, 0)), depart)]
    nouveaux.append(bezier(sentiers[0][len(sentiers[0]) // 3], Vector((11.5, -11.0, 0)), P + Vector((-2.2, -1.2, 0))))
    # le yatsuhashi : huit planches en zigzag le long du marais, du sud au nord
    pas_y, ecart, n_pl = 2.0, .6, 8
    y0 = M[1] - pas_y * n_pl / 2
    zig = [Vector((M[0] + (ecart if k % 2 else -ecart), y0 + pas_y * k, 0)) for k in range(n_pl + 1)]
    for a, b in zip(zig, zig[1:]):
        c = (a + b) / 2; poser("Yatsuhashi_Planche", c.x, c.y, math.degrees(math.atan2(b.y - a.y, b.x - a.x)), z=EAU)
    pont_marais = [a + (b - a) * t / 6 for a, b in zip(zig, zig[1:]) for t in range(7)]
    nouveaux += [bezier(zig[0], Vector((zig[0].x + 2.5, zig[0].y - 4.5, 0)), nouveaux[0][len(nouveaux[0]) // 2]),
                 bezier(zig[-1], Vector((-14.0, 10.2, 0)), T + Vector((-1.4, -.4, 0)))]
    points_sentier += [p for s in nouveaux for p in s] + pont_marais

    def libre(x, y, d=.9): return loin_du_sentier(x, y, d) and math.hypot(x - P.x, y - P.y) > 3.4

    # iris : au bord du marais, dans 0 à 15 cm d'eau, et quelques touffes sur la berge
    n_iris = 0
    for essai in range(3000):
        if n_iris >= 22: break
        x, y = rnd.uniform(M[0] - M[2], M[0] + M[2]), rnd.uniform(M[1] - M[3], M[1] + M[3])
        if not (-.55 < marais(x, y) < .2) or not loin_du_sentier(x, y, .8): continue
        poser("Iris", x, y, ech=rnd.uniform(.9, 1.3)); n_iris += 1
    for nom, ang, vise, ech in (("Rocher_Mousse", 20, .3, .8), ("Rocher_Mousse", 200, .35, 1.0), ("Galets", 160, .05, 1.0), ("Galets", 340, .05, 1.0)):
        poser(nom, *bord(M, ang, vise, marais), ech=ech)
    for nom, ang in (("Nenuphar_Petit", 15), ("Nenuphar_Moyen", 195), ("Nenuphar_Petit", 165), ("Nenuphar_Petit", 335)):
        x, y = bord(M, ang, -1.0, marais)
        x, y = M[0] + (x - M[0]) * .55, M[1] + (y - M[1]) * .8
        if loin_du_sentier(x, y, RAYONS[nom] + .5): poser(nom, x, y, z=EAU - .015)
    # l'azumaya sur son plateau, l'entrée tournée vers le jardin (ouest) ; des karikomi autour
    poser("Azumaya", P.x, P.y, 270)
    # les lanternes kasuga : de part et d'autre de la porte, aux bouts du yatsuhashi, devant l'azumaya
    for x, y, rot in ((PORTE.x - 1.9, PORTE.y + .6, 0), (PORTE.x + 1.9, PORTE.y + .6, 0), (zig[0].x + 1.6, zig[0].y - .6, 90),
                      (zig[-1].x + 1.7, zig[-1].y + .4, 90), (P.x - 3.0, P.y + 2.3, 90)):
        poser("Lanterne_Kasuga", x, y, rot)
    for nom, x, y, rot in (
            ("O_Karikomi", 24.0, 3.2, 80), ("Karikomi_Fleuri", 24.2, -7.4, None), ("Karikomi_Moyen", 20.0, -7.6, None),
            ("Karikomi_Petit", 21.4, -8.6, None), ("Karikomi_Petit", 25.0, -5.6, None),
            ("O_Karikomi", -7.6, -18.0, 0), ("O_Karikomi", 7.8, -17.8, 180), ("Karikomi_Fleuri", -4.3, -18.3, None),
            ("Karikomi_Fleuri", 4.5, -18.2, None), ("Karikomi_Petit", -3.1, -17.0, None), ("Karikomi_Petit", 3.3, -16.9, None),
            ("O_Karikomi", -5.0, 17.7, 0), ("Karikomi_Moyen", -8.7, 17.4, None), ("Karikomi_Petit", -2.2, 16.9, None),
            ("Karikomi_Fleuri", 12.5, 17.6, None), ("Karikomi_Moyen", 14.7, 18.0, None),
            ("Karikomi_Moyen", -17.4, -9.6, None), ("Karikomi_Petit", -18.4, -10.7, None),
            ("Pin_Niwaki", -21.0, -15.6, 30), ("Pin_Niwaki", 21.2, 15.4, 150), ("Erable_Momiji", -23.0, 14.0, None),
            ("Bambous", -24.2, 17.9, None), ("Bambous", -21.6, 18.5, None), ("Bambous", 24.3, -17.9, None),
            ("Rocher_Mousse", 18.6, 1.4, None), ("Rocher_Mousse", -12.0, 16.4, None)):
        if libre(x, y, .6) or nom in ("O_Karikomi",): poser(nom, x, y, rot)
    poser("Cerisier_2", 8.0, 16.0); poser("Petales_Sol", 8.3, 15.6)

    # cinquième lot (jardin_japonais_lot5.py) : un pont de dalles naturelles (5,2 m) sur la pointe ouest de l'étang,
    # assez loin du pin pour passer sous son nuage le plus long ; agrandi s'il le faut pour garder 0,45 m de berge
    PIN = Vector(bord(A, 180, .9) + (0,))
    xi = PIN.x + 2.2; l = largeur(xi); yi = (l[0] + l[1]) / 2
    ech_pont = max(1.0, (l[1] - l[0] + .9) / 5.2)
    poser("Ishibashi_Naturel", xi, yi, 90, ech=ech_pont, z=0)
    print(f"ishibashi : x={xi:.2f}, étang de {l[1] - l[0]:.2f} m, échelle {ech_pont:.2f}")
    ish_s, ish_n = Vector((xi, yi - 2.6 * ech_pont - .3, 0)), Vector((xi, yi + 2.6 * ech_pont + .3, 0))
    pont_pierre = [ish_s + (ish_n - ish_s) * t / 10 for t in range(11)]
    lot5 = [bezier(ish_s, Vector((xi + 1.5, ish_s.y - 3.0, 0)), nouveaux[2][len(nouveaux[2]) * 2 // 3]),
            bezier(ish_n, Vector((xi + .5, ish_n.y + 2.0, 0)), sentiers[1][len(sentiers[1]) // 2])]
    points_sentier += [p for s in lot5 for p in s] + pont_pierre
    nouveaux += lot5
    # pierres : la tour au bord du marais, vue depuis le yatsuhashi ; le gorintō dans le coin des bambous
    poser("Sekito", -17.3, 5.4, 15); poser("Gorinto", -19.6, 16.4, 20)
    # kenninji-gaki : deux rangs de trois modules au sud, des caches à hauteur de grenouille (1,8 m)
    for x0, y in ((-16.6, -15.8), (12.0, -14.0)):
        for k in range(3): poser("Kenninji_Gaki", x0 + 1.2 + 2.4 * k, y, 0)
    # sode-gaki : contre l'azumaya (coin nord-ouest, vers l'ouest) et contre le tsukubai
    poser("Sode_Gaki", P.x - 1.25, P.y + 1.25, 180); poser("Sode_Gaki", T.x + .9, T.y + .35, 90)
    # plantes d'ombre : au pied des arbres, des lanternes, des pierres et des clôtures
    ombres = [(-23.0, 14.0), (-19.6, 16.4), (-17.3, 5.4), (12.8, -6.5), (-1.2, 11.0), (21.2, 15.4), (-21.0, -15.6),
              (T.x, T.y), (P.x - 1.6, P.y + 1.8), (lx, ly), (14.6, 2.0), (-15.0, -14.8), (15.6, -13.0)]
    for cx, cy in ombres:
        for k in range(3):
            a_ = rnd.uniform(0, 2 * math.pi); d = rnd.uniform(.9, 1.7); x, y = cx + math.cos(a_) * d, cy + math.sin(a_) * d
            if eau(x, y) > .3 and libre(x, y, .7):
                poser(rnd.choice(("Hosta", "Fougere", "Fougere", "Mousse_Plaque")), x, y, ech=rnd.uniform(.85, 1.2))
    gue(nouveaux)
    sentiers += nouveaux
    print(f"vitrine agrandie : {len(elements)} éléments en tout, {n_iris} iris dans le marais")

# ---------- le sol et l'eau ----------
m_ = MARGE if CHINE else 0.0                      # vitrine chine : le sol continue sous la muraille et au-delà
sx0, sy0 = X0 - m_, Y0 - m_
nx, ny = int((X1 - X0 + 2 * m_) / PAS), int((Y1 - Y0 + 2 * m_) / PAS)
bm = bmesh.new()
vs = [[bm.verts.new((sx0 + i * PAS, sy0 + j * PAS, sol(sx0 + i * PAS, sy0 + j * PAS))) for j in range(ny + 1)] for i in range(nx + 1)]
for i in range(nx):
    for j in range(ny): bm.faces.new((vs[i][j], vs[i + 1][j], vs[i + 1][j + 1], vs[i][j + 1]))
bmesh.ops.triangulate(bm, faces=bm.faces[:]); bm.normal_update()
proches = [(p.x, p.y) for s in sentiers for p in s]


def couleur_sol(f):
    c = f.calc_center_median(); s = eau(c.x, c.y)
    if c.z < -.35: return ombre(f.normal, *VASE, rnd, .03)
    if s < .05: return ombre(f.normal, *BERGE, rnd, .04)
    if any((px - c.x) ** 2 + (py - c.y) ** 2 < .3 for px, py in proches): return ombre(f.normal, *SENTIER, rnd, .04)
    t = noise.noise(c * .16) * .5 + .5
    col = ombre(f.normal, *[melange(HERBE[k], HERBE_JAUNE[k], t) for k in range(3)], rnd, .012)
    return melange(col, HERBE_SOMBRE, .35 * (1 - s / 1.2)) if s < 1.2 else col


peindre(bm, couleur_sol)
col_sol = collection("Vitrine_Sol"); ob_sol = objet("Vitrine_Sol", bm, col_sol)
e = bmesh.new()
rectangles = [(A[0] - A[2] - 1, B[0] + B[2] + 1, B[1] - B[3] - 1, A[1] + A[3] + 1)]
if GRANDE: rectangles.append((M[0] - M[2] - .6, M[0] + M[2] + .6, M[1] - M[3] - .6, M[1] + M[3] + .6))
for x0, x1, y0, y1 in rectangles:
    e.faces.new([e.verts.new((x, y, EAU)) for x, y in ((x0, y0), (x1, y0), (x1, y1), (x0, y1))])
peindre(e, lambda f: EAU_COUL)
col_eau = collection("Vitrine_Eau"); objet("Vitrine_Eau", e, col_eau)
SUFFIXE = "_Chine" if CHINE else "_Grande" if GRANDE else ""      # les objets gardent leur nom : seul le fichier change
for c in (col_sol, col_eau): bpy.context.view_layer.layer_collection.children[c.name].exclude = False   # rangées au passage d'avant
if not os.environ.get("JARDIN_SANS_EXPORT"):
    exporter(col_sol, os.path.join(DOSSIER, f"Vitrine_Sol{SUFFIXE}.fbx")); exporter(col_eau, os.path.join(DOSSIER, f"Vitrine_Eau{SUFFIXE}.fbx"))
print(f"sol : {poids(col_sol)} faces")


# ---------- la disposition pour Unity ----------
def unity(x, y, z): return [round(x, 3), round(z, 3), round(y, 3)]


def vue(nom, oeil, cible): return dict(nom=nom, pos=unity(*oeil), cible=unity(*cible))


arrivee = PORTE + Vector((0, 1.4, 0)) if GRANDE else depart   # dans la vitrine agrandie, on entre par la porte
dispo = dict(
    elements=[dict(nom=el["nom"], pos=unity(el["x"], el["y"], el["z"]), lacet=round(-el["rot"], 2), echelle=round(el["ech"], 3)) for el in elements],
    depart=unity(arrivee.x, arrivee.y, sol(arrivee.x, arrivee.y)),
    sol="Vitrine_Sol" + SUFFIXE, eau="Vitrine_Eau" + SUFFIXE,
    **(dict(crete=[round(c, 3) for c in CRETE], tours=TOURS_C, porte=PORTE_C) if CHINE else {}),
    bornes=[X0, X1, Y0, Y1],
    controle=unity(A[0], A[1], FOND) + unity(BUTTE[0], BUTTE[1], BUTTE[3]),   # deux points où le sol doit être à cette hauteur
    vues=[vue("troisieme_personne", (arrivee.x, arrivee.y - 4.5 + (2.4 if GRANDE else 0), 2.4), (arrivee.x, arrivee.y + 6, .5)),
          vue("ensemble", (-2, -40, 31) if GRANDE else (-2, -27, 21), (0, 0, -1)),
          vue("pont", (XP + 6.5, YP - 6, 2.2), (XP, YP, .6)),
          vue("nenuphars", (A[0] - 1.5, A[1] - 7.5, 2.2), (A[0], A[1] + 1, -.3)),
          vue("gravier", (G.x - 5, G.y - 6.5, 3.2), (G.x, G.y, 0)),
          vue("cerisiers", (BUTTE[0] + 7, BUTTE[1] + 3.5, 2.4), (BUTTE[0], BUTTE[1], 1.8))]
         + ([vue("ishibashi", (xi + 5.5, yi - 4.5, 2.0), (xi, yi, .1)), vue("sekito", (-13.5, 1.0, 2.0), (-17.3, 5.4, 1.4)),
             vue("kenninji", (-9.0, -11.0, 1.8), (-12.6, -15.8, .9)),
             vue("muraille", (X1 - 4.5, Y0 + 12, 1.3), (X1 + .5, Y0 + 9, 2.0)), vue("muraille_toit", (X1 - 2.2, Y0 + 14, 4.4), (X1 + .6, Y0 + 11, 2.9)),
             *([vue("crete_nord", (6.0, 2.0, 3.0), (-2.0, Y1 + 2, 6.0)), vue("porte_haut", (2.0, Y0 + 13, 3.2), (0.0, Y0 - 1, 6.5))] if CHINE else []),
             vue("marais", (M[0] + 5.5, M[1] - 9.5, 2.6), (M[0], M[1] - 1, 0)),
             vue("azumaya", (PLATEAU[0] - 7.5, PLATEAU[1] - 5, 2.4), (PLATEAU[0], PLATEAU[1], 1.6)),
             vue("porte", (PORTE.x + 1, PORTE.y + 8, 2.0), (PORTE.x, PORTE.y - 2, 2.0)),
             vue("karikomi", (-5.0, 12.8, 1.9), (-5.0, 18.0, .5))] if GRANDE else []))
os.makedirs(os.path.dirname(DISPO), exist_ok=True)
with open(DISPO, "w") as fh: json.dump(dispo, fh, indent=1, ensure_ascii=False)

# ---------- aperçu dans Blender (copies liées des maillages du kit) ----------
apercu = collection("Apercu")
for el in elements:
    m = Matrix.Translation((el["x"], el["y"], el["z"])) @ Matrix.Rotation(math.radians(el["rot"]), 4, "Z") @ Matrix.Scale(el["ech"], 4)
    for src in bpy.data.collections[el["nom"]].objects:
        ob = bpy.data.objects.new(src.name + "_v", src.data); ob.matrix_world = m @ src.matrix_world; apercu.objects.link(ob)
for src in (ob_sol, bpy.data.objects["Vitrine_Eau"]):
    ob = bpy.data.objects.new(src.name + "_v", src.data); apercu.objects.link(ob)


def apercu_rendu(nom, oeil, cible, ortho=None, rx=1600, ry=900):
    sc = bpy.context.scene; sc.render.engine = "BLENDER_WORKBENCH"
    sh = sc.display.shading; sh.light = "FLAT"; sh.color_type = "VERTEX"; sh.show_backface_culling = True
    sc.render.resolution_x, sc.render.resolution_y = rx, ry
    cam = bpy.data.objects.get("Camera") or bpy.data.objects.new("Camera", bpy.data.cameras.new("Camera"))
    if not cam.users_collection: sc.collection.objects.link(cam)
    cam.data.type = "ORTHO" if ortho else "PERSP"; cam.data.lens = 24; cam.data.clip_end = 300
    if ortho: cam.data.ortho_scale = ortho
    sc.camera = cam; cam.location = Vector(oeil)
    cam.rotation_euler = (Vector(cible) - cam.location).to_track_quat("-Z", "Y").to_euler()
    for c in bpy.data.collections:
        for o in c.objects: o.hide_render = c.name != "Apercu"
    os.makedirs(APERCUS, exist_ok=True)
    sc.render.filepath = os.path.join(APERCUS, nom + ".png"); bpy.ops.render.render(write_still=True)


apercu_rendu("dessus", (0, -.01, 60), (0, 0, 0), ortho=(X1 - X0) * 1.03, rx=1600, ry=int(1600 * (Y1 - Y0) / (X1 - X0)))
for v in dispo["vues"]:
    oeil = Vector((v["pos"][0], v["pos"][2], v["pos"][1])); cible = Vector((v["cible"][0], v["cible"][2], v["cible"][1]))
    apercu_rendu(v["nom"], oeil, cible)
print("VITRINE FINIE", APERCUS)
