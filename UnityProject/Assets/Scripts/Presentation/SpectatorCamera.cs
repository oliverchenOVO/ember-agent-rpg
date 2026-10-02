using Ember.Core;
using UnityEngine;
namespace Ember.Presentation
{
    public sealed partial class WorldView
    {
        Vector3 eventFocus;float eventUntil,eventCooldown,manualUntil;int cameraFloor=-1;readonly bool[] cameraAlive={true,true,true,true};
        Vector3 SpectatorFocus(World w,float orbit)
        {
            bool battle=w.phase==Phase.Battle;var overview=battle?new Vector3(-2,0,0):new Vector3(-1,0,0);
            if(orbit!=0){manualUntil=Time.time+6;eventUntil=0;}
            if(Time.time<manualUntil)return overview;
            if(cameraFloor!=w.floor){cameraFloor=w.floor;eventFocus=new Vector3(observedGroup?.boss.x??0,1,observedGroup?.boss.z??1);eventUntil=Time.time+2;eventCooldown=Time.time+8;}
            for(int i=0;i<w.agents.Count;i++)
            {
                var a=w.agents[i];bool present=observedGroup==null||observedGroup.members.Contains(i);
                if(present&&cameraAlive[i]&&!a.alive&&Time.time>eventCooldown){eventFocus=new Vector3(a.x,.5f,a.z);eventUntil=Time.time+2.5f;eventCooldown=Time.time+10;}
                cameraAlive[i]=a.alive;
            }
            if(battle&&observedGroup!=null&&observedGroup.boss.transitioned&&Time.time>eventCooldown){eventFocus=new Vector3(observedGroup.boss.x,1,observedGroup.boss.z);eventUntil=Time.time+2;eventCooldown=Time.time+10;}
            if(!battle)eventUntil=0;
            camera.fieldOfView=Mathf.Lerp(camera.fieldOfView,Time.time<eventUntil?37:42,Time.unscaledDeltaTime*2);return Time.time<eventUntil?Vector3.Lerp(overview,eventFocus,.45f):overview;
        }
        public void ShowInfusions(World w,Catalog catalog)
        {
            for(int i=0;i<4;i++)
            {
                var a=w.agents[i];if(weaponFx[i]==null){weaponFx[i]=Part("Infusion cast origin",PrimitiveType.Sphere,bodies[i],new Vector3(.5f,1.8f,0),Vector3.one*.18f,glow);}
                bool infused=!string.IsNullOrEmpty(a.weapon.infusion);weaponFx[i].gameObject.SetActive(a.alive&&(infused||a.enchant>0));string type=catalog.Item(a.weapon.id).weapon;
                weaponFx[i].localPosition=new Vector3(.55f,type=="Bow"?1.15f:type=="Staff"?2.1f:1.5f,0);weaponFx[i].localScale=type=="Greatsword"?new Vector3(.12f,1.1f,.12f):Vector3.one*(.2f+.03f*Mathf.Sin(Time.time*5));weaponFx[i].GetComponent<Renderer>().sharedMaterial=a.enchant>0?red:glow;
            }
            if(observedGroup==null)return;var cue=observedGroup.boss.skillPresentation;if(cue.serial==lastSkillSerial)return;lastSkillSerial=cue.serial;
            var actor=w.agents[cue.agent];Vector3 from=new Vector3(actor.x+.5f,cue.origin=="Staff"?2.1f:1.4f,actor.z);Vector3 to=cue.target>=0?new Vector3(w.agents[cue.target].x,1.5f,w.agents[cue.target].z):new Vector3(observedGroup.boss.x,2,observedGroup.boss.z);AddEffect(from,to,cue.element=="Fire"?red:cue.element=="Light"?glow:robes[cue.agent]);
        }
    }
}
