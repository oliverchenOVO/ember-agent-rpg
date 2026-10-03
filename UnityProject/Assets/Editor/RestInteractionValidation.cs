using System;
using System.Linq;
using Ember.Core;
using Ember.Core.Phase2;
using UnityEngine;

namespace Ember.Editor
{
    public static class RestInteractionValidation
    {
        public static void Run(){RunFocused();Phase31Validation.ShortGate();}
        public static void RunFocused()
        {
            var c=JsonUtility.FromJson<Catalog>(Resources.Load<TextAsset>("catalog").text);var data=TowerContent.Load();int empty=0,partial=0;
            for(uint seed=1;seed<=64;seed++)
                using(var s=new TowerSimulation(c,data,seed,true))
                {
                    var g=s.State.groups[0];s.EnterRest(g);int count=g.availableRestSites.Count;
                    if(count==0)empty++;if(count>0&&count<data.Rest(g.restId).sites.Length)partial++;
                    var parsed=ExpeditionStore.Parse(JsonUtility.ToJson(s.State),c,data);
                    if(!parsed.groups[0].availableRestSites.SequenceEqual(g.availableRestSites)||!parsed.groups[0].restSitesRolled)throw new Exception("Facility layout changed after save/load");
                    var branch=s.Split(g.id,new[]{2,3});if(branch==null||!branch.availableRestSites.SequenceEqual(g.availableRestSites))throw new Exception("Split lost facility layout");
                }
            if(empty==0||partial==0)throw new Exception("Random layouts did not include empty and partial refuges");
            using(var s=new TowerSimulation(c,data,1729,true))
            {
                var g=s.State.groups[0];s.EnterRest(g);g.availableRestSites.Clear();var a=s.State.world.agents[0];
                if(s.Context(a,g).allowedSites.Any(id=>!id.StartsWith("search:"))||s.Context(a,g).restOptions.Any(site=>site.effect!="Search"))throw new Exception("Absent facilities offered to AI");
                var plan=s.State.Plan(0);plan.decision.intent="Forge";plan.decision.proposedAction="forge";plan.nextDecision=1000;int materials=a.materials;
                s.Step();if(plan.workLeft!=0||a.materials!=materials||plan.decision.intent!="Exit")throw new Exception("Absent forge accepted interaction");
                g.restSitesRolled=false;if(data.Rest(g.restId).AvailableSites(g).Length!=data.Rest(g.restId).sites.Length)throw new Exception("Legacy save facilities changed");
                string legacy=System.Text.RegularExpressions.Regex.Replace(JsonUtility.ToJson(s.State),@",?""restSitesRolled"":(?:true|false)","");
                legacy=System.Text.RegularExpressions.Regex.Replace(legacy,@",?""availableRestSites"":\[[^\]]*\]","");
                legacy=System.Text.RegularExpressions.Regex.Replace(legacy,@",?""refuge(?:Version|Variant)"":\d+","");
                var migrated=ExpeditionStore.Parse(legacy,c,data);var legacyGroup=migrated.groups[0];
                if(legacyGroup.restSitesRolled||data.Rest(legacyGroup.restId).AvailableCount(legacyGroup)!=data.Rest(legacyGroup.restId).sites.Length)throw new Exception("Old JSON save lost its facilities");
            }
            using(var s=new TowerSimulation(c,data,1729,true))
            {
                var g=s.State.groups[0];var a=s.State.world.agents[0];
                g.boss.effects.Add(new CombatEffect{agent=-1,source=a.id,kind="Burn",left=5,power=20,element="Fire"});
                var p=s.State.Plan(a.id);p.discussionLeft=3;p.decision.intent="Explore";s.Die(a,"fixture");float damage=a.damage;s.Step();
                if(a.alive||a.damage<=damage)throw new Exception("Dead source DOT provenance fixture failed");
                var healer=s.State.world.agents[3];healer.unlocked.Add("revive");healer.equipped.Clear();healer.equipped.Add("revive");healer.mp=healer.MaxMp;healer.x=a.x+.5f;healer.z=a.z;
                if(!s.Cast(healer,g,"revive",a.id))throw new Exception("Revival fixture failed");
                s.Step();if(!a.alive||p.discussionLeft>0||p.decision.intent=="Explore")throw new Exception("Revived Agent retained unfinished refuge discussion");
            }
            Debug.Log("REST INTERACTION VALIDATION PASSED / empty="+empty+" / partial="+partial+" / 64 bounded layout fixtures; save/load, split, missing facilities, legacy, dead DOT");
        }
    }
}
