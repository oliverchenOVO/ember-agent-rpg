using UnityEngine;
namespace Ember.Core.Phase2
{
    public sealed partial class TowerSimulation
    {
        public float LoadoutValue(Agent a,Item item)
        {
            var d=Catalog.Item(item.id);if(d.kind!="Weapon")return float.NegativeInfinity;
            float value=d.power*item.quality+item.upgrade*3+(d.weapon==Catalog.Item(Catalog.Class(a.profession).weapon).weapon?3:0);
            if(!string.IsNullOrEmpty(item.infusion)){var s=Catalog.Skill(item.infusion);value+=s.effect=="Heal"?4+4*a.personality.empathy:3+a.personality.aggression*3;}
            switch(item.affix){case "Scorch":value+=5+a.personality.aggression*3;break;case "Mobility":value+=4+State.Profile(a.id).caution*4;break;case "Recovery":value+=5+(1-a.mp/a.MaxMp)*6;break;case "Break":value+=6+a.personality.loyalty*3;break;case "Astral":value+=d.power*item.quality*.1f+6;break;}
            return value;
        }
        void ResolveLoadout(Agent a)
        {
            Item best=a.weapon;float score=LoadoutValue(a,best);
            foreach(var item in a.inventory){float v=LoadoutValue(a,item);if(v>score){score=v;best=item;}}
            if(best!=a.weapon){a.inventory.Remove(best);var old=a.weapon;a.weapon=best;Adapter.AddItem(a,old);}
        }
    }
}
