using System;
using System.Linq;
using UnityEngine;
namespace Ember.Core.Phase2
{
    [Serializable] public sealed class TeamBalanceSettings
    {
        public float[] skillScaling,basicScaling,manaRecoveryBonus;
        public float healerAttackMultiplier,holyRecoveryBase,holyRecoveryWis;
    }
    public static class TeamBalance
    {
        static TeamBalanceSettings settings;
        public static TeamBalanceSettings Settings {get{if(settings==null){settings=JsonUtility.FromJson<TeamBalanceSettings>(Resources.Load<TextAsset>("team-balance").text);if(settings.skillScaling.Length!=4||settings.basicScaling.Length!=4||settings.manaRecoveryBonus.Length!=4||settings.skillScaling.Any(v=>v<=0))throw new InvalidOperationException("Invalid team balance settings");}return settings;}}
        public static float Primary(Agent a,Profession p)=>p==Profession.Warrior?a.stats.str:p==Profession.Archer?a.stats.dex:p==Profession.Mage?a.stats.intel:a.stats.wis;
        public static float Power(Agent a,SkillDef s)=>(s.power+Primary(a,s.profession)*Settings.skillScaling[(int)s.profession])*(s.profession==Profession.Healer&&s.effect=="Damage"?Settings.healerAttackMultiplier:1);
        public static float HolyRecovery(Agent a)=>Settings.holyRecoveryBase+a.stats.wis*Settings.holyRecoveryWis;
        public static float BasicPower(Agent a,Catalog c)=>WeaponPower(a,c,a.weapon);
        public static float WeaponPower(Agent a,Catalog c,Item item)
        {var w=c.Item(item.id);float proficiency=(a.profession==Profession.Warrior&&(w.weapon=="Sword"||w.weapon=="Greatsword"))||(a.profession==Profession.Archer&&w.weapon=="Bow")||((a.profession==Profession.Mage||a.profession==Profession.Healer)&&w.weapon=="Staff")?1.2f:1;float stat=w.weapon=="Bow"?a.stats.dex:w.weapon=="Staff"?(a.profession==Profession.Healer?a.stats.wis:a.stats.intel):a.stats.str;return (w.power*item.quality+item.upgrade*3+stat*.9f+(a.enchant>0?12:0))*proficiency*(item.affix=="Astral"?1.1f:1)*Settings.basicScaling[(int)a.profession];}
    }
    public sealed partial class TowerSimulation
    {
        void EquipRoleSkills(Agent a,GroupState g)
        {
            var peers=State.Members(g).Where(v=>v.alive&&!v.escaped&&v.profession==a.profession).ToList();
            var lead=peers.OrderByDescending(v=>v.stats.vit+State.Profile(v.id).caution*10).ThenBy(v=>v.id).FirstOrDefault();
            string[] preferred=a.profession==Profession.Warrior?(lead==a?new[]{"slash","wave","guard","taunt"}:new[]{"slash","wave","rage","counter"}):a.profession==Profession.Archer?new[]{"shot","poison","pierce","rain"}:a.profession==Profession.Mage?new[]{"fireball","frost",a.personality.aggression>.65f?"meteor":"lightning","shield"}:peers.Count>1&&peers[0]!=a?new[]{"holy","heal","ward","revive"}:State.Members(g).Any(v=>!v.alive)?new[]{"holy","heal","cleanse","revive"}:new[]{"holy","heal","groupheal","cleanse"};
            a.equipped=preferred.Where(id=>a.unlocked.Contains(id)).Concat(a.equipped).Concat(a.unlocked).Distinct().Take(4).ToList();
        }
        // Ready travelling healers reserve their target. Cooldown means the cast
        // already happened, so it must not reserve that target again.
        float CommittedHealing(Agent caster,GroupState g,Agent target)
        {
            float total=0;foreach(var id in g.members){var other=State.world.agents[id];if(other==caster||!other.alive||other.escaped||other.intent.kind!=ActionKind.Skill)continue;var s=Catalog.Skill(other.intent.skill);
                if(s==null||s.effect!="Heal"||s.id=="revive"||!CanUseSkill(other,s.id,other.weapon.infusion==s.id)||(other.intent.target!=target.id&&s.id!="groupheal")||HealTravel(other,target,s)>1.5f)continue;total+=HealPower(other,s)*(s.id=="groupheal"?.65f:1);}
            return total;
        }
        bool TeamSupportIntent(Agent a,GroupState g,out Intent intent)
        {
            intent=null;if(a.profession!=Profession.Healer)return false;
            bool crisis=State.Members(g).Any(v=>v.alive&&!v.escaped&&HealingUrgent(g,v));
            foreach(string id in crisis?new[]{"cleanse","groupheal","heal","revive"}:new[]{"revive","cleanse","groupheal","heal"}){
                if(!CanUseSkill(a,id,false))continue;var skill=Catalog.Skill(id);Agent target=null;
                if(id=="revive")target=State.Members(g).FirstOrDefault(v=>!v.alive&&!State.revivedAgents.Contains(v.id));
                else if(id=="cleanse")target=State.Members(g).FirstOrDefault(v=>v.alive&&!v.escaped&&g.boss.statuses.Any(e=>e.agent==v.id));
                else {target=ChooseHealingTarget(a,g,skill);if(!PredictedHealUseful(a,g,skill,target))target=null;}
                if(target!=null){intent=new Intent{kind=ActionKind.Skill,skill=id,target=target.id,reason=Loc.Token("balance.support",target.name)};return true;}}
            if(CanUseSkill(a,"ward",false)&&g.boss.visible.telegraph&&State.Members(g).Any(v=>v.alive&&!v.escaped&&Effect(g.boss,v.id,"Shield")==null&&g.boss.cue.Contains(v.x,v.z))){intent=new Intent{kind=ActionKind.Skill,skill="ward",reason=Loc.Token("balance.shield_team")};return true;}
            return false;
        }
        void CoordinateTeamIntent(Agent a,GroupState g)
        {
            if(TeamSupportIntent(a,g,out var support)){a.intent=support;return;}
            if(a.intent.kind!=ActionKind.Skill)return;string id=a.intent.skill;var b=g.boss;
            bool useful=true;
            if(id=="ward")useful=State.Members(g).Any(v=>v.alive&&!v.escaped&&Effect(b,v.id,"Shield")==null&&(HealingUrgent(g,v)||b.visible.telegraph&&b.cue.Contains(v.x,v.z)));
            if(id=="shield"||id=="guard"||id=="counter")useful=Effect(b,a.id,"Shield")==null&&(a.hp<a.MaxHp*.65f||b.visible.telegraph&&b.target==a.id);
            if(id=="taunt"){
                var victim=State.world.agents.FirstOrDefault(v=>v.id==b.target);var tank=State.Members(g).Where(v=>v.alive&&!v.escaped&&v.equipped.Contains("taunt")&&CanUseSkill(v,"taunt",false)).OrderByDescending(v=>v.hp+v.armor*8+v.guard*100).ThenBy(v=>v.id).FirstOrDefault();
                useful=b.visible.telegraph&&b.target!=a.id&&victim!=null&&victim.hp<victim.MaxHp*.7f&&victim.guard<.3f&&tank==a&&(b.cue.shape==CueShape.Circle||Data.Ability(b.ability).mechanic==Mechanic.Drain);}
            if(!useful)a.intent=new Intent{kind=ActionKind.Attack,reason=Loc.Token("balance.no_duplicate")};
        }
    }
}
