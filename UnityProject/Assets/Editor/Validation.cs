using System;
using System.IO;
using System.Collections.Generic;
using Ember.Core;
using UnityEditor;
using UnityEngine;

namespace Ember.Editor
{
    public static class Validation
    {
        static int checks;
        static void Check(bool condition,string name) {if(!condition)throw new Exception("FAIL: "+name);checks++;Debug.Log("PASS: "+name);}
        static Catalog Catalog => JsonUtility.FromJson<Catalog>(Resources.Load<TextAsset>("catalog").text);
        public static void Run()
        {
            checks=0;var c=Catalog;var s=new Simulation(c);var a=s.State.agents[0];var h=s.State.agents[3];
            Check(s.State.agents.Count==4&&s.State.agents[0].profession==Profession.Warrior&&s.State.agents[3].profession==Profession.Healer,"four-agent showcase");
            Check(a.stats.str+a.stats.dex+a.stats.intel+a.stats.vit+a.stats.wis+a.stats.mana+a.stats.luck==44,"exactly five free points");
            Check(Simulation.Damage(100,5,.5f)<Simulation.Damage(100,0,0),"armor and guard reduce damage");
            Check(Simulation.Proficiency(a,c)==1.2f,"preferred weapon bonus");
            a.weapon=a.Make("bow");Check(Simulation.Proficiency(a,c)==1&& !Simulation.CanCast(a,c,"slash"),"cross-class weapons legal, sword skill blocked");
            a.weapon=a.Make("spear");Check(Simulation.Proficiency(a,c)==1,"neutral weapon has no class bonus");
            a.weapon=a.Make("greatsword",1,"heal");a.x=h.x=0;a.z=h.z=1;h.hp-=80;float mp=a.mp;
            Check(s.Cast(a,"heal",3,true)&&a.mp==mp-c.Skill("heal").mana,"infused heal costs wielder mana");
            Check(!s.Cast(a,"heal",3,true),"shared cooldown rejects second cast");
            Check(h.Bond(a.id).trust>.1f&&a.healing>0,"rescue changes directed relationship");
            a.mp=0;Check(!Simulation.CanCast(a,c,"heal",true),"insufficient mana rejected");a.mp=a.MaxMp;
            Check(!s.Cast(a,"meteor"),"unlearned skill rejected");
            a.skillPoints=10;Check(!s.Unlock(a,"wave")||a.unlocked.Contains("slash"),"skill prerequisite validation");
            foreach(var sk in c.skills) if(sk.profession==a.profession) s.Unlock(a,sk.id);
            Check(a.equipped.Count==4&&a.unlocked.Count>4,"four active slots while tree can grow");
            s.Die(a,"test");Check(!Simulation.CanCast(a,c,"heal",true),"dead agents cannot cast");
            s.Step();Check(s.State.phase==Phase.Battle&&h.alive,"single death does not end run");
            s=new Simulation(c);a=s.State.agents[0];s.EnterRest();a.x=-1;a.z=2;a.materials=2;
            Check(!s.StartCraft(a,"forge0","slash"),"craft requires materials");a.materials=3;
            Check(!s.StartCraft(a,"forge0","heal"),"infusion requires unlocked skill");
            Check(s.StartCraft(a,"forge0","slash")&&a.materials==0,"craft reserves materials");
            for(int i=0;i<42;i++)s.Step();Check(a.weapon.quality>1.2f&&a.weapon.infusion=="slash","timed forge completes high-quality infused weapon");
            int count=a.inventory.Count;for(int i=0;i<40;i++)s.AddItem(a,a.Make("hp"));Check(a.inventory.Count==24,"inventory cap");
            a.inventory.Clear();a.inventory.Add(a.Make("book3"));int material=a.materials;s.ResolveInventory(a);Check(a.materials==material+2,"unusable class book can be salvaged");
            var json=JsonUtility.ToJson(s.State);var copy=SaveStore.Parse(json);Check(JsonUtility.ToJson(copy)==json,"save/load round trip");
            var twin=new Simulation(c);twin.Restore(copy);for(int i=0;i<20;i++){s.Step();twin.Step();}
            Check(JsonUtility.ToJson(s.State)==JsonUtility.ToJson(twin.State),"RNG and continued simulation deterministic after load");
            string path=Path.GetFullPath("../Artifacts/test-save.json");SaveStore.Save(path,s.State);SaveStore.Save(path,twin.State);File.WriteAllText(path,"broken");Check(SaveStore.Load(path).agents.Count==4,"corrupt primary recovers backup");
            bool rejected=false;try{SaveStore.Parse(json.Replace("\"schemaVersion\":1","\"schemaVersion\":99"));}catch{rejected=true;}Check(rejected,"unknown save schema rejected");
            s=new Simulation(c);a=s.State.agents[0];a.Remember("private memory");a.bonds[0].love=.8f;s.Finish(Outcome.Wipe);s.Begin();Check(s.State.agents[0].memory.Count==0&&s.State.agents[0].bonds[0].love==0,"wipe resets private memory and relationships");
            Check(s.State.book.Count==4&&s.State.book.TrueForAll(e=>e.text.Length<=80),"bounded book survives reset");
            s.State.agents[0].Remember("remember companion");s.State.agents[0].bonds[0].love=.8f;s.Die(s.State.agents[1],"final test");s.State.agents[1].Remember("forget me");s.Finish(Outcome.TowerClear);s.Begin();
            Check(s.State.agents[0].memory.Contains("remember companion")&&s.State.agents[0].bonds[0].love==.8f&&s.State.agents[1].memory.Count==0,"full tower clear retains only survivor memories");
            Check(s.State.agents[0].level==0&&s.State.agents[0].weapon.quality==1,"even winners reset growth and equipment");
            s.Finish(Outcome.SliceComplete);s.Begin();Check(s.State.agents.TrueForAll(x=>x.memory.Count==0),"slice finish does not grant full-clear memory");
            s.EnterRest();a=s.State.agents[0];a.x=-9;s.State.phaseClock=Simulation.RestLimit+2;s.Step();Check(!a.alive,"collapse catches and kills stragglers");
            var summaries=new List<string>();int completed=0,wipes=0,crafted=0;
            for(uint seed=1;seed<=100;seed++)
            {
                s=new Simulation(c,seed,false);bool end=false;
                for(int tick=0;tick<2500;tick++) {s.Step();if(s.State.phase==Phase.Ended){end=true;break;}}
                Check(end,"seed "+seed+" ends within 250 simulated seconds");
                if(s.State.outcome==Outcome.SliceComplete)completed++;else wipes++;
                foreach(var ag in s.State.agents)if(ag.weapon.quality>1.2f)crafted++;
                summaries.Add(seed+": "+s.State.outcome+" at "+s.State.clock.ToString("F1")+"s, survivors "+s.State.history[0].survivors.Count);
                int run=s.State.run;for(int tick=0;tick<82;tick++)s.Step();Check(s.State.run==run+1&&s.State.phase!=Phase.Ended,"seed "+seed+" automatically restarts");
            }
            Check(completed>0&&crafted>0,"soak exercises completion and crafting");
            File.WriteAllText(Path.GetFullPath("../Artifacts/core-tests.txt"),"Passed "+checks+" assertions. Slice completions "+completed+"; wipes "+wipes+"; crafted weapons "+crafted+".\n"+string.Join("\n",summaries));
            Debug.Log("EMBER VALIDATION PASSED: "+checks+" assertions");
        }
    }
}
