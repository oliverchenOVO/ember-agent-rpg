using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using UnityEngine;

namespace Ember.Core.Phase2
{
    public sealed partial class TowerSimulation : IDisposable
    {
        public ExpeditionState State {get;private set;}public Catalog Catalog {get;}public TowerContent Data {get;}
        readonly IAgentReasoner reasoner;readonly RuleBasedReasoner local=new RuleBasedReasoner();readonly UtilityBrain tactical=new UtilityBrain();
        CancellationTokenSource cancellation=new CancellationTokenSource();
        sealed class Pending {public int generation,run,revision;public AgentDecisionContext context;public Task<HighDecision> task;}
        readonly Dictionary<int,Pending> pending=new Dictionary<int,Pending>();
        readonly World projection=new World();
        public TowerSimulation(Catalog catalog,TowerContent data,uint seed=1729,bool showcase=false,IAgentReasoner reasoner=null)
        {
            Catalog=catalog;Data=data;Data.Validate(catalog);this.reasoner=reasoner??local;
            State=ExpeditionStore.Migrate(new Simulation(catalog,seed,showcase).State,catalog,data);
            foreach(var p in State.profiles){p.patience=.2f+State.world.Roll()*.7f;p.confidence=.2f+State.world.Roll()*.7f;p.caution=.2f+State.world.Roll()*.7f;p.attachment=.2f+State.world.Roll()*.7f;p.strategic=.2f+State.world.Roll()*.7f;}
        }
        public void Restore(ExpeditionState state){CancelPending();State=state;State.generation++;}
        void CancelPending(){cancellation.Cancel();cancellation.Dispose();cancellation=new CancellationTokenSource();pending.Clear();}
        bool disposed;
        public void Dispose(){if(disposed)return;disposed=true;CancelPending();cancellation.Dispose();}
        public void Step(float dt=Simulation.StepSeconds)
        {
            var w=State.world;
            if(w.phase==Phase.Ended){w.restartTimer+=dt;if(w.restartTimer>=8)NewLife();return;}
            w.clock+=dt;foreach(var m in State.memories)m.Tick(dt);
            foreach(var g in State.groups.ToArray())
            {
                if(g.terminal||!State.groups.Contains(g))continue;
                if(g.phase==Phase.Battle)Telemetry(g);
                var members=State.Members(g).ToList();if(members.All(a=>!a.alive)){g.terminal=true;g.phase=Phase.Ended;continue;}
                if(g.travelLeft>0){g.travelLeft=Mathf.Max(0,g.travelLeft-dt);if(g.travelLeft==0)NextFloor(g);continue;}
                g.phaseClock+=dt;
                if(g.phase==Phase.Battle){TickEffects(g,dt);g.boss.stabilizePhaseWindow=Rules.phaseWindows&&Rules.recoveryWindow&&g.floor==10;g.boss.stabilizeManaPressure=Rules.phaseWindows&&Rules.reducedManaDrain&&g.floor==10;g.boss.diagnosticDamage=DiagnosticsEnabled?DiagnosticBossDamage:null;g.boss.diagnosticDrain=DiagnosticsEnabled?DiagnosticDrain:null;SampleDiagnostics(g,dt);g.boss.Tick(Data,members,w,dt,Hurt);}
                foreach(var a in members)
                {
                    if(!a.alive||a.escaped||State.GroupOf(a.id)!=g)continue;
                    a.attackTimer=Mathf.Max(0,a.attackTimer-dt);a.guard=Mathf.Max(0,a.guard-dt*.04f);a.enchant=Mathf.Max(0,a.enchant-dt);
                    a.mp=Mathf.Min(a.MaxMp,a.mp+dt*(.75f+a.stats.wis*.035f+(a.weapon.affix=="Recovery"?.6f:0)+(Rules.phaseWindows&&Rules.windowManaRecovery&&g.phase==Phase.Battle&&g.boss.recoveryWindow>0?1.5f:0)));foreach(var cd in a.cooldowns)cd.left=Mathf.Max(0,cd.left-dt);
                    var plan=State.Plan(a.id);if(plan.nextDecision<=w.clock&&plan.workLeft<=0)Decide(a,g,plan);
                    if(!State.groups.Contains(g)||State.GroupOf(a.id)!=g)continue;
                    if(g.phase==Phase.Battle)ExecuteCombat(a,g,dt);else {RecordRestTime(a,g,dt);ExecuteRest(a,g,dt);}
                }
                if(!State.groups.Contains(g))continue;
                if(g.phase==Phase.Battle&&g.boss.visible.hp<=0){g.boss.ResolveDeath(Data,members,Hurt);if(members.Any(a=>a.alive))EnterRest(g);}
                if(g.phase==Phase.Rest)UpdateDepartures(g);
            }
            if(State.groups.All(g=>g.terminal))Finish(State.completedAgents.Count>0?Outcome.TowerClear:Outcome.Wipe);
            w.floor=State.groups.Max(g=>g.floor);
            if(reasoner is LLMReasoner llm)State.replay=llm.Snapshot();
        }
        public AgentDecisionContext Context(Agent a,GroupState g)
        {
            var members=State.Members(g).Where(v=>v.alive&&!v.escaped).ToList();float attachment=members.Where(v=>v.id!=a.id).Select(v=>a.Bond(v.id)?.Attachment??0).DefaultIfEmpty(.1f).Average();
            var memory=State.Memory(a.id);var r=Data.Rest(g.restId);var knowledge=memory.entries.Where(e=>e.key=="intel:"+g.floor).FirstOrDefault();
            return new AgentDecisionContext{run=State.world.run,agent=a.id,group=g.id,floor=g.floor,revision=State.Plan(a.id).revision,phase=g.phase,hp=a.hp/a.MaxHp,mp=a.mp/a.MaxMp,x=a.x,z=a.z,restOptions=r.AvailableSites(g),remaining=r.collapseAfter-g.phaseClock,escapeSeconds=(10-a.x)/Simulation.MoveSpeed,taskSeconds=State.Plan(a.id).workLeft,materials=a.materials,inventoryCount=a.inventory.Count,inventoryValue=a.weapon.quality,readBook=a.readBook,canForge=a.materials>=3&&!State.Plan(a.id).visited.Contains("forge"),personality=a.personality,profile=State.Profile(a.id),attachment=attachment,fear=a.bonds.Select(b=>b.fear).DefaultIfEmpty(0).Average(),bossHealth=g.boss.visible.hp/g.boss.visible.maxHp,intelConfidence=knowledge!=null?knowledge.confidence/(1+knowledge.age/600):0,bookConfidence=memory.entries.Where(e=>e.source==KnowledgeSource.BookOfDead).Select(e=>e.confidence/(1+e.age/600)).DefaultIfEmpty(0).Average(),knowledge=memory.entries.OrderByDescending(e=>e.importance).Take(8).ToArray(),relationshipEvidence=memory.salient.Take(4).ToArray(),intel=memory.entries.Where(e=>e.key=="intel:"+g.floor).Select(e=>e.key).ToArray(),memorySources=memory.entries.Take(8).Select(e=>e.source.ToString()).ToArray(),relationshipReasons=memory.salient.Take(4).Select(e=>e.kind.ToString()).ToArray(),allowedSites=r.AvailableSites(g).Where(s=>s.id=="bed"?State.Plan(a.id).visited.Count(v=>v==s.id)<2:!State.Plan(a.id).visited.Contains(s.id)).Select(s=>s.id).ToArray(),livingMembers=members.Select(v=>v.id).ToArray(),canRejoin=RejoinCandidate(g)!=null};
        }
        void Decide(Agent a,GroupState g,AgentPlan p)
        {
            using var decisionSample=new Unity.Profiling.ProfilerMarker("Ember.HighDecision").Auto();
            if(pending.TryGetValue(a.id,out var task))
            {
                if(task.task.IsCompleted)
                {
                    if(!task.task.IsFaulted&&!task.task.IsCanceled&&task.generation==State.generation&&task.run==State.world.run&&task.revision==p.revision&&task.context.group==g.id&&task.context.phase==g.phase&&AgentDecisionContext.Validate(task.task.Result,Context(a,g)))
                    {ApplyDecision(a,g,p,task.task.Result);pending.Remove(a.id);p.nextDecision=State.world.clock+1.5f;return;}
                    pending.Remove(a.id);
                }
                else {ApplyDecision(a,g,p,local.Decide(Context(a,g)));p.nextDecision=State.world.clock+.5f;return;}
            }
            p.revision++;var c=Context(a,g);c.revision=p.revision;var fallback=local.Decide(c);ApplyDecision(a,g,p,fallback);
            if(reasoner!=local)
            {
                var job=reasoner.DecideAsync(c,cancellation.Token);
                if(job.IsCompletedSuccessfully&&AgentDecisionContext.Validate(job.Result,c))ApplyDecision(a,g,p,job.Result);
                else pending[a.id]=new Pending{generation=State.generation,run=State.world.run,revision=p.revision,context=c,task=job};
            }
            p.nextDecision=State.world.clock+1.5f;
        }
        void ApplyDecision(Agent a,GroupState g,AgentPlan p,HighDecision d)
        {
            if(p.decision.intent!=d.intent){State.world.Say(a.id,Loc.Token("p2.event.goal",Loc.Token("p2.goal."+d.intent)),"decision");}
            p.decision=d;
            State.Memory(a.id).Add(new Knowledge{key="current_goal",text=Loc.Token("p2.goal."+d.intent),scope=MemoryScope.Working,source=KnowledgeSource.OwnExperience,run=State.world.run,confidence=d.confidence,importance=.2f,emotionalWeight=d.riskLevel});
            if(d.intent=="LeaveParty"&&g.phase==Phase.Rest&&p.workLeft<=0&&!p.visited.Contains("split"))
            {p.visited.Add("split");Split(g.id,new[]{a.id});}
            else if(d.intent=="Rejoin"){var other=RejoinCandidate(g);if(other!=null)Rejoin(g.id,other.id);}
        }
        public GroupState Split(int groupId,IEnumerable<int> members)
        {
            var g=State.groups.Find(x=>x.id==groupId);var ids=members.Distinct().ToList();
            if(g==null||g.terminal||g.phase!=Phase.Rest||g.travelLeft>0||ids.Count==0||ids.Count>=g.members.Count||ids.Any(id=>!g.members.Contains(id)||!State.world.agents[id].alive||State.Plan(id).workLeft>0)||State.groups.Count>=4)return null;
            var newGroup=JsonUtility.FromJson<GroupState>(JsonUtility.ToJson(g));newGroup.id=State.nextGroup++;newGroup.members=ids;newGroup.strategy="independent";newGroup.claimedLoot=new List<string>();
            foreach(var id in ids){g.members.Remove(id);State.Plan(id).nextDecision=State.world.clock+1.5f;foreach(var abandoned in State.Members(g).Where(a=>a.alive))Relate(abandoned,State.world.agents[id],RelationshipEventKind.Abandoned);}
            var row=Telemetry(g);if(row!=null)row.splits++;
            State.groups.Add(newGroup);State.splits++;State.world.Say(-1,Loc.Token("p2.event.split",g.id,newGroup.id),"party");return newGroup;
        }
        GroupState RejoinCandidate(GroupState g)=>State.groups.Find(o=>o!=g&&!o.terminal&&!g.terminal&&o.floor==g.floor&&o.phase==Phase.Rest&&g.phase==Phase.Rest&&o.travelLeft==0&&g.travelLeft==0&&State.Members(o).Concat(State.Members(g)).All(a=>!a.escaped&&State.Plan(a.id).workLeft<=0));
        public bool Rejoin(int first,int second)
        {
            var a=State.groups.Find(g=>g.id==first);var b=State.groups.Find(g=>g.id==second);
            if(a==null||b==null||a==b||a.floor!=b.floor||a.terminal||b.terminal||a.phase!=Phase.Rest||b.phase!=Phase.Rest||a.travelLeft>0||b.travelLeft>0||State.Members(a).Concat(State.Members(b)).Any(v=>v.escaped||State.Plan(v.id).workLeft>0))return false;
            foreach(var id in b.members)foreach(var existing in State.Members(a))if(existing.alive&&State.world.agents[id].alive)Relate(existing,State.world.agents[id],RelationshipEventKind.Rejoined);
            var row=Telemetry(a);if(row!=null)row.rejoins++;
            a.members.AddRange(b.members);a.phaseClock=Mathf.Max(a.phaseClock,b.phaseClock);State.groups.Remove(b);State.rejoins++;State.world.Say(-1,Loc.Token("p2.event.rejoin",a.id),"party");return true;
        }
        void Relate(Agent observer,Agent other,RelationshipEventKind kind,float magnitude=1)=>RelationshipSystem.Apply(observer,other,State.Memory(observer.id),State.relationships,kind,State.world.run,State.world.clock,magnitude);
        void DiagnosticDrain(Agent a,float amount)=>RecordMana(a,amount,true);
        public void Hurt(Agent a,float raw,string cause)=>HurtInternal(a,raw,cause,DamageSource.Other,"",a.hp);
        void HurtInternal(Agent a,float raw,string cause,DamageSource source,string ability,float hpBefore)
        {
            if(!a.alive||a.escaped)return;var g=State.GroupOf(a.id);float amount=raw<1?Mathf.Max(0,raw)*100/(100+Mathf.Max(0,a.armor)*8)*(1-Mathf.Clamp(a.guard,0,.8f)):Simulation.Damage(raw,a.armor,a.guard);
            var barrier=Effect(g.boss,a.id,"Shield");if(barrier!=null){float absorbed=Mathf.Min(barrier.power,amount);barrier.power-=absorbed;amount-=absorbed;if(DiagnosticsEnabled){var u=Telemetry(g)?.team.Find(v=>v.agent==a.id);if(u!=null)u.shieldAbsorbed+=absorbed;}}
            var counter=Effect(g.boss,a.id,"Counter");if(counter!=null&&raw>3){Deal(a,g,counter.power,"Physical",true);g.boss.effects.Remove(counter);}
            amount=Mathf.Min(a.hp,amount);RecordDamage(a,raw,amount,source,ability,hpBefore);Measure(a,"hurt",amount);a.hp=Mathf.Max(0,a.hp-amount);if(a.hp<=0)Die(a,cause);
        }
        public void Die(Agent a,string cause)
        {
            if(!a.alive)return;Measure(a,"death",1,cause);a.alive=false;a.hp=0;a.taskTimer=0;a.task="";State.Plan(a.id).workLeft=0;
            State.world.Say(a.id,Loc.Token("event.death",cause),"death");a.lastWords=Loc.Token("epitaph.death");WriteBook(a);
            State.Memory(a.id).Add(new Knowledge{key="death",text=cause,scope=MemoryScope.Run,source=KnowledgeSource.OwnExperience,run=State.world.run,confidence=1,importance=1,emotionalWeight=1});
            foreach(var other in State.Members(State.GroupOf(a.id)).Where(v=>v.alive))Relate(other,a,RelationshipEventKind.Death);
        }
        void WriteBook(Agent a){State.world.book.Add(new Epitaph{run=State.world.run,author=a.id,text=a.lastWords});while(State.world.book.Count>64)State.world.book.RemoveAt(0);}
        public void Finish(Outcome outcome)
        {
            var w=State.world;if(w.phase==Phase.Ended)return;foreach(var g in State.groups)CloseTelemetry(g,false);foreach(var row in State.telemetry)row.finalResult=outcome.ToString();w.phase=Phase.Ended;w.outcome=outcome;w.restartTimer=0;
            var r=new RunRecord{run=w.run,floor=State.groups.Max(g=>g.floor),seconds=w.clock,outcome=outcome};
            foreach(var a in w.agents)
            {
                r.composition.Add(Loc.Token("ui.agent_class",a.name,Loc.Ref("class",a.profession)));r.damage+=a.damage;r.healing+=a.healing;r.builds.Add(a.weapon.id+":"+a.weapon.infusion);
                if(a.alive&&State.completedAgents.Contains(a.id))r.survivors.Add(a.name);if(!a.alive)r.deaths++;
                if(a.alive){a.lastWords=Loc.Token("epitaph.care");WriteBook(a);}
            }
            w.history.Add(r);while(w.history.Count>100)w.history.RemoveAt(0);w.Say(-1,Loc.Token(outcome==Outcome.TowerClear?"p2.event.clear":"event.wipe"),"run");CancelPending();
        }
        public void NewLife()
        {
            var previous=State;var old=JsonUtility.FromJson<World>(JsonUtility.ToJson(previous.world));old.outcome=previous.world.outcome==Outcome.TowerClear?Outcome.TowerClear:Outcome.Wipe;
            foreach(var a in old.agents)if(!previous.completedAgents.Contains(a.id))a.alive=false;
            var seed=new Simulation(Catalog,old.rng,false);seed.Restore(old);old.run++;seed.Begin();
            State=ExpeditionStore.Migrate(seed.State,Catalog,Data);State.generation=previous.generation+1;State.profiles=previous.profiles;
            foreach(var a in State.world.agents)State.memories[a.id]=previous.Memory(a.id).NextLife(previous.world.outcome==Outcome.TowerClear&&previous.completedAgents.Contains(a.id)&&previous.world.agents[a.id].alive);
            CancelPending();if(reasoner is LLMReasoner llm)llm.ResetBudget();
        }
        public World Observe(int agent)
        {
            var g=State.GroupOf(agent);var w=State.world;
            projection.run=w.run;projection.rng=w.rng;projection.floor=g.floor;projection.clock=w.clock;projection.phase=w.phase==Phase.Ended?Phase.Ended:g.phase;projection.phaseClock=g.phaseClock;projection.outcome=w.outcome;projection.restartTimer=w.restartTimer;projection.agents=w.agents;projection.boss=g.boss.visible;projection.messages=w.messages;projection.book=w.book;projection.history=w.history;projection.showcase=w.showcase;return projection;
        }
    }
}
