using System;
using System.Collections.Generic;
using UnityEngine;

namespace Ember.Core.Phase2
{
    [Serializable] public class SkillCount { public string id; public int count; }
    [Serializable] public class UnitTelemetry
    {
        public int agent; public string profession,weapon; public float damage,damageTaken,healing,quality; public int potions,deaths;
    }
    [Serializable] public class FloorTelemetry
    {
        public uint seed;public int run,group,floor,deaths,potions,loot,crafts,splits,rejoins,restActions,collapses,survivors;
        public string boss,result="Active",finalResult="";public float start,clearTime,damage,damageTaken,healing,gearQuality;
        public List<UnitTelemetry> team=new List<UnitTelemetry>();public List<SkillCount> skills=new List<SkillCount>();
        public List<SkillCount> lootItems=new List<SkillCount>(),restSites=new List<SkillCount>();
    }
    public sealed partial class TowerSimulation
    {
        public bool TelemetryEnabled;public uint TelemetrySeed;
        public FloorTelemetry Telemetry(GroupState g)
        {
            if(!TelemetryEnabled||g==null)return null;
            var row=State.telemetry.FindLast(r=>r.run==State.world.run&&r.group==g.id&&r.floor==g.floor);
            if(row!=null)return row;
            if(g.phase!=Phase.Battle){return State.telemetry.FindLast(r=>r.run==State.world.run&&r.floor==g.floor&&r.team.Exists(u=>g.members.Contains(u.agent)));}
            row=new FloorTelemetry{seed=TelemetrySeed,run=State.world.run,group=g.id,floor=g.floor,boss=g.boss.definition,start=State.world.clock};
            foreach(var a in State.Members(g))if(a.alive)row.team.Add(new UnitTelemetry{agent=a.id,profession=a.profession.ToString(),weapon=a.weapon.id,quality=a.weapon.quality});
            State.telemetry.Add(row);return row;
        }
        void Measure(Agent a,string kind,float amount=1,string id="")
        {
            var r=Telemetry(State.GroupOf(a.id));if(r==null)return;var u=r.team.Find(t=>t.agent==a.id);
            switch(kind)
            {
                case "damage":r.damage+=amount;if(u!=null)u.damage+=amount;break;
                case "hurt":r.damageTaken+=amount;if(u!=null)u.damageTaken+=amount;break;
                case "heal":r.healing+=amount;if(u!=null)u.healing+=amount;break;
                case "potion":r.potions++;if(u!=null)u.potions++;break;
                case "death":r.deaths++;if(u!=null)u.deaths++;if(id.Contains("collapse"))r.collapses++;break;
                case "skill":var s=r.skills.Find(v=>v.id==id);if(s==null){s=new SkillCount{id=id};r.skills.Add(s);}s.count++;break;
                case "loot":r.loot+=(int)amount;var drop=r.lootItems.Find(v=>v.id==id);if(drop==null){drop=new SkillCount{id=id};r.lootItems.Add(drop);}drop.count+=(int)amount;break;
                case "craft":r.crafts++;break;
                case "rest":r.restActions++;var site=r.restSites.Find(v=>v.id==id);if(site==null){site=new SkillCount{id=id};r.restSites.Add(site);}site.count++;break;
            }
        }
        void CloseTelemetry(GroupState g,bool clear)
        {
            var r=Telemetry(g);if(r==null||r.result!="Active")return;
            r.result=clear?"Clear":"Wipe";r.clearTime=State.world.clock-r.start;
            r.survivors=0;float quality=0;foreach(var a in State.Members(g))if(a.alive){r.survivors++;quality+=a.weapon.quality;}
            r.gearQuality=r.survivors>0?quality/r.survivors:0;
        }
    }
}
