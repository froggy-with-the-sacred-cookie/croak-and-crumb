"""Kappa, le gobelin du cookie sacré (#78) : ses animations, faites à la main par script (18 novembre 2026).

Les mouvements text_to_motion de Meshy, rejoués sur le kappa, n'étaient « pas géniaux » (Florent). On reprend la méthode
de Froggy P, qu'il a validée : le moteur de froggy_p_animations.py (canaux, poses tenues, ressorts, pieds en IK) est
exécuté tel quel, et seuls les clips sont propres au kappa. Le squelette (kappa_squelette.py) porte les mêmes noms
d'os que Froggy P et regarde aussi vers -Y.
Règles reprises de la fiche Docs/Recherches/froggy-animations-v1-2026-11-18.md : le corps et la tête d'abord, les bras
ensuite ; poses tenues ; la tête suit le corps par un ressort.
Clips (Notion, P5-08 : apparaît, fuit, saute, attrapé ; le saut n'est pas demandé pour l'instant) :
  Course     fuir en boucle, cookie serré contre la poitrine, un regard par-dessus l'épaule par boucle ;
  Attrape    sursaut, se débat, abandonne, croise les bras et boude ;
  Apparition tassé au ras du sol, jaillit, regarde à gauche et à droite, air narquois, part en course.
Lancer depuis la racine du projet (après kappa_squelette.py) :
    FROGGY_ROOT=$PWD /Applications/Blender.app/Contents/MacOS/Blender -b -P Tools/blender/kappa_animations.py
Sorties : Captures/gobelin/7_animations/ (Kappa_<Clip>.fbx, squelette et maillage ; planches de contrôle).
Dans Unity : Assets/Froggy/Gobelin/, import GENERIC (Kappa_Modele crée l'avatar, les clips le recopient, Course en
boucle). En humanoïde, Unity écrasait les postures tenues (penché, cookie serré, bras croisés) : vérifié le 18/11/2026."""
import bpy, bmesh, math, os
import numpy as np
from mathutils import Vector, Matrix

R = os.environ.get("FROGGY_ROOT", os.getcwd())
SORTIE = os.path.join(R, "Captures", "gobelin", "7_animations")
FPS = 30
pi = math.pi


def rot(axes):                                   # comme froggy_p.py
    Rm = Matrix.Identity(3)
    for ax, deg in axes: Rm = Matrix.Rotation(math.radians(deg), 3, ax) @ Rm
    return Rm


def lier(ob):
    bpy.context.scene.collection.objects.link(ob); return ob


def cibles_ik(arm):                              # comme froggy_p.py
    cibles = {}
    for cote in ("Left", "Right"):
        c = lier(bpy.data.objects.new("Cible_" + cote + "Foot", None)); c.empty_display_size = .04
        c.rotation_mode = "QUATERNION"; c.matrix_world = arm.data.bones[cote + "Foot"].matrix_local
        ik = arm.pose.bones[cote + "Leg"].constraints.new("IK"); ik.target = c; ik.chain_count = 2
        cr = arm.pose.bones[cote + "Foot"].constraints.new("COPY_ROTATION"); cr.target = c
        cibles[cote] = c
    return cibles


# le moteur de Froggy P (fonctions et constantes seulement : rien n'y est exécuté au chargement)
exec(open(os.path.join(R, "Tools", "blender", "froggy_p_animations.py")).read())

# ---------------------------------------------------------------- les clips du kappa
# cookie serré : mains sur les deux bords du cookie, penché en avant comme un fuyard (essai « B » sur 4, 18/11/2026)
SERRE = {"bp": 18, "sx": 10, "hx": -12, "bz": -.025, "ax": -40, "ay": 0, "az": -50, "fx": -90, "c": 4}
# bras croisés : bras pendants, coude plié, rotation forte autour de la verticale (essai « J » ; levés vers l'avant,
# les avant-bras montaient devant le menton au lieu de se croiser)
CROISES = {"ax": 5, "ay": -20, "az": -100, "fx": -95}
T_FOULEE = .40                                   # une foulée (deux pas) : petites jambes rapides


def extra_course(t, v):
    for k, phase in (("g", 0.0), ("d", 0.5)):
        u = (t / T_FOULEE + phase) % 1
        if u < .42:                              # appui : le pied file vers l'arrière
            s = u / .42; v[k + "py"] += -.065 + .13 * s
        else:                                    # vol : le pied revient devant, genou haut
            s = (u - .42) / .58
            v[k + "py"] += .065 - .13 * (.5 - .5 * math.cos(pi * s))
            v[k + "pz"] += .08 * math.sin(pi * s); v[k + "pp"] += 32 * math.sin(pi * s)
    w = 2 * pi * t / T_FOULEE
    v["bz"] += .012 * math.cos(2 * w - .6)        # deux rebonds par foulée
    v["br"] += 3 * math.sin(w); v["bw"] += 6 * math.sin(w + pi); v["sz"] += 4 * math.sin(w)
    v["gay"] += 3 * math.sin(2 * w); v["day"] -= 3 * math.sin(2 * w)   # les bras serrés sautillent un peu
    tremble(t, v, .75, 1.05, {"hx": 1, "sy": .4}, 11, 2.5)              # il ricane en se retournant


def extra_attrape(t, v):
    if .12 <= t <= 1.0:                          # se débat : torsions, bras qui battent, coups de pied
        e = math.sin(pi * (t - .12) / .88)
        w = 2 * pi * 3.2 * (t - .12)
        v["sz"] += 26 * e * math.sin(w); v["hz"] += 18 * e * math.sin(w + .6); v["br"] += 6 * e * math.sin(w + 1.2)
        v["gax"] += 45 * e * math.sin(2 * w); v["dax"] -= 45 * e * math.sin(2 * w + .5)
        v["gpz"] += .08 * e * max(0, math.sin(w)); v["dpz"] += .08 * e * max(0, -math.sin(w))
        v["gpp"] += 30 * e * max(0, math.sin(w)); v["dpp"] += 30 * e * max(0, -math.sin(w))


CLIPS_KAPPA = [
    {"nom": "Course", "duree": 1.6, "boucle": True, "extra": extra_course, "accessoire": True,
     "ressorts": {"hx": (4.5, .45), "hz": (4, .5)},
     "cles": [(0, SERRE), (.55, {"tenir": True}),
              (.75, {"hz": 60, "sz": 16, "hx": -4, "hy": 6}, "out"),          # regard par-dessus l'épaule
              (1.05, {"tenir": True}),
              (1.30, {"hz": 0, "sz": 0, "hx": -10, "hy": 0}, "inout"), (1.6, {"tenir": True})]},

    {"nom": "Attrape", "duree": 2.0, "boucle": False, "extra": extra_attrape,
     "ressorts": {"hx": (3.5, .35), "hz": (3.5, .4), "ay": (5, .45)},
     "cles": [(0, SERRE),
              (.06, {"bz": .02, "bp": -4, "sx": -10, "hx": -14, "ay": 95, "ax": -20, "az": 0, "fx": -30, "c": 14,
                     "gpz": .05, "gpp": 20}, "out"),                               # sursaut : il lâche le cookie
              (.15, {"ay": 80, "fx": -40, "sx": -4, "gpz": 0, "gpp": 0}, "inout"),
              (1.05, {"_repos": True, "bz": -.025, "sx": 16, "hx": 22, "c": -10, "ay": 5, "ax": 5, "fx": -5}, "inout"),
              (1.40, {"bz": -.015, "sx": 6, "hx": 8, "hz": -28, "c": 4, **CROISES}, "inout"),   # croise les bras, boude
              (2.0, {"tenir": True})]},

    {"nom": "Apparition", "duree": 1.3, "boucle": False,
     "ressorts": {"hx": (4.5, .45), "hz": (4.5, .45), "ay": (6, .5)},
     "cles": [(0, {"_repos": True, "bz": -.13, "sx": 32, "bp": 10, "hx": 10, "ay": 10, "ax": -45, "fx": -95, "c": 8}),
              (.12, {"tenir": True}),                                              # tassé : on attend le bond
              (.30, {"bz": .08, "sx": -12, "bp": -6, "hx": -15, "ay": 115, "ax": -10, "fx": -10, "c": 18,
                     "pz": .03}, "in"),                                            # jaillit, bras écartés
              (.42, {"bz": -.04, "sx": 10, "bp": 4, "hx": 4, "ay": 60, "fx": -25, "c": 6, "pz": 0}, "out"),
              (.62, {"bz": -.01, "sx": 4, "hz": 45, "sz": 12, "hx": -4}, "inout"),   # à gauche…
              (.82, {"hz": -45, "sz": -12}, "inout"),                                 # … à droite
              (1.00, {"hz": 0, "sz": 0, "hx": -12, "sx": -6, "ay": 40, "fx": -40}, "inout"),   # narquois
              (1.30, SERRE, "inout")]},                                               # prêt à fuir
]


def cookie_demo(arm):
    """Un cookie doré serré contre la poitrine (rendus seulement, jamais exporté)."""
    bm = bmesh.new()
    bmesh.ops.create_cone(bm, cap_ends=True, segments=32, radius1=.11, radius2=.11, depth=.035)
    me = bpy.data.meshes.new("Cookie_demo"); bm.to_mesh(me); bm.free()
    mat = bpy.data.materials.new("Cookie_demo"); mat.diffuse_color = (.95, .72, .25, 1); me.materials.append(mat)
    ob = lier(bpy.data.objects.new("Cookie_demo", me))
    arm.data.pose_position = "REST"; bpy.context.view_layer.update()
    ob.parent = arm; ob.parent_type = "BONE"; ob.parent_bone = "Spine1"
    ob.matrix_world = Matrix.Translation((0, -.115, .42)) @ Matrix.Rotation(pi / 2, 4, "X")
    arm.data.pose_position = "POSE"
    return ob


bpy.ops.wm.open_mainfile(filepath=os.path.join(R, "Captures", "gobelin", "5_rig", "kappa_rig.blend"))
arm = bpy.data.objects["Squelette"]; corps = bpy.data.objects["Kappa"]
for pb in arm.pose.bones: pb.rotation_mode = "QUATERNION"; pb.rotation_quaternion = (1, 0, 0, 0); pb.location = (0, 0, 0)
cibles = cibles_ik(arm)
cookie = cookie_demo(arm)
os.makedirs(SORTIE, exist_ok=True)
sc = bpy.context.scene; sc.render.fps = FPS
sc.render.engine = "BLENDER_WORKBENCH"; sh = sc.display.shading; sh.light = "STUDIO"; sh.color_type = "TEXTURE"
if sc.world is None: sc.world = bpy.data.worlds.new("Monde")
sc.world.color = (.42, .46, .5); sc.render.resolution_x = sc.render.resolution_y = 300
cam = bpy.data.objects.get("Cam") or lier(bpy.data.objects.new("Cam", bpy.data.cameras.new("Cam")))
sc.camera = cam; cam.data.type = "ORTHO"; cam.data.ortho_scale = 1.15

for clip in CLIPS_KAPPA:
    n = animer2(arm, cibles, corps, clip)
    cookie.hide_render = not clip.get("accessoire")
    # export : squelette ET maillage, comme Froggy P. Sans maillage, le FBX n'a pas de pose de liaison : Unity prend
    # la pose du fichier pour la pose de repos, et la posture tenue tout le clip (penché, cookie serré) disparaît du
    # clip humanoïde (18/11/2026 : seules les jambes bougeaient).
    bpy.ops.object.select_all(action="DESELECT"); arm.select_set(True); corps.select_set(True)
    bpy.ops.export_scene.fbx(filepath=os.path.join(SORTIE, f"Kappa_{clip['nom']}.fbx"), use_selection=True,
                             object_types={"ARMATURE", "MESH"}, add_leaf_bones=False, bake_anim=True,
                             bake_anim_use_all_actions=False, bake_anim_use_nla_strips=False,
                             bake_anim_use_all_bones=True, bake_anim_simplify_factor=0,
                             bake_anim_force_startend_keying=True)
    # planche de contrôle : 8 instants, de trois quarts face et de profil
    for k in range(8):
        f = 1 + round((n - 1) * k / 7); sc.frame_set(f)
        for vue, (az, el) in (("face", (-58, 12)), ("profil", (0, 6))):
            a, e = math.radians(az), math.radians(el); c = Vector((0, 0, .40))
            cam.location = c + Vector((math.cos(a) * math.cos(e), math.sin(a) * math.cos(e), math.sin(e))) * 5
            cam.rotation_euler = (c - cam.location).to_track_quat("-Z", "Y").to_euler()
            sc.render.filepath = os.path.join(SORTIE, "planches", clip["nom"], f"{vue}_{k}.png")
            bpy.ops.render.render(write_still=True)
    print("CLIP", clip["nom"], n, "images")
