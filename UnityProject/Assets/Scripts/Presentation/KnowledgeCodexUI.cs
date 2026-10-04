using System;
using System.Linq;
using Ember.Core;
using Ember.Core.Phase2;
using UnityEngine;
namespace Ember.Presentation
{
    public sealed partial class WitnessGame
    {
        enum Inspection { None,Book,Boss,Skills,Inventory,Memories }
        bool bookAll;
        Inspection inspection;Vector2 codexScroll;float codexHeight=1000;bool inspectionWasPaused;
        Profession inspectedClass;string inspectedSkill="";
        void OpenInspection(Inspection mode)
        {
            if(inspection==Inspection.None)inspectionWasPaused=paused;inspection=mode;paused=true;codexScroll=Vector2.zero;codexHeight=1000;
            if(mode==Inspection.Book)bookAll=false;
            inspectedClass=simulation.State.agents[selected].profession;inspectedSkill=simulation.Catalog.skills.First(s=>s.profession==inspectedClass).id;
        }
        void CloseInspection(){inspection=Inspection.None;paused=inspectionWasPaused;}
        void CodexLine(ref float y,string text,GUIStyle style=null)
        {
            var s=style??label;float height=Mathf.Max(24,s.CalcHeight(new GUIContent(text),850));
            CodexLabel(new Rect(16,y,850,height),text,s);y+=height+9;
        }
        void CodexLabel(Rect rect,string text,GUIStyle style)
        {
            if(qaEnabled)
            {
                string issue=style.CalcHeight(new GUIContent(text),rect.width)>rect.height+1?qaScenario+" / codex text height / "+text:null;
                if(issue!=null&&!qaIssues.Contains(issue))qaIssues.Add(issue);
                foreach(char ch in text)if(!char.IsControl(ch)&&!uiFont.HasCharacter(ch)){issue=qaScenario+" / codex missing glyph / U+"+((int)ch).ToString("X4");if(!qaIssues.Contains(issue))qaIssues.Add(issue);}
                if(text.StartsWith("@")){issue=qaScenario+" / unrendered codex token";if(!qaIssues.Contains(issue))qaIssues.Add(issue);}
            }
            glyphDrawIndex++;GUI.Label(rect,text,style);
        }
        void DrawInspection()
        {
            if(inspection==Inspection.None)return;
            Box(0,0,1600,900,new Color(0,0,0,.68f));Box(320,75,960,747,new Color(.035f,.065f,.08f,.99f));
            string heading=Loc.T(inspection==Inspection.Book?"ui.book":inspection==Inspection.Boss?"codex.boss":inspection==Inspection.Inventory?"inspect.inventory":inspection==Inspection.Memories?"inspect.memories":"codex.skills");
            Text(344,90,780,43,heading,title);if(Button(1150,94,102,Loc.T("codex.close")))CloseInspection();
            Text(344,137,895,46,Loc.T(inspection==Inspection.Inventory||inspection==Inspection.Memories?"inspect.observer":"codex.observer"),small);
            float top=190;
            if(inspection==Inspection.Book)
            {
                for(int i=0;i<4;i++)if(Button(344+i*180,190,168,(!bookAll&&selected==i?"• ":"")+simulation.State.agents[i].name)){selected=i;bookAll=false;codexScroll=Vector2.zero;}
                if(Button(1064,190,168,(bookAll?"• ":"")+Loc.T("readability.all_authors"))){bookAll=true;codexScroll=Vector2.zero;}
                top=231;
            }
            if(inspection==Inspection.Skills||inspection==Inspection.Inventory||inspection==Inspection.Memories)
            {
                for(int i=0;i<4;i++)if(Button(344+i*225,190,213,(selected==i?"• ":"")+simulation.State.agents[i].name))
                {selected=i;inspectedClass=simulation.State.agents[i].profession;inspectedSkill=simulation.Catalog.skills.First(s=>s.profession==inspectedClass).id;codexScroll=Vector2.zero;}
                if(inspection==Inspection.Skills)for(int i=0;i<4;i++)if(Button(344+i*225,231,213,(inspectedClass==(Profession)i?"• ":"")+Loc.Profession((Profession)i)))
                {inspectedClass=(Profession)i;inspectedSkill=simulation.Catalog.skills.First(s=>s.profession==inspectedClass).id;codexScroll=Vector2.zero;}
                top=inspection==Inspection.Skills?276:231;
            }
            if(towerQA&&(qaScenario.EndsWith("bottom")||qaScenario.EndsWith("detail")))codexScroll.y=codexHeight;
            codexScroll=GUI.BeginScrollView(new Rect(340,top,920,800-top),codexScroll,new Rect(0,0,890,codexHeight));float y=10;
            if(inspection==Inspection.Book)DrawLegacyPages(ref y);else if(inspection==Inspection.Boss)DrawBossDossier(ref y);else if(inspection==Inspection.Skills)DrawSkillTree(ref y);else if(inspection==Inspection.Inventory)DrawInventory(ref y);else if(inspection==Inspection.Memories)DrawMemories(ref y);
            codexHeight=Mathf.Max(y+20,800-top);GUI.EndScrollView();
        }
        void DrawLegacyPages(ref float y)
        {
            CodexLine(ref y,Loc.T("codex.book_help"),small);var w=simulation.State;
            var pages=w.book.Where(e=>bookAll||e.author==selected).Reverse().ToArray();
            if(pages.Length==0){CodexLine(ref y,Loc.T(bookAll?"ui.book_empty":"readability.no_legacy",w.agents[selected].name));return;}
            foreach(var e in pages)
            {
                CodexLine(ref y,Loc.T("ui.book_author",e.run,w.agents[e.author].name),subtitle);
                if(e.floor>0&&tower!=null&&tower.Data.Boss(e.boss)!=null)
                {
                    CodexLine(ref y,Loc.T("codex.book_meta",e.run,e.floor,Loc.Profession(e.profession),e.level),small);
                    CodexLine(ref y,Loc.T(tower.Data.Boss(e.boss).phases[e.phase].nameKey),small);
                    CodexLine(ref y,RenderForUI(e.text));
                    CodexLine(ref y,Loc.T("codex.book_build",Loc.Item(e.weapon),string.Join(" / ",(e.skills??new System.Collections.Generic.List<string>()).Select(Loc.Skill))),small);
                    foreach(string id in e.observedAbilities??new System.Collections.Generic.List<string>())
                    {
                        var ability=tower.Data.Ability(id);if(ability==null)continue;
                        CodexLine(ref y,Loc.T(ability.nameKey),subtitle);
                        CodexLine(ref y,CodexText.AbilityEffect(ability),small);
                        CodexLine(ref y,Loc.T("codex.timing",CodexText.N(ability.windup),CodexText.N(ability.interval),CodexText.N(ability.radius),CodexText.N(ability.length),CodexText.N(ability.innerRadius)),small);
                        CodexLine(ref y,Loc.T("codex.advice."+ability.mechanic));
                    }
                }
                else {CodexLine(ref y,RenderForUI(e.text));CodexLine(ref y,Loc.T("codex.legacy"),small);}
                Box(16,y,850,1,new Color(.25f,.36f,.36f));y+=22;
            }
        }
        void DrawBossDossier(ref float y)
        {
            if(tower==null)return;var g=tower.State.GroupOf(selected);var b=g.boss;var def=tower.Data.Boss(b.definition);
            CodexLine(ref y,Loc.T(def.nameKey),subtitle);
            CodexLine(ref y,Loc.T("codex.boss_stats",CodexText.N(b.visible.hp),CodexText.N(b.visible.maxHp),CodexText.N(def.armor),Loc.T("codex.element."+def.weaknessElement),Loc.T("codex.element."+def.resistElement)));
            CodexLine(ref y,Loc.T("codex.boss_state",Loc.T(def.phases[b.phase].nameKey),Loc.T(b.visible.enraged?"codex.yes":"codex.no"),CodexText.N(b.shield*100),b.adds),small);
            CodexLine(ref y,Loc.T("codex.boss_note"),small);
            if(def.ambientPressure>0)CodexLine(ref y,Loc.T("pressure.rules",CodexText.N(def.ambientPressure)),small);
            if(def.deathMechanic=="FinalPulse")CodexLine(ref y,Loc.T("codex.death_pulse"),small);
            for(int phase=0;phase<def.phases.Length;phase++)
            {
                var p=def.phases[phase];CodexLine(ref y,Loc.T(p.nameKey),subtitle);
                if(phase==0)CodexLine(ref y,Loc.T("codex.phase_start"),small);
                CodexLine(ref y,Loc.T("codex.phase",Loc.T(p.nameKey),CodexText.N(p.hpBelow*100),CodexText.N(p.afterSeconds),CodexText.N(p.enrageAfter),CodexText.N(p.weakness*100),CodexText.N(p.resistance*100)),small);
                if(p.dpsDeadline>0)CodexLine(ref y,Loc.T("codex.dps",CodexText.N(p.dpsDeadline),CodexText.N(p.requiredDamage*100)),small);
                foreach(string id in p.abilities)
                {
                    var move=tower.Data.Ability(id);var agent=tower.State.world.agents[selected];string badge=tower.KnowsAbility(agent,b.definition,id,KnowledgeSource.OwnExperience)?"codex.known":tower.KnowsAbility(agent,b.definition,id,KnowledgeSource.BookOfDead)?"codex.inherited_badge":"codex.unknown";
                    CodexLine(ref y,Loc.T(move.nameKey)+" / "+Loc.T(badge),subtitle);
                    CodexLine(ref y,CodexText.AbilityEffect(move,tower.Rules.phaseWindows&&tower.Rules.reducedManaDrain&&g.floor==10));
                    CodexLine(ref y,Loc.T("codex.timing",CodexText.N(move.windup),CodexText.N(move.interval),CodexText.N(move.radius),CodexText.N(move.length),CodexText.N(move.innerRadius)),small);
                    CodexLine(ref y,Loc.T("codex.target",Loc.T("codex.shape."+move.shape),Loc.T("codex.target."+move.target),Loc.T(move.interruptible?"codex.yes":"codex.no"),CodexText.N(move.interruptWindow)),small);
                    CodexLine(ref y,Loc.T("codex.advice."+move.mechanic),small);y+=12;
                }
            }
        }
        void DrawSkillTree(ref float y)
        {
            var a=simulation.State.agents[selected];var c=simulation.Catalog;
            CodexLine(ref y,Loc.T("codex.skill_header",a.name,Loc.Profession(a.profession),a.level,a.skillPoints,a.equipped.Count),subtitle);
            CodexLine(ref y,Loc.T("codex.agent_stats",a.stats.str,a.stats.dex,a.stats.intel,a.stats.vit,a.stats.wis,a.stats.mana,a.stats.luck),small);
            CodexLine(ref y,Loc.T("codex.skill_help"),small);
            float baseY=y;var roots=c.skills.Where(s=>s.profession==inspectedClass&&string.IsNullOrEmpty(s.prerequisite)).ToArray();int row=0;
            foreach(var root in roots)
            {
                var node=root;int column=0;
                while(node!=null&&column<3)
                {
                    float x=16+column*285,ny=baseY+row*154;Box(x,ny,266,137,new Color(.10f,.17f,.19f));
                    string state=a.equipped.Contains(node.id)?"codex.equipped":a.unlocked.Contains(node.id)?"codex.unlocked":"codex.locked";
                    if(node.id==inspectedSkill)Box(x,ny,3,137,amber);
                    CodexLabel(new Rect(x+10,ny+8,246,24),Loc.Skill(node.id),subtitle);
                    CodexLabel(new Rect(x+10,ny+35,246,24),Loc.T(state)+(a.weapon.infusion==node.id?" / "+Loc.T("codex.infused"):""),small);
                    CodexLabel(new Rect(x+10,ny+65,246,60),Loc.T("codex.node_cost",node.mana,node.cooldown,node.id=="trap"?8:node.range,node.cost),small);
                    if(GUI.Button(new Rect(x,ny,266,137),GUIContent.none,GUIStyle.none))inspectedSkill=node.id;
                    if(column<2){Box(x+267,ny+66,17,2,amber);CodexLabel(new Rect(x+269,ny+53,17,25),Loc.T("codex.edge"),small);}
                    node=c.skills.FirstOrDefault(s=>s.profession==inspectedClass&&s.prerequisite==node.id);column++;
                }
                row++;
            }
            y=baseY+row*154+8;var skill=c.Skill(inspectedSkill);if(skill==null)return;
            CodexLine(ref y,Loc.Skill(skill.id),subtitle);
            CodexLine(ref y,Loc.T("codex.prerequisite",string.IsNullOrEmpty(skill.prerequisite)?Loc.T("ui.none"):Loc.Skill(skill.prerequisite)),small);
            CodexLine(ref y,Loc.T("codex.skill_cost",skill.mana,skill.cooldown,skill.id=="trap"?8:skill.range,skill.cost),small);
            if(a.profession!=skill.profession)CodexLine(ref y,Loc.T("codex.other_class"),small);
            else if(!a.unlocked.Contains(skill.id))CodexLine(ref y,Loc.T(a.skillPoints>=skill.cost&&(string.IsNullOrEmpty(skill.prerequisite)||a.unlocked.Contains(skill.prerequisite))?"codex.unlockable":"codex.not_ready"),small);
            CodexLine(ref y,Loc.T("codex.weapon",string.IsNullOrEmpty(skill.weapon)?Loc.T("codex.no_weapon"):Loc.T("codex.weapon."+skill.weapon)),small);
            CodexLine(ref y,CodexText.SkillEffect(a,skill,tower==null||tower.Rules.holyRecoveryCost));
            if(skill.effect=="Damage"||skill.effect=="Heal")CodexLine(ref y,Loc.T("codex.formula",skill.power,Loc.T(skill.profession==Profession.Healer?"codex.wis":"codex.int"),CodexText.N(CodexText.Power(a,skill))),small);
            CodexLine(ref y,Loc.T("codex.skill_note"),small);
            if(!string.IsNullOrEmpty(a.weapon.infusion)&&a.weapon.infusion!=skill.id)
            {var infused=c.Skill(a.weapon.infusion);if(infused!=null){CodexLine(ref y,Loc.T("codex.infused")+" / "+Loc.Skill(infused.id),subtitle);CodexLine(ref y,CodexText.SkillEffect(a,infused,tower==null||tower.Rules.holyRecoveryCost),small);}}
        }
    }
}
