"""Kappa, le gobelin du cookie sacré (#78) : squelette humanoïde posé par script sur le modèle Meshy (18 novembre 2026).

Le squelette et les animations de Meshy ne se téléchargent pas par le MCP (skill route-3d-meshy, § 3) : on arme ici,
gratuitement, comme Froggy P (froggy_p.py). Le modèle (Captures/gobelin/4_meshy/Kappa_Meshy.glb, hors git) est ramené à
80 cm (taille proposée : un peu plus petit que Froggy, ~1 m), pieds au sol, regard vers -Y.
Repères mesurés sur le maillage à 80 cm (tranches de 2 cm) : jambes séparées jusqu'à 23 cm, genou ~13 cm, cheville
~5 cm ; épaule à (±0,11 ; 0,47), main à (±0,30 ; 0,265) en pose A ; cou le plus étroit à 0,54 ; tête de 0,55 à 0,80.
Noms d'os du type Mixamo : Unity les reconnaît en humanoïde, et les mouvements text_to_motion s'y rejouent. Le kappa
s'importe pourtant en Generic, pour garder ses postures tenues (voir kappa_animations.py).
Poids : chaleur des os (Blender), carapace rendue rigide au dos (Spine1), puis lissage (comme froggy_p.py).
Lancer depuis la racine du projet :
    FROGGY_ROOT=$PWD /Applications/Blender.app/Contents/MacOS/Blender -b -P Tools/blender/kappa_squelette.py
Sorties hors git : Captures/gobelin/5_rig/ (Kappa_Modele.fbx, kappa_rig.blend, rendus de contrôle en pose)."""
import bpy, math, os
import numpy as np
from mathutils import Vector, Matrix

R = os.environ.get("FROGGY_ROOT", os.getcwd())
ENTREE = os.path.join(R, "Captures", "gobelin", "4_meshy", "Kappa_Meshy.glb")
SORTIE = os.path.join(R, "Captures", "gobelin", "5_rig")
HAUTEUR = 0.80

for ob in list(bpy.data.objects): bpy.data.objects.remove(ob, do_unlink=True)
bpy.ops.import_scene.gltf(filepath=ENTREE)
corps = next(o for o in bpy.context.scene.objects if o.type == "MESH")
for o in list(bpy.context.scene.objects):
    if o != corps and o.type != "MESH": bpy.data.objects.remove(o, do_unlink=True)
corps.parent = None
# mise à 80 cm, pieds au sol, centré : on cuit la transformation dans le maillage
P = np.array([corps.matrix_world @ v.co for v in corps.data.vertices])
k = HAUTEUR / (P[:, 2].max() - P[:, 2].min())
decal = Vector(((P[:, 0].max() + P[:, 0].min()) / 2, (P[:, 1].max() + P[:, 1].min()) / 2, P[:, 2].min()))
mw = corps.matrix_world.copy()
corps.data.transform(Matrix.Scale(k, 4) @ Matrix.Translation(-decal) @ mw)
corps.matrix_world = Matrix(); corps.name = "Kappa"
P = np.array([v.co[:] for v in corps.data.vertices])

# pieds : orteils = point le plus en avant (-Y) près du sol, de chaque côté
pied = {}
for s in (1, -1):
    bas = P[(P[:, 2] < 0.03) & (P[:, 0] * s > 0.02)]
    bout = bas[bas[:, 1].argmin()]
    pied[s] = (float(bas[:, 0].mean()), float(bout[1]))
print("PIEDS", {s: tuple(round(v, 3) for v in p) for s, p in pied.items()})

OS = [  # nom, tête, queue, parent
    ("Hips", (0, -0.005, 0.235), (0, -0.005, 0.30), None),
    ("Spine", (0, -0.005, 0.30), (0, -0.002, 0.37), "Hips"),
    ("Spine1", (0, -0.002, 0.37), (0, 0.0, 0.46), "Spine"),
    ("Neck", (0, 0.0, 0.46), (0, 0.0, 0.545), "Spine1"),
    ("Head", (0, 0.0, 0.545), (0, 0.0, 0.80), "Neck"),
]
for cote, s in (("Left", 1), ("Right", -1)):   # il regarde vers -Y : sa gauche est en +X
    px, py = pied[s]
    OS += [
        (f"{cote}Shoulder", (s * 0.03, 0.0, 0.465), (s * 0.105, 0.0, 0.47), "Spine1"),
        (f"{cote}Arm", (s * 0.105, 0.0, 0.47), (s * 0.195, -0.008, 0.385), f"{cote}Shoulder"),
        (f"{cote}ForeArm", (s * 0.195, -0.008, 0.385), (s * 0.262, -0.012, 0.312), f"{cote}Arm"),
        (f"{cote}Hand", (s * 0.262, -0.012, 0.312), (s * 0.303, -0.016, 0.264), f"{cote}ForeArm"),
        (f"{cote}UpLeg", (s * 0.062, -0.005, 0.235), (s * 0.064, -0.015, 0.13), "Hips"),   # genou un peu en avant
        (f"{cote}Leg", (s * 0.064, -0.015, 0.13), (s * 0.066, 0.0, 0.05), f"{cote}UpLeg"),
        (f"{cote}Foot", (s * 0.066, 0.0, 0.05), ((s * 0.066 + px) / 2, (py + 0.0) / 2, 0.018), f"{cote}Leg"),
        (f"{cote}ToeBase", ((s * 0.066 + px) / 2, (py + 0.0) / 2, 0.018), (px, py, 0.012), f"{cote}Foot"),
    ]

data = bpy.data.armatures.new("Squelette"); arm = bpy.data.objects.new("Squelette", data)
bpy.context.scene.collection.objects.link(arm)
bpy.context.view_layer.objects.active = arm; bpy.ops.object.mode_set(mode="EDIT")
for nom, t, q, parent in OS:
    b = data.edit_bones.new(nom); b.head, b.tail = t, q
    if parent: b.parent = data.edit_bones[parent]; b.use_connect = (Vector(t) - data.edit_bones[parent].tail).length < 1e-6
bpy.ops.armature.select_all(action="SELECT"); bpy.ops.armature.calculate_roll(type="GLOBAL_POS_Y")
bpy.ops.object.mode_set(mode="OBJECT")

# poids : la chaleur des os échoue sur le maillage de Meshy (surfaces qui se croisent : 0 sommet pesé, 18/11/2026).
# On la calcule sur une copie étanche refaite en voxels de 6 mm, puis chaque sommet du vrai modèle reprend les poids du
# point le plus proche de cette copie (interpolés dans le triangle).
from mathutils.bvhtree import BVHTree
from mathutils.geometry import barycentric_transform
proxy = corps.copy(); proxy.data = corps.data.copy(); proxy.name = "Proxy"; bpy.context.scene.collection.objects.link(proxy)
vox = proxy.modifiers.new("Voxels", "REMESH"); vox.mode = "VOXEL"; vox.voxel_size = 0.006
bpy.context.view_layer.objects.active = proxy; bpy.ops.object.modifier_apply(modifier=vox.name)
bpy.ops.object.select_all(action="DESELECT")
proxy.select_set(True); arm.select_set(True); bpy.context.view_layer.objects.active = arm
bpy.ops.object.parent_set(type="ARMATURE_AUTO")
pm = proxy.data; pm.calc_loop_triangles()
QP = [v.co.copy() for v in pm.vertices]; QT = [tuple(t.vertices) for t in pm.loop_triangles]
noms_p = [g.name for g in proxy.vertex_groups]
QW = [{noms_p[g.group]: g.weight for g in v.groups} for v in pm.vertices]
print("PROXY", len(QP), "sommets ; sans poids", sum(1 for w in QW if not w))
bvh = BVHTree.FromPolygons(QP, QT)
for b in data.bones: corps.vertex_groups.new(name=b.name)
me = corps.data; V = len(me.vertices); idx = {g.name: g.index for g in corps.vertex_groups}
W = np.zeros((V, len(corps.vertex_groups)))
for v in me.vertices:
    loc, _, i, _ = bvh.find_nearest(v.co)
    a_, b_, c_ = QT[i]
    ba = barycentric_transform(loc, QP[a_], QP[b_], QP[c_], Vector((1, 0, 0)), Vector((0, 1, 0)), Vector((0, 0, 1)))
    for q, coef in zip((a_, b_, c_), (ba.x, ba.y, ba.z)):
        for n, w in QW[q].items(): W[v.index, idx[n]] += w * max(coef, 0)
bpy.data.objects.remove(proxy, do_unlink=True)
mod = corps.modifiers.new("Armature", "ARMATURE"); mod.object = arm
corps.parent = arm
vides = int((W.sum(1) == 0).sum())
print("SANS_POIDS", vides, "sur", V)

# carapace rigide : tout ce qui est derrière le corps (y > 0,05) entre 0,22 et 0,52 m suit le haut du dos
car =(P[:, 1] > 0.05) & (P[:, 2] > 0.22) & (P[:, 2] < 0.52)
W[car] = 0; W[car, idx["Spine1"]] = 0.7; W[car, idx["Spine"]] = 0.3
# sommets restés sans poids : os le plus proche (distance au segment)
if vides:
    segs = {b.name: (np.array(b.head_local), np.array(b.tail_local)) for b in data.bones}
    for i in np.nonzero(W.sum(1) == 0)[0]:
        p = P[i]; best, bd = None, 9
        for n, (a, b) in segs.items():
            t = np.clip(np.dot(p - a, b - a) / max(np.dot(b - a, b - a), 1e-9), 0, 1); d = np.linalg.norm(p - (a + t * (b - a)))
            if d < bd: best, bd = n, d
        W[i, idx[best]] = 1
# lissage par les arêtes (comme froggy_p.lisser_poids), sauf la carapace
a = np.array([e.vertices[:] for e in me.edges]); deg = np.bincount(a.ravel(), minlength=V)[:, None]
for _ in range(3):
    S = np.zeros_like(W); np.add.at(S, a[:, 0], W[a[:, 1]]); np.add.at(S, a[:, 1], W[a[:, 0]])
    Wn = 0.5 * W + 0.5 * S / np.maximum(deg, 1); Wn[car] = W[car]; W = Wn
W[W < 0.01] = 0; W /= np.maximum(W.sum(1, keepdims=True), 1e-9)
for gi, g in enumerate(corps.vertex_groups):
    g.remove(range(V))
    for i in np.nonzero(W[:, gi])[0]: g.add([int(i)], float(W[i, gi]), "REPLACE")
for pb in arm.pose.bones: pb.rotation_mode = "XYZ"

os.makedirs(SORTIE, exist_ok=True)
bpy.ops.wm.save_as_mainfile(filepath=os.path.join(SORTIE, "kappa_rig.blend"))
bpy.ops.object.select_all(action="SELECT")
bpy.ops.export_scene.fbx(filepath=os.path.join(SORTIE, "Kappa_Modele.fbx"), object_types={"ARMATURE", "MESH"},
                         add_leaf_bones=False, bake_anim=False, path_mode="COPY", embed_textures=True)

# contrôle : une pose franche, donnée dans les axes du monde (bras levés, coude plié, cuisse en avant, genou plié,
# tête tournée), rendue de face, de profil et de trois quarts
pb = arm.pose.bones


def tourner_monde(nom, axe, degres):
    b = pb[nom]; Rm = Matrix.Rotation(math.radians(degres), 4, axe)
    t = b.matrix.translation.copy(); m = Rm @ b.matrix; m.translation = t; b.matrix = m
    bpy.context.view_layer.update()


tourner_monde("LeftArm", "Y", -60); tourner_monde("RightArm", "Y", 60)        # bras levés sur le côté
tourner_monde("LeftForeArm", "Y", -45)                                          # coude plié
tourner_monde("RightUpLeg", "X", -55); tourner_monde("RightLeg", "X", 80)       # cuisse en avant, genou plié
tourner_monde("Head", "Z", 30)                                                  # tête tournée
sc = bpy.context.scene; sc.render.engine = "BLENDER_WORKBENCH"; sh = sc.display.shading
sh.light = "STUDIO"; sh.color_type = "TEXTURE"; sc.render.resolution_x, sc.render.resolution_y = 600, 700
if sc.world is None: sc.world = bpy.data.worlds.new("Monde")
sc.world.color = (.05, .05, .05)
cam = bpy.data.objects.new("Cam", bpy.data.cameras.new("Cam")); sc.collection.objects.link(cam); sc.camera = cam
cam.data.type = "ORTHO"; cam.data.ortho_scale = 1.0; c = Vector((0, 0, 0.42))
for nom, az, el in (("pose_face", -90, 5), ("pose_profil", 0, 5), ("pose_trois_quarts", -50, 20)):
    aa, e = math.radians(az), math.radians(el)
    cam.location = c + Vector((math.cos(aa) * math.cos(e), math.sin(aa) * math.cos(e), math.sin(e))) * 5
    cam.rotation_euler = (c - cam.location).to_track_quat("-Z", "Y").to_euler()
    sc.render.filepath = os.path.join(SORTIE, nom + ".png"); bpy.ops.render.render(write_still=True)
print("OK", len(OS), "os")
