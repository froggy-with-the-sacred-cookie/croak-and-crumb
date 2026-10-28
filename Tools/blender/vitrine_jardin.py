"""Vitrine du jardin japonais : un petit terrain (étang en deux bassins, butte, sentiers) et une disposition des éléments
du kit, pour les juger en jeu. Ce n'est PAS l'arène : une proposition à remanier dans l'éditeur par l'équipe.
Faits appliqués : lanterne yukimi au bord de l'eau, iris au bord de l'eau, tsukubai au bout d'un sentier de pierres,
gravier ratissé en cercles autour des pierres dressées, beaucoup de nénuphars (page Notion « V1 »).
Se lance après jardin_japonais.py, dans la même session de Blender (il en reprend les maillages pour l'aperçu) :
    FROGGY_ROOT=$PWD /Applications/Blender.app/Contents/MacOS/Blender -b -P Tools/blender/jardin_japonais.py -P Tools/blender/vitrine_jardin.py
Écrit Vitrine_Sol.fbx et Vitrine_Eau.fbx à côté du kit, Assets/Froggy/Vitrine/disposition_jardin.json (coordonnées de
Unity : x = x de Blender, y = z, z = y ; le lacet de Unity est l'opposé de la rotation de Blender autour de z ; vérifié
par le contrôle de VitrineJardin.cs), et des aperçus dans Captures/modeles/vitrine/."""
import os, json, ast
exec(open(os.environ["FROGGY_ROOT"] + "/Tools/blender/blender_lib.py").read())
from mathutils import Matrix

DOSSIER = os.path.join(MODELES, "JardinJaponais")
DISPO = os.path.join(RACINE, "Assets", "Froggy", "Vitrine", "disposition_jardin.json")
APERCUS = os.path.join(RENDUS, "vitrine")
X0, X1, Y0, Y1, PAS = -18.0, 18.0, -14.0, 14.0, .4
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


def sol(x, y):
    s = etang(x, y)
    if s < 0: return FOND * min(1, -s / .7)
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


def bord(e, angle, vise):
    """Le point dans la direction `angle` (degrés) depuis le centre du bassin e, où etang() atteint `vise`."""
    a = math.radians(angle)
    for k in range(600):
        x, y = e[0] + math.cos(a) * k * .03, e[1] + math.sin(a) * k * .03
        if etang(x, y) >= vise: return x, y


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

# pierres de gué sur les deux premiers sentiers (le troisième reste en terre battue)
for s in sentiers[:2]:
    dist = 0.0; cote = 1
    for p, q in zip(s, s[1:]):
        dist += (q - p).length
        if dist < .8: continue
        dist = 0.0; cote = -cote; d = (q - p).normalized(); n = Vector((-d.y, d.x, 0))
        x, y = (q + n * .18 * cote).x, (q + n * .18 * cote).y
        if etang(x, y) > .4: poser(f"PierreGue_{rnd.randint(1, 3)}", x, y, z=sol(x, y))
print(f"{len(elements)} éléments, dont {len(nenuphars)} nénuphars et {fleurs} fleurs")

# ---------- le sol et l'eau ----------
nx, ny = int((X1 - X0) / PAS), int((Y1 - Y0) / PAS)
bm = bmesh.new()
vs = [[bm.verts.new((X0 + i * PAS, Y0 + j * PAS, sol(X0 + i * PAS, Y0 + j * PAS))) for j in range(ny + 1)] for i in range(nx + 1)]
for i in range(nx):
    for j in range(ny): bm.faces.new((vs[i][j], vs[i + 1][j], vs[i + 1][j + 1], vs[i][j + 1]))
bmesh.ops.triangulate(bm, faces=bm.faces[:]); bm.normal_update()
proches = [(p.x, p.y) for p in points_sentier]


def couleur_sol(f):
    c = f.calc_center_median(); s = etang(c.x, c.y)
    if c.z < -.35: return ombre(f.normal, *VASE, rnd, .03)
    if s < .05: return ombre(f.normal, *BERGE, rnd, .04)
    if any((px - c.x) ** 2 + (py - c.y) ** 2 < .3 for px, py in proches): return ombre(f.normal, *SENTIER, rnd, .04)
    t = noise.noise(c * .16) * .5 + .5
    col = ombre(f.normal, *[melange(HERBE[k], HERBE_JAUNE[k], t) for k in range(3)], rnd, .012)
    return melange(col, HERBE_SOMBRE, .35 * (1 - s / 1.2)) if s < 1.2 else col


peindre(bm, couleur_sol)
col_sol = collection("Vitrine_Sol"); ob_sol = objet("Vitrine_Sol", bm, col_sol)
e = bmesh.new()
x0, x1, y0, y1 = A[0] - A[2] - 1, B[0] + B[2] + 1, B[1] - B[3] - 1, A[1] + A[3] + 1
e.faces.new([e.verts.new((x, y, EAU)) for x, y in ((x0, y0), (x1, y0), (x1, y1), (x0, y1))])
peindre(e, lambda f: EAU_COUL)
col_eau = collection("Vitrine_Eau"); objet("Vitrine_Eau", e, col_eau)
if not os.environ.get("JARDIN_SANS_EXPORT"):
    exporter(col_sol, os.path.join(DOSSIER, "Vitrine_Sol.fbx")); exporter(col_eau, os.path.join(DOSSIER, "Vitrine_Eau.fbx"))
print(f"sol : {poids(col_sol)} faces")


# ---------- la disposition pour Unity ----------
def unity(x, y, z): return [round(x, 3), round(z, 3), round(y, 3)]


def vue(nom, oeil, cible): return dict(nom=nom, pos=unity(*oeil), cible=unity(*cible))


dispo = dict(
    elements=[dict(nom=el["nom"], pos=unity(el["x"], el["y"], el["z"]), lacet=round(-el["rot"], 2), echelle=round(el["ech"], 3)) for el in elements],
    depart=unity(depart.x, depart.y, sol(depart.x, depart.y)),
    bornes=[X0, X1, Y0, Y1],
    controle=unity(A[0], A[1], FOND) + unity(BUTTE[0], BUTTE[1], BUTTE[3]),   # deux points où le sol doit être à cette hauteur
    vues=[vue("troisieme_personne", (depart.x, depart.y - 4.5, 2.4), (depart.x, depart.y + 6, .5)),
          vue("ensemble", (-2, -27, 21), (0, 0, -1)),
          vue("pont", (XP + 6.5, YP - 6, 2.2), (XP, YP, .6)),
          vue("nenuphars", (A[0] - 1.5, A[1] - 7.5, 2.2), (A[0], A[1] + 1, -.3)),
          vue("gravier", (G.x - 5, G.y - 6.5, 3.2), (G.x, G.y, 0)),
          vue("cerisiers", (BUTTE[0] + 7, BUTTE[1] + 3.5, 2.4), (BUTTE[0], BUTTE[1], 1.8))])
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


apercu_rendu("dessus", (0, -.01, 60), (0, 0, 0), ortho=37, rx=1600, ry=1250)
for v in dispo["vues"]:
    oeil = Vector((v["pos"][0], v["pos"][2], v["pos"][1])); cible = Vector((v["cible"][0], v["cible"][2], v["cible"][1]))
    apercu_rendu(v["nom"], oeil, cible)
print("VITRINE FINIE", APERCUS)
