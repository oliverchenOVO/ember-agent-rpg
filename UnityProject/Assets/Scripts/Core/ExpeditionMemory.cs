using System;
using System.Collections.Generic;
using UnityEngine;

namespace Ember.Core.Phase2
{
    public enum MemoryScope { Working, Run, Relationship, LongTerm, Survivor }
    public enum KnowledgeSource { OwnExperience, BookOfDead, ToldByAgent }
    public enum RelationshipEventKind { Healed, Rescued, Abandoned, LootStolen, TacticSuccess, TacticFailure, Conversation, Death, RiskyRescue, Rejoined }
    [Serializable] public class Knowledge
    {
        public string key,text; public MemoryScope scope; public KnowledgeSource source;
        public int run,informant=-1; public float confidence,age,importance,emotionalWeight;
    }
    [Serializable] public class RelationshipEvent
    {
        public int run,actor,subject;public float time,magnitude;public RelationshipEventKind kind;public string evidence;
    }
    [Serializable] public class AgentMemory
    {
        public int agent;public List<Knowledge> entries=new List<Knowledge>();
        public List<RelationshipEvent> salient=new List<RelationshipEvent>();
        public void Add(Knowledge note)
        {
            var same=entries.Find(k=>k.key==note.key&&k.scope==note.scope&&k.source==note.source);
            if(same!=null)entries.Remove(same);entries.Add(note);
            while(entries.Count>48){int index=entries.FindIndex(k=>k.scope==MemoryScope.Working);entries.RemoveAt(index>=0?index:0);}
        }
        public void Tick(float dt){foreach(var e in entries)e.age+=dt;}
        public AgentMemory NextLife(bool completedSurvivor)
        {
            var next=new AgentMemory{agent=agent};if(!completedSurvivor)return next;
            foreach(var e in entries)if(e.scope!=MemoryScope.Working)
            {
                var clone=JsonUtility.FromJson<Knowledge>(JsonUtility.ToJson(e));clone.scope=e.scope==MemoryScope.LongTerm?MemoryScope.LongTerm:MemoryScope.Survivor;next.Add(clone);
            }
            next.salient=new List<RelationshipEvent>(salient);return next;
        }
    }
    public static class RelationshipSystem
    {
        public static void Apply(Agent observer,Agent other,AgentMemory memory,List<RelationshipEvent> log,RelationshipEventKind kind,int run,float time,float magnitude=1)
        {
            if(observer.id==other.id)return;var b=observer.Bond(other.id);if(b==null){b=new Bond{target=other.id};observer.bonds.Add(b);}
            float scale=Mathf.Clamp(magnitude,0,2);
            float value=kind==RelationshipEventKind.Abandoned||kind==RelationshipEventKind.LootStolen||kind==RelationshipEventKind.TacticFailure?-.06f*scale:.04f*scale;
            if(kind==RelationshipEventKind.Death){b.fear=Mathf.Clamp01(b.fear+.2f*scale);b.love=Mathf.Clamp01(b.love+.01f*scale);}
            else if(value<0){b.trust=Mathf.Clamp(b.trust+value,-1,1);b.resentment=Mathf.Clamp01(b.resentment-value);b.rivalry=Mathf.Clamp01(b.rivalry-value*.5f);b.loyalty=Mathf.Clamp(b.loyalty+value*.3f,-1,1);}
            else {b.Help(value);if(kind==RelationshipEventKind.RiskyRescue)b.fear=Mathf.Clamp01(b.fear-.05f);}
            var e=new RelationshipEvent{run=run,time=time,actor=other.id,subject=observer.id,kind=kind,magnitude=scale,evidence=Loc.Token("p2.relationship."+kind,other.name)};
            log.Add(e);if(log.Count>128)log.RemoveAt(0);memory.salient.Add(e);if(memory.salient.Count>12)memory.salient.RemoveAt(0);
            memory.Add(new Knowledge{key="bond:"+other.id+":"+kind,text=e.evidence,scope=MemoryScope.Relationship,source=KnowledgeSource.OwnExperience,run=run,confidence=.9f,importance=.7f,emotionalWeight=scale*.5f});
        }
    }
}
