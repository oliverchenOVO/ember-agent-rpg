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
        void SetupReadabilityQA(string scenario,GroupState g)
        {
            legacyPreviewAgent=-1;legacyPreview=0;inspectionQAMouse=null;tooltipQA=false;inspectionCameraQA=false;
            var agents=tower.State.world.agents;
            if(scenario.StartsWith("read_book"))
            {
                g.floor=8;g.boss=BossRuntime.Create(tower.Data.Boss(tower.Data.Floor(8).bossId));
                tower.Die(agents[0],Loc.Token("p3.cause.pressure"));tower.Die(agents[1],Loc.Token("p2.cause.hazard"));
                var earlier=JsonUtility.FromJson<Epitaph>(JsonUtility.ToJson(tower.State.world.book.First(e=>e.author==0)));earlier.run=0;tower.State.world.book.Insert(0,earlier);
                selected=scenario=="read_book_lyra"?1:scenario=="read_book_missing"?3:0;tab=1;
                if(scenario=="read_book_old"){legacyPreviewAgent=0;legacyPreview=1;}
                if(scenario=="read_book_full"||scenario=="read_book_all"){OpenInspection(Inspection.Book);bookAll=scenario=="read_book_all";}
                var own=tower.State.world.book.Where(e=>e.author==selected).ToArray();if(own.Any(e=>e.author!=selected)||selected==3&&own.Length!=0||selected==0&&own.Length!=2)throw new Exception("Legacy author fixture failed");
            }
            else
            {
                g.floor=8;g.boss=BossRuntime.Create(tower.Data.Boss(tower.Data.Floor(8).bossId));var b=g.boss;b.visible.telegraph=false;b.visible.timer=5;b.elapsed=30;
                if(scenario=="read_overtime"||scenario=="read_combined")b.elapsed=250;
                if(scenario=="read_burn"||scenario=="read_combined")b.statuses.Add(new StatusState{agent=0,kind="Burn",power=2,left=5,ability="extract"});
                if(scenario=="read_summons"||scenario=="read_combined")b.adds=2;
                if(scenario=="read_hazard"||scenario=="read_combined"){b.hazardLeft=4;b.hazardX=agents[0].x;b.hazardZ=agents[0].z;}
                string before=JsonUtility.ToJson(tower.State);var lines=DamageDescriptions(tower.Data,g,agents[0]);
                if(!lines.Any(l=>l==Loc.T("readability.pressure",(tower.Data.Boss(b.definition).ambientPressure*(1+Mathf.Max(0,b.elapsed-tower.Data.Boss(b.definition).phases[b.phase].enrageAfter)/20)).ToString("F1"))))throw new Exception("Pressure display absent");
                if(scenario=="read_overtime"&&!lines.Any(l=>l.Contains(Loc.T("readability.attrition",(agents[0].MaxHp*10*.015f).ToString("F1")))))throw new Exception("Overtime display absent");
                if(before!=JsonUtility.ToJson(tower.State))throw new Exception("Damage UI mutated simulation");
                view.SetExpedition(tower.Data,g);simulation.Restore(tower.Observe(0));view.Update(simulation.State,.1f,0);if(!view.PressureAuraVisible)throw new Exception("Pressure aura invisible without cast");
                // Exercise the actual Boss tick with no scheduled cast: pressure must still hurt.
                if(scenario=="read_pressure"){float hp=agents[0].hp;b.Tick(tower.Data,g.members.Select(id=>agents[id]).ToList(),tower.State.world,.1f,(a,raw,cause)=>a.hp-=raw);if(agents[0].hp>=hp||b.casts!=0||b.visible.telegraph)throw new Exception("No-cast pressure fixture failed");}
                File.WriteAllText(Path.Combine(artifactPath,"readability-check.txt"),"PASS: no-cast pressure damage, visible persistent aura, overtime labels and observation state unchanged; author-specific legacy fixtures.");
            }
        }
    }
}
