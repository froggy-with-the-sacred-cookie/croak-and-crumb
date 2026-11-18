"""Assemble des scènes de contrôle à partir des vrais FBX ; les originaux restent intacts.
Le repos garde les appuis fixes. Les clips Meshy sont conservés sur leur squelette propre,
car le clip Prime emploie un repère d'armature différent des clips du catalogue.
"""
import bpy
import json
import math
import sys
import shutil
from pathlib import Path
from mathutils import Vector, Matrix

root = Path(sys.argv[sys.argv.index('--') + 1])
exports = root / 'exports'
renders = root / 'rendus'
exports.mkdir(exist_ok=True)
renders.mkdir(exist_ok=True)
bpy.ops.wm.read_factory_settings(use_empty=True)
initial_scene = bpy.context.scene
manifest = []
reference = None

def evaluated_bounds(meshes):
    deps = bpy.context.evaluated_depsgraph_get()
    pts = [o.evaluated_get(deps).matrix_world @ Vector(v) for o in meshes for v in o.evaluated_get(deps).bound_box]
    return Vector([min(p[i] for p in pts) for i in range(3)]), Vector([max(p[i] for p in pts) for i in range(3)])

def set_action(arm, action):
    arm.animation_data_create()
    arm.animation_data.action = action
    if action and action.slots:
        arm.animation_data.action_slot = action.slots[0]

clips = [('Repos','froggy-b-arme'),('Marche','froggy-b-marche-original'),('Course','froggy-b-course-original'),('Saut','froggy-b-saut-original'),('Victoire','froggy-b-victoire-original')]
for index, (label, stem) in enumerate(clips):
    scene = bpy.data.scenes.new(f'{index+1:02d}_{label}')
    bpy.context.window.scene = scene
    bpy.ops.import_scene.fbx(filepath=str(root/'originaux'/(stem+'.fbx')))
    imported = list(scene.objects)
    arms = [o for o in imported if o.type == 'ARMATURE']
    arm = arms[0]
    meshes = [o for o in imported if o.type == 'MESH' and any(m.type == 'ARMATURE' for m in o.modifiers)]
    assert meshes and len(arms) == 1
    for o in imported:
        if o.type == 'MESH' and o not in meshes:
            o.hide_render = True
    for track in list(arm.animation_data.nla_tracks):
        arm.animation_data.nla_tracks.remove(track)
    action = arm.animation_data.action
    scene.frame_set(int(action.frame_range[0]))
    bpy.context.view_layer.update()

    if reference is None:
        lo,hi=evaluated_bounds(meshes)
        reference={'lo':lo.copy(),'hi':hi.copy(),'scale':2/(hi.z-lo.z)}

    if label == 'Repos':
        # Baisser les bras depuis la pose A, en pivotant autour de l'axe frontal.
        # Aucun déplacement du bassin ni des jambes n'est ajouté.
        base = {p.name:p.matrix_basis.copy() for p in arm.pose.bones}
        inv_arm = arm.matrix_world.to_3x3().inverted()
        axis = (inv_arm @ Vector((0,1,0))).normalized()
        for name in ['LeftArm','RightArm']:
            p=arm.pose.bones[name]
            world_dir=arm.matrix_world.to_3x3() @ (p.tail-p.head)
            sign=1 if world_dir.x>0 else -1
            m=p.matrix.copy(); pivot=Matrix.Translation(p.head)
            p.matrix=pivot @ Matrix.Rotation(math.radians(26)*sign,4,axis) @ pivot.inverted() @ m
            bpy.context.view_layer.update()
        base = {p.name:p.matrix_basis.copy() for p in arm.pose.bones}
        rotations = {p.name:p.rotation_quaternion.copy() for p in arm.pose.bones}
        set_action(arm,None)
        idle=bpy.data.actions.new('Repos')
        set_action(arm,idle)
        for f in range(1,122,4):
            phase=2*math.pi*(f-1)/120
            breath=math.sin(phase)+.12*math.sin(2*phase)
            for p in arm.pose.bones:
                p.rotation_mode='QUATERNION'
                p.matrix_basis=base[p.name]
                if p.name in ['Spine','Spine01','Head']:
                    degrees={'Spine':.35,'Spine01':.25,'Head':-.2}[p.name]
                    p.rotation_quaternion=rotations[p.name] @ Matrix.Rotation(math.radians(degrees*breath),4,'X').to_quaternion()
                p.keyframe_insert('location',frame=f,group=p.name)
                p.keyframe_insert('rotation_quaternion',frame=f,group=p.name)
                p.keyframe_insert('scale',frame=f,group=p.name)
        action=arm.animation_data.action
        frame_start,frame_end=1,121
        scene.frame_set(1)
    else:
        frame_start,frame_end=[int(x) for x in action.frame_range]
        action.name=label

    action.use_fake_user=True
    scene.render.fps=30
    scene.frame_start=frame_start
    scene.frame_end=frame_end
    duration=(frame_end-frame_start)/30
    sample_count=max(2,round(duration*15))
    timeline=[frame_start+(frame_end-frame_start)*i/sample_count for i in range(sample_count)]
    samples=[]
    feet=[]
    for f in range(frame_start,frame_end+1):
        scene.frame_set(f)
        bpy.context.view_layer.update()
        feet.append({'frame':f,'left':list(arm.matrix_world @ arm.pose.bones['LeftFoot'].head),'right':list(arm.matrix_world @ arm.pose.bones['RightFoot'].head),'hips':list(arm.matrix_world @ arm.pose.bones['Hips'].head)})
    for f in [frame_start,frame_end]:
        scene.frame_set(f)
        samples.append({p.name: {'location':list(p.location),'rotation':list(p.rotation_quaternion),'scale':list(p.scale)} for p in arm.pose.bones})
    rotation_errors=[]
    for name in samples[0]:
        q0=Vector(samples[0][name]['rotation']).normalized(); q1=Vector(samples[1][name]['rotation']).normalized()
        rotation_errors.append(2*math.acos(min(1,abs(q0.dot(q1)))))
    translation_error=max((Vector(samples[0][name]['location'])-Vector(samples[1][name]['location'])).length for name in samples[0])

    scene.frame_set(frame_start)
    bpy.ops.object.select_all(action='DESELECT')
    for o in meshes+[arm]:o.select_set(True)
    bpy.context.view_layer.objects.active=arm
    fbx_path=exports/f'Froggy_B_{label}.fbx'
    bpy.ops.export_scene.fbx(filepath=str(fbx_path),use_selection=True,object_types={'ARMATURE','MESH'},add_leaf_bones=False,bake_anim=True,bake_anim_use_all_actions=False,bake_anim_use_nla_strips=False,bake_anim_use_all_bones=True,bake_anim_simplify_factor=0,path_mode='COPY',embed_textures=True,axis_forward='-Z',axis_up='Y')

    # Les ajustements de taille suivants servent uniquement à la scène de contrôle.
    group=bpy.data.objects.new(f'Presentation_{label}',None)
    scene.collection.objects.link(group)
    for o in imported:
        if o.parent not in imported:
            world=o.matrix_world.copy();o.parent=group;o.matrix_world=world
    s=reference['scale'];lo=reference['lo'];hi=reference['hi']
    group.scale=(s,s,s)
    group.location=(-(lo.x+hi.x)*.5*s,-(lo.y+hi.y)*.5*s,-lo.z*s)
    bpy.context.view_layer.update()
    scene.render.engine='BLENDER_EEVEE'
    scene.render.resolution_x=480
    scene.render.resolution_y=480
    scene.render.resolution_percentage=100
    scene.render.image_settings.file_format='PNG'
    scene.render.image_settings.color_mode='RGB'
    scene.world=bpy.data.worlds.new('World_'+label)
    scene.world.use_nodes=True
    scene.world.node_tree.nodes['Background'].inputs['Color'].default_value=(.78,.80,.75,1)
    scene.world.node_tree.nodes['Background'].inputs['Strength'].default_value=.65
    scene.view_settings.view_transform='Standard'
    scene.view_settings.look='None'
    for name,loc,power,size in [('Key',(-3,-4,5),200,4),('Fill',(4,-1,3),100,4),('Rear',(0,4,4),140,3)]:
        data=bpy.data.lights.new(label+'_'+name,'AREA');data.energy=power;data.size=size
        obj=bpy.data.objects.new(label+'_'+name,data);scene.collection.objects.link(obj);obj.location=loc;obj.rotation_euler=(Vector((0,0,1))-obj.location).to_track_quat('-Z','Y').to_euler()
    camera_data=bpy.data.cameras.new('Camera_'+label);camera_data.type='ORTHO'
    camera=bpy.data.objects.new('Camera_'+label,camera_data);scene.collection.objects.link(camera);scene.camera=camera
    camera.location=(3.5,-6,2.3);camera.rotation_euler=(Vector((0,0,1))-camera.location).to_track_quat('-Z','Y').to_euler()
    # Cadrage fixe pendant chaque clip, calculé sur toutes ses poses.
    bpy.context.view_layer.update()
    inv=camera.matrix_world.inverted()
    points=[]
    for f in timeline:
        scene.frame_set(int(f),subframe=f-int(f));bpy.context.view_layer.update()
        dg=bpy.context.evaluated_depsgraph_get()
        points.extend(inv @ (o.evaluated_get(dg).matrix_world @ Vector(v)) for o in meshes for v in o.evaluated_get(dg).bound_box)
    xmin,xmax=min(p.x for p in points),max(p.x for p in points)
    ymin,ymax=min(p.y for p in points),max(p.y for p in points)
    camera.location+=camera.rotation_euler.to_quaternion() @ Vector(((xmin+xmax)*.5,(ymin+ymax)*.5,0))
    camera_data.ortho_scale=max(xmax-xmin,ymax-ymin)*1.16
    folder=renders/label.lower();folder.mkdir(exist_ok=True)
    for i,f in enumerate(timeline):
        scene.frame_set(int(f),subframe=f-int(f));scene.render.filepath=str(folder/f'{i:04d}.png');bpy.ops.render.render(write_still=True)
    scene.frame_set(frame_start)
    for area in bpy.context.screen.areas:
        if area.type=='VIEW_3D':
            area.spaces.active.region_3d.view_perspective='CAMERA'
    info={'label':label,'scene':scene.name,'source_file':stem+'.fbx','fbx':str(fbx_path),'action':action.name,'bones':len(arm.data.bones),'faces':sum(len(o.data.polygons) for o in meshes),'fps':30,'frame_range':[frame_start,frame_end],'duration_seconds':duration,'preview_frames':sample_count,'preview_fps':sample_count/duration,'loop':label in ['Repos','Marche','Course'],'endpoint_max_rotation_radians':max(rotation_errors),'endpoint_max_location_armature_units':translation_error,'foot_samples':feet,'render_folder':str(folder),'notes':'Export sur le squelette propre au clip ; repères Prime conservés.' if label=='Saut' else ''}
    manifest.append(info)
    (root/'inspection/animations-livrees.json').write_text(json.dumps(manifest,ensure_ascii=False,indent=2))
    print('CLIP_COMPLETE='+json.dumps({k:v for k,v in info.items() if k!='foot_samples'}),flush=True)

if initial_scene.name in bpy.data.scenes and len(initial_scene.objects)==0:bpy.data.scenes.remove(initial_scene)
bpy.context.window.scene=bpy.data.scenes['01_Repos']
for image in bpy.data.images:
    if image.has_data and image.type not in {'RENDER_RESULT','COMPOSITING'}:
        try:image.pack()
        except RuntimeError:pass
text=bpy.data.texts.new('LIRE_MOI')
text.write('FROGGY B — CINQ ANIMATIONS\nChoisir une scène : 01_Repos, 02_Marche, 03_Course, 04_Saut, 05_Victoire.\nEspace : lecture. Les scènes de contrôle sont normalisées pour le cadrage ; les FBX gardent les unités du modèle.\nMarche/course : mouvements humains de test. Le ventre et les hanches présentent des plis à retravailler.\nChaque export conserve son squelette. Le saut Prime utilise un autre repère interne : ne pas copier ses courbes directement sur un autre rig.\nCoût : 21 crédits supplémentaires, sans autre génération.\n')
bpy.ops.wm.save_as_mainfile(filepath=str(root/'Froggy_B_5_animations.blend'))
print('ANIMATIONS_BUILD_COMPLETE',flush=True)
