using System.Linq;
using Ember.Core;
using Ember.Core.Phase2;
using UnityEngine;
namespace Ember.Presentation
{
    public sealed partial class WitnessGame
    {
        void DrawItem(ref float y,Item item)
        {
            var d=simulation.Catalog.Item(item.id);if(d==null)return;
            CodexLine(ref y,string.IsNullOrEmpty(item.affix)?Loc.Item(item.id):Loc.T("p3.affix",Loc.Item(item.id),Loc.T("p3.affix."+item.affix)),subtitle);
            CodexLine(ref y,Loc.T("inspect.item_stats",Loc.T("inspect.kind."+d.kind),item.quality.ToString("F2"),item.upgrade),small);
            if(d.kind=="Weapon")CodexLine(ref y,Loc.T("inspect.weapon",(d.power*item.quality+item.upgrade*3).ToString("F1"),Loc.T("inspect.weapon_type."+d.weapon),Loc.Skill(item.infusion)),small);
            else if(item.id=="hp"||item.id=="mp")CodexLine(ref y,Loc.T("inspect.potion."+item.id,d.power),small);
            else if(d.kind=="SkillBook")CodexLine(ref y,Loc.T("inspect.skill_book",Loc.Skill(d.skill)),small);
            else if(d.kind=="Armor")CodexLine(ref y,Loc.T("inspect.armor",d.power),small);
            else if(d.kind=="Experience")CodexLine(ref y,Loc.T("inspect.experience"),small);
            if(!string.IsNullOrEmpty(item.infusion)){var skill=simulation.Catalog.Skill(item.infusion);if(skill!=null)CodexLine(ref y,CodexText.SkillEffect(simulation.State.agents[selected],skill,tower==null||tower.Rules.holyRecoveryCost),small);}
            y+=8;
        }
        void DrawInventory(ref float y)
        {
            var a=simulation.State.agents[selected];CodexLine(ref y,Loc.T("inspect.bag_header",a.name,a.inventory.Count,24,a.materials),subtitle);
            CodexLine(ref y,Loc.T("inspect.bag_help"),small);CodexLine(ref y,Loc.T("inspect.worn"),subtitle);DrawItem(ref y,a.weapon);
            CodexLine(ref y,Loc.T("inspect.carried"),subtitle);
            if(a.inventory.Count==0)CodexLine(ref y,Loc.T("inspect.bag_empty"));
            foreach(var item in a.inventory)DrawItem(ref y,item);
        }
        void DrawMemories(ref float y)
        {
            var a=simulation.State.agents[selected];CodexLine(ref y,Loc.T("inspect.memory_header",a.name),subtitle);CodexLine(ref y,Loc.T("inspect.memory_help"),small);
            if(tower==null){foreach(string note in a.memory)CodexLine(ref y,RenderForUI(note));if(a.memory.Count==0)CodexLine(ref y,Loc.T("inspect.memory_empty"));return;}
            var memory=tower.State.Memory(selected);
            if(memory.entries.Count==0)CodexLine(ref y,Loc.T("inspect.memory_empty"));
            foreach(MemoryScope scope in System.Enum.GetValues(typeof(MemoryScope)))
            {
                var notes=memory.entries.Where(e=>e.scope==scope).OrderByDescending(e=>e.importance).ThenBy(e=>e.age).ToArray();if(notes.Length==0)continue;
                CodexLine(ref y,Loc.T("inspect.scope."+scope)+" / "+notes.Length,subtitle);
                foreach(var note in notes){CodexLine(ref y,RenderForUI(note.text));string source=Loc.T("inspect.source."+note.source);if(note.source==KnowledgeSource.ToldByAgent&&note.informant>=0&&note.informant<4)source+=" / "+simulation.State.agents[note.informant].name;
                    CodexLine(ref y,Loc.T("inspect.memory_meta",source,note.run,Mathf.RoundToInt(note.confidence*100),TimeText(note.age),Mathf.RoundToInt(note.importance*100)),small);y+=8;}
            }
            if(memory.salient.Count>0){CodexLine(ref y,Loc.T("inspect.salient"),subtitle);foreach(var e in memory.salient.AsEnumerable().Reverse()){CodexLine(ref y,RenderForUI(e.evidence));CodexLine(ref y,Loc.T("inspect.event_meta",e.run,TimeText(e.time)),small);}}
        }
    }
}
