using System;
using System.IO;
using Ember.Core;
using UnityEngine;

namespace Ember.Presentation
{
    public sealed partial class WitnessGame
    {
        sealed class AllocationBucket
        {
            long current,previous,currentTicks,previousTicks;int frame=-1;
            public void Add(long bytes,long ticks){if(frame!=Time.frameCount){previous=current;previousTicks=currentTicks;current=0;currentTicks=0;frame=Time.frameCount;}current+=Math.Max(0,bytes);currentTicks+=ticks;}
            public long Previous=>frame==Time.frameCount?previous:current;
            public double Milliseconds=>(frame==Time.frameCount?previousTicks:currentTicks)*1000d/System.Diagnostics.Stopwatch.Frequency;
        }
        readonly AllocationBucket uiAlloc=new AllocationBucket(),stepAlloc=new AllocationBucket(),viewAlloc=new AllocationBucket(),renderAlloc=new AllocationBucket();
        readonly struct AllocationScope:IDisposable
        {
            readonly AllocationBucket bucket;readonly long before,stamp;
            public AllocationScope(AllocationBucket target,bool enabled){bucket=enabled?target:null;before=enabled&&allocationCounterValid?GC.GetAllocatedBytesForCurrentThread():0;stamp=enabled?System.Diagnostics.Stopwatch.GetTimestamp():0;}
            public void Dispose(){if(bucket!=null)bucket.Add(allocationCounterValid?GC.GetAllocatedBytesForCurrentThread()-before:0,System.Diagnostics.Stopwatch.GetTimestamp()-stamp);}
        }
        static bool allocationCounterValid;
        public double ProfileUiMs=>uiAlloc.Milliseconds;public double ProfileStepMs=>stepAlloc.Milliseconds;public double ProfileViewMs=>viewAlloc.Milliseconds;public double ProfileRenderMs=>renderAlloc.Milliseconds;
        public bool ProfileDetailed=>profileDetailed;public bool ProfileAllocationCounterValid=>allocationCounterValid;
        void CalibrateAllocationCounter(){long before=GC.GetAllocatedBytesForCurrentThread();var probe=new byte[65536];long delta=GC.GetAllocatedBytesForCurrentThread()-before;GC.KeepAlive(probe);allocationCounterValid=delta>=65536;File.WriteAllText(Path.Combine(artifactPath,"allocation-calibration.txt"),"knownPayloadBytes=65536\nobservedThreadBytes="+delta+"\navailable="+allocationCounterValid);}
        public long ProfileUiAlloc=>uiAlloc.Previous;public long ProfileStepAlloc=>stepAlloc.Previous;public long ProfileViewAlloc=>viewAlloc.Previous;public long ProfileRenderAlloc=>renderAlloc.Previous;
        string RenderForUI(string token)
        {using var scope=new AllocationScope(renderAlloc,profile&&profileDetailed);return Loc.Render(token);}
    }
}
