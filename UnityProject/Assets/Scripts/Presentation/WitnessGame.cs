using System;
using System.IO;
using Ember.Core;
using UnityEngine;

namespace Ember.Presentation
{
    public sealed class WitnessGame : MonoBehaviour
    {
        Simulation simulation; WorldView view; float accumulator, speed=1; bool paused, smoke, collapseSmoke;
        bool shotBattle,shotRest,shotEnd; int selected, tab, lastRun, lastOutcome;
        string notice="", savePath, artifactPath, weaponSignature=""; float noticeTimer;
        Message lastSound;
        GUIStyle title,subtitle,label,small,number,button; Texture2D white;
        Color ink=new Color(.94f,.95f,.93f),muted=new Color(.73f,.79f,.8f),amber=new Color(.98f,.77f,.48f);
        AudioSource audioSource; AudioClip strike,heal;
        void Start()
        {
            Application.targetFrameRate=60; var c=JsonUtility.FromJson<Catalog>(Resources.Load<TextAsset>("catalog").text);
            var args=Environment.GetCommandLineArgs();smoke=Array.IndexOf(args,"--smoke")>=0;collapseSmoke=Array.IndexOf(args,"--collapse-smoke")>=0;
            simulation=new Simulation(c,1729,smoke||Array.IndexOf(args,"--showcase")>=0);view=new WorldView();view.RebuildCharacters(simulation.State,c);lastRun=1;
            savePath=Path.Combine(Application.persistentDataPath,"witness-save.json");
            int index=Array.IndexOf(args,"--artifacts");artifactPath=index>=0&&index+1<args.Length?args[index+1]:Application.persistentDataPath;
            if(smoke||collapseSmoke){Directory.CreateDirectory(artifactPath);speed=2;savePath=Path.Combine(artifactPath,"smoke-save.json");}
            if(collapseSmoke)
            {
                simulation.EnterRest();simulation.State.phaseClock=27;
                foreach(var a in simulation.State.agents){a.x=-2+a.id*1.2f;a.z=-3+a.id*2;}
            }
            white=Texture2D.whiteTexture;audioSource=gameObject.AddComponent<AudioSource>();audioSource.volume=.08f;strike=Tone(150,.12f);heal=Tone(460,.22f);
            Debug.Log("EMBER PLAYER START / save "+savePath);
        }
        AudioClip Tone(float frequency,float duration)
        {
            int n=(int)(44100*duration);var samples=new float[n];for(int i=0;i<n;i++) samples[i]=Mathf.Sin(i*frequency*Mathf.PI*2/44100)*Mathf.Exp(-i/(44100*duration*.2f))*.6f;
            var clip=AudioClip.Create("Ember feedback",n,1,44100,false);clip.SetData(samples,0);return clip;
        }
        void Update()
        {
            if(simulation==null)return;
            if(Input.GetKeyDown(KeyCode.Space))paused=!paused;
            if(Input.GetKeyDown(KeyCode.Alpha1))speed=1;if(Input.GetKeyDown(KeyCode.Alpha2))speed=2;if(Input.GetKeyDown(KeyCode.Alpha3))speed=4;
            if(!paused)
            {
                accumulator+=Mathf.Min(Time.unscaledDeltaTime,.25f)*speed;
                while(accumulator>=Simulation.StepSeconds){simulation.Step();accumulator-=Simulation.StepSeconds;}
            }
            var w=simulation.State;
            string signature="";foreach(var agent in w.agents)signature+=agent.weapon.id+agent.weapon.infusion+";";
            if(signature!=weaponSignature){weaponSignature=signature;view.RebuildCharacters(w,simulation.Catalog);}
            if(w.run!=lastRun) {lastRun=w.run;view.RebuildCharacters(w,simulation.Catalog);TrySave();}
            if((int)w.outcome!=lastOutcome) {lastOutcome=(int)w.outcome;if(w.outcome!=Outcome.None)TrySave();}
            float orbit=(Input.GetKey(KeyCode.RightArrow)?1:0)-(Input.GetKey(KeyCode.LeftArrow)?1:0);
            view.Update(w,Time.unscaledDeltaTime,orbit);noticeTimer-=Time.unscaledDeltaTime;
            if(w.messages.Count>0&&w.messages[w.messages.Count-1]!=lastSound)
            {
                var msg=w.messages[w.messages.Count-1];lastSound=msg;if(msg.category=="skill")audioSource.PlayOneShot(strike);if(msg.category=="heal")audioSource.PlayOneShot(heal);
            }
            if(smoke)
            {
                if(!shotBattle&&w.phase==Phase.Battle&&w.clock>=12){shotBattle=true;ScreenCapture.CaptureScreenshot(Path.Combine(artifactPath,"battle.png"));}
                if(!shotRest&&w.phase==Phase.Rest&&w.phaseClock>=7){shotRest=true;ScreenCapture.CaptureScreenshot(Path.Combine(artifactPath,"refuge.png"));}
                if(!shotEnd&&w.phase==Phase.Ended){shotEnd=true;ScreenCapture.CaptureScreenshot(Path.Combine(artifactPath,"end.png"));File.WriteAllText(Path.Combine(artifactPath,"player-run.json"),JsonUtility.ToJson(w,true));}
                if(w.run>=2&&w.clock>=2) {Debug.Log("EMBER PLAYER SMOKE PASSED / run ended and restarted");Application.Quit(shotBattle&&shotRest&&shotEnd?0:2);}
                if(Time.realtimeSinceStartup>150) {Debug.LogError("EMBER PLAYER SMOKE TIMEOUT");Application.Quit(3);}
            }
            if(collapseSmoke)
            {
                if(!shotRest&&Time.realtimeSinceStartup>1.5f){shotRest=true;ScreenCapture.CaptureScreenshot(Path.Combine(artifactPath,"collapse.png"));}
                if(Time.realtimeSinceStartup>3){Debug.Log("EMBER COLLAPSE PRESENTATION CHECK COMPLETE");Application.Quit(0);}
            }
        }
        void TrySave(){try{SaveStore.Save(savePath,simulation.State);}catch(Exception e){Debug.LogException(e);Notice("Save failed: "+e.Message);}}
        void Notice(string text){notice=text;noticeTimer=5;}
        void Styles()
        {
            if(title!=null)return;
            title=new GUIStyle(GUI.skin.label){fontSize=33,fontStyle=FontStyle.Bold,normal={textColor=ink}};
            subtitle=new GUIStyle(GUI.skin.label){fontSize=11,fontStyle=FontStyle.Bold,normal={textColor=amber}};
            label=new GUIStyle(GUI.skin.label){fontSize=14,normal={textColor=ink},wordWrap=true};
            small=new GUIStyle(label){fontSize=11,normal={textColor=muted}};
            number=new GUIStyle(label){fontSize=27,fontStyle=FontStyle.Bold};
            button=new GUIStyle{fontSize=12,alignment=TextAnchor.MiddleCenter,normal={textColor=ink},hover={textColor=amber},active={textColor=amber},padding=new RectOffset(8,8,5,5)};
        }
        void Box(float x,float y,float width,float height,Color c){GUI.color=c;GUI.DrawTexture(new Rect(x,y,width,height),white);GUI.color=Color.white;}
        void Text(float x,float y,float width,float height,string text,GUIStyle style=null){GUI.Label(new Rect(x,y,width,height),text,style??label);}
        void Bar(float x,float y,float width,float ratio,Color color,float height=4){Box(x,y,width,height,new Color(.14f,.2f,.21f));Box(x,y,width*Mathf.Clamp01(ratio),height,color);}
        bool Button(float x,float y,float width,string text){Box(x,y,width,27,new Color(.13f,.21f,.23f,.95f));return GUI.Button(new Rect(x,y,width,27),text,button);}
        string TimeText(float seconds) => ((int)seconds/60).ToString("00")+":"+((int)seconds%60).ToString("00");
        void OnGUI()
        {
            if(simulation==null)return;Styles();float scale=Mathf.Min(Screen.width/1600f,Screen.height/900f);float ox=(Screen.width-1600*scale)/2,oy=(Screen.height-900*scale)/2;
            GUI.matrix=Matrix4x4.TRS(new Vector3(ox,oy,0),Quaternion.identity,new Vector3(scale,scale,1));
            var w=simulation.State;var a=w.agents[selected];
            Box(0,0,1600,86,new Color(.035f,.065f,.08f,.96f));Box(0,85,1600,1,new Color(.25f,.36f,.36f,.6f));
            Text(30,17,215,43,"E M B E R",title);Text(32,57,260,20,"THE WITNESS TOWER  /  AGENT RPG",small);
            Text(337,21,120,20,"CURRENT LIFE",subtitle);Text(338,44,120,32,w.run.ToString("D3"),number);
            Text(490,21,120,20,"FLOOR",subtitle);Text(491,44,120,32,"01 / 25",number);
            Text(636,21,120,20,"RUN TIME",subtitle);Text(637,44,120,32,TimeText(w.clock),number);
            Text(800,24,230,25,w.phase==Phase.Battle?"ROOTCROWN AMPHITHEATRE":w.phase==Phase.Rest?"ASHEN CROSSING":"LIFE RECORDED",label);
            Text(800,48,240,20,"VERTICAL SLICE  •  LOCAL UTILITY AI",small);
            if(Button(1165,29,78,paused?"RESUME":"PAUSE"))paused=!paused;
            if(Button(1250,29,60,speed.ToString("0")+"×"))speed=speed==1?2:speed==2?4:1;
            if(Button(1317,29,74,"SAVE")){TrySave();Notice("Saved locally.");}
            if(Button(1398,29,74,"LOAD"))
            {
                try {simulation.Restore(SaveStore.Load(savePath));lastRun=simulation.State.run;lastOutcome=(int)simulation.State.outcome;view.RebuildCharacters(simulation.State,simulation.Catalog);accumulator=0;Notice("Restored this life.");}catch(Exception e){Notice("Load: "+e.Message);}
            }
            if(Button(1479,29,94,"NEW LIFE")){simulation.Finish(Outcome.Wipe);simulation.State.run++;simulation.Begin();}
            Box(1282,106,290,692,new Color(.035f,.062f,.075f,.95f));
            Text(1302,123,245,22,"THE FOUR WITNESSES",subtitle);
            for(int i=0;i<4;i++) AgentCard(w.agents[i],i,1300,157+i*113);
            Text(1302,624,245,22,"OBSERVATION CONTROLS",subtitle);
            Text(1302,653,245,70,"SPACE  Pause / resume\n1 · 2 · 3  Playback speed\n← →  Orbit the arena",small);
            Text(1302,735,245,30,w.run==1&&w.showcase?"CLASS CHOICE / SHOWCASE":"CLASS CHOICE / AUTONOMOUS",small);
            Text(1302,771,245,22,"Future lives choose classes freely.",small);
            if(w.phase==Phase.Battle)
            {
                Box(425,110,590,78,new Color(.035f,.062f,.075f,.85f));Text(448,122,360,24,"THE ROOTCROWN",subtitle);
                Text(867,122,130,24,((int)w.boss.hp)+" / "+((int)w.boss.maxHp),small);Bar(448,155,542,w.boss.hp/w.boss.maxHp,amber,6);
                Text(448,167,530,17,w.boss.enraged?"PHASE II  /  THE ROOTS REMEMBER":"PHASE I  /  STONE AND THORN",small);
                if(w.boss.telegraph) {Box(545,208,355,40,new Color(.3f,.09f,.05f,.86f));Text(563,218,320,24,"GROUND BREAK  /  "+w.boss.windup.ToString("F1")+"s",subtitle);}
            }
            else
            {
                Box(425,110,590,78,new Color(.035f,.062f,.075f,.9f));Text(448,122,380,24,w.phase==Phase.Ended?"THE BOOK RECEIVES YOUR STORY":"REFUGE / THE ASHEN CROSSING",subtitle);
                Text(448,149,535,27,w.phase==Phase.Ended?w.outcome+" · New life in "+Mathf.CeilToInt(8-w.restartTimer)+"s":w.phaseClock>=Simulation.RestLimit?"COLLAPSE ADVANCING / REACH THE EASTERN GATE":"COLLAPSE IN "+Mathf.Max(0,Simulation.RestLimit-w.phaseClock).ToString("F1")+"s   /   THE GATE IS TO THE EAST",label);
                Bar(448,178,542,1-w.phaseClock/Simulation.RestLimit,amber,3);
            }
            // Floating nameplates track real 3D actors.
            var usedPlates=new System.Collections.Generic.List<Rect>();
            foreach(var agent in w.agents)
            {
                if(agent.escaped)continue;var p=view.Screen(agent);if(p.z<0)continue;
                float x=(p.x-ox)/scale,y=(Screen.height-p.y-oy)/scale;
                if(x>300&&x<1250&&y>200&&y<650)
                {
                    Rect plate=new Rect(x-45,y,90,30);for(int attempt=0;attempt<4;attempt++){if(!usedPlates.Exists(r=>r.Overlaps(plate)))break;plate.y-=34;}usedPlates.Add(plate);y=plate.y;
                    Box(x-45,y,90,30,new Color(.03f,.06f,.07f,.8f));Text(x-40,y+2,85,20,agent.name+(agent.alive?"":" †"),small);Bar(x-40,y+24,80,agent.hp/agent.MaxHp,view.colors[agent.id],3);
                }
            }
            Box(28,107,294,272,new Color(.035f,.062f,.075f,.89f));Text(47,124,258,22,"INSIDE THE MIND",subtitle);
            Text(47,153,235,30,a.name+" / "+a.profession,label);Text(47,190,252,67,a.intent.reason,label);
            Text(47,266,258,20,"RISK    "+Mathf.RoundToInt(a.personality.risk*100)+"    CURIOSITY    "+Mathf.RoundToInt(a.personality.curiosity*100),small);
            Text(47,290,258,20,"EMPATHY    "+Mathf.RoundToInt(a.personality.empathy*100)+"    GREED    "+Mathf.RoundToInt(a.personality.greed*100),small);
            Text(47,326,258,32,"INTENT  /  "+a.intent.kind.ToString().ToUpper(),subtitle);
            Box(28,394,294,239,new Color(.035f,.062f,.075f,.89f));Text(47,412,258,22,"BUILD / LIFE "+w.run,subtitle);
            Text(47,442,258,25,simulation.Catalog.Item(a.weapon.id).name,label);
            Text(47,468,258,25,"QUALITY "+a.weapon.quality.ToString("F2")+"  /  INFUSION "+(a.weapon.infusion==""?"—":a.weapon.infusion.ToUpper()),small);
            Text(47,503,258,25,"STR "+a.stats.str+"   DEX "+a.stats.dex+"   INT "+a.stats.intel+"   VIT "+a.stats.vit,small);
            Text(47,530,258,45,"SKILLS  "+string.Join(" / ",a.equipped).ToUpper(),small);
            Text(47,587,258,25,"MATERIALS "+a.materials+"   ITEMS "+a.inventory.Count+"   MEMORIES "+a.memory.Count,small);
            Bottom(w);
            Text(32,861,800,25,"AUTONOMOUS LIVES. FINITE MEMORIES.  /  ONE GUARDIAN, THEN THE DANGEROUS REFUGE.",small);
            if(noticeTimer>0){Box(450,830,750,33,new Color(.08f,.16f,.17f,.95f));Text(467,836,715,25,notice,label);}
        }
        void AgentCard(Agent a,int i,float x,float y)
        {
            bool active=selected==i;Box(x,y,254,101,active?new Color(.105f,.17f,.19f):new Color(.065f,.105f,.12f));Box(x,y,3,101,view.colors[i]);
            if(GUI.Button(new Rect(x,y,254,101),GUIContent.none,GUIStyle.none))selected=i;
            Text(x+15,y+10,163,24,a.name,label);Text(x+181,y+11,62,20,a.alive?(a.escaped?"SAFE":"LV "+a.level):"DEAD",small);
            Text(x+15,y+34,220,20,a.profession+" / "+(a.escaped?"Escaped":a.taskTimer>0?"Working "+a.taskTimer.ToString("F1")+"s":a.intent.kind.ToString()),small);
            Bar(x+15,y+61,220,a.hp/a.MaxHp,a.alive?view.colors[i]:muted,5);Bar(x+15,y+72,220,a.mp/a.MaxMp,new Color(.33f,.56f,.75f),3);
            Text(x+15,y+80,220,17,((int)a.hp)+" HP  /  "+((int)a.mp)+" MANA",small);
        }
        void Bottom(World w)
        {
            Box(28,659,1229,182,new Color(.035f,.062f,.075f,.95f));
            string[] tabs={"LIVE CHRONICLE","BOOK OF THE DEAD","RUN HISTORY","RELATIONSHIPS"};
            for(int i=0;i<4;i++) {if(Button(44+i*183,674,173,(tab==i?"• ":"")+tabs[i]))tab=i;}
            Text(980,680,260,20,"OBSERVE · REMEMBER · REPEAT",small);
            if(tab==0)
            {
                int start=Mathf.Max(0,w.messages.Count-4);
                for(int i=start;i<w.messages.Count;i++)
                {
                    var m=w.messages[i];float y=714+(i-start)*27;Text(46,y,55,22,TimeText(m.time),small);
                    Text(109,y,83,22,m.speaker<0?"TOWER":w.agents[m.speaker].name,subtitle);Text(199,y,1022,24,m.text,label);
                }
            }
            else if(tab==1)
            {
                int start=Mathf.Max(0,w.book.Count-4);if(w.book.Count==0)Text(46,721,1100,55,"No former lives have written here yet. Death will leave a small piece of the story.",label);
                for(int i=start;i<w.book.Count;i++) {var e=w.book[i];Text(46,714+(i-start)*27,150,24,"LIFE "+e.run+" / "+w.agents[e.author].name,subtitle);Text(211,714+(i-start)*27,1000,24,e.text,label);}
            }
            else if(tab==2)
            {
                int start=Mathf.Max(0,w.history.Count-4);if(w.history.Count==0)Text(46,721,1100,55,"The first life is still unfolding. Slice completion is recorded separately from a full 25-floor clear.",label);
                for(int i=start;i<w.history.Count;i++) {var r=w.history[i];Text(46,714+(i-start)*27,1130,24,"LIFE "+r.run+"   "+r.outcome+"   "+TimeText(r.seconds)+"   SURVIVORS "+r.survivors.Count+"   DAMAGE "+((int)r.damage)+"   HEALING "+((int)r.healing),label);}
            }
            else
            {
                var a=w.agents[selected];int row=0;
                foreach(var b in a.bonds){Text(46,714+row*30,1140,25,a.name+" → "+w.agents[b.target].name+"   Trust "+b.trust.ToString("F2")+"   Friendship "+b.friendship.ToString("F2")+"   Love "+b.love.ToString("F2")+"   Fear "+b.fear.ToString("F2")+"   Resentment "+b.resentment.ToString("F2"),label);row++;}
            }
        }
    }
}
