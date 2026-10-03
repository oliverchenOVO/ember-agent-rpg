using System.Collections.Generic;
using Ember.Core;
using Ember.Core.Phase2;
using UnityEngine;
namespace Ember.Presentation
{
    public sealed partial class WorldView
    {
        sealed class VisualBurst
        {
            public Transform root,core,ring;public Transform[] shards;public LineRenderer line;
            public Vector3 from,to;public string mode;public float age,duration,radius=2,length=10,inner,angle;public int source=-1;
        }
        readonly List<VisualBurst> bursts=new List<VisualBurst>();readonly Stack<VisualBurst> burstPool=new Stack<VisualBurst>();
        readonly Transform[] actorWeaponRoots=new Transform[4],barriers=new Transform[4];readonly float[] castPoses=new float[4];
        BossRuntime animatedBoss;float artTime;int consumedSkills,consumedBoss;Material lightningLine,poisonArt;readonly Vector3[] linePoints=new Vector3[9];
        public int SkillAnimationsPlayed{get;private set;}public int BossAnimationsPlayed{get;private set;}public int ActiveCombatAnimations=>bursts.Count;
        public void ReplayVisualEvents(){consumedSkills=0;consumedBoss=0;}
        VisualBurst AcquireBurst(string mode,Vector3 from,Vector3 to,Material material,float duration,int source=-1)
        {
            if(noHitFx||bursts.Count>=48)return null;VisualBurst v;
            if(burstPool.Count>0)v=burstPool.Pop();else
            {
                v=new VisualBurst{root=Pivot("Pooled readable combat animation",world,Vector3.zero),shards=new Transform[12]};
                v.core=Part("Projectile or impact core",PrimitiveType.Sphere,v.root,Vector3.zero,Vector3.one,material);
                v.ring=Ring("Attack impact ripple",v.root,Vector3.zero,1,.04f,material,40);
                for(int j=0;j<12;j++)v.shards[j]=Part("Attack fragment",PrimitiveType.Cube,v.root,Vector3.zero,Vector3.one*.1f,material);
                var lineObject=Pivot("Spell ribbon",v.root,Vector3.zero);v.line=lineObject.gameObject.AddComponent<LineRenderer>();v.line.useWorldSpace=true;v.line.numCapVertices=2;v.line.numCornerVertices=2;
                if(lightningLine==null){lightningLine=new Material(Shader.Find("Sprites/Default"));ownedMaterials.Add(lightningLine);}v.line.sharedMaterial=lightningLine;
            }
            v.root.gameObject.SetActive(true);v.mode=mode;v.from=from;v.to=to;v.age=0;v.duration=duration;v.source=source;v.radius=2;v.length=10;v.inner=0;v.angle=0;
            v.core.GetComponent<Renderer>().sharedMaterial=material;v.ring.GetComponent<Renderer>().sharedMaterial=material;foreach(var shard in v.shards)shard.GetComponent<Renderer>().sharedMaterial=material;
            v.line.startColor=material.color;v.line.endColor=material.color;v.line.startWidth=.12f;v.line.endWidth=.045f;v.line.positionCount=0;bursts.Add(v);EffectSerial++;return v;
        }
        Material SpellMaterial(string skill,string element)
        {
            if(skill=="poison"){if(poisonArt==null)poisonArt=Mat(new Color(.42f,.95f,.18f),.1f,.6f,1.4f);return poisonArt;}
            return element=="Fire"?red:element=="Ice"?iceArt:element=="Light"?whiteHot:element=="Dark"||element=="Arcane"?soulArt:brass;
        }
        void BindVisualEventStream()
        {
            if(observedGroup==null)return;var b=observedGroup.boss;if(animatedBoss==b)return;
            ClearVisualBursts();if(artRig!=null){artRig.strike=0;artRig.dash=0;}animatedBoss=b;consumedSkills=b.skillPresentation.serial;consumedBoss=b.presentationSerial;
            for(int i=0;i<4;i++)castPoses[i]=0;
        }
        void ClearVisualBursts()
        {foreach(var v in bursts){v.root.gameObject.SetActive(false);burstPool.Push(v);}bursts.Clear();}
        void EmitSkillAnimation(World w,SkillPresentation e)
        {
            if(e.agent<0||e.agent>=4)return;var a=w.agents[e.agent];if(!a.alive||a.escaped||!observedGroup.members.Contains(a.id))return;
            SkillAnimationsPlayed++;castPoses[a.id]=.7f;var from=new Vector3(e.x,1.6f,e.z);var to=e.target>=0?new Vector3(w.agents[e.target].x,.15f,w.agents[e.target].z):new Vector3(observedGroup.boss.x,1.8f,observedGroup.boss.z);string id=e.skill;
            var mat=SpellMaterial(id,e.element);
            string mode=id=="basic"?(e.origin=="Sword"||e.origin=="Greatsword"?"slash":e.origin=="Bow"?"arrow":"orb"):id=="slash"||id=="wave"||id=="counter"?"slash":id=="shot"||id=="pierce"||id=="snipe"||id=="poison"?"arrow":id=="fireball"||id=="frost"||id=="holy"?"orb":id=="lightning"?"lightning":id=="meteor"?"meteor":id=="rain"?"rain":id=="trap"?"trap":id=="heal"||id=="cleanse"||id=="revive"?"heal":"aura";
            if(id=="guard"||id=="shield"||id=="rage"||id=="enchant"||id=="taunt")to=new Vector3(a.x,.1f,a.z);
            if(id=="groupheal"||id=="ward")
            {foreach(int member in observedGroup.members){var target=w.agents[member];if(target.alive&&!target.escaped)AcquireBurst(id=="groupheal"?"heal":"aura",from,new Vector3(target.x,.1f,target.z),mat,1.15f,a.id);}return;}
            if(mode=="lightning")from=to+Vector3.up*8;
            var v=AcquireBurst(mode,from,to,mat,id=="meteor"?1.3f:id=="revive"?1.5f:1.05f,a.id);if(v!=null){v.radius=id=="wave"?3:id=="trap"?3:id=="meteor"?3:1.25f;}
        }
        void StopBossChargeAnimation()
        {for(int i=bursts.Count-1;i>=0;i--)if(bursts[i].mode=="chargeup"){var finished=bursts[i];finished.root.gameObject.SetActive(false);bursts.RemoveAt(i);burstPool.Push(finished);}}
        void EmitBossAnimation(BossPresentationEvent e)
        {
            BossAnimationsPlayed++;if(artRig==null)return;
            var ability=artData.Ability(e.ability);var from=new Vector3(e.x,2.5f,e.z);var to=new Vector3(e.targetX,.12f,e.targetZ);
            if(e.kind=="Phase"){AcquireBurst("aura",from,new Vector3(e.x,.1f,e.z),whiteHot,1.2f);return;}
            if(e.kind=="Interrupt"){StopBossChargeAnimation();AcquireBurst("shatter",from,from,iceArt,.7f);return;}
            if(ability==null)return;
            var mat=SpellMaterial("",ability.element);
            if(e.kind=="Windup"){var charge=AcquireBurst("chargeup",from,from,mat,Mathf.Min(ability.windup,2));if(charge!=null)charge.radius=.9f;return;}
            StopBossChargeAnimation();
            artRig.strike=.55f;
            if(ability.mechanic==Mechanic.Charge){artRig.dash=.35f;artRig.dashFrom=new Vector3(e.x,0,e.z);AcquireBurst("dash",from,to,mat,.65f);}
            string mode=ability.mechanic==Mechanic.Projectile?"orb":ability.mechanic==Mechanic.Drain?"drain":ability.mechanic==Mechanic.Summon?"summon":ability.mechanic==Mechanic.Shield?"aura":e.shape==CueShape.Lane?"lane":e.shape==CueShape.Cross?"cross":e.shape==CueShape.Annulus?"annulus":"slam";
            if(mode=="drain"){var swap=from;from=to+Vector3.up*1.2f;to=swap;}
            if(mode=="aura"||mode=="summon")to=new Vector3(e.x,.1f,e.z);
            var burst=AcquireBurst(mode,from,to,mat,mode=="orb"?.8f:1.15f);if(burst!=null){burst.radius=e.radius>0?e.radius:2;burst.inner=e.innerRadius;burst.length=e.length;burst.angle=e.angle;}
        }
        void UpdateCombatAnimation(World w,float dt)
        {
            if(observedGroup==null)return;BindVisualEventStream();var b=observedGroup.boss;bool battle=w.phase==Phase.Battle;
            if(!battle)ClearVisualBursts();else
            {
                if(b.skillEvents!=null)foreach(var e in b.skillEvents)if(e.serial>consumedSkills){EmitSkillAnimation(w,e);consumedSkills=e.serial;}
                if(b.presentationEvents!=null)foreach(var e in b.presentationEvents)if(e.serial>consumedBoss){EmitBossAnimation(e);consumedBoss=e.serial;}
            }
            for(int i=bursts.Count-1;i>=0;i--)
            {
                var v=bursts[i];v.age+=dt;if(v.age>=v.duration||v.source>=0&&(!w.agents[v.source].alive||w.agents[v.source].escaped||!observedGroup.members.Contains(v.source)))
                {v.root.gameObject.SetActive(false);bursts.RemoveAt(i);burstPool.Push(v);continue;}AnimateBurst(v);
            }
            artTime+=dt;AnimateBossArt(dt);
            for(int i=0;i<4;i++)
            {
                castPoses[i]=Mathf.Max(0,castPoses[i]-dt);if(actorWeaponRoots[i]!=null)actorWeaponRoots[i].localRotation=Quaternion.Euler(-Mathf.Sin(castPoses[i]*6)*40,Mathf.Sin(castPoses[i]*8)*25,Mathf.Sin(castPoses[i]*6)*22);
                if(barriers[i]==null){barriers[i]=Pivot("Living Agent barrier cage",bodies[i],Vector3.zero);for(int j=0;j<3;j++){var ring=Ring("Arcane shield rib",barriers[i],new Vector3(0,1,0),1,.035f,iceArt,32);ring.localRotation=Quaternion.Euler(j*60,0,j*45);}}
                bool shield=b.effects.Exists(e=>e.agent==i&&e.kind=="Shield"&&e.left>0&&e.power>0);barriers[i].gameObject.SetActive(battle&&w.agents[i].alive&&!w.agents[i].escaped&&observedGroup.members.Contains(i)&&shield);
                barriers[i].localRotation=Quaternion.Euler(0,artTime*30,0);
            }
        }
        void AnimateBurst(VisualBurst v)
        {
            float p=Mathf.Clamp01(v.age/v.duration),fade=1-p;bool projectile=v.mode=="orb"||v.mode=="arrow"||v.mode=="drain";bool cast=v.mode=="chargeup";
            Vector3 center=v.to;if(projectile)center=Vector3.Lerp(v.from,v.to,Mathf.Clamp01(p*1.6f));if(v.mode=="meteor")center=v.to+Vector3.up*(Mathf.Max(0,1-p*2)*8);
            v.root.position=Vector3.zero;v.core.position=center;v.core.rotation=Quaternion.LookRotation((v.to-v.from).sqrMagnitude>.001f?(v.to-v.from).normalized:Vector3.forward);
            v.core.gameObject.SetActive(projectile||v.mode=="meteor"||v.mode=="slam"||cast);v.core.localScale=v.mode=="arrow"?new Vector3(.1f,.1f,1.5f):v.mode=="meteor"?Vector3.one*.9f:Vector3.one*(cast?.4f+p*.6f:.45f*fade+.18f);
            v.ring.gameObject.SetActive(!projectile&&!cast);v.ring.position=new Vector3(v.to.x,.12f,v.to.z);v.ring.localScale=Vector3.one*(v.mode=="annulus"?v.radius:Mathf.Max(.1f,v.radius*p));
            bool beam=v.mode=="lightning"||v.mode=="drain"||v.mode=="lane"||v.mode=="cross"||v.mode=="dash"||projectile;v.line.enabled=beam;
            if(beam)
            {
                v.line.positionCount=9;Vector3 axis=new Vector3(Mathf.Cos(v.angle),0,Mathf.Sin(v.angle));
                for(int j=0;j<9;j++)
                {
                    float f=j/8f;Vector3 q=projectile?Vector3.Lerp(center-(v.to-v.from).normalized*.9f,center,f):Vector3.Lerp(v.from,v.to,f);
                    if(v.mode=="lane")q=v.to+axis*((f-.5f)*v.length)+Vector3.up*(.3f+fade*.9f);
                    else if(v.mode=="cross")q=v.to+(j<5?axis*(j-2)*v.length/4:new Vector3(-axis.z,0,axis.x)*(j-6)*v.length/4)+Vector3.up*.5f;
                    else if(v.mode=="lightning"&&j>0&&j<8)q+=new Vector3(Mathf.Sin(j*2.7f+v.age*40),Mathf.Cos(j*3.1f),Mathf.Sin(j*4))* .27f;
                    else if(v.mode=="drain")q+=Vector3.up*Mathf.Sin(f*Mathf.PI)*1.2f;
                    linePoints[j]=q;
                }
                v.line.SetPositions(linePoints);v.line.startWidth=(v.mode=="lane"||v.mode=="cross"?.55f:v.mode=="lightning"?.3f:.17f)*Mathf.Max(.1f,fade);v.line.endWidth=v.line.startWidth;
            }
            for(int j=0;j<v.shards.Length;j++)
            {
                float a=j*Mathf.PI/6+v.age*2;Vector3 offset=new Vector3(Mathf.Cos(a),0,Mathf.Sin(a));var shard=v.shards[j];shard.gameObject.SetActive(true);
                Vector3 position=center+offset*(.25f+p*.6f)+Vector3.up*Mathf.Sin(a)*.15f;Vector3 scale=Vector3.one*(.1f*fade+.035f);
                if(v.mode=="slash"){float sweep=-1.2f+p*2.4f+j*.08f;position=v.from+new Vector3(Mathf.Sin(sweep),.1f,Mathf.Cos(sweep))* (1.3f+j*.035f);scale=new Vector3(.12f,.08f,.45f)*fade;}
                else if(v.mode=="rain"){position=v.to+offset*(j%3*.7f)+Vector3.up*Mathf.Max(.15f,6*(1-(p+j*.025f)*1.8f));scale=new Vector3(.06f,.9f,.06f);}
                else if(v.mode=="lane"||v.mode=="cross"){var axis=j%2==0?new Vector3(Mathf.Cos(v.angle),0,Mathf.Sin(v.angle)):new Vector3(-Mathf.Sin(v.angle),0,Mathf.Cos(v.angle));if(v.mode=="lane")axis=new Vector3(Mathf.Cos(v.angle),0,Mathf.Sin(v.angle));position=v.to+axis*((j/11f-.5f)*v.length)+Vector3.up*(.4f+Mathf.Sin(p*Mathf.PI)*1.4f);scale=new Vector3(.14f,.7f,.14f)*fade;}
                else if(v.mode=="annulus"){position=v.to+offset*Mathf.Lerp(v.inner+.2f,v.radius,j%3/2f)+Vector3.up*(.2f+fade*.6f);scale=new Vector3(.12f,.6f,.12f)*fade;}
                else if(v.mode=="trap"){position=v.to+offset*1.3f+Vector3.up*.2f;scale=new Vector3(.15f,.5f,.15f);}
                else if(v.mode=="aura"||v.mode=="heal"||v.mode=="summon"){position=v.to+offset*(.45f+j%3*.25f)+Vector3.up*(.2f+(p*3+j*.2f)%2.6f);scale=new Vector3(.1f,.3f,.1f)*fade;}
                else if(v.mode=="slam"||v.mode=="meteor"||v.mode=="shatter"){position=v.to+offset*(.3f+p*v.radius)+Vector3.up*(Mathf.Sin(p*Mathf.PI)*1.4f);scale=Vector3.one*(.2f*fade+.03f);}
                else if(cast){position=v.from+offset*(1-p)*v.radius+Vector3.up*Mathf.Sin(a)*.5f;scale=Vector3.one*.12f;}
                shard.position=position;shard.localScale=scale;shard.rotation=Quaternion.Euler(j*23+v.age*120,j*29,45);
            }
        }
    }
}
