"""Bibliothèque Blender des modèles taillés et peints (skill modele-3d-blender), reprise de Lovecraft (filon de cuivre).
S'exécute DANS Blender (MCP blender, ou Blender -b -P), où __file__ n'existe pas : le dossier du projet vient de la
variable d'environnement FROGGY_ROOT (sinon le dossier courant).
    exec(open(os.environ["FROGGY_ROOT"] + "/Tools/blender/blender_lib.py").read())
puis le script de l'asset construit ses pièces avec ces briques.

Principes (voir le skill) :
- tout est peint dans les couleurs de sommets, en sRGB 0-255 (les calques « octets » de bmesh les gardent telles quelles) ;
  le shader du jeu doit lire la couleur de sommet et éclairer par-dessus : peindre une lumière douce, pas des ombres dures ;
- chaque pièce a un contour : une coque retournée sombre (le matériau du contour cache les faces avant) ;
- les pièces qui partent une à une s'appellent Morceau_1..N, avec leur origine au centre de la pièce."""
import bpy, bmesh, math, random, os
from mathutils import Vector, noise
from mathutils.bvhtree import BVHTree

RACINE = os.environ.get("FROGGY_ROOT", os.getcwd())
MODELES = os.path.join(RACINE, "Assets", "Froggy", "Models")      # proposition : à ajuster à l'arborescence du projet
RENDUS = os.path.join(RACINE, "Captures", "modeles")              # hors git
CONTOUR = (62, 42, 34)                                  # le brun foncé des contours des icônes de Lovecraft : à régler sur la DA de Froggy
EPAISSEUR = .016                                        # épaisseur du contour, en cases (≈ unités de Blender)
LUMIERE = Vector((-.45, -.55, .7)).normalized()        # lumière peinte : en haut à gauche, comme les icônes
REFLET = (255, 240, 212)                                # l'éclat d'un métal
# gris bleuté : reste gris sous les lumières chaudes du jeu (lanternes de la mine, soleil du soir)
ROCHE = {"h": (184, 192, 216), "m": (138, 146, 174), "b": (90, 96, 126), "arete": (200, 208, 228)}


# ---------- couleurs ----------
def srgb(c): return tuple(v / 255 for v in c) + (1.0,)
def melange(a, b, t): t = max(0, min(1, t)); return tuple(a[i] + (b[i] - a[i]) * t for i in range(3))


def ombre(n, haut, milieu, bas, rnd, var=.04):
    """Facette claire face à la lumière et au ciel, sombre dessous (peinture douce, le jeu éclaire aussi)."""
    l = max(0, n.dot(LUMIERE)) * .65 + max(0, n.z) * .35
    c = melange(bas, milieu, (n.z + 1) * 1.1) if n.z < 0 else melange(milieu, haut, l * 1.25)
    k = 1 + rnd.uniform(-var, var)
    return tuple(min(255, max(0, v * k)) for v in c)


def couleur_metal(f, rnd, tons, patine=None, graine=0.0, echelle=6.0, part=.3, reflet=True):
    """Un métal peint : tons = (clair, moyen, sombre) ; patine = (clair, moyen, sombre) posée par taches (bruit 3D),
    part = seuil du bruit (plus haut, moins de patine) ; reflet : les facettes face à la lumière tirent vers le blanc chaud."""
    c = f.calc_center_median(); n = f.normal
    if patine and noise.noise(c * echelle + Vector((graine, graine * .7, 0))) > part: return ombre(n, *patine, rnd)
    col = ombre(n, *tons, rnd, .06)
    l = n.dot(LUMIERE)
    if reflet and l > .8: col = melange(col, REFLET, (l - .8) / .2 * .85)
    return col


def couleur_roche(f, rnd, tons=ROCHE, arete=None):
    """Une facette de roche ; les arêtes biseautées (calque « arete » = 1) sont un peu plus claires."""
    c = ombre(f.normal, tons["h"], tons["m"], tons["b"], rnd)
    if arete is not None and f[arete] == 1: c = melange(c, tons["arete"], .6)
    return c


def calque(bm): return bm.loops.layers.color.get("Col") or bm.loops.layers.color.new("Col")


def peindre(bm, couleur_de):
    """couleur_de(face) -> (r, g, b) en 0-255."""
    lay = calque(bm)
    for f in bm.faces:
        c = srgb(couleur_de(f))
        for lo in f.loops: lo[lay] = c


# ---------- maillages ----------
def fusionner(dest, src_bm):
    """Ajoute src_bm dans dest (src_bm est libéré)."""
    tmp = bpy.data.meshes.new("tmp"); src_bm.to_mesh(tmp); src_bm.free(); dest.from_mesh(tmp); bpy.data.meshes.remove(tmp)


def bm_de_objet(ob):
    """Le maillage évalué d'un objet (modificateurs appliqués, courbe convertie), en bmesh ; l'objet est supprimé."""
    dg = bpy.context.evaluated_depsgraph_get(); ev = ob.evaluated_get(dg)
    me = bpy.data.meshes.new_from_object(ev); bm = bmesh.new(); bm.from_mesh(me)
    bm.transform(ob.matrix_world); bpy.data.meshes.remove(me)
    data = ob.data; bpy.data.objects.remove(ob, do_unlink=True)
    if data is not None and data.users == 0:
        (bpy.data.curves if isinstance(data, bpy.types.Curve) else bpy.data.meshes).remove(data)
    return bm


def objet_temp(nom, bm):
    me = bpy.data.meshes.new(nom); bm.to_mesh(me); bm.free()
    ob = bpy.data.objects.new(nom, me); bpy.context.scene.collection.objects.link(ob); return ob


def ajouter_contour(bm, ep=EPAISSEUR, couleur=CONTOUR):
    """Coque retournée, gonflée le long des normales lissées : le contour des icônes. À appeler après peindre()."""
    copie = bm.copy(); copie.normal_update()
    for v in copie.verts: v.co += v.normal * ep
    bmesh.ops.reverse_faces(copie, faces=copie.faces[:])
    lay = calque(copie); c = srgb(couleur)
    for f in copie.faces:
        for lo in f.loops: lo[lay] = c
    fusionner(bm, copie)


def fibo(n):
    """n directions presque régulièrement réparties sur la sphère."""
    ga = math.pi * (3 - math.sqrt(5))
    for i in range(n):
        z = 1 - 2 * (i + .5) / n; r = math.sqrt(1 - z * z)
        yield Vector((math.cos(ga * i) * r, math.sin(ga * i) * r, z))


def tailler(bm, taille, n, rnd, alea=.12, prof=(.7, .8), garder_dessous=True):
    """Coupe un ellipsoïde (taille) par n plans répartis tout autour : de grandes facettes planes, comme une pierre taillée."""
    for d in fibo(n):
        d = (d + Vector([rnd.uniform(-alea, alea) for _ in range(3)])).normalized()
        if garder_dessous and d.z < -.6: continue
        rayon = Vector((d.x * taille.x, d.y * taille.y, d.z * taille.z)).length
        res = bmesh.ops.bisect_plane(bm, geom=bm.verts[:] + bm.edges[:] + bm.faces[:], plane_co=d * rayon * rnd.uniform(*prof), plane_no=d, clear_outer=True)
        bord = [e for e in res["geom_cut"] if isinstance(e, bmesh.types.BMEdge)]
        if bord: bmesh.ops.holes_fill(bm, edges=bord)


def couper(bm, point, normale):
    """Coupe net par un plan (garde le côté opposé à la normale) et bouche le trou."""
    res = bmesh.ops.bisect_plane(bm, geom=bm.verts[:] + bm.edges[:] + bm.faces[:], plane_co=point, plane_no=normale.normalized(), clear_outer=True)
    bord = [e for e in res["geom_cut"] if isinstance(e, bmesh.types.BMEdge)]
    if bord: bmesh.ops.holes_fill(bm, edges=bord)


def pierre(rnd, n, taille, alea=.12, sub=3, prof=(.7, .8), biseau=.028):
    """Une pierre taillée posée au sol, aux arêtes biseautées. Calque de faces « arete » : 2 facette, 1 arête, 0 face ajoutée
    ensuite (booléen, cassure). sub : finesse de l'ellipsoïde de départ (1 pour un petit éclat, 3 pour un gros bloc)."""
    bm = bmesh.new(); bmesh.ops.create_icosphere(bm, subdivisions=sub, radius=1)
    for v in bm.verts: v.co = Vector((v.co.x * taille.x, v.co.y * taille.y, v.co.z * taille.z))
    tailler(bm, taille, n, rnd, alea, prof)
    couper(bm, Vector((0, 0, -taille.z * .55)), Vector((0, 0, -1)))
    bmesh.ops.dissolve_limit(bm, angle_limit=math.radians(9), verts=bm.verts[:], edges=bm.edges[:])
    mz = min(v.co.z for v in bm.verts)
    for v in bm.verts: v.co.z -= mz
    arete = bm.faces.layers.int.new("arete")
    for f in bm.faces: f[arete] = 2
    if biseau:
        res = bmesh.ops.bevel(bm, geom=bm.edges[:], offset=min(taille) * biseau, segments=1, affect="EDGES", profile=.5, clamp_overlap=True)
        for f in res["faces"]: f[arete] = 1
    return bm


def pepite(rnd, r, fin=True, n=14):
    """Une pépite (ou un fruit, un cristal arrondi…) taillée à facettes, centrée sur l'origine. fin : facettes redécoupées
    (sans perdre leur planéité) pour peindre des taches à l'intérieur ; False pour les petites miettes."""
    t = Vector((r * rnd.uniform(1, 1.2), r * rnd.uniform(.85, 1), r * rnd.uniform(.8, .95)))
    bm = bmesh.new(); bmesh.ops.create_icosphere(bm, subdivisions=2, radius=1)
    for v in bm.verts: v.co = Vector((v.co.x * t.x, v.co.y * t.y, v.co.z * t.z))
    tailler(bm, t, n, rnd, .25, (.74, .86), garder_dessous=False)
    bmesh.ops.dissolve_limit(bm, angle_limit=math.radians(8), verts=bm.verts[:], edges=bm.edges[:])
    bmesh.ops.triangulate(bm, faces=bm.faces[:])
    if fin: bmesh.ops.subdivide_edges(bm, edges=bm.edges[:], cuts=1, use_grid_fill=True)
    return bm


def creuser(bm, trous):
    """Creuse des cavités (booléen) : trous = [(centre, rayon)]. Les faces des creux ont « arete » = 0 (à peindre à part)."""
    bloc = objet_temp("bloc", bm); coupes = []
    for i, (c, r) in enumerate(trous):
        bc = bmesh.new(); bmesh.ops.create_icosphere(bc, subdivisions=1, radius=r)
        for v in bc.verts: v.co += c
        cut = objet_temp(f"creux{i}", bc); cut.hide_render = True; coupes.append(cut)
        m = bloc.modifiers.new(f"creux{i}", "BOOLEAN"); m.operation = "DIFFERENCE"; m.solver = "EXACT"; m.object = cut
    res = bm_de_objet(bloc)
    for cut in coupes:
        me = cut.data; bpy.data.objects.remove(cut, do_unlink=True); bpy.data.meshes.remove(me)
    return res


def point_surface(bvh, centre, az, el):
    """Le point de la surface dans la direction (azimut, hauteur) vue du centre, et sa normale."""
    d = Vector((math.cos(az) * math.cos(el), math.sin(az) * math.cos(el), math.sin(el)))
    hit = bvh.ray_cast(centre + d * 3, -d)
    return (hit[0], hit[1].normalized()) if hit[0] is not None else (None, None)


def veine(bvh, centre, az, el, rnd, n=9, pas=.16):
    """Un tracé qui descend le long d'une surface : marche aléatoire en (azimut, hauteur), projetée (veines, racines, lierre…)."""
    pts = []
    for i in range(n):
        p, nn = point_surface(bvh, centre, az, el)
        if p is None: break
        pts.append(p + nn * .002)
        el -= pas * rnd.uniform(.7, 1.0); az += rnd.uniform(-.14, .14)
        if el < -.35: break
    return pts


def tube(pts, rayon):
    """Un tube doux (courbe NURBS) le long des points."""
    cu = bpy.data.curves.new("tube", "CURVE"); cu.dimensions = "3D"
    sp = cu.splines.new("NURBS"); sp.points.add(len(pts) - 1)
    for i, p in enumerate(pts): sp.points[i].co = (p.x, p.y, p.z, 1)
    sp.order_u = 3; sp.use_endpoint_u = True; sp.resolution_u = 3
    cu.bevel_depth = rayon; cu.bevel_resolution = 1; cu.use_fill_caps = True
    ob = bpy.data.objects.new("tube", cu); bpy.context.scene.collection.objects.link(ob)
    return bm_de_objet(ob)


# ---------- objets, export, rendus ----------
def collection(nom, vider=True):
    col = bpy.data.collections.get(nom) or bpy.data.collections.new(nom)
    if col.name not in bpy.context.scene.collection.children: bpy.context.scene.collection.children.link(col)
    if vider:
        for ob in list(col.objects): bpy.data.objects.remove(ob, do_unlink=True)
    return col


def objet(nom, bm, col, origine=Vector()):
    """Un objet de la collection : faces plates, couleurs actives ; origine = son centre (pour une pièce qui part)."""
    for v in bm.verts: v.co -= origine
    me = bpy.data.meshes.new(nom); bm.to_mesh(me); bm.free()
    for p in me.polygons: p.use_smooth = False
    if me.color_attributes:
        me.color_attributes.active_color_name = me.color_attributes[0].name; me.color_attributes.render_color_index = 0
    ob = bpy.data.objects.new(nom, me); ob.location = origine; col.objects.link(ob)
    return ob


def exporter(col, chemin):
    """Exporte les objets de la collection en FBX (couleurs de sommets sRGB, unités 1:1, faces plates)."""
    os.makedirs(os.path.dirname(chemin), exist_ok=True)
    bpy.ops.object.select_all(action="DESELECT")
    for ob in col.objects: ob.select_set(True)
    bpy.ops.export_scene.fbx(filepath=chemin, use_selection=True, object_types={"MESH"}, colors_type="SRGB",
                             apply_scale_options="FBX_SCALE_UNITS", mesh_smooth_type="FACE", add_leaf_bones=False, bake_anim=False)
    return chemin


def poids(col):
    return sum(len(o.data.polygons) for o in col.objects)


def rendre(nom, az=-90, el=42, cible=Vector((0, 0, .4)), echelle=2.0, seules=None, cachees=(), dossier=RENDUS):
    """Rendu de contrôle (couleurs de sommets, faces arrière cachées comme dans le jeu), vue orthographique.
    az=-90, el=42 : à peu près l'angle de la caméra du jeu, face à l'avant du modèle (−Y dans Blender).
    seules : noms de collections à montrer (les autres sont cachées) ; cachees : noms d'objets à cacher (ex. Morceau_1)."""
    sc = bpy.context.scene; sc.render.engine = "BLENDER_WORKBENCH"
    sh = sc.display.shading; sh.light = "FLAT"; sh.color_type = "VERTEX"; sh.show_backface_culling = True
    sc.render.resolution_x, sc.render.resolution_y = 800, 700; sc.render.film_transparent = False
    cam = bpy.data.objects.get("Camera") or bpy.data.objects.new("Camera", bpy.data.cameras.new("Camera"))
    if cam.name not in sc.collection.objects and not cam.users_collection: sc.collection.objects.link(cam)
    cam.data.type = "ORTHO"; cam.data.ortho_scale = echelle; sc.camera = cam
    a, e = math.radians(az), math.radians(el)
    cam.location = cible + Vector((math.cos(a) * math.cos(e), math.sin(a) * math.cos(e), math.sin(e))) * 6
    cam.rotation_euler = (cible - cam.location).to_track_quat("-Z", "Y").to_euler()
    etat = {}
    for c in bpy.data.collections:
        for o in c.objects:
            etat[o.name] = o.hide_render
            o.hide_render = (seules is not None and c.name not in seules) or o.name in cachees
    os.makedirs(dossier, exist_ok=True)
    sc.render.filepath = os.path.join(dossier, nom + ".png"); bpy.ops.render.render(write_still=True)
    for n, h in etat.items():
        if n in bpy.data.objects: bpy.data.objects[n].hide_render = h
    return sc.render.filepath
