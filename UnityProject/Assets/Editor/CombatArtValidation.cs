using System;
using System.Linq;
using System.Reflection;
using Ember.Core;
using Ember.Core.Phase2;
using UnityEngine;
namespace Ember.Editor
{
    public static class CombatArtValidation
    {
        static void Require(bool ok,string message){if(!ok)throw new Exception(message);}
        public static void Run()
        {
            var catalog=JsonUtility.FromJson<Catalog>(Resources.Load<TextAsset>("catalog").text);var data=TowerContent.Load();
            using(var s=new TowerSimulation(catalog,data,1729,true))
            {
                var g=s.State.groups[0];var b=g.boss;var members=s.State.Members(g).ToList();
                for(int i=0;i<4;i++)
                {
                    var a=members[i];string id=new[]{"wave","poison","meteor","heal"}[i];a.unlocked.Add(id);a.equipped.Clear();a.equipped.Add(id);a.mp=a.MaxMp;a.hp=a.MaxHp*.2f;a.x=b.x;a.z=b.z;
                    Require(s.Cast(a,g,id,i==3?i:-1),"Concurrent cast failed: "+id);
                }
                Require(b.skillEvents.Count==4&&b.skillEvents.Select(e=>e.serial).Distinct().Count()==4,"Simultaneous cast event lost");
                string saved=JsonUtility.ToJson(s.State);var restored=ExpeditionStore.Parse(saved,catalog,data);
                Require(restored.groups[0].boss.skillEvents.Select(e=>JsonUtility.ToJson(e)).SequenceEqual(b.skillEvents.Select(e=>JsonUtility.ToJson(e))),"Save lost ordered visual events");
                var mage=members[2];mage.unlocked.Add("fireball");mage.equipped.Clear();mage.equipped.Add("fireball");b.visible.hp=b.visible.maxHp=1000000;
                for(int i=0;i<100;i++){mage.mp=mage.MaxMp;mage.cooldowns.Clear();Require(s.Cast(mage,g,"fireball"),"Bounded queue fixture cast failed");}
                Require(b.skillEvents.Count==64&&b.skillEvents.Last().serial==104&&b.skillEvents.First().serial==41,"Queue unbounded or unordered");
                mage.alive=false;int serial=b.skillPresentation.serial;Require(!s.Cast(mage,g,"fireball"),"Dead Agent cast");
                b.effects.Clear();b.effects.Add(new CombatEffect{agent=-1,source=mage.id,kind="Burn",element="Fire",power=3,left=3});float hp=b.visible.hp;
                typeof(TowerSimulation).GetMethod("TickEffects",BindingFlags.Instance|BindingFlags.NonPublic).Invoke(s,new object[]{g,.1f});
                Require(b.visible.hp<hp&&b.skillPresentation.serial==serial,"Damage-over-time fabricated a cast");
                mage.alive=true;mage.hp=mage.MaxHp;b.effects.Clear();b.visible.timer=0;b.Tick(data,members,s.State.world,.01f,s.Hurt);
                Require(b.presentationEvents.Last().kind=="Windup","Actual windup not recorded");var ability=data.Ability(b.ability);
                b.Tick(data,members,s.State.world,ability.windup+.01f,s.Hurt);Require(b.presentationEvents.Last().kind=="Impact","Actual impact not recorded");
                var interruptOwner=data.bosses.First(d=>d.phases[0].abilities.Any(id=>data.Ability(id).interruptible));b=BossRuntime.Create(interruptOwner);g.boss=b;g.floor=data.floors.First(f=>f.bossId==interruptOwner.id).floor;string interrupt=interruptOwner.phases[0].abilities.First(id=>data.Ability(id).interruptible);b.ability=interrupt;b.visible.telegraph=true;b.visible.windup=0;
                b.Hit(data,members[0],1,"Physical",true);int impacts=b.presentationEvents.Count(e=>e.kind=="Impact");
                b.Tick(data,members,s.State.world,.01f,s.Hurt);Require(b.presentationEvents.Last().kind=="Interrupt"&&b.presentationEvents.Count(e=>e.kind=="Impact")==impacts,"Interrupted cast still impacted");
                b.skillEvents=null;b.presentationEvents=null;var legacy=ExpeditionStore.Parse(JsonUtility.ToJson(s.State),catalog,data);Require(legacy.groups[0].boss.skillEvents!=null&&legacy.groups[0].boss.presentationEvents!=null,"Legacy event normalization failed");
            }
            KnowledgeCodexValidation.Run();LocalizationValidation.Run();
            Debug.Log("COMBAT ART VALIDATION PASSED / simultaneous casts, 64-event bound, ordered save/load, dead cast rejection, DOT without phantom projectile, actual windup/impact, cancelled impact, legacy normalization");
        }
    }
}
