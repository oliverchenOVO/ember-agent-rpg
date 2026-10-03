using System.Linq;
using Ember.Core;
using Ember.Core.Phase2;
using UnityEngine;

namespace Ember.Presentation
{
    public sealed partial class WorldView
    {
        Transform restFacilities;string restFacilityDefinition="";
        readonly System.Collections.Generic.Dictionary<string,Transform> restFacilityObjects=new System.Collections.Generic.Dictionary<string,Transform>();
        void SetRestFacilities(TowerContent data,GroupState group)
        {
            var rest=data.Rest(group.restId);
            if(restFacilities==null||restFacilityDefinition!=rest.id)
            {
                if(restFacilities!=null){restFacilities.gameObject.SetActive(false);Object.Destroy(restFacilities.gameObject);}
                restFacilityObjects.Clear();restFacilityDefinition=rest.id;
                restFacilities=new GameObject("Available refuge facilities").transform;restFacilities.SetParent(refuge,false);
                foreach(var site in rest.sites)
                {
                    var station=new GameObject("Rest facility / "+site.id).transform;station.SetParent(restFacilities,false);station.localPosition=new Vector3(site.x,0,site.z);
                    Part("Facility",PrimitiveType.Cube,station,new Vector3(0,.35f,0),site.effect=="Heal"?new Vector3(1.8f,.7f,1.4f):new Vector3(1.1f,.7f,1),stone);
                    Ring("Interaction sigil",station,new Vector3(0,.8f,0),.65f,.06f,site.risk>.1f?red:glow);
                    if(site.effect=="Read"||site.effect=="Intel")Part("Book",PrimitiveType.Cube,station,new Vector3(0,.85f,0),new Vector3(.8f,.15f,.6f),brass,new Vector3(10,0,0));
                    restFacilityObjects.Add(site.id,station);
                }
            }
            restFacilities.gameObject.SetActive(group.phase==Phase.Rest);
            foreach(var entry in restFacilityObjects)entry.Value.gameObject.SetActive(rest.HasSite(group,entry.Key));
        }
        GroupState observedGroup;float observedRadius=3.2f;Transform variant,environmentProps,hazard,phaseRing;Transform[] adds;string visualArchetype="",visualTheme="";float previousBossHp,impactUntil;int visualPhase=-1;
        readonly System.Collections.Generic.Dictionary<string,Transform> variantCache=new System.Collections.Generic.Dictionary<string,Transform>();
        public void SetExpedition(TowerContent data,GroupState group)
        {
            SetRestFacilities(data,group);observedGroup=group;observedRestLimit=data.Rest(group.restId).collapseAfter;observedRadius=group.boss.Radius(data);var definition=data.Boss(group.boss.definition);var f=data.Floor(group.floor);var profile=data.environments.First(e=>e.id==f.theme);
            if(visualTheme!=f.theme)
            {
                visualTheme=f.theme;var color=new Color(profile.r,profile.g,profile.b);
                ground.color=color*.4f;stone.color=color*.9f;RenderSettings.fogColor=color*.32f;RenderSettings.fogDensity=profile.fog;RenderSettings.ambientLight=color*.8f+Color.gray*.22f;
                if(environmentProps!=null)Object.Destroy(environmentProps.gameObject);
                environmentProps=new GameObject("Environment profile / "+f.theme).transform;environmentProps.SetParent(arena,false);
                for(int i=0;i<8;i++)
                {
                    float angle=i*Mathf.PI/4;var position=new Vector3(Mathf.Cos(angle)*9,1.6f,Mathf.Sin(angle)*9);
                    bool ice=f.theme.Contains("ice"),castle=f.theme.Contains("castle"),abyss=f.theme.Contains("abyss");
                    var shape=ice?PrimitiveType.Capsule:castle?PrimitiveType.Cube:abyss?PrimitiveType.Sphere:PrimitiveType.Cylinder;
                    Part("Theme silhouette",shape,environmentProps,position,new Vector3(ice?.5f:1,castle?3:1.7f,.65f),ice?glow:abyss?red:stone,new Vector3(0,i*45,ice?18:0));
                }
                camera.backgroundColor=color*.25f;
            }
            if(visualArchetype!=definition.archetype)
            {
                visualArchetype=definition.archetype;if(variant!=null)variant.gameObject.SetActive(false);
                if(variantCache.TryGetValue(visualArchetype,out var cached)){variant=cached;variant.gameObject.SetActive(true);}
                else
                {
                variant=new GameObject("Boss archetype / "+visualArchetype).transform;variant.SetParent(arena,false);
                if(visualArchetype=="caster")
                {Part("Caster mantle",PrimitiveType.Capsule,variant,new Vector3(0,2,0),new Vector3(1.3f,2,.8f),stone);Part("Focus orb",PrimitiveType.Sphere,variant,new Vector3(0,4.3f,0),Vector3.one*.9f,glow);Ring("Orbiting focus",variant,new Vector3(0,2.8f,0),2,.08f,brass);}
                else if(visualArchetype=="charger")
                {Part("Beast body",PrimitiveType.Capsule,variant,new Vector3(0,1.5f,0),new Vector3(1.8f,2.3f,1.8f),green,new Vector3(90,0,0));for(int i=0;i<4;i++)Part("Beast leg",PrimitiveType.Capsule,variant,new Vector3(i%2==0?-.7f:.7f,.6f,i<2?-1:1),new Vector3(.35f,.8f,.35f),dark);Part("Beast jaw",PrimitiveType.Cube,variant,new Vector3(0,1.7f,-1.8f),new Vector3(1.2f,.75f,1),stone);}
                else if(visualArchetype=="summoner")
                {Part("Summoning altar",PrimitiveType.Cylinder,variant,new Vector3(0,1,0),new Vector3(3,1,3),dark);for(int i=0;i<3;i++)Part("Soul vessel",PrimitiveType.Sphere,variant,new Vector3(i-1,2.7f,.4f),Vector3.one*.7f,red);Ring("Summoning seal",variant,new Vector3(0,.1f,0),2.5f,.12f,glow);}
                else if(visualArchetype=="environment")
                {Part("World heart",PrimitiveType.Sphere,variant,new Vector3(0,2.7f,0),Vector3.one*2,glow);for(int i=0;i<4;i++)Part("Orbit fragment",PrimitiveType.Cube,variant,new Vector3(Mathf.Cos(i*Mathf.PI/2)*2.3f,2,Mathf.Sin(i*Mathf.PI/2)*2.3f),Vector3.one*.7f,stone,new Vector3(25,i*45,35));}
                variantCache.Add(visualArchetype,variant);
                }
                previousBossHp=group.boss.visible.hp;visualPhase=-1;
            }
            if(hazard==null)
            {
                hazard=Ring("Persistent hazard",arena,Vector3.zero,3,.08f,red);phaseRing=Ring("Phase transition",arena,Vector3.zero,2.7f,.1f,brass);
                adds=new Transform[6];for(int i=0;i<adds.Length;i++){adds[i]=Part("Summoned echo",PrimitiveType.Capsule,arena,new Vector3(Mathf.Cos(i)*4,1,Mathf.Sin(i)*4+1),new Vector3(.45f,1,.45f),red);}
            }
            boss.gameObject.SetActive(definition.archetype=="guardian"&&group.boss.visible.hp>0);variant.gameObject.SetActive(definition.archetype!="guardian"&&group.boss.visible.hp>0);
            variant.localPosition=new Vector3(group.boss.x,.1f*Mathf.Sin(Time.time),group.boss.z);variant.localRotation=Quaternion.Euler(0,definition.archetype=="environment"?Time.time*15:0,0);
            for(int i=0;i<adds.Length;i++)adds[i].gameObject.SetActive(i<group.boss.adds);
            for(int i=0;i<4;i++)auras[i].GetComponent<Renderer>().sharedMaterial=group.boss.statuses.Any(s=>s.agent==i)?red:robes[i];
            hazard.gameObject.SetActive(group.boss.hazardLeft>0);hazard.localPosition=new Vector3(group.boss.hazardX,.09f,group.boss.hazardZ);
            if(group.boss.visible.hp<previousBossHp)impactUntil=Time.time+.25f;previousBossHp=group.boss.visible.hp;
            if(visualPhase!=group.boss.phase)phaseUntil=Time.time+1.2f;
            phaseRing.gameObject.SetActive(Time.time<impactUntil||Time.time<phaseUntil);phaseRing.localPosition=new Vector3(group.boss.x,.15f,group.boss.z);visualPhase=group.boss.phase;
            if(f.theme=="astral_foundry")SetFoundry(data,group);
            else
            {
                baseArchitecture.gameObject.SetActive(true);baseRestArchitecture.gameObject.SetActive(true);
                if(activeFoundry!=null)activeFoundry.gameObject.SetActive(false);if(activeMachine!=null)activeMachine.gameObject.SetActive(false);if(foundryRest!=null)foundryRest.gameObject.SetActive(false);
                activeFoundry=null;activeMachine=null;
                if(whiteHot==null)whiteHot=Mat(new Color(1,.85f,.55f),.4f,.7f,1.2f);
                ShowCombatCues(group);
            }
        }
    }
}
