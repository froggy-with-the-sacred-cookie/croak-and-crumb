"""Bordure de bambous et de roseaux (28 octobre 2026, nuit) : la troisième clôture de l'arène, la plus sobre. Demande de
Florent : « juste des roseaux ou quelque chose de fin, comme des bambous, pour faire le contour ». Proposition, pas de DA.
Faits appliqués (fiche Docs/Recherches/jardin-japonais-kit-2026-10-28.md, « Bordure de bambous ») :
- bambous traçants (Phyllostachys) : de 4,5 à 9 m de haut, des cannes de 4 cm environ ; en écran, plantés à 1,5 m ou
  plus serrés (conseil de pépiniériste) ;
- roseau commun (Phragmites australis) : de 2 à 4 m, des tiges jusqu'à 2,5 cm, un plumeau de 15 à 50 cm.
Ici : cannes de 4,5 à 6,5 m, de 5 à 7 cm (un peu grossies pour se lire de loin), en deux rangs serrés ; touffes de
roseaux de 1,8 à 2,6 m au pied, plumeau brun violacé. Le sol de la vitrine s'arrête aux bornes : chaque module porte sa
bande de terre herbeuse.
Modules (le long de x, symétriques en y) : Bordure_Bambous_1, _2 (4 m), Bordure_Bambous_Angle (une touffe d'angle).
Pose : celle de la muraille de la vitrine agrandie, pièce pour pièce (la porte devient un module ordinaire) →
Assets/Froggy/Vitrine/muraille_jardin_grande_bambous.json.
    FROGGY_ROOT=$PWD /Applications/Blender.app/Contents/MacOS/Blender -b -P Tools/blender/bordure_bambous.py
JARDIN_SANS_EXPORT=1 : sans écrire les FBX."""
import os, json
_kit = open(os.environ["FROGGY_ROOT"] + "/Tools/blender/jardin_japonais.py").read()
_briques, _suite = _kit.split("# ---------- fabrication, export, rendus ----------")
exec(_briques)                                                   # briques et couleurs du kit (bambous, cylindre, boule…)
exec(_suite.split("for ob in list(bpy.data.objects)")[0])        # photo() et boite_englobante(), sans vider la scène

POSE = os.path.join(RACINE, "Assets", "Froggy", "Vitrine", "muraille_jardin_grande.json")
POSE_B = POSE.replace(".json", "_bambous.json")
ROSEAU = ((196, 196, 128), (156, 160, 100), (104, 110, 74))
PLUMEAU = ((196, 160, 160), (156, 116, 124), (104, 76, 90))
TERRE_H = ((146, 186, 100), (108, 152, 80), (70, 108, 66))


def canne(rnd, pied, haut, r, inclinaison, feuilles=5):
    """Une canne de bambou : entre-nœuds de 0,3 à 0,45 m, nœuds plus clairs, un bouquet de feuilles dans le haut.
    Rend (canne, feuilles) : le contour ne va qu'à la canne (50 modules autour de l'arène : chaque face compte)."""
    tige, fe = [], []; z = 0
    jaune = rnd.random() < .25                                   # quelques cannes plus jaunes : le bosquet n'est pas d'un seul vert
    tons = tuple(melange(c, BAMBOU_SEC[k], .45) for k, c in enumerate(BAMBOU)) if jaune else BAMBOU
    while z < haut:
        seg = rnd.uniform(.3, .45)
        tige.append(peint(cylindre(r, r * .98, seg, 5, Vector((0, 0, z))), tons, rnd, .03))
        tige.append(peint(cylindre(r * 1.14, r * 1.14, .025, 5, Vector((0, 0, z + seg - .012))), tuple(melange(c, (220, 228, 160), .4) for c in tons), rnd, .02))
        z += seg; r *= .985
    for k in range(feuilles):
        az = rnd.uniform(0, 2 * math.pi); h = z - rnd.uniform(.2, haut * .35)
        f = boule(Vector((.42, .13, .06)), rnd, 8, 1, Vector((.32, 0, 0)), .2)
        tourner(f, "Y", math.radians(rnd.uniform(15, 35))); tourner(f, "Z", az)
        bmesh.ops.translate(f, verts=f.verts[:], vec=Vector((0, 0, h)))
        fe.append(peint(f, BAMBOU_FEUILLE, rnd, .06))
    a = rnd.uniform(0, 2 * math.pi); out = []
    for bm in (assembler(*tige), assembler(*fe)):
        tourner(bm, Vector((math.cos(a), math.sin(a), 0)), math.radians(inclinaison))
        bmesh.ops.translate(bm, verts=bm.verts[:], vec=pied); out.append(bm)
    return out


def roseaux(rnd, centre, n=7):
    """Une touffe de roseaux : tiges fines, deux feuilles en lame, un plumeau au bout. Sans contour (trop fin)."""
    pieces = []
    for i in range(n):
        az = rnd.uniform(0, 2 * math.pi); d = rnd.uniform(0, .25); h = rnd.uniform(1.8, 2.6)
        pied = centre + Vector((math.cos(az) * d, math.sin(az) * d, 0))
        t = peint(cylindre(.011, .007, h, 5), ROSEAU, rnd, .04)
        f1 = cylindre(.03, .003, .7, 4)
        for v in f1.verts: v.co.y *= .2
        tourner(f1, "X", math.radians(rnd.uniform(25, 45))); tourner(f1, "Z", rnd.uniform(0, 6.3))
        bmesh.ops.translate(f1, verts=f1.verts[:], vec=Vector((0, 0, h * rnd.uniform(.35, .6))))
        p = boule(Vector((.04, .04, .16)), rnd, 8, 1, Vector((0, 0, h + .1)), .2)
        tige = assembler(t, peint(f1, ROSEAU, rnd, .05), peint(p, PLUMEAU, rnd, .05))
        tourner(tige, Vector((-math.sin(az), math.cos(az), 0)), math.radians(rnd.uniform(2, 9)))
        bmesh.ops.translate(tige, verts=tige.verts[:], vec=pied); pieces.append(tige)
    return assembler(*pieces)


def bande_de_terre(rnd, L, l):
    """La bande herbeuse sous le bosquet, à peine bombée, qui retombe un peu sur les bords."""
    bm = bmesh.new(); nx, ny = 8, 6
    vs = [[bm.verts.new((-L / 2 + L * i / nx, -l / 2 + l * j / ny, .06 * math.sin(math.pi * j / ny) - (.15 if j in (0, ny) else 0)))
           for j in range(ny + 1)] for i in range(nx + 1)]
    for i in range(nx):
        for j in range(ny): bm.faces.new((vs[i][j], vs[i + 1][j], vs[i + 1][j + 1], vs[i][j + 1]))
    bm.normal_update(); peindre(bm, lambda f: ombre(f.normal, *TERRE_H, rnd, .05))
    return bm


def module(rnd, L=4.0, l=2.0):
    """Un module de 4 m : deux rangs de cannes serrées (~0,5 m), des touffes de roseaux des deux côtés au pied."""
    gros, fins = [], []
    pts = []
    for essai in range(400):
        if len(pts) >= 12: break
        x, y = rnd.uniform(-L / 2 + .1, L / 2 - .1), rnd.uniform(-.45, .45)
        if all(math.hypot(x - a, y - b) > .42 for a, b in pts): pts.append((x, y))
    for x, y in pts:
        c, f = canne(rnd, Vector((x, y, 0)), rnd.uniform(4.5, 6.5), rnd.uniform(.028, .036), rnd.uniform(1, 5)); gros.append(c); fins.append(f)
    for s in (-1, 1):
        for x in (-1.3, 0.0, 1.3):
            fins.append(roseaux(rnd, Vector((x + rnd.uniform(-.3, .3), s * rnd.uniform(.65, .85), 0))))
    fins.append(bande_de_terre(rnd, L + .02, l + .6))
    return gros, fins


def angle(rnd):
    """Une touffe d'angle : six cannes en rond, des roseaux autour, la terre dessous."""
    gros, fins = [], []
    for k in range(7):
        a = 2 * math.pi * k / 7 + rnd.uniform(-.2, .2); d = rnd.uniform(.2, .55)
        c, f = canne(rnd, Vector((math.cos(a) * d, math.sin(a) * d, 0)), rnd.uniform(5, 7), rnd.uniform(.03, .038), rnd.uniform(2, 6)); gros.append(c); fins.append(f)
    for k in range(4):
        a = math.pi / 4 + k * math.pi / 2
        fins.append(roseaux(rnd, Vector((math.cos(a) * .9, math.sin(a) * .9, 0))))
    fins.append(bande_de_terre(rnd, 2.6, 2.6))
    return gros, fins


EXPORTER = not os.environ.get("JARDIN_SANS_EXPORT")
modeles = {}
for i, (nom, fab) in enumerate((("Bordure_Bambous_1", module), ("Bordure_Bambous_2", module), ("Bordure_Bambous_Angle", angle))):
    rnd = random.Random(8000 + i); gros, fins = fab(rnd)
    bm = assembler(*gros); ajouter_contour(bm, .008, TRAIT)
    for f in fins: fusionner(bm, f)
    col = collection(nom); bpy.context.view_layer.layer_collection.children[nom].exclude = False
    modeles[nom] = objet(nom, bm, col)
    if EXPORTER: exporter(col, os.path.join(DOSSIER, nom + ".fbx"))
    print(f"{nom:22s} {poids(col):6d} faces")

# la pose : celle de la muraille de la vitrine agrandie, pièce pour pièce
ancienne = json.load(open(POSE))["pieces"]
nouveaux = {"Muraille_Mur_1": "Bordure_Bambous_1", "Muraille_Mur_2": "Bordure_Bambous_2", "Muraille_Porte": "Bordure_Bambous_1",
            "Muraille_Angle": "Bordure_Bambous_Angle"}
pieces = [dict(p, nom=nouveaux[p["nom"]]) for p in ancienne]
if EXPORTER:
    with open(POSE_B, "w") as fh: json.dump(dict(pieces=pieces), fh, indent=1, ensure_ascii=False)
bordure = collection("Bordure_Bambous")
for p in pieces:
    ob = bpy.data.objects.new(p["nom"] + "_pose", modeles[p["nom"]].data)
    ob.matrix_world = Matrix.Translation((p["pos"][0], p["pos"][2], p["pos"][1])) @ Matrix.Rotation(math.radians(-p["lacet"]), 4, "Z")
    bordure.objects.link(ob)
for nom in modeles: bpy.context.view_layer.layer_collection.children[nom].exclude = True
print(f"bordure : {len(pieces)} pièces")

# aperçus, avec le jardin s'il est dans la scène (collection Apercu de vitrine_jardin.py)
sc = bpy.context.scene; sc.render.engine = "BLENDER_WORKBENCH"
sh = sc.display.shading; sh.light = "FLAT"; sh.color_type = "VERTEX"; sh.show_backface_culling = True
sc.render.resolution_x, sc.render.resolution_y = 1600, 900
cam = bpy.data.objects.get("Camera") or bpy.data.objects.new("Camera", bpy.data.cameras.new("Camera"))
if not cam.users_collection: sc.collection.objects.link(cam)
cam.data.type = "PERSP"; cam.data.lens = 24; cam.data.clip_end = 300; sc.camera = cam
dossier = os.path.join(RENDUS, "vitrine_grande"); os.makedirs(dossier, exist_ok=True)
for nom, oeil, cible in (("bambous_ensemble", (-2, -46, 30), (0, 0, 0)), ("bambous_de_pres", (20, -12, 1.6), (26, -16, 2.6)),
                         ("bambous_porte", (1, -10, 1.8), (0, -21, 2.4))):
    cam.location = Vector(oeil); cam.rotation_euler = (Vector(cible) - cam.location).to_track_quat("-Z", "Y").to_euler()
    for c_ in bpy.data.collections:
        for o in c_.objects: o.hide_render = c_.name not in ("Apercu", "Bordure_Bambous")
    sc.render.filepath = os.path.join(dossier, nom + ".png"); bpy.ops.render.render(write_still=True)
print("BORDURE FINIE", dossier)
