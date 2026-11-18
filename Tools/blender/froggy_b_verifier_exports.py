import bpy,json,sys
from pathlib import Path
from mathutils import Vector
root=Path(sys.argv[sys.argv.index('--')+1])
manifest=json.loads((root/'inspection/animations-livrees.json').read_text())
assert len(manifest)==5
report=[]
for record in manifest:
    bpy.ops.wm.read_factory_settings(use_empty=True)
    # Ne pas ajouter l'offset d'une image du lecteur FBX lors de la comparaison.
    bpy.ops.import_scene.fbx(filepath=record['fbx'], anim_offset=0.0)
    arm=next(o for o in bpy.data.objects if o.type=='ARMATURE')
    meshes=[o for o in bpy.data.objects if o.type=='MESH']
    action=arm.animation_data.action
    assert action is not None
    assert len(arm.data.bones)==24
    assert sum(len(o.data.polygons) for o in meshes)==31163
    errors=[]
    points={p['frame']:p for p in record['foot_samples']}
    lo,hi=[int(x) for x in action.frame_range]
    assert lo==record['frame_range'][0] and hi==record['frame_range'][1], (record['label'],[lo,hi])
    for f in [lo,(lo+hi)//2,hi]:
        bpy.context.scene.frame_set(f)
        for bone,key in [('Hips','hips'),('LeftFoot','left'),('RightFoot','right')]:
            measured=arm.matrix_world @ arm.pose.bones[bone].head
            errors.append((measured-Vector(points[f][key])).length)
    assert max(errors)<0.001,(record['label'],max(errors))
    assert all(im.has_data for im in bpy.data.images if im.type not in {'RENDER_RESULT','COMPOSITING'})
    report.append({'label':record['label'],'bone_count':len(arm.data.bones),'faces':31163,'frame_range':[lo,hi],'max_root_or_foot_roundtrip_error':max(errors),'textures_loaded':True})
(root/'inspection/audit-fbx-exportes.json').write_text(json.dumps(report,ensure_ascii=False,indent=2))
print('EXPORT_AUDIT_SUCCESS='+json.dumps(report,ensure_ascii=False))
