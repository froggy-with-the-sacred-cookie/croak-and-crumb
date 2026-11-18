"""Prépare un mouvement Meshy (text_to_motion, qualité prime) pour Unity : garde le squelette et le mouvement, retire le
mannequin de Meshy (16 Mo -> 0,4 Mo), et exporte la plage exacte de l'action (sinon Blender exporte toute la scène :
un clip de 2,5 s sortait à 8,3 s). Le squelette SMPL-H de Meshy (Pelvis, L_Hip, L_Collar, L_Shoulder…) s'importe
directement en humanoïde dans Unity (vérifié le 18/11/2026 : L_Collar = épaule, L_Shoulder = haut du bras), et le
clip se rejoue sur Froggy B, kawaii et P par le retargeting humanoïde.
Lancer depuis la racine du projet :
    SRC=Captures/animations_v1/brut/X_mouvement.fbx DST=Captures/animations_v1/propre/Froggy_Anim_X.fbx \\
    /Applications/Blender.app/Contents/MacOS/Blender -b -P Tools/blender/mouvement_meshy_pour_unity.py
puis copier le résultat dans Assets/Froggy/Personnages/Animations/ (import réglé par ImportPersonnagesFroggy)."""
import bpy, os
R = os.environ.get("FROGGY_ROOT", os.getcwd())
src, dst = (os.path.join(R, os.environ[k]) for k in ("SRC", "DST"))
for ob in list(bpy.data.objects): bpy.data.objects.remove(ob, do_unlink=True)
bpy.ops.import_scene.fbx(filepath=src)
for o in list(bpy.data.objects):
    if o.type != "ARMATURE": bpy.data.objects.remove(o, do_unlink=True)
arm = [o for o in bpy.data.objects if o.type == "ARMATURE"]
print("ARM", [(a.name, len(a.data.bones), a.animation_data.action.name if a.animation_data and a.animation_data.action else None) for a in arm])
act = arm[0].animation_data.action
sc = bpy.context.scene; sc.frame_start, sc.frame_end = int(act.frame_range[0]), int(act.frame_range[1])   # sinon toute la plage de la scène part
print("PLAGE", sc.frame_start, sc.frame_end, sc.render.fps)
bpy.ops.object.select_all(action="DESELECT")
for a in arm: a.select_set(True)
bpy.ops.export_scene.fbx(filepath=dst, use_selection=True, object_types={"ARMATURE"}, add_leaf_bones=False,
                         bake_anim=True, bake_anim_use_all_actions=False, bake_anim_use_nla_strips=False, apply_scale_options="FBX_SCALE_ALL")
print("OUT", os.path.getsize(dst))
