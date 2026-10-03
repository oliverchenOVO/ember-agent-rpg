using System;
using System.Collections.Generic;
namespace Ember.Core.Phase2
{
    [Serializable] public sealed class BossPresentationEvent
    {
        public int serial,phase,target;public string kind,ability;public float x,z,targetX,targetZ,radius,length,innerRadius,angle;public CueShape shape;
    }
    public sealed partial class TowerSimulation
    {
        void RecordSkillVisual(Agent a,GroupState g,string id,int target,bool infused,string element)
        {
            var b=g.boss;if(b.skillEvents==null)b.skillEvents=new List<SkillPresentation>();
            var e=new SkillPresentation{agent=a.id,target=target,serial=b.skillPresentation.serial+1,skill=id,infused=infused,origin=Catalog.Item(a.weapon.id).weapon,element=element,x=a.x,z=a.z};
            b.skillPresentation=e;b.skillEvents.Add(e);if(b.skillEvents.Count>64)b.skillEvents.RemoveAt(0);
        }
    }
}
