using System.Collections.Generic;
using UnityEngine;

namespace Ember.Core
{
    public interface IBrain { Intent Decide(World world, Agent agent, Catalog catalog); }
    public sealed class UtilityBrain : IBrain
    {
        static void Candidate(List<Intent> list, ActionKind kind, float score, string reason, string skill="", int target=-1)
            => list.Add(new Intent {kind=kind,score=score,reason=reason,skill=skill,target=target});
        public Intent Decide(World w, Agent a, Catalog c)
        {
            var options=new List<Intent>();
            if(w.phase==Phase.Battle)
            {
                Candidate(options,ActionKind.Attack,22+a.personality.aggression*15,Loc.Token("reason.attack"));
                foreach(var id in a.equipped) ScoreSkill(options,w,a,c,id,false);
                if(!string.IsNullOrEmpty(a.weapon.infusion)) ScoreSkill(options,w,a,c,a.weapon.infusion,true);
                foreach(var ally in w.agents)
                {
                    if(!ally.alive||ally.id==a.id) continue;
                    float danger=1-ally.hp/ally.MaxHp;
                    float attachment=a.Bond(ally.id).Attachment;
                    // All terms contribute continuously; affection never forces a rescue.
                    float score=danger*(26+24*a.personality.empathy+20*attachment)+a.personality.loyalty*6-(1-a.hp/a.MaxHp)*(22-10*a.personality.risk);
                    Candidate(options,ActionKind.Protect,score,Loc.Token("reason.protect",ally.name),target:ally.id);
                }
                int dead=w.agents.FindAll(x=>!x.alive).Count;
                float despair=dead*(1-a.personality.risk)*7+(1-a.hp/a.MaxHp)*20;
                Candidate(options,ActionKind.GiveUp,despair-22-a.personality.aggression*10,Loc.Token("reason.give_up"));
            }
            else
            {
                float remaining=Simulation.RestLimit-w.phaseClock;
                float escapeSeconds=(9-a.x)/Simulation.MoveSpeed;
                float urgency=Mathf.Clamp01(1-(remaining-escapeSeconds-3)/12);
                Candidate(options,ActionKind.Exit,urgency*(62-15*a.personality.risk)+12,Loc.Token("reason.exit"));
                Candidate(options,ActionKind.Rest,(1-a.hp/a.MaxHp)*46+(1-a.mp/a.MaxMp)*14-urgency*25,Loc.Token("reason.rest"));
                Candidate(options,ActionKind.Explore,15+a.personality.greed*17+a.personality.curiosity*10-urgency*(35-17*a.personality.risk),Loc.Token("reason.explore"));
                if(!a.readBook) Candidate(options,ActionKind.Read,26+a.personality.curiosity*16-urgency*22,Loc.Token("reason.read"));
                if(a.materials>=3) Candidate(options,ActionKind.Craft,25+a.personality.greed*8+(a.weapon.quality<1.2f?14:0)-urgency*36,Loc.Token("reason.craft"));
            }
            var best=options[0]; foreach(var option in options) if(option.score>best.score) best=option;
            return best;
        }
        void ScoreSkill(List<Intent> options, World w, Agent a, Catalog c, string id, bool weapon)
        {
            if(!Simulation.CanCast(a,c,id,weapon)) return;
            var s=c.Skill(id); float score=0; int target=-1;
            if(s.effect=="Heal")
            {
                foreach(var ally in w.agents)
                {
                    if(!ally.alive) continue;
                    float bond=ally.id==a.id?.25f:a.Bond(ally.id).Attachment;
                    float value=(1-ally.hp/ally.MaxHp)*(55+24*a.personality.empathy+24*bond)-s.mana/a.MaxMp*8;
                    if(value>score) {score=value;target=ally.id;}
                }
            }
            else if(s.effect=="Guard") score=(1-a.hp/a.MaxHp)*30+(w.boss.telegraph?23:4)-a.guard*60;
            else if(s.effect=="Enchant") score=a.enchant<=0?37:0;
            else if(s.effect=="Taunt") score=24+a.personality.loyalty*15;
            else score=26+s.power*.35f+a.personality.aggression*10-s.mana/a.MaxMp*14;
            Candidate(options,ActionKind.Skill,score,s.effect=="Heal"?Loc.Token("reason.heal",Loc.Ref("skill",s.id)):Loc.Token("reason.skill",Loc.Ref("skill",s.id)),id,target);
        }
    }
}
