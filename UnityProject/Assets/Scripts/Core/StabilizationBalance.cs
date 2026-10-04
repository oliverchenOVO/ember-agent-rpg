using System;
using UnityEngine;

namespace Ember.Core.Phase2
{
    [Serializable] public class StabilizationRules
    {
        public bool phaseWindows=true,combatRecovery=true,clinicSupplies=true,holyRecoveryCost=true;
        // Granular switches retain candidate-A behavior when all are enabled.
        // Legacy master switches still reproduce the original baseline.
        public bool recoveryWindow=true,windowManaRecovery=true,reducedManaDrain=true;
        public bool potionBeforeMovement=true,manaReserve=true,defensiveAI=true;
        public bool shieldPriority=true,retryInvalidCasts=true,healTiming=true,roleLoadout=true;
        public bool predictiveHealing=false;
        public int clinicStockLimit=2,clinicKitCost=1;
    }
    public sealed partial class TowerSimulation
    {
        public StabilizationRules Rules=new StabilizationRules();
        void CombatPotions(Agent a,GroupState g)
        {
            string id=a.hp<a.MaxHp*.6f&&a.MaxHp-a.hp>=Catalog.Item("hp").power*.8f?"hp":a.mp<a.MaxMp*.35f&&a.MaxMp-a.mp>=Catalog.Item("mp").power*.7f?"mp":"";
            var item=a.inventory.Find(i=>i.id==id);if(item==null)return;
            float raw=Catalog.Item(id).power,effective=id=="hp"?Mathf.Min(a.MaxHp-a.hp,raw):Mathf.Min(a.MaxMp-a.mp,raw);
            RecordRecovery(a,a,"Potion:"+id,raw,effective);Measure(a,"potion");if(DiagnosticsEnabled){var p=DiagnosticPhase(g);if(p!=null)p.potions++;}
            if(id=="hp")a.hp+=effective;else a.mp+=effective;a.inventory.Remove(item);
        }
        bool RecoverySpellUseful(Agent a,GroupState g,SkillDef s,Agent ally)
        {
            if(!Rules.combatRecovery||!Rules.healTiming||s.effect!="Heal")return true;
            if(Rules.predictiveHealing)return PredictedHealUseful(a,g,s,ally);
            if(s.id=="revive"&&ally!=null&&!ally.alive)return true;
            if(s.id=="cleanse"&&ally!=null&&g.boss.statuses.Exists(e=>e.agent==ally.id))return true;
            if(s.id=="groupheal"){float missing=0;foreach(var id in g.members){var member=State.world.agents[id];if(member.alive&&!member.escaped)missing+=member.MaxHp-member.hp;}return missing>=s.power*.6f;}
            return ally!=null&&ally.MaxHp-ally.hp>=Mathf.Max(6,s.power*.25f);
        }
        bool ReserveMana(Agent a,SkillDef s)=>Rules.combatRecovery&&Rules.manaReserve&&s.effect=="Damage"&&a.mp-s.mana<a.MaxMp*.12f;
        void StabilizeIntent(Agent a,GroupState g)
        {
            if(!Rules.combatRecovery)return;
            if(Rules.predictiveHealing&&a.intent.kind==ActionKind.Skill)
            {
                var healing=Catalog.Skill(a.intent.skill);
                if(healing.effect=="Heal"&&healing.id!="revive")
                {
                    var best=ChooseHealingTarget(a,g,healing);a.intent.target=best?.id??-1;
                    if(!PredictedHealUseful(a,g,healing,best))a.intent=new Intent{kind=ActionKind.Attack,reason=Loc.Token("reason.attack")};
                }
            }
            bool urgentHealing=Rules.predictiveHealing&&a.intent.kind==ActionKind.Skill&&Catalog.Skill(a.intent.skill).effect=="Heal"&&State.world.agents.Exists(v=>v.id==a.intent.target&&v.alive&&v.hp<v.MaxHp*.35f);
            if(Rules.defensiveAI&&g.boss.visible.telegraph&&g.boss.target!=a.id&&CanUseSkill(a,"taunt",false))
            {var target=State.world.agents.Find(v=>v.id==g.boss.target);if(target!=null&&target.hp<target.MaxHp*.6f&&(g.boss.cue.shape==CueShape.Circle||Data.Ability(g.boss.ability).mechanic==Mechanic.Drain)){a.intent=new Intent{kind=ActionKind.Skill,skill="taunt",reason=Loc.Token("p31.reason.intercept")};}}
            if(Rules.defensiveAI&&!urgentHealing&&PressureField.Rate(Data,g.boss,a.x,a.z)>0&&a.hp<a.MaxHp*.8f&&a.guard<.15f&&Effect(g.boss,a.id,"Shield")==null)
            {foreach(var id in a.equipped)if((id=="guard"||id=="shield"||id=="ward")&&CanUseSkill(a,id,false)){a.intent=new Intent{kind=ActionKind.Skill,skill=id,reason=Loc.Token("p31.reason.defense")};break;}}
            if(a.intent.kind!=ActionKind.Skill)return;var skill=Catalog.Skill(a.intent.skill);
            bool burstBlocked=Rules.shieldPriority&&skill.effect=="Damage"&&(g.boss.shield>.25f||g.boss.adds>0)&&skill.mana>Catalog.Skill(Catalog.Class(a.profession).starter).mana*1.5f;
            if(ReserveMana(a,skill)||burstBlocked||(Rules.retryInvalidCasts&&!CanUseSkill(a,skill.id,a.weapon.infusion==skill.id)))
            {a.intent=new Intent{kind=ActionKind.Attack,reason=Loc.Token(burstBlocked?"p31.reason.shield":"p31.reason.mana")};}
        }
        void StockClinic(Agent a,GroupState g,RestSite site)
        {
            int cost=Mathf.Clamp(Rules.clinicKitCost,1,3),limit=Mathf.Clamp(Rules.clinicStockLimit,1,2);
            if(!Rules.clinicSupplies||g.floor>10||site.id!="clinic"||a.materials<cost)return;
            bool hp=a.inventory.FindAll(i=>i.id=="hp").Count<limit,mp=a.inventory.FindAll(i=>i.id=="mp").Count<limit;if(!hp&&!mp)return;
            // A paid finite kit; clinic travel/time and its normal fee still apply.
            a.materials-=cost;RecordMaterialsSpent(a,cost);if(hp){Adapter.AddItem(a,a.Make("hp"));Measure(a,"loot",1,"clinic:hp");}if(mp){Adapter.AddItem(a,a.Make("mp"));Measure(a,"loot",1,"clinic:mp");}
            State.world.Say(a.id,Loc.Token("p31.event.supplies"),"rest");
        }
    }
}
