using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Ember.Core;
using Ember.Core.Phase2;
using UnityEngine;

namespace Ember.Editor
{
    public static class Phase2Validation
    {
        static int checks;static Catalog c;static TowerContent data;
        static void Check(bool condition,string name){if(!condition)throw new Exception("Phase 2 FAIL: "+name);checks++;Debug.Log("PHASE2 CHECK / "+name);}
        static TowerSimulation New(uint seed=1729)=>new TowerSimulation(c,data,seed,true);
        sealed class FakeTransport : IReasonerTransport
        {
            public string output;public bool hang;
            public Task<string> SendAsync(string input,CancellationToken cancellation)=>hang?new TaskCompletionSource<string>().Task:Task.FromResult(output);
        }
        static void UnitTests()
        {
            data.Validate(c);Check(data.floors.Length==25&&data.bosses.Length==25,"25 definitions");Check(data.bosses.Take(5).Select(b=>b.archetype).Distinct().Count()==5,"five archetypes");
            using(var s=New())
            {
                for(int n=1;n<=25;n++){var g=s.State.groups[0];Check(g.floor==n,"floor "+n);g.boss.visible.hp=0;s.EnterRest(g);foreach(var a in s.State.world.agents)a.escaped=true;s.NextFloor(g);}
                s.Step();Check(s.State.world.outcome==Outcome.TowerClear&&s.State.completedAgents.Count==4,"full 25F completion");
                s.NewLife();Check(s.State.world.floor==1&&s.State.world.agents.All(a=>a.level==0)&&s.State.memories.All(m=>m.entries.Any(e=>e.scope==MemoryScope.Survivor||e.scope==MemoryScope.LongTerm)),"survivor memory and growth reset");
            }
            using(var s=New())
            {
                var g=s.State.groups[0];g.boss.visible.hp*=.4f;s.Step();Check(g.boss.phase==1,"HP phase transition");
                g.boss.phase=0;g.boss.elapsed=56;g.boss.visible.hp=g.boss.visible.maxHp;s.Step();Check(g.boss.phase==1,"time phase transition");
            }
            using(var s=New())
            {
                var a=s.State.world.agents[0];var g=s.State.groups[0];a.x=-9;a.z=-7;g.boss.ability="slam";g.boss.visible.telegraph=true;g.boss.visible.windup=1.4f;g.boss.visible.targetX=a.x;g.boss.visible.targetZ=a.z;s.Step();Check(a.x>-9||a.z>-7,"dodge moves inward at arena boundary");
                int floor=g.floor;s.NextFloor(g);Check(g.floor==floor,"cannot bypass uncleared boss");
            }
            foreach(Mechanic mechanic in Enum.GetValues(typeof(Mechanic)))
            {
                using(var s=New())
                {
                    var g=s.State.groups[0];var a=s.State.world.agents[0];var ability=data.abilities.First(x=>x.mechanic==mechanic);a.x=a.z=0;
                    g.boss.ability=ability.id;g.boss.target=a.id;g.boss.visible.telegraph=true;g.boss.visible.windup=.01f;g.boss.visible.targetX=g.boss.visible.targetZ=0;
                    float before=a.hp;g.boss.Tick(data,new List<Agent>{a},s.State.world,.1f,s.Hurt);
                    Check(g.boss.casts==1,"ability executes "+mechanic);
                    if(mechanic==Mechanic.Summon){Check(g.boss.adds==2,"summoned adds exist");g.boss.Hit(data,a,50,"Physical",false);Check(g.boss.adds==1,"adds intercept damage");}
                    else if(mechanic==Mechanic.Shield)Check(g.boss.shield>0,"shield buff");
                    else if(mechanic==Mechanic.Survival)Check(g.boss.survivalLeft>0&&g.boss.Hit(data,a,50,"Fire",false)==0,"survival gate");
                    else Check(a.hp<before,"damaging mechanic "+mechanic);
                }
            }
            using(var s=New())
            {
                var g=s.State.groups[0];var a=s.State.world.agents[0];g.boss.ability="bolt";g.boss.visible.telegraph=true;g.boss.visible.windup=.2f;g.boss.Hit(data,a,10,"Fire",true);Check(g.boss.interrupts==1&&!g.boss.visible.telegraph,"interrupt window");
                var fire=BossRuntime.Create(data.bosses[0]);var dark=BossRuntime.Create(data.bosses[0]);Check(fire.Hit(data,a,50,"Fire",false)>dark.Hit(data,a,50,"Dark",false),"element weakness/resistance");
                g.boss.visible.hp=0;s.EnterRest(g);g.phaseClock=10;
                var b=s.Split(g.id,new[]{2,3});Check(b!=null&&s.State.groups.Count==2&&b.phaseClock==10,"2+2 split preserves clock");
                var solo=s.Split(b.id,new[]{3});Check(solo!=null&&s.State.groups.Count==3,"2+1+1 split");
                Check(s.Split(g.id,new[]{1})!=null&&s.State.groups.Count==4,"1+1+1+1 split");
                var other=s.State.GroupOf(1);other.phaseClock=15;Check(s.Rejoin(g.id,other.id)&&g.phaseClock==15,"safe rejoin keeps dangerous clock");
                Check(s.State.relationships.Any(e=>e.kind==RelationshipEventKind.Abandoned)&&s.State.relationships.Any(e=>e.kind==RelationshipEventKind.Rejoined),"relationship event evidence");
                solo.phaseClock=20;s.State.Plan(3).workLeft=2;Check(!s.Rejoin(g.id,solo.id),"cannot merge unfinished work");s.State.Plan(3).workLeft=0;
                foreach(var member in s.State.Members(solo))member.escaped=true;s.NextFloor(solo);Check(solo.floor==2&&g.floor==1&&!ReferenceEquals(solo.boss,g.boss),"independent progression/boss ownership");
                Check(!s.Rejoin(g.id,solo.id),"cannot merge across floors");
                string json=JsonUtility.ToJson(s.State);var parsed=ExpeditionStore.Parse(json,c,data);Check(JsonUtility.ToJson(parsed)==json&&parsed.relationships.Count==s.State.relationships.Count&&parsed.memories[0].salient.Count==s.State.memories[0].salient.Count,"split and relationship event save roundtrip");
                string path=Path.GetFullPath("../Artifacts/phase2-save.json");ExpeditionStore.Save(path,s.State);ExpeditionStore.Save(path,s.State);File.WriteAllText(path,"broken");Check(ExpeditionStore.Load(path,c,data).groups.Count==3,"split backup recovery");
                bool rejected=false;try{var invalid=JsonUtility.FromJson<ExpeditionState>(json);invalid.groups[0].members.Add(3);ExpeditionStore.Parse(JsonUtility.ToJson(invalid),c,data);}catch{rejected=true;}Check(rejected,"duplicate ownership rejected");
                using(var twin=New()){twin.Restore(parsed);for(int n=0;n<20;n++){s.Step();twin.Step();}Check(Canonical(s.State)==Canonical(twin.State),"split save deterministic continuation");}
            }
            using(var s=New())
            {
                var g=s.State.groups[0];s.EnterRest(g);Check(s.Split(g.id,new[]{3})!=null&&s.State.groups[0].members.Count==3,"3+1 split");
                var a=s.State.world.agents[0];var item=a.Make("bow");a.inventory.Add(item);Check(s.TransferItem(a,s.State.world.agents[1],item)&&!s.TransferItem(a,s.State.world.agents[1],item),"ownership transfer cannot duplicate item");s.State.Memory(0).Add(new Knowledge{key="private",text=Loc.Token("p2.memory.clear"),scope=MemoryScope.Run,source=KnowledgeSource.OwnExperience});
                s.Die(a,Loc.Token("cause.collapse"));Check(s.State.world.book.Count==1,"death writes book immediately");s.Finish(Outcome.Wipe);s.NewLife();Check(s.State.memories.All(m=>m.entries.Count==0)&&s.State.world.book.Count>=4,"wipe clears private memories and retains book");
                s.EnterRest(s.State.groups[0]);s.CompleteSite(s.State.world.agents[0],s.State.groups[0],data.rests[0].sites.First(x=>x.id=="book"));Check(s.State.Memory(0).entries.Any(k=>k.source==KnowledgeSource.BookOfDead),"book provenance");
                s.CompleteSite(s.State.world.agents[1],s.State.groups[0],data.rests[0].sites.First(x=>x.id=="library"));Check(s.State.Memory(0).entries.Any(k=>k.source==KnowledgeSource.ToldByAgent&&k.informant==1),"told-by provenance");
                foreach(var site in data.rests[0].sites)s.CompleteSite(s.State.world.agents[0],s.State.groups[0],site);Check(data.rests[0].sites.Length==9,"all nine rest sites execute");
                var mem=s.State.Memory(0);s.Die(s.State.world.agents[1],Loc.Token("cause.slam"));s.State.completedAgents.Add(0);s.Finish(Outcome.TowerClear);s.NewLife();Check(s.State.Memory(0).entries.Count>0&&s.State.Memory(1).entries.Count==0&&s.State.Memory(2).entries.Count==0,"only completed survivors retain memory");
            }
            var old=new Simulation(c,12,true);old.Finish(Outcome.SliceComplete);var migrated=ExpeditionStore.Migrate(SaveStore.Parse(JsonUtility.ToJson(old.State)),c,data);Check(migrated.groups[0].floor==1&&migrated.completedAgents.Count==0&&migrated.world.book.Count==4,"legacy migration does not fake progression");
            using(var s=New())
            {
                s.State.completedAgents.Add(0);s.State.world.agents[0].bonds[0].love=.9f;s.Finish(Outcome.Wipe);s.NewLife();Check(s.State.world.agents[0].bonds[0].love==0&&s.State.Memory(0).entries.Count==0,"aborted expedition grants no survivor retention");
                var g=s.State.groups[0];s.EnterRest(g);s.CompleteSite(s.State.world.agents[0],g,data.rests[0].sites.First(x=>x.id=="book"));g.phaseClock=24;var context=s.Context(s.State.world.agents[0],g);var influenced=new RuleBasedReasoner().Decide(context);context.bookConfidence=0;Check(influenced.utility>new RuleBasedReasoner().Decide(context).utility,"book confidence changes utility without forcing actions");
            }
            using(var s=New())
            {
                var context=s.Context(s.State.world.agents[0],s.State.groups[0]);var invalid=new LLMReasoner(new FakeTransport{output="{\"intent\":\"Teleport\",\"confidence\":1,\"riskLevel\":0,\"reasoningTags\":[]}"}){minimumIntervalSeconds=0,maxRetries=1,requestTokenBudget=8000};
                var decision=invalid.DecideAsync(context,CancellationToken.None).GetAwaiter().GetResult();Check(decision.provider=="fallback"&&invalid.requests==2,"invalid LLM retries/fallback");
                var timeout=new LLMReasoner(new FakeTransport{hang=true}){timeoutMs=25,minimumIntervalSeconds=0,maxRetries=0,requestTokenBudget=8000};
                Check(timeout.DecideAsync(context,CancellationToken.None).GetAwaiter().GetResult().provider=="fallback"&&timeout.Snapshot()[0].status=="timeout","uncooperative transport timeout fallback");
                var valid=new LLMReasoner(new FakeTransport{output="{\"intent\":\"Fight\",\"targetGoal\":\"Fight\",\"proposedAction\":\"\",\"reasoningTags\":[\"boss_strategy\"],\"riskLevel\":0.3,\"dialogueIntent\":\"\",\"confidence\":0.8}"}){minimumIntervalSeconds=100,maxRetries=0,requestTokenBudget=8000};
                Check(valid.DecideAsync(context,CancellationToken.None).GetAwaiter().GetResult().provider=="llm","valid structured decision accepted");Check(valid.DecideAsync(context,CancellationToken.None).GetAwaiter().GetResult().provider=="fallback","rate limit fallback");
                Check(LLMReasoner.Replay(valid.Snapshot()[0],context).intent=="Fight","decision replay");
                var omitted=new LLMReasoner(new FakeTransport{output="{\"intent\":\"Fight\",\"reasoningTags\":[\"confidence\",\"riskLevel\"]}"}){minimumIntervalSeconds=0,maxRetries=0,requestTokenBudget=8000};Check(omitted.DecideAsync(context,CancellationToken.None).Result.provider=="fallback","missing fields cannot hide inside tags");
                var budget=new LLMReasoner(new FakeTransport{hang=true}){runTokenBudget=1};Check(budget.DecideAsync(context,CancellationToken.None).Result.provider=="fallback"&&budget.requests==0,"token budget before network");
            }
        }
        static string Canonical(ExpeditionState s){int generation=s.generation;s.generation=0;string json=JsonUtility.ToJson(s);s.generation=generation;return json;}
        static int seedCount=1000;
        public static void Preview(){seedCount=30;Run();}
        public static void RunFull(){seedCount=1000;Run();}
        public static void Run()
        {
            checks=0;c=JsonUtility.FromJson<Catalog>(Resources.Load<TextAsset>("catalog").text);data=TowerContent.Load();Loc.SetLocale("zh-TW");UnitTests();
            int clears=0,wipes=0,crashes=0,softlocks=0,infiniteCombat=0,splitDeadlocks=0,corruption=0,impossible=0,totalSplits=0,totalRejoins=0,maxFloor=0;float maxSeconds=0;var lines=new List<string>();
            for(uint seed=1;seed<=seedCount;seed++)
            {
                if(seed%50==0)Debug.Log("PHASE2 SEED PROGRESS / "+seed);
                using(var s=new TowerSimulation(c,data,seed,false))
                {
                    bool ended=false;float lastProgress=0;string fingerprint="";bool saved=false;
                    try
                    {
                        for(int tick=0;tick<50000;tick++)
                        {
                            s.Step();maxFloor=Math.Max(maxFloor,s.State.groups.Max(g=>g.floor));
                            if(tick%100==0)
                            {
                                string current=string.Join("|",s.State.groups.Select(g=>g.floor+":"+g.phase+":"+(int)g.boss.visible.hp+":"+g.terminal))+string.Join(",",s.State.world.agents.Select(a=>(int)a.hp+":"+a.escaped));
                                if(current!=fingerprint){fingerprint=current;lastProgress=s.State.world.clock;}
                                if(s.State.world.clock-lastProgress>300){softlocks++;if(s.State.groups.Any(g=>g.phase==Phase.Battle))infiniteCombat++;else splitDeadlocks++;break;}
                                if(s.State.groups.Any(g=>g.floor<1||g.floor>25)){impossible++;break;}
                            }
                            if(!saved&&(s.State.splits>0||tick==100))
                            {saved=true;string json=JsonUtility.ToJson(s.State);if(JsonUtility.ToJson(ExpeditionStore.Parse(json,c,data))!=json){corruption++;break;}}
                            if(s.State.world.phase==Phase.Ended){ended=true;break;}
                        }
                        if(!ended&&s.State.world.clock>=4999)softlocks++;
                    }
                    catch(Exception e){crashes++;lines.Add("ERROR seed "+seed+": "+e);}
                    if(ended){if(s.State.world.outcome==Outcome.TowerClear)clears++;else wipes++;maxSeconds=Math.Max(maxSeconds,s.State.world.clock);}
                    if(seed==1||!ended)File.WriteAllText(Path.GetFullPath("../Artifacts/phase2-seed"+seed+".json"),JsonUtility.ToJson(s.State,true));
                    totalSplits+=s.State.splits;totalRejoins+=s.State.rejoins;
                    lines.Add(seed+": "+s.State.world.outcome+" floor="+s.State.groups.Max(g=>g.floor)+" time="+s.State.world.clock.ToString("F1")+" splits="+s.State.splits+" rejoins="+s.State.rejoins);
                }
            }
            string report="Assertions: "+checks+"\nSeeds: "+seedCount+"; clears="+clears+"; wipes="+wipes+"; crashes="+crashes+"; softlocks="+softlocks+"; impossibleProgression="+impossible+"; infiniteCombat="+infiniteCombat+"; splitDeadlocks="+splitDeadlocks+"; saveCorruption="+corruption+"\nSplits="+totalSplits+"; rejoins="+totalRejoins+"; maxFloor="+maxFloor+"; maxRunSeconds="+maxSeconds.ToString("F1")+"\n";
            File.WriteAllText(Path.GetFullPath("../Artifacts/phase2-tests.txt"),report+string.Join("\n",lines));
            Check(crashes+softlocks+impossible+infiniteCombat+splitDeadlocks+corruption==0,"seed regression safety");Check(clears+wipes==seedCount&&(seedCount<1000||clears>0&&totalSplits>0&&maxFloor==25),"natural full progression and splitting");Check(Loc.MissingKeys.Count==0,"localization coverage");
            Debug.Log("EMBER PHASE 2 VALIDATION PASSED / "+report);
            File.WriteAllText(Path.GetFullPath("../Artifacts/phase2-tests.txt"),report.Replace("Assertions: "+(checks-3),"Assertions: "+checks)+string.Join("\n",lines));
        }
        public static void RunAll(){Validation.Run();RunFull();LocalizationValidation.Run();}
    }
}
