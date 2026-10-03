using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using Ember.Core;
using Ember.Core.Phase2;
using Unity.Profiling;
using Unity.Profiling.LowLevel.Unsafe;
using UnityEngine;
using UnityEngine.Profiling;

namespace Ember.Presentation
{
    // Available only through command-line diagnostics; no network profiler is required.
    public sealed class PerformanceSession : MonoBehaviour
    {
        readonly List<ProfilerRecorder> counters=new List<ProfilerRecorder>();readonly List<string> names=new List<string>();
        StreamWriter writer;WitnessGame game;float started,next,maintenance,checkpoint;int seconds,rows;string folder,mode;StreamWriter events;float inventoryNext;int objects,meshes,materials,particles,audio;int priorRun,priorFloor,priorPhase,priorGroups,priorDeaths,priorFx;bool draining;
        long previousTimestamp;double previousRealtime;
        readonly FrameTiming[] frames=new FrameTiming[1];
        public void Initialize(WitnessGame owner,string path,int duration,string workload)
        {
            game=owner;folder=path;seconds=duration;mode=workload;Directory.CreateDirectory(path);events=new StreamWriter(Path.Combine(path,"events.csv"));events.WriteLine("elapsed,utc,event,detail");Directory.CreateDirectory(path);started=Time.realtimeSinceStartup;maintenance=started+60;checkpoint=started+60;
            foreach(var pair in new[]{(ProfilerCategory.Internal,"CPU Main Thread Frame Time"),(ProfilerCategory.Render,"GPU Frame Time"),(ProfilerCategory.Memory,"GC Allocated In Frame"),(ProfilerCategory.Memory,"GC Used Memory"),(ProfilerCategory.Memory,"GC Reserved Memory"),(ProfilerCategory.Memory,"Total Used Memory"),(ProfilerCategory.Render,"Draw Calls Count"),(ProfilerCategory.Render,"Batches Count"),(ProfilerCategory.Scripts,"Ember.HighDecision"),(ProfilerCategory.Scripts,"Ember.UI")})
            {names.Add(pair.Item2);counters.Add(ProfilerRecorder.StartNew(pair.Item1,pair.Item2,1));}
            writer=new StreamWriter(Path.Combine(path,"frames.csv"));writer.WriteLine("elapsed,run,floor,groups,frame_ms,unity_delta_ms,realtime_gap_ms,cpu_ns,gpu_ns,gc_alloc,gc_used,gc_reserved,total_used,draw_calls,batches,decision_ns,ui_ns,gpu_timing_ms,managed_heap,objects,meshes,materials,particles,audio_sources,simulation_ms,save_ms,telemetry_ms,ui_alloc,step_alloc,view_alloc,render_alloc,ui_ms,step_scope_ms,view_ms,localization_ms");
            var available=new List<ProfilerRecorderHandle>();ProfilerRecorderHandle.GetAvailable(available);var lines=new List<string>();
            foreach(var h in available){var d=ProfilerRecorderHandle.GetDescription(h);if(d.Name.Contains("Job")||d.Name.Contains("Temp")||d.Name.Contains("GPU"))lines.Add(d.Name+" / "+d.Category);}
            File.WriteAllText(Path.Combine(path,"counter-availability.txt"),string.Join("\n",lines));
            File.WriteAllText(Path.Combine(path,"session.txt"),"mode="+mode+"\ndetailedDiagnostics="+game.ProfileDetailed+"\n"+"development="+Debug.isDebugBuild+"\nGPU="+SystemInfo.graphicsDeviceName+"\nAPI="+SystemInfo.graphicsDeviceType+"\nseconds="+seconds+"\ntimestampAlignment=elapsed realtime + startedUTC\nstartedUTC="+DateTime.UtcNow.ToString("O"));
        }
        void LateUpdate()
        {
            if(writer==null)return;float now=Time.realtimeSinceStartup;long stamp=System.Diagnostics.Stopwatch.GetTimestamp();double realtime=Time.realtimeSinceStartupAsDouble;double wallMs=previousTimestamp==0?0:(stamp-previousTimestamp)*1000d/System.Diagnostics.Stopwatch.Frequency,realtimeMs=previousRealtime==0?0:(realtime-previousRealtime)*1000;previousTimestamp=stamp;previousRealtime=realtime;FrameTimingManager.CaptureFrameTimings();
            // Keep frame/counter sampling during cleanup, but never load a scene
            // after ProfileStop has destroyed its character transforms.
            if(!draining&&now-started<seconds)
            {
                if((mode!="baseline"||game.NativeSaveLoad)&&now>=maintenance){game.ProfileMaintenance((int)((now-started)/60));maintenance=now+60;}
                game.ProfileNaturalActions(now-started);
            }
            var state=game.ProfileState;var observed=state.GroupOf(game.ProfileSelected);
            if(state.world.run!=priorRun){Mark("restart",state.world.run.ToString());priorRun=state.world.run;}
            if(observed.floor!=priorFloor){Mark("floor",observed.floor.ToString());priorFloor=observed.floor;}
            if(observed.boss.phase!=priorPhase){Mark("boss_phase",observed.boss.phase.ToString());priorPhase=observed.boss.phase;}
            if(state.groups.Count!=priorGroups){Mark("groups",state.groups.Count.ToString());priorGroups=state.groups.Count;}
            int deaths=0;foreach(var a in state.world.agents)if(!a.alive)deaths++;if(deaths!=priorDeaths){Mark("death_count",deaths.ToString());priorDeaths=deaths;}if(game.ProfileVfxSerial!=priorFx){Mark("vfx_burst",game.ProfileVfxSerial.ToString());priorFx=game.ProfileVfxSerial;}
            if(game.ProfileDetailed&&now>=inventoryNext){inventoryNext=now+5;objects=Resources.FindObjectsOfTypeAll<GameObject>().Length;meshes=Resources.FindObjectsOfTypeAll<Mesh>().Length;materials=Resources.FindObjectsOfTypeAll<Material>().Length;audio=Resources.FindObjectsOfTypeAll<AudioSource>().Length;particles=0;foreach(var ps in Resources.FindObjectsOfTypeAll<ParticleSystem>())particles+=ps.particleCount;Mark("inventory",objects.ToString());}
            if(now>=next){next=now+5;writer.Flush();events.Flush();File.WriteAllText(Path.Combine(folder,"heartbeat.txt"),"elapsed="+(now-started)+";frames="+rows+";utc="+DateTime.UtcNow.ToString("O"));}
            {FrameTimingManager.GetLatestTimings(1,frames);var s=game.ProfileState;
                var values=new List<string>{(now-started).ToString("F3",CultureInfo.InvariantCulture),s.world.run.ToString(),s.world.floor.ToString(),s.groups.Count.ToString(),wallMs.ToString("F6",CultureInfo.InvariantCulture),(Time.unscaledDeltaTime*1000).ToString("F3",CultureInfo.InvariantCulture),realtimeMs.ToString("F6",CultureInfo.InvariantCulture)};
                foreach(var r in counters)values.Add(r.Valid?r.LastValue.ToString():"NA");
                values.Add(frames[0].gpuFrameTime>0?frames[0].gpuFrameTime.ToString("F3",CultureInfo.InvariantCulture):"NA");
                values.Add(Profiler.GetMonoUsedSizeLong().ToString());values.Add(objects.ToString());values.Add(meshes.ToString());values.Add(materials.ToString());values.Add(particles.ToString());values.Add(audio.ToString());
                values.Add(game.profileAiMs.ToString("F4",CultureInfo.InvariantCulture));values.Add(game.profileSaveMs.ToString("F4",CultureInfo.InvariantCulture));values.Add(game.profileTelemetryMs.ToString("F4",CultureInfo.InvariantCulture));values.Add(game.ProfileAllocationCounterValid&&game.ProfileDetailed?game.ProfileUiAlloc.ToString():"NA");values.Add(game.ProfileAllocationCounterValid&&game.ProfileDetailed?game.ProfileStepAlloc.ToString():"NA");values.Add(game.ProfileAllocationCounterValid&&game.ProfileDetailed?game.ProfileViewAlloc.ToString():"NA");values.Add(game.ProfileAllocationCounterValid&&game.ProfileDetailed?game.ProfileRenderAlloc.ToString():"NA");foreach(double cost in new[]{game.ProfileUiMs,game.ProfileStepMs,game.ProfileViewMs,game.ProfileRenderMs})values.Add(game.ProfileDetailed?cost.ToString("F6",CultureInfo.InvariantCulture):"NA");writer.WriteLine(string.Join(",",values));rows++;
            }
            if(now>=checkpoint){File.WriteAllText(Path.Combine(folder,"checkpoint-"+(int)((now-started)/60)+"m.json"),JsonUtility.ToJson(game.ProfileState,true));checkpoint=now+60;}
            if(now-started>=seconds&&!draining){draining=true;Mark("scene_cleanup_begin");game.ProfileStop();StartCoroutine(QuitAfterCleanup());}
        }
        IEnumerator QuitAfterCleanup(){yield return new WaitForSecondsRealtime(1);Mark("scene_cleanup_end");File.WriteAllText(Path.Combine(folder,"complete.txt"),"elapsed="+(Time.realtimeSinceStartup-started)+"; rows="+rows+"; runs="+game.ProfileState.world.run+"; maintenance="+game.profileMaintenances);ReleaseCounters();writer?.Dispose();writer=null;events?.Dispose();events=null;Application.Quit(0);}
        public void Mark(string kind,string detail=""){events?.WriteLine((Time.realtimeSinceStartup-started).ToString("F6",CultureInfo.InvariantCulture)+","+DateTime.UtcNow.ToString("O")+","+kind+","+detail);}
        void ReleaseCounters(){foreach(var r in counters)r.Dispose();counters.Clear();}
        void OnDisable(){ReleaseCounters();writer?.Dispose();writer=null;events?.Dispose();events=null;}
        void OnDestroy(){OnDisable();}
    }
    public sealed partial class WitnessGame
    {
        string profileMode="extreme";PerformanceSession profileSession;bool profile,profileDetailed=true,profileLocaleSwitch,profileSplitDone;int profileLocaleStage;public double profileAiMs,profileSaveMs,profileTelemetryMs;public int profileMaintenances;float profileManualUntil;
        public int ProfileVfxSerial=>view.EffectSerial;
        public ExpeditionState ProfileState=>tower.State;public int ProfileSelected=>selected;
        void StartProfile(string[] args)
        {
            int i=Array.IndexOf(args,"--profile-seconds");if(i<0)return;profile=true;int duration=int.Parse(args[i+1]);if(duration<1||duration>1800)throw new ArgumentOutOfRangeException("profile duration must be 1..1800 seconds");int m=Array.IndexOf(args,"--profile-mode");profileMode=m>=0?args[m+1]:"extreme";if(profileMode!="baseline"&&profileMode!="gameplay"&&profileMode!="extreme")throw new ArgumentException("Unknown profile mode");speed=profileMode=="extreme"?16:1;Directory.CreateDirectory(artifactPath);
            profileDetailed=Array.IndexOf(args,"--diagnostics-off")<0;profileLocaleSwitch=Array.IndexOf(args,"--locale-switch-smoke")>=0;CalibrateAllocationCounter();
            savePath=Path.Combine(artifactPath,"soak-save.json");tower.TelemetryEnabled=profileMode=="extreme";tower.TelemetrySeed=1729;
            profileSession=gameObject.AddComponent<PerformanceSession>();profileSession.Initialize(this,artifactPath,duration,profileMode);
        }
        public void ProfileNaturalActions(float elapsed)
        {
            if(profileLocaleSwitch&&profileLocaleStage<2&&elapsed>=30+profileLocaleStage*30){Loc.SetLocale(Loc.Locale=="zh-TW"?"en":"zh-TW");profileLocaleStage++;profileSession.Mark("locale_switch",Loc.Locale);StartCoroutine(CaptureLocaleAfterRender());}
            if(profileMode=="gameplay"&&!profileSplitDone){var g=tower.State.groups.Find(v=>v.phase==Phase.Rest&&!v.terminal&&v.members.Count>1);if(g!=null){profileSplitDone=tower.Split(g.id,new[]{g.members[0]})!=null;profileSession.Mark("split_natural",profileSplitDone.ToString());}}
        }
        System.Collections.IEnumerator CaptureLocaleAfterRender(){yield return new WaitForEndOfFrame();ScreenCapture.CaptureScreenshot(Path.Combine(artifactPath,"gameplay-switch-"+Loc.Locale+".png"));}
        public void ProfileMaintenance(int minute)
        {
            profileSession.Mark("save_begin");var watch=System.Diagnostics.Stopwatch.StartNew();SaveGame();profileSession.Mark("save_end");profileSession.Mark("load_begin");LoadGame();profileSession.Mark("load_end");profileSaveMs=watch.Elapsed.TotalMilliseconds;profileMaintenances++;
            if(profileMode=="baseline")return;
            selected=(selected+1)%4;
            if(profileMode=="extreme"&&minute%5==0&&tower.State.world.phase!=Phase.Ended)
            {
                // Deliberate fault scenarios supplement natural runs; this is a stress workload, not balance telemetry.
                var g=tower.State.GroupOf(selected);if(g.phase==Phase.Rest){g.phaseClock=55;foreach(var a in tower.State.Members(g))a.escaped=false;}
                else {var a=tower.State.world.agents[selected];if(a.alive)tower.Die(a,Loc.Token("p3.cause.pressure"));}
            }
            if(profileMode=="gameplay"){var splitGroup=tower.State.groups.Find(g=>g.phase==Phase.Rest&&!g.terminal&&g.members.Count>1);if(splitGroup!=null){profileSession.Mark("split_attempt");tower.Split(splitGroup.id,new[]{splitGroup.members[0]});}return;}
            profileSession.Mark("telemetry_begin");var telemetryWatch=System.Diagnostics.Stopwatch.StartNew();File.WriteAllText(Path.Combine(artifactPath,"telemetry-latest.json"),JsonUtility.ToJson(tower.State));profileTelemetryMs=telemetryWatch.Elapsed.TotalMilliseconds;profileSession.Mark("telemetry_end");
        }
        public void ProfileStop(){paused=true;enabled=false;view.Dispose();audioSource.Stop();if(sound!=null)Destroy(sound);if(strike!=null)Destroy(strike);if(heal!=null)Destroy(heal);}
    }
    public sealed class NativeAllocationProbe : MonoBehaviour
    {
        public string mode;float end,nextHeartbeat;string output;StreamWriter nativeWarnings;ParticleSystem probeParticles;bool drained;
        void Start()
        {
            var args=Environment.GetCommandLineArgs();int durationIndex=Array.IndexOf(args,"--probe-seconds");int duration=durationIndex<0?8:int.Parse(args[durationIndex+1]);if(duration<1||duration>1800)throw new ArgumentOutOfRangeException("probe-seconds");end=Time.realtimeSinceStartup+duration;int outputIndex=Array.IndexOf(args,"--artifacts");output=outputIndex<0?Application.persistentDataPath:args[outputIndex+1];Directory.CreateDirectory(output);nativeWarnings=new StreamWriter(Path.Combine(output,"native-warnings.txt"));Application.logMessageReceived+=Warning;Application.SetStackTraceLogType(LogType.Warning,StackTraceLogType.Full);
            if(mode.Contains("camera")){var camera=new GameObject("Probe camera").AddComponent<Camera>();camera.transform.position=new Vector3(0,2,-10);camera.transform.LookAt(Vector3.zero);new GameObject("Probe light").AddComponent<Light>();}
            if(mode.Contains("mesh")){var g=GameObject.CreatePrimitive(PrimitiveType.Cube);g.GetComponent<Renderer>().sharedMaterial=new Material(Shader.Find("Standard"));}
            if(mode.Contains("particles")){probeParticles=new GameObject("Probe particle").AddComponent<ParticleSystem>();probeParticles.Play();}
            Debug.Log("EMBER NATIVE PROBE / "+mode);
        }
        void Warning(string condition,string stack,LogType type){if(condition.Contains("JobTempAlloc")){nativeWarnings.WriteLine(DateTime.UtcNow.ToString("O")+" / mode="+mode+" / frame="+Time.frameCount+" / "+condition+"\n"+stack);nativeWarnings.Flush();}}
        void OnDestroy(){Application.logMessageReceived-=Warning;nativeWarnings?.Dispose();}
        void Update(){if(Time.realtimeSinceStartup>=nextHeartbeat){nextHeartbeat=Time.realtimeSinceStartup+5;File.WriteAllText(Path.Combine(output,"heartbeat.txt"),"utc="+DateTime.UtcNow.ToString("O")+";frame="+Time.frameCount+";remaining="+(end-Time.realtimeSinceStartup));}if(mode.Contains("cleanup")&&!drained&&Time.realtimeSinceStartup>=end-2){drained=true;probeParticles.Stop(true,ParticleSystemStopBehavior.StopEmittingAndClear);Destroy(probeParticles.gameObject);}if(Time.realtimeSinceStartup>=end)Application.Quit(0);}
    }
}

