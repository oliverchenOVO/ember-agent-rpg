using System;
using System.Linq;
using System.IO;
using Ember.Core;
using Ember.Core.Phase2;
using UnityEngine;
namespace Ember.Editor
{
    public static class RefugeValidation
    {
        static void Check(bool ok,string why){if(!ok)throw new Exception(why);}
        public static void Run()
        {
            var c=JsonUtility.FromJson<Catalog>(Resources.Load<TextAsset>("catalog").text);var d=TowerContent.Load();
            using(var s=new TowerSimulation(c,d,1729,true))
            {
                var g=s.State.groups[0];s.EnterRest(g);var a=s.State.world.agents[0];
                for(int variant=0;variant<6;variant++)
                {g.refugeVariant=variant;var map=RefugeMap.For(g);foreach(var site in s.SpatialSites(g)){var p=new Vector2(site.x,site.z);var route=map.Path(RefugeMap.Entry,p);Check(route.Count>0,"Unreachable facility");var prev=RefugeMap.Entry;foreach(var point in route){Check(map.Clear(prev,point),"Route crosses wall");prev=point;}Check(map.Distance(p,RefugeMap.Exit)>0,"No escape route");}Check(!map.Walkable(new Vector2(-10,0)),"Wall not blocking");Check(map.Distance(RefugeMap.Entry,RefugeMap.Exit)>Vector2.Distance(RefugeMap.Entry,RefugeMap.Exit)+1,"Route ignores detour");}
                g.refugeVariant=0;g.availableRestSites.Clear();g.availableRestSites.AddRange(d.Rest(g.restId).sites.Select(site=>site.id));
                Check(s.Context(a,g).restOptions.All(site=>site.effect=="Search"),"Unknown facilities leaked to Agent");
                var plan=s.State.Plan(0);var room=RefugeMap.For(g).rooms[0];a.x=room.x;a.z=room.y;plan.nextDecision=10000;plan.decision=new HighDecision{intent="Explore",proposedAction="search:0"};
                for(int i=0;i<30;i++)s.Step();Check(plan.knownRooms.Contains(0),"Real room search did not discover facilities");Check(s.Context(a,g).restOptions.Any(site=>site.effect!="Search"),"Discovered facility absent from reasoning");
                plan.discussionLeft=3;var before=new Vector2(a.x,a.z);float clock=g.phaseClock;s.Step();Check(new Vector2(a.x,a.z)==before&&g.phaseClock>clock&&plan.discussionLeft<3,"Discussion did not consume time");
                plan.discussionLeft=0;plan.workLeft=0;plan.nextDecision=10000;plan.decision=new HighDecision{intent="Exit"};a.x=-28;a.z=0;
                for(int i=0;i<12;i++)s.Step();string save=JsonUtility.ToJson(s.State);
                using(var resumed=new TowerSimulation(c,d,1,true))
                {resumed.Restore(ExpeditionStore.Parse(save,c,d));for(int i=0;i<30;i++){s.Step();resumed.Step();}Check(JsonUtility.ToJson(s.State).Replace("\"generation\":1","\"generation\":2")==JsonUtility.ToJson(resumed.State),"Mid-route save/load replay diverged");}
            }
            // Real timed movement: safe departure survives, excessive discussion at a late search location dies.
            foreach(bool delayed in new[]{false,true})using(var s=new TowerSimulation(c,d,1729,true))
            {
                var g=s.State.groups[0];s.EnterRest(g);foreach(var other in s.State.world.agents.Skip(1)){other.alive=false;other.hp=0;}
                var a=s.State.world.agents[0];var p=s.State.Plan(0);p.nextDecision=10000;p.decision=new HighDecision{intent="Exit"};
                if(delayed){a.x=-20;a.z=-12;g.phaseClock=RefugeMap.Limit(d.Rest(g.restId),g);p.discussionLeft=20;}
                for(int i=0;i<230&&!a.escaped&&a.alive;i++)s.Step();Check(delayed?!a.alive:a.escaped,"Timed departure result incorrect");
            }
            using(var s=new TowerSimulation(c,d,1729,true))
            {
                var g=s.State.groups[0];s.EnterRest(g);g.refugeVariant=0;
                foreach(var other in s.State.world.agents.Skip(1)){other.alive=false;other.hp=0;}
                var a=s.State.world.agents[0];var plan=s.State.Plan(0);plan.nextDecision=10000;int[] order={2,3,1,5,0,4};int index=0;
                for(int tick=0;tick<900&&a.alive&&!a.escaped;tick++)
                {
                    plan.nextDecision=10000;
                    while(index<order.Length&&plan.knownRooms.Contains(order[index]))index++;
                    if(plan.workLeft==0)plan.decision=new HighDecision{intent=index<order.Length?"Explore":"Exit",proposedAction=index<order.Length?"search:"+order[index]:""};
                    s.Step();
                }
                Check(!a.alive&&plan.knownRooms.Count>0,"Excessive actual searching did not reach collapse");
                Directory.CreateDirectory("../Artifacts");File.WriteAllText("../Artifacts/refuge-oversearch-fixture.json",JsonUtility.ToJson(s.State,true));
                Debug.Log("REFUGE OVERSEARCH / rooms="+plan.knownRooms.Count+" / elapsed="+g.phaseClock+" / died="+!a.alive);
            }
            foreach(var scenario in new[]{"combat","rest","split","before_boss","after_death","before_10","next_life"})
            using(var original=new TowerSimulation(c,d,1729,true))using(var resumed=new TowerSimulation(c,d,1729,true))
            {
                var g=original.State.groups[0];
                if(scenario=="before_boss"||scenario=="before_10"){g.floor=scenario=="before_10"?10:9;g.boss=BossRuntime.Create(d.Boss(d.Floor(g.floor).bossId));}
                if(scenario=="rest"||scenario=="split"){original.EnterRest(g);if(scenario=="split")original.Split(g.id,new[]{2,3});}
                if(scenario=="after_death")original.Die(original.State.world.agents[0],"fixture");if(scenario=="next_life")original.NewLife();
                resumed.Restore(ExpeditionStore.Parse(JsonUtility.ToJson(original.State),c,d));
                for(int t=0;t<300;t++){original.Step();resumed.Step();}
                Check(JsonUtility.ToJson(original.State.world)==JsonUtility.ToJson(resumed.State.world)&&JsonUtility.ToJson(original.State.groups)==JsonUtility.ToJson(resumed.State.groups),"Save replay differs: "+scenario);
                Debug.Log("REFUGE REPLAY / "+scenario+" / PASSED");
            }
            foreach(uint seed in new uint[]{1729,42})using(var s=new TowerSimulation(c,d,seed,true))
            {
                var g=s.State.groups[0];s.EnterRest(g);int ticks=0;
                for(;ticks<900&&g.phase==Phase.Rest&&g.travelLeft==0&&!g.terminal;ticks++)
                {
                    s.Step();foreach(var agent in s.State.Members(g).Where(a=>a.alive&&!a.escaped))if(g.phase==Phase.Rest)Check(RefugeMap.For(g).Walkable(new Vector2(agent.x,agent.z)),"Autonomous Agent crossed wall");
                }
                Check(g.travelLeft>0||g.terminal||g.phase!=Phase.Rest,"Autonomous refuge visit did not resolve within collapse window");
                Debug.Log("REFUGE AUTONOMOUS / seed="+seed+" / ticks="+ticks+" / escaped="+s.State.world.agents.Count(a=>a.escaped)+" / dead="+s.State.world.agents.Count(a=>!a.alive));
            }
            LocalizationValidation.Run();RestInteractionValidation.RunFocused();
            Debug.Log("REFUGE RUNTIME / "+Phase31Validation.RuntimeHash());
            Debug.Log("REFUGE EXPLORATION VALIDATION PASSED / six layouts, all facilities reachable, radius-aware obstacle routes, discovery, paid discussion, mid-route replay, timely escape, delayed collapse");
        }
    }
}
