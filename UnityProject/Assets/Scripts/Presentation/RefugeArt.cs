using System.Collections.Generic;
using Ember.Core;
using Ember.Core.Phase2;
using UnityEngine;
namespace Ember.Presentation
{
    public sealed partial class WorldView
    {
        readonly Dictionary<string,Transform> refugeScenes=new Dictionary<string,Transform>();Transform activeRefugeScene;
        public bool RefugeCollapseVisible=>collapseFront.gameObject.activeInHierarchy&&collapseVoid.gameObject.activeInHierarchy;
        public int RefugeSelected;public bool RefugeOverview;public int PreviewRefugeRoom=-1;public Vector3? PreviewRefugeObject;
        public Vector3 ScreenPoint(Vector3 p)=>camera.WorldToScreenPoint(p);
        public int VisibleRefugeMeshes=>activeRefugeScene==null?0:activeRefugeScene.GetComponentsInChildren<MeshFilter>(true).Length;
        void BindRefugeArt(TowerContent data,GroupState g)
        {
            bool spatial=g.refugeVersion==1;
            if(activeRefugeScene!=null)activeRefugeScene.gameObject.SetActive(spatial);
            if(!spatial)return;
            baseRestArchitecture.gameObject.SetActive(false);if(foundryRest!=null)foundryRest.gameObject.SetActive(false);
            string theme=data.Floor(g.floor).theme,key=theme+":"+g.refugeVariant;
            if(!refugeScenes.TryGetValue(key,out var root))
            {
                root=Pivot("Explorable refuge / "+key,refuge,Vector3.zero);refugeScenes.Add(key,root);var map=RefugeMap.For(g);
                Part("Refuge terrain",PrimitiveType.Cube,root,new Vector3(0,-.45f,0),new Vector3(60,.8f,40),dark);
                for(int x=-28;x<=28;x+=4)for(int z=-18;z<=18;z+=4)Part("Courtyard paving",PrimitiveType.Cube,root,new Vector3(x,-.03f,z),new Vector3(3.8f,.12f,3.8f),theme.Contains("ice")?iceArt:stone);
                bool forest=theme.Contains("forest"),forge=theme=="astral_foundry",ice=theme.Contains("ice"),abyss=theme.Contains("abyss");
                foreach(var rect in map.obstacles)
                {bool divider=rect.width>1&&rect.height>2;Part(divider?"Impassable collapsed masonry":"Building wall",PrimitiveType.Cube,root,new Vector3(rect.center.x,divider?.9f:1.1f,rect.center.y),new Vector3(rect.width,divider?1.8f:2.2f,rect.height),forest?bark:forge?steel:ice?iceArt:abyss?dark:stone);if(divider)for(int j=0;j<4;j++)Part("Rubble crest",PrimitiveType.Cube,root,new Vector3(rect.center.x,.9f+j*.18f,rect.yMin+.7f+j*2),new Vector3(1.1f,.8f,1.2f),stone,new Vector3(0,j*17,12));}
                for(int room=0;room<6;room++)
                {
                    var c=map.rooms[room];var house=Pivot("Accessible cutaway building "+room,root,new Vector3(c.x,0,c.y));
                    Part("Interior floorboards",PrimitiveType.Cube,house,new Vector3(0,.015f,0),new Vector3(10.8f,.12f,7.4f),forest?bark:dark);
                    float door=c.y<0?4:-4;
                    for(int side=-1;side<=1;side+=2)
                    {
                        Part("Door jamb",PrimitiveType.Cube,house,new Vector3(side*2.15f,1.4f,door),new Vector3(.25f,2.8f,.4f),brass);
                        Part("Roof support",PrimitiveType.Cylinder,house,new Vector3(side*5,1.7f,-door*.7f),new Vector3(.3f,1.7f,.3f),forest?bark:stone);
                        Beam("Cutaway roof truss",house,new Vector3(side*5,3.4f,-door*.7f),new Vector3(0,4.6f,-door*.7f),.18f,forest?bark:brass);
                        Lantern(house,new Vector3(side*4.4f,0,door*.45f));
                        if(forge){Tube("Service pipe",house,new[]{new Vector3(side*5,0,0),new Vector3(side*5,2.8f,0),new Vector3(side*4,3.2f,0)},.13f,.13f,steel);Gear(house,new Vector3(side*4.9f,2,0),.3f,brass);}
                        if(ice)Crystal("Ice buttress",house,new Vector3(side*5.1f,1,-door*.8f),new Vector3(.65f,2.7f,.7f),iceArt);
                        if(forest)Tube("Living building root",house,new[]{new Vector3(side*5.5f,0,0),new Vector3(side*5.4f,2,0),new Vector3(side*4.8f,3.2f,.5f)},.25f,.06f,bark);
                    }
                    Beam("Entrance lintel",house,new Vector3(-2.2f,2.8f,door),new Vector3(2.2f,2.8f,door),.22f,brass);
                    if(!forest&&!forge)Tube("Vaulted rear arch",house,new[]{new Vector3(-4,2,-door*.7f),new Vector3(-2,3.8f,-door*.7f),new Vector3(0,4.5f,-door*.7f),new Vector3(2,3.8f,-door*.7f),new Vector3(4,2,-door*.7f)},.14f,.14f,abyss?soulArt:brass);
                }
                for(int j=-1;j<=1;j+=2){Part("Departure gateway pillar",PrimitiveType.Cube,root,new Vector3(28,2,j*2),new Vector3(.7f,4,.7f),brass);Beam("Departure arch",root,new Vector3(28,4,-2),new Vector3(28,4,2),.5f,brass);}
                Ring("Departure portal",root,new Vector3(28,.07f,0),1.7f,.09f,glow);Point(root,new Vector3(28,2,0),new Color(.2f,.95f,.8f),4,6);
                if(forest)for(int j=0;j<7;j++)Tube("Refuge forest perimeter",root,new[]{new Vector3(-26+j*8,0,20),new Vector3(-26+j*8,5,20),new Vector3(-28+j*8,7,19)},.7f,.08f,bark);
                if(abyss)Ring("Refuge abyss edge",root,new Vector3(0,-.2f,0),30,.13f,red);
            }
            if(activeRefugeScene!=root){if(activeRefugeScene!=null)activeRefugeScene.gameObject.SetActive(false);activeRefugeScene=root;}root.gameObject.SetActive(true);
        }
        void UpdateRefugeArt(World w,float dt)
        {
            if(observedGroup==null||observedGroup.refugeVersion!=1||w.phase!=Phase.Rest)return;
            float front=RefugeMap.Front(artData.Rest(observedGroup.restId),observedGroup);bool collapsing=w.phaseClock>=RefugeMap.Limit(artData.Rest(observedGroup.restId),observedGroup);
            collapseFront.gameObject.SetActive(collapsing);collapseVoid.gameObject.SetActive(collapsing);collapseFront.localPosition=new Vector3(front,.3f,0);collapseFront.localScale=new Vector3(.18f,.65f,40);
            collapseVoid.localPosition=new Vector3((-30+front)*.5f,.07f,0);collapseVoid.localScale=new Vector3(Mathf.Max(.01f,front+30),.2f,40);
            Vector3 focus=new Vector3(w.agents[RefugeSelected].x,0,w.agents[RefugeSelected].z);
            if(PreviewRefugeRoom>=0){var c=RefugeMap.For(observedGroup).rooms[PreviewRefugeRoom];focus=new Vector3(c.x,0,c.y);}
            if(PreviewRefugeObject.HasValue){focus=PreviewRefugeObject.Value;camera.transform.position=focus+new Vector3(2,7,-5.5f);camera.transform.LookAt(focus+Vector3.up*.7f);camera.fieldOfView=36;return;}
            var desired=RefugeOverview?new Vector3(31,48,-55):focus+Quaternion.Euler(0,angle,0)*new Vector3(12,19,-19);
            camera.transform.position=Vector3.Lerp(camera.transform.position,desired,Mathf.Clamp01((dt>0?dt:Time.unscaledDeltaTime)*5));camera.transform.LookAt((RefugeOverview?new Vector3(-1,0,0):focus)+Vector3.up*.5f);camera.fieldOfView=RefugeOverview?48:42;
        }
        void ModelRefugeFacility(Transform root,RestSite s)
        {
            if(s.id=="bed")
            {for(int x=-1;x<=1;x+=2)for(int z=-1;z<=1;z+=2)Part("Bed leg",PrimitiveType.Cube,root,new Vector3(x*.9f,.3f,z*.5f),new Vector3(.16f,.6f,.16f),bark);Part("Bed frame",PrimitiveType.Cube,root,new Vector3(0,.6f,0),new Vector3(2.1f,.2f,1.4f),bark);Part("Quilt mattress",PrimitiveType.Cube,root,new Vector3(0,.8f,0),new Vector3(1.9f,.25f,1.2f),green);Part("Pillow",PrimitiveType.Capsule,root,new Vector3(-.65f,1,0),new Vector3(.4f,.55f,.9f),bone,new Vector3(0,0,90));Part("Bed headboard",PrimitiveType.Cube,root,new Vector3(-1,.95f,0),new Vector3(.15f,1.2f,1.5f),bark);for(int j=0;j<5;j++)Part("Quilt seam",PrimitiveType.Cube,root,new Vector3(-.2f+j*.25f,.94f,0),new Vector3(.025f,.018f,1.1f),brass);}
            else if(s.id=="forge")
            {Part("Stone furnace",PrimitiveType.Cube,root,new Vector3(1.8f,.9f,.5f),new Vector3(1.3f,1.8f,1.4f),stone);Part("Forge fire",PrimitiveType.Sphere,root,new Vector3(1.8f,.6f,-.25f),new Vector3(.8f,.7f,.12f),red);Tube("Forge chimney",root,new[]{new Vector3(1.8f,1.8f,.5f),new Vector3(1.8f,3,.5f),new Vector3(2,3.5f,.5f)},.25f,.25f,steel);Part("Anvil stump",PrimitiveType.Cylinder,root,new Vector3(0,.4f,0),new Vector3(.9f,.4f,.9f),bark);Part("Anvil waist",PrimitiveType.Cube,root,new Vector3(0,.95f,0),new Vector3(.5f,.5f,.5f),steel);Part("Anvil face",PrimitiveType.Cube,root,new Vector3(0,1.25f,0),new Vector3(1.5f,.22f,.65f),steel);Crystal("Anvil horn",root,new Vector3(-.95f,1.25f,0),new Vector3(.25f,.8f,.3f),steel).localRotation=Quaternion.Euler(0,0,90);Part("Hammer handle",PrimitiveType.Cylinder,root,new Vector3(.3f,1.43f,0),new Vector3(.07f,.4f,.07f),bark,new Vector3(0,0,80));Part("Hammer head",PrimitiveType.Cube,root,new Vector3(.65f,1.48f,0),new Vector3(.3f,.25f,.25f),steel);}
            else if(s.id=="library"||s.id=="book")
            {Part("Reading desk",PrimitiveType.Cube,root,new Vector3(0,.8f,0),new Vector3(1.8f,.18f,1.1f),bark);for(int side=-1;side<=1;side+=2)Part("Desk support",PrimitiveType.Cube,root,new Vector3(side*.65f,.4f,0),new Vector3(.18f,.8f,.75f),brass);for(int side=-1;side<=1;side+=2)for(int j=0;j<7;j++)Part("Layered open folio",PrimitiveType.Cube,root,new Vector3(side*.35f,.94f+j*.012f,0),new Vector3(.65f,.014f,.8f),bone,new Vector3(0,0,side*-12));for(int j=0;j<5;j++)Part("Written folio line",PrimitiveType.Cube,root,new Vector3(.35f,1.03f,-.28f+j*.13f),new Vector3(.4f,.018f,.025f),s.id=="book"?soulArt:brass);if(s.id=="book"){Part("Memorial skull",PrimitiveType.Sphere,root,new Vector3(-.65f,1.2f,.3f),Vector3.one*.3f,bone);Lantern(root,new Vector3(.8f,0,.4f));}else {Part("Bookshelf backing",PrimitiveType.Cube,root,new Vector3(0,1.5f,1.2f),new Vector3(2.5f,2.8f,.25f),bark);for(int row=0;row<3;row++){Part("Book shelf",PrimitiveType.Cube,root,new Vector3(0,.5f+row*.85f,1),new Vector3(2.5f,.12f,.65f),brass);for(int j=0;j<8;j++)Part("Individual book spine",PrimitiveType.Cube,root,new Vector3(-1+j*.28f,.8f+row*.85f,1),new Vector3(.18f,.5f+j%3*.06f,.4f),j%3==0?green:j%3==1?bone:soulArt);}}}
            else if(s.id=="clinic")
            {Part("Apothecary bench",PrimitiveType.Cube,root,new Vector3(0,.8f,0),new Vector3(2,.2f,1.2f),bone);for(int j=-1;j<=1;j++) {Part("Potion bottle",PrimitiveType.Sphere,root,new Vector3(j*.5f,1.1f,0),new Vector3(.3f,.45f,.3f),j<0?red:j==0?glow:soulArt);Part("Bottle neck",PrimitiveType.Cylinder,root,new Vector3(j*.5f,1.35f,0),new Vector3(.12f,.13f,.12f),brass);}Part("Mortar",PrimitiveType.Cylinder,root,new Vector3(.7f,1.05f,.35f),new Vector3(.35f,.12f,.35f),stone);Part("Medical cross horizontal",PrimitiveType.Cube,root,new Vector3(0,1.8f,.6f),new Vector3(.8f,.18f,.12f),red);Part("Medical cross vertical",PrimitiveType.Cube,root,new Vector3(0,1.8f,.6f),new Vector3(.18f,.8f,.12f),red);for(int j=-1;j<=1;j+=2)Part("Bench legs",PrimitiveType.Cube,root,new Vector3(j*.75f,.4f,0),new Vector3(.15f,.8f,.8f),bark);}
            else if(s.id=="church")
            {Part("Altar steps",PrimitiveType.Cube,root,new Vector3(0,.2f,0),new Vector3(2.4f,.4f,2),stone);Part("Altar table",PrimitiveType.Cube,root,new Vector3(0,.7f,0),new Vector3(1.8f,.5f,1.2f),bone);Part("Shrine effigy",PrimitiveType.Capsule,root,new Vector3(0,1.4f,.35f),new Vector3(.5f,.8f,.4f),brass);Part("Shrine head",PrimitiveType.Sphere,root,new Vector3(0,2.1f,.35f),Vector3.one*.35f,bone);for(int side=-1;side<=1;side+=2)Lantern(root,new Vector3(side*.9f,0,-.3f));Ring("Shrine halo",root,new Vector3(0,2.15f,.4f),.5f,.04f,glow).localRotation=Quaternion.Euler(90,0,0);}
            else if(s.id=="corpse")
            {Part("Fallen adventurer torso",PrimitiveType.Capsule,root,new Vector3(0,.25f,0),new Vector3(.6f,.65f,.4f),dark,new Vector3(0,0,90));Part("Fallen skull",PrimitiveType.Sphere,root,new Vector3(-.8f,.28f,0),new Vector3(.35f,.4f,.3f),bone);for(int side=-1;side<=1;side+=2){Tube("Fallen limb",root,new[]{new Vector3(.5f,.2f,side*.13f),new Vector3(.85f,.2f,side*.35f),new Vector3(1.3f,.15f,side*.3f)},.09f,.06f,bone);Tube("Broken arm",root,new[]{new Vector3(-.3f,.2f,side*.2f),new Vector3(-.25f,.2f,side*.55f),new Vector3(.15f,.15f,side*.6f)},.07f,.04f,bone);}Part("Abandoned blade",PrimitiveType.Cube,root,new Vector3(0,.12f,-.9f),new Vector3(1.7f,.06f,.15f),steel,new Vector3(0,25,0));}
            else if(s.id=="cache")
            {Part("Supply chest",PrimitiveType.Cube,root,new Vector3(0,.5f,0),new Vector3(1.6f,.85f,1),bark);Part("Raised chest lid",PrimitiveType.Cube,root,new Vector3(0,1.02f,.25f),new Vector3(1.6f,.16f,1),bark,new Vector3(-25,0,0));for(int side=-1;side<=1;side+=2)Part("Chest iron strap",PrimitiveType.Cube,root,new Vector3(side*.5f,.65f,-.51f),new Vector3(.12f,.8f,.04f),brass);Part("Chest latch",PrimitiveType.Cube,root,new Vector3(0,.7f,-.56f),new Vector3(.25f,.3f,.12f),brass);for(int j=0;j<6;j++)Part("Chest coins",PrimitiveType.Cylinder,root,new Vector3(-.5f+j*.18f,.95f,0),new Vector3(.18f,.025f,.18f),brass);}
            else
            {Part("Mining cart",PrimitiveType.Cube,root,new Vector3(0,.4f,.5f),new Vector3(1.5f,.65f,1),bark);for(int side=-1;side<=1;side+=2)Gear(root,new Vector3(side*.8f,.25f,.3f),.25f,steel);for(int j=0;j<7;j++)Crystal("Harvestable ore",root,new Vector3((j%3-1)*.5f,.55f+j%2*.2f,(j/3-1)*.4f),new Vector3(.5f,.7f,.5f),j%2==0?soulArt:stone);Part("Pickaxe handle",PrimitiveType.Cylinder,root,new Vector3(1,.65f,0),new Vector3(.06f,.7f,.06f),bark,new Vector3(0,0,25));Tube("Pickaxe head",root,new[]{new Vector3(.4f,1.3f,0),new Vector3(.8f,1.45f,0),new Vector3(1.3f,1.2f,0)},.07f,.02f,steel);}
        }
    }
}
