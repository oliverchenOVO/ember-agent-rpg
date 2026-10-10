using System.Linq;
using Ember.Core;
using Ember.Core.Phase2;
using UnityEngine;
namespace Ember.Presentation
{
    public sealed partial class WitnessGame
    {
        int journalSpeaker=-2;string journalQuery="";GUIStyle journalInput;
        string journalBuildLocale;
        readonly System.Collections.Generic.Dictionary<string,string> journalBuildCache=new System.Collections.Generic.Dictionary<string,string>();
        Message[] JournalMessages()
        {
            string query=journalQuery.Trim();
            return simulation.State.messages.Where(m=>(journalSpeaker==-2||m.speaker==journalSpeaker)&&
                (query.Length==0||RenderForUI(m.text).IndexOf(query,System.StringComparison.OrdinalIgnoreCase)>=0)).Reverse().ToArray();
        }
        float DrawChronicleControls()
        {
            for(int i=0;i<6;i++)
            {
                int speaker=i-2;string name=speaker==-2?Loc.T("journal.all"):speaker==-1?Loc.T("ui.tower"):simulation.State.agents[speaker].name;
                if(Button(344+i*150,190,140,(journalSpeaker==speaker?"• ":"")+name)){journalSpeaker=speaker;codexScroll=Vector2.zero;}
            }
            Text(344,235,95,30,Loc.T("journal.search"),small);
            if(journalInput==null){journalInput=new GUIStyle(GUI.skin.textField){font=uiFont,fontSize=16,padding=new RectOffset(8,8,5,5)};journalInput.normal.textColor=ink;journalInput.focused.textColor=ink;}
            GUI.SetNextControlName("journal-search");string query=GUI.TextField(new Rect(440,232,600,32),journalQuery,80,journalInput);
            if(query!=journalQuery){journalQuery=query;codexScroll=Vector2.zero;}
            if(Button(1054,232,178,Loc.T("journal.clear"))){journalQuery="";GUI.FocusControl(null);codexScroll=Vector2.zero;}
            return 276;
        }
        void DrawChronicle(ref float y)
        {
            var messages=JournalMessages();CodexLine(ref y,Loc.T("journal.help",messages.Length,simulation.State.messages.Count),small);
            if(messages.Length==0){CodexLine(ref y,Loc.T("journal.empty"));return;}
            foreach(var message in messages)
            {
                string speaker=message.speaker>=0&&message.speaker<simulation.State.agents.Count?simulation.State.agents[message.speaker].name:Loc.T("ui.tower");
                CodexLine(ref y,TimeText(message.time)+" / "+speaker,subtitle);
                CodexLine(ref y,RenderForUI(message.text));y+=8;
            }
        }
        void DrawRunHistory(ref float y)
        {
            var history=simulation.State.history;CodexLine(ref y,Loc.T("journal.history_help",history.Count),small);
            if(history.Count==0){CodexLine(ref y,Loc.T("ui.history_empty"));return;}
            foreach(var record in history.AsEnumerable().Reverse())
            {
                CodexLine(ref y,Loc.T("ui.history_row",record.run,Loc.Outcome(record.outcome),TimeText(record.seconds),record.survivors.Count,(int)record.damage,(int)record.healing),subtitle);
                CodexLine(ref y,Loc.T("journal.progress",record.floor,record.deaths),small);
                CodexLine(ref y,Loc.T("journal.composition",string.Join(" / ",record.composition.Select(RenderForUI))),small);
                CodexLine(ref y,Loc.T("journal.survivors",record.survivors.Count==0?Loc.T("p2.none"):string.Join(" / ",record.survivors)),small);
                foreach(string build in record.builds)CodexLine(ref y,Loc.T("journal.build",HistoryBuild(build)),small);
                y+=14;
            }
        }
        string HistoryBuild(string build)
        {
            if(journalBuildLocale!=Loc.Locale){journalBuildCache.Clear();journalBuildLocale=Loc.Locale;}
            build=build??"";if(string.IsNullOrWhiteSpace(build))return Loc.T("p2.none");if(journalBuildCache.TryGetValue(build,out string cached))return cached;
            string result=build.StartsWith("@")?RenderForUI(build):build;
            foreach(var item in simulation.Catalog.items)result=System.Text.RegularExpressions.Regex.Replace(result,"(?<![\\w])"+System.Text.RegularExpressions.Regex.Escape(item.id)+"(?![\\w])",m=>Loc.Item(item.id));
            foreach(var skill in simulation.Catalog.skills)result=System.Text.RegularExpressions.Regex.Replace(result,"(?<![\\w])"+System.Text.RegularExpressions.Regex.Escape(skill.id)+"(?![\\w])",m=>Loc.Skill(skill.id));
            if(journalBuildCache.Count>=512)journalBuildCache.Clear();journalBuildCache[build]=result;return result;
        }
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
