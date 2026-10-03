using UnityEngine;

namespace Ember.Presentation
{
    // Decorative embers use one reusable mesh per emitter. This avoids the affected
    // Unity 6000.2.0f1 ParticleSystemGeometryJob path for an opt-in isolation test.
    // This experiment is not proof that runtime allocation warnings are fixed.
    public sealed class DriftingEmbers : MonoBehaviour
    {
        Mesh mesh;Material material;Camera renderCamera;Vector3[] vertices,origins;float[] offsets;int count;float size,radius;Color color;
        public int ActiveCount=>count;
        public void Initialize(float particleSize,float rate,float area,Color tint,Camera camera)
        {
            renderCamera=camera;size=particleSize;radius=area;color=tint;count=Mathf.Clamp(Mathf.CeilToInt(rate*4),1,100);vertices=new Vector3[count*4];origins=new Vector3[count];offsets=new float[count];var uv=new Vector2[count*4];var colors=new Color32[count*4];var indices=new int[count*6];
            for(int i=0;i<count;i++)
            {
                float angle=i*2.399963f,distance=radius*Mathf.Sqrt((i+.5f)/count);origins[i]=new Vector3(Mathf.Cos(angle)*distance,Mathf.Sin(i*1.7f)*radius*.12f,Mathf.Sin(angle)*distance);offsets[i]=i*4f/count;
                int v=i*4,t=i*6;uv[v]=Vector2.zero;uv[v+1]=Vector2.right;uv[v+2]=Vector2.one;uv[v+3]=Vector2.up;
                for(int j=0;j<4;j++)colors[v+j]=color;indices[t]=v;indices[t+1]=v+1;indices[t+2]=v+2;indices[t+3]=v;indices[t+4]=v+2;indices[t+5]=v+3;
            }
            mesh=new Mesh{name="Ember decorative quad pool"};mesh.MarkDynamic();mesh.vertices=vertices;mesh.uv=uv;mesh.colors32=colors;mesh.triangles=indices;mesh.bounds=new Bounds(Vector3.zero,Vector3.one*(radius*2+4));
            material=new Material(Shader.Find("Sprites/Default")){name="Ember decorative glow",mainTexture=Texture2D.whiteTexture};gameObject.AddComponent<MeshFilter>().sharedMesh=mesh;gameObject.AddComponent<MeshRenderer>().sharedMaterial=material;
        }
        void LateUpdate()
        {
            if(mesh==null)return;var camera=renderCamera;if(camera==null)return;var right=transform.InverseTransformVector(camera.transform.right)*size;var up=transform.InverseTransformVector(camera.transform.up)*size;
            for(int i=0;i<count;i++){float age=Mathf.Repeat(Time.time+offsets[i],4);Vector3 p=origins[i]+Vector3.up*(age*.18f);int v=i*4;vertices[v]=p-right-up;vertices[v+1]=p+right-up;vertices[v+2]=p+right+up;vertices[v+3]=p-right+up;}
            mesh.vertices=vertices;
        }
        void OnDestroy(){if(mesh!=null)Destroy(mesh);if(material!=null)Destroy(material);}
    }
}
