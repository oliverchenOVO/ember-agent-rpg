using System;
using Ember.Core;
using UnityEngine;

namespace Ember.Presentation
{
    public sealed partial class WitnessGame
    {
        sealed class AllocationBucket
        {
            long current,previous;int frame=-1;
            public void Add(long bytes){if(frame!=Time.frameCount){previous=current;current=0;frame=Time.frameCount;}current+=Math.Max(0,bytes);}
            public long Previous=>frame==Time.frameCount?previous:current;
        }
        readonly AllocationBucket uiAlloc=new AllocationBucket(),stepAlloc=new AllocationBucket(),viewAlloc=new AllocationBucket(),renderAlloc=new AllocationBucket();
        readonly struct AllocationScope:IDisposable
        {
            readonly AllocationBucket bucket;readonly long before;
            public AllocationScope(AllocationBucket target,bool enabled){bucket=enabled?target:null;before=enabled?GC.GetAllocatedBytesForCurrentThread():0;}
            public void Dispose(){if(bucket!=null)bucket.Add(GC.GetAllocatedBytesForCurrentThread()-before);}
        }
        public long ProfileUiAlloc=>uiAlloc.Previous;public long ProfileStepAlloc=>stepAlloc.Previous;public long ProfileViewAlloc=>viewAlloc.Previous;public long ProfileRenderAlloc=>renderAlloc.Previous;
        string RenderForUI(string token)
        {using var scope=new AllocationScope(renderAlloc,profile);return Loc.Render(token);}
    }
}
