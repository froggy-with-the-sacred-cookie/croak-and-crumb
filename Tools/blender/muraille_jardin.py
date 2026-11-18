"""Muraille du jardin japonais : le mur d'enceinte sur le contour de l'arène (proposition du 28 octobre 2026, pas de DA).
Un tsuiji-bei : terre damée entre des planches, plus large au pied qu'en tête, poteaux inclinés tous les 2 m, petit toit
de tuiles à deux pentes (kirizuma) ; variante sujibei à cinq lignes blanches (le rang le plus haut : celui du cookie
sacré). Sources : JAANUS « tsuiji » (https://www.aisf.or.jp/~jaanus/deta/t/tsuiji.htm), fiche du MLIT sur le mur du
Jison-in (≈ 3 m de haut, ≈ 1,6 m au pied, ramené ici à 1,2 m). Le soubassement de pierre, les couleurs et les
proportions du toit sont de mémoire, à vérifier.
Deux modules de 4 m (Muraille_Mur_1 et _2 : même forme, taches différentes, raccord sans couture d'un module à l'autre),
un pilier d'angle (Muraille_Angle) et une porte munamon de la même largeur (Muraille_Porte : deux poteaux qui portent un
toit à deux pentes, d'après Wikipedia « Mon (architecture) » ; vantaux de planches à bord droit repris du kabukimon de
JAANUS ; la hauteur du toit et les ferrures sont de mémoire). La pose suit les bornes du sol de la vitrine (disposition_jardin.json), face
intérieure au bord du terrain : la muraille n'empiète pas sur le jardin. Avec l'export, la pose part aussi dans
Assets/Froggy/Vitrine/muraille_jardin.json (lu par VitrineJardin.cs, étape « Poser la muraille »).
VITRINE=grande : la vitrine agrandie (disposition_jardin_grande.json → muraille_jardin_grande.json), avec la porte au
milieu du côté sud ; seul Muraille_Porte est alors exporté (les modules de la vitrine d'origine ne bougent pas).
Se lance seul, ou après jardin_japonais.py et vitrine_jardin.py dans la même session (l'aperçu montre alors le jardin) :
    FROGGY_ROOT=$PWD /Applications/Blender.app/Contents/MacOS/Blender -b -P Tools/blender/muraille_jardin.py
JARDIN_SANS_EXPORT=1 : sans écrire les FBX ; MURAILLE_SANS_APERCU=1 : sans les rendus."""
import os, json
exec(open(os.environ["FROGGY_ROOT"] + "/Tools/blender/blender_lib.py").read())
from mathutils import Matrix

GRANDE = os.environ.get("VITRINE") == "grande"
DOSSIER = os.path.join(MODELES, "JardinJaponais")
DISPO = os.path.join(RACINE, "Assets", "Froggy", "Vitrine", f"disposition_jardin{'_grande' if GRANDE else ''}.json")
POSE = os.path.join(RACINE, "Assets", "Froggy", "Vitrine", f"muraille_jardin{'_grande' if GRANDE else ''}.json")
APERCUS = os.path.join(RENDUS, "vitrine_grande" if GRANDE else "vitrine")
TRAIT = (40, 58, 44)                       # le contour du kit
L, PAS = 4.0, .25                          # longueur d'un module ; pas des tuiles rondes (divise L : les modules se raccordent)
SOCLE, CORPS, EGOUT, FAITE = .42, 2.55, 2.72, 3.05   # haut du soubassement, du mur, de la poutre, du faîte
W_SOCLE, W_PIED, W_TETE = .6, .5, .38      # demi-épaisseurs
DEBORD = .78                               # demi-largeur du toit à l'égout
LIGNES = [1.92 + k * .11 for k in range(5)]   # bas de chaque ligne blanche, .05 de haut
POTEAUX = (-1.0, 1.0)                      # tous les 2 m, raccords compris
ENDUIT = ((238, 206, 152), (210, 172, 122), (150, 116, 88))   # terre ocre : chaude contre les verts, sans voler le rouge du pont
SALI = (146, 116, 90)
BLANC = ((252, 248, 238), (234, 228, 216), (190, 184, 176))
TUILE = ((152, 162, 182), (104, 112, 134), (60, 64, 84))      # tuile fumée (ibushi-gawara), gris argent bleuté
BOIS_SOMBRE = ((132, 96, 72), (94, 66, 54), (56, 40, 36))
BOIS_PORTE = ((176, 130, 94), (134, 96, 70), (84, 60, 48))     # un ton plus clair que la poutre : la porte se repère de loin
FER = ((70, 70, 78), (46, 46, 52), (28, 28, 32))
GRANIT = {"h": (204, 204, 208), "m": (160, 160, 168), "b": (106, 106, 118), "arete": (222, 222, 228)}


# ---------- briques ----------
def assembler(*pieces):
    bm = bmesh.new()
    for p in pieces: fusionner(bm, p)
    return bm


def peint(bm, tons, rnd, var=.04):
    bm.normal_update(); peindre(bm, lambda f: ombre(f.normal, *tons, rnd, var)); return bm


def cube_vers(fonction):
    """Un cube dont chaque sommet (coins en ±.5) est envoyé ailleurs : boîtes inclinées, pentes de toit, poteaux."""
    bm = bmesh.new(); bmesh.ops.create_cube(bm, size=1)
    for v in bm.verts: v.co = fonction(v.co.copy())
    bmesh.ops.recalc_face_normals(bm, faces=bm.faces[:])
    return bm


def boite(dims, centre):
    return cube_vers(lambda c: Vector((c.x * dims[0], c.y * dims[1], c.z * dims[2])) + centre)


def bloc(rnd, dims, centre, biseau=.035):
    """Pierre de taille : une boîte aux arêtes biseautées, biseaux plus clairs."""
    bm = boite(dims, centre); arete = bm.faces.layers.int.new("arete")
    for f in bm.faces: f[arete] = 2
    res = bmesh.ops.bevel(bm, geom=bm.edges[:], offset=biseau, segments=1, affect="EDGES", profile=.5, clamp_overlap=True)
    for f in res["faces"]: f[arete] = 1
    bm.normal_update(); peindre(bm, lambda f: couleur_roche(f, rnd, GRANIT, arete))
    return bm


def anneau(bm, z, hx, hy, nx, ny):
    """Le tour d'un rectangle à la hauteur z, découpé en nx segments sur x et ny sur y."""
    pts = [(-hx + 2 * hx * i / nx, -hy) for i in range(nx)] + [(hx, -hy + 2 * hy * j / ny) for j in range(ny)]
    pts += [(hx - 2 * hx * i / nx, hy) for i in range(nx)] + [(-hx, hy - 2 * hy * j / ny) for j in range(ny)]
    return [bm.verts.new((x, y, z)) for x, y in pts]


def prisme(zs, hx, hy, nx, ny):
    """Un prisme fermé à section rectangulaire hx(z) × hy(z), en tranches aux hauteurs zs (assez pour y peindre)."""
    bm = bmesh.new(); anneaux = [anneau(bm, z, hx(z), hy(z), nx, ny) for z in zs]
    for a, b in zip(anneaux, anneaux[1:]):
        for k in range(len(a)): bm.faces.new((a[k], a[(k + 1) % len(a)], b[(k + 1) % len(b)], b[k]))
    bm.faces.new(anneaux[0]); bm.faces.new(anneaux[-1])
    bmesh.ops.recalc_face_normals(bm, faces=bm.faces[:])
    return bm


def tranches(bas, haut):
    """Les hauteurs de découpe du mur : une tranche par ligne blanche, et des tranches de ~.3 m ailleurs."""
    zs = {bas, haut}
    for z in LIGNES: zs |= {z, z + .05}
    z = bas
    while z < LIGNES[0] - .15: zs.add(round(z, 3)); z += .3
    return sorted(zs)


def couleur_enduit(rnd, graine, coulures):
    """Terre ocre en taches douces, éclaboussée de terre au pied, coulures sous le toit, cinq lignes blanches."""
    def couleur(f):
        c, n = f.calc_center_median(), f.normal
        if abs(n.z) > .9: return ombre(n, *ENDUIT, rnd, .02)
        if any(z < c.z < z + .05 for z in LIGNES): return ombre(n, *BLANC, rnd, .01)
        col = ombre(n, *ENDUIT, rnd, .015)
        t = noise.noise(Vector((c.x * .7 + graine, c.y * .7, c.z * 1.4)))
        col = melange(col, ENDUIT[2] if t < 0 else ENDUIT[0], abs(t) * .35)
        if c.z < SOCLE + .4: col = melange(col, SALI, (1 - (c.z - SOCLE) / .4) * .55)
        for x, force in coulures:
            if abs(c.x - x) < PAS * .6 and c.z > 1.2: col = melange(col, SALI, force * (c.z - 1.2) / 1.3)
        return col
    return couleur


def demi(z, pied, tete): return pied + (tete - pied) * (z - SOCLE) / (CORPS - SOCLE)


def toit(rnd, longueur, nx, dz=0.0, debord=DEBORD):
    """Toit à deux pentes : dalles, tuiles rondes tous les PAS (le bout tourné vers l'égout), faîtage à deux rangs.
    dz : rehaussé de dz ; debord : demi-largeur à l'égout."""
    pieces = []; haut = FAITE - .1 + dz
    for s in (-1, 1):
        crete, egout = Vector((0, 0, haut)), Vector((0, s * debord, EGOUT + dz))
        d = (egout - crete).normalized(); nrm = Vector((0, -d.z, d.y))   # perpendiculaire à la pente, vers le ciel
        if nrm.z < 0: nrm = -nrm

        def pente(c, crete=crete, egout=egout, nrm=nrm):
            p = crete + (egout - crete) * (c.y * s + .5)
            return Vector((c.x * longueur, p.y, p.z)) + nrm * (c.z + .5) * .07
        pieces.append(peint(cube_vers(pente), TUILE, rnd, .02))
        lg = (egout - crete).length + .05
        for k in range(nx):
            x = -longueur / 2 + PAS / 2 + k * PAS
            t = bmesh.new(); bmesh.ops.create_cone(t, cap_ends=True, segments=6, radius1=.065, radius2=.065, depth=lg)
            bmesh.ops.rotate(t, verts=t.verts[:], cent=Vector(), matrix=Vector((0, 0, 1)).rotation_difference(d).to_matrix())
            bmesh.ops.translate(t, verts=t.verts[:], vec=Vector((x, 0, 0)) + crete + d * (lg / 2 - .02) + nrm * .07)
            pieces.append(peint(t, TUILE, rnd, .03))
    pieces.append(peint(boite((longueur, .3, .2), Vector((0, 0, FAITE - .06 + dz))), TUILE, rnd, .02))
    pieces.append(peint(boite((longueur, .18, .1), Vector((0, 0, FAITE + .09 + dz))), TUILE, rnd, .02))
    return pieces


# ---------- les éléments ----------
def mur(rnd, graine):
    """Un module de 4 m le long de x, symétrique en y : soubassement, corps en talus, poteaux, poutre, toit."""
    nx = int(L / PAS); pieces = []
    x = -L / 2
    while x < L / 2 - .01:                    # des pierres de longueurs inégales, qui tombent juste aux deux bouts
        lg = min(rnd.uniform(.55, 1.05), L / 2 - x)
        if L / 2 - x - lg < .4: lg = L / 2 - x
        h = SOCLE + .3 + rnd.uniform(-.03, 0)
        pieces.append(bloc(rnd, (lg - .02, W_SOCLE * 2 - rnd.uniform(0, .04), h), Vector((x + lg / 2, 0, -.3 + h / 2))))
        x += lg
    corps = prisme(tranches(SOCLE, CORPS), lambda z: L / 2, lambda z: demi(z, W_PIED, W_TETE), nx, 1)
    coulures = [(-L / 2 + PAS * (k + .5), rnd.uniform(.12, .26)) for k in rnd.sample(range(nx), 3)]
    corps.normal_update(); peindre(corps, couleur_enduit(rnd, graine, coulures)); pieces.append(corps)
    for s in (-1, 1):
        for xp in POTEAUX:                     # poteau incliné, collé au talus
            pieces.append(peint(cube_vers(lambda c, s=s, xp=xp: Vector((
                xp + c.x * .16, s * (demi(SOCLE if c.z < 0 else CORPS, W_PIED, W_TETE) + (.05 if c.y * s > 0 else -.03)),
                SOCLE if c.z < 0 else CORPS))), BOIS_SOMBRE, rnd))
    pieces.append(peint(boite((L, (W_TETE + .07) * 2, EGOUT - CORPS + .02), Vector((0, 0, (CORPS + EGOUT) / 2))), BOIS_SOMBRE, rnd))
    return assembler(*pieces, *toit(rnd, L, nx))


def angle(rnd):
    """Le pilier d'angle : un carré un peu plus large que le mur, toit en pyramide à quatre arêtiers, épi au sommet."""
    c = W_SOCLE + .1; pieces = [bloc(rnd, (c * 2, c * 2, SOCLE + .3), Vector((0, 0, -.3 + (SOCLE + .3) / 2)))]
    h = lambda z: demi(z, W_PIED + .1, W_TETE + .1)
    corps = prisme(tranches(SOCLE, CORPS), h, h, 3, 3)
    corps.normal_update(); peindre(corps, couleur_enduit(rnd, 7.0, [])); pieces.append(corps)
    pieces.append(peint(boite(((W_TETE + .17) * 2,) * 2 + (EGOUT - CORPS + .02,), Vector((0, 0, (CORPS + EGOUT) / 2))), BOIS_SOMBRE, rnd))
    R, sommet = DEBORD + .08, FAITE + .2
    p = bmesh.new(); bmesh.ops.create_cone(p, cap_ends=True, segments=4, radius1=R * math.sqrt(2), radius2=.06, depth=sommet - EGOUT)
    bmesh.ops.rotate(p, verts=p.verts[:], cent=Vector(), matrix=Matrix.Rotation(math.pi / 4, 3, "Z"))
    bmesh.ops.translate(p, verts=p.verts[:], vec=Vector((0, 0, (sommet + EGOUT) / 2)))
    pieces.append(peint(p, TUILE, rnd, .02))
    for k in range(4):                         # les arêtiers, du sommet vers chaque coin
        a = math.pi / 4 + k * math.pi / 2; coin = Vector((math.cos(a) * R * math.sqrt(2), math.sin(a) * R * math.sqrt(2), EGOUT))
        haut = Vector((0, 0, sommet)); d = coin - haut
        ar = boite((.12, .12, d.length), Vector()); bmesh.ops.rotate(ar, verts=ar.verts[:], cent=Vector(), matrix=Vector((0, 0, 1)).rotation_difference(d).to_matrix())
        bmesh.ops.translate(ar, verts=ar.verts[:], vec=(haut + coin) / 2 + Vector((0, 0, .05)))
        pieces.append(peint(ar, TUILE, rnd, .02))
    pieces.append(peint(cylindre_debout(.1, .05, .22, sommet - .02), TUILE, rnd, .02))
    return assembler(*pieces)


def porte(rnd):
    """Porte munamon dans un module de 4 m : deux bouts de mur de 0,5 m (raccord aux modules voisins, tuiles comprises),
    deux poteaux sur pierres, linteau, toit à deux pentes rehaussé, deux vantaux de planches fermés (la vitrine finit là)."""
    pieces = []
    for s in (-1, 1):
        xc = s * (L / 2 - .25)
        corps = prisme(tranches(SOCLE, CORPS), lambda z: .25, lambda z: demi(z, W_PIED, W_TETE), 2, 1)
        corps.normal_update(); peindre(corps, couleur_enduit(rnd, 11.0 + s, []))
        bout = assembler(bloc(rnd, (.48, W_SOCLE * 2, SOCLE + .3), Vector((0, 0, -.3 + (SOCLE + .3) / 2))), corps,
                         peint(boite((.5, (W_TETE + .07) * 2, EGOUT - CORPS + .02), Vector((0, 0, (CORPS + EGOUT) / 2))), BOIS_SOMBRE, rnd),
                         *toit(rnd, .5, 2))
        bmesh.ops.translate(bout, verts=bout.verts[:], vec=Vector((xc, 0, 0))); pieces.append(bout)
    for s in (-1, 1):                          # poteaux sur pierres
        pieces.append(bloc(rnd, (.46, .46, .42), Vector((s * 1.33, 0, -.09))))
        pieces.append(peint(boite((.3, .3, 3.15), Vector((s * 1.33, 0, .12 + 3.15 / 2))), BOIS_PORTE, rnd))
    pieces.append(peint(boite((3.3, .24, .28), Vector((0, 0, 3.0))), BOIS_PORTE, rnd))      # linteau
    pieces.append(peint(boite((2.4, .14, .16), Vector((0, 0, 2.62))), BOIS_PORTE, rnd))     # traverse
    pieces.append(peint(boite((3.0, .5, .3), Vector((0, 0, 3.29))), BOIS_SOMBRE, rnd))      # sous le toit
    pieces.append(peint(cube_vers(lambda c: Vector((c.x * 3.0, c.y * (1.4 if c.z < 0 else .02), 3.44 if c.z < 0 else 3.72))), BOIS_SOMBRE, rnd))
    pieces += toit(rnd, 3.5, 14, dz=.8, debord=1.05)
    pieces.append(peint(boite((2.5, .3, .12), Vector((0, 0, .06))), BOIS_PORTE, rnd))       # seuil
    for s in (-1, 1):                          # vantaux : planches debout, deux barres, clous de fer
        for k in range(5):
            x = s * (.12 + .23 * k + .005)
            ton = tuple(melange(c, BOIS_PORTE[2], rnd.uniform(0, .18)) for c in BOIS_PORTE)
            pieces.append(peint(boite((.225, .08, 2.36), Vector((x, 0, .12 + 1.18))), ton, rnd, .03))
            for z in (.55, 2.0):
                for sy in (-1, 1): pieces.append(peint(boite((.05, .02, .05), Vector((x, sy * .05, z))), FER, rnd))
        for z in (.55, 2.0):
            for sy in (-1, 1): pieces.append(peint(boite((1.17, .03, .14), Vector((s * .595, sy * .055, z))), BOIS_SOMBRE, rnd))
    return assembler(*pieces)


def cylindre_debout(r1, r2, h, z):
    bm = bmesh.new(); bmesh.ops.create_cone(bm, cap_ends=True, segments=8, radius1=r1, radius2=r2, depth=h)
    bmesh.ops.translate(bm, verts=bm.verts[:], vec=Vector((0, 0, z + h / 2))); return bm


# ---------- fabrication ----------
EXPORTER = not os.environ.get("JARDIN_SANS_EXPORT")
modeles = {}
for i, (nom, fab) in enumerate((("Muraille_Mur_1", lambda r: mur(r, 0.0)), ("Muraille_Mur_2", lambda r: mur(r, 23.0)),
                                ("Muraille_Angle", angle), ("Muraille_Porte", porte))):
    rnd = random.Random(2000 + i); bm = fab(rnd); ajouter_contour(bm, .02, TRAIT)
    col = collection(nom); bpy.context.view_layer.layer_collection.children[nom].exclude = False   # rangée au passage d'avant
    modeles[nom] = objet(nom, bm, col)
    if EXPORTER and (nom == "Muraille_Porte" or not GRANDE): exporter(col, os.path.join(DOSSIER, nom + ".fbx"))
    print(f"{nom:16s} {poids(col):6d} faces")

# ---------- la pose sur le contour ----------
X0, X1, Y0, Y1 = json.load(open(DISPO))["bornes"] if os.path.exists(DISPO) else (-18.0, 18.0, -14.0, 14.0)
for v, cote in ((X1 - X0, "x"), (Y1 - Y0, "y")):
    if abs(v / L - round(v / L)) > 1e-6: print(f"ATTENTION : le côté {cote} ({v} m) n'est pas un multiple de {L} m")
pose = []      # nom, x, y, rotation (degrés autour de z)
rnd = random.Random(77)
for y, sens in ((Y0 - W_SOCLE, 0), (Y1 + W_SOCLE, 180)):
    for k in range(round((X1 - X0) / L)):
        nom = rnd.choice(("Muraille_Mur_1", "Muraille_Mur_2")); rot = sens + rnd.choice((0, 180))
        if GRANDE and sens == 0 and k == round((X1 - X0) / L) // 2: nom = "Muraille_Porte"   # au sud, au milieu
        pose.append((nom, X0 + L * (k + .5), y, rot))
for x, sens in ((X0 - W_SOCLE, 90), (X1 + W_SOCLE, 270)):
    for k in range(round((Y1 - Y0) / L)):
        pose.append((rnd.choice(("Muraille_Mur_1", "Muraille_Mur_2")), x, Y0 + L * (k + .5), sens + rnd.choice((0, 180))))
for x in (X0 - W_SOCLE, X1 + W_SOCLE):
    for y in (Y0 - W_SOCLE, Y1 + W_SOCLE): pose.append(("Muraille_Angle", x, y, 0))

muraille = collection("Muraille")
for nom, x, y, rot in pose:
    ob = bpy.data.objects.new(nom + "_pose", modeles[nom].data)
    ob.matrix_world = Matrix.Translation((x, y, 0)) @ Matrix.Rotation(math.radians(rot), 4, "Z"); muraille.objects.link(ob)
for nom in modeles: bpy.context.view_layer.layer_collection.children[nom].exclude = True   # les modules d'origine, rangés
print(f"muraille : {len(pose)} pièces, contour de {2 * (X1 - X0 + Y1 - Y0):.0f} m")
if EXPORTER:   # la pose pour Unity, aux conventions de disposition_jardin.json (x, z, y ; lacet opposé)
    with open(POSE, "w") as fh:
        json.dump(dict(pieces=[dict(nom=n, pos=[round(x, 3), 0.0, round(y, 3)], lacet=round(-r % 360, 2)) for n, x, y, r in pose]),
                  fh, indent=1, ensure_ascii=False)


# ---------- aperçus (avec le jardin s'il est dans la scène) ----------
def apercu(nom, oeil, cible, lens=24, rx=1600, ry=900):
    sc = bpy.context.scene; sc.render.engine = "BLENDER_WORKBENCH"
    sh = sc.display.shading; sh.light = "FLAT"; sh.color_type = "VERTEX"; sh.show_backface_culling = True
    if sc.world is None: sc.world = bpy.data.worlds.new("Monde")
    sc.world.color = (.55, .62, .66)
    sc.render.resolution_x, sc.render.resolution_y = rx, ry
    cam = bpy.data.objects.get("Camera") or bpy.data.objects.new("Camera", bpy.data.cameras.new("Camera"))
    if not cam.users_collection: sc.collection.objects.link(cam)
    cam.data.type = "PERSP"; cam.data.lens = lens; cam.data.clip_end = 300; sc.camera = cam
    cam.location = Vector(oeil); cam.rotation_euler = (Vector(cible) - cam.location).to_track_quat("-Z", "Y").to_euler()
    for c in bpy.data.collections:
        for o in c.objects: o.hide_render = c.name not in ("Apercu", "Muraille")
    os.makedirs(APERCUS, exist_ok=True)
    sc.render.filepath = os.path.join(APERCUS, nom + ".png"); bpy.ops.render.render(write_still=True)
    return sc.render.filepath


if not os.environ.get("MURAILLE_SANS_APERCU"):
    apercu("muraille_ensemble", (-2, -34, 24), (0, 0, -1))
    apercu("muraille_angle", (X1 - 7, Y0 + 5, 2.0), (X1, Y0 - .5, 2.0))
    apercu("muraille_de_pres", (X0 + 9, Y1 - 4.5, 1.2), (X0 + 4, Y1 + .5, 2.2), lens=20)
    if GRANDE: apercu("muraille_porte", ((X0 + X1) / 2 + 3.5, Y0 + 7, 1.6), ((X0 + X1) / 2, Y0 - .5, 2.2), lens=24)
    print("MURAILLE FINIE", APERCUS)
