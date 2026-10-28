"""Jardin japonais : premiers éléments du kit 3D (premiers jets proposés, 28 octobre 2026 ; pas de DA validée).
Faits appliqués (fiche Docs/Recherches/jardin-japonais-kit-2026-10-28.md) :
- nénuphar : feuille ronde à échancrure en V étroite, dessous rougeâtre ; agrandi (1,2 à 2,6 m) pour qu'une grenouille
  d'1 m y atterrisse, échancrure ramenée à 1/4 du diamètre pour garder la surface d'atterrissage ;
- pierres de gué (tobi-ishi) : pierres plates, dessus plat ;
- lanterne yukimi-dōrō : quatre pieds, boîte à lumière à six côtés, toit large hexagonal ;
- pont taiko-bashi : arche raide franchie par des marches ; rouge vermillon (sanctuaire) ou bois naturel (jardin) ;
- pin taillé en nuages (niwaki), érable, bambous, clôture yotsume-gaki (cette dernière faite de mémoire).
Lancer depuis la racine du projet :
    FROGGY_ROOT=$PWD /Applications/Blender.app/Contents/MacOS/Blender -b -P Tools/blender/jardin_japonais.py
JARDIN_SANS_EXPORT=1 : rendus seulement, sans écrire les FBX."""
import os
exec(open(os.environ["FROGGY_ROOT"] + "/Tools/blender/blender_lib.py").read())
from mathutils import Matrix

DOSSIER = os.path.join(MODELES, "JardinJaponais")
RENDUS_JJ = os.path.join(RENDUS, "jardin")
TRAIT = (40, 58, 44)            # contour vert sombre, celui des recherches de personnage (à régler avec la DA)

# tons (clair, moyen, sombre) en sRGB 0-255 ; une gamme chaud / froid : verts et gris bleutés, accents chauds
PIERRE = {"h": (186, 194, 208), "m": (142, 150, 168), "b": (92, 98, 120), "arete": (206, 212, 226)}
MOUSSE = ((156, 200, 96), (106, 154, 72), (60, 100, 60))
FEUILLE = ((146, 202, 100), (94, 160, 80), (52, 108, 66))
NERVURE = (192, 226, 132)
DESSOUS = ((182, 96, 102), (144, 66, 80), (94, 44, 62))
ROSE = ((255, 236, 240), (240, 160, 190), (180, 92, 132))
POLLEN = ((255, 226, 112), (238, 188, 64), (172, 122, 44))
PAPIER = ((250, 238, 206), (232, 206, 156), (190, 152, 112))
BOIS = ((200, 154, 108), (158, 114, 78), (100, 70, 54))
VERMILLON = ((240, 104, 66), (208, 66, 46), (132, 38, 42))
BRONZE = ((120, 112, 104), (86, 80, 76), (52, 48, 48))
ECORCE = ((146, 116, 96), (106, 82, 68), (66, 50, 48))
PIN = ((116, 168, 98), (66, 120, 76), (34, 76, 60))
ERABLE = ((252, 146, 88), (224, 84, 56), (142, 42, 50))
BAMBOU = ((178, 208, 112), (124, 170, 86), (72, 118, 66))
BAMBOU_FEUILLE = ((156, 206, 104), (98, 158, 78), (52, 104, 66))
BAMBOU_SEC = ((222, 206, 146), (182, 164, 108), (118, 106, 74))
CORDE = ((72, 62, 56), (50, 44, 40), (30, 26, 24))
ECORCE_CERISIER = ((156, 116, 110), (116, 84, 84), (72, 52, 58))     # gris rougeâtre, lenticelles horizontales
FLEURS_CERISIER = [(250, 214, 226), (255, 238, 243), (240, 176, 202)]  # rose pâle qui pâlit vers le blanc
EAU_BASSIN = ((150, 206, 214), (96, 160, 176), (60, 110, 130))
FEUILLE_IRIS = ((132, 186, 98), (80, 140, 76), (44, 94, 62))
IRIS = ((178, 132, 230), (124, 78, 188), (72, 42, 126))
JAUNE_IRIS = (252, 214, 84)
GALET = ((136, 140, 150), (98, 102, 114), (60, 62, 74))
GRAVIER = ((242, 238, 226), (206, 200, 186), (150, 146, 136))
GRAVIER_CENTRES = [(-1.8, .5, 1.15), (1.1, -.8, .95), (2.4, 1.2, .75)]   # cercles autour des pierres dressées (x, y, rayon)


# ---------- briques ----------
def peint(bm, tons, rnd, var=.04):
    bm.normal_update(); peindre(bm, lambda f: ombre(f.normal, *tons, rnd, var)); return bm


def assembler(*pieces):
    bm = bmesh.new()
    for p in pieces: fusionner(bm, p)
    return bm


def cylindre(r1, r2, h, n=8, bas=Vector()):
    """Un cylindre (ou un cône tronqué) debout, posé sur bas."""
    bm = bmesh.new()
    bmesh.ops.create_cone(bm, cap_ends=True, cap_tris=False, segments=n, radius1=r1, radius2=r2, depth=h)
    bmesh.ops.translate(bm, verts=bm.verts[:], vec=bas + Vector((0, 0, h / 2)))
    return bm


def boite(dims, centre=Vector(), rot=None):
    bm = bmesh.new(); bmesh.ops.create_cube(bm, size=1)
    for v in bm.verts: v.co = Vector((v.co.x * dims[0], v.co.y * dims[1], v.co.z * dims[2]))
    if rot: bmesh.ops.rotate(bm, verts=bm.verts[:], cent=Vector(), matrix=rot)
    bmesh.ops.translate(bm, verts=bm.verts[:], vec=centre)
    return bm


def boule(taille, rnd, n=10, sub=2, centre=Vector(), alea=.2):
    """Une masse taillée à facettes, centrée (feuillage, pétale, bouton)."""
    bm = bmesh.new(); bmesh.ops.create_icosphere(bm, subdivisions=sub, radius=1)
    for v in bm.verts: v.co = Vector((v.co.x * taille.x, v.co.y * taille.y, v.co.z * taille.z))
    tailler(bm, taille, n, rnd, alea, (.8, .92), garder_dessous=False)
    bmesh.ops.dissolve_limit(bm, angle_limit=math.radians(8), verts=bm.verts[:], edges=bm.edges[:])
    bmesh.ops.translate(bm, verts=bm.verts[:], vec=centre)
    return bm


def tourner(bm, axe, angle, centre=Vector()):
    bmesh.ops.rotate(bm, verts=bm.verts[:], cent=centre, matrix=Matrix.Rotation(angle, 3, axe)); return bm


def poutre_courbe(xs, zf, y, h, e):
    """Une poutre qui suit la courbe z = zf(x) : section h (hauteur, sous la courbe) × e (épaisseur en y)."""
    bm = bmesh.new(); anneaux = []
    for x in xs:
        z = zf(x)
        anneaux.append([bm.verts.new(p) for p in ((x, y - e / 2, z - h), (x, y + e / 2, z - h), (x, y + e / 2, z), (x, y - e / 2, z))])
    for a, b in zip(anneaux, anneaux[1:]):
        for k in range(4): bm.faces.new((a[k], b[k], b[(k + 1) % 4], a[(k + 1) % 4]))
    bm.faces.new(anneaux[0][::-1]); bm.faces.new(anneaux[-1])
    bmesh.ops.recalc_face_normals(bm, faces=bm.faces[:])
    return bm


# ---------- les éléments ----------
def nenuphar(rnd, d, prof=.25, ouverture=16, ep=.06, n=36):
    """Feuille ronde, échancrure en V (profondeur prof × diamètre), dessus plat au bord à peine relevé."""
    R = d / 2; a0 = math.radians(ouverture) / 2; leve = .03
    bord = [Vector((R * (1 - 2 * prof), 0, 0))]
    for i in range(n + 1):
        a = a0 + (2 * math.pi - 2 * a0) * i / n; r = R * (1 + rnd.uniform(-.015, .015))
        bord.append(Vector((math.cos(a) * r, math.sin(a) * r, 0)))
    m = len(bord); bm = bmesh.new(); sect = bm.faces.layers.int.new("secteur")
    fr = (.4, .75, 1.0)
    c = bm.verts.new((0, 0, ep))
    haut = [[bm.verts.new((p.x * f, p.y * f, ep + (leve if f == 1 else 0))) for p in bord] for f in fr]
    cb = bm.verts.new((0, 0, 0))
    bas = [bm.verts.new((p.x, p.y, leve)) for p in bord]
    for i in range(m):
        j = (i + 1) % m
        f = bm.faces.new((c, haut[0][i], haut[0][j])); f[sect] = i
        for k in range(len(fr) - 1):
            f = bm.faces.new((haut[k][i], haut[k + 1][i], haut[k + 1][j], haut[k][j])); f[sect] = i + 1000 * (k + 1)
        f = bm.faces.new((bas[i], bas[j], haut[-1][j], haut[-1][i])); f[sect] = -1
        f = bm.faces.new((cb, bas[j], bas[i])); f[sect] = -1
    bm.normal_update()

    def couleur(f):
        s = f[sect]
        if s < 0: return ombre(f.normal, *DESSOUS, rnd)
        col = ombre(f.normal, *FEUILLE, rnd, .03)
        if (s % 1000) % 4 == 0: col = melange(col, NERVURE, .14)
        if s < 1000: col = melange(col, NERVURE, .06)
        return col
    peindre(bm, couleur)
    return bm


def fleur(rnd):
    """Fleur de nénuphar : trois couronnes de pétales, blanc au cœur, rose à la pointe ; cœur jaune."""
    pieces = []
    for cour, (n, incl, lg, z) in enumerate([(9, 22, .24, .02), (7, 42, .2, .04), (5, 62, .15, .06)]):
        for k in range(n):
            az = 2 * math.pi * k / n + cour * .35 + rnd.uniform(-.08, .08)
            p = boule(Vector((lg / 2, .06, .022)), rnd, 8, 1, Vector((lg / 2, 0, 0)), .15)
            tourner(p, "Y", -math.radians(incl)); tourner(p, "Z", az)
            bmesh.ops.translate(p, verts=p.verts[:], vec=Vector((0, 0, z)))
            p.normal_update()

            def couleur(f):
                t = min(1, f.calc_center_median().length / .22)
                tons = tuple(melange(ROSE[0], ROSE[i], t * .9) if i else ROSE[0] for i in range(3))
                return ombre(f.normal, *tons, rnd, .03)
            peindre(p, couleur); pieces.append(p)
    pieces.append(peint(boule(Vector((.055, .055, .04)), rnd, 8, 1, Vector((0, 0, .1))), POLLEN, rnd))
    return assembler(*pieces)


def pierre_gue(rnd, taille, haut=.14):
    bm = pierre(rnd, 13, taille, alea=.15, sub=2)
    couper(bm, Vector((0, 0, haut)), Vector((0, 0, 1)))
    arete = bm.faces.layers.int.get("arete"); bm.normal_update()
    peindre(bm, lambda f: melange(couleur_roche(f, rnd, PIERRE, arete), PIERRE["arete"], .3) if f.normal.z > .95
            else couleur_roche(f, rnd, PIERRE, arete))
    return bm


def rocher_mousse(rnd):
    bm = pierre(rnd, 20, Vector((1.0, .8, .75)), alea=.14, sub=3)
    arete = bm.faces.layers.int.get("arete"); bm.normal_update()

    def couleur(f):
        c = f.calc_center_median()
        if f.normal.z > .45 and noise.noise(c * 1.6 + Vector((3.1, 0, 0))) > -.25: return ombre(f.normal, *MOUSSE, rnd, .06)
        return couleur_roche(f, rnd, PIERRE, arete)
    peindre(bm, couleur)
    return bm


def lanterne(rnd):
    pieces = []
    for k in range(4):
        a = math.radians(45 + 90 * k); d = Vector((math.cos(a), math.sin(a), 0))
        pts = [d * .46, d * .45 + Vector((0, 0, .2)), d * .34 + Vector((0, 0, .38)), d * .18 + Vector((0, 0, .47))]
        pieces.append(peint(tube(pts, .05), (PIERRE["h"], PIERRE["m"], PIERRE["b"]), rnd))
    pieces.append(peint(cylindre(.28, .28, .08, 6, Vector((0, 0, .44))), (PIERRE["h"], PIERRE["m"], PIERRE["b"]), rnd))
    # boîte à lumière : six côtés, quatre fenêtres de papier en retrait
    bm = cylindre(.2, .2, .3, 6, Vector((0, 0, .52))); bm.normal_update()
    role = bm.faces.layers.int.new("role")
    cotes = sorted([f for f in bm.faces if abs(f.normal.z) < .1], key=lambda f: math.atan2(f.normal.y, f.normal.x))
    fen = [f for i, f in enumerate(cotes) if i % 3 != 2]
    for f in fen: f[role] = 1
    res = bmesh.ops.inset_individual(bm, faces=fen, thickness=.028, depth=-.035)
    for f in res["faces"]: f[role] = 0
    bm.normal_update()
    peindre(bm, lambda f: ombre(f.normal, *PAPIER, rnd, .02) if f[role] == 1 else couleur_roche(f, rnd, PIERRE))
    pieces.append(bm)
    # toit large hexagonal aux coins relevés, mousse sur le dessus
    toit = assembler(cylindre(.64, .64, .05, 6, Vector((0, 0, .82))), cylindre(.64, .1, .2, 6, Vector((0, 0, .87))))
    for v in toit.verts:
        if Vector((v.co.x, v.co.y)).length > .6: v.co.z += .05
    toit.normal_update()
    peindre(toit, lambda f: ombre(f.normal, *MOUSSE, rnd, .06)
            if f.normal.z > .3 and noise.noise(f.calc_center_median() * 5) > .1 else couleur_roche(f, rnd, PIERRE))
    pieces.append(toit)
    pieces.append(peint(cylindre(.06, .045, .05, 6, Vector((0, 0, 1.07))), (PIERRE["h"], PIERRE["m"], PIERRE["b"]), rnd))
    pieces.append(peint(boule(Vector((.07, .07, .09)), rnd, 8, 1, Vector((0, 0, 1.18))), (PIERRE["h"], PIERRE["m"], PIERRE["b"]), rnd))
    return assembler(*pieces)


def pont(rnd, rouge=True, L=4.4, s=.8, W=1.1, marches=11):
    """Taiko-bashi : arche en arc de cercle (portée L, flèche s), marches plates, garde-corps."""
    R = (L * L / 4 + s * s) / (2 * s); zc = s - R
    arc = lambda x: zc + math.sqrt(max(0, R * R - min(abs(x), L / 2) ** 2))
    cadre = VERMILLON if rouge else BOIS
    marche = BOIS if rouge else tuple(melange(c, (230, 200, 160), .2) for c in BOIS)
    pieces = []
    pas = L / marches
    for i in range(marches):
        x = -L / 2 + (i + .5) * pas
        pieces.append(peint(boite((pas + .02, W, .07), Vector((x, 0, arc(x) - .035))), marche, rnd, .06))
    xs = [-L / 2 - .15 + (L + .3) * i / 28 for i in range(29)]
    poteaux = [-L / 2 + L * i / 6 for i in range(7)]
    for y in (-W / 2 - .05, W / 2 + .05):
        pieces.append(peint(poutre_courbe(xs, arc, y, .28, .1), cadre, rnd))
        pieces.append(peint(poutre_courbe(xs[1:-1], lambda x: arc(x) + .62, y, .07, .08), cadre, rnd))
        pieces.append(peint(poutre_courbe(xs[1:-1], lambda x: arc(x) + .34, y, .05, .05), cadre, rnd))
        for i, x in enumerate(poteaux):
            bout = i in (0, len(poteaux) - 1)
            h = .74 if bout else .64
            pieces.append(peint(boite((.1, .1, h), Vector((x, y, arc(x) + h / 2))), cadre, rnd))
            if bout:  # giboshi : chapeau en bulbe sur les poteaux d'extrémité
                pieces.append(peint(cylindre(.065, .065, .05, 8, Vector((x, y, arc(x) + h))), BRONZE, rnd))
                pieces.append(peint(boule(Vector((.075, .075, .09)), rnd, 8, 1, Vector((x, y, arc(x) + h + .1))), BRONZE, rnd))
    for x in (-.95, .95):
        for y in (-W / 2 + .1, W / 2 - .1):
            h = arc(x) - .1 + .45
            pieces.append(peint(boite((.16, .16, h), Vector((x, y, arc(x) - .1 - h / 2))), cadre, rnd))
    return assembler(*pieces)


def pin_niwaki(rnd):
    pieces = [peint(cylindre(.26, .13, .3, 7), ECORCE, rnd)]
    tronc = [Vector(p) for p in ((0, 0, 0), (.16, 0, .6), (-.06, .05, 1.2), (.1, -.05, 1.8), (0, 0, 2.35))]
    pieces.append(peint(tube(tronc, .13), ECORCE, rnd, .06))
    nuages = [((.95, .25, 1.25), (.78, .62, .28)), ((-.85, -.3, 1.65), (.66, .56, .25)),
              ((.55, -.65, 2.0), (.56, .5, .22)), ((0, 0, 2.55), (.74, .66, .32))]
    for c, t in nuages:
        c, t = Vector(c), Vector(t)
        if c.z < 2.4:
            depart = Vector((tronc[2].x if c.z < 1.5 else tronc[3].x, 0, c.z - .25))
            milieu = (depart + c) / 2 + Vector((0, 0, .08))
            pieces.append(peint(tube([depart, milieu, c - Vector((0, 0, .1))], .055), ECORCE, rnd))
        bm = boule(t, rnd, 12, 2, c, .25)
        couper(bm, c - Vector((0, 0, t.z * .35)), Vector((0, 0, -1)))
        pieces.append(peint(bm, PIN, rnd, .08))
    return assembler(*pieces)


def erable(rnd):
    pieces = []
    for pts, r in ((((0, 0, 0), (.05, 0, .8), (0, .05, 1.4)), .12), (((0, .05, 1.35), (.5, .1, 1.95), (.8, 0, 2.35)), .07),
                   (((0, .05, 1.35), (-.4, -.1, 1.9), (-.7, .1, 2.25)), .07), (((0, .05, 1.35), (.1, -.4, 2.1), (0, -.5, 2.55)), .06)):
        pieces.append(peint(tube([Vector(p) for p in pts], r), ECORCE, rnd, .06))
    pieces.append(peint(cylindre(.22, .12, .25, 7), ECORCE, rnd))
    grappes = [(.85, 0, 2.45), (-.75, .1, 2.35), (0, -.55, 2.6), (.25, .45, 2.7), (-.15, -.05, 2.95), (.5, -.35, 2.2), (-.45, .5, 2.2)]
    for i, c in enumerate(grappes):
        t = Vector((rnd.uniform(.5, .65), rnd.uniform(.45, .6), rnd.uniform(.36, .46)))
        chaud = rnd.uniform(0, .45)   # certaines grappes tirent vers l'orangé : l'érable n'est pas d'un seul rouge
        tons = tuple(melange(ERABLE[k], (255, 176, 90), chaud * (1 - k * .4)) for k in range(3))
        pieces.append(peint(boule(t, rnd, 12, 2, Vector(c), .25), tons, rnd, .07))
    return assembler(*pieces)


def bambous(rnd, n=7):
    pieces = []
    for i in range(n):
        a = 2 * math.pi * i / n + rnd.uniform(-.3, .3); d = rnd.uniform(.12, .5) if i else 0
        pied = Vector((math.cos(a) * d, math.sin(a) * d, 0))
        haut = rnd.uniform(3.2, 4.6); r = rnd.uniform(.04, .055)
        tige = []; z = 0
        while z < haut:
            seg = rnd.uniform(.32, .44)
            tige.append(peint(cylindre(r, r * .97, seg, 8, Vector((0, 0, z))), BAMBOU, rnd, .03))
            tige.append(peint(cylindre(r * 1.15, r * 1.15, .03, 8, Vector((0, 0, z + seg - .015))),
                              tuple(melange(c, (214, 226, 156), .4) for c in BAMBOU), rnd, .02))
            z += seg; r *= .985
        for k in range(4):
            az = rnd.uniform(0, 2 * math.pi); h = z - rnd.uniform(.1, 1.1)
            f = boule(Vector((.36, .12, .07)), rnd, 8, 1, Vector((.3, 0, 0)), .2)
            tourner(f, "Y", math.radians(rnd.uniform(10, 30))); tourner(f, "Z", az)
            bmesh.ops.translate(f, verts=f.verts[:], vec=Vector((0, 0, h)))
            tige.append(peint(f, BAMBOU_FEUILLE, rnd, .06))
        bm = assembler(*tige)
        tourner(bm, Vector((-math.sin(a), math.cos(a), 0)), math.radians(rnd.uniform(1, 6) if i else 0))
        bmesh.ops.translate(bm, verts=bm.verts[:], vec=pied)
        pieces.append(bm)
    return assembler(*pieces)


def cloture(rnd, L=2.4):
    """Yotsume-gaki (faite de mémoire) : poteaux, trois traverses de bambou, bambous verticaux alternés, liens noirs."""
    pieces = []
    for x in (-L / 2, 0, L / 2):
        pieces.append(peint(cylindre(.055, .055, 1.05, 8, Vector((x, 0, 0))), BOIS, rnd))
    for z in (.3, .62, .92):
        pieces.append(peint(tourner(cylindre(.025, .025, L + .12, 8, Vector((0, 0, -(L + .12) / 2))), "Y", math.pi / 2,), BAMBOU_SEC, rnd))
        bmesh.ops.translate(pieces[-1], verts=pieces[-1].verts[:], vec=Vector((0, .07, z)))
    for i in range(8):
        x = -L / 2 + .15 + (L - .3) * i / 7
        y = .07 + (.045 if i % 2 else -.045)
        pieces.append(peint(cylindre(.02, .02, .98, 8, Vector((x, y, 0))), BAMBOU_SEC, rnd, .05))
        for z in (.3, .62, .92):
            pieces.append(peint(boite((.06, .03, .045), Vector((x, .07, z))), CORDE, rnd))
    return assembler(*pieces)


def peint_ecorce(bm, rnd):
    """Écorce de cerisier : bandes sombres horizontales (les lenticelles)."""
    bm.normal_update()
    peindre(bm, lambda f: melange(ombre(f.normal, *ECORCE_CERISIER, rnd, .05), ECORCE_CERISIER[2], .45)
            if math.sin(f.calc_center_median().z * 15) > .72 else ombre(f.normal, *ECORCE_CERISIER, rnd, .05))
    return bm


def cerisier(rnd):
    """Cerisier en fleurs (Somei-Yoshino) : petit tronc, branches qui s'étalent large, fleurs rose pâle à blanches."""
    pieces = [peint_ecorce(cylindre(.3, .16, .3, 7), rnd)]
    pieces.append(peint_ecorce(tube([Vector(p) for p in ((0, 0, 0), (.08, 0, .6), (0, .05, 1.15))], .16), rnd))
    bouts = []
    for k in range(4):
        a = 2 * math.pi * k / 4 + rnd.uniform(-.4, .4); d = rnd.uniform(1.5, 2.1); h = rnd.uniform(2.1, 2.6)
        bout = Vector((math.cos(a) * d, math.sin(a) * d, h))
        milieu = Vector((math.cos(a) * d * .45, math.sin(a) * d * .45, 1.15 + (h - 1.15) * .8))
        pieces.append(peint_ecorce(tube([Vector((0, .05, 1.1)), milieu, bout], .085), rnd))
        bouts.append(bout)
    centres = [b + Vector((0, 0, .3)) for b in bouts] + [Vector((0, 0, 2.95))]
    centres += [(bouts[i] + bouts[(i + 1) % 4]) * .4 + Vector((0, 0, .7)) for i in range(4)]
    for c in centres:
        t = Vector((rnd.uniform(.75, .95), rnd.uniform(.65, .85), rnd.uniform(.42, .52)))
        bm = boule(t, rnd, 14, 2, c, .25); bm.normal_update()

        def couleur(f):
            base = rnd.choices(FLEURS_CERISIER, (5, 3, 2))[0]
            # dessous à peine plus rose (pas mauve) : le jeu assombrit déjà ce qui regarde le sol
            return ombre(f.normal, base, melange(base, (226, 150, 182), .25), melange(base, (206, 128, 162), .4), rnd, .03)
        peindre(bm, couleur); pieces.append(bm)
    return assembler(*pieces)


def petales(rnd, n=110, R=2.6):
    """Pétales tombés, à poser sous un cerisier : petits triangles à plat, sans contour."""
    bm = bmesh.new()
    for i in range(n):
        r = R * math.sqrt(rnd.random()); a = rnd.uniform(0, 2 * math.pi); c = Vector((math.cos(a) * r, math.sin(a) * r, .012))
        s = rnd.uniform(.05, .08); b = rnd.uniform(0, 2 * math.pi)
        bm.faces.new([bm.verts.new(c + Vector((math.cos(b + k * 2.2) * s, math.sin(b + k * 2.2) * s * .7, 0))) for k in range(3)])
    bm.normal_update()
    peindre(bm, lambda f: ombre(f.normal, *[melange(rnd.choice(FLEURS_CERISIER), (232, 140, 176), .25)] * 3, rnd, .04))
    return bm


def tsukubai(rnd):
    """Bassin d'ablutions : bassin creusé (chōzubachi), pierre de devant (mae-ishi), pierres à bougie et à seau de part et
    d'autre (teshoku-ishi, yuoke-ishi), galets sombres au centre, arrivée d'eau en bambou (kakei), louche (hishaku)."""
    pieces = []
    bm = pierre(rnd, 12, Vector((.38, .36, .3)), alea=.1, sub=2)
    couper(bm, Vector((0, 0, .34)), Vector((0, 0, 1)))
    bm = creuser(bm, [(Vector((0, 0, .4)), .22)])
    arete = bm.faces.layers.int.get("arete"); bm.normal_update()
    peindre(bm, lambda f: ombre(f.normal, *[melange(c, (70, 74, 90), .35) for c in (PIERRE["h"], PIERRE["m"], PIERRE["b"])], rnd)
            if f[arete] == 0 and f.calc_center_median().z < .33 else couleur_roche(f, rnd, PIERRE, arete))
    pieces.append(bm)
    pieces.append(peint(cylindre(.17, .17, .01, 12, Vector((0, 0, .27))), EAU_BASSIN, rnd, .02))
    for (x, y), t, h in (((0, -.78), (.34, .26, .18), .1), ((-.68, -.25), (.22, .2, .26), .3), ((.68, -.22), (.24, .2, .22), .24)):
        p = pierre_gue(rnd, Vector(t), h)
        bmesh.ops.translate(p, verts=p.verts[:], vec=Vector((x, y, 0))); pieces.append(p)
    for i in range(16):
        r = rnd.uniform(.035, .055)
        pieces.append(peint(boule(Vector((r * 1.2, r, r * .6)), rnd, 8, 1, Vector((rnd.uniform(-.45, .45), rnd.uniform(-.55, -.12), r * .3)), .15), GALET, rnd, .08))
    pieces.append(peint(cylindre(.035, .035, .78, 8, Vector((0, .55, 0))), BAMBOU_SEC, rnd))
    pieces.append(peint(tube([Vector((0, .55, .7)), Vector((0, .38, .66)), Vector((0, .2, .56))], .025), BAMBOU_SEC, rnd))
    louche = assembler(peint(boite((.018, .44, .012), Vector((.1, .02, .352))), BOIS, rnd), peint(cylindre(.045, .045, .06, 8, Vector((.1, -.22, .33))), BOIS, rnd))
    pieces.append(tourner(louche, "Z", math.radians(18)))
    return assembler(*pieces)


def shishi_odoshi(rnd, pivot=Vector((0, 0, .42)), repos=20):
    """Bambou basculant : le tube (pivot décalé vers la bouche) se remplit, bascule, se vide et retombe sur la pierre.
    Rend deux parties : le support, et le tube à part (origine au pivot) pour l'animer. Au repos, bouche levée de `repos`°."""
    socle = []
    for x in (-.09, .09):
        socle.append(peint(cylindre(.035, .035, .52, 8, Vector((x, 0, 0))), BAMBOU_SEC, rnd))
    socle.append(peint(tourner(cylindre(.012, .012, .26, 6, Vector((0, 0, -.13))), "Y", math.pi / 2), CORDE, rnd))
    bmesh.ops.translate(socle[-1], verts=socle[-1].verts[:], vec=pivot)
    pierre_frappe = pierre(rnd, 10, Vector((.16, .15, .13)), sub=1)
    bmesh.ops.translate(pierre_frappe, verts=pierre_frappe.verts[:], vec=Vector((0, -.52, 0)))
    arete = pierre_frappe.faces.layers.int.get("arete"); pierre_frappe.normal_update()
    peindre(pierre_frappe, lambda f: couleur_roche(f, rnd, PIERRE, arete)); socle.append(pierre_frappe)
    socle.append(peint(cylindre(.035, .035, .8, 8, Vector((0, .62, 0))), BAMBOU_SEC, rnd))
    socle.append(peint(tube([Vector((0, .62, .74)), Vector((0, .5, .74)), Vector((0, .4, .68))], .022), BAMBOU_SEC, rnd))
    # le tube, le long de +Y : bout fermé (lourd) à -0,56, bouche coupée en biais à +0,34
    t = tourner(cylindre(.05, .05, .9, 10, Vector((0, 0, -.56))), "X", -math.pi / 2)
    couper(t, Vector((0, .3, 0)), Vector((0, .7, .7)))
    tourner(t, "X", math.radians(repos))
    bmesh.ops.translate(t, verts=t.verts[:], vec=pivot)
    peint(t, BAMBOU, rnd, .03)
    return [("", assembler(*socle), Vector()), ("_Bascule", t, pivot)]


def iris(rnd, feuilles=14, fleurs=4):
    """Touffe d'iris d'eau (hanashōbu) : feuilles en lame, fleurs violettes à tache jaune."""
    pieces = []
    for i in range(feuilles):
        h = rnd.uniform(.55, .85); az = rnd.uniform(0, 2 * math.pi)
        f = cylindre(.035, .004, h, 4)
        for v in f.verts: v.co.y *= .22
        tourner(f, "X", math.radians(rnd.uniform(4, 18))); tourner(f, "Z", az)
        bmesh.ops.translate(f, verts=f.verts[:], vec=Vector((math.cos(az) * .08, math.sin(az) * .08, 0)))
        pieces.append(peint(f, FEUILLE_IRIS, rnd, .05))
    for i in range(fleurs):
        az = rnd.uniform(0, 2 * math.pi); h = rnd.uniform(.78, .95); c = Vector((math.cos(az) * .12, math.sin(az) * .12, h))
        pieces.append(peint(cylindre(.008, .008, h, 5, Vector((c.x, c.y, 0))), FEUILLE_IRIS, rnd))
        for k in range(6):
            tombe = k % 2 == 0   # trois pétales retombants (à tache jaune), trois dressés
            p = boule(Vector((.07, .036, .012)) if tombe else Vector((.05, .025, .01)), rnd, 8, 1, Vector((.06 if tombe else .04, 0, 0)), .15)
            tourner(p, "Y", math.radians(35 if tombe else -60)); tourner(p, "Z", k * math.pi / 3 + az)
            bmesh.ops.translate(p, verts=p.verts[:], vec=c); p.normal_update()
            peindre(p, lambda f, c=c, tombe=tombe: JAUNE_IRIS if tombe and (f.calc_center_median() - c).length < .045
                    else ombre(f.normal, *IRIS, rnd, .04))
            pieces.append(p)
    return assembler(*pieces)


def galets(rnd, n=13, R=.6):
    """Galets de rive, sombres et lisses."""
    pieces = []
    for i in range(n):
        r = rnd.uniform(.05, .14); a = rnd.uniform(0, 2 * math.pi); d = R * math.sqrt(rnd.random())
        tons = tuple(melange(c, (176, 180, 186), rnd.uniform(0, .35)) for c in GALET)
        pieces.append(peint(boule(Vector((r * 1.25, r, r * .55)), rnd, 9, 1, Vector((math.cos(a) * d, math.sin(a) * d, r * .3)), .12), tons, rnd, .05))
    return assembler(*pieces)


def rocher_dresse(rnd):
    """Pierre dressée, de mousse seulement au pied."""
    bm = pierre(rnd, 16, Vector((.42, .36, .95)), alea=.14, sub=3)
    arete = bm.faces.layers.int.get("arete"); bm.normal_update()
    peindre(bm, lambda f: ombre(f.normal, *MOUSSE, rnd, .06) if f.calc_center_median().z < .16 else couleur_roche(f, rnd, PIERRE, arete))
    return bm


def gravier(rnd, L=7.0, l=5.0, pas=.1, sillon=.25):
    """Gravier ratissé (karesansui) : lignes droites, cercles concentriques (mizumon) autour des pierres de
    GRAVIER_CENTRES, bordure de planches."""
    bm = bmesh.new(); nx, ny = int(L / pas), int(l / pas)

    def hauteur(x, y):
        for cx, cy, r in GRAVIER_CENTRES:
            d = math.hypot(x - cx, y - cy)
            if d < r: return .05 + .014 * math.cos(2 * math.pi * d / sillon)
        return .05 + .014 * math.cos(2 * math.pi * y / sillon)
    vs = [[bm.verts.new((-L / 2 + i * pas, -l / 2 + j * pas, hauteur(-L / 2 + i * pas, -l / 2 + j * pas))) for j in range(ny + 1)] for i in range(nx + 1)]
    for i in range(nx):
        for j in range(ny): bm.faces.new((vs[i][j], vs[i + 1][j], vs[i + 1][j + 1], vs[i][j + 1]))
    bmesh.ops.triangulate(bm, faces=bm.faces[:]); bm.normal_update()
    lh = Vector((LUMIERE.x, LUMIERE.y, 0)).normalized()   # sillons : le versant tourné vers la lumière, plus clair

    def couleur(f):
        s = f.normal.dot(lh) * 2.4
        return melange(GRAVIER[1], GRAVIER[0] if s > 0 else GRAVIER[2], min(1, abs(s)) * .8)
    peindre(bm, couleur)
    planches = [boite((L + .2, .1, .14), Vector((0, s * (l / 2 + .05), .07))) for s in (-1, 1)]
    planches += [boite((.1, l, .14), Vector((s * (L / 2 + .05), 0, .07))) for s in (-1, 1)]
    bord = assembler(*[peint(p, BOIS, rnd) for p in planches]); ajouter_contour(bord, .01, TRAIT)   # contour sur la bordure seule
    return assembler(bm, bord)


def froggy_echelle(rnd):
    """Silhouette d'échelle, 1 m (hypothèse : aucune taille n'est fixée). Pas exportée."""
    vert = ((170, 220, 120), (120, 180, 90), (70, 120, 70))
    return assembler(peint(boule(Vector((.3, .26, .36)), rnd, 14, 2, Vector((0, 0, .36)), .1), vert, rnd),
                     peint(boule(Vector((.3, .27, .2)), rnd, 14, 2, Vector((0, 0, .8)), .1), vert, rnd),
                     *[peint(boule(Vector((.09, .09, .09)), rnd, 10, 1, Vector((x, -.08, .96)), .1), vert, rnd) for x in (-.14, .14)])


# ---------- fabrication, export, rendus ----------
def photo(nom, az, el, cible, echelle, rx=800, ry=700, seules=None):
    """Rendu de contrôle (couleurs de sommets, contour), vue orthographique ; seules : collections montrées."""
    sc = bpy.context.scene; sc.render.engine = "BLENDER_WORKBENCH"
    sh = sc.display.shading; sh.light = "FLAT"; sh.color_type = "VERTEX"; sh.show_backface_culling = True
    if sc.world is None: sc.world = bpy.data.worlds.new("Monde")
    sc.world.color = (.55, .62, .66)
    sc.render.resolution_x, sc.render.resolution_y = rx, ry; sc.render.film_transparent = False
    cam = bpy.data.objects.get("Camera") or bpy.data.objects.new("Camera", bpy.data.cameras.new("Camera"))
    if not cam.users_collection: sc.collection.objects.link(cam)
    cam.data.type = "ORTHO"; cam.data.ortho_scale = echelle; cam.data.clip_end = 200; sc.camera = cam
    a, e = math.radians(az), math.radians(el)
    cam.location = cible + Vector((math.cos(a) * math.cos(e), math.sin(a) * math.cos(e), math.sin(e))) * 40
    cam.rotation_euler = (cible - cam.location).to_track_quat("-Z", "Y").to_euler()
    for c in bpy.data.collections:
        for o in c.objects: o.hide_render = seules is not None and c.name not in seules
    os.makedirs(RENDUS_JJ, exist_ok=True)
    sc.render.filepath = os.path.join(RENDUS_JJ, nom + ".png"); bpy.ops.render.render(write_still=True)
    return sc.render.filepath


def boite_englobante(ob):
    pts = [ob.matrix_world @ Vector(c) for c in ob.bound_box]
    return Vector([min(p[i] for p in pts) for i in range(3)]), Vector([max(p[i] for p in pts) for i in range(3)])


for ob in list(bpy.data.objects): bpy.data.objects.remove(ob, do_unlink=True)

ELEMENTS = [  # nom, fabrique, épaisseur du contour, rangée de la planche (0 : eau et sol, 1 : grands éléments)
    ("Nenuphar_Grand", lambda r: nenuphar(r, 2.6), .02, 0),
    ("Nenuphar_Moyen", lambda r: nenuphar(r, 1.8), .018, 0),
    ("Nenuphar_Petit", lambda r: nenuphar(r, 1.2), .016, 0),
    ("Fleur_Nenuphar", fleur, .008, 0),
    ("PierreGue_1", lambda r: pierre_gue(r, Vector((.46, .36, .2))), .014, 0),
    ("PierreGue_2", lambda r: pierre_gue(r, Vector((.4, .34, .2))), .014, 0),
    ("PierreGue_3", lambda r: pierre_gue(r, Vector((.36, .3, .2))), .014, 0),
    ("Rocher_Mousse", rocher_mousse, .022, 0),
    ("Lanterne_Yukimi", lanterne, .012, 0),
    ("Pont_Taiko_Rouge", lambda r: pont(r, True), .016, 1),
    ("Pont_Taiko_Bois", lambda r: pont(r, False), .016, 1),
    ("Pin_Niwaki", pin_niwaki, .02, 1),
    ("Erable_Momiji", erable, .02, 1),
    ("Bambous", bambous, .01, 1),
    ("Cloture_Bambou", cloture, .01, 1),
    ("Cerisier_1", cerisier, .02, 2),
    ("Cerisier_2", cerisier, .02, 2),
    ("Petales_Sol", petales, 0, 2),
    ("Tsukubai", tsukubai, .01, 2),
    ("ShishiOdoshi", shishi_odoshi, .008, 2),
    ("Iris", iris, .006, 2),
    ("Galets", galets, .008, 2),
    ("Rocher_Dresse", rocher_dresse, .016, 2),
    ("Gravier_Ratisse", gravier, 0, 2),
]
EXPORTER = not os.environ.get("JARDIN_SANS_EXPORT")
objets = {}
for i, (nom, fab, ep, rang) in enumerate(ELEMENTS):
    rnd = random.Random(1000 + i)
    parties = fab(rnd)
    if not isinstance(parties, list): parties = [("", parties, Vector())]
    col = collection(nom); racine = None
    for suffixe, bm, origine in parties:   # une pièce mobile devient un enfant, origine à son pivot
        if ep: ajouter_contour(bm, ep, TRAIT)
        ob = objet(nom + suffixe, bm, col, origine)
        if racine: ob.parent = racine
        else: racine = ob
    objets[nom] = (racine, rang)
    if EXPORTER: exporter(col, os.path.join(DOSSIER, nom + ".fbx"))
    print(f"{nom:20s} {poids(col):6d} faces")

echelle = collection("Echelle"); ajouter_contour(bm := froggy_echelle(random.Random(7)), .012, TRAIT)
froggy = objet("Froggy_1m", bm, echelle)

# une vue par élément, à peu près sous l'angle d'une caméra de 3e personne (basse, de trois quarts)
for nom, (ob, rang) in objets.items():
    lo, hi = boite_englobante(ob); taille = max(hi - lo)
    el = 50 if nom.split("_")[0] in ("Nenuphar", "PierreGue", "Galets", "Petales", "Gravier", "Tsukubai") else 22
    photo(nom, -62, el, (lo + hi) / 2, taille * 1.35, seules=[nom])

# une planche par rangée, avec la silhouette d'échelle d'1 m
for rang in (0, 1, 2):
    x = 0.0; noms = [n for n, (o, r) in objets.items() if r == rang]
    for n in noms:
        ob = objets[n][0]; lo, hi = boite_englobante(ob)
        ob.location.x += x - lo.x; x += (hi.x - lo.x) + .6
    froggy.location = Vector((x + .3, 0, 0)); x += 1.2
    haut = max(boite_englobante(objets[n][0])[1].z for n in noms)
    photo(f"planche_{rang + 1}", -90, 12, Vector((x / 2, 0, haut / 2)), x * 1.04, 3200, int(3200 * (haut * 1.3) / (x * 1.04)),
          seules=noms + ["Echelle"])
    for n in noms: objets[n][0].location = Vector()

objets["Gravier_Ratisse"][0]["centres"] = repr(GRAVIER_CENTRES)   # relu par vitrine_jardin.py pour poser les pierres dressées
bpy.ops.wm.save_as_mainfile(filepath=os.path.join(RENDUS_JJ, "jardin_japonais.blend"))
print("FINI", RENDUS_JJ)
