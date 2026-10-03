using System.Collections.Generic;
using Ember.Core.Phase2;
using UnityEngine;
namespace Ember.Presentation
{
    public sealed partial class WorldView
    {
        readonly Dictionary<string,Transform> sceneryArt=new Dictionary<string,Transform>();Transform activeScenery;
        void BindEnvironmentArt(TowerContent data,GroupState g)
        {
            string theme=data.Floor(g.floor).theme;string key=theme+(theme=="astral_foundry"?g.floor.ToString():"");
            if(!sceneryArt.TryGetValue(key,out var scene))
            {
                scene=Pivot("Authored environment detail / "+key,arena,Vector3.zero);sceneryArt.Add(key,scene);
                if(theme=="astral_foundry")FoundryDressing(scene,g.floor);
                else if(theme.Contains("ice"))IceDressing(scene);
                else if(theme.Contains("castle"))CastleDressing(scene);
                else if(theme.Contains("abyss"))AbyssDressing(scene);
                else ForestDressing(scene);
            }
            if(activeScenery!=scene){if(activeScenery!=null)activeScenery.gameObject.SetActive(false);activeScenery=scene;}scene.gameObject.SetActive(true);
            if(environmentProps!=null)environmentProps.gameObject.SetActive(false);
        }
        void ForestDressing(Transform root)
        {
            for(int i=0;i<9;i++)
            {
                float a=(i+1)*Mathf.PI/10;var p=new Vector3(Mathf.Cos(a)*13,0,Mathf.Sin(a)*13);
                Tube("Ancient tree buttress",root,new[]{p,p+Vector3.up*2,p+Vector3.up*4+new Vector3(-.7f,0,.3f),p+Vector3.up*6+new Vector3(-1.2f,0,.4f)},.7f,.28f,bark,12);
                for(int s=-1;s<=1;s+=2)Tube("Overhanging branch",root,new[]{p+Vector3.up*4,p+new Vector3(s*2,5,.3f),p+new Vector3(s*3.2f,5.3f,-.6f)},.24f,.02f,bark);
                for(int k=0;k<3;k++){var q=p.normalized*(10.8f+k*.4f);Part("Moss terrace stone",PrimitiveType.Cube,root,q+Vector3.up*.12f,new Vector3(1.5f,.25f,.8f),green,new Vector3(0,i*20,0));}
            }
            for(int i=0;i<16;i++){float a=i*Mathf.PI/8;var p=new Vector3(Mathf.Cos(a)*10.5f,.03f,Mathf.Sin(a)*10.5f);Tube("Carved floor fissure",root,new[]{p,p*.96f+new Vector3(.25f,0,.15f),p*.9f+new Vector3(.4f,0,-.1f)},.028f,.006f,glow,6);}
            var arch=Pivot("Overgrown witness arch",root,new Vector3(0,0,13));Tube("Ritual arch",arch,new[]{new Vector3(-3,0,0),new Vector3(-3,3,0),new Vector3(-2,5,0),new Vector3(0,5.8f,0),new Vector3(2,5,0),new Vector3(3,3,0),new Vector3(3,0,0)},.32f,.32f,stone,10);
            Ring("Rear celestial seal",arch,new Vector3(0,3,0),1.5f,.05f,brass).localRotation=Quaternion.Euler(90,0,0);
        }
        void IceDressing(Transform root)
        {
            for(int i=0;i<14;i++){float a=i*Mathf.PI/7;var p=new Vector3(Mathf.Cos(a)*12,0,Mathf.Sin(a)*12);Crystal("Glacial cathedral spire",root,p+Vector3.up*(1+i%3*.4f),new Vector3(1.1f,3+i%4,1),iceArt).localRotation=Quaternion.Euler(0,i*23,i%2==0?12:-12);Crystal("Fractured ice foothill",root,p*.91f+Vector3.up*.4f,new Vector3(1.4f,1.1f,1),stone);}
            for(int j=-3;j<=3;j++)Tube("Glacial vein",root,new[]{new Vector3(-8,.035f,j*2),new Vector3(-3,.035f,j*2+.4f),new Vector3(2,.035f,j*2-.25f),new Vector3(8,.035f,j*2+.3f)},.018f,.018f,iceArt,6);
        }
        void CastleDressing(Transform root)
        {
            for(int i=-3;i<=3;i++)
            {
                var p=new Vector3(i*3.1f,0,12);Part("Cathedral column plinth",PrimitiveType.Cube,root,p+Vector3.up*.2f,new Vector3(1.6f,.4f,1.6f),dark);Part("Fluted cathedral column",PrimitiveType.Cylinder,root,p+Vector3.up*2.7f,new Vector3(.9f,2.5f,.9f),stone);Part("Column capital",PrimitiveType.Cube,root,p+Vector3.up*5.3f,new Vector3(1.4f,.4f,1.4f),brass);
                if(i<3)Tube("Gothic arch",root,new[]{p+Vector3.up*4.7f,p+new Vector3(.7f,6,0),p+new Vector3(1.55f,6.5f,0),p+new Vector3(2.4f,6,0),p+new Vector3(3.1f,4.7f,0)},.14f,.14f,stone);
                Part("Hanging witness banner",PrimitiveType.Cube,root,p+new Vector3(0,4.1f,.6f),new Vector3(.5f,1.8f,.05f),i%2==0?violet:dark);Lantern(root,p+new Vector3(0,0,-1));
            }
            for(int j=0;j<5;j++)Part("Broken cathedral stair",PrimitiveType.Cube,root,new Vector3(0,-.5f+j*.16f,10+j*.6f),new Vector3(7,.18f,1),stone);
        }
        void AbyssDressing(Transform root)
        {
            for(int i=0;i<12;i++){float a=i*Mathf.PI/6;var p=new Vector3(Mathf.Cos(a)*12,0,Mathf.Sin(a)*12);Crystal("Basalt fracture wall",root,p+Vector3.up*1.5f,new Vector3(1.8f,4+i%3,1.6f),dark).localRotation=Quaternion.Euler(0,i*35,12);Tube("Molten rift seam",root,new[]{p+Vector3.up*.3f,p+Vector3.up*1.2f+Vector3.right*.15f,p+Vector3.up*2.8f},.035f,.015f,red);}
            Ring("Abyssal moat",root,new Vector3(0,-.05f,0),10.8f,.24f,red);for(int j=0;j<7;j++)Crystal("Floating void stone",root,new Vector3(-9+j*3,5+j%3*.8f,15),new Vector3(1.6f,2,1.4f),stone);
        }
        void FoundryDressing(Transform root,int floor)
        {
            for(int s=-1;s<=1;s+=2)
            {
                for(int j=0;j<4;j++){var p=new Vector3(s*11,0,-5+j*4);Tube("Industrial coolant pipe",root,new[]{p+Vector3.up*.4f,p+Vector3.up*3,p+new Vector3(-s*.5f,3.6f,0),p+new Vector3(-s*.7f,3.6f,2)},.18f,.18f,steel);Part("Pipe support bracket",PrimitiveType.Cube,root,p+Vector3.up*2,new Vector3(.55f,.12f,.6f),brass);Gear(root,p+new Vector3(-s*.3f,1.9f,-.22f),.33f,brass);}
                for(int j=0;j<5;j++){Part("Riveted wall panel",PrimitiveType.Cube,root,new Vector3(s*12,1.6f,-7+j*3.6f),new Vector3(.3f,3.2f,3.4f),dark);Part("Warning strip",PrimitiveType.Cube,root,new Vector3(s*11.8f,.6f,-7+j*3.6f),new Vector3(.08f,.12f,2.8f),brass);}
            }
            for(int j=-3;j<=3;j++){Part("Rear furnace rib",PrimitiveType.Cube,root,new Vector3(j*3,2.5f,11),new Vector3(.35f,5,.45f),steel);Part("Rear arc lamp",PrimitiveType.Sphere,root,new Vector3(j*3,4.6f,10.5f),Vector3.one*.28f,glow);}
            if(floor==8)for(int i=-1;i<=1;i++)for(int j=0;j<5;j++)Crystal("Hatchery shard cluster",root,new Vector3(i*6+(j-2)*.2f,.8f+j*.12f,9),new Vector3(.35f,.8f+j*.2f,.35f),soulArt);
            if(floor==10){var astrolabe=Ring("Giant rear astrolabe",root,new Vector3(0,6,13),4,.15f,brass,64);astrolabe.localRotation=Quaternion.Euler(90,0,0);for(int j=0;j<12;j++){float a=j*Mathf.PI/6;Part("Astrolabe star mark",PrimitiveType.Sphere,root,new Vector3(Mathf.Cos(a)*3.6f,6+Mathf.Sin(a)*3.6f,12.9f),Vector3.one*.14f,glow);}}
        }
        void DressFacility(Transform station,RestSite site)
        {
            if(site.effect=="Read"||site.effect=="Intel")
            {
                for(int s=-1;s<=1;s+=2)for(int j=0;j<5;j++)Part("Open book pages",PrimitiveType.Cube,station,new Vector3(s*.22f,.92f+j*.012f,0),new Vector3(.42f,.014f,.55f),bone??stone,new Vector3(0,0,s*-8));
                Lantern(station,new Vector3(-.9f,0,.25f));
            }
            else if(site.effect=="Craft")
            {
                Part("Anvil base",PrimitiveType.Cylinder,station,new Vector3(0,.83f,0),new Vector3(.65f,.14f,.65f),brass);Part("Anvil face",PrimitiveType.Cube,station,new Vector3(0,1.05f,0),new Vector3(.95f,.18f,.4f),stone);Part("Anvil horn",PrimitiveType.Capsule,station,new Vector3(.6f,1.04f,0),new Vector3(.2f,.35f,.2f),brass,new Vector3(0,0,90));Lantern(station,new Vector3(-1,0,0));
            }
            else if(site.effect=="Heal"||site.effect=="Cleanse")
            {Part("Recovery mattress",PrimitiveType.Cube,station,new Vector3(0,.78f,0),new Vector3(1.65f,.16f,1.1f),green);Part("Recovery pillow",PrimitiveType.Cube,station,new Vector3(-.55f,.9f,0),new Vector3(.4f,.18f,.8f),bone??stone);for(int j=0;j<3;j++)Part("Clinic vial",PrimitiveType.Cylinder,station,new Vector3(.85f,.65f,-.35f+j*.35f),new Vector3(.14f,.25f,.14f),glow);}
            else for(int j=0;j<3;j++)Part("Supply crate brace",PrimitiveType.Cube,station,new Vector3(-.35f+j*.35f,.85f,0),new Vector3(.08f,.15f,1),brass);
        }
    }
}
