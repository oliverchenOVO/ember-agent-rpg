using System;
using System.IO;
using System.Linq;
using System.Diagnostics;
using System.Security.Cryptography;
using System.Text;
using Ember.Core;
using Ember.Core.Phase2;
using UnityEngine;
using Debug=UnityEngine.Debug;

namespace Ember.Editor
{
    public static class Phase31Validation
    {
        [Serializable] public class RunConfig { public string label="baseline100",composition="Natural",executablePath="",expectedRuntimeHash="";public StabilizationRules rules=new StabilizationRules();public int firstSeed=1,count=100,floorLimit=25,maxTicks=50000,maxWallSeconds=900; }
        [Serializable] public class Manifest { public string runtimeHash,configHash,simulationVersion,balanceVersion,startedUtc,executableHash,managedAssemblyHash,runner="Unity Editor / pure simulation";public RunConfig config; }
        [Serializable] public class InventorySnapshot {public int agent,initialMaterials,materials,hpPotions,mpPotions,items; public bool alive;}
        [Serializable] public class SeedResult { public uint seed;public string result;public int ticks,floor,living;public float seconds;public FloorTelemetry[] encounters;public InventorySnapshot[] inventory; }
        static readonly string root=DiagnosticRoot();
        static string DiagnosticRoot(){var args=Environment.GetCommandLineArgs();int i=Array.IndexOf(args,"--phase31-root");return Path.GetFullPath(i>=0?args[i+1]:"../Artifacts/Phase31");}
        static string Hash(string text){using(var h=SHA256.Create())return BitConverter.ToString(h.ComputeHash(Encoding.UTF8.GetBytes(text))).Replace("-","").ToLowerInvariant();}
        public static string RuntimeHash()=>Hash(string.Join("\n",Directory.GetFiles("Assets/Scripts/Core","*",SearchOption.AllDirectories).Concat(Directory.GetFiles("Assets/Resources","*.json",SearchOption.AllDirectories)).Where(p=>!p.EndsWith(".meta")).OrderBy(p=>p,StringComparer.Ordinal).Select(p=>p+":"+Hash(File.ReadAllText(p)))));
        static Catalog Catalog()=>JsonUtility.FromJson<Catalog>(Resources.Load<TextAsset>("catalog").text);
        static void Configure(TowerSimulation s,Catalog c,string composition)
        {
            if(composition=="Natural"||composition=="Mixed")return;
            if(!Enum.TryParse<Profession>(composition,out var profession))throw new Exception("Unknown composition");
            foreach(var a in s.State.world.agents){var d=c.Class(profession);a.profession=d.profession;a.stats=JsonUtility.FromJson<Stats>(JsonUtility.ToJson(d.stats));a.weapon=a.Make(d.weapon);a.unlocked.Clear();a.equipped.Clear();a.unlocked.Add(d.starter);a.equipped.Add(d.starter);a.hp=a.MaxHp;a.mp=a.MaxMp;}
        }
        public static void Unit()
        {
            var c=Catalog();var data=TowerContent.Load();int checks=0;
            void Check(bool condition,string message){if(!condition)throw new Exception("PHASE31 FAIL / "+message);checks++;}
            using(var plain=new TowerSimulation(c,data,1729,true))using(var observed=new TowerSimulation(c,data,1729,true){TelemetryEnabled=true,DiagnosticsEnabled=true})
            {
                for(int t=0;t<6000&&plain.State.world.phase!=Phase.Ended;t++){plain.Step();observed.Step();}
                Check(JsonUtility.ToJson(plain.State.world)==JsonUtility.ToJson(observed.State.world),"instrumentation does not change world state or RNG");
                Check(observed.State.telemetry.Any(r=>r.phases.Count>0&&r.phases.Any(p=>p.damageSources.Count>0)),"typed source observations");
                var restored=ExpeditionStore.Parse(JsonUtility.ToJson(observed.State),c,data);Check(JsonUtility.ToJson(restored.telemetry)==JsonUtility.ToJson(observed.State.telemetry),"diagnostics round trip");
            }
            var old=ExpeditionStore.Parse(File.ReadAllText("../Tools/Fixtures/phase2-save.json"),c,data);Check(old.schemaVersion==2,"old schema 2 migration");
            using(var s=new TowerSimulation(c,data,1,true){TelemetryEnabled=true,DiagnosticsEnabled=true}){var a=s.State.world.agents[0];s.Hurt(a,100000,"fixture");var d=s.State.telemetry[0].deathEvents.Single();Check(d.source==DamageSource.Other&&d.hpBefore>0&&d.groupSize==4,"lethal prehit snapshot");Check(s.State.telemetry[0].phases[0].deaths==1,"one death record");}
            var cue=new CombatCue{shape=CueShape.Annulus,radius=8,innerRadius=3};Check(cue.SignedDistance(5,0)<0&&cue.SignedDistance(0,0)>0,"danger distance sign");
            Directory.CreateDirectory(root);File.WriteAllText(Path.Combine(root,"diagnostic-tests.txt"),"Assertions="+checks+"\nUTC="+DateTime.UtcNow.ToString("O"));Debug.Log("PHASE31 UNIT PASSED / "+checks);
        }
        public static void Run()
        {
            Directory.CreateDirectory(root);var config=JsonUtility.FromJson<RunConfig>(File.ReadAllText(Path.Combine(root,"run-config.json")));
            if(config.count<1||config.count>5000||config.firstSeed<1||config.maxTicks<1||config.maxTicks>50000||config.maxWallSeconds<1||config.maxWallSeconds>900||config.floorLimit<1||config.floorLimit>25||config.label.IndexOfAny(Path.GetInvalidFileNameChars())>=0||config.label.Contains(".."))throw new Exception("Unbounded/invalid run config");
            string dir=Path.Combine(root,config.label);Directory.CreateDirectory(dir);string manifestPath=Path.Combine(dir,"manifest.json"),rawPath=Path.Combine(dir,"results.jsonl");
            var manifest=new Manifest{runtimeHash=RuntimeHash(),configHash=Hash(JsonUtility.ToJson(config)),config=config,simulationVersion=TowerSimulation.SimulationVersion,balanceVersion=TowerSimulation.BalanceVersion,startedUtc=DateTime.UtcNow.ToString("O")};
            if(!string.IsNullOrEmpty(config.executablePath))
            {
                if(config.expectedRuntimeHash!=manifest.runtimeHash)throw new Exception("Build binding rejected: runtime differs");
                string exe=Path.GetFullPath(config.executablePath),assembly=Path.Combine(Path.GetDirectoryName(exe),"Ember_Data/Managed/Assembly-CSharp.dll");
                using(var h=SHA256.Create()){manifest.executableHash=BitConverter.ToString(h.ComputeHash(File.ReadAllBytes(exe))).Replace("-","").ToLowerInvariant();manifest.managedAssemblyHash=BitConverter.ToString(h.ComputeHash(File.ReadAllBytes(assembly))).Replace("-","").ToLowerInvariant();}
            }
            if(File.Exists(manifestPath)){var prior=JsonUtility.FromJson<Manifest>(File.ReadAllText(manifestPath));if(prior.runtimeHash!=manifest.runtimeHash||prior.configHash!=manifest.configHash||prior.executableHash!=manifest.executableHash||prior.managedAssemblyHash!=manifest.managedAssemblyHash)throw new Exception("Resume rejected: runtime/config/player differs");manifest=prior;}else File.WriteAllText(manifestPath,JsonUtility.ToJson(manifest,true));
            int done=0,clears=0,wipes=0,nonterminal=0;
            if(File.Exists(rawPath))foreach(var line in File.ReadLines(rawPath)){var r=JsonUtility.FromJson<SeedResult>(line);if(r==null||r.seed!=config.firstSeed+done||r.encounters==null)throw new Exception("Resume rejected: truncated/noncontiguous prefix");done++;if(r.result=="TowerClear"||r.result=="ReachedLimit")clears++;else if(r.result=="Wipe")wipes++;else nonterminal++;}
            if(done>config.count)throw new Exception("Resume prefix exceeds range");var clock=Stopwatch.StartNew();var c=Catalog();var data=TowerContent.Load();
            Debug.Log("PHASE31 START / "+config.label+" / retained="+done+" / hash="+manifest.runtimeHash);
            using(var writer=new StreamWriter(rawPath,true))
            {
                while(done<config.count&&clock.Elapsed.TotalSeconds<config.maxWallSeconds)
                {
                    uint seed=(uint)(config.firstSeed+done);using(var s=new TowerSimulation(c,data,seed,config.composition!="Natural"){TelemetryEnabled=true,DiagnosticsEnabled=true,TelemetrySeed=seed})
                    {
                        s.Rules=config.rules??new StabilizationRules();Configure(s,c,config.composition);var inventory=s.State.world.agents.Select(a=>new InventorySnapshot{agent=a.id,initialMaterials=a.materials}).ToArray();int tick=0;for(;tick<config.maxTicks&&s.State.world.phase!=Phase.Ended&&s.State.groups.Max(g=>g.floor)<=config.floorLimit;tick++)s.Step();
                        string outcome=s.State.groups.Max(g=>g.floor)>config.floorLimit?"ReachedLimit":s.State.world.phase==Phase.Ended?s.State.world.outcome.ToString():"Nonterminal";
                        if(outcome=="TowerClear"||outcome=="ReachedLimit")clears++;else if(outcome=="Wipe")wipes++;else nonterminal++;
                        foreach(var entry in inventory){var a=s.State.world.agents[entry.agent];entry.materials=a.materials;entry.hpPotions=a.inventory.Count(v=>v.id=="hp");entry.mpPotions=a.inventory.Count(v=>v.id=="mp");entry.items=a.inventory.Count;entry.alive=a.alive;}
                        writer.WriteLine(JsonUtility.ToJson(new SeedResult{seed=seed,result=outcome,ticks=tick,floor=s.State.groups.Max(g=>g.floor),living=s.State.world.agents.Count(a=>a.alive),seconds=s.State.world.clock,encounters=s.State.telemetry.ToArray(),inventory=inventory}));writer.Flush();
                    }
                    done++;string progress=$"completed={done}/{config.count}; clears={clears}; wipes={wipes}; nonterminal={nonterminal}; elapsedThisInvocation={clock.Elapsed.TotalSeconds:F2}; UTC={DateTime.UtcNow:O}";
                    File.WriteAllText(Path.Combine(dir,"heartbeat.txt"),progress);File.WriteAllText(Path.Combine(dir,"checkpoint.txt"),done.ToString());if(done%5==0)Debug.Log("PHASE31 PROGRESS / "+progress);
                }
            }
            File.WriteAllText(Path.Combine(dir,"summary.txt"),$"completed={done}; requested={config.count}; clears={clears}; wipes={wipes}; nonterminal={nonterminal}; complete={done==config.count}; finishedUtc={DateTime.UtcNow:O}\n");
            if(nonterminal>0)throw new Exception("Nonterminal seed: retain artifacts and investigate");Debug.Log("PHASE31 BOUNDED RUN ENDED / "+done+" / "+config.count);
        }
        public static void Compositions()
        {
            Unit();Directory.CreateDirectory(root);string path=Path.Combine(root,"run-config.json"),original=File.ReadAllText(path);
            var template=JsonUtility.FromJson<RunConfig>(original);
            try { foreach(var composition in new[]{"Warrior","Archer","Mage","Healer","Mixed"})
                {var config=new RunConfig{label=template.label+"-"+composition,composition=composition,count=50,floorLimit=10,maxWallSeconds=180,rules=template.rules??new StabilizationRules()};File.WriteAllText(path,JsonUtility.ToJson(config,true));Run();}
            } finally {File.WriteAllText(path,original);}
        }
        public static void Snapshots()
        {
            Directory.CreateDirectory(root);var c=Catalog();var data=TowerContent.Load();
            using(var output=new StreamWriter(Path.Combine(root,"attribute-snapshots.csv")))
            {output.WriteLine("rules,composition,floor,agent,level,str,dex,intel,vit,wis,hp,max_hp,mp,max_mp,quality,weapon,equipped");
                foreach(bool candidate in new[]{false,true})foreach(var composition in new[]{"Warrior","Archer","Mage","Healer","Mixed"})
                using(var s=new TowerSimulation(c,data,1,true))
                {
                    s.Rules=new StabilizationRules{phaseWindows=candidate,combatRecovery=candidate,clinicSupplies=candidate,holyRecoveryCost=candidate};Configure(s,c,composition);int previous=0;
                    for(int tick=0;tick<50000&&s.State.world.phase!=Phase.Ended&&s.State.groups.Max(g=>g.floor)<=10;tick++)
                    {
                        int floor=s.State.groups.Max(g=>g.floor);if(floor!=previous){previous=floor;foreach(var a in s.State.world.agents)output.WriteLine(string.Join(",",candidate,composition,floor,a.id,a.level,a.stats.str,a.stats.dex,a.stats.intel,a.stats.vit,a.stats.wis,a.hp,a.MaxHp,a.mp,a.MaxMp,a.weapon.quality,a.weapon.id,string.Join("|",a.equipped)));}s.Step();
                    }
                }
            }
            Debug.Log("PHASE31 ATTRIBUTE SNAPSHOTS COMPLETE");
        }
        public static void ShortGate()
        {
            Unit();Phase3Validation.Unit();LocalizationValidation.Run();Snapshots();
            var c=Catalog();var data=TowerContent.Load();int checks=0;
            foreach(var scenario in new[]{"combat","rest","split","before_boss","after_death","before_10","next_life"})
            using(var original=new TowerSimulation(c,data,1729,true))using(var resumed=new TowerSimulation(c,data,1729,true))
            {
                var g=original.State.groups[0];
                if(scenario=="before_boss"||scenario=="before_10"){g.floor=scenario=="before_10"?10:9;g.boss=BossRuntime.Create(data.Boss(data.Floor(g.floor).bossId));g.boss.visible.timer=0;}
                if(scenario=="rest"||scenario=="split"){g.boss.visible.hp=0;original.EnterRest(g);if(scenario=="split")original.Split(g.id,new[]{2,3});}
                if(scenario=="after_death")original.Die(original.State.world.agents[0],Loc.Token("cause.slam"));
                if(scenario=="next_life")original.NewLife();
                string path=Path.Combine(root,"save-"+scenario+".json");ExpeditionStore.Save(path,original.State);
                resumed.Restore(ExpeditionStore.Load(path,c,data));
                for(int t=0;t<300;t++){original.Step();resumed.Step();}
                if(JsonUtility.ToJson(original.State.world)!=JsonUtility.ToJson(resumed.State.world)||JsonUtility.ToJson(original.State.groups)!=JsonUtility.ToJson(resumed.State.groups))throw new Exception("Save replay differs / "+scenario);
                checks++;Debug.Log("PHASE31 SAVE REPLAY / "+scenario+" / PASSED");
            }
            File.WriteAllText(Path.Combine(root,"save-replay-tests.txt"),"passed="+checks+"\nUTC="+DateTime.UtcNow.ToString("O"));
        }
        public static void Ablations()
        {
            string path=Path.Combine(root,"run-config.json"),original=File.ReadAllText(path);
            try{foreach(var name in new[]{"all","no-windows","no-combat","no-clinic","no-holy-cost"})
            {var config=new RunConfig{label="candidate1-ablation-"+name,count=100,floorLimit=10,maxWallSeconds=180,rules=new StabilizationRules{phaseWindows=name!="no-windows",combatRecovery=name!="no-combat",clinicSupplies=name!="no-clinic",holyRecoveryCost=name!="no-holy-cost"}};File.WriteAllText(path,JsonUtility.ToJson(config,true));Run();}}
            finally{File.WriteAllText(path,original);}
        }
    }
}
