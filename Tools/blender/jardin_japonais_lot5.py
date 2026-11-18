"""Jardin japonais, cinquième lot (28 octobre 2026, nuit) : pierre, clôtures et plantes basses pour la vitrine agrandie.
Premiers jets proposés, pas de DA validée. Faits appliqués (fiche Docs/Recherches/jardin-japonais-kit-2026-10-28.md,
« Cinquième lot ») :
- ishibashi : une dalle de granit seule (les taillées dépassent 2 m de long et 20 cm d'épaisseur), ou des dalles
  naturelles en deux ou trois travées ;
- tour de pierre (sekitō) : un nombre impair d'étages, cinq le plus souvent ; les pierres sont posées sans mortier ;
- gorintō : cinq formes empilées, de bas en haut cube (terre), sphère (eau), pyramide (feu), demi-lune (vent), joyau
  (vide) ;
- kenninji-gaki : bambou fendu en quatre, posé serré et en double (l'écorce dehors des deux côtés), tenu par des barres
  horizontales de demi-bambou liées de cordes, un gros bambou en faîtage ;
- sode-gaki : petite clôture d'environ 1 m sur 2 m, contre un bâtiment, un poteau ou un bassin ; coin du haut arrondi,
  bordure roulée épaisse, éléments debout tenus par des lattes horizontales ;
- giboshi (hosta) et fougères, plantes d'ombre ; mousse en plaques.
Il reprend les briques et les couleurs de jardin_japonais.py sans refaire le kit :
    FROGGY_ROOT=$PWD /Applications/Blender.app/Contents/MacOS/Blender -b -P Tools/blender/jardin_japonais_lot5.py
JARDIN_SANS_EXPORT=1 : rendus seulement, sans écrire les FBX."""
import os
_kit = open(os.environ["FROGGY_ROOT"] + "/Tools/blender/jardin_japonais.py").read()
_briques, _suite = _kit.split("# ---------- fabrication, export, rendus ----------")
exec(_briques)                                                   # briques et couleurs du kit
exec(_suite.split("for ob in list(bpy.data.objects)")[0])        # photo() et boite_englobante(), sans vider la scène

P = (PIERRE["h"], PIERRE["m"], PIERRE["b"])
BAMBOU_FENDU = ((226, 210, 150), (186, 168, 112), (122, 110, 76))   # bambou passé au soleil (frais, il serait vert)
BORDURE = ((150, 112, 82), (110, 80, 62), (66, 50, 44))             # bordure roulée de brindilles
HOSTA = ((140, 196, 150), (86, 150, 114), (44, 98, 80))             # vert bleuté, plus froid que l'herbe
HOSTA_BORD = (226, 232, 178)
FOUGERE = ((168, 214, 104), (110, 168, 76), (60, 112, 58))
MOUSSE_SOL = ((92, 146, 72), (60, 108, 56), (34, 72, 44))   # plus sombre que l'herbe : clair et rond, on la prenait pour un nénuphar
LAVANDE = ((206, 186, 238), (160, 136, 206), (100, 84, 150))


def mousse_dessus(bm, rnd, part, echelle=4.0, tons=P):
    bm.normal_update()
    peindre(bm, lambda f: ombre(f.normal, *MOUSSE, rnd, .06) if f.normal.z > .3 and noise.noise(f.calc_center_median() * echelle) > part
            else ombre(f.normal, *tons, rnd, .04))
    return bm


def dalle(rnd, L, l, e, centre, biseau=.03):
    """Une pierre taillée en dalle : boîte aux arêtes biseautées, biseaux plus clairs."""
    bm = boite((L, l, e), centre); arete = bm.faces.layers.int.new("arete")
    for f in bm.faces: f[arete] = 2
    res = bmesh.ops.bevel(bm, geom=bm.edges[:], offset=biseau, segments=1, affect="EDGES", profile=.5, clamp_overlap=True)
    for f in res["faces"]: f[arete] = 1
    bm.normal_update(); peindre(bm, lambda f: couleur_roche(f, rnd, PIERRE, arete)); return bm


def pierre_plate(rnd, taille, haut, centre):
    """Une pierre naturelle au dessus plat, posée à `haut` (son dessus), centrée en x, y."""
    bm = pierre(rnd, 14, taille, alea=.14, sub=2); couper(bm, Vector((0, 0, taille.z * 1.0)), Vector((0, 0, 1)))
    top = max(v.co.z for v in bm.verts)
    bmesh.ops.translate(bm, verts=bm.verts[:], vec=Vector((centre.x, centre.y, haut - top)))
    arete = bm.faces.layers.int.get("arete"); bm.normal_update()
    peindre(bm, lambda f: melange(couleur_roche(f, rnd, PIERRE, arete), PIERRE["arete"], .25) if f.normal.z > .95
            else couleur_roche(f, rnd, PIERRE, arete))
    return bm


def dalle_naturelle(rnd, L, l, e, haut, centre, n=16):
    """Une dalle naturelle : contour de rectangle aux coins ronds, bords irréguliers, épaisseur e, dessus plat à `haut`."""
    bm = bmesh.new(); g = rnd.uniform(0, 9); pts = []
    for i in range(n):
        a = 2 * math.pi * i / n; c, s_ = math.cos(a), math.sin(a)
        r = 1 / (abs(c) ** 3.5 + abs(s_) ** 3.5) ** (1 / 3.5)            # super-ellipse : un rectangle aux coins ronds
        k = 1 + noise.noise(Vector((c * 1.6 + g, s_ * 1.6, 0))) * .14
        pts.append((c * r * L / 2 * k, s_ * r * l / 2 * k))
    bas = [bm.verts.new((x + centre.x, y + centre.y, haut - e)) for x, y in pts]
    hautv = [bm.verts.new((x * .96 + centre.x, y * .94 + centre.y, haut)) for x, y in pts]
    bm.faces.new(hautv); bm.faces.new(bas[::-1])
    for i in range(n):
        j = (i + 1) % n; bm.faces.new((bas[i], bas[j], hautv[j], hautv[i]))
    bmesh.ops.recalc_face_normals(bm, faces=bm.faces[:]); bm.normal_update()
    peindre(bm, lambda f: melange(ombre(f.normal, *P, rnd, .04), PIERRE["arete"], .25) if f.normal.z > .95 else ombre(f.normal, *P, rnd, .05))
    return bm


def appui(rnd, x, haut, larg=.9):
    """Pierre d'appui sous le bout d'un pont, enfoncée dans la berge ou la vase."""
    return pierre_plate(rnd, Vector((.42, larg / 2 + .12, .55)), haut, Vector((x, 0, 0)))


# ---------- les éléments ----------
def ishibashi_taille(rnd, L=2.8, W=.8, haut=.22):
    """Dalle de granit taillée, posée sur deux pierres d'appui ; dessus à `haut` au-dessus du sol de la berge."""
    return assembler(dalle(rnd, L, W, .2, Vector((0, 0, haut - .1))), appui(rnd, -L / 2 + .25, haut - .2), appui(rnd, L / 2 - .25, haut - .2))


def ishibashi_naturel(rnd, haut=.22):
    """Deux dalles naturelles en deux travées, décalées l'une de l'autre, sur une pile de pierre au milieu (≈ 5 m)."""
    pieces = []
    for s, dy in ((-1, -.14), (1, .14)):
        pieces.append(dalle_naturelle(rnd, 2.7, .95, .22, haut, Vector((s * 1.25, dy, 0))))
    pile = pierre(rnd, 12, Vector((.5, .55, .55)), alea=.14, sub=2)
    couper(pile, Vector((0, 0, .55)), Vector((0, 0, 1)))
    bmesh.ops.translate(pile, verts=pile.verts[:], vec=Vector((0, 0, haut - .26 - .55 + .02)))
    pieces.append(mousse_dessus(pile, rnd, .1, 3))
    for x in (-2.45, 2.45): pieces.append(appui(rnd, x, haut - .24))
    return assembler(*pieces)


def toit_carre(w, h, releve=.05, haut_retrait=.13):
    """Un toit carré de pierre (frustum à 4 pans) aux coins relevés, base à z=0."""
    bm = bmesh.new(); bmesh.ops.create_cone(bm, cap_ends=True, segments=4, radius1=w / 2 * math.sqrt(2),
                                            radius2=(w / 2 - haut_retrait) * math.sqrt(2), depth=h)
    tourner(bm, "Z", math.pi / 4); bmesh.ops.translate(bm, verts=bm.verts[:], vec=Vector((0, 0, h / 2)))
    for v in bm.verts:
        if Vector((v.co.x, v.co.y)).length > w / 2 * math.sqrt(2) * .98: v.co.z += releve
    return bm


def sekito(rnd, etages=5):
    """Tour de pierre à cinq étages : socle, corps, cinq toits carrés aux coins relevés de plus en plus petits, séparés
    par des cales, flèche à anneaux (sōrin, de mémoire) et joyau. ~2,6 m (hypothèse)."""
    pieces = [dalle(rnd, .95, .95, .16, Vector((0, 0, .08)))]
    pieces.append(peint(boite((.64, .64, .3), Vector((0, 0, .31))), P, rnd))
    corps = boite((.46, .46, .42), Vector((0, 0, .67))); corps.normal_update()
    role = corps.faces.layers.int.new("role")                     # le calque d'abord : il invalide les faces déjà prises
    cotes = [f for f in corps.faces if abs(f.normal.z) < .1]
    res = bmesh.ops.inset_individual(corps, faces=cotes, thickness=.06, depth=-.015)
    for f in cotes: f[role] = 1
    corps.normal_update()
    peindre(corps, lambda f: ombre(f.normal, *[melange(c, (70, 74, 90), .25) for c in P], rnd) if f[role] == 1 else ombre(f.normal, *P, rnd))
    pieces.append(corps)
    z = .88
    for k in range(etages):
        w = .98 - .1 * k
        pieces.append(mousse_dessus(toit_carre(w, .15), rnd, .35, 6))
        bmesh.ops.translate(pieces[-1], verts=pieces[-1].verts[:], vec=Vector((0, 0, z)))
        z += .15
        if k < etages - 1:
            pieces.append(peint(boite((w * .48, w * .48, .1), Vector((0, 0, z + .05))), P, rnd)); z += .1
    pieces.append(peint(cylindre(.07, .07, .08, 8, Vector((0, 0, z))), P, rnd)); z += .08
    pieces.append(peint(cylindre(.04, .035, .42, 8, Vector((0, 0, z))), P, rnd))
    for k in range(5): pieces.append(peint(cylindre(.07 - k * .006, .07 - k * .006, .03, 8, Vector((0, 0, z + .05 + k * .07))), P, rnd))
    z += .42
    pieces.append(peint(boule(Vector((.08, .08, .09)), rnd, 10, 1, Vector((0, 0, z + .06))), P, rnd))
    pieces.append(peint(cylindre(.04, .004, .07, 8, Vector((0, 0, z + .12))), P, rnd))
    return assembler(*pieces)


def gorinto(rnd):
    """Gorintō, ~1,6 m : cube (terre), sphère (eau), toit pyramidal (feu), demi-lune (vent), joyau (vide)."""
    pieces = [mousse_dessus(dalle(rnd, .62, .62, .5, Vector((0, 0, .25))), rnd, .3)]
    pieces.append(peint(boule(Vector((.32, .32, .29)), rnd, 14, 2, Vector((0, 0, .5 + .27)), .06), P, rnd))
    feu = mousse_dessus(toit_carre(.7, .28, .06, .26), rnd, .3, 6)
    bmesh.ops.translate(feu, verts=feu.verts[:], vec=Vector((0, 0, 1.03))); pieces.append(feu)
    vent = boule(Vector((.21, .21, .2)), rnd, 12, 2, Vector((0, 0, 1.31 + .17)), .06)
    couper(vent, Vector((0, 0, 1.48)), Vector((0, 0, 1)))                       # la demi-lune : un bol, plat dessus
    pieces.append(peint(vent, P, rnd))
    pieces.append(peint(boule(Vector((.14, .14, .14)), rnd, 10, 1, Vector((0, 0, 1.6))), P, rnd))
    pieces.append(peint(cylindre(.07, .004, .1, 8, Vector((0, 0, 1.68))), P, rnd))
    return assembler(*pieces)


def kenninji(rnd, L=2.4, H=1.8):
    """Kenninji-gaki, un module de L : lattes de bambou fendu serrées, en double ; quatre barres de demi-bambou de chaque
    côté, liées de corde noire ; gros bambou en faîtage ; poteaux aux bouts. 1,8 m : Froggy s'y cache (hypothèse)."""
    pieces = []
    n = int(L / .06)
    for cote in (-1, 1):
        for i in range(n):
            x = -L / 2 + (i + .5) * L / n
            ton = tuple(melange(c, BAMBOU_FENDU[2], rnd.uniform(0, .16)) for c in BAMBOU_FENDU)
            pieces.append(peint(boite((L / n - .006, .028, H - .06), Vector((x, cote * .016, (H - .06) / 2))), ton, rnd, .02))
        for z in (.28, .72, 1.16, 1.56):
            pieces.append(peint(boite((L, .045, .06), Vector((0, cote * .054, z))), tuple(melange(c, BAMBOU_SEC[2], .2) for c in BAMBOU_FENDU), rnd))
            for x in [-L / 2 + .2 + k * (L - .4) / 5 for k in range(6)]:
                pieces.append(peint(boite((.035, .02, .085), Vector((x, cote * .08, z))), CORDE, rnd))
    faite = tourner(cylindre(.055, .055, L + .1, 8, Vector((0, 0, -(L + .1) / 2))), "Y", math.pi / 2)
    bmesh.ops.translate(faite, verts=faite.verts[:], vec=Vector((0, 0, H - .02))); pieces.append(peint(faite, BAMBOU_SEC, rnd))
    for x in [-L / 2 + .25 + k * (L - .5) / 3 for k in range(4)]:
        pieces.append(peint(boite((.04, .14, .14), Vector((x, 0, H - .02))), CORDE, rnd))
    for x in (-L / 2, L / 2): pieces.append(peint(cylindre(.06, .06, H + .08, 8, Vector((x, 0, 0))), BOIS, rnd))
    return assembler(*pieces)


def sode_gaki(rnd, W=1.0, H=1.9, R=.45):
    """Sode-gaki : poteau d'attache à x=0, panneau vers +x, coin du haut arrondi (rayon R) côté libre, bordure roulée
    épaisse, bambous debout serrés, trois lattes de chaque côté liées de corde."""
    pieces = [peint(cylindre(.06, .06, H + .05, 8, Vector((0, 0, 0))), BOIS, rnd)]

    def haut(x):                        # le haut du panneau à l'abscisse x (le coin arrondi)
        if x <= W - R: return H
        return H - R + math.sqrt(max(0, R * R - (x - (W - R)) ** 2))
    contour = [Vector((W, 0, .12))] + [Vector((W - R + math.cos(a) * R, 0, H - R + math.sin(a) * R))
                                         for a in (math.radians(90 * i / 6) for i in range(7))] + [Vector((.04, 0, H))]
    pieces.append(peint(tube(contour, .055), BORDURE, rnd, .06))
    pieces.append(peint(boite((W, .08, .08), Vector((W / 2, 0, .12))), BORDURE, rnd))
    x = .1
    while x < W - .04:
        h = haut(x) - .14
        pieces.append(peint(cylindre(.014, .014, h, 6, Vector((x, 0, .14))), BAMBOU_FENDU, rnd, .05)); x += .045
    for z in (.55, 1.05, 1.5):
        for s in (-1, 1):
            pieces.append(peint(boite((W - .06, .02, .035), Vector((W / 2, s * .025, z))), BAMBOU_SEC, rnd))
        for xk in (.25, .55, .8):
            if haut(xk) > z: pieces.append(peint(boite((.03, .07, .05), Vector((xk, 0, z))), CORDE, rnd))
    return assembler(*pieces)


def hosta(rnd, n=11):
    """Giboshi : une touffe de larges feuilles ovales qui s'écartent en rosette, bord plus clair ; deux hampes à
    clochettes lavande."""
    pieces = []
    for i in range(n):
        az = 2 * math.pi * i / n + rnd.uniform(-.2, .2); lg = rnd.uniform(.26, .34); incl = rnd.uniform(18, 40) if i % 2 else rnd.uniform(40, 58)
        f = boule(Vector((lg, lg * .58, .02)), rnd, 9, 1, Vector((lg + .04, 0, 0)), .1)
        tourner(f, "Y", -math.radians(incl)); tourner(f, "Z", az)
        bmesh.ops.translate(f, verts=f.verts[:], vec=Vector((0, 0, .03))); f.normal_update()
        c0 = Vector((0, 0, .03))
        peindre(f, lambda fa, lg=lg: melange(ombre(fa.normal, *HOSTA, rnd, .04), HOSTA_BORD, .55)
                if (fa.calc_center_median() - c0).length > lg * 1.7 else ombre(fa.normal, *HOSTA, rnd, .04))
        pieces.append(f)
    for k in range(2):
        az = rnd.uniform(0, 2 * math.pi); base = Vector((math.cos(az) * .06, math.sin(az) * .06, 0)); h = rnd.uniform(.6, .75)
        pieces.append(peint(cylindre(.008, .006, h, 5, base), HOSTA, rnd))
        for j in range(5):
            pieces.append(peint(boule(Vector((.022, .022, .035)), rnd, 6, 1, base + Vector((.03 * (j % 2 * 2 - 1), 0, h - .05 - j * .07))), LAVANDE, rnd))
    return assembler(*pieces)


def fougere(rnd, n=9):
    """Fougère : des frondes arquées, folioles en dents de scie des deux côtés de la tige, visibles des deux faces."""
    bm = bmesh.new()
    for i in range(n):
        az = 2 * math.pi * i / n + rnd.uniform(-.25, .25); lg = rnd.uniform(.5, .7); leve = rnd.uniform(.45, .65)
        d = Vector((math.cos(az), math.sin(az), 0)); cote = Vector((-d.y, d.x, 0))
        pts = [d * lg * t + Vector((0, 0, leve * math.sin(math.pi * t * .75) + .02)) for t in (k / 12 for k in range(13))]
        for k in range(1, 12):
            p, q = pts[k], pts[k + 1]; w = .12 * (1 - k / 13) + .02
            for s in (-1, 1):
                tri = [p - (q - p) * .4, (p + q) / 2 + cote * s * w - Vector((0, 0, .02)), q]
                for ordre in (tri, tri[::-1]):                 # les deux faces : pas de contour, culling du jeu
                    bm.faces.new([bm.verts.new(v) for v in ordre])
    bm.normal_update(); peindre(bm, lambda f: ombre(f.normal if f.normal.z > 0 else -f.normal, *FOUGERE, rnd, .06))
    return bm


def mousse_plaque(rnd, R=.7, n=18):
    """Une plaque de mousse au sol (sugigoke) : contour irrégulier, légèrement bombée, quelques coussins."""
    bm = bmesh.new(); c = bm.verts.new((0, 0, .05))
    bord = []
    for i in range(n):
        a = 2 * math.pi * i / n; r = R * (1 + noise.noise(Vector((math.cos(a) * 1.3, math.sin(a) * 1.3, rnd.uniform(0, 9)))) * .35)
        bord.append(bm.verts.new((math.cos(a) * r, math.sin(a) * r, .008)))
    milieu = [bm.verts.new((v.co.x * .55, v.co.y * .55, .038)) for v in bord]
    for i in range(n):
        j = (i + 1) % n
        bm.faces.new((c, milieu[i], milieu[j])); bm.faces.new((milieu[i], bord[i], bord[j], milieu[j]))
    bm.normal_update(); peindre(bm, lambda f: ombre(f.normal, *MOUSSE_SOL, rnd, .07))
    pieces = [bm]
    for k in range(12):
        a = rnd.uniform(0, 2 * math.pi); d = rnd.uniform(0, R * .75); r = rnd.uniform(.1, .2)
        pieces.append(peint(boule(Vector((r, r * .9, r * .35)), rnd, 8, 1, Vector((math.cos(a) * d, math.sin(a) * d, .03))), MOUSSE_SOL, rnd, .07))
    return assembler(*pieces)


ELEMENTS_LOT5 = [  # nom, fabrique, épaisseur du contour
    ("Ishibashi_Taille", ishibashi_taille, .014),
    ("Ishibashi_Naturel", ishibashi_naturel, .014),
    ("Sekito", sekito, .012),
    ("Gorinto", gorinto, .012),
    ("Kenninji_Gaki", kenninji, .008),
    ("Sode_Gaki", sode_gaki, .008),
    ("Hosta", hosta, .006),
    ("Fougere", fougere, 0),
    ("Mousse_Plaque", mousse_plaque, 0),
]

EXPORTER = not os.environ.get("JARDIN_SANS_EXPORT")
objets = {}
for i, (nom, fab, ep) in enumerate(ELEMENTS_LOT5):
    rnd = random.Random(5000 + i); bm = fab(rnd)
    if ep: ajouter_contour(bm, ep, TRAIT)
    col = collection(nom); bpy.context.view_layer.layer_collection.children[nom].exclude = False
    objets[nom] = objet(nom, bm, col)
    if EXPORTER: exporter(col, os.path.join(DOSSIER, nom + ".fbx"))
    print(f"{nom:20s} {poids(col):6d} faces")

echelle = collection("Echelle"); bpy.context.view_layer.layer_collection.children["Echelle"].exclude = False
ajouter_contour(bm := froggy_echelle(random.Random(7)), .012, TRAIT); froggy = objet("Froggy_1m", bm, echelle)
for nom, ob in objets.items():
    lo, hi = boite_englobante(ob)
    photo(nom, -62, 50 if nom in ("Mousse_Plaque", "Ishibashi_Taille", "Ishibashi_Naturel") else 22, (lo + hi) / 2, max(hi - lo) * 1.35, seules=[nom])
x = 0.0
for n, ob in objets.items():
    lo, hi = boite_englobante(ob); ob.location.x += x - lo.x; x += (hi.x - lo.x) + .6
froggy.location = Vector((x + .3, 0, 0)); x += 1.2
haut = max(boite_englobante(o)[1].z for o in objets.values())
photo("planche_5", -90, 12, Vector((x / 2, 0, haut / 2)), x * 1.04, 3200, int(3200 * (haut * 1.3) / (x * 1.04)), seules=list(objets) + ["Echelle"])
for ob in objets.values(): ob.location = Vector()
bpy.context.view_layer.layer_collection.children["Echelle"].exclude = True
print("LOT 5 FINI", RENDUS_JJ)
