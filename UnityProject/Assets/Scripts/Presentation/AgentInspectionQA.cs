using System;
using System.IO;
using System.Linq;
using Ember.Core;
using Ember.Core.Phase2;
using UnityEngine;
namespace Ember.Presentation
{
    public sealed partial class WitnessGame
    {
        bool inspectionCameraQA,portfolioQA;
        void SetupJournalQA(string scenario)
        {
            var w=tower.State.world;w.messages.Clear();w.history.Clear();
            for(int i=0;i<(portfolioQA?125:100);i++)
            {
                int speaker=i%5-1;w.clock=i;
                w.Say(speaker,Loc.Token("event.gift",speaker<0?"KAEL":w.agents[speaker].name,Loc.Ref("item","greatsword"),Loc.Ref("skill","heal")));
            }
            for(int i=1;i<=8;i++)w.history.Add(new RunRecord{run=i,floor=i+2,deaths=2,seconds=i*62,damage=i*140,healing=i*30,outcome=Outcome.Wipe,
                composition=new System.Collections.Generic.List<string>{Loc.Token("ui.agent_class","KAEL",Loc.Ref("class",Profession.Mage))},
                survivors=new System.Collections.Generic.List<string>{"LYRA","SERA"},builds=new System.Collections.Generic.List<string>{"greatsword:meteor","KAEL: greatsword [meteor] / heal,fireball"}});
            simulation.Restore(tower.Observe(selected));string before=JsonUtility.ToJson(tower.State);
            paused=false;OpenInspection(Inspection.Chronicle);if(!paused)throw new Exception("Journal must pause simulation");CloseInspection();if(paused)throw new Exception("Journal must restore running state");
            paused=true;OpenInspection(Inspection.Chronicle);CloseInspection();if(!paused)throw new Exception("Journal must preserve paused state");
            OpenInspection(Inspection.Chronicle);journalSpeaker=-2;
            if(JournalMessages().Length!=100||JournalMessages()[0].time!=(portfolioQA?124:99))throw new Exception("Journal retention/order");
            journalSpeaker=1;if(JournalMessages().Length!=20||JournalMessages().Any(m=>m.speaker!=1))throw new Exception("Journal speaker filter");
            journalSpeaker=-1;if(JournalMessages().Length!=20)throw new Exception("Journal system filter");
            journalSpeaker=-2;journalQuery="  "+Loc.Item("greatsword")+"  ";if(JournalMessages().Length!=100)throw new Exception("Journal localized search/trim");
            journalQuery="__none__";if(JournalMessages().Length!=0)throw new Exception("Journal empty search");
            journalQuery="";journalSpeaker=scenario=="journal_agent"?1:scenario=="journal_system"?-1:-2;
            if(scenario=="journal_search")journalQuery="kael";
            if(scenario=="journal_empty")journalQuery="__none__";
            if(scenario.StartsWith("journal_history")){if(scenario.EndsWith("empty"))w.history.Clear();simulation.Restore(tower.Observe(selected));OpenInspection(Inspection.History);}
            if(scenario=="journal_history_empty")before=JsonUtility.ToJson(tower.State);
            if(before!=JsonUtility.ToJson(tower.State))throw new Exception("Inspection changed simulation");
            string renderedBuild=HistoryBuild("greatsword:meteor");
            if(!renderedBuild.Contains(Loc.Item("greatsword"))||!renderedBuild.Contains(Loc.Skill("meteor")))throw new Exception("History build must show actual equipment and infusion");
            string legacyBuild=HistoryBuild("KAEL: greatsword [meteor] / heal,fireball");
            if(!legacyBuild.Contains("KAEL")||!legacyBuild.Contains(Loc.Skill("heal"))||!legacyBuild.Contains(Loc.Skill("fireball")))throw new Exception("Legacy history build must show actual equipped skills");
            if(portfolioQA)CheckPortfolioObserver(scenario);
            Debug.Log("EMBER JOURNAL QA / "+scenario+" / filters, order, localized search, pause restoration and state isolation passed");
        }
        void CheckPortfolioObserver(string scenario)
        {
            void Require(bool value,string message){if(!value)throw new Exception("Portfolio regression: "+message);}
            string fixture=JsonUtility.ToJson(tower.State);Inspection originalMode=inspection;int originalSelected=selected,originalSpeaker=journalSpeaker;string originalQuery=journalQuery;
            string locale=Loc.Locale;CloseInspection();paused=false;speed=4;OpenInspection(Inspection.Chronicle);
            string state=JsonUtility.ToJson(tower.State);journalSpeaker=-2;journalQuery="";
            Require(JournalMessages().Length==100&&JournalMessages().Last().time==25,"125 events retain latest 100");
            journalSpeaker=1;journalQuery=Loc.Item("greatsword");Require(JournalMessages().Length==20,"combined localized filter/search");
            selected=3;OpenInspection(Inspection.Chronicle);Require(journalSpeaker==3&&JournalMessages().All(m=>m.speaker==3),"selection source");
            for(int i=1;i<=3;i++)Require(ObserverHotkeys(false,true,i,i-2,true)==0&&paused&&speed==4,"IME digit/Space/arrow isolation");
            Require(ObserverHotkeys(true,false,0,1,true)==0&&inspection==Inspection.Chronicle,"IME Escape stays in reader");
            Require(ObserverHotkeys(false,true,1,-1,false)==0&&paused&&speed==4,"non-IME input isolation");
            Loc.SetLocale(locale=="en"?"zh-TW":"en");journalSpeaker=-2;journalQuery=Loc.Item("greatsword");
            Require(JournalMessages().Length==100,"language changes localized search");Require(HistoryBuild("greatsword:meteor").Contains(Loc.Skill("meteor")),"history locale cache");
            Loc.SetLocale(locale);Require(codexScroll==Vector2.zero,"locale scroll reset");
            Require(HistoryBuild(null)==Loc.T("p2.none"),"missing historical build fallback");
            Require(state==JsonUtility.ToJson(tower.State),"reader/filter/locale leave world and RNG unchanged");
            CloseInspection();Require(!paused&&speed==4,"restore playing and speed");paused=true;OpenInspection(Inspection.History);CloseInspection();Require(paused&&speed==4,"restore previous pause");
            // Current expedition round-trip; no real user save is touched by QA.
            SaveGame();float hp=tower.State.world.agents[0].hp;tower.State.world.agents[0].hp=1;LoadGame();
            Require(tower.State.world.agents[0].hp==hp&&tower.State.world.messages.Count==100,"current save/load");
            // Schema-1 fixture exercises the actual legacy loader and migration path.
            string currentSave=savePath;savePath=Path.Combine(artifactPath,"legacy-fixture.json");SaveStore.Save(savePath,tower.State.world);LoadGame();savePath=currentSave;Require(tower.State.world.history.Count==(scenario=="journal_history_empty"?0:8),"older schema-1 history migration");
            Require(tower.State.world.history.Count==0||HistoryBuild(tower.State.world.history[0].builds[1]).Contains(Loc.Skill("heal")),"legacy names after load");
            TogglePause();Require(!paused,"resume control");TogglePause();CycleSpeed();Require(speed==1,"speed cycle control");
            int run=tower.State.world.run;NewLife();Require(tower.State.world.run==run+1,"new life");
            tower.Restore(ExpeditionStore.Parse(fixture,simulation.Catalog,tower.Data));selected=originalSelected;simulation.Restore(tower.Observe(selected));OpenInspection(originalMode);
            journalSpeaker=scenario=="journal_combined"?1:originalSpeaker;journalQuery=scenario=="journal_combined"?Loc.Item("greatsword"):scenario=="journal_input"?"123 巨劍":originalQuery;
            Debug.Log("EMBER PORTFOLIO OBSERVER ASSERTIONS PASSED / "+scenario+" / retention, combined search, locale cache, input dispatch, pause/speed, schema 1/2, save/load/new life");
        }
        void SetupInspectionQA(string scenario,GroupState g)
        {
            inspectionQAMouse=null;tooltipQA=false;view.ResetRefugeCamera();view.RefugeOverview=false;inspectionCameraQA=scenario.StartsWith("inspect_camera");
            var a=tower.State.world.agents[0];
            if(scenario.StartsWith("inspect_bag"))
            {
                a.materials=11;a.weapon=a.Make("greatsword",1.5f,"meteor");a.weapon.affix="Astral";a.weapon.upgrade=2;a.inventory.Clear();
                if(!scenario.EndsWith("empty"))for(int i=0;i<24;i++){var def=simulation.Catalog.items[i%simulation.Catalog.items.Length];a.inventory.Add(a.Make(def.id,1+i*.02f,def.kind=="Weapon"&&i%5==0?"heal":""));}
                OpenInspection(Inspection.Inventory);
            }
            else if(scenario.StartsWith("inspect_memory"))
            {
                var m=tower.State.Memory(0);m.entries.Clear();m.salient.Clear();
                if(!scenario.EndsWith("empty"))
                {
                    g.boss.ability=tower.Data.Boss(g.boss.definition).phases[0].abilities[0];g.boss.visible.telegraph=true;tower.ObserveBoss(g);
                    for(int i=0;i<45;i++)m.Add(new Knowledge{key="fixture:"+i,text=Loc.Token("memory.craft",Loc.Ref("item","greatsword"),Loc.Ref("skill","meteor")),scope=(MemoryScope)(i%5),source=(KnowledgeSource)(i%3),informant=1,run=1,confidence=.7f,importance=.5f,age=i*17});
                    RelationshipSystem.Apply(a,tower.State.world.agents[1],m,tower.State.relationships,RelationshipEventKind.Healed,1,32);
                }
                OpenInspection(Inspection.Memories);
            }
            else if(scenario.StartsWith("inspect_hover"))
            {
                a.weapon.infusion=scenario.EndsWith("infusion")?"meteor":"";a.equipped.Clear();a.equipped.AddRange(new[]{"meteor","frost","shield","fireball"});
                inspectionQAMouse=scenario.EndsWith("blank")?new Vector2(80,525):scenario.EndsWith("infusion")?new Vector2(80,480):new Vector2(60,555);
            }
            else
            {
                g.boss.visible.hp=0;tower.EnterRest(g);g.phaseClock=7;view.SetExpedition(tower.Data,g);simulation.Restore(tower.Observe(0));view.Update(simulation.State,1,0);
                string before=JsonUtility.ToJson(tower.State);
                view.ObserveRefugeCamera(1,new Vector2(30,20),new Vector2(15,-10));
                if(!view.RefugeManual)throw new Exception("Camera gesture did not enter manual observation");
                float distance=view.RefugeDistance,yaw=view.RefugeYaw;Vector3 pivot=view.RefugePivot;
                view.ObserveRefugeCamera(1,Vector2.zero,Vector2.zero);if(view.RefugeDistance>=distance)throw new Exception("Wheel zoom failed");
                view.ObserveRefugeCamera(0,new Vector2(30,20),Vector2.zero);if(view.RefugeYaw==yaw)throw new Exception("Orbit failed");
                view.ObserveRefugeCamera(0,Vector2.zero,new Vector2(20,20));if(view.RefugePivot==pivot)throw new Exception("Pan failed");
                view.ObserveRefugeCamera(1000,new Vector2(0,10000),new Vector2(100000,100000));if(view.RefugeDistance!=8||view.RefugePitch!=80||Mathf.Abs(view.RefugePivot.x)>30||Mathf.Abs(view.RefugePivot.z)>20)throw new Exception("Camera limits failed");
                if(before!=JsonUtility.ToJson(tower.State))throw new Exception("Observation changed simulation state");
                view.ResetRefugeCamera();view.Update(simulation.State,1,0);
                if(scenario.EndsWith("zoom"))view.ObserveRefugeCamera(6,Vector2.zero,Vector2.zero);
                if(scenario.EndsWith("orbit"))view.ObserveRefugeCamera(0,new Vector2(180,80),Vector2.zero);
                if(scenario.EndsWith("pan"))view.ObserveRefugeCamera(0,Vector2.zero,new Vector2(90,25));
                if(scenario.EndsWith("reset")){view.ObserveRefugeCamera(3,new Vector2(60,0),Vector2.zero);view.ResetRefugeCamera();if(view.RefugeManual)throw new Exception("Reset failed");}
                File.WriteAllText(Path.Combine(artifactPath,"camera-check.txt"),"PASS: wheel zoom, orbit, pan, limits, reset and unchanged simulation state. Shared input controller; physical mouse hardware not automated.");
            }
        }
    }
}
