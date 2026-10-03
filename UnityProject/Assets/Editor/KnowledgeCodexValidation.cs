using System;
using System.Linq;
using Ember.Core;
using Ember.Core.Phase2;
using UnityEngine;
namespace Ember.Editor
{
    public static class KnowledgeCodexValidation
    {
        [Serializable] sealed class LegacyEpitaph { public int run,author; public string text; }
        static void Require(bool condition,string message){if(!condition)throw new Exception(message);}
        public static void Run()
        {
            var c=JsonUtility.FromJson<Catalog>(Resources.Load<TextAsset>("catalog").text);var data=TowerContent.Load();
            using(var s=new TowerSimulation(c,data,1729,true))
            {
                var g=s.State.groups[0];g.floor=10;g.boss=BossRuntime.Create(data.Boss(data.Floor(10).bossId));var a=s.State.world.agents[0];var other=s.State.world.agents[1];
                a.personality.risk=1;other.personality.empathy=1;var move=data.Boss(g.boss.definition).phases[0].abilities[0];g.boss.ability=move;g.boss.visible.telegraph=true;
                s.State.world.agents[3].escaped=true;s.ObserveBoss(g);
                Require(!s.KnowsAbility(s.State.world.agents[3],g.boss.definition,move,KnowledgeSource.OwnExperience),"Escaped Agent witnessed a move");
                s.Die(a,Loc.Token("p2.cause.ability",Loc.Token(data.Ability(move).nameKey)));s.Die(other,Loc.Token("cause.collapse"));
                var first=s.State.world.book[0];Require(first.observedAbilities.SequenceEqual(new[]{move}),"Epitaph invented unseen moves");
                Require(first.text!=s.State.world.book[1].text&&first.tone=="risk"&&s.State.world.book[1].tone=="empathy","Personalities or causes were lost");
                string before=JsonUtility.ToJson(s.State);var parsed=ExpeditionStore.Parse(before,c,data);Require(parsed.world.book[0].observedAbilities.SequenceEqual(first.observedAbilities),"Save lost move evidence");
                a.alive=true;a.hp=a.MaxHp;a.profession=Profession.Mage;a.unlocked.Clear();a.unlocked.AddRange(new[]{"fireball","frost","meteor","shield","enchant","lightning"});a.equipped.Clear();a.equipped.AddRange(new[]{"fireball","frost","meteor","lightning"});
                var reader=s.State.world.agents[2];s.ReadLegacy(reader,g);Require(s.State.Memory(reader.id).entries.Any(e=>e.source==KnowledgeSource.BookOfDead&&e.informant==first.author),"Reading other authors failed");
                s.ReadLegacy(a,g);Require(s.KnowsAbility(a,g.boss.definition,move,KnowledgeSource.BookOfDead),"Reading did not learn a move");
                Require(!s.KnowsAbility(a,g.boss.definition,"extract",KnowledgeSource.BookOfDead),"Reading learned an unseen move");
                s.PrepareFromBook(a,g);Require(a.equipped.Contains("shield")&&a.equipped.Contains("fireball")&&a.equipped.Count==4,"Legacy did not change loadout within limits");
                Require(s.Context(a,g).bookConfidence>0&&s.State.Memory(a.id).entries.Any(e=>e.informant==first.author&&e.source==KnowledgeSource.BookOfDead),"Book source not supplied to reasoning");
                a.mp=a.MaxMp;a.cooldowns.Clear();Require(s.Cast(a,g,"shield"),"Shield fixture cast failed");Require(g.boss.effects.Any(e=>e.agent==a.id&&e.kind=="Shield"&&e.power==35+a.stats.intel*2),"Displayed shield strength differs from actual cast");
                var memory=s.State.Memory(a.id).NextLife(false);Require(!memory.entries.Any(e=>e.key.StartsWith("seen:")),"Non-survivor inherited memory without reading");
                s.State.world.book.Add(new Epitaph{run=1,author=0,text=Loc.Token("epitaph.death")});var old=ExpeditionStore.Parse(JsonUtility.ToJson(s.State),c,data);Require(old.world.book.Last().floor==0,"Legacy record compatibility failed");
                string withFields=JsonUtility.ToJson(s.State.world.book.Last());string legacyRow=JsonUtility.ToJson(new LegacyEpitaph{run=1,author=0,text=s.State.world.book.Last().text});
                var absent=ExpeditionStore.Parse(JsonUtility.ToJson(s.State).Replace(withFields,legacyRow),c,data);Require(absent.world.book.Last().observedAbilities!=null&&absent.world.book.Last().floor==0,"Actual old JSON missing new fields failed");
                var remembered=ExpeditionStore.Parse(JsonUtility.ToJson(s.State),c,data);Require(remembered.Memory(reader.id).entries.Any(e=>e.key==TowerSimulation.AbilityMemoryKey(first.boss,move)&&e.source==KnowledgeSource.BookOfDead&&e.informant==first.author),"Save/load lost inherited move provenance");
                a.equipped.Clear();a.equipped.AddRange(new[]{"fireball","frost","meteor","lightning"});g.floor=9;g.phase=Phase.Rest;g.boss=BossRuntime.Create(data.Boss(data.Floor(9).bossId));g.boss.visible.hp=0;foreach(var alive in s.State.Members(g).Where(v=>v.alive))alive.escaped=true;
                s.NextFloor(g);Require(g.floor==10&&a.equipped.Contains("shield")&&a.equipped.Count==4&&!a.escaped,"Real next-floor transition did not prepare from legacy");
                foreach(var locale in new[]{"zh-TW","en"}){Loc.SetLocale(locale);foreach(var sk in c.skills)Require(!string.IsNullOrWhiteSpace(CodexText.SkillEffect(a,sk)),"Empty skill effect");foreach(var sk in data.abilities)CodexText.AbilityEffect(sk);Loc.Render(first.text);}
                Require(Loc.MissingKeys.Count==0,"Missing codex strings: "+string.Join(",",Loc.MissingKeys));
            }
            Phase31Validation.ShortGate();
            Debug.Log("KNOWLEDGE CODEX VALIDATION PASSED / personal epitaph, witnessed-only moves, save compatibility, legacy source, preparation, live shield values, finite memory; 24 skill descriptions");
        }
    }
}
