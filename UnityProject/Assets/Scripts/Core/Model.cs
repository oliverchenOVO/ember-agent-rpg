using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

namespace Ember.Core
{
    public enum Profession { Warrior, Archer, Mage, Healer }
    public enum Phase { Battle, Rest, Ended }
    public enum Outcome { None, Wipe, SliceComplete, TowerClear }
    public enum ActionKind { Attack, Skill, Protect, Rest, Explore, Craft, Read, Exit, GiveUp }
    [Serializable] public class Stats
    {
        public int str, dex, intel, vit, wis, mana, luck;
        public void Add(int index, int amount) { switch(index) { case 0:str+=amount;break;case 1:dex+=amount;break;case 2:intel+=amount;break;case 3:vit+=amount;break;case 4:wis+=amount;break;case 5:mana+=amount;break;default:luck+=amount;break; } }
    }
    [Serializable] public class Personality { public float risk, greed, curiosity, empathy, loyalty, aggression; }
    [Serializable] public class Bond
    {
        public int target; public float trust=0.1f, respect, friendship, love, fear, resentment, loyalty, rivalry;
        public float Attachment => trust*.22f + friendship*.25f + love*.3f + loyalty*.23f - resentment*.45f;
        public void Help(float v) { trust=Mathf.Clamp(trust+v,-1,1); friendship=Mathf.Clamp(friendship+v*.8f,-1,1); respect=Mathf.Clamp(respect+v*.5f,-1,1); loyalty=Mathf.Clamp(loyalty+v*.5f,-1,1); love=Mathf.Clamp(love+v*.12f,0,1); }
    }
    [Serializable] public class SkillDef { public string id, name, effect, weapon, prerequisite; public Profession profession; public float power, mana, cooldown, range; public int cost=1; }
    [Serializable] public class ClassDef { public Profession profession; public Stats stats; public string starter, weapon; }
    [Serializable] public class ItemDef { public string id, name, kind, weapon, skill; public Profession profession; public float power; }
    [Serializable] public class RecipeDef { public string id, item; public int materials; public float seconds, quality; }
    [Serializable] public class BossDef { public string id, name; public float hp, damage, interval, windup, radius; }
    [Serializable] public class Catalog
    {
        public ClassDef[] classes; public SkillDef[] skills; public ItemDef[] items; public RecipeDef[] recipes; public BossDef boss;
        public SkillDef Skill(string id) => Array.Find(skills,s=>s.id==id);
        public ItemDef Item(string id) => Array.Find(items,i=>i.id==id);
        public ClassDef Class(Profession p) => Array.Find(classes,c=>c.profession==p);
        public RecipeDef Recipe(string id) => Array.Find(recipes,r=>r.id==id);
    }
    [Serializable] public class Item { public int uid; public string id, infusion="",affix=""; public float quality=1; public int upgrade; }
    [Serializable] public class Cooldown { public string id; public float left; }
    [Serializable] public class Intent { public ActionKind kind; public string skill="", reason=""; public int target=-1; public float score; }
    [Serializable] public class Agent
    {
        public int id, level, skillPoints, materials=2, kills, uidCounter, taskRecipient=-1;
        public string name; public Profession profession; public Personality personality; public Stats stats;
        public float hp, mp, x, z, attackTimer, decisionTimer, taskTimer, damage, healing, armor, enchant, guard;
        public bool alive=true, escaped, readBook;
        public string task="", lastWords="";
        public Item weapon; public List<Item> inventory=new List<Item>();
        public List<string> unlocked=new List<string>(), equipped=new List<string>(), memory=new List<string>();
        public List<Cooldown> cooldowns=new List<Cooldown>(); public List<Bond> bonds=new List<Bond>(); public Intent intent=new Intent();
        public float MaxHp => 75 + stats.vit*8 + level*12;
        public float MaxMp => 15 + stats.mana*6 + stats.wis*2;
        public Bond Bond(int target) => bonds.Find(b=>b.target==target);
        public Item Make(string definition, float quality=1, string infusion="") => new Item {uid=++uidCounter,id=definition,quality=quality,infusion=infusion};
        public void Remember(string note) { memory.Add(note); /* Full-clear survivors retain their history. */ }
    }
    [Serializable] public class BossState { public float hp, maxHp, timer=2, windup, targetX, targetZ; public bool enraged, telegraph; public int hits; }
    [Serializable] public class Message { public float time; public int speaker=-1; public string text, category; }
    [Serializable] public class Epitaph
    {
        public int run,author,floor,phase,level;public string text,boss,cause,tone,weapon;public Profession profession;
        public List<string> skills=new List<string>(),observedAbilities=new List<string>();
    }
    [Serializable] public class RunRecord
    {
        public int run, floor, deaths; public float seconds; public Outcome outcome;
        public List<string> composition=new List<string>(), survivors=new List<string>(), builds=new List<string>();
        public float damage, healing;
    }
    [Serializable] public class World
    {
        public int schemaVersion=1, run=1, floor=1; public uint rng=1729; public float clock, phaseClock, restartTimer;
        public bool showcase=true; public Phase phase; public Outcome outcome;
        public List<Agent> agents=new List<Agent>(); public BossState boss=new BossState();
        public List<Message> messages=new List<Message>(); public List<Epitaph> book=new List<Epitaph>();
        public List<RunRecord> history=new List<RunRecord>();
        public float Roll() { uint r=rng; r^=r<<13; r^=r>>17; r^=r<<5; rng=r==0?1729:r; return (rng&0xffffff)/16777216f; }
        public int Pick(int n) => Math.Min(n-1,(int)(Roll()*n));
        public void Say(int speaker,string text,string category="decision") { messages.Add(new Message {time=clock,speaker=speaker,text=text,category=category}); if(messages.Count>100) messages.RemoveAt(0); }
    }
    public static class SaveStore
    {
        public static void Save(string path, World w)
        {
            Directory.CreateDirectory(Path.GetDirectoryName(Path.GetFullPath(path)));
            File.WriteAllText(path+".tmp",JsonUtility.ToJson(w,true));
            if(File.Exists(path)) File.Replace(path+".tmp",path,path+".bak"); else File.Move(path+".tmp",path);
        }
        public static World Load(string path)
        {
            try { return Parse(File.ReadAllText(path)); }
            catch { if(File.Exists(path+".bak")) return Parse(File.ReadAllText(path+".bak")); throw; }
        }
        public static World Parse(string json)
        {
            var w=JsonUtility.FromJson<World>(json);
            if(w==null||w.schemaVersion!=1||w.agents==null||w.agents.Count!=4||w.boss==null||w.rng==0||!Enum.IsDefined(typeof(Phase),w.phase)) throw new InvalidDataException("Invalid save schema/state");
            for(int i=0;i<4;i++) { var a=w.agents[i]; if(a.id!=i||a.stats==null||a.personality==null||a.intent==null||a.weapon==null||a.inventory==null||a.bonds==null||float.IsNaN(a.hp)||float.IsNaN(a.mp)) throw new InvalidDataException("Invalid agent state"); }
            return w;
        }
    }
}
