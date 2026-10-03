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
        [Serializable] public class RunConfig { public string label="baseline100",composition="Natural";public int firstSeed=1,count=100,floorLimit=25,maxTicks=50000,maxWallSeconds=900; }
        [Serializable] public class Manifest { public string runtimeHash,configHash,simulationVersion,balanceVersion,startedUtc;public RunConfig config; }
        [Serializable] public class SeedResult { public uint seed;public string result;public int ticks,floor,living;public float seconds;public FloorTelemetry[] encounters; }
        static readonly string root=Path.GetFullPath("../Artifacts/Phase31");
        static string Hash(string text){using(var h=SHA256.Create())return BitConverter.ToString(h.ComputeHash(Encoding.UTF8.GetBytes(text))).Replace("-","").ToLowerInvariant();}
        static string RuntimeHash()=>Hash(string.Join("\n",Directory.GetFiles("Assets/Scripts/Core","*",SearchOption.AllDirectories).Concat(Directory.GetFiles("Assets/Resources","*.json",SearchOption.AllDirectories)).Where(p=>!p.EndsWith(".meta")).OrderBy(p=>p,StringComparer.Ordinal).Select(p=>p+":"+Hash(File.ReadAllText(p)))));
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
            if(File.Exists(manifestPath)){var prior=JsonUtility.FromJson<Manifest>(File.ReadAllText(manifestPath));if(prior.runtimeHash!=manifest.runtimeHash||prior.configHash!=manifest.configHash)throw new Exception("Resume rejected: runtime/config differs");manifest=prior;}else File.WriteAllText(manifestPath,JsonUtility.ToJson(manifest,true));
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
                        Configure(s,c,config.composition);int tick=0;for(;tick<config.maxTicks&&s.State.world.phase!=Phase.Ended&&s.State.groups.Max(g=>g.floor)<=config.floorLimit;tick++)s.Step();
                        string outcome=s.State.groups.Max(g=>g.floor)>config.floorLimit?"ReachedLimit":s.State.world.phase==Phase.Ended?s.State.world.outcome.ToString():"Nonterminal";
                        if(outcome=="TowerClear"||outcome=="ReachedLimit")clears++;else if(outcome=="Wipe")wipes++;else nonterminal++;
                        writer.WriteLine(JsonUtility.ToJson(new SeedResult{seed=seed,result=outcome,ticks=tick,floor=s.State.groups.Max(g=>g.floor),living=s.State.world.agents.Count(a=>a.alive),seconds=s.State.world.clock,encounters=s.State.telemetry.ToArray()}));writer.Flush();
                    }
                    done++;string progress=$"completed={done}/{config.count}; clears={clears}; wipes={wipes}; nonterminal={nonterminal}; elapsedThisInvocation={clock.Elapsed.TotalSeconds:F2}; UTC={DateTime.UtcNow:O}";
                    File.WriteAllText(Path.Combine(dir,"heartbeat.txt"),progress);File.WriteAllText(Path.Combine(dir,"checkpoint.txt"),done.ToString());if(done%5==0)Debug.Log("PHASE31 PROGRESS / "+progress);
                }
            }
            File.WriteAllText(Path.Combine(dir,"summary.txt"),$"completed={done}; requested={config.count}; clears={clears}; wipes={wipes}; nonterminal={nonterminal}; complete={done==config.count}; finishedUtc={DateTime.UtcNow:O}\n");
            if(nonterminal>0)throw new Exception("Nonterminal seed: retain artifacts and investigate");Debug.Log("PHASE31 BOUNDED RUN ENDED / "+done+" / "+config.count);
        }
    }
}
