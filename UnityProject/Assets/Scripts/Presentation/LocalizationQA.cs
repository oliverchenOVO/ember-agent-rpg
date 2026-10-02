using System;
using System.Collections.Generic;
using System.IO;
using Ember.Core;
using UnityEngine;

namespace Ember.Presentation
{
    public sealed partial class WitnessGame
    {
        bool qaEnabled,qaCaptured;int qaStage=-1;float qaNext;string qaScenario="warmup";
        readonly HashSet<string> qaIssues=new HashSet<string>();
        readonly string[] qaCases={"battle","telegraph","enraged","refuge","four_skills","collapse","book_empty","history_empty","book","history","relationships","healer","pause","speed","saved","loaded","load_failed","save_failed","new_life","english","zh_restored","legacy"};
        void TogglePause(){paused=!paused;}
        void CycleSpeed(){speed=speed==1?2:speed==2?4:1;}
        void SaveGame(){try{if(tower!=null)Ember.Core.Phase2.ExpeditionStore.Save(savePath,tower.State);else SaveStore.Save(savePath,simulation.State);Notice(Loc.Token("notice.saved"));}catch(Exception e){if(!qaEnabled)Debug.LogException(e);Notice(Loc.Token("notice.save_failed"));}}
        void LoadGame()
        {
            try{if(tower!=null)LoadExpedition();else simulation.Restore(SaveStore.Load(savePath));lastRun=simulation.State.run;lastOutcome=(int)simulation.State.outcome;view.RebuildCharacters(simulation.State,simulation.Catalog);accumulator=0;Notice(Loc.Token("notice.loaded"));}
            catch(Exception e){if(!qaEnabled)Debug.LogException(e);Notice(Loc.Token("notice.load_failed"));}
        }
        void NewLife(){if(tower!=null){tower.Finish(Outcome.Wipe);tower.NewLife();simulation.Restore(tower.Observe(selected));}else {simulation.Finish(Outcome.Wipe);simulation.State.run++;simulation.Begin();}}
        void CheckLayout(Rect rect,string text,GUIStyle style,bool isButton)
        {
            float required=style.CalcHeight(new GUIContent(text),rect.width);
            if(required>rect.height+1)qaIssues.Add(qaScenario+" / height "+required.ToString("F1")+">"+rect.height+" / "+text);
            if(isButton&&style.CalcSize(new GUIContent(text)).x>rect.width+1)qaIssues.Add(qaScenario+" / button width / "+text);
            if(rect.xMin<0||rect.xMax>1600||rect.yMin<0||rect.yMax>900)qaIssues.Add(qaScenario+" / outside viewport / "+text);
            foreach(char ch in text)if(!char.IsControl(ch)&&!uiFont.HasCharacter(ch))qaIssues.Add(qaScenario+" / missing glyph / U+"+((int)ch).ToString("X4"));
            if(text.Contains("@event.")||text.Contains("@reason.")||text.Contains("@epitaph."))qaIssues.Add(qaScenario+" / untranslated token");
        }
        void UpdateLocalizationQA()
        {
            if(Time.realtimeSinceStartup<qaNext)return;
            if(qaStage>=0&&!qaCaptured){ScreenCapture.CaptureScreenshot(Path.Combine(artifactPath,qaCases[qaStage]+".png"));qaCaptured=true;qaNext=Time.realtimeSinceStartup+.2f;return;}
            qaCaptured=false;
            qaStage++;
            if(qaStage>=qaCases.Length)
            {
                foreach(string missing in Loc.MissingKeys)qaIssues.Add("Missing string key: "+missing);
                File.WriteAllText(Path.Combine(artifactPath,"layout-results.txt"),"Scenarios: "+qaCases.Length+" / resolution "+Screen.width+"x"+Screen.height+" / Issues: "+qaIssues.Count+"\n"+string.Join("\n",qaIssues));
                Debug.Log("EMBER LOCALIZATION PLAYER QA / issues "+qaIssues.Count);qaEnabled=false;paused=true;Invoke(nameof(ExitLocalizationQA),.8f);return;
            }
            qaScenario=qaCases[qaStage];SetupLocalizationScenario(qaScenario);qaNext=Time.realtimeSinceStartup+.6f;
        }
        void ExitLocalizationQA(){RequestQuit(qaIssues.Count==0?0:4);}
        void SetupLocalizationScenario(string scenario)
        {
            simulation=new Simulation(simulation.Catalog,1729,true);selected=0;tab=0;paused=true;noticeTimer=0;notice="";speed=1;
            Loc.SetLocale(scenario=="english"?"en":"zh-TW");var w=simulation.State;
            if(scenario=="telegraph"){w.boss.telegraph=true;w.boss.windup=.7f;w.boss.targetX=w.agents[0].x;w.boss.targetZ=w.agents[0].z;}
            if(scenario=="enraged"){w.boss.enraged=true;w.boss.hp=w.boss.maxHp*.35f;}
            bool rest=scenario=="refuge"||scenario=="four_skills"||scenario=="collapse";
            if(rest){w.boss.hp=0;simulation.EnterRest();w.phaseClock=scenario=="collapse"?29:7;}
            if(scenario=="four_skills")
            {
                var a=w.agents[0];a.skillPoints=10;foreach(var sk in simulation.Catalog.skills)if(sk.profession==a.profession)simulation.Unlock(a,sk.id);
                a.weapon=a.Make("greatsword",1.57f,"groupheal");a.materials=999;a.level=99;a.taskTimer=3.9f;
            }
            if(scenario=="book_empty")tab=1;if(scenario=="history_empty")tab=2;
            if(scenario=="book"||scenario=="history")
            {
                simulation.Die(w.agents[0],Loc.Token("cause.collapse"));simulation.Finish(Outcome.Wipe);tab=scenario=="book"?1:2;
            }
            if(scenario=="relationships"||scenario=="healer"){tab=3;selected=scenario=="healer"?3:0;w.agents[selected].bonds[0].trust=.75f;}
            if(scenario=="pause"){TogglePause();TogglePause();}
            if(scenario=="speed"){CycleSpeed();CycleSpeed();}
            if(scenario=="saved")SaveGame();
            if(scenario=="loaded"){SaveGame();w.agents[0].hp=1;LoadGame();}
            if(scenario=="load_failed") {string original=savePath;savePath=Path.Combine(artifactPath,"does-not-exist.json");LoadGame();savePath=original;}
            if(scenario=="save_failed")
            {
                string original=savePath,blocker=Path.Combine(artifactPath,"blocked-path");File.WriteAllText(blocker,"QA fixture");savePath=Path.Combine(blocker,"save.json");SaveGame();savePath=original;
            }
            if(scenario=="new_life")NewLife();
            if(scenario=="legacy")
            {
                w.Say(0,"Use Crescent slash; it fits the current opening.");w.Say(3,"My path ends here: the collapsing refuge.","death");w.Say(-1,"Thorns ripple across the arena.","boss");
            }
            if(scenario=="english")w.Say(0,Loc.Token("event.gift","LYRA",Loc.Ref("item","greatsword"),Loc.Ref("skill","heal")),"social");
            lastRun=simulation.State.run;lastOutcome=(int)simulation.State.outcome;view.RebuildCharacters(simulation.State,simulation.Catalog);weaponSignature="";
        }
    }
}
