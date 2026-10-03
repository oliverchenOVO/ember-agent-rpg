using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEngine;

namespace Ember.Core.Phase2
{
    [Serializable] public class GroupState
    {
        public int id,floor=1; public List<int> members=new List<int>();public Phase phase;
        public string strategy="balanced",restId="crossing";public float phaseClock,travelLeft;
        public bool completed,terminal;public BossRuntime boss;
        public List<string> claimedLoot=new List<string>();
        public bool restSitesRolled;public List<string> availableRestSites=new List<string>();
    }
    [Serializable] public class AgentPlan
    {
        public int agent,revision;public float nextDecision;public HighDecision decision=new HighDecision();
        public string site="";public float workLeft;public bool siteVisited;public List<string> visited=new List<string>();
    }
    [Serializable] public class ExpeditionState
    {
        public int schemaVersion=2,nextGroup=2,generation=1;public World world;
        public List<GroupState> groups=new List<GroupState>();public List<PersonalityProfile> profiles=new List<PersonalityProfile>();
        public List<AgentPlan> plans=new List<AgentPlan>();public List<AgentMemory> memories=new List<AgentMemory>();
        public List<RelationshipEvent> relationships=new List<RelationshipEvent>();public List<ReasonerRecord> replay=new List<ReasonerRecord>();
        public List<int> completedAgents=new List<int>();public int splits,rejoins,floorsCleared;
        public List<FloorTelemetry> telemetry=new List<FloorTelemetry>();
        public List<int> revivedAgents=new List<int>();
        public GroupState GroupOf(int agent)=>groups.Find(g=>g.members.Contains(agent));
        public AgentMemory Memory(int agent)=>memories.Find(m=>m.agent==agent);
        public AgentPlan Plan(int agent)=>plans.Find(p=>p.agent==agent);
        public PersonalityProfile Profile(int agent)=>profiles.Find(p=>p.agent==agent);
        public IEnumerable<Agent> Members(GroupState g)=>g.members.Select(id=>world.agents[id]);
    }
    public static class ExpeditionStore
    {
        public static void Save(string path,ExpeditionState state)
        {
            Directory.CreateDirectory(Path.GetDirectoryName(Path.GetFullPath(path)));File.WriteAllText(path+".tmp",JsonUtility.ToJson(state,true));
            if(File.Exists(path))File.Replace(path+".tmp",path,path+".bak");else File.Move(path+".tmp",path);
        }
        public static ExpeditionState Load(string path,Catalog catalog,TowerContent data)
        {
            try{return Parse(File.ReadAllText(path),catalog,data);}catch{if(File.Exists(path+".bak"))return Parse(File.ReadAllText(path+".bak"),catalog,data);throw;}
        }
        public static ExpeditionState Parse(string json,Catalog c,TowerContent data)
        {
            var s=JsonUtility.FromJson<ExpeditionState>(json);if(s==null||s.schemaVersion!=2||s.world==null)throw new InvalidDataException("Not an expedition save");
            SaveStore.Parse(JsonUtility.ToJson(s.world));
            if(s.telemetry==null)s.telemetry=new List<FloorTelemetry>();if(s.revivedAgents==null)s.revivedAgents=new List<int>();
            if(s.telemetry.Count>200||s.revivedAgents.Any(id=>id<0||id>3)||s.revivedAgents.Distinct().Count()!=s.revivedAgents.Count)throw new InvalidDataException("Invalid phase 3 state");
            foreach(var row in s.telemetry){if(row.phases==null)row.phases=new List<PhaseDiagnostic>();if(row.deathEvents==null)row.deathEvents=new List<DeathDiagnostic>();if(row.recovery==null)row.recovery=new List<SourceAmount>();if(row.phases.Count>8||row.deathEvents.Count>8||row.recovery.Count>32||row.phases.Any(p=>p.damageSources==null||p.damageSources.Count>128))throw new InvalidDataException("Unbounded diagnostic payload");}
            bool Finite(float v)=>!float.IsNaN(v)&&!float.IsInfinity(v);
            if(s.groups==null||s.groups.Count<1||s.groups.Count>4||s.groups.Select(g=>g.id).Distinct().Count()!=s.groups.Count||s.groups.SelectMany(g=>g.members).OrderBy(x=>x).SequenceEqual(new[]{0,1,2,3})==false)throw new InvalidDataException("Invalid group ownership");
            if(s.profiles==null||s.plans==null||s.memories==null||s.relationships==null||s.replay==null||s.completedAgents==null||s.completedAgents.Distinct().Count()!=s.completedAgents.Count||s.completedAgents.Any(id=>id<0||id>3))throw new InvalidDataException("Invalid cognition state");
            foreach(var g in s.groups)
            {
                if(g.availableRestSites==null)g.availableRestSites=new List<string>();
                if(g.restSitesRolled&&(g.availableRestSites.Distinct().Count()!=g.availableRestSites.Count||g.availableRestSites.Any(id=>data.Rest(g.restId)==null||!data.Rest(g.restId).sites.Any(site=>site.id==id))))throw new InvalidDataException("Invalid refuge facility layout");
                if(data.Floor(g.floor)==null||g.boss==null||g.boss.definition!=data.Floor(g.floor).bossId||g.boss.phase<0||g.boss.phase>=data.Boss(g.boss.definition).phases.Length||!Finite(g.phaseClock)||g.phaseClock<0||!Finite(g.travelLeft)||g.travelLeft<0||!Finite(g.boss.visible.hp)||!Finite(g.boss.visible.maxHp)||g.boss.visible.maxHp<=0||g.boss.visible.hp<0||g.boss.visible.hp>g.boss.visible.maxHp||!Enum.IsDefined(typeof(Phase),g.phase)||data.Rest(g.restId)==null||g.members.Count==0)throw new InvalidDataException("Invalid group progress");
            }
            for(int id=0;id<4;id++)
            {
                if(s.profiles.Count(p=>p.agent==id)!=1||s.plans.Count(p=>p.agent==id)!=1||s.memories.Count(p=>p.agent==id)!=1)throw new InvalidDataException("Missing agent cognition");
                var a=s.world.agents[id];if(c.Item(a.weapon.id)==null||a.inventory.Any(i=>c.Item(i.id)==null)||a.inventory.Count>24||a.equipped.Count>4||a.equipped.Any(k=>c.Skill(k)==null)||!Finite(a.x)||!Finite(a.z)||!Finite(a.taskTimer)||!Finite(s.Plan(id).workLeft)||s.Memory(id).entries.Count>48||s.Memory(id).salient.Count>12)throw new InvalidDataException("Invalid agent payload");
            }
            if(s.relationships.Count>128||s.replay.Count>128||s.world.book.Count>64||s.world.history.Count>100)throw new InvalidDataException("Unbounded save payload");
            return s;
        }
        public static ExpeditionState Migrate(World old,Catalog c,TowerContent data)
        {
            // Import a slice as a new expedition, retaining book/history/personality. No fake cleared floors.
            var sim=new Simulation(c,old.rng,old.showcase);sim.Restore(JsonUtility.FromJson<World>(JsonUtility.ToJson(old)));
            if(old.phase==Phase.Ended){sim.State.run++;sim.Begin();}
            var w=sim.State;w.floor=1;w.phase=Phase.Battle;w.phaseClock=0;w.outcome=Outcome.None;w.restartTimer=0;
            foreach(var a in w.agents){a.escaped=false;a.task="";a.taskTimer=0;}
            var s=new ExpeditionState{world=w};s.groups.Add(new GroupState{id=1,members=new List<int>{0,1,2,3},boss=BossRuntime.Create(data.Boss(data.Floor(1).bossId))});
            foreach(var a in w.agents){s.profiles.Add(new PersonalityProfile{agent=a.id});s.plans.Add(new AgentPlan{agent=a.id});var m=new AgentMemory{agent=a.id};foreach(string text in a.memory)m.Add(new Knowledge{key="legacy:"+m.entries.Count,text=Loc.MigrateLegacy(text),scope=MemoryScope.Run,source=KnowledgeSource.OwnExperience,run=w.run,confidence=.7f,importance=.5f});s.memories.Add(m);}
            return s;
        }
    }
}
