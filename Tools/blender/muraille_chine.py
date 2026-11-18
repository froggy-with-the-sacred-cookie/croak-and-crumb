"""Muraille de Chine (28 octobre 2026, nuit), pour la vitrine « chine » (VITRINE=chine dans vitrine_jardin.py).
Demande de Florent : « un truc style muraille de Chine » ; premier jet jugé « muraille de château » : un rectangle plat
à tours carrées et toits en pointe (gardé dans Captures/vitrine_chine/premier_jet_*). Cette version suit la crête du
terrain et prend ce qui distingue la muraille d'un château. Proposition, pas de DA : la V1 de Notion est un jardin
japonais, le mélange est à trancher par l'équipe. Faits appliqués (fiche Docs/Recherches/jardin-japonais-kit-2026-10-28.md,
« Muraille de Chine ») :
- elle suit les crêtes des montagnes (Simatai, Jinshanling), en montant et en descendant ; tours de guet rapprochées
  (une tous les 100 à 150 m à Jinshanling), creuses, en maçonnerie, à fenêtres voûtées, terrasse crénelée ;
- profil en trapèze (Badaling : 6,5 à 7,5 m au pied, 4,5 à 5,8 m en haut, 6 à 9 m de haut) ; ici ≈ 0,55 du réel ;
- fondation de pierre de taille, corps de briques ; créneaux (duokou) côté ennemi, grandes ouvertures en haut,
  meurtrières en bas ; portes voûtées côté intérieur ;
- grande porte (Shanhaiguan) : tour de deux étages à double avant-toit, des animaux sculptés aux coins des avant-toits,
  des meurtrières, une plaque ; vantaux rouges à 81 clous dorés (neuf rangs de neuf, le rang de l'empereur).
De mémoire ou par hypothèse : la courbe des toits (plus plats à l'égout, coins relevés), les ornements du faîtage, les
gargouilles, les pavillons sur les tours, la taille des briques peintes (0,4 × 0,2 m), les proportions des tours.
Modèles (le jardin du côté +Y ; un module de mur va de x = −2 à +2 et monte de Δ sur sa longueur) :
Chine_Mur_1_Pm2 … _Pp2 (Δ = −1,2 à +1,2 m par pas de 0,6), Chine_Mur_2 (plat, porte voûtée), Chine_Tour,
Chine_Tour_Pavillon, Chine_Angle, Chine_Porte. Pose : muraille_jardin_chine.json, sur la crête lue dans
disposition_jardin_chine.json (lancer vitrine_jardin.py avec VITRINE=chine avant).
    FROGGY_ROOT=$PWD /Applications/Blender.app/Contents/MacOS/Blender -b -P Tools/blender/muraille_chine.py
JARDIN_SANS_EXPORT=1 : sans écrire les FBX ; MURAILLE_SANS_APERCU=1 : sans les rendus."""
import os, json
_m = open(os.environ["FROGGY_ROOT"] + "/Tools/blender/muraille_jardin.py").read()
exec(_m.split("# ---------- fabrication ----------")[0])     # briques (assembler, peint, cube_vers, boite, bloc…) et tons

DISPO = os.path.join(RACINE, "Assets", "Froggy", "Vitrine", "disposition_jardin_chine.json")
POSE = os.path.join(RACINE, "Assets", "Froggy", "Vitrine", "muraille_jardin_chine.json")
APERCUS = os.path.join(RENDUS, "vitrine_chine")
Z = Vector((0, 0, 1))
Lm, PAS_C = 4.0, .6                         # longueur d'un module ; pas de la crête
PL, DECK = .9, 4.4                          # haut de la fondation, du chemin de ronde
B_BAS, B_HAUT = 1.95, 1.55                  # demi-épaisseurs : au pied, au chemin de ronde
CRENEAU, MERLON_H, PARAPET_BAS = .34, 1.3, .7
PARAPET_INT = .6
BRIQUE = ((206, 196, 178), (166, 158, 144), (112, 106, 100))      # gris beige des briques des Ming, passées
BRIQUE_SOMBRE = ((172, 164, 152), (134, 128, 120), (88, 84, 82))
TROU = (40, 34, 34)
ROUGE = ((226, 84, 64), (186, 50, 40), (110, 30, 32))
ROUGE_SOMBRE = ((150, 52, 44), (112, 36, 34), (70, 24, 26))
OR = ((255, 226, 128), (230, 182, 72), (150, 108, 40))
BLEU = ((70, 94, 150), (44, 62, 112), (26, 36, 70))
VERT_BOIS = ((96, 150, 128), (60, 112, 98), (36, 72, 66))         # le dessous des avant-toits (de mémoire)
TUILE_C = ((128, 134, 142), (90, 94, 104), (56, 58, 68))
TUILE_SOMBRE = tuple(melange(c, (30, 30, 36), .3) for c in TUILE_C)
PIERRE_C = (GRANIT["h"], GRANIT["m"], GRANIT["b"])
rnd_b = random.Random(77)


# ---------- briques ----------
def prisme_rect(zs, xa, xb, ya, yb, pas=.2):
    """Un bloc à section rectangulaire [xa(z), xb(z)] × [ya(z), yb(z)], tranché aux hauteurs zs et découpé tous les
    `pas` le long des faces (de quoi peindre les briques). Même découpage à toutes les hauteurs (le fruit change la
    largeur, pas le nombre de points)."""
    bm = bmesh.new(); anneaux = []
    z = zs[0]
    nx, ny = max(1, round((xb(z) - xa(z)) / pas)), max(1, round((yb(z) - ya(z)) / pas))
    for z in zs:
        x0, x1, y0, y1 = xa(z), xb(z), ya(z), yb(z)
        pts = [(x0 + (x1 - x0) * i / nx, y0) for i in range(nx)] + [(x1, y0 + (y1 - y0) * j / ny) for j in range(ny)]
        pts += [(x1 - (x1 - x0) * i / nx, y1) for i in range(nx)] + [(x0, y1 - (y1 - y0) * j / ny) for j in range(ny)]
        anneaux.append([bm.verts.new((x, y, z)) for x, y in pts])
    for a, b in zip(anneaux, anneaux[1:]):
        for k in range(len(a)): bm.faces.new((a[k], a[(k + 1) % len(a)], b[(k + 1) % len(b)], b[k]))
    bm.faces.new(anneaux[0]); bm.faces.new(anneaux[-1])
    bmesh.ops.recalc_face_normals(bm, faces=bm.faces[:])
    return bm


def tranches_pas(bas, haut, pas=.2):
    n = max(1, round((haut - bas) / pas)); return [bas + (haut - bas) * i / n for i in range(n + 1)]


MOUSSE_C = ((150, 182, 110), (110, 142, 84), (70, 98, 64))


def couleur_brique(z0, tons=BRIQUE):
    """Briques de 0,4 × 0,2 m en appareil alterné, à peine différentes l'une de l'autre : vu de loin, le mur reste une
    seule masse claire (au premier jet, des briques trop marquées faisaient maçonnerie de donjon) ; mousse au pied."""
    def couleur(f):
        c, n = f.calc_center_median(), f.normal
        if abs(n.z) > .9: return ombre(n, *tons, rnd_b, .02)
        r = int(math.floor((c.z - z0) / .2 + 1e-3)); u = c.x if abs(n.y) >= abs(n.x) else c.y
        b = int(math.floor((u + 50 + (r % 2) * .2) / .4))
        h = random.Random(hash((b, r, round(n.x), round(n.y))) & 0xffffffff)
        t = BRIQUE_SOMBRE if h.random() < 1 / 25 else tons
        k = h.uniform(-.035, .035)
        col = ombre(n, *[tuple(min(255, v * (1 + k)) for v in c_) for c_ in t], rnd_b, .005)
        if z0 <= PL + .01 and c.z < z0 + .25 and h.random() < .5: col = melange(col, MOUSSE_C[1], .25)
        return col
    return couleur


def bloc_brique(zs, xa, xb, ya, yb, z0=None):
    bm = prisme_rect(zs, xa, xb, ya, yb); bm.normal_update()
    peindre(bm, couleur_brique(zs[0] if z0 is None else z0)); return bm


def fondation(rnd, x0, x1, ya, yb, gros, assises=((0, .3), (.3, .6), (.6, PL))):
    """Des assises de pierre de taille, joints décalés, un léger retrait à chaque assise."""
    for k, (zb, zh) in enumerate(assises):
        x = x0
        while x < x1 - .01:
            lg = min(rnd.uniform(.7, 1.3), x1 - x)
            if x1 - x - lg < .4: lg = x1 - x
            r = .02 * k
            gros.append(bloc(rnd, (lg - .02, (yb - ya) - 2 * r, zh - zb), Vector((x + lg / 2, (ya + yb) / 2, (zb + zh) / 2)), biseau=.04))
            x += lg


def plaque(centre, a, n, w, h, tons, rnd, arche=True, k=8):
    """Un panneau plat dans le plan de la face (a : horizontale de la face, n : sa normale) : rectangle, ou arche (le haut
    en demi-cercle). Sert aux portes voûtées, fenêtres, meurtrières (peints sombres : des trous), plaques."""
    up = a.cross(n).normalized()
    if up.z < 0: up = -up
    pts = [(-w / 2, 0), (w / 2, 0)]
    if arche:
        r = w / 2; hd = max(0, h - r)
        pts += [(w / 2, hd)] + [(math.cos(math.pi * i / k) * r, hd + math.sin(math.pi * i / k) * r) for i in range(1, k)] + [(-w / 2, hd)]
    else: pts += [(w / 2, h), (-w / 2, h)]
    bm = bmesh.new(); f = bm.faces.new([bm.verts.new(centre + a * x + up * y + n * .006) for x, y in pts])
    bm.normal_update()
    if f.normal.dot(n) < 0: bmesh.ops.reverse_faces(bm, faces=[f])
    bmesh.ops.triangulate(bm, faces=bm.faces[:], quad_method="BEAUTY", ngon_method="EAR_CLIP")
    return peint(bm, tons, rnd, .02)


def boite_axes(centre, axes):
    """Une boîte dont les trois axes sont donnés : [(vecteur unitaire, longueur), ...]."""
    return cube_vers(lambda c: centre + axes[0][0] * c.x * axes[0][1] + axes[1][0] * c.y * axes[1][1] + axes[2][0] * c.z * axes[2][1])


def face_y(z, cote):
    """La face du corps à la hauteur z (fruit du mur) : y, et sa normale (côté -1 dehors, +1 jardin)."""
    y = cote * (B_BAS + (B_HAUT - B_BAS) * (z - PL) / (DECK - PL))
    n = Vector((0, cote * (DECK - PL), (B_BAS - B_HAUT))).normalized()
    return y, n


def creneaux(rnd, x0, x1, y_ext, ep, gros, fins, z_bas, periode=4 / 3, decal=2 / 3, hauteur=MERLON_H, cote=-1, axe="x", bas=PARAPET_BAS):
    """Le parapet crénelé : un bandeau plein jusqu'à `bas`, puis des merlons étroits séparés par des créneaux tous les
    `periode`. Dans chaque merlon, une petite fenêtre de guet en haut ; dans le bandeau, une meurtrière sous chaque
    créneau (« grandes ouvertures en haut, petites en bas »)."""
    def B(xa, xb, za, zb, couverture=False):
        hi = (y_ext, y_ext + ep * -cote); ya, yb = min(hi), max(hi)
        if axe == "x": bm = bloc_brique(tranches_pas(za, zb), lambda z: xa, lambda z: xb, lambda z: ya, lambda z: yb, z_bas)
        else: bm = bloc_brique(tranches_pas(za, zb), lambda z: ya, lambda z: yb, lambda z: xa, lambda z: xb, z_bas)
        gros.append(bm)
        if couverture:
            c = Vector(((xa + xb) / 2, (ya + yb) / 2, zb + .04)) if axe == "x" else Vector(((ya + yb) / 2, (xa + xb) / 2, zb + .04))
            d = (xb - xa + .02, yb - ya + .06, .08) if axe == "x" else (yb - ya + .06, xb - xa + .02, .08)
            fins.append(peint(boite(d, c), BRIQUE_SOMBRE, rnd, .02))

    def trou(u, z, w, h):
        n = Vector((0, cote, 0)) if axe == "x" else Vector((cote, 0, 0))
        a = Vector((1, 0, 0)) if axe == "x" else Vector((0, 1, 0))
        c = Vector((u, y_ext, z)) if axe == "x" else Vector((y_ext, u, z))
        fins.append(plaque(c, a, n, w, h, (TROU,) * 3, rnd, arche=False))
    B(x0, x1, z_bas, z_bas + bas)
    bords = [x0]
    k = math.floor((x0 - decal) / periode)
    while True:
        xc = decal + k * periode; k += 1
        if xc - CRENEAU / 2 >= x1: break
        if xc + CRENEAU / 2 <= x0: continue
        bords += [max(x0, xc - CRENEAU / 2), min(x1, xc + CRENEAU / 2)]
        if x0 < xc < x1: trou(xc, z_bas + bas * .35, .12, .14)
    bords.append(x1)
    for xa, xb in zip(bords[::2], bords[1::2]):
        if xb - xa < .05: continue
        B(xa, xb, z_bas + bas, z_bas + hauteur, True)
        if xb - xa > .5: trou((xa + xb) / 2, z_bas + hauteur - .32, .14, .16)
    fins.append(peint(boite((x1 - x0, ep + .06, .06), Vector(((x0 + x1) / 2, y_ext + ep * -cote / 2, z_bas + bas + .03))) if axe == "x"
                      else boite((ep + .06, x1 - x0, .06), Vector((y_ext + ep * -cote / 2, (x0 + x1) / 2, z_bas + bas + .03))), BRIQUE_SOMBRE, rnd, .02))


def dallage(x0, x1, y0, y1, z, rnd, fins, pas=.5):
    """Un dallage de briques carrées, un ton par dalle (un seul facteur : gris, pas arc-en-ciel)."""
    bm = bmesh.new(); nx, ny = max(1, round((x1 - x0) / pas)), max(1, round((y1 - y0) / pas))
    for i in range(nx):
        for j in range(ny):
            xa, xb = x0 + (x1 - x0) * i / nx, x0 + (x1 - x0) * (i + 1) / nx
            ya, yb = y0 + (y1 - y0) * j / ny, y0 + (y1 - y0) * (j + 1) / ny
            bm.faces.new([bm.verts.new((x, y, z)) for x, y in ((xa + .01, ya + .01), (xb - .01, ya + .01), (xb - .01, yb - .01), (xa + .01, yb - .01))])
    bm.normal_update()
    peindre(bm, lambda f: (lambda k: tuple(v * k for v in BRIQUE[0]))(rnd.uniform(.92, 1.04)))
    fins.append(bm)


def cisailler(bm, delta):
    """Le module monte de delta sur sa longueur : chaque point est relevé de delta × (x/L + ½)."""
    if delta:
        for v in bm.verts: v.co.z += (v.co.x / Lm + .5) * delta
    return bm


# ---------- toits chinois ----------
def toit_chinois(W, D, z0, hr, rnd, gros, fins, releve=.38, t_max=1.0, epaisseur=.16):
    """Toit en croupe à la chinoise (forme de mémoire) : plus plat à l'égout qu'au faîte (z = z0 + hr·t²), les quatre
    coins relevés, des rangs de tuiles en bandes du bas vers le haut, un dessous peint ; un faîtage à ornements dressés
    aux bouts ; des arêtiers qui portent trois petites bêtes (Shanhaiguan : des bêtes sculptées aux coins des
    avant-toits). t_max < 1 : seulement l'avant-toit bas d'un double avant-toit."""
    n_t, nxs, nys = 7, 10, 6
    rx = max(.3, (W - D) / 2)

    def anneau(t):
        hx, hy = W / 2 + (rx - W / 2) * t, max(.04, D / 2 * (1 - t))
        pts = [(-hx + 2 * hx * i / nxs, -hy) for i in range(nxs)] + [(hx, -hy + 2 * hy * j / nys) for j in range(nys)]
        pts += [(hx - 2 * hx * i / nxs, hy) for i in range(nxs)] + [(-hx, hy - 2 * hy * j / nys) for j in range(nys)]
        return [Vector((x, y, z0 + hr * t * t + releve * ((abs(x) / hx) * (abs(y) / hy)) ** 4 * (1 - t) ** 2)) for x, y in pts]
    anneaux_ = [anneau(t_max * i / n_t) for i in range(n_t + 1)]
    bm = bmesh.new(); bande = bm.faces.layers.int.new("bande")
    vs = [[bm.verts.new(p) for p in a] for a in anneaux_]
    m = len(vs[0])
    for a, b in zip(vs, vs[1:]):
        for k in range(m):
            f = bm.faces.new((a[k], a[(k + 1) % m], b[(k + 1) % m], b[k])); f[bande] = k
    if t_max >= 1: bm.faces.new(vs[-1])
    bas = [bm.verts.new(v.co - Z * epaisseur) for v in vs[0]]
    for k in range(m):
        f = bm.faces.new((bas[k], bas[(k + 1) % m], vs[0][(k + 1) % m], vs[0][k])); f[bande] = -1
    centre = bm.verts.new(Vector((0, 0, z0 + .05)))
    for k in range(m):
        f = bm.faces.new((centre, bas[(k + 1) % m], bas[k])); f[bande] = -2
    bmesh.ops.recalc_face_normals(bm, faces=bm.faces[:]); bm.normal_update()

    def couleur(f):
        b = f[bande]
        if b == -2: return ombre(f.normal, *VERT_BOIS, rnd, .03)
        if b == -1: return ombre(f.normal, *TUILE_SOMBRE, rnd, .02)
        return ombre(f.normal, *[melange(c, (40, 42, 50), .14 * (b % 2)) for c in TUILE_C], rnd, .02)   # les rangs de tuiles
    peindre(bm, couleur); gros.append(bm)
    if t_max < 1: return
    zf = z0 + hr
    gros.append(peint(boite((2 * rx + .25, .2, .24), Vector((0, 0, zf + .1))), TUILE_SOMBRE, rnd, .02))
    for s in (-1, 1):       # ornements du faîtage, dressés et recourbés vers l'intérieur
        o = boite_axes(Vector((s * (rx + .12), 0, zf + .4)), [(Vector((1, 0, 0)), .16), (Vector((0, 1, 0)), .14), (Z, .42)])
        bmesh.ops.rotate(o, verts=o.verts[:], cent=Vector((s * (rx + .12), 0, zf + .2)), matrix=Matrix.Rotation(-s * .3, 3, "Y"))
        fins.append(peint(o, TUILE_C, rnd, .02))
        fins.append(peint(boite((.22, .16, .1), Vector((s * (rx + .02), 0, zf + .66))), TUILE_C, rnd, .02))
    for idx in (0, nxs, nxs + nys, 2 * nxs + nys):     # arêtiers : ils suivent la courbe du toit, d'un coin d'anneau à l'autre
        pts = [a[idx] for a in anneaux_]
        for j, (p, q) in enumerate(zip(pts, pts[1:])):
            v = q - p; L = v.length; v.normalize()
            cote = v.cross(Z).normalized(); up = cote.cross(v).normalized()
            if up.z < 0: up = -up
            fins.append(peint(boite_axes((p + q) / 2 + up * .07, [(v, L + .04), (cote, .13), (up, .12)]), TUILE_SOMBRE, rnd, .02))
            if j < 2:                                       # les bêtes, près du coin
                for i in range(2 - j):
                    fins.append(peint(boite_axes(p + v * (.2 + .2 * i) + up * .19, [(v, .1), (cote, .07), (Z, .14)]), TUILE_C, rnd, .02))


def salle(rnd, W, D, z0, h, gros, fins, treillis=("-y", "+y")):
    """Un corps de pavillon en bois : colonnes rouges, murs rouge sombre, treillis sur les faces données, poutre peinte."""
    gros.append(peint(boite((W - .3, D - .3, h), Vector((0, 0, z0 + h / 2))), ROUGE_SOMBRE, rnd, .02))
    nxc, nyc = max(1, round(W / 1.3)), max(1, round(D / 1.3))
    poteaux = [(-W / 2 + W * i / nxc, s * D / 2) for i in range(nxc + 1) for s in (-1, 1)]
    poteaux += [(s * W / 2, -D / 2 + D * j / nyc) for j in range(1, nyc) for s in (-1, 1)]
    for x, y in poteaux:
        p = peint(cylindre_debout(.11, .11, h, z0), ROUGE, rnd, .02)
        bmesh.ops.translate(p, verts=p.verts[:], vec=Vector((x, y, 0))); gros.append(p)
    gros.append(peint(boite((W + .1, D + .1, .24), Vector((0, 0, z0 + h - .12))), VERT_BOIS, rnd, .02))
    for cote_ in treillis:
        s = -1 if cote_ == "-y" else 1; y = s * (D / 2 - .14)
        for i in range(int((W - .4) / .18) + 1):
            fins.append(peint(boite((.03, .02, h * .5), Vector((-W / 2 + .2 + i * .18, y + s * .01, z0 + h * .55))), ROUGE, rnd, .02))
        for j in range(4):
            fins.append(peint(boite((W - .4, .02, .03), Vector((0, y + s * .012, z0 + h * .32 + j * h * .15))), ROUGE, rnd, .02))


# ---------- les éléments ----------
def mur(rnd, delta=0.0, porte_voutee=False):
    """Un module de 4 m le long de x, le jardin du côté +Y, qui monte de delta : fondation, corps de briques en fruit,
    chemin de ronde dallé, créneaux serrés côté dehors, parapet bas côté jardin, gargouille ; _2 : une porte voûtée."""
    gros, fins = [], []
    fondation(rnd, -Lm / 2, Lm / 2, -B_BAS - .08, B_BAS + .08, gros)
    gros.append(bloc_brique(tranches_pas(PL, DECK), lambda z: -Lm / 2, lambda z: Lm / 2, lambda z: face_y(z, -1)[0], lambda z: face_y(z, 1)[0]))
    dallage(-Lm / 2, Lm / 2, -B_HAUT + .45, B_HAUT - .4, DECK + .005, rnd, fins)
    creneaux(rnd, -Lm / 2, Lm / 2, -B_HAUT, .45, gros, fins, DECK)
    gros.append(bloc_brique(tranches_pas(DECK, DECK + PARAPET_INT), lambda z: -Lm / 2, lambda z: Lm / 2, lambda z: B_HAUT - .4, lambda z: B_HAUT, DECK))
    fins.append(peint(boite((Lm, .46, .07), Vector((0, B_HAUT - .2, DECK + PARAPET_INT + .035))), BRIQUE_SOMBRE, rnd, .02))
    xg = 1.4 if porte_voutee else rnd.uniform(-1.2, 1.2)
    y, n = face_y(DECK - .3, 1)
    fins.append(bloc(rnd, (.22, .5, .14), Vector((xg, y + .22, DECK - .3)), biseau=.02))
    fins.append(plaque(Vector((xg, y + .47, DECK - .34)), Vector((1, 0, 0)), Vector((0, 1, 0)), .1, .07, (TROU,) * 3, rnd, arche=False))
    if porte_voutee:
        y, n = face_y(PL, 1); a = Vector((1, 0, 0))
        fins.append(plaque(Vector((0, y, PL)), a, n, 1.25, 1.95, PIERRE_C, rnd))
        fins.append(plaque(Vector((0, y + .003, PL)) + n * .004, a, n, .95, 1.8, (TROU,) * 3, rnd))
    for bm in gros + fins: cisailler(bm, delta)
    return gros, fins


def fenetre(c, a, n, rnd, fins, w=.5, h=.85):
    fins.append(plaque(c, a, n, w + .2, h + .12, PIERRE_C, rnd))
    fins.append(plaque(c + n * .004 + Z * .06, a, n, w, h, (TROU,) * 3, rnd))


def corps_de_tour(rnd, xa, xb, ya, yb, gros, fins, portes_x=True, pavillon=False):
    """Une tour de guet trapue sur le chemin de ronde : salle de briques à fenêtres voûtées alignées (trois par face
    longue), portes voûtées là où passe le chemin de ronde, terrasse dallée et crénelée ; un pavillon s'il le faut."""
    H1 = DECK + 2.5
    gros.append(bloc_brique(tranches_pas(DECK, H1), lambda z: xa, lambda z: xb, lambda z: ya, lambda z: yb, PL))
    gros.append(peint(boite((xb - xa + .18, yb - ya + .18, .14), Vector(((xa + xb) / 2, (ya + yb) / 2, H1 + .07))), BRIQUE_SOMBRE, rnd, .02))
    X, Yv = Vector((1, 0, 0)), Vector((0, 1, 0))
    for a, n, lo, hi, fixe, nb in ((X, -Yv, xa, xb, ya, 3), (X, Yv, xa, xb, yb, 3),
                                   (Yv, -X, ya, yb, xa, 0 if portes_x else 3), (Yv, X, ya, yb, xb, 0 if portes_x else 3)):
        for i in range(nb):
            u = lo + (hi - lo) * (i + .5) / nb
            fenetre(Vector((u, fixe, DECK + .7)) if a.x else Vector((fixe, u, DECK + .7)), a, n, rnd, fins)
    if portes_x:
        for s, x in ((-1, xa), (1, xb)):
            fins.append(plaque(Vector((x, (ya + yb) / 2 - .2, DECK)), Yv, Vector((s, 0, 0)), 1.0, 1.75, PIERRE_C, rnd))
            fins.append(plaque(Vector((x + s * .004, (ya + yb) / 2 - .2, DECK)), Yv, Vector((s, 0, 0)), .8, 1.6, (TROU,) * 3, rnd))
    z = H1 + .14; ep = .3
    dallage(xa + ep, xb - ep, ya + ep, yb - ep, z + .005, rnd, fins)
    kw = dict(periode=1.0, hauteur=.95, bas=.55)
    creneaux(rnd, xa, xb, ya, ep, gros, fins, z, decal=(xa + xb) / 2 + .5, cote=-1, axe="x", **kw)
    creneaux(rnd, xa, xb, yb, ep, gros, fins, z, decal=(xa + xb) / 2 + .5, cote=1, axe="x", **kw)
    creneaux(rnd, ya + ep, yb - ep, xa, ep, gros, fins, z, decal=(ya + yb) / 2 + .5, cote=-1, axe="y", **kw)
    creneaux(rnd, ya + ep, yb - ep, xb, ep, gros, fins, z, decal=(ya + yb) / 2 + .5, cote=1, axe="y", **kw)
    if pavillon:
        W, D = min(2.8, xb - xa - 1.4), min(2.0, yb - ya - 1.4)
        loc_g, loc_f = [], []
        salle(rnd, W, D, z, 1.6, loc_g, loc_f)
        toit_chinois(W + 1.1, D + 1.1, z + 1.6, .95, rnd, loc_g, loc_f)
        c = Vector(((xa + xb) / 2, (ya + yb) / 2, 0))
        for bm in loc_g + loc_f: bmesh.ops.translate(bm, verts=bm.verts[:], vec=c)
        gros += loc_g; fins += loc_f


def massif(rnd, xa, xb, ya, yb, gros):
    """Fondation et corps de briques en fruit d'une tour, jusqu'au chemin de ronde ; rend le haut du corps."""
    fondation(rnd, xa - .08, xb + .08, ya - .08, yb + .08, gros)
    r = (DECK - PL) * .05
    gros.append(bloc_brique(tranches_pas(PL, DECK), lambda z: xa + (z - PL) * .05, lambda z: xb - (z - PL) * .05,
                            lambda z: ya + (z - PL) * .05, lambda z: yb - (z - PL) * .05))
    return xa + r, xb - r, ya + r, yb - r


def tour_mur(rnd, pavillon=False):
    """Tour de guet de la ligne : plus large que le mur (4,8 m) et débordant des deux côtés, trapue."""
    gros, fins = [], []
    corps_de_tour(rnd, *massif(rnd, -2.4, 2.4, -B_BAS - .9, B_BAS + .7, gros), gros, fins, True, pavillon)
    return gros, fins


def angle(rnd):
    """Tour d'angle, carrée et trapue (6 m) : le chemin de ronde y bute des deux côtés."""
    gros, fins = [], []
    corps_de_tour(rnd, *massif(rnd, -3.0, 3.0, -3.0, 3.0, gros), gros, fins, False, False)
    return gros, fins


def porte(rnd):
    """La porte (à la manière de Shanhaiguan) : un massif de briques percé côté jardin d'une arche à vantaux rouges
    (9 × 9 clous dorés), et dessus une tour de bois à deux étages et double avant-toit, plaque bleue à cadre doré."""
    gros, fins = [], []
    ya, yb = -B_BAS - .6, B_BAS + .6
    xa_, xb_, ya_, yb_ = massif(rnd, -2.6, 2.6, ya, yb, gros)
    creneaux(rnd, xa_, xb_, ya_, .4, gros, fins, DECK, periode=1.0, decal=.5, hauteur=1.0, bas=.6)
    gros.append(bloc_brique(tranches_pas(DECK, DECK + .6), lambda z: xa_, lambda z: xb_, lambda z: yb_ - .4, lambda z: yb_, DECK))
    y0 = yb + .08 + .05; c = Vector((0, y0, 0)); a = Vector((1, 0, 0)); n = Vector((0, 1, 0))
    fins.append(plaque(c, a, n, 2.6, 3.3, PIERRE_C, rnd))
    fins.append(plaque(c + n * .004, a, n, 2.2, 3.05, (TROU,) * 3, rnd))
    for s in (-1, 1):
        x0 = s * .55
        fins.append(peint(boite((1.06, .08, 2.5), Vector((x0, y0 + .05, 1.25))), ROUGE, rnd, .02))
        for i in range(9):
            for j in range(9):
                clou = bmesh.new(); bmesh.ops.create_icosphere(clou, subdivisions=1, radius=.026)
                for v in clou.verts: v.co.y *= .7
                bmesh.ops.translate(clou, verts=clou.verts[:], vec=Vector((x0 - .4 + .1 * i, y0 + .1, .25 + .26 * j)))
                fins.append(peint(clou, OR, rnd, .03))
        fins.append(peint(boite((.06, .03, .06), Vector((s * .1, y0 + .11, 1.3))), OR, rnd, .02))
    # la tour de bois : deux étages, double avant-toit, plaque sur l'étage
    W, D = 5.6, 3.6; z = DECK + .005; yc = (ya + yb) / 2 + .05
    gros.append(peint(boite((W + .5, D + .5, .25), Vector((0, yc, z + .12))), PIERRE_C, rnd, .02))
    loc_g, loc_f = [], []
    salle(rnd, W, D, z + .25, 2.0, loc_g, loc_f)
    toit_chinois(W + 1.3, D + 1.3, z + 2.2, .55, rnd, loc_g, loc_f, releve=.32, t_max=.5)
    salle(rnd, W - 1.0, D - .9, z + 2.45, 1.45, loc_g, loc_f)
    toit_chinois(W + .6, D + 1.0, z + 3.9, 1.45, rnd, loc_g, loc_f, releve=.45)
    loc_f.append(peint(boite((1.5, .08, .55), Vector((0, (D - .9) / 2 + .06, z + 3.25))), BLEU, rnd, .02))
    loc_f.append(peint(boite((1.62, .06, .67), Vector((0, (D - .9) / 2 + .03, z + 3.25))), OR, rnd, .02))
    for bm in loc_g + loc_f: bmesh.ops.translate(bm, verts=bm.verts[:], vec=Vector((0, yc, 0)))
    return gros + loc_g, fins + loc_f


# ---------- fabrication ----------
EXPORTER = not os.environ.get("JARDIN_SANS_EXPORT")
PENTES = {-2: "Pm2", -1: "Pm1", 0: "P0", 1: "Pp1", 2: "Pp2"}
FABRIQUES = [(f"Chine_Mur_1_{nom_p}", lambda r, k=k: mur(r, k * PAS_C)) for k, nom_p in PENTES.items()]
FABRIQUES += [("Chine_Mur_2", lambda r: mur(r, 0.0, True)), ("Chine_Tour", tour_mur), ("Chine_Tour_Pavillon", lambda r: tour_mur(r, True)),
              ("Chine_Angle", angle), ("Chine_Porte", porte)]
for ancien in ("Chine_Mur_1", "Chine_Mur_2", "Chine_Tour", "Chine_Angle", "Chine_Porte", "Muraille_Chine"):   # le premier jet, rangé
    if ancien in bpy.context.view_layer.layer_collection.children: bpy.context.view_layer.layer_collection.children[ancien].exclude = True
modeles = {}
for i, (nom, fab) in enumerate(FABRIQUES):
    rnd = random.Random(7000 + (0 if nom.startswith("Chine_Mur_1") else i)); gros, fins = fab(rnd)
    bm = assembler(*gros)
    if nom.startswith("Chine_Mur"):   # les bouts cachés par le voisin : sans eux, le contour trace un joint tous les 4 m
        bm.normal_update()
        bmesh.ops.delete(bm, geom=[f for f in bm.faces if abs(f.normal.x) > .95 and abs(abs(f.calc_center_median().x) - Lm / 2) < .004], context="FACES")
    ajouter_contour(bm, .02, TRAIT)
    for f in fins: fusionner(bm, f)
    col = collection(nom); bpy.context.view_layer.layer_collection.children[nom].exclude = False
    modeles[nom] = objet(nom, bm, col)
    if EXPORTER: exporter(col, os.path.join(DOSSIER, nom + ".fbx"))
    print(f"{nom:22s} {poids(col):6d} faces")

# ---------- la pose : sur la crête, le parement intérieur au bord du terrain ----------
dispo = json.load(open(DISPO))
X0, X1, Y0, Y1 = dispo["bornes"]; CRETE = dispo["crete"]; TOURS = sorted(dispo["tours"]); PORTE = dispo["porte"]
N = len(CRETE); nx, ny = round((X1 - X0) / Lm), round((Y1 - Y0) / Lm)
c = B_BAS + .08
PAVILLONS = set(sorted(TOURS, key=lambda i: -CRETE[i])[:2])        # les deux tours les plus hautes portent un pavillon
pose = []
rnd = random.Random(78)
cotes = [(0, nx, lambda k: (X0 + Lm * (k + .5), Y0 - c), 0), (nx, ny, lambda k: (X1 + c, Y0 + Lm * (k + .5)), 90),
         (nx + ny, nx, lambda k: (X1 - Lm * (k + .5), Y1 + c), 180), (2 * nx + ny, ny, lambda k: (X0 - c, Y1 - Lm * (k + .5)), 270)]
for i0, n, ou, rot in cotes:
    for k in range(n):
        i = i0 + k; h = CRETE[i]; d = round((CRETE[(i + 1) % N] - h) / PAS_C)
        if i == PORTE: nom = "Chine_Porte"
        elif i in TOURS: nom = "Chine_Tour_Pavillon" if i in PAVILLONS else "Chine_Tour"
        elif d == 0 and rnd.random() < .3: nom = "Chine_Mur_2"
        else: nom = f"Chine_Mur_1_{PENTES[d]}"
        x, y = ou(k); pose.append((nom, x, y, h, rot))
for i, (x, y) in ((0, (X0 - c, Y0 - c)), (nx, (X1 + c, Y0 - c)), (nx + ny, (X1 + c, Y1 + c)), (2 * nx + ny, (X0 - c, Y1 + c))):
    pose.append(("Chine_Angle", x, y, CRETE[i], 0))
# ---------- au-delà : des montagnes, et la muraille qui s'en va sur leurs crêtes ----------
# Un rectangle fermé reste un plan de ville fortifiée : ce qui dit « Grande Muraille », c'est le mur qui part au loin
# sur les arêtes (Simatai, Jinshanling). Décor seulement : personne ne sort de l'arène.
R_X, R_Y = X1 + 9.0, Y1 + 9.0                     # le sol de la vitrine s'arrête là (MARGE de vitrine_jardin.py)


def montagne_brute(x, y):
    e = max(abs(x) - R_X, abs(y) - R_Y, 0.0)
    t = min(1.0, e / 28.0); t = t * t * (3 - 2 * t)
    n = noise.noise(Vector((x * .028, y * .028, 3.1))) * .5 + .5
    d = noise.noise(Vector((x * .09, y * .09, 7.7)))
    return t * (5.0 + 13.0 * n) + min(1.0, e / 10.0) * 1.4 * d


def chemin(depart, cap, n, virages):
    """Une ligne de n modules de 4 m depuis `depart`, le cap tournant de virages[i] degrés à chaque module."""
    pts, a = [depart], math.radians(cap)
    for i in range(n):
        a += math.radians(virages[i % len(virages)]); pts.append(pts[-1] + Vector((math.cos(a), math.sin(a), 0)) * Lm)
    return pts


branches = []
for coin_i, coin, cap, virages, n_b in ((nx + ny, Vector((X1 + c, Y1 + c, 0)), 45, [6, 5, 3, 0, -4, -6, -6, -3, 0, 4, 6, 4], 26),
                                        (2 * nx + ny, Vector((X0 - c, Y1 + c, 0)), 150, [-3, -5, -4, 0, 4, 6, 5, 2, -2, -5], 22)):
    d0 = Vector((math.cos(math.radians(cap)), math.sin(math.radians(cap)), 0))
    pts = chemin(coin + d0 * 3.0, cap, n_b, virages)
    hs = [CRETE[coin_i]]
    for k in range(n_b):
        cible = max(montagne_brute(pts[k + 1].x, pts[k + 1].y), 1.0)
        pas_ = max(-2, min(2, round((cible - hs[-1]) / PAS_C)))
        if k % 6 == 5: pas_ = 0                                     # une tour tous les six modules : plat
        hs.append(hs[-1] + pas_ * PAS_C)
    branches.append((pts, hs))


def chemin_proche(x, y):
    """La hauteur de la muraille au plus près de (x, y), et la distance à sa ligne."""
    meilleur = (1e9, 0.0)
    for pts, hs in branches:
        for k in range(len(pts) - 1):
            a, b = pts[k], pts[k + 1]; ab = b - a
            t = max(0.0, min(1.0, ((x - a.x) * ab.x + (y - a.y) * ab.y) / ab.length_squared))
            dist = math.hypot(x - (a.x + ab.x * t), y - (a.y + ab.y * t))
            if dist < meilleur[0]: meilleur = (dist, hs[k] + (hs[k + 1] - hs[k]) * t)
    return meilleur


def montagne(x, y):
    h = montagne_brute(x, y)
    dist, hc = chemin_proche(x, y)
    if dist < 9.0:                                  # sous la muraille, le sol suit sa ligne ; il s'y raccorde en 6 m
        w = 1.0 if dist < 3.0 else 1 - (dist - 3.0) / 6.0
        w = w * w * (3 - 2 * w); h = h + (hc - h) * w
    return h


HERBE_M = ((150, 188, 104), (110, 154, 82), (66, 104, 66))
FORET_M = ((96, 146, 86), (64, 112, 70), (38, 74, 54))
ROC_M = ((176, 172, 162), (140, 136, 130), (92, 90, 92))
PAS_M = 3.0; ex, ey = 130.0, 120.0
gm = bmesh.new(); nmx, nmy = int(2 * ex / PAS_M), int(2 * ey / PAS_M)
vm = [[None] * (nmy + 1) for _ in range(nmx + 1)]
for i in range(nmx + 1):
    for j in range(nmy + 1):
        x, y = -ex + i * PAS_M, -ey + j * PAS_M
        dedans = abs(x) < R_X - .5 and abs(y) < R_Y - .5
        vm[i][j] = gm.verts.new((x, y, -.4 if dedans else montagne(x, y)))
for i in range(nmx):
    for j in range(nmy):
        q = (vm[i][j], vm[i + 1][j], vm[i + 1][j + 1], vm[i][j + 1])
        if all(abs(v.co.x) < R_X - .5 and abs(v.co.y) < R_Y - .5 for v in q): continue   # sous le sol de la vitrine
        gm.faces.new(q)
bmesh.ops.delete(gm, geom=[v for v in gm.verts if not v.link_faces], context="VERTS")
bmesh.ops.triangulate(gm, faces=gm.faces[:]); gm.normal_update()
rnd_m = random.Random(5)


def couleur_montagne(f):
    cz, n = f.calc_center_median().z, f.normal
    k = noise.noise(f.calc_center_median() * .05) * 3
    if n.z < .72 or cz > 13 + k: return ombre(n, *ROC_M, rnd_m, .04)
    if cz > 4 + k: return ombre(n, *FORET_M, rnd_m, .04)
    return ombre(n, *HERBE_M, rnd_m, .03)
peindre(gm, couleur_montagne)
col_m = collection("Chine_Montagnes"); bpy.context.view_layer.layer_collection.children["Chine_Montagnes"].exclude = False
modeles["Chine_Montagnes"] = objet("Chine_Montagnes", gm, col_m)
if EXPORTER: exporter(col_m, os.path.join(DOSSIER, "Chine_Montagnes.fbx"))
print(f"montagnes : {poids(col_m)} faces")
pose.append(("Chine_Montagnes", 0.0, 0.0, 0.0, 0))
for pts, hs in branches:
    for k in range(len(pts) - 1):
        a, b = pts[k], pts[k + 1]; m_ = (a + b) / 2
        d = round((hs[k + 1] - hs[k]) / PAS_C)
        nom = "Chine_Tour" if k % 6 == 5 else f"Chine_Mur_1_{PENTES[d]}"
        pose.append((nom, m_.x, m_.y, hs[k], math.degrees(math.atan2(b.y - a.y, b.x - a.x))))
    print(f"branche : {len(pts) - 1} modules, de {hs[0]:.1f} à {max(hs):.1f} m")
muraille = collection("Muraille_Chine2")
for nom, x, y, h, rot in pose:
    ob = bpy.data.objects.new(nom + "_pose", modeles[nom].data)
    ob.matrix_world = Matrix.Translation((x, y, h)) @ Matrix.Rotation(math.radians(rot), 4, "Z"); muraille.objects.link(ob)
for nom in modeles:
    if nom != "Chine_Montagnes": bpy.context.view_layer.layer_collection.children[nom].exclude = True
print(f"muraille de Chine : {len(pose)} pièces, crête de {min(CRETE):.1f} à {max(CRETE):.1f} m")
if EXPORTER:
    with open(POSE, "w") as fh:
        json.dump(dict(pieces=[dict(nom=n_, pos=[round(x, 3), round(h, 3), round(y, 3)], lacet=round(-r % 360, 2)) for n_, x, y, h, r in pose]),
                  fh, indent=1, ensure_ascii=False)


def apercu_c(nom, oeil, cible, lens=24):
    sc = bpy.context.scene; sc.render.engine = "BLENDER_WORKBENCH"
    sh = sc.display.shading; sh.light = "FLAT"; sh.color_type = "VERTEX"; sh.show_backface_culling = True
    sc.render.resolution_x, sc.render.resolution_y = 1600, 900
    cam = bpy.data.objects.get("Camera") or bpy.data.objects.new("Camera", bpy.data.cameras.new("Camera"))
    if not cam.users_collection: sc.collection.objects.link(cam)
    cam.data.type = "PERSP"; cam.data.lens = lens; cam.data.clip_end = 300; sc.camera = cam
    cam.location = Vector(oeil); cam.rotation_euler = (Vector(cible) - cam.location).to_track_quat("-Z", "Y").to_euler()
    for c_ in bpy.data.collections:
        for o in c_.objects: o.hide_render = c_.name not in ("Apercu", "Muraille_Chine2")
    sc.camera.data.clip_end = 400
    os.makedirs(APERCUS, exist_ok=True)
    sc.render.filepath = os.path.join(APERCUS, nom + ".png"); bpy.ops.render.render(write_still=True)


if not os.environ.get("MURAILLE_SANS_APERCU"):
    apercu_c("chine_ensemble", (-2, -48, 30), (0, 0, 0))
    apercu_c("chine_lointain", (8, -14, 3.5), (-20, 60, 10), 22)
    apercu_c("chine_survol", (40, -95, 55), (0, 25, 0), 24)
    apercu_c("chine_porte", (1.5, Y0 + 11, 2.6), (0, Y0 - 1, 5.0))
    apercu_c("chine_crete", (6, 2, 3), (-2, Y1 + 2, 6.5))
    apercu_c("chine_dessus", (X1 - 3, Y0 + 9, 11.5), (X1 + 1.5, Y0 + 16, 7.0), 22)
    print("MURAILLE DE CHINE FINIE", APERCUS)
