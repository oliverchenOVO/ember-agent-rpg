using System;
using System.IO;
using Ember.Core;
using Ember.Core.Phase2;
using UnityEngine;

namespace Ember.Presentation
{
    public sealed partial class WitnessGame : MonoBehaviour
    {
        bool nativeNoSave,nativeUIOnly;public bool NativeSaveLoad{get;private set;}
        string qaLocaleOverride; Simulation simulation; WorldView view; float accumulator, speed=1; bool paused, smoke, collapseSmoke;
        bool shotBattle,shotRest,shotEnd; int selected, tab, lastRun, lastOutcome;
        string notice="", savePath, artifactPath, weaponSignature=""; float noticeTimer;
        Message lastSound;
        GUIStyle title,subtitle,label,small,number,button; Font uiFont; Texture2D white;
        Color ink=new Color(.94f,.95f,.93f),muted=new Color(.73f,.79f,.8f),amber=new Color(.98f,.77f,.48f);
        AudioSource audioSource; AudioClip strike,heal;
        void Start()
        {
            var args=Environment.GetCommandLineArgs();int qaLocaleIndex=Array.IndexOf(args,"--qa-locale");if(qaLocaleIndex>=0)qaLocaleOverride=args[qaLocaleIndex+1];int localeIndex=Array.IndexOf(args,"--locale");
            int fontProbe=Array.IndexOf(args,"--font-probe");if(fontProbe>=0){int outIndex=Array.IndexOf(args,"--artifacts");var test=gameObject.AddComponent<FontReproduction>();test.mode=args[fontProbe+1];test.path=args[outIndex+1];enabled=false;return;}
            int probe=Array.IndexOf(args,"--native-diag");if(probe>=0){gameObject.AddComponent<NativeAllocationProbe>().mode=args[probe+1];enabled=false;return;}
            Application.wantsToQuit+=CloseWindow;
            Loc.SetLocale(localeIndex>=0&&localeIndex+1<args.Length?args[localeIndex+1]:PlayerPrefs.GetString("locale","zh-TW"));
            uiFont=Resources.Load<Font>("Fonts/NotoSansCJKtc-Regular");
            if(uiFont==null)throw new InvalidOperationException("Missing embedded CJK font");
            Loc.Changed+=RefreshLanguage;
            Application.targetFrameRate=60; var c=JsonUtility.FromJson<Catalog>(Resources.Load<TextAsset>("catalog").text);
            smoke=Array.IndexOf(args,"--smoke")>=0;collapseSmoke=Array.IndexOf(args,"--collapse-smoke")>=0;qaEnabled=Array.IndexOf(args,"--localization-smoke")>=0;
            simulation=new Simulation(c,1729,smoke||Array.IndexOf(args,"--showcase")>=0);view=new WorldView();view.RebuildCharacters(simulation.State,c);lastRun=1;
            if(!smoke&&!collapseSmoke&&!qaEnabled&&Array.IndexOf(args,"--vertical-slice")<0)StartExpedition(c,args);
            savePath=Path.Combine(Application.persistentDataPath,"witness-save.json");
            if(tower!=null)savePath=Path.Combine(Application.persistentDataPath,"ember-expedition-save.json");
            int index=Array.IndexOf(args,"--artifacts");artifactPath=index>=0&&index+1<args.Length?args[index+1]:Application.persistentDataPath;
            if(smoke||collapseSmoke||qaEnabled||towerSmoke||towerQA){Directory.CreateDirectory(artifactPath);speed=towerSmoke?16:2;savePath=Path.Combine(artifactPath,tower!=null?"phase2-save.json":"smoke-save.json");}
            if(collapseSmoke)
            {
                simulation.EnterRest();simulation.State.phaseClock=27;
                foreach(var a in simulation.State.agents){a.x=-2+a.id*1.2f;a.z=-3+a.id*2;}
            }
            white=Texture2D.whiteTexture;audioSource=gameObject.AddComponent<AudioSource>();audioSource.volume=.08f;strike=Tone(150,.12f);heal=Tone(460,.22f);
            Debug.Log("EMBER PLAYER START / save "+savePath);
            Invoke(nameof(LocalizeWindowTitle),.3f);
            if(qaEnabled){paused=true;qaNext=Time.realtimeSinceStartup+.5f;}
            if(towerQA){paused=true;qaEnabled=true;qaNext=Time.realtimeSinceStartup+.5f;}
            nativeNoSave=Array.IndexOf(args,"--no-save-load")>=0;nativeUIOnly=Array.IndexOf(args,"--ui-only")>=0;NativeSaveLoad=Array.IndexOf(args,"--native-save-load")>=0;
            StartGlyphDiagnostics(args);StartProfile(args);if(nativeUIOnly){paused=true;view.SetUIOnly();}
        }
        AudioClip Tone(float frequency,float duration)
        {
            int n=(int)(44100*duration);var samples=new float[n];for(int i=0;i<n;i++) samples[i]=Mathf.Sin(i*frequency*Mathf.PI*2/44100)*Mathf.Exp(-i/(44100*duration*.2f))*.6f;
            var clip=AudioClip.Create("Ember feedback",n,1,44100,false);clip.SetData(samples,0);return clip;
        }
        void Update()
        {
            if(simulation==null)return;
            if(towerQA)UpdateTowerQA();else if(qaEnabled)UpdateLocalizationQA();
            if(Input.GetKeyDown(KeyCode.F8))reasonerDebug=!reasonerDebug;
            if(Input.GetKeyDown(KeyCode.Space))paused=!paused;
            if(Input.GetKeyDown(KeyCode.Alpha1))speed=1;if(Input.GetKeyDown(KeyCode.Alpha2))speed=2;if(Input.GetKeyDown(KeyCode.Alpha3))speed=4;
            if(!paused)
            {
                using var allocationStep=new AllocationScope(stepAlloc,profile&&profileDetailed);var stepWatch=profile?System.Diagnostics.Stopwatch.StartNew():null;
                accumulator+=Mathf.Min(Time.unscaledDeltaTime,.25f)*speed;
                while(accumulator>=Simulation.StepSeconds){if(tower!=null)tower.Step();else simulation.Step();accumulator-=Simulation.StepSeconds;}
                if(profile)profileAiMs=stepWatch.Elapsed.TotalMilliseconds;
            }
            if(tower!=null){simulation.Restore(tower.Observe(selected));view.SetExpedition(tower.Data,tower.State.GroupOf(selected));}
            var w=simulation.State;
            string signature="";foreach(var agent in w.agents)signature+=agent.weapon.id+agent.weapon.infusion+";";
            if(signature!=weaponSignature){weaponSignature=signature;view.RebuildCharacters(w,simulation.Catalog);}
            if(w.run!=lastRun) {lastRun=w.run;view.RebuildCharacters(w,simulation.Catalog);TrySave();}
            if((int)w.outcome!=lastOutcome) {lastOutcome=(int)w.outcome;if(w.outcome!=Outcome.None)TrySave();}
            float orbit=(Input.GetKey(KeyCode.RightArrow)?1:0)-(Input.GetKey(KeyCode.LeftArrow)?1:0);
            if(!nativeUIOnly)using(var allocationView=new AllocationScope(viewAlloc,profile&&profileDetailed)){view.Update(w,Time.unscaledDeltaTime,orbit);view.ShowInfusions(w,simulation.Catalog);}noticeTimer-=Time.unscaledDeltaTime;
            if(towerSmoke)UpdateTowerSmoke();
            UpdateAudio();
            if(w.messages.Count>0&&w.messages[w.messages.Count-1]!=lastSound)
            {
                var msg=w.messages[w.messages.Count-1];lastSound=msg;
                if(sound!=null){if(msg.category=="skill")sound.Play(AudioEvent.Spell);if(msg.category=="heal")sound.Play(AudioEvent.Heal);if(msg.category=="craft")sound.Play(AudioEvent.Craft);if(msg.category=="exit")sound.Play(AudioEvent.Loot);}
            }
            if(smoke)
            {
                if(!shotBattle&&w.phase==Phase.Battle&&w.clock>=12){shotBattle=true;ScreenCapture.CaptureScreenshot(Path.Combine(artifactPath,"battle.png"));}
                if(!shotRest&&w.phase==Phase.Rest&&w.phaseClock>=7){shotRest=true;ScreenCapture.CaptureScreenshot(Path.Combine(artifactPath,"refuge.png"));}
                if(!shotEnd&&w.phase==Phase.Ended){shotEnd=true;ScreenCapture.CaptureScreenshot(Path.Combine(artifactPath,"end.png"));File.WriteAllText(Path.Combine(artifactPath,"player-run.json"),JsonUtility.ToJson(w,true));}
                if(w.run>=2&&w.clock>=2) {Debug.Log("EMBER PLAYER SMOKE PASSED / run ended and restarted");RequestQuit(shotBattle&&shotRest&&shotEnd?0:2);}
                if(Time.realtimeSinceStartup>150) {Debug.LogError("EMBER PLAYER SMOKE TIMEOUT");RequestQuit(3);}
            }
            if(collapseSmoke)
            {
                if(!shotRest&&Time.realtimeSinceStartup>1.5f){shotRest=true;ScreenCapture.CaptureScreenshot(Path.Combine(artifactPath,"collapse.png"));}
                if(Time.realtimeSinceStartup>3){Debug.Log("EMBER COLLAPSE PRESENTATION CHECK COMPLETE");RequestQuit(0);}
            }
        }
        void TrySave(){if(nativeNoSave)return;try{if(tower!=null)ExpeditionStore.Save(savePath,tower.State);else SaveStore.Save(savePath,simulation.State);}catch(Exception e){Debug.LogException(e);Notice(Loc.Token("notice.save_failed"));}}
        void LocalizeWindowTitle(){PlayerWindow.LocalizeTitle();}
        void RefreshLanguage(){title=null;LocalizeWindowTitle();}
        void OnDestroy(){CloseGlyphDiagnostics();Application.wantsToQuit-=CloseWindow;Loc.Changed-=RefreshLanguage;tower?.Dispose();llmTransport?.Dispose();view?.Dispose();if(strike!=null)Destroy(strike);if(heal!=null)Destroy(heal);}
        void Notice(string text){notice=text;noticeTimer=5;}
        void Styles()
        {
            if(title!=null)return;
            title=new GUIStyle(GUI.skin.label){font=uiFont,fontSize=29,padding=new RectOffset(),fontStyle=FontStyle.Bold,normal={textColor=ink}};
            subtitle=new GUIStyle(GUI.skin.label){font=uiFont,fontSize=14,padding=new RectOffset(),fontStyle=FontStyle.Bold,normal={textColor=amber}};
            label=new GUIStyle(GUI.skin.label){font=uiFont,fontSize=16,padding=new RectOffset(),normal={textColor=ink},wordWrap=true};
            small=new GUIStyle(label){font=uiFont,fontSize=14,normal={textColor=muted}};
            number=new GUIStyle(label){font=uiFont,fontSize=27,fontStyle=FontStyle.Bold};
            button=new GUIStyle{font=uiFont,fontSize=14,alignment=TextAnchor.MiddleCenter,normal={textColor=ink},hover={textColor=amber},active={textColor=amber},padding=new RectOffset(5,5,5,5)};
        }
        void Box(float x,float y,float width,float height,Color c){GUI.color=c;GUI.DrawTexture(new Rect(x,y,width,height),white);GUI.color=Color.white;}
        void Text(float x,float y,float width,float height,string text,GUIStyle style=null)
        {
            var chosen=style??label;var rect=new Rect(x,y,width,height);if(qaEnabled)CheckLayout(rect,text,chosen,false);
            glyphDrawIndex++;GUI.Label(rect,text,chosen);
        }
        void Bar(float x,float y,float width,float ratio,Color color,float height=4){Box(x,y,width,height,new Color(.14f,.2f,.21f));Box(x,y,width*Mathf.Clamp01(ratio),height,color);}
        bool Button(float x,float y,float width,string text){var rect=new Rect(x,y,width,32);Box(x,y,width,32,new Color(.13f,.21f,.23f,.95f));if(qaEnabled)CheckLayout(rect,text,button,true);bool pressed=GUI.Button(rect,text,button);if(pressed&&sound!=null)sound.Play(AudioEvent.UI);return pressed;}
        string TimeText(float seconds) => ((int)seconds/60).ToString("00")+":"+((int)seconds%60).ToString("00");
        void OnGUI()
        {
            using var allocationUI=new AllocationScope(uiAlloc,profile&&profileDetailed);using var uiSample=new Unity.Profiling.ProfilerMarker("Ember.UI").Auto();
            if(simulation==null||!enabled)return;glyphDrawIndex=0;Styles();float scale=Mathf.Min(Screen.width/1600f,Screen.height/900f);float ox=(Screen.width-1600*scale)/2,oy=(Screen.height-900*scale)/2;
            GUI.matrix=Matrix4x4.TRS(new Vector3(ox,oy,0),Quaternion.identity,new Vector3(scale,scale,1));
            var w=simulation.State;var a=w.agents[selected];
            Box(0,0,1600,86,new Color(.035f,.065f,.08f,.96f));Box(0,85,1600,1,new Color(.25f,.36f,.36f,.6f));
            Text(30,17,215,43,Loc.T("ui.brand"),title);Text(32,57,260,20,Loc.T("ui.tagline"),small);
            Text(337,21,120,24,Loc.T("ui.life"),subtitle);Text(338,42,120,42,w.run.ToString("D3"),number);
            Text(490,21,120,24,Loc.T("ui.floor"),subtitle);Text(491,42,120,42,w.floor.ToString("00")+" / 25",number);
            Text(636,21,120,24,Loc.T("ui.time"),subtitle);Text(637,42,120,42,TimeText(w.clock),number);
            Text(800,24,230,25,tower!=null?Loc.T(tower.Data.Floor(w.floor).nameKey):w.phase==Phase.Battle?Loc.T("ui.arena"):w.phase==Phase.Rest?Loc.T("ui.refuge"):Loc.T("ui.recorded"),label);
            Text(800,48,350,24,Loc.T("ui.edition"),small);
            if(Button(1160,29,67,paused?Loc.T("ui.resume"):Loc.T("ui.pause")))TogglePause();
            if(Button(1234,29,67,Loc.T("ui.speed",speed.ToString("0"))))CycleSpeed();
            if(Button(1308,29,67,Loc.T("ui.save")))SaveGame();
            if(Button(1382,29,67,Loc.T("ui.load")))
            {
                LoadGame();
            }
            if(Button(1456,29,117,Loc.T("ui.new_life")))NewLife();
            Box(1282,106,290,692,new Color(.035f,.062f,.075f,.95f));
            Text(1302,123,245,22,Loc.T("ui.witnesses"),subtitle);
            for(int i=0;i<4;i++) AgentCard(w.agents[i],i,1300,157+i*113);
            Text(1302,624,245,22,Loc.T("ui.controls"),subtitle);
            Text(1302,653,245,70,Loc.T("ui.controls_help"),small);
            if(tower!=null)DrawGroupSidebar();else {Text(1302,730,245,30,w.run==1&&w.showcase?Loc.T("ui.showcase"):Loc.T("ui.autonomous"),small);Text(1302,771,245,44,Loc.T("ui.next_classes"),small);}
            if(w.phase==Phase.Battle)
            {
                Box(425,110,590,78,new Color(.035f,.062f,.075f,.85f));Text(448,122,360,24,tower!=null?Loc.T(tower.Data.Boss(tower.State.GroupOf(selected).boss.definition).nameKey):Loc.T("ui.boss"),subtitle);
                Text(843,122,160,24,Loc.T("ui.agent_class",(int)w.boss.hp,(int)w.boss.maxHp),small);Bar(448,155,542,w.boss.hp/w.boss.maxHp,amber,6);
                Text(448,164,530,24,tower!=null?BossStatus():w.boss.enraged?Loc.T("ui.boss_phase2"):Loc.T("ui.boss_phase1"),small);
                if(w.boss.telegraph) {Box(545,208,355,40,new Color(.3f,.09f,.05f,.86f));Text(563,218,320,24,tower!=null?Loc.T(tower.Data.Ability(tower.State.GroupOf(selected).boss.ability).nameKey):Loc.T("ui.slam",w.boss.windup.ToString("F1")),subtitle);}
            }
            else
            {
                Box(425,110,590,78,new Color(.035f,.062f,.075f,.9f));Text(448,122,535,24,w.phase==Phase.Ended?Loc.T("ui.epilogue"):tower==null?Loc.T("ui.refuge_title"):Loc.T("ui.refuge_title")+" · "+Loc.T(tower.Data.Rest(tower.State.GroupOf(selected).restId).AvailableCount(tower.State.GroupOf(selected))==0?"ui.rest_empty":"ui.rest_count",tower.Data.Rest(tower.State.GroupOf(selected).restId).AvailableCount(tower.State.GroupOf(selected))),subtitle);
                Text(448,149,535,27,w.phase==Phase.Ended?Loc.T("ui.restart",Loc.Outcome(w.outcome),Mathf.CeilToInt(8-w.restartTimer)):w.phaseClock>=(tower!=null?tower.Data.Rest(tower.State.GroupOf(selected).restId).collapseAfter:Simulation.RestLimit)?Loc.T("ui.collapse_active"):Loc.T("ui.collapse_timer",Mathf.Max(0,(tower!=null?tower.Data.Rest(tower.State.GroupOf(selected).restId).collapseAfter:Simulation.RestLimit)-w.phaseClock).ToString("F1")),label);
                Bar(448,178,542,1-w.phaseClock/(tower!=null?tower.Data.Rest(tower.State.GroupOf(selected).restId).collapseAfter:Simulation.RestLimit),amber,3);
            }
            // Floating nameplates track real 3D actors.
            var usedPlates=new System.Collections.Generic.List<Rect>();
            foreach(var agent in w.agents)
            {
                if(agent.escaped||(tower!=null&&!tower.State.GroupOf(selected).members.Contains(agent.id)))continue;var p=view.Screen(agent);if(p.z<0)continue;
                float x=(p.x-ox)/scale,y=(Screen.height-p.y-oy)/scale;
                if(x>300&&x<1250&&y>200&&y<650)
                {
                    Rect plate=new Rect(x-45,y,90,30);for(int attempt=0;attempt<4;attempt++){if(!usedPlates.Exists(r=>r.Overlaps(plate)))break;plate.y-=34;}usedPlates.Add(plate);y=plate.y;
                    Box(x-45,y,90,30,new Color(.03f,.06f,.07f,.8f));Text(x-40,y+2,85,20,agent.name+(agent.alive?"":" †"),small);Bar(x-40,y+24,80,agent.hp/agent.MaxHp,view.colors[agent.id],3);
                }
            }
            Box(28,107,294,272,new Color(.035f,.062f,.075f,.89f));Text(47,124,258,22,Loc.T("ui.mind"),subtitle);
            Text(47,153,235,30,Loc.T("ui.agent_class",a.name,Loc.Profession(a.profession)),label);Text(47,188,252,75,tower!=null?Loc.T("p2.goal",Loc.T("p2.goal."+tower.State.Plan(selected).decision.intent)):RenderForUI(a.intent.reason),label);
            Text(47,273,258,22,Loc.T("ui.traits1",Mathf.RoundToInt(a.personality.risk*100),Mathf.RoundToInt(a.personality.curiosity*100)),small);
            Text(47,301,258,22,Loc.T("ui.traits2",Mathf.RoundToInt(a.personality.empathy*100),Mathf.RoundToInt(a.personality.greed*100)),small);
            Text(47,326,258,32,Loc.T("ui.intent",Loc.Action(a.intent.kind)),subtitle);
            Box(28,394,294,251,new Color(.035f,.062f,.075f,.89f));Text(47,412,258,22,Loc.T("ui.build",w.run),subtitle);
            Text(47,442,258,25,string.IsNullOrEmpty(a.weapon.affix)?Loc.Item(a.weapon.id):Loc.T("p3.affix",Loc.Item(a.weapon.id),Loc.T("p3.affix."+a.weapon.affix)),label);
            Text(47,468,258,52,Loc.T("ui.quality",a.weapon.quality.ToString("F2"),Loc.Skill(a.weapon.infusion)),small);
            Text(47,523,258,24,Loc.T("ui.stats",a.stats.str,a.stats.dex,a.stats.intel,a.stats.vit),small);
            Text(47,552,258,50,Loc.T("ui.skills",string.Join(" / ",a.equipped.ConvertAll(Loc.Skill))),small);
            Text(47,609,258,22,Loc.T("ui.resources",a.materials,a.inventory.Count,tower!=null?tower.State.Memory(a.id).entries.Count:a.memory.Count),small);
            Bottom(w);
            DrawSkillTooltip(a);
            Text(32,861,1230,29,Loc.T(tower!=null?"p2.footer":"ui.footer"),small);
            if(Button(1350,843,222,Loc.T("ui.language"))){Loc.SetLocale(Loc.Locale=="zh-TW"?"en":"zh-TW");PlayerPrefs.SetString("locale",Loc.Locale);PlayerPrefs.Save();}
            if(noticeTimer>0){Box(450,830,750,33,new Color(.08f,.16f,.17f,.95f));Text(467,836,715,25,RenderForUI(notice),label);}
        }
        void AgentCard(Agent a,int i,float x,float y)
        {
            bool active=selected==i;Box(x,y,254,104,active?new Color(.105f,.17f,.19f):new Color(.065f,.105f,.12f));Box(x,y,3,104,view.colors[i]);
            if(GUI.Button(new Rect(x,y,254,101),GUIContent.none,GUIStyle.none))selected=i;
            Text(x+15,y+10,163,24,a.name,label);Text(x+181,y+11,62,20,a.alive?(a.escaped?Loc.T("ui.safe"):Loc.T("ui.level",a.level)):Loc.T("ui.dead"),small);
            Text(x+15,y+34,220,20,tower!=null?Loc.T("p2.membership",tower.State.GroupOf(i).id,tower.State.GroupOf(i).floor):Loc.T("ui.agent_class",Loc.Profession(a.profession),a.escaped?Loc.T("ui.escaped"):a.taskTimer>0?Loc.T(a.task=="cache"?"ui.exploring":"ui.working",a.taskTimer.ToString("F1")):Loc.Action(a.intent.kind)),small);
            Bar(x+15,y+61,220,a.hp/a.MaxHp,a.alive?view.colors[i]:muted,5);Bar(x+15,y+72,220,a.mp/a.MaxMp,new Color(.33f,.56f,.75f),3);
            Text(x+15,y+80,220,22,Loc.T("ui.vitals",(int)a.hp,(int)a.mp),small);
        }
        void Bottom(World w)
        {
            Box(28,659,1229,182,new Color(.035f,.062f,.075f,.95f));
            string[] tabs={Loc.T("ui.chronicle"),Loc.T("ui.book"),Loc.T("ui.history"),Loc.T("ui.relationships")};
            for(int i=0;i<4;i++) {if(Button(44+i*183,674,173,(tab==i?"• ":"")+tabs[i]))tab=i;}
            if(tower!=null){if(Button(776,674,173,(tab==4?"• ":"")+Loc.T("p2.groups")))tab=4;if(Button(959,674,173,(tab==5?"• ":"")+Loc.T("p2.details")))tab=5;}else Text(980,680,260,20,Loc.T("ui.observe"),small);
            if(tower!=null&&tab>=4){DrawExpeditionDetail();return;}
            if(tab==0)
            {
                int start=Mathf.Max(0,w.messages.Count-4);
                for(int i=start;i<w.messages.Count;i++)
                {
                    var m=w.messages[i];float y=714+(i-start)*27;Text(46,y,55,22,TimeText(m.time),small);
                    Text(109,y,83,24,m.speaker<0?Loc.T("ui.tower"):w.agents[m.speaker].name,subtitle);Text(199,y,1022,26,RenderForUI(m.text),label);
                }
            }
            else if(tab==1)
            {
                int start=Mathf.Max(0,w.book.Count-4);if(w.book.Count==0)Text(46,721,1100,55,Loc.T("ui.book_empty"),label);
                for(int i=start;i<w.book.Count;i++) {var e=w.book[i];Text(46,714+(i-start)*27,150,24,Loc.T("ui.book_author",e.run,w.agents[e.author].name),subtitle);Text(211,714+(i-start)*27,1000,24,RenderForUI(e.text),label);}
            }
            else if(tab==2)
            {
                int start=Mathf.Max(0,w.history.Count-4);if(w.history.Count==0)Text(46,721,1100,55,Loc.T("ui.history_empty"),label);
                for(int i=start;i<w.history.Count;i++) {var r=w.history[i];Text(46,714+(i-start)*27,1130,24,Loc.T("ui.history_row",r.run,Loc.Outcome(r.outcome),TimeText(r.seconds),r.survivors.Count,(int)r.damage,(int)r.healing),label);}
            }
            else
            {
                var a=w.agents[selected];int row=0;
                foreach(var b in a.bonds){Text(46,714+row*30,1140,25,Loc.T("ui.bond_row",a.name,w.agents[b.target].name,b.trust.ToString("F2"),b.friendship.ToString("F2"),b.love.ToString("F2"),b.fear.ToString("F2"),b.resentment.ToString("F2")),label);row++;}
            }
        }
    }
}
