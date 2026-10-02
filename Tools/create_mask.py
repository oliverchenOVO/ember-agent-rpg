"""Run with Blender 3.1 --background --python Tools/create_mask.py.
Creates an original beveled guardian mask; no downloaded assets.
"""
import bpy
from pathlib import Path
from math import radians
bpy.ops.object.select_all(action='SELECT')
bpy.ops.object.delete(use_global=False)
outline=[(-.68,.52),(-.38,.8),(.38,.8),(.68,.52),(.57,-.24),(0,-.78),(-.57,-.24)]
verts=[(x,-.12,z) for x,z in outline]+[(x*.77,.35,z*.8) for x,z in outline]+[(0,-.42,.05)]
faces=[]
for i in range(7):
 j=(i+1)%7
 faces.append((14,j,i));faces.append((i,j,7+j,7+i))
faces.append(tuple(range(7,14)))
mesh=bpy.data.meshes.new('RootcrownMask');mesh.from_pydata(verts,[],faces);mesh.update()
obj=bpy.data.objects.new('RootcrownMask',mesh);bpy.context.collection.objects.link(obj);bpy.context.view_layer.objects.active=obj;obj.select_set(True)
bevel=obj.modifiers.new('Hand-carved chamfer','BEVEL');bevel.width=.065;bevel.segments=2
bpy.ops.object.modifier_apply(modifier=bevel.name)
mesh.use_auto_smooth=True
normal=obj.modifiers.new('Weighted face normals','WEIGHTED_NORMAL');bpy.ops.object.modifier_apply(modifier=normal.name)
root=Path(__file__).resolve().parents[1]
out=root/'UnityProject/Assets/Resources/RootcrownMask.fbx'
bpy.ops.export_scene.fbx(filepath=str(out),use_selection=True,axis_forward='-Z',axis_up='Y',apply_unit_scale=True,object_types={'MESH'},bake_anim=False)
print('EMBER MASK EXPORTED',out)
