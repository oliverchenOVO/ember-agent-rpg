using System.Collections.Generic;
using Ember.Core.Phase2;
using UnityEngine;
namespace Ember.Presentation
{
    public sealed partial class WorldView
    {
        sealed class ArtRig
        {
            public Transform root,head,left,right,core;public readonly List<Transform> rotors=new List<Transform>();
            public string id,archetype;public float strike,dash;public Vector3 dashFrom,coreScale;public Quaternion coreRotation;
        }
        readonly Dictionary<string,ArtRig> artRigs=new Dictionary<string,ArtRig>();ArtRig artRig;
        void EnsureArtMaterials()
        {
            if(bark==null){bark=Mat(new Color(.28f,.16f,.095f),.05f,.35f);bone=Mat(new Color(.8f,.75f,.58f),.12f,.45f);steel=Mat(new Color(.37f,.46f,.52f),.78f,.65f);iceArt=Mat(new Color(.35f,.77f,.95f),.35f,.8f,.65f);soulArt=Mat(new Color(.5f,.3f,.85f),.15f,.6f,1.3f);}
        }
        Material bark,bone,steel,iceArt,soulArt;TowerContent artData;
        public int DetailedBossCount=>artRigs.Count;
        public int DetailedBossMeshCount=>artRig==null?0:artRig.root.GetComponentsInChildren<MeshFilter>(true).Length;
        Transform Pivot(string name,Transform parent,Vector3 position)
        {var t=new GameObject(name).transform;t.SetParent(parent,false);t.localPosition=position;return t;}
        Transform Tube(string name,Transform parent,Vector3[] path,float start,float end,Material material,int sides=10)
        {
            var vertices=new Vector3[path.Length*sides];var uv=new Vector2[vertices.Length];var indices=new int[(path.Length-1)*sides*6];
            for(int i=0;i<path.Length;i++)
            {
                var tangent=path[Mathf.Min(path.Length-1,i+1)]-path[Mathf.Max(0,i-1)];var q=Quaternion.FromToRotation(Vector3.forward,tangent.normalized);float radius=Mathf.Lerp(start,end,(float)i/(path.Length-1));
                for(int j=0;j<sides;j++){float a=j*Mathf.PI*2/sides;float ridge=1+.07f*Mathf.Sin(j*3+i*.6f);vertices[i*sides+j]=path[i]+q*new Vector3(Mathf.Cos(a)*radius*ridge,Mathf.Sin(a)*radius*ridge,0);uv[i*sides+j]=new Vector2((float)j/sides,(float)i/(path.Length-1));}
                if(i==path.Length-1)continue;
                for(int j=0;j<sides;j++){int k=(i*sides+j)*6,n=(j+1)%sides;indices[k]=i*sides+j;indices[k+1]=(i+1)*sides+j;indices[k+2]=i*sides+n;indices[k+3]=i*sides+n;indices[k+4]=(i+1)*sides+j;indices[k+5]=(i+1)*sides+n;}
            }
            for(int i=0;i<indices.Length;i+=3){int swap=indices[i+1];indices[i+1]=indices[i+2];indices[i+2]=swap;}
            var mesh=new Mesh{name=name};mesh.vertices=vertices;mesh.uv=uv;mesh.triangles=indices;mesh.RecalculateNormals();mesh.RecalculateBounds();ownedMeshes.Add(mesh);
            var t=Pivot(name,parent,Vector3.zero);t.gameObject.AddComponent<MeshFilter>().sharedMesh=mesh;t.gameObject.AddComponent<MeshRenderer>().sharedMaterial=material;return t;
        }
        Transform Crystal(string name,Transform parent,Vector3 p,Vector3 size,Material material)
        {
            var mesh=new Mesh{name=name};var v=new Vector3[14];v[0]=Vector3.up*.65f;v[13]=Vector3.down*.6f;
            for(int j=0;j<6;j++){float a=j*Mathf.PI/3;v[1+j]=new Vector3(Mathf.Cos(a)*.5f,.25f,Mathf.Sin(a)*.5f);v[7+j]=new Vector3(Mathf.Cos(a)*.4f,-.3f,Mathf.Sin(a)*.4f);}
            var tr=new List<int>();for(int j=0;j<6;j++){int n=(j+1)%6;tr.AddRange(new[]{0,1+j,1+n,1+j,7+j,1+n,1+n,7+j,7+n,13,7+n,7+j});}
            for(int i=0;i<tr.Count;i+=3){int swap=tr[i+1];tr[i+1]=tr[i+2];tr[i+2]=swap;}
            mesh.vertices=v;mesh.triangles=tr.ToArray();mesh.RecalculateNormals();ownedMeshes.Add(mesh);var t=Pivot(name,parent,p);t.localScale=size;t.gameObject.AddComponent<MeshFilter>().sharedMesh=mesh;t.gameObject.AddComponent<MeshRenderer>().sharedMaterial=material;return t;
        }
        Transform Gear(Transform parent,Vector3 p,float radius,Material material)
        {
            var t=Pivot("Machined gear",parent,p);var ring=Ring("Gear rim",t,Vector3.zero,radius,.13f,material,40);ring.localRotation=Quaternion.Euler(90,0,0);
            for(int j=0;j<14;j++){float a=j*Mathf.PI/7;Part("Gear tooth",PrimitiveType.Cube,t,new Vector3(Mathf.Cos(a)*radius,Mathf.Sin(a)*radius,0),new Vector3(.22f,.33f,.24f),material,new Vector3(0,0,j*180f/7));}
            Part("Gear hub",PrimitiveType.Sphere,t,Vector3.zero,Vector3.one*.3f,steel);return t;
        }
        void ArtEyes(Transform parent,float width,float y,float z,Material material)
        {for(int s=-1;s<=1;s+=2)Part("Luminous eye",PrimitiveType.Sphere,parent,new Vector3(s*width,y,z),new Vector3(.18f,.13f,.12f),material);}
        void CrownBranches(Transform parent,int branches,Material material,float height=1)
        {
            for(int i=0;i<branches;i++){float a=i*Mathf.PI*2/branches;var d=new Vector3(Mathf.Cos(a),0,Mathf.Sin(a));Tube("Forked crown antler",parent,new[]{d*.25f,d*.65f+Vector3.up*.5f*height,d*1.25f+Vector3.up*1.25f*height,d*1.1f+Vector3.up*1.8f*height},.13f,.015f,material);Tube("Antler fork",parent,new[]{d*.65f+Vector3.up*.5f*height,d*1.25f+Vector3.up*.8f*height,d*1.5f+Vector3.up*1.1f*height},.08f,.005f,material);}
        }
        ArtRig BuildArtRig(BossDefinition def)
        {
            EnsureArtMaterials();
            var r=new ArtRig{root=Pivot("Detailed Boss / "+def.id,arena,Vector3.zero),id=def.id,archetype=def.archetype};
            if(def.archetype=="stoker"||def.archetype=="railjudge"||def.archetype=="weavemother"||def.archetype=="metronome"||def.archetype=="armillary")MachineSculpture(r);
            else if(def.id=="thornweaver")ThornSpiderSculpture(r);
            else if(def.id=="grimoire")BookSculpture(r);
            else if(def.id=="hellgate")GateSculpture(r);
            else if(def.id=="stilldragon")DragonSculpture(r);
            else if(def.id=="falsesaint")SaintSculpture(r);
            else if(def.archetype=="charger")BeastSculpture(r);
            else if(def.archetype=="summoner")CoilSculpture(r);
            else if(def.archetype=="environment")HeartSculpture(r);
            else TreeSculpture(r,def.archetype=="caster");
            if(r.core==null)r.core=Part("Living core",PrimitiveType.Sphere,r.root,new Vector3(0,2.3f,-.5f),Vector3.one*.4f,glow);
            r.coreScale=r.core.localScale;r.coreRotation=r.core.localRotation;return r;
        }
        void ThornSpiderSculpture(ArtRig r)
        {
            Part("Thorn silk abdomen",PrimitiveType.Sphere,r.root,new Vector3(0,2.1f,.7f),new Vector3(2.3f,2.1f,2.5f),bark);
            r.head=Part("Spider queen mask",PrimitiveType.Sphere,r.root,new Vector3(0,2.4f,-1),new Vector3(1.4f,1.2f,1),bone);ArtEyes(r.head,.28f,.15f,-.45f,soulArt);ArtEyes(r.head,.48f,.36f,-.34f,soulArt);
            for(int s=-1;s<=1;s+=2)for(int j=0;j<4;j++){var leg=Pivot("Thorn leg articulation",r.root,new Vector3(s*.8f,2.4f,-.8f+j*.5f));if(j==0){if(s<0)r.left=leg;else r.right=leg;}Tube("Curved eight-leg thorn",leg,new[]{Vector3.zero,new Vector3(s*1.1f,.8f,-.7f+j*.3f),new Vector3(s*2,-.4f,-1+j*.65f),new Vector3(s*2.2f,-2.4f,-1.4f+j*.8f)},.22f,.018f,bark);for(int k=0;k<3;k++)Crystal("Leg thorn",leg,new Vector3(s*(.5f+k*.45f),.6f-k*.3f,-.4f+j*.25f),new Vector3(.15f,.5f,.15f),bone);}
            CrownBranches(r.head,3,bark,.4f);r.core=Crystal("Thorn brood heart",r.root,new Vector3(0,2.5f,-1.6f),new Vector3(.45f,.7f,.25f),soulArt);
            for(int j=0;j<7;j++)Crystal("Layered thorn carapace",r.root,new Vector3(0,3,.1f+j*.3f),new Vector3(.8f,.75f,.4f),dark);
        }
        void TreeSculpture(ArtRig r,bool caster)
        {
            var root=r.root;bool volcanic=r.id=="lavabeast"||r.id=="ashheart",headless=r.id=="headless",rift=r.id=="riftfiend";var wood=volcanic?dark:bark;var light=volcanic?red:rift?soulArt:glow;
            Tube("Ridged ancient trunk",root,new[]{new Vector3(0,.6f,.15f),new Vector3(0,1.4f,0),new Vector3(-.12f,2.3f,0),new Vector3(.1f,3.05f,0),new Vector3(0,3.5f,0)},.85f,.48f,wood,16);
            for(int j=0;j<8;j++){float a=j*Mathf.PI/4;var d=new Vector3(Mathf.Cos(a),0,Mathf.Sin(a));Tube("Gnarled grounding root",root,new[]{d*.55f+Vector3.up*.7f,d*1.15f+Vector3.up*.32f,d*1.8f+Vector3.up*.08f,d*2.35f+Vector3.up*.04f},.3f,.025f,wood);}
            for(int s=-1;s<=1;s+=2)
            {
                var arm=Pivot("Articulated branch shoulder",root,new Vector3(s*.8f,2.85f,0));if(s<0)r.left=arm;else r.right=arm;
                Tube("Muscular root arm",arm,new[]{Vector3.zero,new Vector3(s*.45f,-.4f,0),new Vector3(s*.85f,-1.05f,-.2f)},.31f,.21f,wood);
                for(int k=0;k<4;k++)Tube("Curved root finger",arm,new[]{new Vector3(s*.85f,-1,-.2f),new Vector3(s*(.6f+k*.2f),-1.4f,-.3f),new Vector3(s*(.6f+k*.23f),-1.75f,-.55f)},.08f,.008f,bone);
                for(int j=0;j<5;j++)Crystal("Layered bark plate",root,new Vector3(s*(.48f+j*.04f),1.2f+j*.4f,-.55f),new Vector3(.4f,.8f,.16f),volcanic?stone:wood);
            }
            r.core=Crystal("Ember heart" ,root,new Vector3(0,2.25f,-.76f),new Vector3(.75f,1,.3f),light);
            r.head=Pivot("Head socket",root,new Vector3(0,3.5f,-.03f));
            if(!headless)
            {
                Part("Carved skull",PrimitiveType.Sphere,r.head,new Vector3(0,.36f,-.1f),new Vector3(1.05f,1.1f,.82f),bone);ArtEyes(r.head,.25f,.46f,-.52f,light);
                Tube("Jaw frame",r.head,new[]{new Vector3(-.45f,.28f,-.46f),new Vector3(-.3f,-.05f,-.55f),new Vector3(0,-.22f,-.53f),new Vector3(.3f,-.05f,-.55f),new Vector3(.45f,.28f,-.46f)},.08f,.08f,wood);
                if(r.id=="rootcrown"||r.id=="thornweaver")CrownBranches(r.head,7,wood,caster?.7f:1);
                else CrownBranches(r.head,rift?2:5,volcanic?red:bone,.7f);
            }
            else {Ring("Broken neck socket",r.head,new Vector3(0,.12f,0),.48f,.12f,bone);for(int j=0;j<6;j++)Crystal("Broken neck splinter",r.head,new Vector3(Mathf.Cos(j)*.4f,.2f,Mathf.Sin(j)*.4f),new Vector3(.13f,.45f,.13f),bone);}
            if(caster){var focus=Pivot("Thorn spell spindle",root,new Vector3(0,1.9f,-1.8f));r.rotors.Add(focus);for(int j=0;j<6;j++){float a=j*Mathf.PI/3;Crystal("Floating thorn needle",focus,new Vector3(Mathf.Cos(a)*.9f,0,Mathf.Sin(a)*.9f),new Vector3(.15f,.7f,.15f),light);}}
        }
        void BeastSculpture(ArtRig r)
        {
            var root=r.root;bool machine=r.id=="clockwork",chimera=r.id=="chimera",king=r.id=="voidking";var skin=machine?steel:king?dark:bone;
            Part("Long ribcage",PrimitiveType.Capsule,root,new Vector3(0,1.5f,.25f),new Vector3(1.45f,1.75f,1.4f),skin,new Vector3(90,0,0));
            for(int s=-1;s<=1;s+=2)for(int j=0;j<2;j++){float z=j==0?-1.05f:1.15f;var hip=Pivot("Beast leg hinge",root,new Vector3(s*.65f,1.6f,z));if(j==0){if(s<0)r.left=hip;else r.right=hip;}Tube("Bent hunting leg",hip,new[]{Vector3.zero,new Vector3(s*.2f,-.65f,.25f),new Vector3(s*.12f,-1.25f,-.12f)},.23f,.12f,skin);Part("Clawed paw",PrimitiveType.Sphere,hip,new Vector3(s*.12f,-1.4f,-.25f),new Vector3(.45f,.25f,.6f),dark);for(int k=0;k<3;k++)Crystal("Claw",hip,new Vector3(s*.12f+(k-1)*.13f,-1.42f,-.58f),new Vector3(.1f,.32f,.1f),bone).localRotation=Quaternion.Euler(80,0,0);}
            r.head=Pivot("Predator neck",root,new Vector3(0,2,-1.2f));Part("Wolf skull",PrimitiveType.Sphere,r.head,Vector3.zero,new Vector3(1.1f,.85f,1.1f),skin);Part("Snout",PrimitiveType.Capsule,r.head,new Vector3(0,-.15f,-.65f),new Vector3(.7f,.7f,.55f),skin,new Vector3(90,0,0));ArtEyes(r.head,.36f,.17f,-.48f,king?soulArt:glow);
            for(int s=-1;s<=1;s+=2){Crystal("Moon fang",r.head,new Vector3(s*.28f,-.35f,-.82f),new Vector3(.14f,.55f,.14f),bone);Crystal("Pointed ear",r.head,new Vector3(s*.4f,.62f,.15f),new Vector3(.38f,.7f,.25f),skin);}
            for(int j=0;j<7;j++)Crystal("Dorsal scale",root,new Vector3(0,2.3f,-.8f+j*.35f),new Vector3(.38f,.65f,.26f),machine?brass:king?soulArt:stone);
            Tube("Whip tail",root,new[]{new Vector3(0,1.7f,1.6f),new Vector3(.5f,1.5f,2.2f),new Vector3(1.1f,1.1f,2.6f),new Vector3(1.5f,1.6f,2.8f)},.22f,.015f,skin);
            if(machine){r.rotors.Add(Gear(root,new Vector3(-.82f,1.7f,0),.6f,brass));r.rotors.Add(Gear(root,new Vector3(.82f,1.7f,0),.6f,brass));}
            if(chimera)for(int s=-1;s<=1;s+=2){var h=Part("Secondary chimera skull",PrimitiveType.Sphere,r.head,new Vector3(s*.95f,.15f,.3f),new Vector3(.75f,.7f,.9f),s<0?bark:green);ArtEyes(h,.22f,.12f,-.4f,red);CrownBranches(h,2,bone,.45f);}
            if(king){CrownBranches(r.head,6,brass,.6f);Ring("Royal void halo",root,new Vector3(0,.06f,0),2.7f,.08f,soulArt);}
        }
        void CoilSculpture(ArtRig r)
        {
            bool chimera=r.id=="endchimera",eater=r.id=="souleater";var root=r.root;var path=new Vector3[41];for(int j=0;j<41;j++){float a=j*.24f;float radius=1.8f-j*.023f;path[j]=new Vector3(Mathf.Cos(a)*radius,.15f+j*.062f,Mathf.Sin(a)*radius);}
            Tube("Continuous swamp serpent",root,path,.35f,.24f,eater?dark:green,12);r.head=Pivot("Coiled brood head",root,new Vector3(Mathf.Cos(9.6f)*.85f,2.7f,Mathf.Sin(9.6f)*.85f));
            Part("Split serpent skull",PrimitiveType.Sphere,r.head,new Vector3(0,.4f,0),new Vector3(1.5f,1.3f,1.15f),bone);ArtEyes(r.head,.4f,.6f,-.58f,eater?soulArt:red);
            for(int j=0;j<8;j++){float a=j*Mathf.PI/4;Crystal("Skull coronet",r.head,new Vector3(Mathf.Cos(a)*.65f,.65f,Mathf.Sin(a)*.55f),new Vector3(.15f,.75f,.15f),bone);}
            r.left=Pivot("Brood claw left",root,new Vector3(-1.1f,2.2f,-.5f));r.right=Pivot("Brood claw right",root,new Vector3(1.1f,2.2f,-.5f));
            foreach(var arm in new[]{r.left,r.right}){float s=arm==r.left?-1:1;Tube("Serpent claw",arm,new[]{Vector3.zero,new Vector3(s*.8f,-.4f,-.25f),new Vector3(s*1.15f,-1.1f,-.8f)},.13f,.008f,bone);}
            for(int j=0;j<6;j++){float a=j*Mathf.PI/3;Part("Brood egg",PrimitiveType.Sphere,root,new Vector3(Mathf.Cos(a)*2.2f,.28f,Mathf.Sin(a)*2.2f),new Vector3(.4f,.55f,.4f),eater?soulArt:glow);}
            if(chimera)for(int s=-1;s<=1;s+=2){Part("Conjoined skull",PrimitiveType.Sphere,r.head,new Vector3(s*.9f,.3f,.3f),Vector3.one*.8f,bone);Tube("Conjoined horn",r.head,new[]{new Vector3(s*.8f,.7f,.3f),new Vector3(s*1.1f,1.2f,.2f),new Vector3(s*1.3f,1.5f,0)},.12f,.01f,bone);}
        }
        void HeartSculpture(ArtRig r)
        {
            var root=r.root;bool witness=r.id=="witness",meteor=r.id=="starfall";r.core=Part(witness?"Witness iris":"Living heart",PrimitiveType.Sphere,root,new Vector3(0,2.8f,0),new Vector3(1.6f,1.9f,1.35f),meteor?red:glow);
            if(witness){Part("Dark pupil",PrimitiveType.Sphere,r.core,new Vector3(0,0,-.5f),new Vector3(.4f,.6f,.14f),dark);for(int j=0;j<3;j++){var ring=Ring("Celestial witness halo",root,new Vector3(0,2.8f,0),2+j*.35f,.06f,brass);ring.localRotation=Quaternion.Euler(50+j*30,j*45,0);r.rotors.Add(ring);}}
            else for(int j=0;j<7;j++){float a=j*Mathf.PI*2/7;var d=new Vector3(Mathf.Cos(a),0,Mathf.Sin(a));if(meteor){var rotor=Pivot("Meteor orbit",root,Vector3.up*2.7f);Crystal("Fallen star fragment",rotor,d*2.1f+Vector3.up*(j%3*.3f),new Vector3(.8f,1.7f,.7f),j%2==0?stone:red);r.rotors.Add(rotor);}else{Tube("Heartwood rib",root,new[]{d*2.4f,d*1.4f+Vector3.up*1.5f,d*1.1f+Vector3.up*3.2f,d*.7f+Vector3.up*4.2f},.28f,.04f,bark);Tube("Heartwood crown",root,new[]{d*.7f+Vector3.up*4.2f,d*1.5f+Vector3.up*4.8f,d*2.4f+Vector3.up*4.6f},.15f,.005f,bark);}}
            if(witness||meteor)for(int j=0;j<12;j++){float a=j*Mathf.PI/6;Crystal("Orbit inscription shard",root,new Vector3(Mathf.Cos(a)*2.7f,2.8f,Mathf.Sin(a)*2.7f),new Vector3(.12f,.4f,.12f),brass);}
            Ring("Heart seal",root,new Vector3(0,.06f,0),2.6f,.08f,brass);
        }
        void BookSculpture(ArtRig r)
        {
            var root=r.root;for(int s=-1;s<=1;s+=2){var wing=Pivot("Hinged grimoire cover",root,new Vector3(0,2.5f,0));wing.localRotation=Quaternion.Euler(0,0,s*-14);if(s<0)r.left=wing;else r.right=wing;Part("Leather cover",PrimitiveType.Cube,wing,new Vector3(s*.85f,0,0),new Vector3(1.65f,.2f,2.1f),bark);for(int j=0;j<8;j++)Part("Layered paper page",PrimitiveType.Cube,wing,new Vector3(s*.82f,.12f+j*.025f,0),new Vector3(1.5f,.015f,1.95f),bone);for(int j=0;j<4;j++)Part("Illuminated script",PrimitiveType.Cube,wing,new Vector3(s*.82f,.35f,-.65f+j*.4f),new Vector3(.9f,.025f,.035f),soulArt);for(int j=-1;j<=1;j+=2)Part("Gilt book corner",PrimitiveType.Cube,wing,new Vector3(s*1.55f,.12f,j*.95f),new Vector3(.22f,.4f,.25f),brass);}
            r.core=Crystal("Grimoire binding eye",root,new Vector3(0,2.7f,-.8f),new Vector3(.4f,.7f,.3f),soulArt);CrownBranches(Pivot("Binding tendrils",root,new Vector3(0,1,0)),6,dark,.65f);
        }
        void GateSculpture(ArtRig r)
        {
            for(int s=-1;s<=1;s+=2){Tube("Twisted portal pillar",r.root,new[]{new Vector3(s*1.8f,0,0),new Vector3(s*1.7f,1.5f,0),new Vector3(s*1.6f,3.5f,0),new Vector3(s*.75f,4.8f,0),new Vector3(0,5.1f,0)},.4f,.25f,dark,14);for(int j=0;j<6;j++)Crystal("Portal thorn",r.root,new Vector3(s*1.8f,.4f+j*.6f,-.2f),new Vector3(.4f,.6f,.4f),red);}
            r.core=Part("Hellgate throat",PrimitiveType.Sphere,r.root,new Vector3(0,2.5f,0),new Vector3(2.4f,4.1f,.18f),soulArt);var rim=Ring("Vertical portal rim",r.root,new Vector3(0,2.5f,-.12f),1.7f,.12f,red);rim.localRotation=Quaternion.Euler(90,0,0);rim.localScale=new Vector3(1,1,1.25f);r.rotors.Add(rim);
        }
        void SaintSculpture(ArtRig r)
        {
            Part("Saint altar",PrimitiveType.Cylinder,r.root,new Vector3(0,.3f,0),new Vector3(3,.3f,3),dark);Tube("Cracked marble vestment",r.root,new[]{new Vector3(0,.6f,0),new Vector3(0,1.6f,0),new Vector3(0,2.8f,0)},.8f,.4f,bone,14);r.head=Part("False saint mask",PrimitiveType.Sphere,r.root,new Vector3(0,3.4f,0),new Vector3(.75f,1,.65f),bone);ArtEyes(r.head,.2f,.05f,-.44f,red);
            for(int s=-1;s<=1;s+=2){var wing=Pivot("Shattered saint wing",r.root,new Vector3(s*.6f,2.7f,.4f));if(s<0)r.left=wing;else r.right=wing;for(int j=0;j<7;j++)Crystal("Stone wing feather",wing,new Vector3(s*(.4f+j*.22f),.8f-j*.12f,.2f),new Vector3(.2f,1.8f-j*.12f,.2f),bone).localRotation=Quaternion.Euler(0,0,s*-30);}
            var halo=Ring("Broken saint halo",r.root,new Vector3(0,4.3f,0),.9f,.1f,brass);halo.localRotation=Quaternion.Euler(70,0,0);r.rotors.Add(halo);
        }
        void DragonSculpture(ArtRig r)
        {
            BeastSculpture(r);for(int s=-1;s<=1;s+=2){var wing=Pivot("Dragon wing shoulder",r.root,new Vector3(s*.5f,2,.6f));if(s<0)r.left=wing;else r.right=wing;for(int j=0;j<4;j++){Tube("Dragon wing spar",wing,new[]{Vector3.zero,new Vector3(s*1.9f,1.8f,.3f),new Vector3(s*(3-j*.3f),.6f,1+j*.6f)},.12f,.01f,bone);Tube("Wing membrane ridge",wing,new[]{new Vector3(s*1.9f,1.8f,.3f),new Vector3(s*2.1f,1,.7f+j*.5f),new Vector3(s*(3-j*.3f),.6f,1+j*.6f)},.25f,.08f,iceArt);}}
        }
        void MachineSculpture(ArtRig r)
        {
            var root=r.root;string id=r.archetype;
            if(id=="armillary")
            {
                r.core=Part("Incandescent astral heart",PrimitiveType.Sphere,root,new Vector3(0,2.8f,0),Vector3.one*1.45f,whiteHot);
                for(int j=0;j<4;j++){var frame=Pivot("Gimballed celestial ring",root,new Vector3(0,2.8f,0));frame.localRotation=Quaternion.Euler(j*43+15,j*51,20);r.rotors.Add(frame);Ring("Engraved orbital band",frame,Vector3.zero,1.9f+j*.28f,.1f,j==1?glow:brass,64);for(int k=0;k<12;k++){float a=k*Mathf.PI/6;Part("Orbital tick",PrimitiveType.Cube,frame,new Vector3(Mathf.Cos(a)*(1.9f+j*.28f),.04f,Mathf.Sin(a)*(1.9f+j*.28f)),new Vector3(.12f,.18f,.28f),steel,new Vector3(0,-k*30,0));}}
                for(int j=0;j<4;j++){float a=j*Mathf.PI/2;Tube("Armillary suspension",root,new[]{new Vector3(Mathf.Cos(a)*2.8f,0,Mathf.Sin(a)*2.8f),new Vector3(Mathf.Cos(a)*1.6f,1.3f,Mathf.Sin(a)*1.6f),new Vector3(0,2,0)},.16f,.08f,steel);}
            }
            else if(id=="weavemother")
            {
                r.core=Crystal("Faceted crystal brood",root,new Vector3(0,2.6f,0),new Vector3(1.8f,2.7f,1.8f),soulArt);for(int j=0;j<8;j++){float a=j*Mathf.PI/4;var d=new Vector3(Mathf.Cos(a),0,Mathf.Sin(a));var leg=Pivot("Loom leg hinge",root,d*.7f+Vector3.up*2.8f);if(j==1)r.left=leg;if(j==5)r.right=leg;Tube("Articulated spider loom",leg,new[]{Vector3.zero,d*1.5f+Vector3.up*.8f,d*2.5f-Vector3.up*.6f,d*2.8f-Vector3.up*2.8f},.16f,.045f,steel);Part("Loom hinge cap",PrimitiveType.Sphere,leg,d*1.5f+Vector3.up*.8f,Vector3.one*.32f,brass);Crystal("Brood crystal egg",root,d*2+Vector3.up*.3f,new Vector3(.4f,.65f,.4f),glow);}
                Ring("Weaving shuttle",root,new Vector3(0,1.8f,0),1.5f,.06f,glow);
            }
            else if(id=="metronome")
            {
                for(int s=-1;s<=1;s+=2){Tube("Clock tower frame",root,new[]{new Vector3(s*1.8f,0,0),new Vector3(s*1.5f,3,0),new Vector3(0,4.2f,0)},.18f,.14f,brass);r.rotors.Add(Gear(root,new Vector3(s*.9f,2.5f,-.15f),.8f,brass));}
                r.left=Pivot("Pendulum fulcrum",root,new Vector3(0,3.8f,0));Tube("Hanging pendulum",r.left,new[]{Vector3.zero,new Vector3(0,-1.2f,0),new Vector3(0,-2.7f,0)},.12f,.12f,steel);Part("Clock hammer weight",PrimitiveType.Cube,r.left,new Vector3(0,-2.7f,0),new Vector3(1.6f,.6f,.8f),dark);r.core=Part("Cracked clock face",PrimitiveType.Cylinder,root,new Vector3(0,3,-.4f),new Vector3(1.5f,.12f,1.5f),bone,new Vector3(90,0,0));
                for(int j=0;j<12;j++){float a=j*Mathf.PI/6;Part("Clock numeral mark",PrimitiveType.Cube,root,new Vector3(Mathf.Sin(a)*.63f,3+Mathf.Cos(a)*.63f,-.56f),new Vector3(.06f,.16f,.025f),brass,new Vector3(0,0,-j*30));}
            }
            else
            {
                bool stoker=id=="stoker";Part("Riveted armored chassis",PrimitiveType.Cube,root,new Vector3(0,stoker?2:.9f,0),stoker?new Vector3(2.1f,2.5f,1.3f):new Vector3(3.1f,1.2f,2),dark);
                for(int s=-1;s<=1;s+=2){var arm=Pivot(stoker?"Furnace piston shoulder":"Judicial blade hinge",root,new Vector3(s*(stoker?1.1f:1.4f),stoker?2.8f:1.7f,0));if(s<0)r.left=arm;else r.right=arm;
                    Tube("Hydraulic arm",arm,new[]{Vector3.zero,new Vector3(s*.65f,-.4f,0),new Vector3(s*1.2f,-1.2f,-.3f)},.22f,.17f,steel);Part("Piston joint",PrimitiveType.Sphere,arm,new Vector3(s*.65f,-.4f,0),Vector3.one*.5f,brass);
                    for(int k=-1;k<=1;k+=2)Tube(stoker?"Furnace tong jaw":"Execution blade",arm,new[]{new Vector3(s*1.2f,-1.1f,-.3f),new Vector3(s*1.55f,-1.7f,k*.3f-.4f),new Vector3(s*1.1f,-2,k*.2f-.6f)},stoker?.12f:.22f,.015f,stoker?steel:bone);
                    for(int j=0;j<2;j++){var gear=Gear(root,new Vector3(s*1.65f,.6f,j==0?-.75f:.75f),.48f,brass);r.rotors.Add(gear);}
                    for(int j=0;j<5;j++)Part("Armor rivet",PrimitiveType.Sphere,root,new Vector3(s*.95f,(stoker?1.1f:.5f)+j*.3f,-.69f),Vector3.one*.09f,brass);
                }
                if(stoker){r.core=Part("Furnace hearth",PrimitiveType.Sphere,root,new Vector3(0,1.9f,-.74f),new Vector3(1.25f,1.3f,.18f),red);for(int j=-2;j<=2;j++)Part("Hearth grate",PrimitiveType.Cylinder,root,new Vector3(j*.2f,1.9f,-.9f),new Vector3(.05f,.65f,.05f),steel);for(int s=-1;s<=1;s+=2){Tube("Smokestack",root,new[]{new Vector3(s*.65f,3,0),new Vector3(s*.65f,3.7f,0),new Vector3(s*.65f,4.4f,.2f)},.22f,.2f,steel);Ring("Stack collar",root,new Vector3(s*.65f,4.45f,.2f),.27f,.06f,brass);}}
                else {var blade=Part("Sweeping judgement blade",PrimitiveType.Cube,root,new Vector3(0,2.5f,-.5f),new Vector3(5.8f,.16f,.8f),steel);r.rotors.Add(blade);Part("Judge visor",PrimitiveType.Cube,root,new Vector3(0,1.7f,-1.05f),new Vector3(1.1f,.16f,.12f),red);for(int j=-2;j<=2;j++)Crystal("Blade cutting tooth",root,new Vector3(j*1.1f,2.35f,-.9f),new Vector3(.35f,.5f,.2f),bone);}
            }
        }
        void BindBossArt(TowerContent data,GroupState g)
        {
            artData=data;var def=data.Boss(g.boss.definition);
            if(!artRigs.TryGetValue(def.id,out var r)){r=BuildArtRig(def);artRigs.Add(def.id,r);}
            if(artRig!=r){if(artRig!=null)artRig.root.gameObject.SetActive(false);artRig=r;}
            boss.gameObject.SetActive(false);if(variant!=null)variant.gameObject.SetActive(false);if(activeMachine!=null)activeMachine.gameObject.SetActive(false);
            r.root.gameObject.SetActive(g.phase==Ember.Core.Phase.Battle&&g.boss.visible.hp>0);
        }
        void AnimateBossArt(float dt)
        {
            if(artRig==null||observedGroup==null)return;var r=artRig;var b=observedGroup.boss;if(!r.root.gameObject.activeSelf)return;
            float t=artTime;r.strike=Mathf.Max(0,r.strike-dt);r.dash=Mathf.Max(0,r.dash-dt);var ability=artData.Ability(b.ability);float charge=b.visible.telegraph&&ability!=null?Mathf.Clamp01(1-b.visible.windup/Mathf.Max(.01f,ability.windup)):0;
            Vector3 position=new Vector3(b.x,.05f*Mathf.Sin(t*2),b.z);if(r.dash>0)position=Vector3.Lerp(r.dashFrom,position,1-r.dash/.35f);r.root.localPosition=position;
            float aim=Mathf.Atan2(b.visible.targetX-b.x,b.visible.targetZ-b.z)*Mathf.Rad2Deg+180;
            r.root.localRotation=Quaternion.Euler(r.archetype=="charger"?-charge*13:Mathf.Sin(r.strike*12)*r.strike*7,r.archetype=="charger"?aim:Mathf.Sin(t*.6f)*4,0);
            float lift=charge*-85+r.strike*100;if(r.left!=null)r.left.localRotation=Quaternion.Euler(lift,0,r.archetype=="metronome"?Mathf.Sin(t*3)*28:Mathf.Sin(t*2)*5);if(r.right!=null)r.right.localRotation=Quaternion.Euler(lift,0,-Mathf.Sin(t*2)*5);
            if(r.head!=null)r.head.localRotation=Quaternion.Euler(-charge*12,Mathf.Sin(t)*6,0);
            foreach(var rotor in r.rotors)rotor.Rotate(rotor.name=="Machined gear"?Vector3.forward:Vector3.up,(15+b.phase*8+charge*80)*dt,Space.Self);
            if(r.core!=null){float pulse=1+.07f*Mathf.Sin(t*(3+charge*8));r.core.localScale=r.coreScale*pulse;var renderer=r.core.GetComponent<Renderer>();if(renderer!=null)renderer.transform.localRotation=r.coreRotation*Quaternion.Euler(0,Mathf.Sin(t)*8,0);}
        }
    }
}
