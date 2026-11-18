"""Quatre vues d'un modèle du kit, sans contour, pour la reconstruction par Meshy (multi_image_to_3d, élément 0 = la face).
Le contour (coque retournée peinte en TRAIT) est retiré : sinon il se cuit dans la texture de Meshy (skill route-3d-meshy).
Vues orthographiques à la même échelle, couleurs de sommets telles quelles (lumière douce déjà peinte), fond clair uni.
Lancer depuis la racine du projet (sorties hors git) :
    FROGGY_ROOT=$PWD MODELE=Assets/Froggy/Models/JardinJaponais/Lanterne_Kasuga.fbx \\
    SORTIE=Captures/jardin_meshy/Lanterne_Kasuga/3_planche \\
    /Applications/Blender.app/Contents/MacOS/Blender -b -P Tools/blender/vues_pour_meshy.py"""
import bpy, bmesh, math, os
from mathutils import Vector

RACINE = os.environ.get("FROGGY_ROOT", os.getcwd())
MODELE = os.path.join(RACINE, os.environ["MODELE"])
SORTIE = os.path.join(RACINE, os.environ["SORTIE"])
TRAIT = (40 / 255, 58 / 255, 44 / 255)          # couleur du contour du kit (jardin_japonais.py)
VUES = [("1_face", -90, 0), ("2_droite", 0, 0), ("3_dos", 90, 0), ("4_trois_quarts", -45, 28)]

for ob in list(bpy.data.objects): bpy.data.objects.remove(ob, do_unlink=True)
bpy.ops.import_scene.fbx(filepath=MODELE)
maillages = [o for o in bpy.data.objects if o.type == "MESH"]

retirees = 0
for ob in maillages:                              # retire les faces du contour
    me = ob.data; col = me.color_attributes.active_color or me.color_attributes[0]
    srgb = hasattr(col.data[0], "color_srgb")
    bm = bmesh.new(); bm.from_mesh(me)
    lay = (bm.loops.layers.color.get(col.name) if col.data_type == "BYTE_COLOR" else bm.loops.layers.float_color.get(col.name))
    a_retirer = []
    for f in bm.faces:
        c = f.loops[0][lay]
        if col.data_type != "BYTE_COLOR" or not srgb:   # couleur linéaire : la repasser en sRGB pour comparer
            c = [((v * 12.92) if v <= .0031308 else (1.055 * v ** (1 / 2.4) - .055)) for v in c[:3]]
        if all(abs(c[i] - TRAIT[i]) < .03 for i in range(3)): a_retirer.append(f)
    bmesh.ops.delete(bm, geom=a_retirer, context="FACES"); retirees += len(a_retirer)
    bm.to_mesh(me); bm.free()
print("faces de contour retirées :", retirees)

pts = [o.matrix_world @ Vector(c) for o in maillages for c in o.bound_box]
lo = Vector([min(p[i] for p in pts) for i in range(3)]); hi = Vector([max(p[i] for p in pts) for i in range(3)])
centre = (lo + hi) / 2; taille = max(hi - lo)
print("dimensions (m) :", tuple(round(v, 3) for v in hi - lo))

sc = bpy.context.scene; sc.render.engine = "BLENDER_WORKBENCH"
sh = sc.display.shading; sh.light = "FLAT"; sh.color_type = "VERTEX"; sh.show_backface_culling = True
if sc.world is None: sc.world = bpy.data.worlds.new("Monde")
sc.world.color = (.92, .92, .92)
sc.render.resolution_x = sc.render.resolution_y = 1024; sc.render.film_transparent = False
cam = bpy.data.objects.new("Camera", bpy.data.cameras.new("Camera")); sc.collection.objects.link(cam)
cam.data.type = "ORTHO"; cam.data.ortho_scale = taille * 1.15; cam.data.clip_end = 500; sc.camera = cam
os.makedirs(SORTIE, exist_ok=True)
for nom, az, el in VUES:
    a, e = math.radians(az), math.radians(el)
    cam.location = centre + Vector((math.cos(a) * math.cos(e), math.sin(a) * math.cos(e), math.sin(e))) * (taille * 10)
    cam.rotation_euler = (centre - cam.location).to_track_quat("-Z", "Y").to_euler()
    sc.render.filepath = os.path.join(SORTIE, nom + ".png"); bpy.ops.render.render(write_still=True)
