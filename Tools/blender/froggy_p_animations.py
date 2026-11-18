"""Animations de jeu de Froggy P (V1), faites à la main par script. Exécuté par froggy_p.py (exec), après ses outils.

Pourquoi à la main : les mouvements humains de Meshy (text_to_motion), rejoués sur Froggy P par le retargeting humanoïde
de Unity, ne se voyaient presque pas (18/11/2026) : ses bras font 23 cm sous une tête de 84 cm de large, et vus de dos
la tête les cache. Ici chaque geste passe d'abord par le corps et la tête (bascule, torsion, accroupi), les bras
ensuite ; poses tenues, et la tête suit le corps avec un temps de retard (ressort).

Faits appliqués (Docs/Recherches/froggy-animations-v1-2026-11-18.md) :
- croassement : le sac vocal gonfle en même temps que le chant, ~450 ms pour un chant de 400 ms, dégonflé ~50 ms après
  la fin du son (Taylor et al. 2011, JEB 214:815) ; ici de 0,30 à 0,78 s : le son du jeu part à 0,30 s du clip ;
- atterrissage : bras tendus vers l'avant avant le contact, puis accroupi profond (Cox & Gillis 2017) ;
- nage : les deux jambes poussent ensemble et vite, glisse jambes tendues, genoux qui remontent lentement
  (Richards 2010 ; Stamhuis & Nauwelaerts 2005) ;
- plongeon : bras lancés devant (pose « Superman »), jambes tendues derrière (Virginia Tech, 2024).

Canaux (degrés, mètres ; Froggy regarde vers −Y, sa gauche est en +X) :
  bx by bz : bassin (by < 0 : en avant) · bp : bascule du bassin (+ en avant) · br : roulis (+ vers sa gauche)
  bw : lacet (+ tourne vers sa gauche) · sx sy sz : dos (penché, côté, torsion) · hx hy hz : tête (+ hoche en avant)
  bras, préfixe g (gauche) ou d (droite), sans préfixe pour les deux : c (épaule haussée), ay (bras levé de côté),
  ax (bras balancé, − en avant), az (bras levé ramené, − vers l'avant), fx (coude, − plié vers l'avant), mx (main)
  pieds (cibles IK), même préfixe : px (vers l'extérieur), py (− en avant), pz (haut), pp (+ pointe en bas), pw (pointe
  vers l'extérieur) · lie : 0 pieds tenus au sol, 1 pieds portés par le bassin (nage, vol)
  formes : gorge (sac vocal), yf (yeux fermés), yh (yeux heureux), yp (yeux plissés), de 0 à 1.
Chaque canal a ses propres clés : une clé ne touche que les canaux qu'elle nomme (« tenir » tient tous les autres,
« _repos » remet tout à zéro). Entre deux clés : cubique monotone (pas de dépassement), ou l'accélération indiquée.
"""

COTES = (("Left", 1, "g"), ("Right", -1, "d"))
BRAS = ("c", "ay", "ax", "az", "fx", "mx")
PIED = ("px", "py", "pz", "pp", "pw")
CANAUX = (["bx", "by", "bz", "bp", "br", "bw", "sx", "sy", "sz", "hx", "hy", "hz", "lie",
           "gorge", "yf", "yh", "yp"] + [k + c for k in "gd" for c in BRAS + PIED])
FORMES = {"gorge": "Gorge", "yf": "Yeux_Fermes", "yh": "Yeux_Heureux", "yp": "Yeux_Plisses"}
NEUTRE = {"_repos": True, "bz": -.01}
ACCEL = {"lin": lambda u: u, "in": lambda u: u * u, "in3": lambda u: u ** 3, "out": lambda u: 1 - (1 - u) ** 2,
         "out3": lambda u: 1 - (1 - u) ** 3, "inout": lambda u: u * u * (3 - 2 * u)}


# ---------------------------------------------------------------- sac vocal et formes des yeux
SAC_GONFLE = ((0, -.275, .49), (.175, .170, .150))       # centre, demi-axes (x, y, z) : une bulle sous le « w »
SAC_REPLIE = ((0, -.10, .50), (.026, .026, .026))         # caché dans le corps


def sac_vocal(mats_noms):
    """Le sac vocal : une bulle sous le menton, couleur du ventre, cernée du contour, pesée sur la tête. Repliée dans le
    corps au repos, la forme « Gorge » la gonfle (une sphère interpolée entre deux sphères reste une sphère)."""
    bm = bmesh.new()
    bmesh.ops.create_uvsphere(bm, u_segments=28, v_segments=18, radius=1.0)
    n = len(bm.verts)
    bmesh.ops.create_uvsphere(bm, u_segments=28, v_segments=18, radius=1.0)
    bm.verts.ensure_lookup_table(); bm.faces.ensure_lookup_table()
    contour = [f for f in bm.faces if f.verts[0].index >= n]
    bmesh.ops.reverse_faces(bm, faces=contour)
    for f in bm.faces:
        f.material_index = mats_noms.index("Contour") if f in contour else mats_noms.index("Ventre"); f.smooth = True
    unites = [v.co.copy() for v in bm.verts]
    me = bpy.data.meshes.new("Sac"); bm.to_mesh(me); bm.free()
    ob = lier(bpy.data.objects.new("Sac", me))

    def place(centre, demi, i):
        e = CONTOUR_EP if i >= n else 0
        u = unites[i]
        return Vector(centre) + Vector((u.x * (demi[0] + e), u.y * (demi[1] + e), u.z * (demi[2] + e)))

    for i, v in enumerate(me.vertices): v.co = place(*SAC_REPLIE, i)
    ob.shape_key_add(name="Basis", from_mix=False)
    gorge = ob.shape_key_add(name="Gorge", from_mix=False)
    for i in range(len(me.vertices)): gorge.data[i].co = place(*SAC_GONFLE, i)
    g = ob.vertex_groups.new(name="Head"); g.add(list(range(len(me.vertices))), 1.0, "REPLACE")
    return ob


def cles_yeux(deco, bvh, yeux):
    """Trois formes des yeux, dessinées de face puis projetées sur la tête comme les yeux eux-mêmes :
    Yeux_Fermes « ︶ » (clignement, tristesse), Yeux_Heureux « ^ ^ », Yeux_Plisses « > < » (effort, coup reçu).
    Le disque de l'œil devient un trait effilé aux deux bouts : aucun sommet ajouté, l'œil ouvert reste la forme de base."""
    deco.shape_key_add(name="Basis", from_mix=False)
    formes = {n: deco.shape_key_add(name=n, from_mix=False) for n in ("Yeux_Fermes", "Yeux_Heureux", "Yeux_Plisses")}
    for debut, fin, cx, cz, ax, az, anneaux, secteurs, decal in yeux:
        s = 1 if cx > 0 else -1
        for k in range(fin - debut):
            if k == 0: u = w = 0.0
            else:
                i, j = divmod(k - 1, secteurs); r = (i + 1) / anneaux; th = 2 * pi * j / secteurs
                u, w = r * math.cos(th), r * math.sin(th)
            dessins = {
                "Yeux_Fermes": (cx + ax * 1.05 * u, cz - .30 * az - .30 * az * (1 - u * u) + .14 * az * w),
                "Yeux_Heureux": (cx + ax * 1.05 * u, cz - .25 * az + .55 * az * (1 - u * u) + .14 * az * w),
                # le trait part de la pointe (côté nez) et s'ouvre vers l'extérieur, en haut et en bas
                "Yeux_Plisses": (cx - s * ax * .55 + s * ax * 1.1 * abs(w) + .17 * ax * u, cz + .62 * az * w),
            }
            for nom, (x, z) in dessins.items():
                p, _ = projeter(bvh, x, z, decal)
                if p is not None: formes[nom].data[debut + k].co = p


# ---------------------------------------------------------------- courbes par canal
def deplier(ch):
    out = {}
    for k, val in ch.items():                       # d'abord les raccourcis (les deux côtés), puis le précis
        if k in BRAS or k in PIED: out["g" + k] = out["d" + k] = val
    for k, val in ch.items():
        if k in BRAS or k in PIED or k in ("_repos", "tenir"): continue
        if k not in CANAUX: raise KeyError(f"canal inconnu : {k}")
        out[k] = val
    return out


def pistes(cles, duree, boucle):
    """cles : [(t, {canal: valeur}, accélération facultative)] → {canal: [(t, valeur, accel)]}."""
    p = {}
    for cle in sorted(cles, key=lambda c: c[0]):
        t, ch = cle[0], cle[1]
        acc = cle[2] if len(cle) > 2 else None
        precis = deplier(ch)
        if ch.get("_repos"):
            for k in CANAUX: precis.setdefault(k, 0.0)
        if ch.get("tenir"):
            for k, cl in p.items():
                if k not in precis and cl: precis[k] = cl[-1][1]
        for k, val in precis.items(): p.setdefault(k, []).append((t, float(val), acc))
    for k, cl in p.items():
        if cl[0][0] > 0: cl.insert(0, (0.0, 0.0, None))
        if boucle and cl[-1][0] < duree: cl.append((duree, cl[0][1], None))
    return p


def tangentes(cl, boucle):
    T = [c[0] for c in cl]; Y = [c[1] for c in cl]; n = len(cl)
    if n < 2: return [0.0] * n
    d = [(Y[i + 1] - Y[i]) / max(T[i + 1] - T[i], 1e-6) for i in range(n - 1)]

    def m(d0, d1, h0, h1):
        if d0 * d1 <= 0: return 0.0
        w1, w2 = 2 * h1 + h0, h1 + 2 * h0
        return (w1 + w2) / (w1 / d0 + w2 / d1)
    out = [0.0] * n
    for i in range(1, n - 1): out[i] = m(d[i - 1], d[i], T[i] - T[i - 1], T[i + 1] - T[i])
    if boucle and n > 2:
        out[0] = out[-1] = m(d[-1], d[0], T[-1] - T[-2], T[1] - T[0])
    return out


def valeur(cl, tg, t):
    if t <= cl[0][0]: return cl[0][1]
    if t >= cl[-1][0]: return cl[-1][1]
    i = max(k for k in range(len(cl) - 1) if cl[k][0] <= t)
    (t0, y0, _), (t1, y1, acc) = cl[i], cl[i + 1]
    h = t1 - t0; u = (t - t0) / h
    if acc: return y0 + (y1 - y0) * ACCEL[acc](u)
    u2, u3 = u * u, u * u * u
    return (2 * u3 - 3 * u2 + 1) * y0 + (u3 - 2 * u2 + u) * h * tg[i] + (-2 * u3 + 3 * u2) * y1 + (u3 - u2) * h * tg[i + 1]


def ressort(xs, f, z, boucle):
    """Suivi amorti (fréquence f en Hz, amortissement z) : la masse suit son ancre avec retard et la dépasse un peu."""
    w = 2 * pi * f; sous = 8; h = 1 / FPS / sous

    def passe(y, v, seq):
        out = []
        for x in seq:
            for _ in range(sous):
                v += (w * w * (x - y) - 2 * z * w * v) * h; y += v * h
            out.append(y)
        return out, y, v
    if not boucle: return passe(xs[0], 0.0, xs)[0]
    y, v, out = xs[0], 0.0, xs
    for _ in range(4): out, y, v = passe(y, v, xs)
    return out


def echantillons(clip):
    n = round(clip["duree"] * FPS)
    p = pistes(clip["cles"], clip["duree"], clip["boucle"])
    tg = {k: tangentes(cl, clip["boucle"]) for k, cl in p.items()}
    seq = [{k: (valeur(p[k], tg[k], f / FPS) if k in p else 0.0) for k in CANAUX} for f in range(n + 1)]
    if clip.get("extra"):
        for f, v in enumerate(seq): clip["extra"](f / FPS, v)
    for k, (fr, am) in clip.get("ressorts", {}).items():
        for canal in ([k] if k in CANAUX else ["g" + k, "d" + k]):
            xs = [v[canal] for v in seq]
            ys = ressort(xs[:-1] if clip["boucle"] else xs, fr, am, clip["boucle"])
            if clip["boucle"]: ys = ys + [ys[0]]
            for v, y in zip(seq, ys): v[canal] = y
    return seq


# ---------------------------------------------------------------- pose d'une image
def cle_pose(arm, nom, axes, f):
    pb = arm.pose.bones[nom]; repos = arm.data.bones[nom].matrix_local.to_3x3()
    q = (repos.inverted() @ rot(axes) @ repos).to_quaternion()
    if pb.rotation_quaternion.dot(q) < 0: q.negate()
    pb.rotation_quaternion = q; pb.keyframe_insert("rotation_quaternion", frame=f)


def poser2(arm, cibles, cle, v, f):
    hanche = [("X", v["bp"]), ("Y", v["br"]), ("Z", v["bw"])]
    cle_pose(arm, "Hips", hanche, f)
    cle_pose(arm, "Spine", [("X", .55 * v["sx"]), ("Y", .55 * v["sy"]), ("Z", .5 * v["sz"])], f)
    cle_pose(arm, "Spine1", [("X", .45 * v["sx"]), ("Y", .45 * v["sy"]), ("Z", .5 * v["sz"])], f)
    cle_pose(arm, "Neck", [("X", .3 * v["hx"]), ("Y", .3 * v["hy"]), ("Z", .3 * v["hz"])], f)
    cle_pose(arm, "Head", [("X", .7 * v["hx"]), ("Y", .7 * v["hy"]), ("Z", .7 * v["hz"])], f)
    for c, s, k in COTES:
        cle_pose(arm, c + "Shoulder", [("Y", -s * v[k + "c"])], f)
        cle_pose(arm, c + "Arm", [("Y", -s * v[k + "ay"]), ("X", v[k + "ax"]), ("Z", s * v[k + "az"])], f)
        cle_pose(arm, c + "ForeArm", [("X", v[k + "fx"])], f)
        cle_pose(arm, c + "Hand", [("X", v[k + "mx"])], f)
        for os_ in ("UpLeg", "Leg", "Foot", "ToeBase"): cle_pose(arm, c + os_, [], f)
    hb = arm.pose.bones["Hips"]; repos_h = arm.data.bones["Hips"].matrix_local
    dep = Vector((v["bx"], v["by"], v["bz"]))
    hb.location = repos_h.to_3x3().inverted() @ dep; hb.keyframe_insert("location", frame=f)
    W = rot(hanche); pivot = repos_h.translation
    for c, s, k in COTES:
        repos = arm.data.bones[c + "Foot"].matrix_local
        d = Vector((s * v[k + "px"], v[k + "py"], v[k + "pz"]))
        Rp = rot([("X", v[k + "pp"]), ("Z", s * v[k + "pw"])]) @ repos.to_3x3()
        au_sol = repos.translation + d
        porte = pivot + dep + W @ (repos.translation + d - pivot)
        L = max(0.0, min(1.0, v["lie"]))
        cible = cibles[c]
        cible.location = au_sol.lerp(porte, L)
        q = Rp.to_quaternion().slerp((W @ Rp).to_quaternion(), L)
        if cible.rotation_quaternion.dot(q) < 0: q.negate()
        cible.rotation_quaternion = q
        cible.keyframe_insert("location", frame=f); cible.keyframe_insert("rotation_quaternion", frame=f)
    if cle is not None:
        for canal, nom in FORMES.items():
            kb = cle.key_blocks.get(nom)
            if kb is not None:
                kb.value = max(0.0, min(1.0, v[canal])); kb.keyframe_insert("value", frame=f)


def point_bas(corps):
    dg = bpy.context.evaluated_depsgraph_get()
    ev = corps.evaluated_get(dg); me = ev.to_mesh()
    co = np.empty(len(me.vertices) * 3, np.float32); me.vertices.foreach_get("co", co)
    ev.to_mesh_clear()
    return float(co[2::3].min())


def animer2(arm, cibles, corps, clip):
    nom = clip["nom"]
    for o in [arm, *cibles.values()]:
        o.animation_data_create(); o.animation_data.action = bpy.data.actions.new(nom if o is arm else f"{nom}_{o.name}")
    cle = corps.data.shape_keys
    if cle is not None:
        cle.animation_data_create(); cle.animation_data.action = bpy.data.actions.new(f"{nom}_Formes")
    seq = echantillons(clip)
    for f, v in enumerate(seq): poser2(arm, cibles, cle, v, f + 1)
    sc = bpy.context.scene; sc.frame_start, sc.frame_end = 1, len(seq)
    # Au sol : le point le plus bas du corps (contour compris) posé sur le sol, sans passer dessous.
    for t0, t1, mode in clip.get("sol", []):
        corr = {}
        for f, v in enumerate(seq):
            if t0 <= f / FPS <= t1:
                sc.frame_set(f + 1); bas = point_bas(corps)
                corr[f] = -bas if (mode == "contact" or bas < 0) else 0.0
        fs = sorted(corr)
        for i, f in enumerate(fs):                    # lissé sur trois images
            voisins = [corr[g] for g in fs[max(0, i - 1):i + 2]]
            seq[f]["bz"] += sum(voisins) / len(voisins)
            poser2(arm, cibles, cle, seq[f], f + 1)
    return len(seq)


# ---------------------------------------------------------------- les clips
def tremble(t, v, t0, t1, canaux, freq, amp):
    if t0 <= t <= t1:
        env = math.sin(pi * (t - t0) / (t1 - t0))
        for k, a in canaux.items(): v[k] += amp * a * env * math.sin(2 * pi * freq * (t - t0))


def extra_croassement(t, v):
    tremble(t, v, .38, .74, {"hx": 1, "sz": .6, "hy": .5}, 10, 1.5)    # le corps vibre avec le chant


def extra_sortie(t, v):
    if .80 <= t <= 1.14:                                                  # s'ébroue comme un chien mouillé
        u = (t - .80) / .34; env = math.sin(pi * u) * (1 - .35 * u)
        ph = 2 * pi * 5.5 * (t - .80)
        v["hz"] += 24 * env * math.sin(ph); v["sz"] -= 9 * env * math.sin(ph - .5)
        v["hy"] += 6 * env * math.sin(ph + 1.0); v["gay"] += 12 * env * math.sin(ph); v["day"] -= 12 * env * math.sin(ph)


def extra_porter(t, v):
    w = 2 * pi * t / 1.2                                                  # équilibre : les bras corrigent, la tête reste à plat
    v["gay"] += 6 * math.sin(w); v["day"] -= 6 * math.sin(w)
    v["sy"] -= 1.6 * math.sin(w + .4); v["hy"] += 2.2 * math.sin(w + 1.1)
    v["bz"] += .003 * math.sin(2 * w)


def extra_porter_bras(t, v):
    w = 2 * pi * t / 1.2
    v["bz"] += .004 * math.sin(2 * w); v["sx"] += 1.2 * math.sin(2 * w + .5); v["hx"] -= 1.0 * math.sin(2 * w + 1.2)


def extra_gele(t, v):
    tremble(t, v, .08, .48, {"sz": 1, "hz": .7, "gay": 1.4, "day": -1.4}, 14, 2.2)   # frisson, puis figé


def extra_decue(t, v):
    if t > 1.6: v["bz"] += .004 * math.sin(2 * pi * (t - 1.6) / 1.4); v["sx"] += 1.0 * math.sin(2 * pi * (t - 1.6) / 1.4)


def extra_comique(t, v):
    for t0 in (1.45, 1.95):                                               # la patte gauche tressaute deux fois
        if t0 <= t <= t0 + .22:
            e = math.sin(pi * (t - t0) / .22)
            v["gpz"] += .035 * e; v["gpp"] -= 35 * e


SAUT_FIN = {"bz": .045, "sx": 3.9, "hx": -5, "ax": -15, "ay": 66, "fx": -32, "pz": .095, "py": -.02, "pp": -10}
NAGE_BASE = {"lie": 1, "bp": 72, "sx": 12, "hx": -62, "bz": .02}

CLIPS_JEU = [
    {"nom": "Atterrissage", "duree": .60, "boucle": False, "ressorts": {"hx": (4.5, .45), "ay": (6, .5)},
     "cles": [(0, {"_repos": True, **SAUT_FIN}),
              (.08, {"bz": 0, "pz": 0, "py": 0, "pp": -12, "ax": -45, "ay": 40, "fx": -15, "sx": 6, "hx": -2}, "in"),
              (.17, {"bz": -.07, "sx": 18, "bp": 4, "hx": 4, "ax": -62, "ay": 22, "fx": -8, "px": .012, "pp": 0}, "out"),
              (.32, {"bz": .004, "sx": -3, "bp": 0, "hx": -5, "ax": -8, "ay": 22, "fx": -12, "px": .006}, "inout"),
              (.46, {"bz": -.014, "sx": 2, "hx": 1, "ax": 0, "ay": 8, "fx": -4, "px": 0}, "inout"),
              (.60, NEUTRE, "inout")]},

    {"nom": "Torgnole", "duree": .72, "boucle": False,
     "ressorts": {"hx": (4.5, .5), "hy": (4.5, .5), "hz": (5, .55), "gax": (7, .6)},
     # Un crochet du droit : le bras, tendu à l'horizontale à hauteur d'épaule (sous le bord de la tête, donc visible
     # de dos comme de face), part de l'arrière et balaie vers l'avant avec la torsion du corps.
     "cles": [(0, NEUTRE),
              (.13, {"bz": -.035, "by": .015, "bw": -14, "sz": -28, "sx": -4, "hz": 22, "hx": -4,
                     "day": 55, "daz": 45, "dfx": -30, "dc": 10, "gax": -50, "gay": 15, "gfx": -45}, "out"),
              (.23, {"tenir": True, "sz": -32, "bw": -15, "daz": 55, "hz": 25}),              # l'élan se lit
              (.30, {"bz": -.02, "by": -.045, "bw": 18, "sz": 32, "sx": 8, "bp": 3, "hz": -8, "hx": -2,
                     "day": 58, "daz": -70, "dfx": -35, "gax": 40, "gay": 20, "gfx": -40, "yp": .8}, "in"),  # frappe
              (.36, {"sz": 36, "daz": -80, "sx": 9}, "out"),                                # impact
              (.42, {"tenir": True}),
              (.62, {"_repos": True, "bz": -.015, "sx": 3}, "inout"),
              (.72, NEUTRE, "inout")]},

    {"nom": "Touche", "duree": .85, "boucle": False,
     "ressorts": {"hx": (3.2, .32), "hy": (3.2, .32), "hz": (3.2, .35), "ay": (5, .45)},
     # Une gifle reçue : la tête part de côté et le corps tourne avec, les bras sautent (le cookie tombe), un pied se
     # lève ; puis la tête vacille avant de revenir.
     "cles": [(0, NEUTRE),
              (.06, {"by": .03, "bz": .005, "bp": -4, "sx": -7, "sz": -18, "bw": -8, "hx": -7, "hy": 11, "hz": -12,
                     "ay": 105, "ax": -15, "fx": -35, "c": 12, "gpz": .045, "gpy": .02, "gpp": 25, "yp": 1}, "out"),
              (.16, {"bp": -5, "sx": -8, "sz": -22, "hx": -8, "hy": 14, "ay": 112}, "out"),
              (.32, {"by": .01, "bz": -.035, "bp": 3, "sx": 8, "sz": 6, "bw": 0, "hx": 10, "hy": -8, "hz": 4, "ay": 45,
                     "ax": -10, "fx": -20, "c": 0, "gpz": 0, "gpy": 0, "gpp": 0}, "inout"),
              (.46, {"by": 0, "bz": -.012, "bp": 0, "sx": 2, "sz": 0, "hx": -3, "hy": 7, "hz": 0}, "inout"),
              (.60, {"hy": -3, "yp": 0}, "inout"),
              (.85, NEUTRE, "inout")]},

    {"nom": "Repousse", "duree": .90, "boucle": False, "ressorts": {"hx": (4, .4), "hy": (4, .4), "ay": (5, .5)},
     "cles": [(0, NEUTRE),
              (.07, {"by": .05, "bz": .01, "bp": -13, "sx": -8, "hx": -8, "ax": -70, "ay": 45, "fx": -70, "c": 10,
                     "pp": -18, "yp": 1}, "out"),
              (.25, {"by": .07, "bz": .025, "bp": -16, "sx": -10, "hx": -10, "pz": .03, "py": -.02}, "out"),   # soulevé
              (.42, {"by": .03, "bz": -.045, "bp": 6, "sx": 12, "hx": 10, "pz": 0, "py": 0, "pp": 0,
                     "gax": -60, "dax": 40, "gay": 70, "day": 60, "fx": -20, "c": 0}, "in"),        # se rattrape
              (.55, {"bz": -.02, "bp": 0, "sx": 4, "hx": 2, "gax": 30, "dax": -50, "ay": 75, "hy": 8}, "inout"),
              (.70, {"gax": -20, "dax": 15, "ay": 40, "hy": -5, "yp": 0}, "inout"),
              (.90, NEUTRE, "inout")]},

    {"nom": "Croassement", "duree": 1.25, "boucle": False, "extra": extra_croassement,
     "ressorts": {"hx": (4, .5), "ay": (5, .55)},
     "cles": [(0, NEUTRE),
              (.24, {"bz": -.03, "sx": 12, "bp": 3, "hx": 10, "ax": 15, "ay": 18, "fx": -25, "c": 4}, "inout"),  # se ramasse
              (.30, {"tenir": True, "gorge": 0}),
              # chante tête levée, comme une vraie grenouille : la gorge s'offre aux regards
              (.38, {"bz": .018, "sx": -10, "bp": -4, "hx": -18, "ax": 35, "ay": 50, "fx": -15, "c": 14, "yp": 1}, "in"),
              (.50, {"gorge": 1}, "out"),
              (.62, {"gorge": 1}),
              (.66, {"tenir": True, "sx": -12, "hx": -20}),
              (.78, {"gorge": 0}, "in"),
              (.84, {"bz": -.02, "sx": 6, "bp": 0, "hx": 6, "ax": 10, "ay": 20, "c": 0, "yp": .3}, "out"),
              (1.05, {"_repos": True, "bz": -.012}, "inout"),
              (1.25, NEUTRE, "inout")]},

    # Lancer de côté (le gel éclair part droit devant) : levé derrière la tête, le bras serait caché par la tête de face
    # comme de dos ; à l'horizontale il se voit. Genou levé à l'élan, pied posé en avant au lâcher (0,38 s).
    {"nom": "Lancer", "duree": .88, "boucle": False, "ressorts": {"hx": (4.5, .5), "hz": (5, .55), "gax": (7, .6)},
     "cles": [(0, NEUTRE),
              (.20, {"bz": -.02, "by": .02, "bw": -16, "sz": -30, "sx": -8, "hz": 24, "hx": -4,
                     "day": 58, "daz": 60, "dfx": -20, "dc": 8, "gax": -75, "gay": 30, "gfx": -10,
                     "gpz": .06, "gpy": -.02, "gpp": 20}, "out"),
              (.30, {"tenir": True, "sz": -33, "daz": 66}),
              (.38, {"bz": -.03, "by": -.04, "bw": 16, "sz": 30, "sx": 14, "bp": 5, "hz": -6, "hx": 6,
                     "day": 50, "daz": -60, "dfx": -5, "gax": 30, "gay": 20, "gfx": -45,
                     "gpz": 0, "gpy": -.05, "gpp": 0, "yp": .6}, "in"),                     # lâcher
              (.48, {"sx": 18, "bz": -.04, "day": 30, "daz": -85, "dfx": -25, "hx": 8}, "out"),
              (.62, {"gpz": .03, "gpy": -.025}, "inout"),                                    # le pied revient en l'air
              (.72, {"_repos": True, "bz": -.015, "sx": 3}, "inout"),
              (.88, NEUTRE, "inout")]},

    {"nom": "Gele", "duree": .55, "boucle": False, "extra": extra_gele,
     "cles": [(0, NEUTRE),
              (.08, {"bz": .012, "sx": -6, "hx": -8, "ay": 55, "ax": -25, "fx": -30, "c": 14}, "out"),
              (.55, {"tenir": True})]},

    {"nom": "Porter", "duree": 1.2, "boucle": True, "extra": extra_porter, "accessoire": "tete",
     "cles": [(0, {"bz": -.012, "sx": 3, "hx": -3, "ay": 72, "ax": -8, "fx": -25, "c": 6})]},

    {"nom": "PorterBras", "duree": 1.2, "boucle": True, "extra": extra_porter_bras, "accessoire": "ventre",
     "cles": [(0, {"bz": -.015, "sx": -6, "bp": -2, "hx": -5, "ax": -70, "ay": 0, "az": -35, "fx": -15, "c": 5})]},

    {"nom": "Nage", "duree": 1.0, "boucle": True, "eau": True, "ressorts": {"hx": (3.5, .6)},
     "cles": [(0, {**NAGE_BASE, "pz": .055, "px": .05, "pp": -25, "pw": 25, "ax": -135, "ay": 62, "fx": -20}),
              (.15, {"pz": -.02, "px": .015, "pp": 45, "pw": 0, "by": -.03, "bz": .03, "bp": 68, "hx": -58}, "out"),
              (.22, {"ax": -165, "ay": 12, "fx": -5}),
              (.55, {"pz": -.02, "px": .015, "pp": 45, "pw": 0, "by": 0, "bz": .02, "bp": 72, "hx": -62}),
              (.70, {"ax": -165, "ay": 12, "fx": -5}),
              (1.0, {"pz": .055, "px": .05, "pp": -25, "pw": 25, "ax": -135, "ay": 62, "fx": -20}, "inout")]},

    {"nom": "Plongeon", "duree": .95, "boucle": False, "eau": True, "ortho": 2.1,
     "ressorts": {"hx": (4, .55)},
     "cles": [(0, NEUTRE),
              (.16, {"bz": -.06, "sx": 16, "bp": 4, "hx": 4, "ax": 55, "ay": 18, "fx": -10, "c": 5}, "out"),
              (.21, {"tenir": True}),
              (.34, {"bz": .07, "by": -.03, "bp": 28, "sx": 0, "hx": -10, "ax": -165, "ay": 12, "fx": 0, "c": 15,
                     "pp": 50}, "in"),
              (.46, {"lie": 1, "pz": -.02}, "inout"),
              (.58, {"bz": .11, "by": -.06, "bp": 80, "sx": 6, "hx": 8}, "out"),
              (.80, {"bz": .02, "by": -.09, "bp": 125, "hx": 12}, "in"),
              (.95, {"bz": -.10, "by": -.10, "bp": 140}, "lin")]},

    {"nom": "SortieEau", "duree": 1.3, "boucle": False, "eau": True, "extra": extra_sortie,
     "ressorts": {"hx": (4, .5)},
     "cles": [(0, {**NAGE_BASE, "pz": -.02, "px": .015, "pp": 45, "ax": -165, "ay": 12, "fx": -5}),
              (.20, {"ax": -150, "ay": 35, "fx": -20, "hx": -66, "pz": .055, "px": .05, "pp": -25}, "inout"),
              (.40, {"bz": .10, "bp": 40, "sx": 8, "hx": -35, "ax": -70, "ay": 30, "fx": -30, "pz": -.02, "px": .015,
                     "pp": 45}, "in"),
              (.58, {"bz": .07, "bp": 0, "sx": -4, "hx": -4, "ax": -20, "ay": 60, "fx": -20, "lie": 0, "pz": .04,
                     "pp": -5}, "out"),
              (.70, {"bz": -.055, "sx": 12, "hx": 6, "pz": 0, "px": .01, "pp": 0, "ay": 30, "ax": -30, "yp": 0}, "in"),
              (.80, {"bz": -.015, "sx": 2, "hx": 0, "ay": 25, "ax": 0, "fx": -10, "yp": 1}, "inout"),
              (1.14, {"tenir": True}),
              (1.30, NEUTRE, "inout")]},

    {"nom": "DefaiteDecue", "duree": 2.6, "boucle": False, "extra": extra_decue, "sol": [(1.2, 2.6, "contact")],
     "cles": [(0, NEUTRE),
              (.40, {"c": -12, "sx": 12, "hx": 18, "ay": -6, "ax": 6, "fx": -6, "bz": -.025, "yf": .7}, "inout"),
              (.75, {"bz": -.012, "sx": 7, "hx": 12}, "inout"),
              (1.05, {"bz": -.03, "sx": 14, "hx": 22}, "inout"),
              (1.55, {"bz": -.105, "by": .045, "sx": 20, "bp": -6, "hx": 26, "py": -.075, "pp": -35, "px": .02,
                      "ay": 0, "ax": -12, "yf": 1}, "inout"),
              (2.6, {"tenir": True})]},

    {"nom": "DefaiteComique", "duree": 2.5, "boucle": False, "extra": extra_comique, "ortho": 2.1,
     "sol": [(.40, 2.5, "contact")], "ressorts": {"hx": (4, .35), "ay": (5, .4)},
     "cles": [(0, NEUTRE),
              (.22, {"hy": 14, "br": 6, "ay": 70, "ax": -20, "fx": -20, "by": .015, "yp": 1}, "out"),
              (.30, {"tenir": True}),
              (.62, {"bp": -88, "br": 0, "sx": -6, "hx": -6, "hy": 0, "by": .10, "lie": 1, "pz": .10, "py": -.12,
                     "pp": 20, "ay": 95, "ax": -30, "fx": -10}, "in"),
              (.72, {"bp": -96}, "out"),
              (.85, {"bp": -90, "yp": 0, "yf": 1}, "inout"),
              (2.5, {"tenir": True})]},
]


# ---------------------------------------------------------------- rendus de contrôle des nouveaux clips
VUES = {"face": (-58, 12), "profil": (0, 6), "dos": (90, 24)}


def accessoires(arm):
    """Cookies de démonstration (rendus seulement, jamais exportés) : sur la tête, ou serré contre le ventre."""
    brun = materiau("Cookie_apercu", "#c98a3e", .7)
    choco = materiau("Pepite_apercu", "#4a2a17", .5)
    noir = bpy.data.materials.get("Froggy_Contour")      # faces arrière cachées : la coque retournée fait le trait
    objets = {}
    arm.data.pose_position = "REST"; bpy.context.view_layer.update()
    for nom, os_, M in (("tete", "Head", Matrix.Translation((0, .02, 1.035))),
                        ("ventre", "Spine1", Matrix.Translation((0, -.235, .33)) @ Matrix.Rotation(pi / 2, 4, "X"))):
        bm = bmesh.new()
        bmesh.ops.create_cone(bm, cap_ends=True, segments=40, radius1=.165, radius2=.165, depth=.05)
        for i, (x, y) in enumerate(((.07, .04), (-.06, .07), (-.02, -.08), (.09, -.06), (-.10, -.02))):
            r = bmesh.ops.create_uvsphere(bm, u_segments=8, v_segments=6, radius=.022,
                                          matrix=Matrix.Translation((x, y, .028)))
            for f in {f for v in r["verts"] for f in v.link_faces}: f.material_index = 1
        me = bpy.data.meshes.new("Cookie_" + nom); bm.to_mesh(me); bm.free()
        for m in (brun, choco, noir, noir): me.materials.append(m)
        ob = lier(bpy.data.objects.new("Cookie_" + nom, me))
        coque = ob.modifiers.new("contour", "SOLIDIFY")
        coque.thickness = CONTOUR_EP; coque.offset = 1; coque.use_flip_normals = True; coque.material_offset = 2
        ob.parent = arm; ob.parent_type = "BONE"; ob.parent_bone = os_
        ob.matrix_world = M
        ob.hide_render = True
        objets[nom] = ob
    arm.data.pose_position = "POSE"
    return objets


def planche_clip(cam, sol, clip, n, poses=8):
    dossier = SORTIE / "controle" / "planches" / clip["nom"]; dossier.mkdir(parents=True, exist_ok=True)
    sc = bpy.context.scene; sc.render.resolution_x = sc.render.resolution_y = 300
    sol.hide_render = bool(clip.get("eau"))
    vues = ("profil", "face", "dos") if clip.get("eau") else ("face", "profil", "dos")
    temps = []
    for k in range(poses):
        f = 1 + round((n - 1) * k / (poses - 1)); temps.append((f - 1) / FPS)
        sc.frame_set(f)
        for vue in vues:
            az, el = VUES[vue]
            viser(cam, az, el, (0, 0, .5), ortho=clip.get("ortho", 1.6))
            sc.render.filepath = str(dossier / f"{vue}_{k}.png"); bpy.ops.render.render(write_still=True)
    sol.hide_render = False
    (dossier / "planche.json").write_text(json.dumps({"vues": list(vues), "temps": temps}))
