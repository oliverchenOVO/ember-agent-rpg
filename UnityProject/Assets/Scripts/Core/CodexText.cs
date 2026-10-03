using System.Globalization;
namespace Ember.Core.Phase2
{
    public static class CodexText
    {
        public static string N(float n)=>n.ToString("0.##",CultureInfo.InvariantCulture);
        public static float Power(Agent a,SkillDef s)=>s.power+a.stats.str*.4f+(s.profession==Profession.Healer?a.stats.wis:a.stats.intel)*.8f;
        public static string SkillEffect(Agent a,SkillDef s,bool holyExtra=true)
        {
            float p=Power(a,s);return Loc.T("codex.skill."+s.id,N(p),N(p*.4f),N(p*.6f),N(p*.65f),N(p*1.2f),N(p*1.15f),N(35+a.stats.intel*2),N(15+a.stats.vit),N(20+a.stats.wis*2),N(5+a.stats.wis*.3f),holyExtra?2:0);
        }
        public static string AbilityEffect(AbilityDefinition a,bool reducedDrain=false)
        {
            string result=Loc.T("codex.mechanic."+a.mechanic);
            if(a.damage>0&&a.mechanic!=Mechanic.Summon&&a.mechanic!=Mechanic.Shield&&a.mechanic!=Mechanic.Survival)result+="\n"+Loc.T("codex.damage",N(a.damage),Loc.T("codex.element."+a.element));
            if(a.resourceDrain>0||a.mechanic==Mechanic.Drain)result+="\n"+Loc.T("codex.drain",N(System.Math.Max(0,a.resourceDrain-(reducedDrain&&a.id=="extract"?6:0))+(a.mechanic==Mechanic.Drain?10:0)));
            if(!string.IsNullOrEmpty(a.status)&&a.mechanic!=Mechanic.Survival&&a.mechanic!=Mechanic.Summon&&a.mechanic!=Mechanic.Shield)result+="\n"+Loc.T("codex.status",Loc.T("p2.status."+a.status),N(a.duration),Loc.T("codex.status_effect."+a.status));
            if(a.push>0)result+="\n"+Loc.T("codex.push",N(a.push));
            if(a.mechanic==Mechanic.Storm||a.mechanic==Mechanic.Survival)result+="\n"+Loc.T("codex.hazard",N(a.duration));
            return result;
        }
    }
}
