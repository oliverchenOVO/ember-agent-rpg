using System;
using System.Linq;
using UnityEngine;
namespace Ember.Core.Phase2
{
    public sealed partial class TowerSimulation
    {
        public static string AbilityMemoryKey(string boss,string ability)=>"seen:"+boss+":"+ability;
        public bool KnowsAbility(Agent a,string boss,string ability,KnowledgeSource source)=>!string.IsNullOrEmpty(ability)&&State.Memory(a.id).entries.Any(e=>e.key==AbilityMemoryKey(boss,ability)&&e.source==source&&e.confidence>0);
        // Capture a visible telegraph before it can kill its witnesses, and newly started casts after Tick.
        public void ObserveBoss(GroupState g)
        {
            var b=g.boss;if(g.phase!=Phase.Battle||!b.visible.telegraph||Data.Ability(b.ability)==null||!Data.Boss(b.definition).phases.SelectMany(p=>p.abilities).Contains(b.ability))return;
            foreach(var a in State.Members(g).Where(a=>a.alive&&!a.escaped))
            {
                if(KnowsAbility(a,b.definition,b.ability,KnowledgeSource.OwnExperience))continue;
                State.Memory(a.id).Add(new Knowledge{key=AbilityMemoryKey(b.definition,b.ability),text=Loc.Token("codex.observed",Loc.Token(Data.Boss(b.definition).nameKey),Loc.Token(Data.Ability(b.ability).nameKey)),scope=MemoryScope.Run,source=KnowledgeSource.OwnExperience,run=State.world.run,confidence=1,importance=.9f});
            }
        }
        void WriteBook(Agent a,string cause)
        {
            var g=State.GroupOf(a.id);var p=a.personality;string[] tones={"risk","curiosity","empathy","greed","loyalty","aggression"};float[] values={p.risk,p.curiosity,p.empathy,p.greed,p.loyalty,p.aggression};int tone=Array.IndexOf(values,values.Max());
            var entry=new Epitaph{run=State.world.run,author=a.id,floor=g.floor,phase=g.boss.phase,level=a.level,boss=g.boss.definition,cause=cause,tone=tones[tone],profession=a.profession,weapon=a.weapon.id,skills=a.equipped.ToList()};
            string prefix=AbilityMemoryKey(entry.boss,"");entry.observedAbilities=State.Memory(a.id).entries.Where(e=>e.source==KnowledgeSource.OwnExperience&&e.key.StartsWith(prefix)).Select(e=>e.key.Substring(prefix.Length)).Distinct().Where(id=>Data.Ability(id)!=null).ToList();
            string[] moves=entry.observedAbilities.Select(id=>Loc.Token(Data.Ability(id).nameKey)).ToArray();
            string list="";foreach(string move in moves)list=list.Length==0?move:Loc.Token("codex.list",list,move);
            string evidence=moves.Length==0?Loc.Token("codex.no_observation"):Loc.Token("codex.witnessed",list);
            entry.text=Loc.Token("codex.epitaph",Loc.Token("codex.tone."+entry.tone),entry.floor,Loc.Token(Data.Boss(entry.boss).nameKey),cause,evidence);a.lastWords=entry.text;State.world.book.Add(entry);while(State.world.book.Count>64)State.world.book.RemoveAt(0);
        }
        public void ReadLegacy(Agent a,GroupState g)
        {
            a.readBook=true;string nextBoss=Data.Floor(Math.Min(25,g.floor+1)).bossId;
            var rows=State.world.book.OrderByDescending(e=>e.boss==nextBoss).ThenByDescending(e=>e.run).Take(8).ToArray();
            State.Memory(a.id).Add(new Knowledge{key="book:"+g.floor,text=rows.FirstOrDefault()?.text??Loc.Token("event.blank"),scope=MemoryScope.Run,source=KnowledgeSource.BookOfDead,run=State.world.run,confidence=rows.Length>0?.7f:0,importance=.7f});
            var learned=new System.Collections.Generic.HashSet<string>();
            foreach(var entry in rows)
            {
                if(string.IsNullOrEmpty(entry.boss)||Data.Boss(entry.boss)==null)continue;
                foreach(string id in (entry.observedAbilities??new System.Collections.Generic.List<string>()).Take(6))
                {
                    if(Data.Ability(id)==null||!learned.Add(AbilityMemoryKey(entry.boss,id)))continue;
                    State.Memory(a.id).Add(new Knowledge{key=AbilityMemoryKey(entry.boss,id),text=Loc.Token("codex.inherited",State.world.agents[entry.author].name,Loc.Token(Data.Ability(id).nameKey),Loc.Token("codex.advice."+Data.Ability(id).mechanic)),scope=MemoryScope.Run,source=KnowledgeSource.BookOfDead,informant=entry.author,run=State.world.run,confidence=.75f,importance=.95f});
                }
            }
        }
        public void PrepareFromBook(Agent a,GroupState g)
        {
            var abilities=Data.Boss(g.boss.definition).phases.SelectMany(p=>p.abilities).Distinct().Select(Data.Ability).Where(b=>KnowsAbility(a,g.boss.definition,b.id,KnowledgeSource.BookOfDead)).ToArray();if(abilities.Length==0)return;
            bool status=abilities.Any(b=>!string.IsNullOrEmpty(b.status));bool summons=abilities.Any(b=>b.mechanic==Mechanic.Summon);
            string[] counters=a.profession==Profession.Warrior?new[]{"guard","counter"}:a.profession==Profession.Archer?(summons?new[]{"rain","trap"}:new[]{"trap","pierce"}):a.profession==Profession.Mage?(summons?new[]{"lightning","shield"}:new[]{"shield","frost"}):(status?new[]{"cleanse","ward"}:new[]{"ward","groupheal"});
            string skill=counters.FirstOrDefault(id=>a.unlocked.Contains(id));if(skill==null)return;
            if(!a.equipped.Contains(skill))
            {
                if(a.equipped.Count>=4){string removable=a.equipped.LastOrDefault(id=>!string.IsNullOrEmpty(Catalog.Skill(id).prerequisite));if(removable==null)removable=a.equipped.Last();a.equipped.Remove(removable);}
                a.equipped.Add(skill);
            }
            State.world.Say(a.id,Loc.Token("codex.prepared",Loc.Ref("skill",skill),Loc.Token(Data.Ability(abilities[0].id).nameKey)),"decision");
        }
    }
}
