"""Kit de sakura indépendant, gratuit, proposition visuelle du 29 octobre 2026.

NC State : Somei-Yoshino rose pâle, fleurs simples qui pâlissent vers le blanc.
La silhouette des pétales échancrés vient de la fiche botanique et de ses photos.
La chute (0,5–0,75 m/s), l'échelle et les rotations sont des choix artistiques,
pas des mesures biologiques. La démonstration est précalculée ; le vent du jeu
doit rester piloté par VentJardin pour une utilisation en particules.

FROGGY_ROOT=<projet> Blender -b --factory-startup -P Tools/blender/sakura_voltige.py
SAKURA_PREVIEW=1 ajoute 96 images d'aperçu ; les sources .blend restent dans Captures/.
Ne touche ni à la scène Blender ouverte ni aux scènes/préfabriqués Unity existants.
"""
import os
import json
import math
import random
from pathlib import Path

import bpy
from mathutils import Vector

ROOT = Path(os.environ.get("FROGGY_ROOT", os.getcwd()))
exec((ROOT / "Tools/blender/blender_lib.py").read_text())
OUT = ROOT / "Assets/Froggy/Effets/Sakura"
PREVIEW = ROOT / "Captures/sakura_voltige"
OUT.mkdir(parents=True, exist_ok=True)
PREVIEW.mkdir(parents=True, exist_ok=True)
FPS, DURATION = 24, 8
END = FPS * DURATION + 1
TAU = math.tau

bpy.ops.object.select_all(action="SELECT")
bpy.ops.object.delete(use_global=False)
scene = bpy.context.scene
scene.render.fps = FPS
scene.frame_start, scene.frame_end = 1, END
scene.unit_settings.system = "METRIC"
scene.unit_settings.scale_length = 1.0


def linear(c):
    return c / 12.92 if c < .04045 else ((c + .055) / 1.055) ** 2.4


def rgba(rgb):
    return tuple(linear(v / 255) for v in rgb) + (1.,)


# Texture neutre : rose au départ, ivoire au bord, nervure centrale très douce.
image = bpy.data.images.new("Sakura_RoseIvoire", width=128, height=128, alpha=True)
pixels = []
for y in range(128):
    t = y / 127
    for x in range(128):
        u = x / 127
        mix = min(1., .15 + .85 * t ** .65)
        color = [a + (b-a) * mix for a, b in zip((219, 100, 141), (255, 229, 231))]
        vein = math.exp(-((u - .5) / .025) ** 2) * (1-t) * 9
        color = [color[0], color[1]-vein, color[2]-vein*.45]
        pixels.extend(tuple(c / 255 for c in color) + (1.,))
image.pixels = pixels
image.filepath_raw = str(OUT / "Sakura_RoseIvoire.png")
image.file_format = "PNG"
image.save()

petal_mat = bpy.data.materials.new("Sakura_RoseIvoire")
petal_mat.use_nodes = True
bsdf = next(n for n in petal_mat.node_tree.nodes if n.type == "BSDF_PRINCIPLED")
bsdf.inputs["Roughness"].default_value = .76
tex = petal_mat.node_tree.nodes.new("ShaderNodeTexImage")
tex.image = image
petal_mat.node_tree.links.new(tex.outputs["Color"], bsdf.inputs["Base Color"])
petal_mat.diffuse_color = rgba((252, 192, 208))


def mesh_object(name, verts, faces, uv, colors=None):
    mesh = bpy.data.meshes.new(name)
    mesh.from_pydata(verts, [], faces)
    mesh.update()
    for poly in mesh.polygons:
        poly.use_smooth = True
    layer = mesh.uv_layers.new(name="UVMap")
    col = mesh.color_attributes.new(name="Col", type="BYTE_COLOR", domain="CORNER")
    for loop in mesh.loops:
        u, v = uv[loop.vertex_index]
        layer.data[loop.index].uv = (u, v)
        m = min(1., .15 + .85 * v ** .65)
        c = tuple((a+(b-a)*m)/255 for a, b in zip((219,100,141),(255,229,231)))
        col.data[loop.index].color_srgb = colors[loop.vertex_index] if colors else c + (1.,)
    mesh.color_attributes.active_color_name = "Col"
    ob = bpy.data.objects.new(name, mesh)
    scene.collection.objects.link(ob)
    ob.data.materials.append(petal_mat)
    return ob


def petal(name, curl, twist, width):
    # Cinq rails : lobes arrondis et encoche peu profonde au bout.
    rows = [(0., .018), (.12, .13), (.30, .245), (.51, .30), (.72, .30), (.87, .245), (1., .145)]
    verts, uv, faces = [], [], []
    for j, (t, w) in enumerate(rows):
        for side in (-1, -.5, 0, .5, 1):
            ty = t
            if j == len(rows)-1:
                ty -= .085 if side == 0 else .065 if abs(side) == 1 else 0
            x = side * w * width + twist * (t-.35) ** 2
            z = curl * (t-.28) ** 2 + .075 * side*side * math.sin(t*math.pi)
            z += twist * side * t * .22
            verts.append((x, ty-.49, z-.04))
            uv.append((.5+side*.49, ty))
    for j in range(len(rows)-1):
        for i in range(4):
            a = j*5+i
            faces.extend([(a, a+1, a+6), (a, a+6, a+5)])
    # Géométrie double face : aucun pétale invisible en se retournant.
    count = len(verts)
    verts += verts[:]
    uv += uv[:]
    faces += [tuple(count+i for i in reversed(f)) for f in faces[:]]
    return mesh_object(name, verts, faces, uv)


petals = [petal("Petale_Sakura_Ouvert", .17, -.12, 1.),
          petal("Petale_Sakura_Courbe", .53, .20, .89),
          petal("Petale_Sakura_Torsade", .30, -.43, .78)]


models = petals
manifest = {"statut":"proposition, validation visuelle en attente", "cout_credits":0,
            "duree_boucle_s":DURATION, "fps":FPS, "graine":291026,
            "longueur_maillage_particule_m":1, "maillages":{}}


def export_fbx(objects, path, animated=False):
    bpy.ops.object.select_all(action="DESELECT")
    for ob in objects:
        ob.select_set(True)
    bpy.context.view_layer.objects.active = objects[0]
    bpy.ops.export_scene.fbx(filepath=str(path), use_selection=True,
        object_types={"MESH","EMPTY"}, axis_forward="-Z", axis_up="Y",
        apply_scale_options="FBX_SCALE_UNITS", mesh_smooth_type="FACE", colors_type="SRGB",
        add_leaf_bones=False, bake_anim=animated, bake_anim_use_all_actions=False,
        bake_anim_use_nla_strips=False, bake_anim_simplify_factor=0., path_mode="RELATIVE")


for ob in models:
    export_fbx([ob], OUT / (ob.name+".fbx"))
    tris = sum(len(p.vertices)-2 for p in ob.data.polygons)
    manifest["maillages"][ob.name] = {"triangles_double_face":tris, "sommets":len(ob.data.vertices)}

# Planche de contrôle : les trois variantes, angles permettant de voir les plis.
for ob, x in zip(models, (-1.4, 0., 1.4)):
    ob.location = (x, 0, .1)
    ob.rotation_euler = (.10, -.20, -.1)
scene.render.engine = "BLENDER_WORKBENCH"
scene.render.resolution_x, scene.render.resolution_y = 1400, 650
scene.render.resolution_percentage = 100
shade = scene.display.shading
shade.light = "STUDIO"
shade.color_type = "VERTEX"
shade.show_shadows = False
shade.show_cavity = False
shade.show_backface_culling = True
shade.show_specular_highlight = False
shade.cavity_type = "BOTH"
shade.curvature_ridge_factor = 1.15
shade.curvature_valley_factor = .7
shade.show_object_outline = False
shade.background_type = "WORLD"
scene.world.color = (.075, .11, .12)
scene.view_settings.view_transform = "Standard"
camdata = bpy.data.cameras.new("Camera_Sakura")
camera = bpy.data.objects.new("Camera_Sakura", camdata)
scene.collection.objects.link(camera)
scene.camera = camera
camdata.type = "ORTHO"
camdata.ortho_scale = 5.
camera.location = (0,-3.5,7)
camera.rotation_euler = (Vector((0,0,0))-camera.location).to_track_quat("-Z","Y").to_euler()
scene.render.filepath = str(PREVIEW / "Planche_Sakura.png")
bpy.ops.render.render(write_still=True)

for ob in models:
    ob.hide_render = True
    ob.hide_viewport = True
    ob.location = (0,0,0)
    ob.rotation_euler = (0,0,0)

root = bpy.data.objects.new("Sakura_Voltige_Boucle_8s", None)
scene.collection.objects.link(root)
rng = random.Random(291026)
animated = [root]
params = []
for i in range(48):
    template = petals[i%3]
    # L'exporteur FBX de Blender 5.2 avertit et perd des slots si les données sont
    # partagées ici ; une copie par instance garde les matériaux à l'export.
    ob = bpy.data.objects.new(f"Petale_Volant_{i:02}", template.data.copy())
    scene.collection.objects.link(ob)
    ob.parent = root
    phase = rng.random()
    x, y = rng.uniform(-2.,2.), rng.uniform(-.8,.8)
    speed = rng.uniform(.5,.75)
    size = rng.uniform(.075,.115)
    lean = rng.uniform(-.7,.7)
    spin = [rng.choice((-2,-1,1,2)) for _ in range(3)]
    params.append((phase,x,y,speed,size,lean,spin))
    for f in range(1,END+1):
        t = (f-1)/FPS
        life = (t/DURATION+phase)%1
        theta = TAU*t/DURATION+TAU*phase
        # Descente + dérive + petites variations ; chaque instance a sa propre phase.
        ob.location = (x+.38*math.sin(theta)+.12*math.sin(theta*3+lean),
                       y+.23*math.sin(theta*2+lean),
                       4.6-speed*DURATION*life+.09*math.sin(theta*4+lean))
        ob.rotation_euler = (lean+.5*math.sin(theta*3)+spin[0]*TAU*t/DURATION,
                            .4*math.cos(theta*2)+spin[1]*TAU*t/DURATION,
                            TAU*phase+spin[2]*TAU*t/DURATION)
        # Sortie et réapparition à échelle nulle : pas de saut visible au bouclage.
        fade = min(1., life/.07, (1-life)/.09)
        fade = max(0., fade)
        fade = fade*fade*(3-2*fade)
        ob.scale = (size*fade,)*3
        ob.keyframe_insert(data_path="location", frame=f)
        ob.keyframe_insert(data_path="rotation_euler", frame=f)
        ob.keyframe_insert(data_path="scale", frame=f)
    animated.append(ob)

scene.frame_set(1)
export_fbx(animated, OUT / "Sakura_Voltige_Boucle_8s.fbx", True)

# Vérification à la source : poids, mouvement, reprise exacte et chute signée.
opening = {o.name:o.matrix_local.copy() for o in animated[1:]}
scene.frame_set(END)
closure = max(abs(o.matrix_local[r][c]-opening[o.name][r][c])
              for o in animated[1:] for r in range(4) for c in range(4))
assert closure < 1e-5, closure
scene.frame_set(25)
moved = sum((o.matrix_local.translation-opening[o.name].translation).length > .05 for o in animated[1:])
assert moved == 48, moved
manifest["verification"] = {"instances":48,"fleurs":0,"petales":48,
    "ecart_bouclage_max":closure,"instances_deplacees_apres_1s":moved,
    "triangles_scene":sum(sum(len(p.vertices)-2 for p in o.data.polygons) for o in animated[1:]),
    "chute_artistique_m_s":[.5,.75],"aucune_validation_Unity":True}
(OUT / "Sakura_Manifest.json").write_text(json.dumps(manifest,ensure_ascii=False,indent=2)+"\n")

scene.render.resolution_x, scene.render.resolution_y = 1000, 850
camdata.ortho_scale = 6.3
camera.location = (6,-12,7)
camera.rotation_euler = (Vector((0,0,2.2))-camera.location).to_track_quat("-Z","Y").to_euler()
scene.frame_set(48)
scene.render.filepath = str(PREVIEW / "Voltige_Sakura.png")
bpy.ops.render.render(write_still=True)
scene.frame_end = END-1
scene.frame_set(1)
bpy.ops.wm.save_as_mainfile(filepath=str(PREVIEW / "Sakura_Voltige_Source.blend"))

if os.environ.get("SAKURA_PREVIEW") == "1":
    scene.render.resolution_x, scene.render.resolution_y = 800, 680
    for frame in range(1,END,2):
        scene.frame_set(frame)
        scene.render.filepath = str(PREVIEW / "frames" / f"{(frame-1)//2:04}.png")
        bpy.ops.render.render(write_still=True)
print("SAKURA_OK " + json.dumps(manifest["verification"]))
