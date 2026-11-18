"""Froggy kawaii : les yeux suivent la peau de la tête (18 novembre 2026).

froggy_kawaii_corriger.py accrochait les deux yeux (ellipsoïdes noirs) à l'os Head. Mais Meshy a pesé la grosse tête du
kawaii surtout sur le cou : dès qu'une animation tourne la tête (les clips text_to_motion de Meshy le font), les yeux
partent seuls, à côté du visage. Ici, chaque sommet d'un œil reçoit les poids de la peau juste en dessous (point le plus
proche du corps, au repos, poids interpolés sur la face), puis un modificateur Armature : l'œil suit la même
déformation que le visage.

Les FBX d'origine de Meshy n'existent plus : on corrige les exports du projet, en place, avec les mêmes réglages
d'export que froggy_kawaii_corriger.py (une animation par fichier, texture embarquée).
Lancer depuis la racine du projet, Unity hors mode Play :
    FROGGY_ROOT=$PWD /Applications/Blender.app/Contents/MacOS/Blender -b -P Tools/blender/froggy_kawaii_yeux_peau.py
SAUVEGARDE=<dossier> : copie les FBX d'avant dans ce dossier (hors git) avant de les remplacer."""
import bpy, os, shutil
from mathutils import Vector
from mathutils.bvhtree import BVHTree
from mathutils.geometry import barycentric_transform

R = os.environ.get("FROGGY_ROOT", os.getcwd())
DOSSIER = os.environ.get("DOSSIER_KAWAII") or os.path.join(R, "Assets", "Froggy", "Personnages", "FroggyKawaii")
CLIPS = ["Repos", "Marche", "Course", "Saut", "Victoire"]
SAUVEGARDE = os.environ.get("SAUVEGARDE")


def poids_de_la_peau(corps, oeil):
    """Pour chaque sommet de l'œil, les poids du point le plus proche de la peau (interpolés dans le triangle)."""
    me = corps.data; mw = corps.matrix_world
    me.calc_loop_triangles()
    pts = [mw @ v.co for v in me.vertices]
    tris = [tuple(t.vertices) for t in me.loop_triangles]
    bvh = BVHTree.FromPolygons(pts, tris)
    noms = [g.name for g in corps.vertex_groups]
    w = [{g.group: g.weight for g in v.groups} for v in me.vertices]
    res = []
    for v in oeil.data.vertices:
        p = oeil.matrix_world @ v.co
        loc, _, i, _ = bvh.find_nearest(p)
        a, b, c = tris[i]
        # coordonnées barycentriques du point trouvé dans le triangle
        ba = barycentric_transform(loc, pts[a], pts[b], pts[c], Vector((1, 0, 0)), Vector((0, 1, 0)), Vector((0, 0, 1)))
        acc = {}
        for k, coef in zip((a, b, c), (ba.x, ba.y, ba.z)):
            for g, val in w[k].items(): acc[g] = acc.get(g, 0) + val * max(coef, 0)
        tot = sum(acc.values()) or 1
        res.append({noms[g]: val / tot for g, val in acc.items() if val / tot > 1e-3})
    return res


def ecart_yeux_peau(corps, yeux):
    """Distance moyenne (m) entre le centre de chaque œil déformé et la peau déformée la plus proche."""
    dg = bpy.context.evaluated_depsgraph_get()
    ce = corps.evaluated_get(dg); me = ce.to_mesh()
    bvh = BVHTree.FromPolygons([ce.matrix_world @ v.co for v in me.vertices], [tuple(p.vertices) for p in me.polygons])
    d = []
    for o in yeux:
        oe = o.evaluated_get(dg); m2 = oe.to_mesh()
        c = sum((oe.matrix_world @ v.co for v in m2.vertices), Vector()) / len(m2.vertices)
        d.append(bvh.find_nearest(c)[3]); oe.to_mesh_clear()
    ce.to_mesh_clear()
    return d


def controle(arm, corps, yeux):
    """Tourne la tête et le cou de 25° : l'écart œil-peau doit rester celui du repos (l'œil suit la peau)."""
    pb = arm.pose.bones; tete = pb["Head"]; cou = tete.parent
    sauve = {b.name: (b.rotation_mode, b.rotation_quaternion.copy(), b.rotation_euler.copy()) for b in (tete, cou)}
    bpy.context.scene.frame_set(int(arm.animation_data.action.frame_range[0]))
    avant = ecart_yeux_peau(corps, yeux)
    for b in (tete, cou): b.rotation_mode = "XYZ"; b.rotation_euler = (0.44, 0.3, 0.2)
    bpy.context.view_layer.update()
    apres = ecart_yeux_peau(corps, yeux)
    for b in (tete, cou):
        mode, q, e = sauve[b.name]; b.rotation_mode = mode; b.rotation_quaternion = q; b.rotation_euler = e
    print("CONTROLE ecart oeil-peau (m) repos", [round(x, 4) for x in avant], "tete tournee", [round(x, 4) for x in apres])


def corriger(clip):
    chemin = os.path.join(DOSSIER, f"Froggy_K_{clip}.fbx")
    bpy.ops.wm.read_factory_settings(use_empty=True)
    bpy.ops.import_scene.fbx(filepath=chemin, anim_offset=0.0)
    arm = next(o for o in bpy.data.objects if o.type == "ARMATURE")
    corps = next(o for o in bpy.data.objects if o.type == "MESH" and not o.name.startswith("Oeil"))
    yeux = [o for o in bpy.data.objects if o.name.startswith("Oeil")]
    act = arm.animation_data.action
    arm.data.pose_position = "REST"; bpy.context.view_layer.update()
    for oeil in yeux:
        monde = oeil.matrix_world.copy()
        oeil.parent = None; oeil.matrix_world = monde
        bpy.context.view_layer.objects.active = oeil
        for o in bpy.data.objects: o.select_set(o == oeil)
        bpy.ops.object.transform_apply(location=True, rotation=True, scale=True)
        poids = poids_de_la_peau(corps, oeil)
        for gname in {n for p in poids for n in p}:
            g = oeil.vertex_groups.new(name=gname)
            for i, p in enumerate(poids):
                if gname in p: g.add([i], p[gname], "REPLACE")
        mod = oeil.modifiers.new("Armature", "ARMATURE"); mod.object = arm
        oeil.parent = arm; oeil.matrix_parent_inverse = arm.matrix_world.inverted()
    arm.data.pose_position = "POSE"; bpy.context.view_layer.update()
    controle(arm, corps, yeux)
    lo, hi = (int(v) for v in act.frame_range)
    bpy.context.scene.frame_start, bpy.context.scene.frame_end = lo, hi
    if SAUVEGARDE:
        os.makedirs(SAUVEGARDE, exist_ok=True); shutil.copy2(chemin, os.path.join(SAUVEGARDE, os.path.basename(chemin)))
    bpy.ops.object.select_all(action="DESELECT")
    bpy.ops.export_scene.fbx(filepath=chemin, object_types={"ARMATURE", "MESH"}, add_leaf_bones=False,
                             bake_anim=True, bake_anim_use_all_actions=False, bake_anim_use_nla_strips=False,
                             path_mode="COPY", embed_textures=True)
    print("CLIP", clip, "yeux", [(o.name, len(o.vertex_groups)) for o in yeux], "images", lo, hi)


for clip in CLIPS:
    corriger(clip)
