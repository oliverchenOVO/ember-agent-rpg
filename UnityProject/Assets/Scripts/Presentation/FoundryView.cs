using System.Collections.Generic;
using Ember.Core;
using Ember.Core.Phase2;
using UnityEngine;

namespace Ember.Presentation
{
    public sealed partial class WorldView
    {
        Transform baseArchitecture,baseRestArchitecture,foundryRest;int foundryFloor;Transform activeFoundry,activeMachine;
        readonly Dictionary<int,Transform> foundries=new Dictionary<int,Transform>();readonly Dictionary<string,Transform> machines=new Dictionary<string,Transform>();
        Transform laneCue,crossCue,annulusCue,innerCue,castRing,feedbackRing;float phaseUntil,deathUntil,introUntil;int machinePhase=-1,machineGroup=-1;string machineId="";
        Material violet,whiteHot;int lastSkillSerial;readonly Transform[] weaponFx=new Transform[4];
        readonly Transform[] controlZones=new Transform[8];
        void Beam(string name,Transform parent,Vector3 from,Vector3 to,float thickness,Material mat)
        {
            var part=Part(name,PrimitiveType.Cube,parent,(from+to)*.5f,new Vector3(thickness,(to-from).magnitude,thickness),mat);part.rotation=Quaternion.FromToRotation(Vector3.up,to-from);
        }
        Transform FoundryArena(int floor)
        {
            var root=new GameObject("Foundry / "+floor).transform;root.SetParent(arena,false);
            // Rectilinear load-bearing geometry, open central gameplay area and story props beyond its bounds.
            Part("Iron dais",PrimitiveType.Cube,root,new Vector3(0,-.35f,0),new Vector3(21,.6f,18),dark);
            for(int row=0;row<5;row++)for(int col=0;col<6;col++)Part("Ceramic work tile",PrimitiveType.Cube,root,new Vector3(-8.5f+col*3.4f,-.025f,-6.8f+row*3.4f),new Vector3(3.25f,.12f,3.25f),stone);
            for(int side=-1;side<=1;side+=2)
            {
                Beam("Overhead truss",root,new Vector3(side*10,0,8),new Vector3(side*10,7,8),.65f,brass);
                Beam("Roof gantry",root,new Vector3(side*10,7,8),new Vector3(0,8,9),.4f,brass);
                Part("Cold conduit",PrimitiveType.Cube,root,new Vector3(side*10,.13f,0),new Vector3(.18f,.18f,17),glow);
                for(int j=0;j<3;j++){Part("Empty worker station",PrimitiveType.Cube,root,new Vector3(side*11.5f,.8f,-6+j*5),new Vector3(1.8f,1.6f,2),dark);Part("Observation slit",PrimitiveType.Cube,root,new Vector3(side*11.3f,2.2f,-6+j*5),new Vector3(.2f,.3f,2),glow);}
            }
            if(floor==6)
            {
                Part("Furnace backwall",PrimitiveType.Cube,root,new Vector3(0,2.4f,10),new Vector3(10,4.8f,1),dark);
                for(int i=-2;i<=2;i++)Part("Hot vent",PrimitiveType.Cube,root,new Vector3(i*1.6f,1.8f,9.4f),new Vector3(.7f,2.3f,.1f),red);
                for(int side=-1;side<=1;side+=2)Beam("Entry arch",root,new Vector3(side*6,0,-9),new Vector3(side*6,5,-9),.7f,brass);
            }
            if(floor==7||floor==9)for(int side=-1;side<=1;side+=2)
            {
                Part("Load rail",PrimitiveType.Cube,root,new Vector3(0,.1f,side*3),new Vector3(20,.1f,.25f),brass);
                for(int i=-4;i<=4;i++)Part("Rail tie",PrimitiveType.Cube,root,new Vector3(i*2,.06f,side*3),new Vector3(.18f,.12f,1.5f),dark);
            }
            if(floor==8)for(int j=0;j<3;j++)
            {
                Part("Hatchery vat",PrimitiveType.Cylinder,root,new Vector3(-6+j*6,.7f,9),new Vector3(3,.8f,3),dark);
                Part("Abandoned crystal",PrimitiveType.Cube,root,new Vector3(-6+j*6,2.2f,9),new Vector3(.9f,2,.9f),violet,new Vector3(0,45,14));
                Ring("Vat collar",root,new Vector3(-6+j*6,1.7f,9),1.5f,.12f,brass);
            }
            if(floor==10)
            {
                Ring("Armillary track",root,new Vector3(0,.08f,0),8.6f,.14f,brass);Ring("Outer circuit",root,new Vector3(0,.09f,0),9.4f,.05f,glow);
                for(int j=0;j<8;j++){float t=j*Mathf.PI/4;Part("Star dial",PrimitiveType.Cube,root,new Vector3(Mathf.Cos(t)*10,1,Mathf.Sin(t)*10),new Vector3(1.2f,2,1.2f),brass,new Vector3(0,j*45,0));}
            }
            Particles(root,new Vector3(0,5,8),new Color(.5f,.65f,1,.5f),.035f,3,6);
            return root;
        }
        Transform FoundryMachine(string id)
        {
            var root=new GameObject("Modular Boss / "+id).transform;root.SetParent(arena,false);
            if(id=="stoker")
            {
                Part("Furnace torso",PrimitiveType.Cube,root,new Vector3(0,2,0),new Vector3(2.6f,3,1.4f),dark);Part("Ceramic face",PrimitiveType.Cube,root,new Vector3(0,3.2f,-.8f),new Vector3(1.3f,.7f,.2f),whiteHot);
                for(int side=-1;side<=1;side+=2){Beam("Piston leg",root,new Vector3(side*.8f,.3f,.2f),new Vector3(side*.8f,1.6f,0),.45f,brass);Beam("Crane shoulder",root,new Vector3(side*1.3f,3,0),new Vector3(side*2.8f,2,-.5f),.5f,brass);for(int j=-1;j<=1;j+=2)Beam("Clamp jaw",root,new Vector3(side*2.8f,2,-.5f),new Vector3(side*3.1f,1.1f,j*.5f-.7f),.25f,dark);}
                Part("Heat core",PrimitiveType.Sphere,root,new Vector3(0,2,-.85f),new Vector3(.7f,1,.3f),red);
            }
            else if(id=="railjudge")
            {
                Part("Rail chassis",PrimitiveType.Cube,root,new Vector3(0,.9f,0),new Vector3(3.5f,1,2),dark);
                for(int x=-1;x<=1;x+=2)for(int z=-1;z<=1;z+=2)Part("Rail wheel",PrimitiveType.Cylinder,root,new Vector3(x*1.7f,.6f,z*.7f),new Vector3(1.1f,.18f,1.1f),brass,new Vector3(0,0,90));
                Part("Cantilever blade",PrimitiveType.Cube,root,new Vector3(0,2.3f,-.5f),new Vector3(7,.18f,.8f),stone,new Vector3(0,0,8));Part("Judge optic",PrimitiveType.Cube,root,new Vector3(0,1.7f,-1.1f),new Vector3(1,.25f,.1f),glow);
            }
            else if(id=="weavemother")
            {
                for(int j=0;j<3;j++){float t=j*Mathf.PI*2/3;var p=new Vector3(Mathf.Cos(t)*2.5f,0,Mathf.Sin(t)*2.5f);Beam("Suspension strut",root,p,p*.5f+Vector3.up*4,.3f,brass);Beam("Articulated loom",root,p*.5f+Vector3.up*4,new Vector3(0,2,0),.2f,dark);}
                Part("Suspended brood",PrimitiveType.Cube,root,new Vector3(0,2,0),new Vector3(1.7f,2.2f,1.7f),violet,new Vector3(20,45,20));Ring("Crystal collar",root,new Vector3(0,2,0),1.7f,.08f,glow);
            }
            else if(id=="metronome")
            {
                for(int side=-1;side<=1;side+=2){Beam("Pendulum arm",root,new Vector3(side*2.2f,0,0),new Vector3(side*1.2f,4,0),.25f,brass);Part("Counterweight",PrimitiveType.Sphere,root,new Vector3(side*2.2f,1,-.5f),Vector3.one*.8f,dark);var wheel=Ring("Pendulum wheel",root,new Vector3(side*1.2f,3,0),1.2f,.18f,brass);wheel.localRotation=Quaternion.Euler(90,0,0);}
                Part("Clock hammer",PrimitiveType.Cylinder,root,new Vector3(0,2,0),new Vector3(1.3f,1.6f,1.3f),stone);Part("Timing optic",PrimitiveType.Sphere,root,new Vector3(0,3.4f,0),Vector3.one*.45f,red);
            }
            else
            {
                Part("Whitehot core",PrimitiveType.Sphere,root,new Vector3(0,3,0),Vector3.one*1.8f,whiteHot);
                for(int j=0;j<3;j++){var orbit=Ring("Armillary orbit",root,new Vector3(0,3,0),2.5f+j*.4f,.12f,j==1?glow:brass);orbit.localRotation=Quaternion.Euler(j*55+20,j*60,25);}
                for(int j=0;j<4;j++){float t=j*Mathf.PI/2;Beam("Core support",root,new Vector3(Mathf.Cos(t)*3,0,Mathf.Sin(t)*3),new Vector3(0,2,0),.25f,dark);}
            }
            return root;
        }
        void SetFoundry(TowerContent data,GroupState g)
        {
            if(violet==null){violet=Mat(new Color(.46f,.2f,.7f),.3f,.55f,.7f);whiteHot=Mat(new Color(1,.85f,.55f),.4f,.7f,1.2f);}
            baseArchitecture.gameObject.SetActive(false);baseRestArchitecture.gameObject.SetActive(false);boss.gameObject.SetActive(false);if(variant!=null)variant.gameObject.SetActive(false);if(environmentProps!=null)environmentProps.gameObject.SetActive(false);
            if(!foundries.TryGetValue(g.floor,out var scene)){scene=FoundryArena(g.floor);foundries.Add(g.floor,scene);}
            if(activeFoundry!=scene){if(activeFoundry!=null)activeFoundry.gameObject.SetActive(false);activeFoundry=scene;scene.gameObject.SetActive(true);}
            if(foundryRest==null)
            {
                foundryRest=new GameObject("Foundry service room").transform;foundryRest.SetParent(refuge,false);Part("Maintenance floor",PrimitiveType.Cube,foundryRest,new Vector3(0,-.2f,0),new Vector3(22,.4f,13),dark);
                foreach(var site in data.Rest("foundry_service").sites){Part("Maintenance station / "+site.id,PrimitiveType.Cube,foundryRest,new Vector3(site.x,.35f,site.z),new Vector3(1.4f,.7f,1.1f),stone);Ring("Station port",foundryRest,new Vector3(site.x,.8f,site.z),.6f,.06f,site.id=="forge"?red:glow);}
                for(int i=-1;i<=1;i+=2)Beam("Maintenance cable",foundryRest,new Vector3(i*10,0,6),new Vector3(i*10,5,6),.5f,brass);
                Ring("Service exit",foundryRest,new Vector3(9,.04f,0),1.2f,.1f,glow);
            }
            foundryRest.gameObject.SetActive(true);
            string id=data.Boss(g.boss.definition).archetype;
            if(!machines.TryGetValue(id,out var model)){model=FoundryMachine(id);machines.Add(id,model);}
            if(activeMachine!=model||machineGroup!=g.id)
            {if(activeMachine!=null)activeMachine.gameObject.SetActive(false);activeMachine=model;machineGroup=g.id;machinePhase=-1;introUntil=Time.time+2;previousBossHp=g.boss.visible.hp;machineId=id;}
            var b=g.boss;if(b.phase!=machinePhase){machinePhase=b.phase;phaseUntil=Time.time+1.2f;}
            if(previousBossHp>0&&b.visible.hp<=0)deathUntil=Time.time+1.4f;previousBossHp=b.visible.hp;
            float dissolve=b.visible.hp>0?1:Mathf.Clamp01((deathUntil-Time.time)/1.4f);model.gameObject.SetActive(dissolve>0);
            float anticipation=b.visible.telegraph?Mathf.Clamp01(1-b.visible.windup/2):0;
            model.localPosition=new Vector3(b.x,Mathf.Sin(Time.time*(id=="metronome"?4:1.5f))*.08f+(Time.time<introUntil?(introUntil-Time.time)*.5f:0),b.z);
            model.localScale=Vector3.one*(dissolve*(1+anticipation*.06f));model.localRotation=Quaternion.Euler(id=="railjudge"?anticipation*-12:0,id=="armillary"?Time.time*(4+b.phase*3):id=="weavemother"?Mathf.Sin(Time.time)*8:0,id=="metronome"?Mathf.Sin(Time.time*2)*12:0);
            ground.color=new Color(.07f,.11f,.14f);stone.color=new Color(.27f,.32f,.36f);RenderSettings.fogColor=new Color(.035f,.06f,.085f);RenderSettings.fogDensity=.015f;RenderSettings.ambientLight=new Color(.28f,.38f,.46f);camera.backgroundColor=RenderSettings.fogColor;
            ShowCombatCues(g);
        }
        void ShowCombatCues(GroupState g)
        {
            var b=g.boss;
            if(laneCue==null)
            {
                laneCue=Part("Lane indicator",PrimitiveType.Cube,arena,Vector3.zero,Vector3.one,red);crossCue=new GameObject("Cross indicator").transform;crossCue.SetParent(arena,false);
                Part("Cross X",PrimitiveType.Cube,crossCue,Vector3.zero,new Vector3(18,.04f,2),red);Part("Cross Z",PrimitiveType.Cube,crossCue,Vector3.zero,new Vector3(2,.04f,18),red);
                annulusCue=Ring("Outer resonance",arena,Vector3.zero,1,.05f,red);innerCue=Ring("Safe inner boundary",arena,Vector3.zero,1,.05f,glow);castRing=Ring("Interruptible cast",arena,Vector3.zero,2,.07f,glow);feedbackRing=Ring("Boss feedback",arena,Vector3.zero,2.8f,.14f,brass);
            }
            bool active=g.phase==Phase.Battle;
            int zoneIndex=0;foreach(var effect in b.effects)if(effect.kind=="Zone"&&effect.radius>0&&zoneIndex<controlZones.Length)
            {if(controlZones[zoneIndex]==null)controlZones[zoneIndex]=Ring("Skill control zone",arena,Vector3.zero,3,.06f,violet);var zone=controlZones[zoneIndex++];zone.gameObject.SetActive(active);zone.localPosition=new Vector3(effect.x,.1f,effect.z);zone.localScale=Vector3.one*(effect.radius/3);}
            for(int i=zoneIndex;i<controlZones.Length;i++)if(controlZones[i]!=null)controlZones[i].gameObject.SetActive(false);
            laneCue.gameObject.SetActive(active&&b.visible.telegraph&&b.cue.shape==CueShape.Lane);crossCue.gameObject.SetActive(active&&b.visible.telegraph&&b.cue.shape==CueShape.Cross);annulusCue.gameObject.SetActive(active&&b.visible.telegraph&&b.cue.shape==CueShape.Annulus);innerCue.gameObject.SetActive(annulusCue.gameObject.activeSelf);
            laneCue.localPosition=new Vector3(b.cue.x,.07f,b.cue.z);laneCue.localScale=new Vector3(b.cue.length,.045f,b.cue.radius*2);laneCue.localRotation=Quaternion.Euler(0,-b.cue.angle*Mathf.Rad2Deg,0);
            crossCue.localPosition=new Vector3(b.cue.x,.07f,b.cue.z);crossCue.localRotation=laneCue.localRotation;crossCue.localScale=Vector3.one;crossCue.GetChild(0).localScale=new Vector3(b.cue.length,.04f,b.cue.radius*2);crossCue.GetChild(1).localScale=new Vector3(b.cue.radius*2,.04f,b.cue.length);
            annulusCue.localPosition=new Vector3(b.cue.x,.08f,b.cue.z);innerCue.localPosition=annulusCue.localPosition;annulusCue.localScale=Vector3.one*b.cue.radius;innerCue.localScale=Vector3.one*b.cue.innerRadius;
            castRing.gameObject.SetActive(active&&b.visible.telegraph&&b.cue.interruptible);castRing.localPosition=new Vector3(b.x,.1f,b.z);
            bool debuffed=b.effects.Exists(e=>e.agent==-1);
            feedbackRing.gameObject.SetActive(active&&(Time.time<phaseUntil||b.hitFlash>0||b.interruptFlash>0||b.survivalLeft>0||b.blockedFlash>0||b.shield>0||b.weakFlash>0||b.visible.enraged||debuffed));feedbackRing.localPosition=new Vector3(b.x,.12f,b.z);
            feedbackRing.GetComponent<Renderer>().sharedMaterial=b.interruptFlash>0?glow:b.weakFlash>0?whiteHot:b.survivalLeft>0||b.blockedFlash>0||b.shield>0?stone:b.visible.enraged?red:debuffed?violet:brass;
        }
    }
}
