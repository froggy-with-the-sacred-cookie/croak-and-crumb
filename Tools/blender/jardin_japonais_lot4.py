"""Jardin japonais, quatrième lot (28 octobre 2026, soir) : de quoi remplir la vitrine agrandie. Premiers jets proposés,
pas de DA validée. Faits appliqués (fiche Docs/Recherches/jardin-japonais-kit-2026-10-28.md, « Quatrième lot ») :
- yatsuhashi : de larges planches posées une à une en zigzag, sur de courts pieux plantés dans la vase, au-dessus des iris ;
- kasuga-dōrō : lanterne haute et mince sur un pied ; de haut en bas hōju, ukebana, kasa à six côtés aux coins relevés
  (warabite), hibukuro à six côtés percé du soleil et de la lune, chūdai, sao rond, kiso ;
- azumaya : abri ouvert sur les quatre côtés, plan carré sur quatre poteaux posés sur des pierres, toit de chaume en
  pyramide, garde-corps sur trois côtés et une partie du quatrième ;
- karikomi : arbustes (azalées satsuki) au sommet arrondi, dont les flancs descendent droit jusqu'au sol, jamais une boule
  parfaite ; o-karikomi : une seule masse faite de plusieurs arbustes.
Il reprend les briques et les couleurs de jardin_japonais.py sans refaire le kit (les éléments déjà dans la scène restent) :
    FROGGY_ROOT=$PWD /Applications/Blender.app/Contents/MacOS/Blender -b -P Tools/blender/jardin_japonais_lot4.py
JARDIN_SANS_EXPORT=1 : rendus seulement, sans écrire les FBX."""
import os
_kit = open(os.environ["FROGGY_ROOT"] + "/Tools/blender/jardin_japonais.py").read()
_briques, _suite = _kit.split("# ---------- fabrication, export, rendus ----------")
exec(_briques)                                                   # briques et couleurs du kit
exec(_suite.split("for ob in list(bpy.data.objects)")[0])        # photo() et boite_englobante(), sans vider la scène

BOIS_GRIS = ((196, 180, 154), (150, 134, 112), (96, 86, 76))     # planches délavées par l'eau
PIEU = ((120, 98, 80), (86, 70, 60), (54, 46, 42))
POTEAU = ((178, 130, 94), (134, 96, 70), (84, 62, 50))
CHAUME = ((218, 194, 142), (170, 146, 106), (110, 94, 74))
CHAUME_COUPE = ((240, 216, 160), (204, 178, 126), (142, 120, 88))   # le bout coupé des tiges, à l'égout
FAITAGE = ((112, 96, 84), (80, 68, 62), (50, 44, 42))
AZALEE = ((124, 174, 94), (76, 128, 72), (40, 86, 58))           # petites feuilles serrées, un peu plus sombres que l'herbe
FLEURS_AZALEE = [(246, 116, 160), (252, 156, 190), (230, 84, 138)]   # satsuki rose et magenta
P = (PIERRE["h"], PIERRE["m"], PIERRE["b"])


def anneau(bm, z, h, n):
    """Le tour d'un carré de demi-côté h à la hauteur z, n segments par côté."""
    pts = [(-h + 2 * h * i / n, -h) for i in range(n)] + [(h, -h + 2 * h * i / n) for i in range(n)]
    pts += [(h - 2 * h * i / n, h) for i in range(n)] + [(-h, h - 2 * h * i / n) for i in range(n)]
    return [bm.verts.new((x, y, z)) for x, y in pts]


def pyramide(zs, demi, n):
    """Pyramide (ou bande) à base carrée, en tranches aux hauteurs zs, demi-côté demi(z) : assez de faces pour y peindre."""
    bm = bmesh.new(); anneaux = [anneau(bm, z, demi(z), n) for z in zs]
    for a, b in zip(anneaux, anneaux[1:]):
        for k in range(len(a)): bm.faces.new((a[k], a[(k + 1) % len(a)], b[(k + 1) % len(b)], b[k]))
    bm.faces.new(anneaux[0]); bm.faces.new(anneaux[-1])
    bmesh.ops.recalc_face_normals(bm, faces=bm.faces[:])
    return bm


def mousse(bm, rnd, part, echelle=3.0, tons=P):
    """Pierre à mousse : la mousse prend les dessus, par taches."""
    bm.normal_update()
    peindre(bm, lambda f: ombre(f.normal, *MOUSSE, rnd, .06) if f.normal.z > .3 and noise.noise(f.calc_center_median() * echelle) > part
            else ombre(f.normal, *tons, rnd, .04))
    return bm


# ---------- les éléments ----------
def yatsuhashi(rnd, L=2.6, W=.8, haut=.22, pieu=.8):
    """Une planche de yatsuhashi : deux planches côte à côte sur deux traverses et quatre pieux. Origine au niveau de l'eau,
    dessus à `haut`. La vitrine les pose bout à bout en zigzag (le pont entier se compose dans l'éditeur)."""
    pieces = []; e = .07
    for s in (-1, 1):
        ton = tuple(melange(c, (210, 196, 170), rnd.uniform(0, .2)) for c in BOIS_GRIS)
        pieces.append(peint(boite((L + rnd.uniform(-.05, .05), W / 2 - .02, e), Vector((rnd.uniform(-.02, .02), s * W / 4, haut - e / 2))), ton, rnd, .03))
    for x in (-L / 2 + .3, L / 2 - .3):
        pieces.append(peint(boite((.14, W + .16, .1), Vector((x, 0, haut - e - .05))), PIEU, rnd))
        for s in (-1, 1):
            pieces.append(peint(cylindre(.055, .05, pieu + haut - e - .1, 7, Vector((x, s * (W / 2 + .02), -pieu))), PIEU, rnd))
    return assembler(*pieces)


def croissant(r, n=14):
    """La lune : un croissant plat dans le plan xz, face vers -y (un seul polygone, triangulé à l'oreille)."""
    ext = [(math.cos(a) * r, math.sin(a) * r) for a in (math.radians(60 + 240 * i / n) for i in range(n + 1))]
    cx, ri = r * .42, r * .82          # le cercle qui mange le disque
    a0, a1 = math.atan2(ext[-1][1], ext[-1][0] - cx), math.atan2(ext[0][1], ext[0][0] - cx)
    if a0 < a1: a0 += 2 * math.pi
    inte = [(cx + math.cos(a) * ri, math.sin(a) * ri) for a in (a0 + (a1 - a0) * i / n for i in range(1, n))]
    bm = bmesh.new(); bm.faces.new([bm.verts.new((x, 0, z)) for x, z in ext + inte])
    bmesh.ops.triangulate(bm, faces=bm.faces[:], quad_method="BEAUTY", ngon_method="EAR_CLIP")
    return bm


def kasuga(rnd):
    """Kasuga-dōrō, ~2,2 m : kiso à lotus, sao rond à nœud, chūdai, hibukuro à six côtés (deux fenêtres de papier, un soleil
    et une lune percés, deux panneaux pleins à la place des cerfs), kasa à six côtés et coins relevés, ukebana, hōju."""
    pieces = [mousse(cylindre(.4, .34, .2, 6), rnd, .2, 4)]
    pieces.append(peint(cylindre(.26, .15, .1, 6, Vector((0, 0, .2))), P, rnd))
    for k in range(12):                                            # le lotus du kiso
        a = 2 * math.pi * k / 12; p = boule(Vector((.1, .065, .05)), rnd, 8, 1)
        tourner(p, "Y", math.radians(-18)); tourner(p, "Z", a)
        bmesh.ops.translate(p, verts=p.verts[:], vec=Vector((math.cos(a) * .27, math.sin(a) * .27, .22))); pieces.append(peint(p, P, rnd))
    pieces.append(peint(cylindre(.12, .12, .82, 10, Vector((0, 0, .3))), P, rnd))          # sao
    pieces.append(peint(cylindre(.15, .15, .07, 10, Vector((0, 0, .68))), P, rnd))         # son nœud (de mémoire)
    pieces.append(peint(cylindre(.16, .34, .12, 6, Vector((0, 0, 1.12))), P, rnd))         # chūdai
    pieces.append(peint(cylindre(.34, .34, .06, 6, Vector((0, 0, 1.24))), P, rnd))
    # hibukuro : six côtés, rangés par angle
    bm = cylindre(.25, .25, .36, 6, Vector((0, 0, 1.3))); bm.normal_update(); role = bm.faces.layers.int.new("role")
    cotes = sorted([f for f in bm.faces if abs(f.normal.z) < .1], key=lambda f: math.atan2(f.normal.y, f.normal.x))
    normales = [f.normal.copy() for f in cotes]; centres = [f.calc_center_median() for f in cotes]
    fenetres = [cotes[0], cotes[3]]
    for f in fenetres: f[role] = 1
    res = bmesh.ops.inset_individual(bm, faces=fenetres + [cotes[1], cotes[2], cotes[4], cotes[5]], thickness=.035, depth=-.02)
    for f in res["faces"]: f[role] = 0
    bm.normal_update()
    peindre(bm, lambda f: ombre(f.normal, *PAPIER, rnd, .02) if f[role] == 1 else couleur_roche(f, rnd, PIERRE))
    pieces.append(bm)
    for i, forme in ((1, "soleil"), (4, "lune")):                  # les découpes : du papier éclairé, au ras de la pierre
        if forme == "soleil": d = bmesh.new(); bmesh.ops.create_circle(d, cap_ends=True, segments=12, radius=.075)
        else: d = croissant(.08)
        d.normal_update(); d.faces.ensure_lookup_table()          # la face se tourne vers le dehors, quel que soit son sens
        bmesh.ops.rotate(d, verts=d.verts[:], cent=Vector(), matrix=d.faces[0].normal.rotation_difference(normales[i]).to_matrix())
        bmesh.ops.translate(d, verts=d.verts[:], vec=centres[i] - normales[i] * .017)
        pieces.append(peint(d, PAPIER, rnd, .02))
    # kasa : six côtés, coins relevés, une boucle (warabite) à chaque coin
    kasa = assembler(cylindre(.48, .48, .045, 6, Vector((0, 0, 1.66))), cylindre(.48, .08, .3, 6, Vector((0, 0, 1.705))))
    coins = {}
    for v in kasa.verts:
        if Vector((v.co.x, v.co.y)).length > .46:
            v.co.z += .05
            coins[(round(v.co.x, 2), round(v.co.y, 2))] = max(v.co.z, coins.get((round(v.co.x, 2), round(v.co.y, 2)), 0))
    coins = [Vector((x, y, z)) for (x, y), z in coins.items()]
    pieces.append(mousse(kasa, rnd, .45, 9))
    for c in coins:
        pieces.append(peint(boule(Vector((.05, .05, .055)), rnd, 8, 1, c + Vector((0, 0, .04)) + Vector((c.x, c.y, 0)).normalized() * .01), P, rnd))
    pieces.append(peint(cylindre(.08, .11, .06, 8, Vector((0, 0, 1.99))), P, rnd))           # ukebana
    pieces.append(peint(boule(Vector((.1, .1, .11)), rnd, 10, 1, Vector((0, 0, 2.15))), P, rnd))   # hōju
    pieces.append(peint(cylindre(.05, .006, .08, 8, Vector((0, 0, 2.23))), P, rnd))
    return assembler(*pieces)


def azumaya(rnd, S=2.5, H=2.25, R=1.95, sommet=4.35):
    """Abri ouvert : poteaux à ±S/2 sur pierres, garde-corps sur trois côtés et une partie du quatrième (l'entrée, côté −Y),
    bancs le long du fond et d'un côté, toit de chaume en pyramide (égout épais, coupe plus claire), faîte coiffé."""
    pieces = []; h = S / 2
    for sx in (-1, 1):
        for sy in (-1, 1):
            c = Vector((sx * h, sy * h, 0))
            b = pierre(rnd, 9, Vector((.26, .26, .22)), alea=.1, sub=2); couper(b, Vector((0, 0, .14)), Vector((0, 0, 1)))
            arete = b.faces.layers.int.get("arete"); b.normal_update(); peindre(b, lambda f, a=arete: couleur_roche(f, rnd, PIERRE, a))
            bmesh.ops.translate(b, verts=b.verts[:], vec=c); pieces.append(b)
            pieces.append(peint(cylindre(.1, .09, H - .14, 8, c + Vector((0, 0, .14))), POTEAU, rnd))
    for s in (-1, 1):                                               # sablières
        pieces.append(peint(boite((S + .5, .13, .16), Vector((0, s * h, H - .08))), POTEAU, rnd))
        pieces.append(peint(boite((.13, S + .5, .16), Vector((s * h, 0, H - .1))), POTEAU, rnd))

    def garde_corps(a, b):
        """Main courante, lisse basse et barreaux de a à b (au sol, en x, y)."""
        d = b - a; L = d.length; u = d.normalized(); rot = Matrix.Rotation(math.atan2(u.y, u.x), 3, "Z"); m = (a + b) / 2
        out = [peint(boite((L, .07, .07), Vector((m.x, m.y, z)), rot), POTEAU, rnd) for z in (.8, .22)]
        for k in range(1, int(L / .3)):
            p = a + u * (L * k / int(L / .3))
            out.append(peint(boite((.045, .045, .58), Vector((p.x, p.y, .51))), POTEAU, rnd))
        return out
    coins = [Vector((x, y, 0)) for x, y in ((-h, -h), (h, -h), (h, h), (-h, h))]
    pieces += garde_corps(coins[1], coins[2]) + garde_corps(coins[2], coins[3]) + garde_corps(coins[3], coins[0])
    pieces += garde_corps(coins[0], Vector((-h + .5, -h, 0))) + garde_corps(Vector((h - .5, -h, 0)), coins[1])   # l'entrée : 1,5 m
    # bancs (koshikake, hypothèse) : le long du fond (+Y) et du côté gauche (−X)
    for dims, centre in (((S - .3, .38, .05), Vector((0, h - .26, .44))), ((.38, 1.5, .05), Vector((-h + .26, .02, .44)))):
        pieces.append(peint(boite(dims, centre), BOIS_GRIS, rnd))
        long_x = dims[0] > dims[1]
        for k in (-1, 1):
            off = Vector((k * (dims[0] / 2 - .1), 0, 0)) if long_x else Vector((0, k * (dims[1] / 2 - .1), 0))
            pieces.append(peint(boite((.06, .3, .42) if long_x else (.3, .06, .42), Vector((centre.x, centre.y, .21)) + off), POTEAU, rnd))
    # toit de chaume : égout épais vertical, puis la pyramide, en bandes pour la peinture
    g = rnd.uniform(0, 9)

    def chaume(f):
        c = f.calc_center_median(); n = f.normal
        col = ombre(n, *CHAUME, rnd, .03)
        if int((c.z - H) / ((haut_chaume - H - .22) / 10)) % 2: col = melange(col, CHAUME[2], .12)   # les rangs de chaume
        col = melange(col, CHAUME[2], max(0, noise.noise(Vector((c.x * .9 + g, c.y * .9, c.z * 2)))) * .3)
        if c.z < H + .7 and noise.noise(c * 2.2 + Vector((g, 0, 0))) > .5: col = melange(col, MOUSSE[1], .35)
        return col
    egout = pyramide([H - .02, H + .22], lambda z: R, 10); egout.normal_update()
    peindre(egout, lambda f: ombre(f.normal, *CHAUME_COUPE, rnd, .04) if abs(f.normal.z) < .5 else ombre(f.normal, *CHAUME, rnd, .03))
    haut_chaume = sommet - .3
    zs = [H + .22 + (haut_chaume - H - .22) * i / 10 for i in range(11)]
    pente = pyramide(zs, lambda z: R + (.3 - R) * (z - H - .22) / (haut_chaume - H - .22), 10); pente.normal_update(); peindre(pente, chaume)
    pieces += [egout, pente]
    pieces.append(peint(boite((.66, .66, .22), Vector((0, 0, haut_chaume + .08))), FAITAGE, rnd))
    cap = bmesh.new(); bmesh.ops.create_cone(cap, cap_ends=True, segments=4, radius1=.4 * math.sqrt(2), radius2=.02, depth=.28)
    tourner(cap, "Z", math.pi / 4); bmesh.ops.translate(cap, verts=cap.verts[:], vec=Vector((0, 0, haut_chaume + .19 + .14)))
    pieces.append(peint(cap, FAITAGE, rnd))
    return assembler(*pieces)


def lobe(rnd, c, r, flanc, dome, fleurs=0.0):
    """Un arbuste taillé : couronne arrondie (à facettes) sur des flancs droits jusqu'au sol. fleurs : part des facettes
    du dessus fleuries (0 : aucune)."""
    couronne = boule(Vector((r, r * rnd.uniform(.9, 1.0), dome)), rnd, 16, 2, Vector((c.x, c.y, flanc)), .18)
    couper(couronne, Vector((c.x, c.y, flanc - .02)), Vector((0, 0, -1)))
    req = max(Vector((v.co.x - c.x, v.co.y - c.y)).length for v in couronne.verts if abs(v.co.z - flanc) < .03)
    jupe = cylindre(req * .99, req * .95, flanc, 12, Vector((c.x, c.y, 0)))
    g = rnd.uniform(0, 50)

    def couleur(f):
        n = f.normal; col = ombre(n, *AZALEE, rnd, .05)
        if fleurs and noise.noise(f.calc_center_median() * 3.2 + Vector((g, 0, 0))) > .5 - fleurs:
            fl = rnd.choice(FLEURS_AZALEE); col = ombre(n, fl, fl, melange(fl, (150, 60, 100), .45), rnd, .04)
        return col
    couronne.normal_update(); peindre(couronne, couleur)
    jupe.normal_update(); peindre(jupe, couleur)
    return assembler(couronne, jupe)


def karikomi(rnd, lobes, fleurs=0.0):
    return assembler(*[lobe(rnd, Vector((x, y, 0)), r, fl, d, fleurs) for x, y, r, fl, d in lobes])


O_KARIKOMI = [(-1.35, .05, .62, .5, .42), (-.45, .3, .78, .68, .52), (.55, -.1, .82, .78, .56), (1.45, .22, .6, .52, .42),
              (.15, .78, .52, .4, .38), (-.1, -.7, .5, .35, .34)]

ELEMENTS_LOT4 = [  # nom, fabrique, épaisseur du contour
    ("Yatsuhashi_Planche", yatsuhashi, .012),
    ("Lanterne_Kasuga", kasuga, .012),
    ("Azumaya", azumaya, .018),
    ("Karikomi_Petit", lambda r: karikomi(r, [(0, 0, .6, .26, .44)]), .014),
    ("Karikomi_Moyen", lambda r: karikomi(r, [(0, 0, .95, .42, .66)]), .016),
    ("Karikomi_Fleuri", lambda r: karikomi(r, [(0, 0, .95, .42, .66)], .8), .016),
    ("O_Karikomi", lambda r: karikomi(r, O_KARIKOMI), .018),
]

EXPORTER = not os.environ.get("JARDIN_SANS_EXPORT")
objets = {}
for i, (nom, fab, ep) in enumerate(ELEMENTS_LOT4):
    rnd = random.Random(4000 + i); bm = fab(rnd)
    if ep: ajouter_contour(bm, ep, TRAIT)
    col = collection(nom); bpy.context.view_layer.layer_collection.children[nom].exclude = False
    objets[nom] = objet(nom, bm, col)
    if EXPORTER: exporter(col, os.path.join(DOSSIER, nom + ".fbx"))
    print(f"{nom:20s} {poids(col):6d} faces")

echelle = collection("Echelle"); ajouter_contour(bm := froggy_echelle(random.Random(7)), .012, TRAIT)
froggy = objet("Froggy_1m", bm, echelle)
for nom, ob in objets.items():
    lo, hi = boite_englobante(ob)
    photo(nom, -62, 50 if nom.startswith("Yatsuhashi") else 22, (lo + hi) / 2, max(hi - lo) * 1.35, seules=[nom])
x = 0.0
for n, ob in objets.items():
    lo, hi = boite_englobante(ob); ob.location.x += x - lo.x; x += (hi.x - lo.x) + .6
froggy.location = Vector((x + .3, 0, 0)); x += 1.2
haut = max(boite_englobante(o)[1].z for o in objets.values())
photo("planche_4", -90, 12, Vector((x / 2, 0, haut / 2)), x * 1.04, 3200, int(3200 * (haut * 1.3) / (x * 1.04)), seules=list(objets) + ["Echelle"])
for ob in objets.values(): ob.location = Vector()
bpy.context.view_layer.layer_collection.children["Echelle"].exclude = True
print("LOT 4 FINI", RENDUS_JJ)
