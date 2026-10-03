using System;
using Ember.Core;
using Ember.Core.Phase2;
using UnityEngine;
namespace Ember.Presentation
{
    public sealed partial class WitnessGame
    {
        void SetupKnowledgeQA(string scenario,GroupState g)
        {
            g.floor=10;g.boss=BossRuntime.Create(tower.Data.Boss(tower.Data.Floor(10).bossId));
            var agents=tower.State.world.agents;g.boss.ability=tower.Data.Boss(g.boss.definition).phases[0].abilities[0];g.boss.visible.telegraph=true;g.boss.visible.windup=10;tower.ObserveBoss(g);
            if(scenario=="codex_book"||scenario=="codex_book_bottom")
            {
                agents[0].personality.curiosity=1;agents[1].personality.empathy=1;
                tower.Die(agents[0],Loc.Token("p2.cause.ability",Loc.Token(tower.Data.Ability(g.boss.ability).nameKey)));
                tower.Die(agents[1],Loc.Token("p2.cause.status",Loc.Token("p2.status.Burn")));OpenInspection(Inspection.Book);bookAll=true;
            }
            else if(scenario=="codex_legacy"){tower.State.world.book.Add(new Epitaph{run=1,author=0,text=Loc.Token("epitaph.death")});OpenInspection(Inspection.Book);}
            else if(scenario=="codex_empty")OpenInspection(Inspection.Book);
            else if(scenario.StartsWith("codex_boss"))OpenInspection(Inspection.Boss);
            else
            {
                selected=scenario.StartsWith("codex_archer")?1:scenario=="codex_mage"||scenario=="codex_skill_detail"?2:scenario.StartsWith("codex_healer")?3:0;
                OpenInspection(Inspection.Skills);if(scenario=="codex_skill_detail")inspectedSkill="meteor";if(scenario=="codex_healer_detail")inspectedSkill="revive";if(scenario=="codex_archer_detail")inspectedSkill="poison";
            }
            if(scenario.EndsWith("bottom")||scenario=="codex_skill_detail")codexScroll=new Vector2(0,100000);
        }
    }
}
