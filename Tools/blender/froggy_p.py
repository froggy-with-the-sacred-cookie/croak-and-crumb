"""Froggy P : le Froggy kawaii modelé, armé et animé par script Blender (gratuit, sans Meshy), d'après l'image
d63b6b90-…png.webp. Mesures prises sur l'image (1014 × 1522) : 1 m = 928 px, sol à y = 1248, axe à x = 507.

1. corps en métaballes (tête, bosses des yeux, torse, bras, mains et doigts, jambes, pieds et orteils), converti,
   allégé, plante des pieds aplanie ;
2. ventre taillé dans la peau (il plie avec elle) ; joues et reflets, yeux, bouche : décalques projetés de face sur la
   tête, qui est rigide, puisque le dessin est vu de face ;
3. squelette humanoïde aux noms Mixamo (l'avatar humanoïde de Unity le reconnaît seul), poids automatiques, décalques
   pesés comme la peau la plus proche, contour noir en coque retournée (le trait du dessin) ;
4. cinq clips sur place, à 30 i/s : Repos, Marche, Course (boucles), Saut, Victoire ; pieds tenus par IK, cuits à
   l'export. Pas d'échelle d'os : l'humanoïde de Unity l'ignorerait ;
5. un FBX par clip (Froggy_P_<Clip>.fbx), le .blend, et des rendus de contrôle (superposition à l'image, quatre vues,
   un GIF par clip).

FROGGY_ROOT=<dépôt> blender -b --factory-startup --python Tools/blender/froggy_p.py -- <dossier de sortie> [--sans-rendus]
"""
import bpy, bmesh, math, sys, os, json, subprocess, shutil
import numpy as np
from pathlib import Path
from mathutils import Vector, Matrix
from mathutils.bvhtree import BVHTree

args = sys.argv[sys.argv.index("--") + 1:]
SORTIE = Path(args[0]).resolve(); SORTIE.mkdir(parents=True, exist_ok=True)
RENDUS = "--sans-rendus" not in args
RACINE = Path(os.environ.get("FROGGY_ROOT", os.getcwd()))
REFERENCE = RACINE / "d63b6b90-cbbb-418b-87d0-492cdd0b6408.png.webp"
PX, SOL, AXE = 928, 1248, 507          # échelle de l'image : px par mètre, ligne du sol, axe de symétrie
FPS = 30
pi = math.pi

# couleurs relevées sur l'image (sRGB)
PEAU, VENTRE, JOUE, NOIR, BLANC = "#c9f695", "#f9f9d0", "#fda5c1", "#0a0a0a", "#f6fde8"
CONTOUR_EP = .011                      # le trait de l'image fait ~15 px, soit 1,6 cm ; un peu moins en 3D (il s'ajoute des deux côtés)


def lin(h):
    c = [int(h[i:i + 2], 16) / 255 for i in (1, 3, 5)]
    return tuple(v / 12.92 if v <= .04045 else ((v + .055) / 1.055) ** 2.4 for v in c)


def materiau(nom, hexa, rugosite=.45, culling=False):
    mat = bpy.data.materials.new(nom)
    bsdf = mat.node_tree.nodes.get("Principled BSDF")
    bsdf.inputs["Base Color"].default_value = (*lin(hexa), 1)
    bsdf.inputs["Roughness"].default_value = rugosite
    mat.diffuse_color = (*lin(hexa), 1)
    mat.use_backface_culling = culling
    mat.use_backface_culling_shadow = culling      # la coque du contour n'ombre pas la peau
    return mat


def lier(ob):
    bpy.context.scene.collection.objects.link(ob)
    return ob


# ---------------------------------------------------------------- 1. le corps
R_MB = 1 / .575   # demi-axe visible = 0,575 × rayon × taille (raideur 2, seuil 0,6 ; mesuré sur Blender 5.2)


def ellipsoide(mb, centre, demi, axe=None):
    e = mb.elements.new(type="ELLIPSOID")
    e.co = centre; e.radius = R_MB; e.stiffness = 2.0
    e.size_x, e.size_y, e.size_z = demi
    if axe is not None: e.rotation = Vector((0, 0, 1)).rotation_difference(Vector(axe))
    return e


def segment(mb, a, b, r):
    a, b = Vector(a), Vector(b)
    ellipsoide(mb, (a + b) / 2, (r, r, (b - a).length / 2), b - a)


def corps_metaballes():
    mb = bpy.data.metaballs.new("Corps"); mb.resolution = mb.render_resolution = .0065; mb.threshold = .6
    ob = lier(bpy.data.objects.new("Corps_mb", mb))
    ellipsoide(mb, (0, 0, .675), (.40, .29, .235))                       # tête : 0,84 m de large au niveau des joues
    for s in (-1, 1):
        ellipsoide(mb, (s * .18, 0, .61), (.185, .24, .15))            # bas des joues : la tête reste large jusqu'en bas
        ellipsoide(mb, (s * .217, -.02, .855), (.14, .13, .145))        # bosses des yeux, haut à 1 m
        segment(mb, (s * .205, 0, .41), (s * .29, -.01, .27), .05)       # bras
        ellipsoide(mb, (s * .29, -.012, .262), (.05, .037, .04))        # main
        for x, y, z in ((.262, -.02, .238), (.294, -.026, .232), (.322, -.015, .244)):
            ellipsoide(mb, (s * x, y, z), (.017, .017, .017))           # trois doigts ronds
        segment(mb, (s * .115, 0, .17), (s * .12, 0, .05), .056)         # jambe
        ellipsoide(mb, (s * .15, -.035, .028), (.08, .07, .032))         # pied, tourné vers l'extérieur
        for x, y in ((.215, -.065), (.185, -.10), (.135, -.115)):
            ellipsoide(mb, (s * x, y, .022), (.022, .022, .018))         # orteils
    ellipsoide(mb, (0, 0, .29), (.195, .165, .17))                       # torse
    ellipsoide(mb, (0, 0, .19), (.19, .15, .09))                         # bas du ventre, en poire
    dg = bpy.context.evaluated_depsgraph_get()
    me = bpy.data.meshes.new_from_object(ob.evaluated_get(dg))
    bpy.data.objects.remove(ob); bpy.data.metaballs.remove(mb)

    bm = bmesh.new(); bm.from_mesh(me)
    bmesh.ops.remove_doubles(bm, verts=bm.verts, dist=1e-5)
    coupe = bmesh.ops.bisect_plane(bm, geom=bm.verts[:] + bm.edges[:] + bm.faces[:], plane_co=(0, 0, .006),
                                   plane_no=(0, 0, 1), clear_inner=True)
    bmesh.ops.holes_fill(bm, edges=[e for e in coupe["geom_cut"] if isinstance(e, bmesh.types.BMEdge)])
    bmesh.ops.triangulate(bm, faces=[f for f in bm.faces if len(f.verts) > 4])
    for v in bm.verts: v.co.z -= .006
    bm.to_mesh(me); bm.free()
    corps = lier(bpy.data.objects.new("Froggy", me))
    dec = corps.modifiers.new("allege", "DECIMATE"); dec.ratio = min(1, 10000 / sum(len(p.vertices) - 2 for p in me.polygons))   # en triangles
    dg = bpy.context.evaluated_depsgraph_get()
    leger = bpy.data.meshes.new_from_object(corps.evaluated_get(dg))
    corps.modifiers.clear(); corps.data = leger; bpy.data.meshes.remove(me)
    leger.shade_smooth()
    return corps


# ---------------------------------------------------------------- 2. décalques projetés de face
def bvh_de(ob):
    bm = bmesh.new(); bm.from_mesh(ob.data); t = BVHTree.FromBMesh(bm); bm.free(); return t


def projeter(bvh, x, z, decal):
    p, n, _, _ = bvh.ray_cast(Vector((x, -2, z)), Vector((0, 1, 0)))
    return (p + n * decal, n) if p is not None else (None, None)


def px(x, y): return ((x - AXE) / PX, (SOL - y) / PX)


def ellipse(bm, bvh, cx, cz, ax, az, decal, mat, anneaux=8, secteurs=40):
    centre = bm.verts.new(projeter(bvh, cx, cz, decal)[0])
    rangs = []
    for i in range(1, anneaux + 1):
        r = i / anneaux
        rangs.append([bm.verts.new(projeter(bvh, cx + ax * r * math.cos(2 * pi * j / secteurs),
                                            cz + az * r * math.sin(2 * pi * j / secteurs), decal)[0])
                      for j in range(secteurs)])
    faces = [bm.faces.new((centre, rangs[0][j], rangs[0][(j + 1) % secteurs])) for j in range(secteurs)]
    for a, b in zip(rangs, rangs[1:]):
        faces += [bm.faces.new((a[j], b[j], b[(j + 1) % secteurs], a[(j + 1) % secteurs])) for j in range(secteurs)]
    for f in faces: f.material_index = mat; f.smooth = True


def bouche(bm, bvh, mat, rayon=.0075, cotes=8):
    """Le « w » : points relevés sur l'image, lissés (Catmull-Rom), posés sur la peau, gainés d'un tube."""
    cles = [px(x, y) for x, y in ((427, 522), (440, 545), (465, 562), (490, 550), (507, 533), (524, 550),
                                  (548, 562), (573, 545), (587, 522))]
    pts = []
    for i in range(len(cles) - 1):
        p0, p1, p2, p3 = (Vector(cles[max(0, min(len(cles) - 1, k))]) for k in (i - 1, i, i + 1, i + 2))
        for s in range(6):
            t = s / 6
            pts.append(.5 * (2 * p1 + (-p0 + p2) * t + (2 * p0 - 5 * p1 + 4 * p2 - p3) * t * t
                             + (-p0 + 3 * p1 - 3 * p2 + p3) * t ** 3))
    pts.append(Vector(cles[-1]))
    surf = [projeter(bvh, p.x, p.y, .003 + rayon * .6) for p in pts]
    anneaux = []
    for i, (p, n) in enumerate(surf):
        t = (surf[min(i + 1, len(surf) - 1)][0] - surf[max(i - 1, 0)][0]).normalized()
        b = t.cross(n).normalized(); n2 = b.cross(t)
        anneaux.append([bm.verts.new(p + (n2 * math.cos(2 * pi * k / cotes) + b * math.sin(2 * pi * k / cotes)) * rayon)
                        for k in range(cotes)])
    for a, c in zip(anneaux, anneaux[1:]):
        for k in range(cotes):
            f = bm.faces.new((a[k], a[(k + 1) % cotes], c[(k + 1) % cotes], c[k])); f.material_index = mat; f.smooth = True
    for p, _ in (surf[0], surf[-1]):          # bouts arrondis
        r = bmesh.ops.create_uvsphere(bm, u_segments=8, v_segments=6, radius=rayon, matrix=Matrix.Translation(p))
        for f in {f for v in r["verts"] for f in v.link_faces}: f.material_index = mat; f.smooth = True


def decouper_ventre(corps, i_ventre, cx=0, cz=.278, ax=.172, az=.155):
    """Le ventre est taillé dans la peau : arêtes coupées sur l'ellipse vue de face, faces intérieures en crème.
    Un décalque posé au-dessus se faisait traverser par la peau dès que le torse pliait (saut, marche)."""
    bm = bmesh.new(); bm.from_mesh(corps.data)
    f = lambda co: ((co.x - cx) / ax) ** 2 + ((co.z - cz) / az) ** 2 - 1
    devant = lambda co: co.y < -.03
    val = {v: f(v.co) for v in bm.verts}; coupes = set()
    for e in list(bm.edges):
        a, b = e.verts
        if devant(a.co) and devant(b.co) and val[a] * val[b] < 0:
            t = val[a] / (val[a] - val[b]); pos = a.co.lerp(b.co, t)
            _, nv = bmesh.utils.edge_split(e, a, t); nv.co = pos; val[nv] = 0; coupes.add(nv)
    for face in list(bm.faces):
        vs = [v for v in face.verts if v in coupes]
        if len(vs) == 2 and not any(vs[1] in (e.other_vert(vs[0]),) for e in vs[0].link_edges):
            bmesh.utils.face_split(face, vs[0], vs[1])
    bmesh.ops.triangulate(bm, faces=[fa for fa in bm.faces if len(fa.verts) > 4])
    for fa in bm.faces:
        c = fa.calc_center_median()
        if devant(c) and f(c) < 0: fa.material_index = i_ventre
    bm.to_mesh(corps.data); bm.free(); corps.data.shade_smooth()


def decalques(corps, mats):
    bvh = bvh_de(corps)
    bm = bmesh.new()
    i = {m: k for k, m in enumerate(mats)}
    for s in (-1, 1):
        ellipse(bm, bvh, s * .271, .697, .087, .079, .0025, i["Joue"])
        ellipse(bm, bvh, s * .29, .735, .016, .013, .0045, i["Blanc"], anneaux=2, secteurs=16)
        ellipse(bm, bvh, s * .209, .831, .059, .072, .003, i["Noir"])
    bouche(bm, bvh, i["Noir"])
    me = bpy.data.meshes.new("Decalques"); bm.to_mesh(me); bm.free()
    ob = lier(bpy.data.objects.new("Decalques", me))
    return ob


# ---------------------------------------------------------------- 3. squelette, poids, contour
OS = [  # nom, tête, queue, parent
    ("Hips", (0, 0, .17), (0, 0, .23), None),
    ("Spine", (0, 0, .23), (0, 0, .31), "Hips"),
    ("Spine1", (0, 0, .31), (0, 0, .39), "Spine"),
    ("Neck", (0, 0, .39), (0, 0, .45), "Spine1"),
    ("Head", (0, 0, .45), (0, 0, .95), "Neck"),
]
for cote, s in (("Left", 1), ("Right", -1)):   # la grenouille regarde vers −Y : sa gauche est en +X
    OS += [
        (f"{cote}Shoulder", (s * .04, 0, .38), (s * .18, 0, .40), "Spine1"),
        (f"{cote}Arm", (s * .18, 0, .405), (s * .238, -.005, .335), f"{cote}Shoulder"),
        (f"{cote}ForeArm", (s * .238, -.005, .335), (s * .283, -.01, .272), f"{cote}Arm"),
        (f"{cote}Hand", (s * .283, -.01, .272), (s * .305, -.018, .215), f"{cote}ForeArm"),
        (f"{cote}UpLeg", (s * .112, 0, .16), (s * .116, -.008, .095), "Hips"),     # genou un peu en avant : l'IK plie par là
        (f"{cote}Leg", (s * .116, -.008, .095), (s * .12, 0, .035), f"{cote}UpLeg"),
        (f"{cote}Foot", (s * .12, 0, .035), (s * .16, -.06, .014), f"{cote}Leg"),
        (f"{cote}ToeBase", (s * .16, -.06, .014), (s * .18, -.10, .014), f"{cote}Foot"),
    ]


def squelette():
    data = bpy.data.armatures.new("Squelette"); arm = lier(bpy.data.objects.new("Squelette", data))
    bpy.context.view_layer.objects.active = arm; bpy.ops.object.mode_set(mode="EDIT")
    for nom, t, q, parent in OS:
        b = data.edit_bones.new(nom); b.head, b.tail = t, q
        if parent: b.parent = data.edit_bones[parent]; b.use_connect = (Vector(t) - data.edit_bones[parent].tail).length < 1e-6
    bpy.ops.armature.select_all(action="SELECT"); bpy.ops.armature.calculate_roll(type="GLOBAL_POS_Y")
    bpy.ops.object.mode_set(mode="OBJECT")
    for pb in arm.pose.bones: pb.rotation_mode = "QUATERNION"
    return arm


def lisser_poids(ob, passes=4, part=.5):
    """Moyenne des voisins, puis normalisation : la chaleur des os plissait le ventre à la marche."""
    me = ob.data; V, G = len(me.vertices), len(ob.vertex_groups)
    W = np.zeros((V, G))
    for v in me.vertices:
        for g in v.groups: W[v.index, g.group] = g.weight
    a = np.array([e.vertices[:] for e in me.edges])
    deg = np.bincount(a.ravel(), minlength=V)[:, None]
    for _ in range(passes):
        S = np.zeros_like(W); np.add.at(S, a[:, 0], W[a[:, 1]]); np.add.at(S, a[:, 1], W[a[:, 0]])
        W = (1 - part) * W + part * S / np.maximum(deg, 1)
    W[W < .01] = 0; W /= np.maximum(W.sum(1, keepdims=True), 1e-9)
    for gi, g in enumerate(ob.vertex_groups):
        g.remove(range(V))
        for i in np.nonzero(W[:, gi])[0]: g.add([int(i)], float(W[i, gi]), "REPLACE")


def rendre_au_bassin(ob, bas=.10, haut=.16):
    """Les cuisses n'ont pas d'influence au-dessus de l'entrejambe (transition entre 10 et 16 cm) : la chaleur des os
    leur donnait le bas du ventre, qui se plissait dès qu'elles se repliaient (saut, course)."""
    g = {x.name: x for x in ob.vertex_groups}
    for v in ob.data.vertices:
        k = max(0, min(1, (v.co.z - bas) / (haut - bas)))
        if k == 0: continue
        rendu = 0
        for cote in ("Left", "Right"):
            for os_ in (cote + "UpLeg", cote + "Leg"):
                w = next((e.weight for e in v.groups if e.group == g[os_].index), 0)
                if w: g[os_].add([v.index], w * (1 - k), "REPLACE"); rendu += w * k
        if rendu:
            w = next((e.weight for e in v.groups if e.group == g["Hips"].index), 0)
            g["Hips"].add([v.index], w + rendu, "REPLACE")


def peser(arm, corps, deco):
    bpy.ops.object.select_all(action="DESELECT")
    corps.select_set(True); arm.select_set(True); bpy.context.view_layer.objects.active = arm
    bpy.ops.object.parent_set(type="ARMATURE_AUTO")
    rendre_au_bassin(corps)
    lisser_poids(corps)
    vides = [v.index for v in corps.data.vertices if not v.groups]
    # décalques : poids interpolés sur la face de peau la plus proche (un seul sommet voisin froissait le ventre)
    vs = corps.data.vertices
    bvh = BVHTree.FromPolygons([v.co for v in vs], [p.vertices[:] for p in corps.data.polygons])
    noms = {g.index: g.name for g in corps.vertex_groups}
    for g in corps.vertex_groups: deco.vertex_groups.new(name=g.name)
    for v in deco.data.vertices:
        q, _, k, _ = bvh.find_nearest(v.co)
        somme, poids = 0, {}
        for i in corps.data.polygons[k].vertices:
            w = 1 / ((vs[i].co - q).length + 1e-4); somme += w
            for g in vs[i].groups: poids[g.group] = poids.get(g.group, 0) + w * g.weight
        for gi, w in poids.items(): deco.vertex_groups[noms[gi]].add([v.index], w / somme, "REPLACE")
    # contour : copie de la peau poussée le long des normales, faces retournées, matériau aux faces arrière cachées
    contour = corps.copy(); contour.data = corps.data.copy(); contour.name = "Contour"; lier(contour)
    bm = bmesh.new(); bm.from_mesh(contour.data); bm.normal_update()
    for v in bm.verts: v.co += v.normal * CONTOUR_EP
    bmesh.ops.reverse_faces(bm, faces=bm.faces[:])
    for f in bm.faces: f.material_index = MATS.index("Contour")
    bm.to_mesh(contour.data); bm.free()
    deco.parent = arm
    bpy.ops.object.select_all(action="DESELECT")
    for o in (corps, deco, contour): o.select_set(True)
    bpy.context.view_layer.objects.active = corps; bpy.ops.object.join()
    corps.modifiers[0].object = arm
    return len(vides)


# ---------------------------------------------------------------- 4. les clips
def rot(axes):
    R = Matrix.Identity(3)
    for ax, deg in axes: R = Matrix.Rotation(math.radians(deg), 3, ax) @ R
    return R


def poser(arm, cibles, pose, f):
    """pose : {os: [(axe du monde, degrés)…]} appliqué au repos de l'os (FK) ; « _bassin » : déplacement des Hips ;
    « _pied<Côté> » : (déplacement de la cheville, rotations) pour la cible IK. Axes : X vers la gauche de Froggy,
    Y vers son dos, Z en haut ; +X sur un os qui pend le porte en arrière, sur un os qui monte le penche en avant."""
    for nom, pb in arm.pose.bones.items():
        repos = arm.data.bones[nom].matrix_local.to_3x3()
        q = (repos.inverted() @ rot(pose.get(nom, [])) @ repos).to_quaternion()
        if pb.rotation_quaternion.dot(q) < 0: q.negate()
        pb.rotation_quaternion = q; pb.keyframe_insert("rotation_quaternion", frame=f)
    h = arm.pose.bones["Hips"]
    h.location = arm.data.bones["Hips"].matrix_local.to_3x3().inverted() @ Vector(pose.get("_bassin", (0, 0, 0)))
    h.keyframe_insert("location", frame=f)
    for cote, cible in cibles.items():
        d, axes = pose.get("_pied" + cote, ((0, 0, 0), []))
        repos = arm.data.bones[cote + "Foot"].matrix_local
        cible.location = repos.translation + Vector(d)
        q = (rot(axes) @ repos.to_3x3()).to_quaternion()
        if cible.rotation_quaternion.dot(q) < 0: q.negate()
        cible.rotation_quaternion = q
        cible.keyframe_insert("location", frame=f); cible.keyframe_insert("rotation_quaternion", frame=f)


def bras(p, ax=0., ay=0., fx=0., cote=None):
    """ax : balancier (+ en arrière) ; ay : écarte le bras vers le haut ; fx : plie le coude (− vers l'avant)."""
    for c, s in ((("Left", 1), ("Right", -1)) if cote is None else (cote,)):
        p.setdefault(c + "Arm", []).extend([("Y", -s * ay), ("X", ax)])
        p.setdefault(c + "ForeArm", []).append(("X", fx))


def pied(ph, appui, L, h):
    """Trajectoire d'une cheville sur place, ph ∈ [0, 1[ : 0 = pose au sol devant. Renvoie (y, z, inclinaison °)."""
    if ph < appui:
        u = ph / appui
        incl = -10 * (1 - u / .15) if u < .15 else 22 * max(0, (u - .75) / .25)   # pointe relevée à la pose, talon décollé au départ
        return -L + 2 * L * u, .07 * .5 * math.sin(math.radians(max(incl, 0))), incl
    u = (ph - appui) / (1 - appui)
    return L - 2 * L * (.5 - .5 * math.cos(pi * u)), h * math.sin(pi * u), 22 * (1 - u) - 10 * u


def allure(t, T, appui, L, h, bob, roulis_deg, lacet_deg, penche, balancier, coude, tete):
    phi = (t / T) % 1; w = 2 * pi * phi
    p = {}
    for cote, dec in (("Left", 0), ("Right", .5)):
        y, z, incl = pied((phi + dec) % 1, appui, L, h)
        p["_pied" + cote] = ((0, y, z), [("X", incl)])
    sur_gauche = math.cos(w - 2 * pi * appui / 2)              # +1 au milieu de l'appui gauche
    retard = math.cos(w - 2 * pi * (appui / 2 + .09))           # la tête suit avec un temps de retard
    p["_bassin"] = (.010 * sur_gauche, 0, -.014 + bob * math.cos(2 * (w - pi * appui)))
    p["Hips"] = [("Y", roulis_deg * sur_gauche), ("Z", lacet_deg * math.sin(w))]
    p["Spine"] = [("X", penche), ("Y", -.6 * roulis_deg * sur_gauche), ("Z", -.8 * lacet_deg * math.sin(w))]
    p["Spine1"] = [("X", penche * .3), ("Y", -.25 * roulis_deg * sur_gauche)]
    p["Head"] = [("X", tete - 2 * math.cos(2 * (w - pi * appui) - .6)), ("Y", .5 * roulis_deg * retard)]
    bras(p, balancier * math.cos(w), 4 + 3 * abs(math.sin(w)), coude, ("Left", 1))
    bras(p, -balancier * math.cos(w), 4 + 3 * abs(math.sin(w)), coude, ("Right", -1))
    return p


def repos(t, T=3.0):
    s = .5 - .5 * math.cos(2 * pi * t / T)                       # souffle
    p = {"_bassin": (0, 0, -.008 - .006 * s),
         "Spine": [("X", 1.5 * s)], "Spine1": [("X", 1.0 * s)],
         "Head": [("X", -2 * s), ("Y", 3 * math.sin(2 * pi * t / T))]}
    bras(p, -2 * s, 3 + 4 * s, -6)
    return p


CLES = ("bz", "sx", "hx", "hy", "ax", "ay", "fx", "pz", "py", "pp", "roul")


def par_cles(cles, t):
    """Interpole des poses clés {temps: {param: valeur}} (lissage en S, valeurs absentes = 0)."""
    temps = sorted(cles)
    if t <= temps[0]: a = b = temps[0]
    elif t >= temps[-1]: a = b = temps[-1]
    else: a = max(k for k in temps if k <= t); b = min(k for k in temps if k > t)
    u = 0 if a == b else (t - a) / (b - a); u = u * u * (3 - 2 * u)
    return {k: cles[a].get(k, 0) * (1 - u) + cles[b].get(k, 0) * u for k in CLES}


def depuis_params(v):
    p = {"_bassin": (0, 0, v["bz"]), "Hips": [("Y", v["roul"])],
         "Spine": [("X", v["sx"]), ("Y", -.6 * v["roul"])], "Spine1": [("X", .3 * v["sx"])],
         "Head": [("X", v["hx"]), ("Y", v["hy"])]}
    bras(p, v["ax"], v["ay"], v["fx"])
    for c in ("Left", "Right"): p["_pied" + c] = ((0, v["py"], v["pz"]), [("X", v["pp"])])
    return p


SAUT = {0: {"bz": -.01},
        .10: {"bz": -.05, "sx": 12, "hx": -6, "ax": 30, "fx": -10},                     # tassé
        .22: {"bz": .03, "sx": -4, "hx": -8, "ax": -70, "ay": 25, "fx": -20, "pz": .02, "pp": 35},   # détente
        .45: {"bz": .04, "sx": 4, "hx": -4, "ax": -20, "ay": 60, "fx": -30, "pz": .09, "py": -.02, "pp": -10},  # en l'air, pattes repliées
        .80: {"bz": .045, "sx": 3, "hx": -5, "ax": -15, "ay": 66, "fx": -32, "pz": .095, "py": -.02, "pp": -10}}

VICTOIRE = {0: {"bz": -.01},
            .15: {"bz": -.05, "sx": 10, "ax": 20, "hx": -2},
            .30: {"bz": .10, "pz": .10, "sx": -6, "hx": -10, "ay": 80, "ax": -10, "fx": -25, "pp": 30},
            .45: {"bz": -.05, "sx": 8, "ay": 50, "hx": -4},
            .60: {"bz": .09, "pz": .09, "sx": -6, "hx": -10, "ay": 85, "ax": -10, "fx": -25, "pp": 30},
            .76: {"bz": -.04, "sx": 5, "ay": 65, "hx": -6},
            .90: {"bz": -.012, "ay": 82, "hx": -8, "fx": -30},
            1.70: {"bz": -.012, "ay": 82, "hx": -8, "fx": -30},
            2.0: {"bz": -.01}}


def victoire(t):
    v = par_cles(VICTOIRE, t)
    env = max(0, min(1, (t - .85) / .1, (1.75 - t) / .1))      # coucou des bras et dandinement entre 0,85 et 1,75 s
    v["ay"] += env * 12 * math.sin(2 * pi * (t - .85) / .45)
    v["roul"] += env * 6 * math.sin(2 * pi * (t - .85) / .9)
    v["hy"] += env * 9 * math.sin(2 * pi * (t - .85) / .9 - .5)
    return depuis_params(v)


CLIPS = [  # nom, durée (s), boucle, pose(t)
    ("Repos", 3.0, True, repos),
    ("Marche", .8, True, lambda t: allure(t, .8, .6, .065, .045, .006, 6, 5, 4, 20, -12, 0)),
    ("Course", 16 / FPS, True, lambda t: allure(t, 16 / FPS, .38, .085, .085, .014, 4, 8, 12, 45, -45, -10)),
    ("Saut", .8, False, lambda t: depuis_params(par_cles(SAUT, t))),
    ("Victoire", 2.0, False, victoire),
]


def cibles_ik(arm):
    cibles = {}
    for cote in ("Left", "Right"):
        c = lier(bpy.data.objects.new("Cible_" + cote + "Foot", None)); c.empty_display_size = .04
        c.rotation_mode = "QUATERNION"; c.matrix_world = arm.data.bones[cote + "Foot"].matrix_local
        ik = arm.pose.bones[cote + "Leg"].constraints.new("IK"); ik.target = c; ik.chain_count = 2
        cr = arm.pose.bones[cote + "Foot"].constraints.new("COPY_ROTATION"); cr.target = c
        cibles[cote] = c
    return cibles


def animer(arm, cibles, nom, duree, fonction):
    for o in [arm, *cibles.values()]:
        o.animation_data_create(); o.animation_data.action = bpy.data.actions.new(nom if o is arm else f"{nom}_{o.name}")
    n = round(duree * FPS)
    for f in range(n + 1): poser(arm, cibles, fonction(f / FPS), f + 1)
    sc = bpy.context.scene; sc.frame_start, sc.frame_end = 1, n + 1
    return n + 1


def exporter(arm, corps, nom):
    chemin = SORTIE / f"Froggy_P_{nom}.fbx"
    bpy.ops.object.select_all(action="DESELECT"); arm.select_set(True); corps.select_set(True)
    bpy.ops.export_scene.fbx(filepath=str(chemin), use_selection=True, object_types={"ARMATURE", "MESH"},
                             add_leaf_bones=False, bake_anim=True, bake_anim_use_all_actions=False,
                             bake_anim_use_nla_strips=False, bake_anim_use_all_bones=True,
                             bake_anim_simplify_factor=0, bake_anim_force_startend_keying=True)
    return chemin


# ---------------------------------------------------------------- 5. rendus de contrôle
def scene_rendu():
    sc = bpy.context.scene; sc.render.engine = "BLENDER_EEVEE"
    sc.view_settings.view_transform = "Standard"
    sc.world = bpy.data.worlds.new("Monde"); sc.world.color = (.75, .75, .72)
    bg = sc.world.node_tree.nodes.get("Background")
    if bg: bg.inputs[0].default_value = (.62, .63, .60, 1); bg.inputs[1].default_value = .9
    soleil = lier(bpy.data.objects.new("Soleil", bpy.data.lights.new("Soleil", "SUN")))
    soleil.data.energy = 3.2; soleil.data.angle = math.radians(12)
    soleil.rotation_euler = (math.radians(40), 0, math.radians(-35))
    sol = bpy.data.meshes.new("Sol"); bm = bmesh.new(); bmesh.ops.create_grid(bm, x_segments=1, y_segments=1, size=3)
    bm.to_mesh(sol); bm.free(); so = lier(bpy.data.objects.new("Sol", sol))
    so.data.materials.append(materiau("Sol", "#e8e3d4", .9))
    cam = lier(bpy.data.objects.new("Camera", bpy.data.cameras.new("Camera"))); sc.camera = cam
    return cam, so


def viser(cam, az, el, cible=(0, 0, .5), ortho=1.35, dist=6):
    a, e = math.radians(az), math.radians(el); c = Vector(cible)
    cam.data.type = "ORTHO"; cam.data.ortho_scale = ortho
    cam.location = c + Vector((math.cos(a) * math.cos(e), math.sin(a) * math.cos(e), math.sin(e))) * dist
    cam.rotation_euler = (c - cam.location).to_track_quat("-Z", "Y").to_euler()


def rendre(chemin, l, h):
    sc = bpy.context.scene; sc.render.resolution_x, sc.render.resolution_y = l, h
    sc.render.filepath = str(chemin); bpy.ops.render.render(write_still=True)


def pixels(chemin):
    im = bpy.data.images.load(str(chemin)); W, H = im.size
    a = np.array(im.pixels[:], dtype=np.float32).reshape(H, W, 4)[::-1]; bpy.data.images.remove(im); return a


def ecrire_png(a, chemin):
    H, W = a.shape[:2]; im = bpy.data.images.new("tmp", W, H, alpha=True)
    im.pixels[:] = np.ascontiguousarray(a[::-1]).ravel(); im.filepath_raw = str(chemin); im.file_format = "PNG"
    im.save(); bpy.data.images.remove(im)


def superposition(cam, sol):
    """Rendu de face à l'échelle exacte de l'image, silhouettes comparées (contour compris)."""
    ref = pixels(REFERENCE)[:, :, :3]; H, W = ref.shape[:2]
    sc = bpy.context.scene; sc.render.film_transparent = True; sol.hide_render = True
    viser(cam, -90, 0, (0, 0, (SOL - H / 2) / PX), ortho=H / PX)
    rendre(SORTIE / "controle" / "face_echelle.png", W, H)
    sc.render.film_transparent = False; sol.hide_render = False
    mod = pixels(SORTIE / "controle" / "face_echelle.png")
    m_mod = mod[:, :, 3] > .5
    couleur = (ref.max(2) - ref.min(2) > .12) | (ref.min(2) > .75)          # vert, crème, rose, blanc
    m_ref = couleur.copy()
    for _ in range(14):                                                     # rendre le trait noir autour
        d = m_ref.copy(); d[1:] |= m_ref[:-1]; d[:-1] |= m_ref[1:]; d[:, 1:] |= m_ref[:, :-1]; d[:, :-1] |= m_ref[:, 1:]; m_ref = d
    iou = (m_mod & m_ref).sum() / (m_mod | m_ref).sum()
    out = np.ones((H, W, 4), np.float32); g = ref.mean(2) * .5 + .25
    out[:, :, 0] = out[:, :, 1] = out[:, :, 2] = g
    out[m_ref & ~m_mod] = (.9, .15, .15, 1); out[m_mod & ~m_ref] = (.15, .35, .95, 1)
    ecrire_png(out, SORTIE / "controle" / "superposition.png")
    cote = np.ones((H, 2 * W, 4), np.float32); cote[:, :W, :3] = ref; cote[:, W:] = mod
    cote[:, W:, :3] = mod[:, :, :3] * mod[:, :, 3:] + np.array(lin("#18191f")) * (1 - mod[:, :, 3:]); cote[:, W:, 3] = 1
    ecrire_png(cote, SORTIE / "controle" / "image_et_modele.png")
    return float(iou)


def quatre_vues(cam):
    vues = []
    for i, (az, el) in enumerate(((-90, 8), (-50, 14), (0, 8), (90, 8), (-120, 35))):
        viser(cam, az, el); p = SORTIE / "controle" / f"vue_{i}.png"; rendre(p, 520, 620); vues.append(pixels(p))
    ecrire_png(np.concatenate(vues, 1), SORTIE / "controle" / "vues.png")


def apercu_clip(cam, nom, n):
    dossier = SORTIE / "controle" / nom; dossier.mkdir(parents=True, exist_ok=True)
    viser(cam, -58, 12, (0, 0, .52), ortho=1.45)
    sc = bpy.context.scene; sc.render.resolution_x = sc.render.resolution_y = 420
    sc.render.filepath = str(dossier / "#####"); sc.frame_start, sc.frame_end = 1, n
    bpy.ops.render.render(animation=True)
    ffmpeg = shutil.which("ffmpeg") or "/opt/homebrew/bin/ffmpeg"
    gif = SORTIE / "controle" / f"Froggy_P_{nom}.gif"
    subprocess.run([ffmpeg, "-hide_banner", "-loglevel", "error", "-y", "-framerate", str(FPS), "-start_number", "1",
                    "-i", str(dossier / "%05d.png"), "-vf", "split[a][b];[a]palettegen=max_colors=96[p];[b][p]paletteuse",
                    "-loop", "0", str(gif)], check=True)
    # planche de 8 poses
    ims = [pixels(dossier / f"{round(1 + (n - 1) * k / 7):05d}.png") for k in range(8)]
    ecrire_png(np.concatenate(ims, 1), SORTIE / "controle" / f"poses_{nom}.png")
    return gif


# ---------------------------------------------------------------- déroulé
bpy.ops.wm.read_factory_settings(use_empty=True)
bpy.context.scene.render.fps = FPS
MATS = ["Peau", "Ventre", "Joue", "Noir", "Blanc", "Contour"]
mats = [materiau("Froggy_Peau", PEAU, .35), materiau("Froggy_Ventre", VENTRE, .45), materiau("Froggy_Joue", JOUE, .4),
        materiau("Froggy_Noir", NOIR, .25), materiau("Froggy_Blanc", BLANC, .3),
        materiau("Froggy_Contour", "#000000", 1.0, culling=True)]

corps = corps_metaballes()
for m in mats: corps.data.materials.append(m)
decouper_ventre(corps, MATS.index("Ventre"))
deco = decalques(corps, MATS)
for m in mats: deco.data.materials.append(m)
arm = squelette()
sans_poids = peser(arm, corps, deco)
cibles = cibles_ik(arm)

rapport = {"faces": len(corps.data.polygons), "sommets": len(corps.data.vertices), "os": len(arm.data.bones),
           "sommets_sans_poids": sans_poids,
           "hauteur": max((corps.matrix_world @ v.co).z for v in corps.data.vertices), "clips": []}

if RENDUS:
    (SORTIE / "controle").mkdir(exist_ok=True)
    cam, sol = scene_rendu()
    if REFERENCE.exists():
        rapport["iou_silhouette"] = superposition(cam, sol)
    else:                                   # l'image de référence n'est pas dans le dépôt (source à vérifier)
        print("SUPERPOSITION sautée : pas d'image", REFERENCE.name, flush=True)
    quatre_vues(cam)

for nom, duree, boucle, fonction in CLIPS:
    n = animer(arm, cibles, nom, duree, fonction)
    chemin = exporter(arm, corps, nom)
    info = {"clip": nom, "images": n, "secondes": duree, "boucle": boucle, "fbx": str(chemin)}
    if RENDUS: info["gif"] = str(apercu_clip(cam, nom, n))
    rapport["clips"].append(info)
    print("CLIP", nom, n, flush=True)

bpy.ops.wm.save_as_mainfile(filepath=str(SORTIE / "froggy_p.blend"))
(SORTIE / "rapport.json").write_text(json.dumps(rapport, indent=2, ensure_ascii=False))
print("RAPPORT", json.dumps(rapport, ensure_ascii=False), flush=True)
