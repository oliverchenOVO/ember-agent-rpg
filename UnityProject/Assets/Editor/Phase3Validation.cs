using System;
using System.IO;
using System.Linq;
using Ember.Core;
using Ember.Core.Phase2;
using UnityEngine;

namespace Ember.Editor
{
    public static class Phase3Validation
    {
        public static void Baseline(){Seeds("baseline",1000);}
        public static void Seeds(string label,int count)
        {
            var c=JsonUtility.FromJson<Catalog>(Resources.Load<TextAsset>("catalog").text);var data=TowerContent.Load();
            string root=Path.GetFullPath("../Artifacts/Phase3");Directory.CreateDirectory(root);
            int clears=0,wipes=0,errors=0;long ticks=0;
            using(var writer=new StreamWriter(Path.Combine(root,label+".jsonl")))
            {
                for(uint seed=1;seed<=count;seed++)
                {
                    using(var s=new TowerSimulation(c,data,seed,false){TelemetryEnabled=true,TelemetrySeed=seed})
                    {
                        int tick=0;for(;tick<50000&&s.State.world.phase!=Phase.Ended;tick++)s.Step();ticks+=tick;
                        if(s.State.world.phase!=Phase.Ended)errors++;else if(s.State.world.outcome==Outcome.TowerClear)clears++;else wipes++;
                        foreach(var row in s.State.telemetry)writer.WriteLine(JsonUtility.ToJson(row));
                    }
                    if(seed%100==0)Debug.Log("PHASE3 TELEMETRY / "+label+" / "+seed);
                }
            }
            File.WriteAllText(Path.Combine(root,label+"-summary.txt"),$"Seeds={count}; clears={clears}; wipes={wipes}; nonterminal={errors}; ticks={ticks}\n");
            if(errors>0)throw new Exception("Nonterminal telemetry seeds");
        }
    }
}
