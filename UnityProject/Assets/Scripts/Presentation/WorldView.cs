using System.Collections.Generic;
using Ember.Core;
using Ember.Core.Phase2;
using UnityEngine;

namespace Ember.Presentation
{
    public sealed partial class WorldView
    {
        readonly bool noHitFx=System.Array.IndexOf(System.Environment.GetCommandLineArgs(),"--no-hit-fx")>=0;
        public int EffectSerial{get;private set;}
        public void SetUIOnly(){world.gameObject.SetActive(false);}
        public Camera camera; public Color[] colors={new Color(.95f,.6f,.22f),new Color(.35f,.85f,.72f),new Color(.57f,.55f,1),new Color(.92f,.76f,.52f)};
        Transform world, arena, refuge, boss, crown, warning, shock, collapseFront, collapseVoid;
        Transform[] bodies=new Transform[4], auras=new Transform[4];
        readonly List<Transform> arms=new List<Transform>();
        Material ground, stone, dark, brass, green, glow, red;
        Material[] robes=new Material[4];
        float angle=0,observedRestLimit=Simulation.RestLimit;
        float[] previousDamage=new float[4], previousHealing=new float[4];
        class Effect {public Transform visual;public Vector3 from,to;public float left=.35f;}
        readonly List<Effect> effects=new List<Effect>();
        readonly Stack<Effect> effectPool=new Stack<Effect>();
        readonly List<Mesh> ownedMeshes=new List<Mesh>();readonly List<Material> ownedMaterials=new List<Material>();bool disposed;
        public void Dispose(){if(disposed)return;disposed=true;foreach(var ps in world.GetComponentsInChildren<ParticleSystem>(true)){ps.Stop(true,ParticleSystemStopBehavior.StopEmittingAndClear);}Object.Destroy(world.gameObject);Object.Destroy(camera.gameObject);foreach(var m in ownedMeshes)Object.Destroy(m);foreach(var m in ownedMaterials)Object.Destroy(m);}
        public WorldView()
        {
            world=new GameObject("EMBER WORLD").transform;
            ground=Mat(new Color(.075f,.13f,.15f),.25f,.45f);stone=Mat(new Color(.17f,.24f,.25f),.15f,.55f);dark=Mat(new Color(.035f,.055f,.065f),.3f,.65f);
            brass=Mat(new Color(.64f,.42f,.16f),.7f,.7f);green=Mat(new Color(.18f,.31f,.22f),.25f,.4f);
            glow=Mat(new Color(.24f,.81f,.77f),.2f,.8f,1.6f);red=Mat(new Color(.95f,.27f,.13f),.1f,.6f,1.5f);
            violet=Mat(new Color(.46f,.2f,.7f),.3f,.55f,.7f);whiteHot=Mat(new Color(1,.85f,.55f),.4f,.7f,1.2f);
            for(int i=0;i<4;i++)robes[i]=Mat(colors[i],.35f,.65f,.25f);
            camera=new GameObject("Witness Camera").AddComponent<Camera>();camera.fieldOfView=42;camera.nearClipPlane=.2f;camera.farClipPlane=160;camera.backgroundColor=new Color(.035f,.065f,.08f);
            RenderSettings.fog=true;RenderSettings.fogColor=camera.backgroundColor;RenderSettings.fogDensity=.012f;
            RenderSettings.ambientLight=new Color(.27f,.38f,.43f);RenderSettings.ambientIntensity=.85f;
            var key=new GameObject("Moon / key").AddComponent<Light>();key.transform.SetParent(world,false);key.type=LightType.Directional;key.color=new Color(.63f,.81f,1);key.intensity=1.6f;key.transform.rotation=Quaternion.Euler(48,-38,0);key.shadows=LightShadows.Soft;
            QualitySettings.shadows=ShadowQuality.All;QualitySettings.shadowDistance=60;QualitySettings.antiAliasing=4;
            arena=new GameObject("01 / Rootcrown Amphitheatre").transform;arena.SetParent(world);
            refuge=new GameObject("Refuge / Ashen Crossing").transform;refuge.SetParent(world);
            var arenaParent=arena;arena=new GameObject("Rootcrown architecture").transform;arena.SetParent(arenaParent,false);MakeArena();baseArchitecture=arena;arena=arenaParent;
            var restParent=refuge;refuge=new GameObject("Crossing architecture").transform;refuge.SetParent(restParent,false);MakeRefuge();baseRestArchitecture=refuge;refuge=restParent;MakeBoss();
            warning=Ring("Danger telegraph",arena,Vector3.zero,3.2f,.13f,red);
            shock=Ring("Impact pulse",arena,Vector3.zero,3.2f,.1f,glow);
            for(int i=0;i<4;i++) {bodies[i]=new GameObject("Witness "+i).transform;bodies[i].SetParent(world);auras[i]=Ring("Witness aura",bodies[i],new Vector3(0,.03f,0),.65f,.04f,robes[i]);}
        }
        Material Mat(Color c,float metal,float smooth,float emission=0)
        {
            var m=new Material(Shader.Find("Standard"));m.color=c;m.SetFloat("_Metallic",metal);m.SetFloat("_Glossiness",smooth);
            if(emission>0){m.EnableKeyword("_EMISSION");m.SetColor("_EmissionColor",c*emission);}ownedMaterials.Add(m);return m;
        }
        Transform Part(string name,PrimitiveType type,Transform parent,Vector3 p,Vector3 scale,Material m,Vector3 rotation=default)
        {
            var g=GameObject.CreatePrimitive(type);g.name=name;g.transform.SetParent(parent,false);g.transform.localPosition=p;g.transform.localScale=scale;g.transform.localEulerAngles=rotation;g.GetComponent<Renderer>().sharedMaterial=m;
            Object.Destroy(g.GetComponent<Collider>());return g.transform;
        }
        Transform Ring(string name,Transform parent,Vector3 pos,float radius,float width,Material material,int segments=96)
        {
            var g=new GameObject(name);g.transform.SetParent(parent,false);g.transform.localPosition=pos;
            var mesh=new Mesh();ownedMeshes.Add(mesh);var vertices=new Vector3[segments*2];var triangles=new int[segments*6];
            for(int i=0;i<segments;i++) {float a=i*Mathf.PI*2/segments;vertices[i*2]=new Vector3(Mathf.Cos(a)*(radius-width),.035f,Mathf.Sin(a)*(radius-width));vertices[i*2+1]=new Vector3(Mathf.Cos(a)*(radius+width),.035f,Mathf.Sin(a)*(radius+width));int n=(i+1)%segments;int j=i*6;triangles[j]=i*2;triangles[j+1]=n*2;triangles[j+2]=i*2+1;triangles[j+3]=i*2+1;triangles[j+4]=n*2;triangles[j+5]=n*2+1;}
            mesh.vertices=vertices;mesh.triangles=triangles;mesh.RecalculateNormals();g.AddComponent<MeshFilter>().sharedMesh=mesh;g.AddComponent<MeshRenderer>().sharedMaterial=material;return g.transform;
        }
        void Point(Transform parent,Vector3 pos,Color color,float intensity,float range)
        {
            var l=new GameObject("Lantern light").AddComponent<Light>();l.transform.SetParent(parent,false);l.transform.localPosition=pos;l.type=LightType.Point;l.color=color;l.intensity=intensity;l.range=range;
        }
        void Lantern(Transform parent,Vector3 p)
        {
            Part("Lantern plinth",PrimitiveType.Cylinder,parent,p+Vector3.up*.3f,new Vector3(.8f,.3f,.8f),dark);
            Part("Lantern flame",PrimitiveType.Sphere,parent,p+Vector3.up*.85f,new Vector3(.22f,.7f,.22f),brass);
            Point(parent,p+Vector3.up*1.1f,new Color(1,.48f,.16f),3.2f,7);
        }
        void MakeArena()
        {
            Part("Foundation",PrimitiveType.Cylinder,arena,new Vector3(0,-.65f,0),new Vector3(23,.6f,23),dark);
            Part("Ritual floor",PrimitiveType.Cylinder,arena,new Vector3(0,-.11f,0),new Vector3(21,.12f,21),ground);
            Ring("Gilded outer ring",arena,Vector3.zero,10.1f,.055f,brass);Ring("Inner inscription",arena,Vector3.zero,6,.028f,glow);Ring("Boss seal",arena,new Vector3(0,0,1),2,.045f,brass);
            for(int i=0;i<12;i++)
            {
                float a=i*Mathf.PI/6;Vector3 p=new Vector3(Mathf.Sin(a)*11,0,Mathf.Cos(a)*11);
                float height=3+(i*7%5)*.55f;
                Part("Broken monolith",PrimitiveType.Cube,arena,p+Vector3.up*height*.5f,new Vector3(1.2f,height,1.2f),stone,new Vector3(0,i*30,0));
                Part("Monolith gold seam",PrimitiveType.Cube,arena,p+Vector3.up*(height-.25f),new Vector3(1.28f,.07f,1.28f),brass,new Vector3(0,i*30,0));
                if(i%2==0)Lantern(arena,p.normalized*9.7f);
                for(int j=0;j<2;j++)Part("Overgrown stone",PrimitiveType.Cube,arena,p+new Vector3(j*.9f,.3f,1),new Vector3(.8f,.6f,.7f),green,new Vector3(6,j*34,13));
            }
            for(int i=0;i<32;i++)
            {
                float a=i*Mathf.PI/16;Vector3 p=new Vector3(Mathf.Cos(a)*15,-.5f,Mathf.Sin(a)*15);
                Part("Distant standing stone",PrimitiveType.Cube,arena,p,new Vector3(1.4f,4+i%5,1.7f),dark,new Vector3(0,i*17,i%3*6));
            }
            for(int i=0;i<24;i++)
            {
                float a=i*Mathf.PI/12;Part("Ritual glyph",PrimitiveType.Cube,arena,new Vector3(Mathf.Cos(a)*7.5f,.018f,Mathf.Sin(a)*7.5f),new Vector3(.07f,.035f,.5f),brass,new Vector3(0,-i*15,0));
            }
            Part("Mist beneath",PrimitiveType.Plane,world,new Vector3(0,-1.5f,0),new Vector3(15,1,15),dark);
            Particles(arena,new Vector3(0,.5f,0),new Color(.4f,.85f,.77f,.7f),.06f,8,9);
        }
        void MakeRefuge()
        {
            Part("Bridge foundation",PrimitiveType.Cube,refuge,new Vector3(0,-.5f,0),new Vector3(22,.8f,11),dark);
            for(int i=0;i<11;i++)Part("Crossing slabs",PrimitiveType.Cube,refuge,new Vector3(-10+i*2,-.04f,0),new Vector3(1.94f,.12f,10),ground);
            for(int i=0;i<8;i++) {Lantern(refuge,new Vector3(-8+i*2.3f,0,4.7f));Part("Broken arch",PrimitiveType.Cube,refuge,new Vector3(-8+i*2.3f,2.2f,-5),new Vector3(.55f,4.4f,.55f),stone);}
            Part("Sanctuary bed",PrimitiveType.Cube,refuge,new Vector3(-4,.35f,-2),new Vector3(2,.7f,2),stone);Ring("Healing sigil",refuge,new Vector3(-4,.73f,-2),1,.04f,glow);Point(refuge,new Vector3(-4,2,-2),new Color(.2f,.8f,.7f),4,6);
            Part("Legacy lectern",PrimitiveType.Cube,refuge,new Vector3(-4,.7f,3),new Vector3(.8f,1.4f,.8f),dark);Part("Book of the Dead",PrimitiveType.Cube,refuge,new Vector3(-4,1.45f,3),new Vector3(1.2f,.12f,.9f),brass,new Vector3(12,0,0));
            Part("Forge anvil",PrimitiveType.Cube,refuge,new Vector3(-1,.7f,2),new Vector3(1.8f,1.4f,.9f),stone);Point(refuge,new Vector3(-1,1,2),new Color(1,.38f,.1f),5,7);Particles(refuge,new Vector3(-1,1.5f,2),new Color(1,.5f,.1f),.04f,9,1);
            for(int i=0;i<3;i++)Part("Cache",PrimitiveType.Cube,refuge,new Vector3(3+i*.5f,.3f,-3),new Vector3(.7f,.6f,.5f),brass,new Vector3(0,i*17,0));
            Part("Exit left",PrimitiveType.Cube,refuge,new Vector3(10,3,-2),new Vector3(.5f,6,.5f),stone);Part("Exit right",PrimitiveType.Cube,refuge,new Vector3(10,3,2),new Vector3(.5f,6,.5f),stone);Part("Exit arch",PrimitiveType.Cube,refuge,new Vector3(10,5.8f,0),new Vector3(.5f,.5f,4.5f),brass);
            Ring("Exit sigil",refuge,new Vector3(9,.02f,0),1.8f,.08f,glow);Point(refuge,new Vector3(9,3,0),new Color(.22f,.95f,.85f),6,8);
            collapseFront=Part("Advancing collapse",PrimitiveType.Cube,refuge,new Vector3(-10,.35f,0),new Vector3(.16f,.7f,11),red);
            collapseVoid=Part("Consumed ground",PrimitiveType.Cube,refuge,new Vector3(-10,-.02f,0),new Vector3(.01f,.2f,11),red);
        }
        void MakeBoss()
        {
            boss=new GameObject("ROOTCROWN / animated construct").transform;boss.SetParent(arena,false);boss.localPosition=new Vector3(0,0,1);
            Part("Bark torso",PrimitiveType.Sphere,boss,new Vector3(0,2.2f,0),new Vector3(2.5f,3,1.6f),green);
            Part("Chest seal",PrimitiveType.Sphere,boss,new Vector3(0,2.2f,-.78f),new Vector3(.65f,.8f,.22f),glow);
            var maskAsset=Resources.Load<GameObject>("RootcrownMask");
            if(maskAsset!=null)
            {
                var mask=Object.Instantiate(maskAsset,boss);mask.name="Blender / carved guardian mask";
                var renderers=mask.GetComponentsInChildren<Renderer>();
                Bounds bounds=renderers[0].bounds;foreach(var renderer in renderers){renderer.sharedMaterial=stone;bounds.Encapsulate(renderer.bounds);}
                // Preserve FBX root conversion and fit imported geometry into the head socket.
                mask.transform.localScale*=1.35f/Mathf.Max(.001f,bounds.size.y);
                bounds=renderers[0].bounds;foreach(var renderer in renderers)bounds.Encapsulate(renderer.bounds);
                mask.transform.position+=boss.TransformPoint(new Vector3(0,3.8f,-.2f))-bounds.center;
            }
            else Part("Face mask",PrimitiveType.Sphere,boss,new Vector3(0,3.8f,-.1f),new Vector3(1.3f,1.2f,1),stone);
            for(int side=-1;side<=1;side+=2)
            {
                Part("Eye",PrimitiveType.Sphere,boss,new Vector3(side*.32f,3.88f,-.57f),new Vector3(.14f,.11f,.1f),glow);
                Part("Root leg",PrimitiveType.Capsule,boss,new Vector3(side*.75f,.8f,0),new Vector3(.7f,1.1f,.7f),dark,new Vector3(0,0,side*-9));
                var arm=new GameObject("Arm joint").transform;arm.SetParent(boss,false);arm.localPosition=new Vector3(side*1.25f,3,0);arms.Add(arm);
                Part("Branch arm",PrimitiveType.Capsule,arm,new Vector3(side*.45f,-.65f,0),new Vector3(.6f,1.1f,.6f),green,new Vector3(0,0,side*32));
                Part("Stone fist",PrimitiveType.Sphere,arm,new Vector3(side*.9f,-1.4f,-.15f),new Vector3(.9f,.95f,.9f),stone);
                for(int k=0;k<3;k++)Part("Root talon",PrimitiveType.Capsule,arm,new Vector3(side*(.6f+k*.25f),-1.85f,-.4f),new Vector3(.12f,.42f,.12f),brass,new Vector3(18,0,side*12));
            }
            crown=new GameObject("Crown / branch skeleton").transform;crown.SetParent(boss,false);crown.localPosition=new Vector3(0,4.2f,0);
            for(int side=-1;side<=1;side+=2)
            {
                for(int j=0;j<3;j++)
                {
                    Part("Antler",PrimitiveType.Capsule,crown,new Vector3(side*(.6f+j*.45f),.5f+j*.55f,.12f),new Vector3(.18f,.6f,.18f),brass,new Vector3(0,0,-side*38));
                    Part("Branch tip",PrimitiveType.Capsule,crown,new Vector3(side*(1+j*.45f),.95f+j*.55f,.12f),new Vector3(.1f,.4f,.1f),green,new Vector3(0,0,side*15));
                }
            }
            Point(boss,new Vector3(0,2,-1),new Color(.2f,.9f,.8f),2,6);
            for(int j=0;j<5;j++)Part("Back root ridge",PrimitiveType.Capsule,boss,new Vector3(0,1.5f+j*.38f,.9f),new Vector3(.25f,.5f,.25f),brass,new Vector3(40+j*6,0,0));
        }
        void Particles(Transform parent,Vector3 p,Color c,float size,float rate,float radius)
        {
            if(System.Array.IndexOf(System.Environment.GetCommandLineArgs(),"--no-particles")>=0)return;
            var g=new GameObject("Drifting embers");g.transform.SetParent(parent,false);g.transform.localPosition=p;
            if(System.Array.IndexOf(System.Environment.GetCommandLineArgs(),"--mesh-embers")>=0){g.AddComponent<DriftingEmbers>().Initialize(size,rate,radius,c,camera);return;}
            var ps=g.AddComponent<ParticleSystem>();var main=ps.main;main.startColor=c;main.startSize=size;main.startLifetime=4;main.startSpeed=.18f;main.maxParticles=100;var emission=ps.emission;emission.rateOverTime=rate;var shape=ps.shape;shape.shapeType=ParticleSystemShapeType.Sphere;shape.radius=radius;
            var m=new Material(Shader.Find("Particles/Standard Unlit"));ownedMaterials.Add(m);m.color=c;ps.GetComponent<ParticleSystemRenderer>().sharedMaterial=m;ps.Play();
        }
        public void RebuildCharacters(World w,Catalog catalog)
        {
            for(int i=0;i<4;i++)
            {
                var body=bodies[i];for(int j=body.childCount-1;j>=0;j--)if(body.GetChild(j)!=auras[i])Object.Destroy(body.GetChild(j).gameObject);
                var a=w.agents[i];var robe=robes[i];
                Part("Cloak",PrimitiveType.Capsule,body,new Vector3(0,.85f,0),new Vector3(.58f,.75f,.45f),robe);
                Part("Head",PrimitiveType.Sphere,body,new Vector3(0,1.65f,0),new Vector3(.38f,.42f,.38f),brass);
                Part("Shoulder armor",PrimitiveType.Cube,body,new Vector3(0,1.28f,0),new Vector3(.75f,.18f,.46f),dark);
                for(int k=-1;k<=1;k+=2)Part("Boot",PrimitiveType.Capsule,body,new Vector3(k*.17f,.25f,0),new Vector3(.18f,.3f,.22f),dark);
                string type=catalog.Item(a.weapon.id).weapon;
                if(type=="Greatsword"||type=="Sword") {Part("Blade",PrimitiveType.Cube,body,new Vector3(.55f,1.05f,-.1f),new Vector3(.15f,1.7f,.075f),stone,new Vector3(0,0,-15));Part("Crossguard",PrimitiveType.Cube,body,new Vector3(.42f,.45f,-.1f),new Vector3(.45f,.08f,.12f),brass);}
                else if(type=="Bow") {Part("Bow",PrimitiveType.Capsule,body,new Vector3(.55f,1.1f,0),new Vector3(.1f,.8f,.15f),brass,new Vector3(0,0,-18));Part("String",PrimitiveType.Cube,body,new Vector3(.65f,1.1f,0),new Vector3(.025f,1.3f,.025f),glow);}
                else {Part("Staff shaft",PrimitiveType.Cylinder,body,new Vector3(.5f,1.05f,0),new Vector3(.09f,1.05f,.09f),brass);Part("Staff crystal",PrimitiveType.Sphere,body,new Vector3(.5f,2.1f,0),new Vector3(.32f,.42f,.32f),glow);}
            }
        }
        public void Update(World w,float dt,float orbit)
        {
            bool battle=w.phase==Phase.Battle||(w.phase==Phase.Ended&&w.boss.hp>0);arena.gameObject.SetActive(battle);refuge.gameObject.SetActive(!battle);angle+=orbit*dt*35;
            Vector3 focus=SpectatorFocus(w,orbit);
            var desired=focus+Quaternion.Euler(0,angle,0)*new Vector3(17,22,-27);
            camera.transform.position=Vector3.Lerp(camera.transform.position,desired,camera.transform.position==Vector3.zero?1:dt*3);camera.transform.LookAt(focus+Vector3.up*.5f);
            float t=Time.time;
            if(battle)
            {
                boss.localPosition=new Vector3(0,.06f*Mathf.Sin(t*1.4f),1);boss.localScale=Vector3.one*(1+.015f*Mathf.Sin(t*1.4f));
                float lift=w.boss.telegraph?Mathf.Clamp01(1-w.boss.windup/1.2f)*-95:Mathf.Sin(t*1.7f)*8;
                for(int i=0;i<arms.Count;i++)arms[i].localRotation=Quaternion.Euler(lift,0,(i==0?1:-1)*5);
                crown.localRotation=Quaternion.Euler(0,Mathf.Sin(t)*4,0);
                warning.gameObject.SetActive(w.boss.telegraph&&(observedGroup==null||observedGroup.boss.cue.shape==CueShape.Circle));warning.position=new Vector3(w.boss.targetX,.06f,w.boss.targetZ);warning.localScale=Vector3.one*(observedGroup!=null?observedRadius/3.2f:w.boss.enraged?1.25f:1);
                shock.gameObject.SetActive(!w.boss.telegraph&&w.boss.timer>2.9f);shock.position=warning.position;shock.localScale=Vector3.one*(1+(3.6f-w.boss.timer)*2);
            }
            for(int i=0;i<4;i++)
            {
                var a=w.agents[i];bodies[i].gameObject.SetActive(!a.escaped&&(observedGroup==null||observedGroup.members.Contains(i)));
                bodies[i].position=new Vector3(a.x,a.alive?.05f*Mathf.Sin(t*3+i):.1f,a.z);
                bodies[i].rotation=a.alive?Quaternion.Euler(0,battle?Mathf.Atan2(-a.x,1-a.z)*Mathf.Rad2Deg:90,0):Quaternion.Euler(0,0,85);
                auras[i].gameObject.SetActive(a.alive);auras[i].localScale=Vector3.one*(1+.06f*Mathf.Sin(t*2+i));
                bool present=observedGroup==null||observedGroup.members.Contains(i);
                if(battle&&present&&a.damage>previousDamage[i]) AddEffect(new Vector3(a.x,1.4f,a.z),new Vector3(observedGroup?.boss.x??0,2,observedGroup?.boss.z??1),robes[i]);
                if(battle&&present&&a.healing>previousHealing[i])
                {
                    var target=a.intent.target>=0?w.agents[a.intent.target]:a;AddEffect(new Vector3(a.x,1.4f,a.z),new Vector3(target.x,1.4f,target.z),glow);
                }
                previousDamage[i]=a.damage;previousHealing[i]=a.healing;
            }
            for(int i=effects.Count-1;i>=0;i--)
            {
                var e=effects[i];e.left-=dt;e.visual.position=Vector3.Lerp(e.from,e.to,1-e.left/.35f);e.visual.localScale=Vector3.one*(.15f+.1f*Mathf.Sin((1-e.left/.35f)*Mathf.PI));
                if(e.left<=0){e.visual.gameObject.SetActive(false);effects.RemoveAt(i);effectPool.Push(e);}
            }
            if(!battle)
            {
                float collapse=-10+Mathf.Max(0,w.phaseClock-observedRestLimit)*2.3f;
                collapseFront.gameObject.SetActive(collapse>-10);collapseVoid.gameObject.SetActive(collapse>-10);
                collapseFront.localPosition=new Vector3(collapse,.35f,0);collapseVoid.localPosition=new Vector3((-10+collapse)*.5f,.01f,0);collapseVoid.localScale=new Vector3(Mathf.Max(.01f,collapse+10),.2f,11);
                if(collapse>-10) camera.backgroundColor=new Color(.12f,.04f,.04f);else camera.backgroundColor=new Color(.035f,.065f,.08f);
            }
        }
        void AddEffect(Vector3 from,Vector3 to,Material material)
        {
            if(noHitFx||effects.Count>=64)return;EffectSerial++;
            var effect=effectPool.Count>0?effectPool.Pop():new Effect{visual=Part("Pooled impact mote",PrimitiveType.Sphere,world,from,Vector3.one*.2f,material)};
            effect.from=from;effect.to=to;effect.left=.35f;effect.visual.gameObject.SetActive(true);effect.visual.GetComponent<Renderer>().sharedMaterial=material;effects.Add(effect);
        }
        public Vector3 Screen(Agent a) => camera.WorldToScreenPoint(new Vector3(a.x,2.5f,a.z));
    }
}
