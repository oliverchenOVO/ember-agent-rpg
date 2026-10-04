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
        TowerSimulation tower;HttpReasonerTransport llmTransport;bool towerSmoke,towerQA,reasonerDebug;
        bool towerCaptured,towerClear;int towerLastFloor,towerMaxFloor;
        string[] towerCases={"guardian","caster","charger","summoner","environment","phase_transition","refuge","split","independent","memory","relationships","saved","loaded","book","history","final","english","ice","castle","abyss","terminal_forest","terminal_ice","terminal_castle","terminal_abyss","debug"};
        void StartExpedition(Catalog c,string[] args)
        {
            IAgentReasoner reasoner=null;
            if(Array.IndexOf(args,"--llm")>=0)
            {
                string endpoint=Environment.GetEnvironmentVariable("EMBER_REASONER_URL");
                if(!string.IsNullOrWhiteSpace(endpoint))
                {
                    try{llmTransport=new HttpReasonerTransport(endpoint,Environment.GetEnvironmentVariable("EMBER_REASONER_KEY"));reasoner=new LLMReasoner(llmTransport);}
                    catch{Debug.LogWarning("EMBER optional reasoner unavailable; using local rules");}
                }
            }
            tower=new TowerSimulation(c,TowerContent.Load(),1729,Array.IndexOf(args,"--showcase")>=0,reasoner);
            int rulesIndex=Array.IndexOf(args,"--balance-config");if(rulesIndex>=0)tower.Rules=JsonUtility.FromJson<StabilizationRules>(File.ReadAllText(args[rulesIndex+1]));
            towerSmoke=Array.IndexOf(args,"--phase2-smoke")>=0;towerQA=Array.IndexOf(args,"--phase2-qa")>=0;simulation.Restore(tower.Observe(0));
            if(Array.IndexOf(args,"--phase3-qa")>=0){towerQA=true;towerCases=new[]{"foundry6","foundry7","foundry8","foundry9","foundry10","lane","cross","annulus","foundry_phase2","foundry_phase3","foundry_refuge","infusion_sword","infusion_bow","infusion_staff","foundry_intel","foundry_death","foundry_english","foundry_saved","foundry_loaded","foundry_split","holy_tooltip"};}
            StartCombatArtQA(args);StartRefugeQA(args);
            if(Array.IndexOf(args,"--knowledge-qa")>=0){towerQA=true;towerCases=new[]{"codex_book","codex_book_bottom","codex_boss","codex_boss_bottom","codex_warrior","codex_archer","codex_mage","codex_healer","codex_skill_detail","codex_archer_detail","codex_healer_detail","codex_legacy","codex_empty"};}
            if(Array.IndexOf(args,"--readability-qa")>=0){towerQA=true;towerCases=new[]{"read_pressure","read_overtime","read_burn","read_summons","read_hazard","read_combined","read_book_kael","read_book_lyra","read_book_missing","read_book_old","read_book_full","read_book_all"};}
            if(Array.IndexOf(args,"--pressure-qa")>=0){towerQA=true;towerCases=new[]{"pressure_idle","pressure_warning","pressure_active","pressure_outside","pressure_overtime","pressure_broken","pressure_disabled","pressure_combined","pressure_attack","pressure_saved","pressure_refuge","pressure_dossier"};}
            if(Array.IndexOf(args,"--inspection-qa")>=0){towerQA=true;towerCases=new[]{"inspect_bag","inspect_bag_bottom","inspect_bag_empty","inspect_memory","inspect_memory_bottom","inspect_memory_empty","inspect_camera","inspect_camera_zoom","inspect_camera_orbit","inspect_camera_pan","inspect_camera_reset","inspect_hover_blank","inspect_hover_skill","inspect_hover_infusion"};}
            if(Array.IndexOf(args,"--rest-interaction-qa")>=0){towerQA=true;towerCases=new[]{"foundry_empty","foundry_partial","crossing_empty","crossing_partial","foundry_dead_mage"};}
        }
        void LoadExpedition()
        {
            string path=savePath;
            if(!File.Exists(path)&&!towerSmoke&&!towerQA){string legacy=Path.Combine(Application.persistentDataPath,"witness-save.json");if(File.Exists(legacy))path=legacy;}
            ExpeditionState loaded;
            try{loaded=ExpeditionStore.Load(path,simulation.Catalog,tower.Data);}
            catch{loaded=ExpeditionStore.Migrate(SaveStore.Load(path),simulation.Catalog,tower.Data);}
            tower.Restore(loaded);simulation.Restore(tower.Observe(selected));
        }
        string GroupStatus(GroupState g)=>Loc.T(g.completed?"p2.group_complete":g.terminal?"p2.group_dead":g.travelLeft>0?"p2.group_travel":g.phase==Phase.Battle?"p2.group_battle":"p2.group_rest");
        string BossStatus()
        {
            var b=tower.State.GroupOf(selected).boss;return Loc.T("p2.boss_state",Loc.T(tower.Data.Boss(b.definition).phases[b.phase].nameKey),b.adds,b.interrupts);
        }
        void DrawGroupSidebar()
        {
            int row=0;foreach(var g in tower.State.groups){Text(1302,729+row*22,250,22,Loc.T("p2.group_row",g.id,g.floor,GroupStatus(g)),small);row++;}
        }
        void DrawExpeditionDetail()
        {
            if(tab==4)
            {
                int row=0;foreach(var g in tower.State.groups)
                {
                    string members=string.Join(" / ",g.members.Select(id=>tower.State.world.agents[id].name));
                    Text(46,714+row*27,1150,25,Loc.T("p2.group_row",g.id,g.floor,GroupStatus(g))+" / "+members,label);row++;
                }
            }
            else
            {
                var m=tower.State.Memory(selected);var plan=tower.State.Plan(selected);
                var selectedBoss=tower.State.GroupOf(selected).boss;
                var statuses=selectedBoss.statuses.Where(s=>s.agent==selected).Select(s=>Loc.T("p2.status."+s.kind)).Concat(selectedBoss.effects.Where(e=>e.agent==selected).Select(e=>Loc.T("p3.status."+e.kind))).Distinct();
                Text(46,714,1140,24,Loc.T("p2.goal",Loc.T("p2.goal."+plan.decision.intent))+" / "+Loc.T("p2.status",statuses.Any()?string.Join(" / ",statuses):Loc.T("p2.none")),label);
                var g=tower.State.GroupOf(selected);var learned=m.entries.FirstOrDefault(e=>e.key=="intel:"+g.floor);
                string intel=learned!=null?(string.IsNullOrEmpty(tower.Data.Floor(g.floor).intelKey)?string.Join(" / ",tower.Data.Boss(g.boss.definition).phases[g.boss.phase].abilities.Select(id=>Loc.T(tower.Data.Ability(id).nameKey))):Loc.T(tower.Data.Floor(g.floor).intelKey)):Loc.T("p2.intel_unknown");
                Text(46,741,1140,24,Loc.T("p2.intel",intel),small);
                var salient=m.salient.LastOrDefault();Text(46,767,1140,24,salient!=null?RenderForUI(salient.evidence):Loc.T("p2.observation"),small);
                var memory=m.entries.OrderByDescending(e=>e.importance).ThenBy(e=>e.age).FirstOrDefault();
                Text(46,794,1140,28,reasonerDebug?Loc.T("p2.provider",Loc.T("p2."+plan.decision.provider),plan.revision):memory!=null?Loc.T("p2.memory",Loc.T("p2.source."+memory.source),RenderForUI(memory.text)):Loc.T("p2.split_status",tower.State.groups.Count,tower.State.splits,tower.State.rejoins),small);
            }
        }
        void UpdateTowerSmoke()
        {
            int floor=tower.State.groups.Max(g=>g.floor);
            towerMaxFloor=Math.Max(towerMaxFloor,floor);
            if(floor!=towerLastFloor){towerLastFloor=floor;Debug.Log("EMBER EXPEDITION FLOOR / "+floor);}
            if(!shotBattle&&tower.State.world.clock>5){shotBattle=true;ScreenCapture.CaptureScreenshot(Path.Combine(artifactPath,"phase2-battle.png"));}
            if(!shotRest&&tower.State.groups.Any(g=>g.phase==Phase.Rest)){shotRest=true;tab=4;ScreenCapture.CaptureScreenshot(Path.Combine(artifactPath,"phase2-groups.png"));}
            if(!shotEnd&&tower.State.world.phase==Phase.Ended){shotEnd=true;towerClear=tower.State.world.outcome==Outcome.TowerClear;tab=2;ScreenCapture.CaptureScreenshot(Path.Combine(artifactPath,"phase2-end.png"));ExpeditionStore.Save(savePath,tower.State);ExpeditionStore.Save(Path.Combine(artifactPath,"phase2-run.json"),tower.State);}
            if(tower.State.world.run>=2&&tower.State.world.clock>=2){Debug.Log("EMBER PHASE2 PLAYER SMOKE / max floor "+towerMaxFloor+" / clear "+towerClear);RequestQuit(shotBattle&&shotRest&&shotEnd&&towerLastFloor==1&&towerMaxFloor==25&&towerClear?0:5);}
            if(Time.realtimeSinceStartup>600){Debug.LogError("EMBER PHASE2 PLAYER TIMEOUT");RequestQuit(6);}
        }
        void UpdateTowerQA()
        {
            if(Time.realtimeSinceStartup<qaNext)return;
            if(qaStage>=0&&!towerCaptured){ScreenCapture.CaptureScreenshot(Path.Combine(artifactPath,towerCases[qaStage]+".png"));towerCaptured=true;qaNext=Time.realtimeSinceStartup+.2f;return;}
            towerCaptured=false;qaStage++;
            if(qaStage>=towerCases.Length)
            {
                foreach(string key in Loc.MissingKeys)qaIssues.Add("Missing key "+key);
                File.WriteAllText(Path.Combine(artifactPath,"layout-results.txt"),"Phase2 scenarios: "+towerCases.Length+" / "+Screen.width+"x"+Screen.height+" / issues: "+qaIssues.Count+"\n"+string.Join("\n",qaIssues));
                towerQA=false;qaEnabled=false;paused=true;Invoke(nameof(ExitLocalizationQA),.8f);return;
            }
            qaScenario=towerCases[qaStage];SetupTowerScenario(qaScenario);qaNext=Time.realtimeSinceStartup+(combatArtQA?.22f:.6f);
        }
        void SetupTowerScenario(string scenario)
        {
            inspection=Inspection.None;codexScroll=Vector2.zero;
            tower.Dispose();tower=new TowerSimulation(simulation.Catalog,TowerContent.Load(),1729,true);selected=0;tab=0;paused=true;noticeTimer=0;reasonerDebug=false;speed=1;
            Loc.SetLocale(qaLocaleOverride??(scenario=="english"||scenario=="foundry_english"?"en":"zh-TW"));var g=tower.State.groups[0];int floor=Array.IndexOf(towerCases,scenario)+1;
            if(floor>5)floor=scenario=="final"?25:scenario=="ice"?6:scenario=="castle"?11:scenario=="abyss"?16:scenario=="terminal_forest"?21:scenario=="terminal_ice"?22:scenario=="terminal_castle"?23:scenario=="terminal_abyss"?24:1;
            g.floor=floor;g.boss=BossRuntime.Create(tower.Data.Boss(tower.Data.Floor(floor).bossId));
            if(towerCases[0].StartsWith("foundry"))SetupFoundryQA(scenario,g);
            if(scenario=="caster"||scenario=="charger"||scenario=="summoner"||scenario=="environment")
            {g.boss.ability=tower.Data.Boss(g.boss.definition).phases[0].abilities[0];g.boss.visible.telegraph=true;g.boss.visible.windup=1;g.boss.visible.targetX=-2;g.boss.visible.targetZ=-3;if(scenario=="summoner")g.boss.adds=3;}
            if(scenario=="phase_transition"){g.boss.visible.hp*=.4f;tower.Step();}
            if(scenario=="refuge"||scenario=="split"||scenario=="independent"||scenario=="saved"||scenario=="loaded"||scenario=="debug")
            {
                g.boss.visible.hp=0;tower.EnterRest(g);g.phaseClock=7;
                if(scenario!="refuge"){var branch=tower.Split(g.id,new[]{2,3});tower.Split(g.id,new[]{1});if(scenario=="independent"){foreach(var a in tower.State.Members(branch))a.escaped=true;tower.NextFloor(branch);selected=3;}tab=4;}
            }
            if(scenario=="memory"||scenario=="relationships"||scenario=="debug"||scenario=="english")
            {tower.CompleteSite(tower.State.world.agents[0],g,tower.Data.rests[0].sites.First(s=>s.id=="library"));selected=1;tab=scenario=="relationships"?3:5;if(scenario=="debug")reasonerDebug=true;}
            simulation.Restore(tower.Observe(selected));
            if(scenario=="saved")SaveGame();if(scenario=="loaded"){SaveGame();tower.State.world.agents[0].hp=1;LoadGame();}
            if(scenario=="book"||scenario=="history"||scenario=="final")
            {
                if(scenario=="final"){g.floor=25;g.boss=BossRuntime.Create(tower.Data.Boss(tower.Data.Floor(25).bossId));g.boss.visible.hp=0;tower.EnterRest(g);foreach(var a in tower.State.Members(g))a.escaped=true;tower.NextFloor(g);tower.Finish(Outcome.TowerClear);}
                else {tower.Die(tower.State.world.agents[0],Loc.Token("cause.collapse"));tower.Finish(Outcome.Wipe);}tab=scenario=="book"?1:2;
            }
            if(scenario.StartsWith("pressure_"))SetupPressureQA(scenario,g);
            if(scenario.StartsWith("read_"))SetupReadabilityQA(scenario,g);
            if(scenario.StartsWith("inspect_"))SetupInspectionQA(scenario,g);
            if(scenario.StartsWith("codex_"))SetupKnowledgeQA(scenario,g);
            if(scenario.StartsWith("art_"))SetupCombatArtQA(scenario,g);
            if(scenario.StartsWith("refuge_"))SetupRefugeQA(scenario,g);
            simulation.Restore(tower.Observe(selected));lastRun=simulation.State.run;lastOutcome=(int)simulation.State.outcome;view.RebuildCharacters(simulation.State,simulation.Catalog);weaponSignature="";
        }
    }
}
