"""Muraille artisanale (28 octobre 2026, nuit) : la même muraille que muraille_jardin.py, détaillée comme la feraient
des artisans. Demande de Florent : « plus faite par des artisans ». Proposition, pas de DA. Faits appliqués (fiche
Docs/Recherches/jardin-japonais-kit-2026-10-28.md, « Muraille artisanale ») :
- toit hongawarabuki : des tuiles plates et creuses (hiragawara) posées d'abord, rang par rang depuis l'égout, puis des
  tuiles rondes (marugawara) sur leurs joints ; à l'égout, en alternance, des tuiles rondes fermées par un disque (gatō)
  et des tuiles plates à lèvre pendante ;
- faîtage : des tuiles épaisses empilées (noshigawara), coiffées d'une tuile ronde (ganburigawara) ; tuiles d'ogre
  (onigawara) au bout des faîtages et aux angles ;
- tsuiji : terre et argile damées entre des planches (seki-ita), en couches (hanchiku) ; poteaux inclinés tous les
  1,8 à 2 m ; cinq lignes blanches d'enduit ;
- soubassement en uchikomi-hagi : des pierres dégrossies au marteau, ajustées les unes aux autres, sans mortier.
De mémoire ou par hypothèse (dit dans la fiche) : la terre qui apparaît en strates là où l'enduit est tombé, les
traces des joints de planches, le dessin des tuiles d'ogre, le bossage du disque, les pentures des vantaux.
Le fait main : chaque tuile est posée un peu de travers et n'a pas tout à fait le ton de sa voisine.
Nouveaux FBX, l'ancienne muraille reste : Muraille_Artisan_Mur_1, _Mur_2, Muraille_Artisan_Angle,
Muraille_Artisan_Porte ; la pose part dans muraille_jardin[_grande]_artisan.json (VITRINE=grande comme pour
muraille_jardin.py). Petites pièces (tuiles, disques, éclats) sans contour : il est réservé aux grandes masses.
    FROGGY_ROOT=$PWD /Applications/Blender.app/Contents/MacOS/Blender -b -P Tools/blender/muraille_artisanale.py
JARDIN_SANS_EXPORT=1 : sans écrire les FBX ; MURAILLE_SANS_APERCU=1 : sans les rendus."""
import os, json
_m = open(os.environ["FROGGY_ROOT"] + "/Tools/blender/muraille_jardin.py").read()
exec(_m.split("# ---------- fabrication ----------")[0])     # briques, tons et dimensions de la muraille d'origine

POSE_A = POSE.replace(".json", "_artisan.json")
Z = Vector((0, 0, 1))
TUILE_NEUVE = ((132, 146, 176), (88, 98, 126), (52, 56, 78))   # une tuile remplacée : plus sombre, plus bleue
TERRE = [(196, 156, 110), (170, 132, 96), (148, 112, 84)]        # strates de terre damée, sous l'enduit tombé
FISSURE = (96, 72, 60)
FER_P = ((70, 70, 78), (46, 46, 52), (28, 28, 32))


# ---------- briques des tuiles ----------
def boite_axes(centre, axes):
    """Une boîte dont les trois axes sont donnés : [(vecteur unitaire, longueur), ...]."""
    return cube_vers(lambda c: centre + axes[0][0] * c.x * axes[0][1] + axes[1][0] * c.y * axes[1][1] + axes[2][0] * c.z * axes[2][1])


def ton_tuile(rnd):
    """Le ton d'une tuile : chacune sort du four un peu différente ; une sur douze a été remplacée."""
    if rnd.random() < 1 / 12: return TUILE_NEUVE
    k = rnd.uniform(-.07, .07)
    return tuple(tuple(min(255, v * (1 + k)) for v in c) for c in TUILE)


def plate(bas, haut, nrm, a, w, ep, leve, rnd, tons):
    """Tuile plate (hiragawara) de `bas` à `haut` le long de la pente, large de w selon a, son bord bas relevé de `leve`."""
    jx = rnd.uniform(-.006, .006); jr = rnd.uniform(-.012, .012)
    return peint(cube_vers(lambda c: bas.lerp(haut, c.y + .5) + a * (c.x * w + jx + jr * (c.y + .5))
                           + nrm * ((c.z + .5) * ep + leve * (.5 - c.y))), tons, rnd, .02)


def ronde(bas, haut, nrm, a, r_bas, r_haut, leve, rnd, tons, n=6):
    """Tuile ronde (marugawara) : demi-cylindre couché sur la pente, plus large en bas, son bout bas relevé."""
    bm = bmesh.new(); jx = a * rnd.uniform(-.006, .006)
    anneaux = []
    for p, r, l in ((bas, r_bas, leve), (haut, r_haut, 0.0)):
        anneaux.append([bm.verts.new(p + jx + nrm * l + a * math.cos(math.pi * i / n) * r + nrm * math.sin(math.pi * i / n) * r) for i in range(n + 1)])
    for i in range(n): bm.faces.new((anneaux[0][i], anneaux[0][i + 1], anneaux[1][i + 1], anneaux[1][i]))
    bm.faces.new(anneaux[0]); bm.faces.new(anneaux[1][::-1])
    bmesh.ops.recalc_face_normals(bm, faces=bm.faces[:])
    return peint(bm, tons, rnd, .02)


def disque(centre, axe, r, ep, tons, rnd, n=10):
    d = bmesh.new(); bmesh.ops.create_cone(d, cap_ends=True, segments=n, radius1=r, radius2=r, depth=ep)
    bmesh.ops.rotate(d, verts=d.verts[:], cent=Vector(), matrix=Z.rotation_difference(axe).to_matrix())
    bmesh.ops.translate(d, verts=d.verts[:], vec=centre); return peint(d, tons, rnd, .02)


def rang_de_tuiles(bas, haut, nrm, a, rnd, fins, ronde_ici, w=PAS - .02, cours=.22, base=.07):
    """Une file de tuiles de l'égout (bas) au faîte (haut) : rondes ou plates, par rangs qui se recouvrent ; au bout bas,
    le disque d'une ronde ou la lèvre d'une plate. Les tuiles vont dans fins (sans contour)."""
    d = (haut - bas); L = d.length; d.normalize()
    n = max(1, round(L / cours)); pas_ = L / n
    tons = ton_tuile(rnd)
    for j in range(n):
        p0, p1 = bas + d * (j * pas_), bas + d * min(L, (j + 1) * pas_ + .05)
        if rnd.random() < .25: tons = ton_tuile(rnd)
        if ronde_ici:
            fins.append(ronde(p0 + nrm * (base + .012), p1 + nrm * (base + .012), nrm, a, .066, .058, .028, rnd, tons))
        else:
            fins.append(plate(p0 + nrm * base, p1 + nrm * base, nrm, a, w, .016, .03, rnd, tons))
    if ronde_ici:                        # le disque du bout (gatō) et son bossage
        c = bas + nrm * (base + .012 + .028) - d * .012
        fins.append(disque(c, -d, .074, .026, ton_tuile(rnd), rnd))
        fins.append(disque(c - d * .02, -d, .03, .016, ton_tuile(rnd), rnd, 8))
    else:                                # la lèvre pendante d'une plate d'égout
        c = bas + nrm * (base + .03) - d * .005 - Z * .035
        fins.append(peint(boite_axes(c, [(a, w), (d, .022), (Z, .075)]), ton_tuile(rnd), rnd, .02))


def pile_de_faitage(debut, fin, rnd, fins, couches=4, larg=.36):
    """Faîtage : des tuiles épaisses empilées (noshigawara), chaque couche un peu plus étroite, coiffées d'une ronde
    (ganburigawara). De debut à fin (le bas de la pile)."""
    v = fin - debut; L = v.length; v.normalize()
    cote = v.cross(Z).normalized() if abs(v.z) < .99 else Vector((1, 0, 0)); haut = cote.cross(v).normalized()
    if haut.z < 0: haut = -haut
    n = max(1, round(L / .33))
    for k in range(couches):
        w = larg - .035 * k
        for i in range(n):
            c = debut + v * (L * (i + .5) / n) + haut * (.024 + k * .048 + rnd.uniform(-.003, .003)) + cote * rnd.uniform(-.006, .006)
            fins.append(peint(boite_axes(c, [(v, L / n - .012), (cote, w), (haut, .044)]), ton_tuile(rnd), rnd, .02))
    for i in range(n):
        p0 = debut + v * (L * i / n) + haut * (couches * .048); p1 = debut + v * min(L, L * (i + 1) / n + .03) + haut * (couches * .048)
        fins.append(ronde(p0, p1, haut, cote, .085, .08, 0, rnd, ton_tuile(rnd), 8))
    return debut + haut * (couches * .048 + .085), fin + haut * (couches * .048 + .085)


def oni(centre, face, rnd, fins, h=.36, w=.32):
    """Tuile d'ogre (onigawara), dessin de mémoire : un écu épais, deux cornes, un disque en relief au centre."""
    cote = face.cross(Z).normalized()
    fins.append(peint(boite_axes(centre + Z * h / 2, [(cote, w), (face, .08), (Z, h)]), ton_tuile(rnd), rnd, .02))
    for s in (-1, 1):
        corne = boite_axes(Vector(), [(cote, .07), (face, .07), (Z, .14)])
        bmesh.ops.rotate(corne, verts=corne.verts[:], cent=Vector(), matrix=Matrix.Rotation(-s * .45, 3, face))
        bmesh.ops.translate(corne, verts=corne.verts[:], vec=centre + cote * s * (w / 2 - .02) + Z * (h + .05))
        fins.append(peint(corne, ton_tuile(rnd), rnd, .02))
    fins.append(disque(centre + Z * h * .55 + face * .05, face, .08, .04, ton_tuile(rnd), rnd))
    fins.append(disque(centre + Z * h * .55 + face * .075, face, .035, .03, ton_tuile(rnd), rnd, 8))


# ---------- toits ----------
def toit_a(rnd, longueur, nx, fins, dz=0.0, debord=DEBORD, x0=0.0, oni_aux_bouts=False):
    """Toit à deux pentes en hongawarabuki : un voligeage sombre (avec contour, c'est la silhouette), les tuiles par
    rangs, les disques et les lèvres d'égout, la pile du faîtage. Rend les grosses pièces ; les tuiles vont dans fins."""
    gros = []; haut = FAITE - .1 + dz; o = Vector((x0, 0, 0))
    for s in (-1, 1):
        crete, egout = Vector((0, 0, haut)), Vector((0, s * debord, EGOUT + dz))
        d = (egout - crete).normalized(); nrm = Vector((0, -d.z, d.y))
        if nrm.z < 0: nrm = -nrm
        a = Vector((1, 0, 0))

        def pente(c, crete=crete, egout=egout, nrm=nrm):
            p = crete + (egout - crete) * (c.y * s + .5)
            return Vector((c.x * longueur, p.y, p.z)) + nrm * (c.z + .5) * .07 + o
        gros.append(peint(cube_vers(pente), tuple(melange(c, (40, 40, 52), .35) for c in TUILE), rnd, .02))
        bas_y, haut_y = egout + d * .03, crete - d * .14
        # plates d'abord, entre les rondes (des demi-tuiles aux bouts du module : le voisin pose l'autre moitié)
        for k in range(nx + 1):
            x = -longueur / 2 + k * PAS; w = PAS - .02 if 0 < k < nx else PAS / 2 - .01
            if k == 0: x += PAS / 4
            if k == nx: x -= PAS / 4
            rang_de_tuiles(bas_y + Vector((x, 0, 0)) + o, haut_y + Vector((x, 0, 0)) + o, nrm, a, rnd, fins, False, w)
        for k in range(nx):
            x = -longueur / 2 + PAS / 2 + k * PAS
            rang_de_tuiles(bas_y + Vector((x, 0, 0)) + o, haut_y + Vector((x, 0, 0)) + o, nrm, a, rnd, fins, True)
    gros.append(peint(boite((longueur, .3, .16), Vector((0, 0, haut + .02)) + o), tuple(melange(c, (40, 40, 52), .35) for c in TUILE), rnd, .02))
    debut, fin = Vector((-longueur / 2, 0, haut + .06)) + o, Vector((longueur / 2, 0, haut + .06)) + o
    pile_de_faitage(debut, fin, rnd, fins)
    if oni_aux_bouts:
        for p, f in ((debut, Vector((-1, 0, 0))), (fin, Vector((1, 0, 0)))): oni(p + f * .02 - Z * .02, f, rnd, fins)
    return gros


# ---------- le mur ----------
def tranches_a(bas, haut):
    """Hauteurs de découpe : les lignes blanches, les joints de planches (tous les 0,3 m, une bande de 2 cm) et des
    tranches de 0,1 m ailleurs, pour peindre les strates."""
    zs = {bas, haut}
    for z in LIGNES: zs |= {z, z + .05}
    z = bas + .3
    while z < LIGNES[0] - .1: zs |= {round(z, 3), round(z + .02, 3)}; z += .3
    z = bas
    while z < LIGNES[0] - .05: zs.add(round(z, 3)); z += .1
    return sorted(zs)


def dans_trou(p, cote, trous, graine):
    """0 au bord d'un trou d'enduit, 1 au fond ; None hors des trous (même test pour creuser et pour peindre)."""
    for c, x, z, rx, rz in trous:
        if c != cote: continue
        e = ((p.x - x) / rx) ** 2 + ((p.z - z) / rz) ** 2 + noise.noise(Vector((p.x * 3 + graine, p.z * 3, c))) * .5
        if e < 1: return 1 - e
    return None


def creuser_trous(corps, trous, graine, prof=.035):
    """Enfonce l'enduit là où il est tombé : la terre damée est en retrait de quelques centimètres."""
    for v in corps.verts:
        if abs(v.co.y) < .05: continue
        t = dans_trou(v.co, 1 if v.co.y > 0 else -1, trous, graine)
        if t is not None: v.co.y -= (1 if v.co.y > 0 else -1) * prof * min(1, t * 3)


def enduit_artisan(rnd, graine, coulures, trous):
    """L'enduit d'ocre avec ses lignes blanches, les joints de planches à peine marqués, et des trous où il est tombé :
    la terre damée y paraît en strates (trous : (côté, x, z, rx, rz))."""
    base = couleur_enduit(rnd, graine, coulures)
    joints = [round(SOCLE + .3 * k, 3) for k in range(1, 6)]

    def couleur(f):
        c, n = f.calc_center_median(), f.normal
        if abs(n.z) < .9:
            cote = 1 if c.y > 0 else -1
            if dans_trou(c, cote, trous, graine) is not None:
                return ombre(n, *[TERRE[int((c.z - SOCLE) / .1) % 3]] * 3, rnd, .04)
            if any(j < c.z < j + .02 for j in joints): return melange(base(f), SALI, .22)
            if SOCLE < c.z < LIGNES[0]:                  # sous l'enduit, les couches damées se devinent à peine
                return melange(base(f), ENDUIT[2], .06 * (int((c.z - SOCLE) / .1) % 2))
        return base(f)
    return couleur


def fissure(rnd, x, cote, z_haut, fins, long_=.7):
    """Une fissure fine qui descend en zigzag sur la face du mur (côté `cote`)."""
    z = z_haut; pts = []
    while z > z_haut - long_:
        pts.append(Vector((x, cote * (demi(z, W_PIED, W_TETE) + .004), z))); z -= rnd.uniform(.035, .07); x += rnd.uniform(-.035, .035)
    bm = bmesh.new()
    for p, q in zip(pts, pts[1:]):
        e = .008
        vs = [bm.verts.new(v) for v in (p + Vector((-e, 0, 0)), p + Vector((e, 0, 0)), q + Vector((e, 0, 0)), q + Vector((-e, 0, 0)))]
        bm.faces.new(vs if cote < 0 else vs[::-1])
    bm.normal_update(); peindre(bm, lambda f: FISSURE); fins.append(bm)


def lignes_blanches(rnd, fins, x0, x1, ny_cotes=(-1, 1)):
    """Les cinq lignes d'enduit blanc, en léger relief, tirées à la main (elles ondulent d'un ou deux millimètres)."""
    for cote in ny_cotes:
        for z in LIGNES:
            n = 4; xs = [x0 + (x1 - x0) * i / n for i in range(n + 1)]
            for xa, xb in zip(xs, xs[1:]):
                dz = rnd.uniform(-.003, .003); y = cote * (demi(z + .025, W_PIED, W_TETE) + .006)
                fins.append(peint(boite(((xb - xa) + .004, .012, .05 + rnd.uniform(-.004, .004)), Vector(((xa + xb) / 2, y, z + .025 + dz))), BLANC, rnd, .01))


def soubassement(rnd, x0, x1, gros, fins):
    """Uchikomi-hagi : deux assises de pierres dégrossies, joints décalés, chacune un peu de biais ; des cales dans
    quelques joints, de la mousse au pied."""
    for zb, zh, lmin, lmax in ((-.3, .18, .6, 1.0), (.18, SOCLE + .02, .4, .8)):
        x = x0
        while x < x1 - .01:
            lg = min(rnd.uniform(lmin, lmax), x1 - x)
            if x1 - x - lg < .3: lg = x1 - x
            h = zh - zb + rnd.uniform(-.015, .01)
            b = bloc(rnd, (lg - .025, W_SOCLE * 2 - rnd.uniform(0, .05), h), Vector((x + lg / 2, rnd.uniform(-.012, .012), zb + h / 2)), biseau=.045)
            bmesh.ops.rotate(b, verts=b.verts[:], cent=Vector((x + lg / 2, 0, zb)), matrix=Matrix.Rotation(rnd.uniform(-.012, .012), 3, "Z"))
            b.normal_update()
            lay = calque(b)
            for f in b.faces:                       # mousse au pied, sur les faces qui touchent le sol
                if f.calc_center_median().z < .08 and abs(f.normal.z) < .5 and rnd.random() < .7:
                    col = srgb(ombre(f.normal, *MOUSSE_MUR, rnd, .06))
                    for lo in f.loops: lo[lay] = col
            gros.append(b)
            if x > x0 and rnd.random() < .45:     # une cale dans le joint
                for cote in (-1, 1):
                    cale = pierre(rnd, 8, Vector((.05, .03, .04)), sub=1); arete = cale.faces.layers.int.get("arete")
                    cale.normal_update(); peindre(cale, lambda f, a_=arete: couleur_roche(f, rnd, GRANIT, a_))
                    bmesh.ops.translate(cale, verts=cale.verts[:], vec=Vector((x, cote * (W_SOCLE + .005), zh - .06)))
                    fins.append(cale)
            x += lg


MOUSSE_MUR = ((150, 186, 104), (104, 146, 80), (62, 98, 62))


def mur_a(rnd, graine):
    """Un module de 4 m, artisanal : soubassement en uchikomi-hagi, corps en talus avec strates, joints de planches,
    trous d'enduit et fissures, lignes blanches en relief, poteaux inclinés, poutre, toit en hongawarabuki."""
    gros, fins = [], []
    soubassement(rnd, -L / 2, L / 2, gros, fins)
    nx = int(L / PAS)
    # le corps est découpé deux fois plus fin que les tuiles : sinon les trous d'enduit sortent en rectangles
    corps = prisme(tranches_a(SOCLE, CORPS), lambda z: L / 2, lambda z: demi(z, W_PIED, W_TETE), nx * 2, 1)
    coulures = [(-L / 2 + PAS * (k + .5), rnd.uniform(.12, .26)) for k in rnd.sample(range(nx), 3)]
    trous = [(c, rnd.uniform(-1.4, 1.4), rnd.uniform(.75, 1.5), rnd.uniform(.3, .55), rnd.uniform(.18, .32)) for c in (-1, 1) if rnd.random() < .8]
    creuser_trous(corps, trous, graine)
    corps.normal_update(); peindre(corps, enduit_artisan(rnd, graine, coulures, trous)); gros.append(corps)
    for c in (-1, 1):
        if rnd.random() < .7: fissure(rnd, rnd.uniform(-1.6, 1.6), c, CORPS - .05, fins, rnd.uniform(.25, .5))
    lignes_blanches(rnd, fins, -L / 2, L / 2)
    for s in (-1, 1):
        for xp in POTEAUX:
            ton = tuple(melange(c, BOIS_SOMBRE[0], rnd.uniform(0, .2)) for c in BOIS_SOMBRE)
            gros.append(peint(cube_vers(lambda c, s=s, xp=xp: Vector((
                xp + c.x * .16, s * (demi(SOCLE if c.z < 0 else CORPS, W_PIED, W_TETE) + (.05 if c.y * s > 0 else -.03)),
                SOCLE if c.z < 0 else CORPS))), ton, rnd))
    gros.append(peint(boite((L, (W_TETE + .07) * 2, EGOUT - CORPS + .02), Vector((0, 0, (CORPS + EGOUT) / 2))), BOIS_SOMBRE, rnd))
    gros += toit_a(rnd, L, nx, fins)
    return gros, fins


def angle_a(rnd):
    """Le pilier d'angle : même corps que l'original, toit en pyramide couvert de tuiles en files qui raccourcissent
    vers les arêtiers, arêtiers en piles de faîtage, une tuile d'ogre au bas de chacun."""
    gros, fins = [], []
    c = W_SOCLE + .1; soubassement(rnd, -c, c, gros, fins)
    h = lambda z: demi(z, W_PIED + .1, W_TETE + .1)
    corps = prisme(tranches_a(SOCLE, CORPS), h, h, 3, 3)
    corps.normal_update(); peindre(corps, enduit_artisan(rnd, 7.0, [], [])); gros.append(corps)
    for (sx, sy) in ((1, 0), (-1, 0), (0, 1), (0, -1)):        # lignes blanches sur les quatre faces
        for z in LIGNES:
            y = demi(z + .025, W_PIED + .1, W_TETE + .1) + .006; w = 2 * (y - .006)
            ax, ay = (Vector((0, 1, 0)), Vector((1, 0, 0))) if sx else (Vector((1, 0, 0)), Vector((0, 1, 0)))
            fins.append(peint(boite_axes(Vector((sx * y, sy * y, z + .025)), [(ax, w), (ay, .012), (Z, .05)]), BLANC, rnd, .01))
    gros.append(peint(boite(((W_TETE + .17) * 2,) * 2 + (EGOUT - CORPS + .02,), Vector((0, 0, (CORPS + EGOUT) / 2))), BOIS_SOMBRE, rnd))
    R, sommet = DEBORD + .08, FAITE + .2
    p = bmesh.new(); bmesh.ops.create_cone(p, cap_ends=True, segments=4, radius1=R * math.sqrt(2), radius2=.06, depth=sommet - EGOUT)
    bmesh.ops.rotate(p, verts=p.verts[:], cent=Vector(), matrix=Matrix.Rotation(math.pi / 4, 3, "Z"))
    bmesh.ops.translate(p, verts=p.verts[:], vec=Vector((0, 0, (sommet + EGOUT) / 2)))
    gros.append(peint(p, tuple(melange(c, (40, 40, 52), .35) for c in TUILE), rnd, .02))
    H = sommet - EGOUT
    for k in range(4):                                  # chaque pan : des files de tuiles, de l'égout à l'arêtier
        rot = Matrix.Rotation(k * math.pi / 2, 3, "Z")
        out = rot @ Vector((0, -1, 0)); a = rot @ Vector((1, 0, 0))
        d = (Vector((0, 0, sommet)) - (out * R + Z * EGOUT)).normalized()   # vers le haut de la pente
        nrm = a.cross(d).normalized()
        if nrm.z < 0: nrm = -nrm
        n = int(R / (PAS / 2))
        for i in range(-n + 1, n):
            u = i * PAS / 2
            bas = out * R + a * u + Z * EGOUT
            fin_y = abs(u) + .1                         # la file s'arrête sous l'arêtier
            if fin_y > R - .15: continue
            haut = out * fin_y + a * u + Z * (EGOUT + (R - fin_y) / R * H)
            rang_de_tuiles(bas + d * -.03, haut, nrm, a, rnd, fins, i % 2 == 1, PAS - .02, base=.04)
    for k in range(4):                                  # arêtiers : piles de faîtage, une tuile d'ogre en bas
        ang = math.pi / 4 + k * math.pi / 2
        coin = Vector((math.cos(ang) * R * math.sqrt(2), math.sin(ang) * R * math.sqrt(2), EGOUT))
        bas_ar = Vector((coin.x * .92, coin.y * .92, EGOUT + .08 * H + .02)); sommet_v = Vector((0, 0, sommet - .05))
        pile_de_faitage(bas_ar, sommet_v, rnd, fins, couches=3, larg=.26)
        oni(bas_ar - Z * .02, Vector((coin.x, coin.y, 0)).normalized(), rnd, fins, h=.3, w=.26)
    gros.append(peint(cylindre_debout(.12, .06, .26, sommet - .02), TUILE, rnd, .02))
    return gros, fins


def porte_a(rnd):
    """La porte munamon, artisanale : bouts de mur et toit comme les modules, tuiles d'ogre au bout du faîtage, vantaux
    à pentures de fer (les ferrures décoratives du kabukimon) et rangs de clous."""
    gros, fins = [], []
    for s in (-1, 1):
        xc = s * (L / 2 - .25)
        soubassement(rnd, xc - .25, xc + .25, gros, fins)
        corps = prisme(tranches_a(SOCLE, CORPS), lambda z: .25, lambda z: demi(z, W_PIED, W_TETE), 2, 1)
        bmesh.ops.translate(corps, verts=corps.verts[:], vec=Vector((xc, 0, 0)))
        corps.normal_update(); peindre(corps, enduit_artisan(rnd, 11.0 + s, [], [])); gros.append(corps)
        lignes_blanches(rnd, fins, xc - .25, xc + .25)
        gros.append(peint(boite((.5, (W_TETE + .07) * 2, EGOUT - CORPS + .02), Vector((xc, 0, (CORPS + EGOUT) / 2))), BOIS_SOMBRE, rnd))
        gros += toit_a(rnd, .5, 2, fins, x0=xc)
    for s in (-1, 1):
        gros.append(bloc(rnd, (.46, .46, .42), Vector((s * 1.33, 0, -.09)), biseau=.045))
        gros.append(peint(boite((.3, .3, 3.15), Vector((s * 1.33, 0, .12 + 3.15 / 2))), BOIS_PORTE, rnd))
        for z in (.4, 2.9):                               # bagues de fer au pied et en tête des poteaux
            fins.append(peint(boite((.32, .32, .05), Vector((s * 1.33, 0, z))), FER_P, rnd))
    gros.append(peint(boite((3.3, .24, .28), Vector((0, 0, 3.0))), BOIS_PORTE, rnd))
    gros.append(peint(boite((2.4, .14, .16), Vector((0, 0, 2.62))), BOIS_PORTE, rnd))
    gros.append(peint(boite((3.0, .5, .3), Vector((0, 0, 3.29))), BOIS_SOMBRE, rnd))
    gros.append(peint(cube_vers(lambda c: Vector((c.x * 3.0, c.y * (1.4 if c.z < 0 else .02), 3.44 if c.z < 0 else 3.72))), BOIS_SOMBRE, rnd))
    gros += toit_a(rnd, 3.5, 14, fins, dz=.8, debord=1.05, oni_aux_bouts=True)
    gros.append(peint(boite((2.5, .3, .12), Vector((0, 0, .06))), BOIS_PORTE, rnd))
    for s in (-1, 1):
        for k in range(5):
            x = s * (.12 + .23 * k + .005)
            ton = tuple(melange(c, BOIS_PORTE[2], rnd.uniform(0, .18)) for c in BOIS_PORTE)
            gros.append(peint(boite((.225, .08, 2.36), Vector((x, 0, .12 + 1.18))), ton, rnd, .03))
        for z in (.55, 2.0):
            for sy in (-1, 1):
                gros.append(peint(boite((1.17, .03, .14), Vector((s * .595, sy * .055, z))), BOIS_SOMBRE, rnd))
                # penture : une bande de fer depuis le poteau, finie par un disque ; clous en rang sur la barre
                fins.append(peint(boite((.55, .012, .09), Vector((s * (1.17 - .275), sy * .077, z))), FER_P, rnd))
                fins.append(disque(Vector((s * (1.17 - .56), sy * .08, z)), Vector((0, sy, 0)), .06, .012, FER_P, rnd, 8))
                for xk in range(6):
                    fins.append(peint(boite((.03, .014, .03), Vector((s * (.08 + .17 * xk), sy * .077, z + .035))), FER_P, rnd))
    return gros, fins


# ---------- fabrication ----------
EXPORTER = not os.environ.get("JARDIN_SANS_EXPORT")
modeles = {}
for i, (nom, fab) in enumerate((("Muraille_Artisan_Mur_1", lambda r: mur_a(r, 0.0)), ("Muraille_Artisan_Mur_2", lambda r: mur_a(r, 23.0)),
                                ("Muraille_Artisan_Angle", angle_a), ("Muraille_Artisan_Porte", porte_a))):
    rnd = random.Random(6000 + i); gros, fins = fab(rnd)
    bm = assembler(*gros)
    if "Angle" not in nom:   # les bouts du module, cachés par le voisin : sinon leur contour trace un joint tous les 4 m
        bm.normal_update()
        bmesh.ops.delete(bm, geom=[f for f in bm.faces if abs(f.normal.x) > .95 and abs(abs(f.calc_center_median().x) - L / 2) < .004], context="FACES")
    ajouter_contour(bm, .02, TRAIT)
    for f in fins: fusionner(bm, f)
    col = collection(nom); bpy.context.view_layer.layer_collection.children[nom].exclude = False
    modeles[nom] = objet(nom, bm, col)
    if EXPORTER: exporter(col, os.path.join(DOSSIER, nom + ".fbx"))
    print(f"{nom:24s} {poids(col):6d} faces")

# la pose : celle de la muraille d'origine, pièce pour pièce
ancienne = json.load(open(POSE))["pieces"] if os.path.exists(POSE) else []
pieces = [dict(p, nom=p["nom"].replace("Muraille_", "Muraille_Artisan_")) for p in ancienne]
if EXPORTER and pieces:
    with open(POSE_A, "w") as fh: json.dump(dict(pieces=pieces), fh, indent=1, ensure_ascii=False)
muraille = collection("Muraille_Artisanale")
for p in pieces:
    ob = bpy.data.objects.new(p["nom"] + "_pose", modeles[p["nom"]].data)
    ob.matrix_world = Matrix.Translation((p["pos"][0], p["pos"][2], 0)) @ Matrix.Rotation(math.radians(-p["lacet"]), 4, "Z"); muraille.objects.link(ob)
for nom in modeles: bpy.context.view_layer.layer_collection.children[nom].exclude = True
print(f"muraille artisanale : {len(pieces)} pièces")


def apercu_a(nom, oeil, cible, quelle, lens=24):
    sc = bpy.context.scene; sc.render.engine = "BLENDER_WORKBENCH"
    sh = sc.display.shading; sh.light = "FLAT"; sh.color_type = "VERTEX"; sh.show_backface_culling = True
    sc.render.resolution_x, sc.render.resolution_y = 1600, 900
    cam = bpy.data.objects.get("Camera") or bpy.data.objects.new("Camera", bpy.data.cameras.new("Camera"))
    if not cam.users_collection: sc.collection.objects.link(cam)
    cam.data.type = "PERSP"; cam.data.lens = lens; cam.data.clip_end = 300; sc.camera = cam
    cam.location = Vector(oeil); cam.rotation_euler = (Vector(cible) - cam.location).to_track_quat("-Z", "Y").to_euler()
    for c in bpy.data.collections:
        for o in c.objects: o.hide_render = c.name not in ("Apercu", quelle)
    os.makedirs(APERCUS, exist_ok=True)
    sc.render.filepath = os.path.join(APERCUS, nom + ".png"); bpy.ops.render.render(write_still=True)


if not os.environ.get("MURAILLE_SANS_APERCU") and pieces:
    X0, X1, Y0, Y1 = json.load(open(DISPO))["bornes"]
    for quelle, suffixe in (("Muraille", "ancienne"), ("Muraille_Artisanale", "artisanale")):
        apercu_a(f"muraille_de_pres_{suffixe}", (X1 - 4.5, Y0 + 12, 1.2), (X1 + .5, Y0 + 9, 2.0), quelle, 20)
        apercu_a(f"muraille_angle_{suffixe}", (X0 + 3, Y0 + 7.5, 2.2), (X0 - .5, Y0 - .5, 2.4), quelle)
        apercu_a(f"muraille_toit_{suffixe}", (X1 - 2.5, Y0 + 14, 4.2), (X1 + .6, Y0 + 11, 2.9), quelle, 28)
        if GRANDE: apercu_a(f"muraille_porte_{suffixe}", ((X0 + X1) / 2 + 3.5, Y0 + 7, 1.6), ((X0 + X1) / 2, Y0 - .5, 2.2), quelle)
    print("MURAILLE ARTISANALE FINIE", APERCUS)
