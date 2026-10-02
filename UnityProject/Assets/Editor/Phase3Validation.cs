using System;
using System.IO;
using System.Linq;
using Ember.Core;
using Ember.Core.Phase2;
using UnityEngine;

namespace Ember.Editor
{
    public static class Phase3Validation
    {
        public static void Baseline(){Seeds("baseline",1000);}
        public static void Tune(){Unit();Seeds("tune",100);}
        public static void Full(){Phase2Validation.RunAll();Unit();Seeds("final",5000);}
        public static void Compositions()
        {
            var c=JsonUtility.FromJson<Catalog>(Resources.Load<TextAsset>("catalog").text);var data=TowerContent.Load();string root=Path.GetFullPath("../Artifacts/Phase3");Directory.CreateDirectory(root);
            using(var writer=new StreamWriter(Path.GetFullPath("../Artifacts/phase3-compositions.csv")))
            {
                writer.WriteLine("composition,seeds,cleared_10F,wipes,nonterminal,mean_sim_seconds,mean_living");
                for(int type=0;type<5;type++)
                {
                    int clears=0,wipes=0,stalls=0;double time=0,alive=0;
                    for(uint seed=1;seed<=50;seed++)using(var s=new TowerSimulation(c,data,seed,true))
                    {
                        if(type<4)foreach(var a in s.State.world.agents){var d=c.Class((Profession)type);a.profession=d.profession;a.stats=JsonUtility.FromJson<Stats>(JsonUtility.ToJson(d.stats));a.weapon=a.Make(d.weapon);a.unlocked.Clear();a.equipped.Clear();a.unlocked.Add(d.starter);a.equipped.Add(d.starter);a.hp=a.MaxHp;a.mp=a.MaxMp;}
                        int tick=0;for(;tick<50000&&s.State.world.phase!=Phase.Ended&&s.State.groups.Max(g=>g.floor)<=10;tick++)s.Step();
                        if(s.State.groups.Max(g=>g.floor)>10)clears++;else if(s.State.world.phase==Phase.Ended)wipes++;else stalls++;
                        time+=s.State.world.clock;alive+=s.State.world.agents.Count(a=>a.alive);
                    }
                    writer.WriteLine((type<4?((Profession)type).ToString():"Mixed")+",50,"+clears+","+wipes+","+stalls+","+(time/50).ToString("F2",System.Globalization.CultureInfo.InvariantCulture)+","+(alive/50).ToString("F2",System.Globalization.CultureInfo.InvariantCulture));
                    if(stalls>0)throw new Exception("Composition stalled");
                }
            }
        }
        static int checks;
        static void Check(bool condition,string message){if(!condition)throw new Exception("PHASE3 FAIL / "+message);checks++;}
        public static void Unit()
        {
            checks=0;var c=JsonUtility.FromJson<Catalog>(Resources.Load<TextAsset>("catalog").text);var data=TowerContent.Load();
            Check(data.floors.Skip(5).Take(5).All(f=>!f.placeholder&&f.theme=="astral_foundry"&&f.difficulty!=null),"bespoke Theme B");
            Check(data.bosses.Skip(5).Take(5).Select(b=>b.archetype).Distinct().Count()==5,"five new encounter identities");Check(data.bosses[9].phases.Length==3,"capstone three phases");
            var oldSave=ExpeditionStore.Parse(File.ReadAllText(Path.GetFullPath("../Tools/Fixtures/phase2-save.json")),c,data);Check(oldSave.groups.All(g=>g.boss.effects!=null&&g.boss.cue!=null)&&oldSave.telemetry!=null&&oldSave.revivedAgents!=null,"Phase 2 save remains compatible");
            Check(data.loot.Where(l=>l.id.StartsWith("foundry_")).All(l=>!string.IsNullOrEmpty(l.affix)&&l.items.Any(id=>c.Item(id).kind=="Weapon")),"usable unique boss loot");
            Check(c.recipes.All(r=>r.quality+.25f>1.15f),"crafted raw quality exceeds average drop");
            var cue=new CombatCue{shape=CueShape.Lane,radius=1,length=18};Check(cue.Contains(8,0)&&!cue.Contains(0,2),"lane geometry");cue.shape=CueShape.Cross;Check(cue.Contains(0,8)&&cue.Contains(8,0)&&!cue.Contains(3,3),"cross geometry");cue.shape=CueShape.Annulus;cue.radius=8;cue.innerRadius=3;Check(!cue.Contains(0,0)&&cue.Contains(5,0)&&!cue.Contains(9,0),"annulus safe pocket");
            foreach(var f in data.floors.Skip(5).Take(5))
            {
                using(var s=new TowerSimulation(c,data,123,true)){var g=s.State.groups[0];g.floor=f.floor;g.boss=BossRuntime.Create(data.Boss(f.bossId));g.boss.visible.timer=0;s.Step();Check(g.boss.visible.telegraph&&g.boss.cue.radius>0,"structured cue "+f.floor);g.boss.visible.hp*=.2f;s.Step();Check(g.boss.phase==1,"transition "+f.floor);if(f.floor==10){s.Step();Check(g.boss.phase==2,"third phase");}}
            }
            foreach(var skill in c.skills)
            {
                using(var s=new TowerSimulation(c,data,1729,true))
                {
                    var g=s.State.groups[0];var a=s.State.world.agents[(int)skill.profession];a.unlocked.Add(skill.id);a.equipped.Clear();a.equipped.Add(skill.id);a.mp=100;a.x=g.boss.x;a.z=g.boss.z;a.hp=a.MaxHp*.5f;
                    if(skill.id=="revive"){s.Die(s.State.world.agents[0],Loc.Token("cause.slam"));a.x=s.State.world.agents[0].x;a.z=s.State.world.agents[0].z;}
                    Check(s.Cast(a,g,skill.id,skill.id=="revive"?0:a.id),"skill effect executes "+skill.id);Check(a.mp==100-skill.mana&&a.cooldowns.Any(cd=>cd.id==skill.id),"cost/cooldown "+skill.id);
                    if(skill.id=="revive"){Check(s.State.world.agents[0].alive&&s.State.revivedAgents.Contains(0),"real one-life revival");s.Die(s.State.world.agents[0],Loc.Token("cause.slam"));a.cooldowns.Clear();a.mp=100;Check(!s.Cast(a,g,"revive",0)&&a.mp==100&&s.State.world.agents[0].hp==0,"second revival rejected without cost");}
                    if(skill.id=="poison")Check(g.boss.effects.Any(e=>e.kind=="Poison"),"poison persists");if(skill.id=="shield")Check(g.boss.effects.Any(e=>e.kind=="Shield"),"barrier persists");
                    var loaded=ExpeditionStore.Parse(JsonUtility.ToJson(s.State),c,data);Check(loaded.groups[0].boss.effects.Count==g.boss.effects.Count,"effects save/load "+skill.id);
                }
            }
            using(var s=new TowerSimulation(c,data,1729,true){TelemetryEnabled=true,TelemetrySeed=1729})
            {
                var a=s.State.world.agents[0];var g=s.State.groups[0];a.weapon.infusion="heal";a.mp=100;a.hp=a.MaxHp*.5f;a.x=g.boss.x;a.z=g.boss.z;
                Check(s.Cast(a,g,"heal",a.id,true)&&a.mp==92&&g.boss.skillPresentation.infused&&g.boss.skillPresentation.origin=="Greatsword","cross-class holy greatsword cast");
                a.weapon.infusion="shot";Check(s.Cast(a,g,"shot",-1,true),"ranged infusion on greatsword");a.weapon=a.Make("bow");Check(!s.Cast(a,g,"heal",a.id,true),"weapon swap removes old infusion");
                s.Hurt(a,10,Loc.Token("cause.slam"));g.boss.visible.hp=0;s.EnterRest(g);Check(s.State.telemetry.Any(r=>r.damageTaken>0&&r.result=="Clear"&&r.skills.Count==2),"telemetry integrity");
            }
            using(var s=new TowerSimulation(c,data,1729,true))
            {
                var g=s.State.groups[0];var a=s.State.world.agents[2];a.unlocked.Add("meteor");a.equipped.Add("meteor");a.mp=100;a.x=g.boss.x;a.z=g.boss.z;
                Check(s.Cast(a,g,"meteor"),"real spatial control zone");foreach(var other in s.State.world.agents)other.escaped=true;
                float hp=g.boss.visible.hp;g.boss.x=8;g.boss.z=7;s.Step();Check(g.boss.visible.hp==hp,"zone does not damage outside its area");g.boss.x=0;g.boss.z=1;s.Step();Check(g.boss.visible.hp<hp,"zone damages inside its area");
                var b=BossRuntime.Create(data.bosses[0]);float tiny=b.Hit(data,a,.2f,"Physical",false);Check(tiny>0&&tiny<1,"continuous damage preserves dt");
            }
            File.WriteAllText(Path.GetFullPath("../Artifacts/phase3-tests.txt"),"Phase3 assertions: "+checks+"\n");Debug.Log("EMBER PHASE3 UNIT PASSED / "+checks);
        }
        public static void Seeds(string label,int count)
        {
            var c=JsonUtility.FromJson<Catalog>(Resources.Load<TextAsset>("catalog").text);var data=TowerContent.Load();
            string root=Path.GetFullPath("../Artifacts/Phase3");Directory.CreateDirectory(root);
            int clears=0,wipes=0,errors=0;long ticks=0;
            using(var writer=new StreamWriter(Path.Combine(root,label+".jsonl")))
            {
                for(uint seed=1;seed<=count;seed++)
                {
                    using(var s=new TowerSimulation(c,data,seed,false){TelemetryEnabled=true,TelemetrySeed=seed})
                    {
                        int tick=0;for(;tick<50000&&s.State.world.phase!=Phase.Ended;tick++)s.Step();ticks+=tick;
                        if(s.State.world.phase!=Phase.Ended){errors++;File.WriteAllText(Path.Combine(root,label+"-nonterminal-"+seed+".json"),JsonUtility.ToJson(s.State,true));}else if(s.State.world.outcome==Outcome.TowerClear)clears++;else wipes++;
                        foreach(var row in s.State.telemetry)writer.WriteLine(JsonUtility.ToJson(row));
                    }
                    if(seed%100==0)Debug.Log("PHASE3 TELEMETRY / "+label+" / "+seed);
                }
            }
            File.WriteAllText(Path.Combine(root,label+"-summary.txt"),$"Seeds={count}; clears={clears}; wipes={wipes}; nonterminal={errors}; ticks={ticks}\n");
            if(errors>0)throw new Exception("Nonterminal telemetry seeds");
        }
    }
}
