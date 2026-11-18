"""Froggy kawaii : répare les yeux du modèle Meshy et exporte un FBX par animation, prêt pour Unity.

Meshy a bavé le noir des yeux sur les côtés des bosses (vues de trois quarts). Le script :
1. repère les texels des yeux par leur position 3D (la bouche est au centre, |x| < 0,18 m ; les yeux au-delà de 0,22 m),
   les repeint par diffusion du vert voisin, et ramène le vert vers celui de la planche validée (saturation × 0,68) ;
2. remplace les yeux peints par deux ellipsoïdes noirs mesurés sur les texels vus de face, attachés à l'os Head ;
3. fabrique le repos (bras baissés, respiration légère, boucle de 4 s) ;
4. exporte Froggy_K_<Clip>.fbx (modèle, squelette, texture embarquée, une seule animation).
Les FBX d'origine et la texture d'origine ne sont jamais modifiés.

blender -b --factory-startup --python froggy_kawaii_corriger.py -- <dossier 5_animations>
(le dossier contient originaux/froggy-k-{arme,marche-original,course-original,saut-original,victoire-original}.fbx)
"""
import bpy, sys, json, math
import numpy as np
from pathlib import Path
from mathutils import Vector, Matrix, Quaternion

racine = Path(sys.argv[sys.argv.index("--") + 1])
originaux, exports = racine / "originaux", racine / "exports"
exports.mkdir(exist_ok=True)
(racine / "textures").mkdir(exist_ok=True)

CLIPS = [("Repos", "froggy-k-arme"), ("Marche", "froggy-k-marche-original"), ("Course", "froggy-k-course-original"),
         ("Saut", "froggy-k-saut-original"), ("Victoire", "froggy-k-victoire-original")]
X_BOUCHE, X_YEUX, Z_YEUX = 0.18, 0.20, 1.2   # mesurés sur le modèle à 1,70 m (histogrammes des texels sombres)
SATURATION = 0.68                              # vert de la planche ~#B8F088 contre ~#A0F858 dans la texture Meshy


def importer(nom):
    bpy.ops.wm.read_factory_settings(use_empty=True)
    bpy.ops.import_scene.fbx(filepath=str(originaux / f"{nom}.fbx"), anim_offset=0.0)
    for o in list(bpy.context.scene.objects):
        if o.type == "MESH" and "icosphere" in o.name.lower():
            bpy.data.objects.remove(o)
    arm = next(o for o in bpy.context.scene.objects if o.type == "ARMATURE")
    corps = next(o for o in bpy.context.scene.objects if o.type == "MESH")
    return arm, corps


def image_du(corps):
    return next(n.image for m in corps.data.materials for n in m.node_tree.nodes if n.type == "TEX_IMAGE")


def carte_texels(corps, arm):
    """Position 3D et normale au repos de chaque texel couvert par une face."""
    arm.data.pose_position = "REST"; bpy.context.view_layer.update()
    e = corps.evaluated_get(bpy.context.evaluated_depsgraph_get()); me = e.to_mesh(); me.calc_loop_triangles()
    W, H = image_du(corps).size
    mw = np.array(e.matrix_world)
    co = np.array([mw[:3, :3] @ v.co + mw[:3, 3] for v in me.vertices])
    nr = np.array([mw[:3, :3] @ v.normal for v in me.vertices]); nr /= np.linalg.norm(nr, axis=1)[:, None]
    uvl = me.uv_layers.active.data
    pos = np.full((H, W, 3), np.nan, np.float32); nor = np.zeros((H, W, 3), np.float32)
    for t in me.loop_triangles:
        uv = np.array([uvl[l].uv for l in t.loops]) * [W, H]
        x0, y0 = np.floor(uv.min(0)).astype(int); x1, y1 = np.ceil(uv.max(0)).astype(int)
        xs, ys = np.meshgrid(np.arange(max(x0, 0), min(x1 + 1, W)), np.arange(max(y0, 0), min(y1 + 1, H)))
        p = np.stack([xs + 0.5, ys + 0.5], -1).reshape(-1, 2)
        a, b, c = uv; d = (b[1] - c[1]) * (a[0] - c[0]) + (c[0] - b[0]) * (a[1] - c[1])
        if abs(d) < 1e-12: continue
        l1 = ((b[1] - c[1]) * (p[:, 0] - c[0]) + (c[0] - b[0]) * (p[:, 1] - c[1])) / d
        l2 = ((c[1] - a[1]) * (p[:, 0] - c[0]) + (a[0] - c[0]) * (p[:, 1] - c[1])) / d
        ok = (l1 >= -0.01) & (l2 >= -0.01) & (1 - l1 - l2 >= -0.01)
        if not ok.any(): continue
        L = np.stack([l1, l2, 1 - l1 - l2], -1)[ok]; q = p[ok].astype(int); vi = list(t.vertices)
        pos[q[:, 1], q[:, 0]] = L @ co[vi]; nor[q[:, 1], q[:, 0]] = L @ nr[vi]
    e.to_mesh_clear()
    arm.data.pose_position = "POSE"; bpy.context.view_layer.update()
    return pos, nor


def corriger_texture(corps, arm):
    img = image_du(corps); W, H = img.size
    px = np.array(img.pixels[:], np.float32).reshape(H, W, 4)
    pos, nor = carte_texels(corps, arm)
    couvert = ~np.isnan(pos[..., 0])
    lum = px[..., :3] @ np.array([0.3, 0.59, 0.11], np.float32)
    zone = couvert & (np.abs(np.nan_to_num(pos[..., 0])) > X_YEUX) & (np.nan_to_num(pos[..., 2]) > Z_YEUX)
    oeil = zone & (lum < 0.55)
    for _ in range(3):  # bord antialiasé
        v = oeil.copy()
        for dy, dx in ((1, 0), (-1, 0), (0, 1), (0, -1)): v |= np.roll(np.roll(oeil, dy, 0), dx, 1)
        oeil = v & zone

    # Forme des yeux : texels sombres vus de face (normale vers -Y), un œil par côté
    face = oeil & (lum < 0.25) & (nor[..., 1] < -0.55)
    yeux = []
    for signe in (-1, 1):
        m = face & (np.sign(pos[..., 0]) == signe)
        P, N = pos[m], nor[m]
        n = N.mean(0); n /= np.linalg.norm(n)
        haut = np.array([0, 0, 1.0]) - n * n[2]; haut /= np.linalg.norm(haut); cote = np.cross(haut, n)
        # étendue mesurée dans le plan de la peau (l'œil est incliné vers le haut, ses extents en x et z sont écrasés)
        yeux.append({"centre": P.mean(0).tolist(), "normale": n.tolist(),
                     "largeur": float(np.ptp(P @ cote)), "hauteur": float(np.ptp(P @ haut)), "texels": int(m.sum())})

    # Repeindre : diffusion du vert connu vers les texels des yeux
    connu = couvert & ~oeil
    rgb = px[..., :3].copy(); rgb[~connu] = 0
    reste = oeil.copy()
    for _ in range(400):
        if not reste.any(): break
        s = np.zeros_like(rgb); k = np.zeros(connu.shape, np.float32)
        for dy in (-1, 0, 1):
            for dx in (-1, 0, 1):
                if dy or dx:
                    s += np.roll(np.roll(rgb * connu[..., None], dy, 0), dx, 1)
                    k += np.roll(np.roll(connu, dy, 0), dx, 1)
        neuf = reste & (k > 0)
        rgb[neuf] = s[neuf] / k[neuf][:, None]; connu |= neuf; reste &= ~neuf
    px[..., :3] = np.where(oeil[..., None], rgb, px[..., :3])

    # Vert ramené vers la planche : saturation réduite sur les teintes vertes seulement
    r, g, b = px[..., 0], px[..., 1], px[..., 2]
    mx, mn = px[..., :3].max(-1), px[..., :3].min(-1); s = np.where(mx > 0, (mx - mn) / np.maximum(mx, 1e-6), 0)
    teinte = np.degrees(np.arctan2(np.sqrt(3) * (g - b), 2 * r - g - b)) % 360
    vert = (teinte > 70) & (teinte < 150) & (s > 0.3)
    nouveau_mn = mx - (mx - mn) * SATURATION
    for c in range(3):
        canal = px[..., c]
        px[..., c] = np.where(vert, nouveau_mn + (canal - mn) / np.maximum(mx - mn, 1e-6) * (mx - nouveau_mn), canal)

    neuve = bpy.data.images.new("froggy-k_base_color_corrige", W, H, alpha=True)
    neuve.pixels.foreach_set(px.ravel())
    neuve.filepath_raw = str(racine / "textures" / "froggy-k_base_color_corrige.png"); neuve.file_format = "PNG"
    neuve.save()
    return neuve.filepath_raw, yeux, int(oeil.sum())


def poser_texture(corps, chemin):
    img = bpy.data.images.load(chemin)
    for m in corps.data.materials:
        for n in m.node_tree.nodes:
            if n.type == "TEX_IMAGE": n.image = img


def ajouter_yeux(arm, yeux):
    noir = bpy.data.materials.new("Froggy_K_Yeux"); noir.use_nodes = True
    bsdf = noir.node_tree.nodes["Principled BSDF"]
    bsdf.inputs["Base Color"].default_value = (0.01, 0.01, 0.012, 1); bsdf.inputs["Roughness"].default_value = 0.15
    arm.data.pose_position = "REST"; bpy.context.view_layer.update()
    for nom, oeil in zip(("Oeil_D", "Oeil_G"), yeux):  # x < 0 : œil droit du personnage (il regarde vers -Y)
        bpy.ops.mesh.primitive_uv_sphere_add(segments=32, ring_count=16, radius=1)
        o = bpy.context.active_object; o.name = nom; o.data.materials.append(noir)
        bpy.ops.object.shade_smooth()
        n = Vector(oeil["normale"]); haut = (Vector((0, 0, 1)) - n * n.z).normalized(); cote = haut.cross(n)
        demi_l, demi_h = oeil["largeur"] / 2, oeil["hauteur"] / 2
        epaisseur = min(demi_l, demi_h) * 0.45
        rot = Matrix((cote, haut, n)).transposed().to_4x4()
        centre = Vector(oeil["centre"]) - n * epaisseur * 0.35  # à moitié enfoncé : l'œil affleure en léger relief
        o.matrix_world = Matrix.Translation(centre) @ rot @ Matrix.Diagonal((demi_l, demi_h, epaisseur, 1))
        bpy.ops.object.transform_apply(location=False, rotation=True, scale=True)
        monde = o.matrix_world.copy()
        o.parent = arm; o.parent_type = "BONE"; o.parent_bone = "Head"
        bpy.context.view_layer.update(); o.matrix_world = monde
    bpy.context.view_layer.update()
    arm.data.pose_position = "POSE"; bpy.context.view_layer.update()


def faire_repos(arm):
    """Repos gratuit : bras baissés de 50° et ramenés de 25° vers l'avant, respiration et tête qui accompagne, boucle de 4 s à 30 i/s."""
    sc = bpy.context.scene; sc.render.fps = 30; sc.frame_start, sc.frame_end = 1, 121
    act = bpy.data.actions.new("Repos"); arm.animation_data_create(); arm.animation_data.action = act
    pb = arm.pose.bones
    for b in pb: b.rotation_mode = "QUATERNION"; b.rotation_quaternion = Quaternion()
    bpy.context.view_layer.update()

    def tourner_monde(b, axe, angle):
        R = (arm.matrix_world.to_3x3().inverted() @ Matrix.Rotation(angle, 3, axe) @ arm.matrix_world.to_3x3()).to_4x4()
        t = b.matrix.translation.copy(); m = R @ b.matrix; m.translation = t; b.matrix = m
        bpy.context.view_layer.update()

    for f, souffle in ((1, 0.0), (61, 1.0), (121, 0.0)):
        for b in pb: b.rotation_quaternion = Quaternion()
        bpy.context.view_layer.update()
        tourner_monde(pb["LeftArm"], Vector((0, 1, 0)), math.radians(50 - 3 * souffle))
        tourner_monde(pb["RightArm"], Vector((0, 1, 0)), math.radians(-(50 - 3 * souffle)))
        for cote in ("LeftArm", "RightArm"):  # le rig Meshy tire les bras vers l'arrière : mains dans le dos sans ça
            tourner_monde(pb[cote], Vector((1, 0, 0)), math.radians(-25))
        tourner_monde(pb["Spine01"], Vector((1, 0, 0)), math.radians(-1.5 * souffle))
        tourner_monde(pb["Head"], Vector((1, 0, 0)), math.radians(1.0 * souffle))
        for b in pb: b.keyframe_insert("rotation_quaternion", frame=f)
    return act


def exporter(arm, corps, clip):
    act = arm.animation_data.action
    act.name = clip
    lo, hi = (int(v) for v in act.frame_range)
    bpy.context.scene.frame_start, bpy.context.scene.frame_end = lo, hi
    chemin = exports / f"Froggy_K_{clip}.fbx"
    bpy.ops.object.select_all(action="DESELECT")
    bpy.ops.export_scene.fbx(filepath=str(chemin), object_types={"ARMATURE", "MESH"}, add_leaf_bones=False,
                             bake_anim=True, bake_anim_use_all_actions=False, bake_anim_use_nla_strips=False,
                             path_mode="COPY", embed_textures=True)
    return {"clip": clip, "fbx": str(chemin), "images": [lo, hi], "secondes": (hi - lo) / bpy.context.scene.render.fps}


rapport = {"clips": []}
arm, corps = importer("froggy-k-arme")
texture, yeux, n_oeil = corriger_texture(corps, arm)
rapport.update(texture=texture, yeux=yeux, texels_repeints=n_oeil)
print("YEUX", json.dumps(yeux))
for clip, nom in CLIPS:
    arm, corps = importer(nom)
    poser_texture(corps, texture)
    ajouter_yeux(arm, yeux)
    if clip == "Repos": faire_repos(arm)
    rapport["clips"].append(exporter(arm, corps, clip))
    bpy.ops.wm.save_as_mainfile(filepath=str(racine / f"Froggy_K_{clip}.blend"))
(racine / "rapport-correction.json").write_text(json.dumps(rapport, ensure_ascii=False, indent=2))
print("RAPPORT", json.dumps(rapport, ensure_ascii=False))
