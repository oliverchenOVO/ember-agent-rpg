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
        StreamWriter writer;WitnessGame game;float started,next,maintenance,checkpoint;int seconds,rows;string folder;
        readonly FrameTiming[] frames=new FrameTiming[1];
        public void Initialize(WitnessGame owner,string path,int duration)
        {
            game=owner;folder=path;seconds=duration;Directory.CreateDirectory(path);started=Time.realtimeSinceStartup;maintenance=started+60;checkpoint=started+1800;
            foreach(var pair in new[]{(ProfilerCategory.Internal,"CPU Main Thread Frame Time"),(ProfilerCategory.Render,"GPU Frame Time"),(ProfilerCategory.Memory,"GC Allocated In Frame"),(ProfilerCategory.Memory,"GC Used Memory"),(ProfilerCategory.Memory,"GC Reserved Memory"),(ProfilerCategory.Memory,"Total Used Memory"),(ProfilerCategory.Render,"Draw Calls Count"),(ProfilerCategory.Render,"Batches Count")})
            {names.Add(pair.Item2);counters.Add(ProfilerRecorder.StartNew(pair.Item1,pair.Item2,1));}
            writer=new StreamWriter(Path.Combine(path,"frames.csv"));writer.WriteLine("elapsed,run,floor,groups,frame_ms,cpu_ns,gpu_ns,gc_alloc,gc_used,gc_reserved,total_used,draw_calls,batches,gpu_timing_ms,managed_heap,objects,meshes,materials,particles,audio_sources,ai_ms,save_ms,telemetry_ms");
            var available=new List<ProfilerRecorderHandle>();ProfilerRecorderHandle.GetAvailable(available);var lines=new List<string>();
            foreach(var h in available){var d=ProfilerRecorderHandle.GetDescription(h);if(d.Name.Contains("Job")||d.Name.Contains("Temp")||d.Name.Contains("GPU"))lines.Add(d.Name+" / "+d.Category);}
            File.WriteAllText(Path.Combine(path,"counter-availability.txt"),string.Join("\n",lines));
            File.WriteAllText(Path.Combine(path,"session.txt"),"development="+Debug.isDebugBuild+"\nGPU="+SystemInfo.graphicsDeviceName+"\nAPI="+SystemInfo.graphicsDeviceType+"\nseconds="+seconds+"\nstartedUTC="+DateTime.UtcNow.ToString("O"));
        }
        void LateUpdate()
        {
            if(writer==null)return;float now=Time.realtimeSinceStartup;FrameTimingManager.CaptureFrameTimings();
            if(now>=maintenance){game.ProfileMaintenance((int)((now-started)/60));maintenance=now+60;}
            if(now>=next)
            {
                next=now+1;FrameTimingManager.GetLatestTimings(1,frames);var s=game.ProfileState;
                var values=new List<string>{(now-started).ToString("F3",CultureInfo.InvariantCulture),s.world.run.ToString(),s.world.floor.ToString(),s.groups.Count.ToString(),(Time.unscaledDeltaTime*1000).ToString("F3",CultureInfo.InvariantCulture)};
                foreach(var r in counters)values.Add(r.Valid?r.LastValue.ToString():"NA");
                values.Add(frames[0].gpuFrameTime>0?frames[0].gpuFrameTime.ToString("F3",CultureInfo.InvariantCulture):"NA");
                values.Add(Profiler.GetMonoUsedSizeLong().ToString());values.Add(Resources.FindObjectsOfTypeAll<GameObject>().Length.ToString());values.Add(Resources.FindObjectsOfTypeAll<Mesh>().Length.ToString());values.Add(Resources.FindObjectsOfTypeAll<Material>().Length.ToString());
                int active=0;foreach(var ps in Resources.FindObjectsOfTypeAll<ParticleSystem>())active+=ps.particleCount;values.Add(active.ToString());values.Add(Resources.FindObjectsOfTypeAll<AudioSource>().Length.ToString());
                values.Add(game.profileAiMs.ToString("F4",CultureInfo.InvariantCulture));values.Add(game.profileSaveMs.ToString("F4",CultureInfo.InvariantCulture));values.Add(game.profileTelemetryMs.ToString("F4",CultureInfo.InvariantCulture));writer.WriteLine(string.Join(",",values));writer.Flush();rows++;
            }
            if(now>=checkpoint){File.WriteAllText(Path.Combine(folder,"checkpoint-"+(int)((now-started)/60)+"m.json"),JsonUtility.ToJson(game.ProfileState,true));checkpoint=now+1800;}
            if(now-started>=seconds){File.WriteAllText(Path.Combine(folder,"complete.txt"),"elapsed="+(now-started)+"; rows="+rows+"; runs="+game.ProfileState.world.run+"; maintenance="+game.profileMaintenances);game.ProfileStop();StartCoroutine(QuitAfterCleanup());writer.Dispose();writer=null;}
        }
        IEnumerator QuitAfterCleanup(){yield return new WaitForSecondsRealtime(1);Application.Quit(0);}
        void OnDestroy(){writer?.Dispose();foreach(var r in counters)r.Dispose();counters.Clear();}
    }
    public sealed partial class WitnessGame
    {
        bool profile;public double profileAiMs,profileSaveMs,profileTelemetryMs;public int profileMaintenances;float profileManualUntil;
        public ExpeditionState ProfileState=>tower.State;
        void StartProfile(string[] args)
        {
            int i=Array.IndexOf(args,"--profile-seconds");if(i<0)return;profile=true;int duration=int.Parse(args[i+1]);speed=16;
            savePath=Path.Combine(artifactPath,"soak-save.json");tower.TelemetryEnabled=true;tower.TelemetrySeed=1729;
            gameObject.AddComponent<PerformanceSession>().Initialize(this,artifactPath,duration);
        }
        public void ProfileMaintenance(int minute)
        {
            var watch=System.Diagnostics.Stopwatch.StartNew();SaveGame();LoadGame();profileSaveMs=watch.Elapsed.TotalMilliseconds;profileMaintenances++;
            selected=(selected+1)%4;
            if(minute%5==0&&tower.State.world.phase!=Phase.Ended)
            {
                // Deliberate fault scenarios supplement natural runs; this is a stress workload, not balance telemetry.
                var g=tower.State.GroupOf(selected);if(g.phase==Phase.Rest){g.phaseClock=55;foreach(var a in tower.State.Members(g))a.escaped=false;}
                else {var a=tower.State.world.agents[selected];if(a.alive)tower.Die(a,Loc.Token("p3.cause.pressure"));}
            }
            var telemetryWatch=System.Diagnostics.Stopwatch.StartNew();File.WriteAllText(Path.Combine(artifactPath,"telemetry-latest.json"),JsonUtility.ToJson(tower.State));profileTelemetryMs=telemetryWatch.Elapsed.TotalMilliseconds;
        }
        public void ProfileStop(){paused=true;enabled=false;view.Dispose();audioSource.Stop();if(sound!=null)Destroy(sound);if(strike!=null)Destroy(strike);if(heal!=null)Destroy(heal);}
    }
    public sealed class NativeAllocationProbe : MonoBehaviour
    {
        public string mode;float end;ParticleSystem probeParticles;bool drained;
        void Start()
        {
            end=Time.realtimeSinceStartup+8;
            if(mode.Contains("camera")){new GameObject("Probe camera").AddComponent<Camera>();new GameObject("Probe light").AddComponent<Light>();}
            if(mode.Contains("mesh")){var g=GameObject.CreatePrimitive(PrimitiveType.Cube);g.GetComponent<Renderer>().sharedMaterial=new Material(Shader.Find("Standard"));}
            if(mode.Contains("particles")){probeParticles=new GameObject("Probe particle").AddComponent<ParticleSystem>();probeParticles.Play();}
            Debug.Log("EMBER NATIVE PROBE / "+mode);
        }
        void Update(){if(mode.Contains("cleanup")&&!drained&&Time.realtimeSinceStartup>=end-2){drained=true;probeParticles.Stop(true,ParticleSystemStopBehavior.StopEmittingAndClear);Destroy(probeParticles.gameObject);}if(Time.realtimeSinceStartup>=end)Application.Quit(0);}
    }
}
