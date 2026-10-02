using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using UnityEngine;

namespace Ember.Core.Phase2
{
    public enum Goal { Fight, Support, Recover, Forge, Intel, ReadBook, Loot, Exit, Rescue, LeaveParty, Rejoin }
    [Serializable] public class PersonalityProfile
    {
        public int agent; public float patience=.5f, confidence=.5f, caution=.5f, attachment=.5f, strategic=.5f;
        // Original risk/curiosity/greed/empathy/loyalty/aggression remain on Agent.personality.
    }
    [Serializable] public class HighDecision
    {
        public string intent="Fight",targetGoal="",proposedAction="",dialogueIntent="",provider="rule";
        public string[] reasoningTags=Array.Empty<string>(); public float riskLevel,confidence=.8f,utility;
    }
    [Serializable] public class AgentDecisionContext
    {
        public int run,agent,group,floor,revision; public Phase phase;
        public float hp,mp,x,z,remaining,escapeSeconds,taskSeconds,fear,attachment,inventoryValue,bossHealth,intelConfidence;
        public int materials,inventoryCount; public bool readBook,canForge,canRejoin;
        public Personality personality; public PersonalityProfile profile;
        public string[] intel,memorySources,relationshipReasons,allowedSites; public int[] livingMembers;
        public Knowledge[] knowledge;public RelationshipEvent[] relationshipEvidence;
        public RestSite[] restOptions;
        public static bool Validate(HighDecision d,AgentDecisionContext c)
        {
            if(d==null||d.targetGoal==null||d.proposedAction==null||d.dialogueIntent==null||!Enum.TryParse<Goal>(d.intent,out var goal)||!Enum.IsDefined(typeof(Goal),goal)||float.IsNaN(d.confidence)||float.IsInfinity(d.confidence)||d.confidence<0||d.confidence>1||float.IsNaN(d.riskLevel)||d.riskLevel<0||d.riskLevel>1||d.reasoningTags==null||d.reasoningTags.Length>8)return false;
            if((d.targetGoal??"").Length>80||(d.dialogueIntent??"").Length>80||(d.proposedAction??"").Length>80)return false;
            if(d.reasoningTags.Any(s=>s==null||s.Length>32))return false;
            if(c.phase==Phase.Battle&&goal!=Goal.Fight&&goal!=Goal.Support&&goal!=Goal.Rescue)return false;
            if(c.phase==Phase.Rest&&goal==Goal.Fight)return false;
            if(goal==Goal.Forge&&!c.canForge||goal==Goal.Rejoin&&!c.canRejoin||goal==Goal.LeaveParty&&c.livingMembers.Length<2)return false;
            return string.IsNullOrEmpty(d.proposedAction)||(c.allowedSites??Array.Empty<string>()).Contains(d.proposedAction);
        }
    }
    public interface IAgentReasoner { Task<HighDecision> DecideAsync(AgentDecisionContext context,CancellationToken cancellation); }
    public sealed class RuleBasedReasoner : IAgentReasoner
    {
        public Task<HighDecision> DecideAsync(AgentDecisionContext c,CancellationToken cancellation)=>Task.FromResult(Decide(c));
        public HighDecision Decide(AgentDecisionContext c)
        {
            var p=c.personality;var q=c.profile;var options=new List<HighDecision>();
            void Add(Goal g,float score,string tag,string site=""){if(site!=""&&!c.allowedSites.Contains(site))return;if(site!=""&&c.restOptions!=null){var station=c.restOptions.First(s=>s.id==site);float total=Simulation.Distance(c.x,c.z,station.x,station.z)/Simulation.MoveSpeed+station.seconds+(10-station.x)/Simulation.MoveSpeed;if(total+2>c.remaining)score-=90+q.caution*30;score-=station.risk*(12+q.caution*12-p.risk*8);}options.Add(new HighDecision{intent=g.ToString(),targetGoal=g.ToString(),proposedAction=site,dialogueIntent=tag,reasoningTags=new[]{tag},riskLevel=p.risk,confidence=.8f,utility=score});}
            if(c.phase==Phase.Battle)
            {
                Add(Goal.Fight,25+p.aggression*18+q.confidence*8+q.strategic*(4+c.intelConfidence*8)-c.fear*8,"boss_strategy");
                if(c.livingMembers.Length>1){Add(Goal.Support,(1-c.hp)*10+p.empathy*12+c.attachment*8+q.attachment*5,"protect_group");Add(Goal.Rescue,c.fear*14+p.empathy*10+c.attachment*10-(1-c.hp)*18,"rescue_ally");}
            }
            else
            {
                float urgency=Mathf.Clamp01(1-(c.remaining-c.escapeSeconds-3)/12);
                Add(Goal.Exit,12+urgency*(90+q.caution*20-p.risk*12),"collapse_budget");
                Add(Goal.Recover,(1-c.hp)*75+(1-c.mp)*22+q.caution*8-urgency*70,"recover","bed");
                if(c.materials>0)Add(Goal.Support,12+p.empathy*14+q.caution*8-urgency*65,"bless_group","church");
                if(c.canForge)Add(Goal.Forge,24+p.greed*10+q.strategic*15-c.inventoryValue*7-urgency*65,"plan_build","forge");
                Add(Goal.Intel,16+p.curiosity*14+q.strategic*12-(c.intel.Length>0?24:0)-urgency*55,"learn_boss","library");
                if(!c.readBook)Add(Goal.ReadBook,22+p.curiosity*17-urgency*60,"legacy_source","book");
                Add(Goal.Loot,17+p.greed*17+p.curiosity*6-c.inventoryCount*.4f-urgency*(68-p.risk*15),"loot_value",p.greed>.7f?"resource":"cache");
                Add(Goal.Loot,13+p.greed*15-urgency*60,"fallen_adventurer","corpse");
                Add(Goal.Rescue,c.fear*18+p.empathy*10+c.attachment*9-(1-c.hp)*20-urgency*30,"rescue_ally");
                if(c.livingMembers.Length>1)Add(Goal.LeaveParty,4+(1-c.attachment)*15+c.fear*8+p.risk*10+q.confidence*7-p.loyalty*12-q.attachment*10-urgency*25,"independent_strategy");
                if(c.canRejoin)Add(Goal.Rejoin,15+p.loyalty*15+q.attachment*8+c.attachment*8-urgency*20,"meet_companions");
            }
            var best=options.OrderByDescending(x=>x.utility).First();return best;
        }
        public static string SelectSkill(Agent a,Catalog c,PersonalityProfile p)
            =>c.skills.Where(s=>s.profession==a.profession&&!a.unlocked.Contains(s.id)&&a.skillPoints>=s.cost&&(string.IsNullOrEmpty(s.prerequisite)||a.unlocked.Contains(s.prerequisite))).OrderByDescending(s=>s.effect=="Heal"?a.personality.empathy*20:s.effect=="Guard"?p.caution*18:s.power*(.3f+a.personality.aggression)).Select(s=>s.id).FirstOrDefault();
        public static int SelectAttribute(Agent a,PersonalityProfile p)
        {
            float[] scores={a.profession==Profession.Warrior?2:0,a.profession==Profession.Archer?2:0,a.profession==Profession.Mage?2:0,1+p.caution, a.profession==Profession.Healer?2:0,.4f, .1f};
            scores[0]+=a.personality.aggression; scores[3]+=(1-a.hp/a.MaxHp)*3;scores[4]+=a.personality.empathy;return Array.IndexOf(scores,scores.Max());
        }
    }
}
