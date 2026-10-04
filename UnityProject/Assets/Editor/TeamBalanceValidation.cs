using System;
using System.IO;
using System.Linq;
using System.Diagnostics;
using Ember.Core;
using Ember.Core.Phase2;
using UnityEngine;
using Debug=UnityEngine.Debug;
namespace Ember.Editor
{
    public static class TeamBalanceValidation
    {
        [Serializable] public class Row {public int floor,seed,living,ticks;public string composition;public bool clear;public float seconds,bossRemaining,damage,healing,manaRemaining,damageTaken,overheal;public float[] agentDamage,agentHealing;}
        public static TowerSimulation Fixture(Catalog c,TowerContent data,int seed,int floor,string composition)
        {
            var s=new TowerSimulation(c,data,(uint)seed,true){TelemetryEnabled=true,DiagnosticsEnabled=true};var g=s.State.groups[0];g.floor=floor;g.boss=BossRuntime.Create(data.Boss(data.Floor(floor).bossId));
            string[][] skills={new[]{"slash","wave","guard","taunt"},new[]{"shot","poison","rain","pierce"},new[]{"fireball","frost","lightning","shield"},new[]{"holy","heal","groupheal","ward"}};
            foreach(var a in s.State.world.agents){var role=composition=="Mixed"?(Profession)a.id:composition=="NoHealer"?(Profession)Mathf.Min(a.id,2):(Profession)Enum.Parse(typeof(Profession),composition);var d=c.Class(role);a.profession=role;a.stats=JsonUtility.FromJson<Stats>(JsonUtility.ToJson(d.stats));a.stats.Add(role==Profession.Warrior?0:role==Profession.Archer?1:role==Profession.Mage?2:4,(floor-1)*2);a.stats.vit+=floor-1;a.level=floor-1;a.weapon=a.Make(d.weapon);a.weapon.quality=1.15f;a.inventory.Clear();a.unlocked=c.skills.Where(k=>k.profession==role).Select(k=>k.id).ToList();a.equipped=skills[(int)role].ToList();a.hp=a.MaxHp;a.mp=a.MaxMp;a.damage=a.healing=0;}
            return s;
        }
        static object Call(TowerSimulation s,string method,params object[] args)=>typeof(TowerSimulation).GetMethod(method,System.Reflection.BindingFlags.Instance|System.Reflection.BindingFlags.NonPublic).Invoke(s,args);
        public static void Unit()
        {
            var c=JsonUtility.FromJson<Catalog>(Resources.Load<TextAsset>("catalog").text);var data=TowerContent.Load();int checks=0;
            void Check(bool ok,string reason){if(!ok)throw new Exception("Team balance: "+reason);checks++;}
            using(var s=Fixture(c,data,1729,8,"Mixed")){
                var g=s.State.groups[0];var a=s.State.world.agents[0];a.x=g.boss.x;a.z=g.boss.z;
                Check(s.Cast(a,g,"slash"),"warrior cast");float first=a.damage;a.stats.intel+=30;g.boss=BossRuntime.Create(data.Boss(g.boss.definition));a.cooldowns.Clear();a.mp=a.MaxMp;a.damage=0;
                Check(s.Cast(a,g,"slash")&&Mathf.Abs(a.damage-first)<.001f,"warrior uses intelligence");a.stats.str+=10;g.boss=BossRuntime.Create(data.Boss(g.boss.definition));a.cooldowns.Clear();a.mp=a.MaxMp;a.damage=0;
                Check(s.Cast(a,g,"slash")&&a.damage>first,"warrior strength growth ignored");
                var mage=s.State.world.agents[2];Check(s.LoadoutValue(mage,mage.Make("staff"))>s.LoadoutValue(mage,mage.Make("greatsword")),"mage chooses wrong scaling weapon");
                var healer=s.State.world.agents[3];healer.x=a.x;healer.z=a.z;a.hp=a.MaxHp*.25f;float before=a.hp;s.Step();Check(a.hp>before&&healer.healing>0&&g.boss.pressureCores.Any(n=>n.hp>0),"support did not preempt core attack");
            }
            using(var s=Fixture(c,data,1729,8,"Healer")){
                var g=s.State.groups[0];var a=s.State.world.agents[0];var target=s.State.world.agents[3];foreach(var v in s.State.world.agents){v.x=0;v.z=0;}target.hp=target.MaxHp*.25f;
                float mp=a.mp;Check(!s.Cast(a,g,"groupheal",target.id)&&a.mp==mp,"single target wastes group heal");s.State.world.agents[2].hp=s.State.world.agents[2].MaxHp*.25f;Check(s.Cast(a,g,"groupheal",target.id),"multi-target group heal rejected");
                foreach(var v in s.State.world.agents){v.hp=v.MaxHp;v.cooldowns.Clear();v.mp=v.MaxMp;v.intent=new Intent();}g.boss.effects.Clear();target.hp-=70;a.x=-8;a.z=-7;target.x=5;target.z=0;
                a.intent=new Intent{kind=ActionKind.Skill,skill="heal",target=target.id};var other=s.State.world.agents[1];other.x=target.x;other.z=target.z;mp=other.mp;
                Check(!s.Cast(other,g,"heal",target.id)&&other.mp==mp,"travelling healer reservation ignored");
                target.hp=target.MaxHp*.25f;Check(s.Cast(other,g,"heal",target.id),"urgent rescue blocked by reservation");
                foreach(var v in s.State.world.agents){v.hp=v.MaxHp;v.cooldowns.Clear();v.mp=v.MaxMp;v.intent=new Intent();}g.boss.effects.Clear();a.x=g.boss.x;a.z=g.boss.z;float heal=a.healing;
                Check(s.Cast(a,g,"holy")&&a.healing==heal,"full health holy fabricates recovery");
                foreach(var v in s.State.world.agents)Call(s,"EquipRoleSkills",v,g);Check(s.State.world.agents[0].equipped.Contains("groupheal")&&s.State.world.agents.Skip(1).All(v=>v.equipped.Contains("ward")&&v.equipped.Contains("revive")&&v.equipped.Count==4),"healer roles or slot bound");
                g.boss.visible.telegraph=true;g.boss.cue=new CombatCue{shape=CueShape.Circle,x=0,z=0,radius=3};foreach(var v in s.State.world.agents){v.x=0;v.z=0;}
                a.equipped.Remove("cleanse");a.equipped.Add("ward");Check(s.Cast(a,g,"ward"),"ward cast");other.intent=new Intent{kind=ActionKind.Skill,skill="ward"};Call(s,"CoordinateTeamIntent",other,g);Check(other.intent.kind==ActionKind.Attack,"duplicate ward accepted");
            }
            using(var s=Fixture(c,data,1730,8,"Warrior")){
                var g=s.State.groups[0];foreach(var v in s.State.world.agents)Call(s,"EquipRoleSkills",v,g);Check(s.State.world.agents.Count(v=>v.equipped.Contains("taunt"))==1&&s.State.world.agents.All(v=>v.equipped.Count==4),"all warriors forced to tank");
                var tank=s.State.world.agents.Single(v=>v.equipped.Contains("taunt"));var victim=s.State.world.agents.First(v=>v!=tank);victim.hp=victim.MaxHp*.5f;g.boss.target=victim.id;g.boss.visible.telegraph=true;g.boss.cue=new CombatCue{shape=CueShape.Circle,x=victim.x,z=victim.z,radius=3};tank.intent=new Intent{kind=ActionKind.Skill,skill="taunt"};Call(s,"CoordinateTeamIntent",tank,g);
                Check(tank.intent.kind==ActionKind.Skill&&s.Cast(tank,g,"taunt")&&g.boss.target==tank.id,"tank did not intercept");victim.equipped.Remove("counter");victim.equipped.Add("taunt");victim.intent=new Intent{kind=ActionKind.Skill,skill="taunt"};Call(s,"CoordinateTeamIntent",victim,g);Check(victim.intent.kind==ActionKind.Attack,"taunt ping-pong");
            }
            using(var s=Fixture(c,data,1729,8,"Mixed")){
                var g=s.State.groups[0];var healer=s.State.world.agents[3];healer.equipped=new System.Collections.Generic.List<string>{"holy","heal","revive","ward"};var wounded=s.State.world.agents[0];var dead=s.State.world.agents[1];wounded.hp=wounded.MaxHp*.2f;dead.alive=false;dead.hp=0;float hp=wounded.hp;g.boss.visible.timer=10000;
                s.Step();Check(wounded.hp>hp&&!dead.alive,"revive outranks critically wounded survivor");s.Step();Check(dead.alive&&s.State.revivedAgents.Contains(dead.id),"revive after crisis rejected");
            }
            using(var s=Fixture(c,data,1729,8,"Mixed")){
                var g=s.State.groups[0];var b=g.boss;var healer=s.State.world.agents[3];var target=s.State.world.agents[1];b.ability=data.Boss(b.definition).phases[0].abilities[0];b.visible.telegraph=true;b.visible.windup=.8f;b.target=target.id;b.cue=new CombatCue{shape=CueShape.Circle,x=target.x,z=target.z,radius=3};
                s.Step();Check(b.effects.Count(e=>e.kind=="Shield")==4&&b.skillEvents.Any(e=>e.agent==healer.id&&e.skill=="ward"),"safe healer did not shield threatened party");s.Step();Check(s.State.telemetry[0].skillEconomy.Single(e=>e.skill=="ward").casts==1,"proactive ward duplicates");
            }
            using(var s=Fixture(c,data,1729,8,"Healer")){
                var g=s.State.groups[0];var healer=s.State.world.agents[0];var target=s.State.world.agents[1];healer.equipped.Remove("ward");healer.equipped.Add("revive");healer.x=target.x;healer.z=target.z;target.hp=target.MaxHp*.25f;float hp=target.hp;
                Check(s.Cast(healer,g,"revive",target.id)&&target.hp>hp&&!s.State.revivedAgents.Contains(target.id),"living-target renewal differs from codex");
            }
            using(var s=Fixture(c,data,1729,8,"Mixed"))using(var copy=Fixture(c,data,1729,8,"Mixed")){
                for(int i=0;i<20;i++)s.Step();copy.Restore(ExpeditionStore.Parse(JsonUtility.ToJson(s.State),c,data));for(int i=0;i<100;i++){s.Step();copy.Step();}
                Check(JsonUtility.ToJson(s.State.world)==JsonUtility.ToJson(copy.State.world)&&s.State.groups.Select(g=>JsonUtility.ToJson(g)).SequenceEqual(copy.State.groups.Select(g=>JsonUtility.ToJson(g))),"10-second save replay diverged");
            }
            Debug.Log("TEAM BALANCE UNIT PASSED / checks="+checks);
        }
        static string Root(){var args=Environment.GetCommandLineArgs();int i=Array.IndexOf(args,"--phase31-root");return args[i+1];}
        public static void Baseline()=>Bench("baseline");
        public static void Candidate()=>Bench("candidate");
        public static void Bench(string label)
        {
            string root=Root();Directory.CreateDirectory(root);string path=Path.Combine(root,"team-"+label+".jsonl");if(File.Exists(path))throw new Exception("Preserve existing benchmark");
            File.WriteAllText(Path.Combine(root,"team-"+label+"-manifest.txt"),"RuntimeHash="+Phase31Validation.RuntimeHash()+"\nBalance="+TowerSimulation.BalanceVersion+"\n36 controlled encounters, 180 simulated seconds each, 60 wall seconds total; no progression/soak gate.\n");
            var clock=Stopwatch.StartNew();var c=JsonUtility.FromJson<Catalog>(Resources.Load<TextAsset>("catalog").text);var data=TowerContent.Load();int done=0;
            using(var writer=new StreamWriter(path))foreach(int seed in new[]{1729,1730})foreach(int floor in new[]{6,8,10})foreach(string composition in new[]{"Warrior","Archer","Mage","Healer","Mixed","NoHealer"})
            {
                if(clock.Elapsed.TotalSeconds>60)throw new Exception("Bounded benchmark stopped; preserve completed rows");
                using(var s=Fixture(c,data,seed,floor,composition)){
                    var g=s.State.groups[0];int ticks=0;for(;ticks<1800&&g.phase==Phase.Battle&&!g.terminal;ticks++)s.Step();var a=s.State.world.agents;var t=s.State.telemetry.FirstOrDefault();
                    writer.WriteLine(JsonUtility.ToJson(new Row{seed=seed,floor=floor,composition=composition,ticks=ticks,clear=g.boss.visible.hp<=0,seconds=s.State.world.clock,bossRemaining=g.boss.visible.hp/g.boss.visible.maxHp,living=a.Count(v=>v.alive),damage=a.Sum(v=>v.damage),healing=a.Sum(v=>v.healing),manaRemaining=a.Sum(v=>v.mp/v.MaxMp)/4,damageTaken=t?.phases.Sum(v=>v.damageTaken)??0,overheal=t?.recovery.Sum(v=>v.overheal)??0,agentDamage=a.Select(v=>v.damage).ToArray(),agentHealing=a.Select(v=>v.healing).ToArray()}));writer.Flush();done++;Debug.Log("TEAM BENCH / "+label+" / "+done+" / "+composition+" / F"+floor);
                }
            }
            Debug.Log("TEAM BENCH PASSED / "+label+" / encounters="+done+" / wall="+clock.Elapsed.TotalSeconds);
        }
    }
}
