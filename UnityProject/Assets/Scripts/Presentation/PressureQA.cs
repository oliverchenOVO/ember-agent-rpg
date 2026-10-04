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
        void SetupPressureQA(string scenario,GroupState g)
        {
            g.floor=8;g.boss=BossRuntime.Create(tower.Data.Boss(tower.Data.Floor(8).bossId));var b=g.boss;var a=tower.State.world.agents[0];
            b.visible.timer=10000;var node=b.pressureCores[0];a.x=node.x;a.z=node.z;
            if(scenario=="pressure_warning"){node.stage=0;node.left=2;}
            if(scenario=="pressure_active"||scenario=="pressure_outside"||scenario=="pressure_overtime"||scenario=="pressure_combined"){node.stage=1;node.left=4;}
            if(scenario=="pressure_outside"){a.x=0;a.z=0;}
            if(scenario=="pressure_overtime")b.elapsed=250;
            if(scenario=="pressure_broken")node.hp=0;
            if(scenario=="pressure_disabled")foreach(var n in b.pressureCores)n.hp=0;
            if(scenario=="pressure_combined"){b.statuses.Add(new StatusState{agent=0,kind="Burn",left=4,power=2});b.adds=2;b.hazardLeft=4;b.hazardX=a.x;b.hazardZ=a.z;}
            if(scenario=="pressure_attack"){a.x=-2;a.z=3;for(int i=0;i<3;i++)tower.Step();}
            if(scenario=="pressure_refuge"){b.visible.hp=0;tower.EnterRest(g);}
            if(scenario=="pressure_dossier")OpenInspection(Inspection.Boss);
            if(scenario=="pressure_saved"){node.hp*=.5f;node.stage=0;node.left=1.4f;string saved=JsonUtility.ToJson(tower.State);tower.Restore(ExpeditionStore.Parse(saved,simulation.Catalog,tower.Data));g=tower.State.groups[0];b=g.boss;}
            string before=JsonUtility.ToJson(tower.State);view.SetExpedition(tower.Data,g);simulation.Restore(tower.Observe(0));view.Update(simulation.State,.1f,0);
            int count=g.phase==Phase.Battle?3:0,warnings=g.phase==Phase.Battle?b.pressureCores.Count(n=>n.hp>0&&n.stage==0):0,pulses=g.phase==Phase.Battle?b.pressureCores.Count(n=>n.hp>0&&n.stage==1):0;
            if(view.VisiblePressureCores!=count||view.VisiblePressureWarnings!=warnings||view.VisiblePressurePulses!=pulses||view.PressureAuraVisible)throw new Exception("Pressure visual/state mismatch: "+scenario);
            if(before!=JsonUtility.ToJson(tower.State))throw new Exception("Pressure presentation mutated simulation");
            if(scenario=="pressure_outside"&&PressureField.Rate(tower.Data,b,a.x,a.z)!=0)throw new Exception("Outside still hurts");
            File.AppendAllText(Path.Combine(artifactPath,"pressure-checks.txt"),scenario+" / PASS / cores="+count+" / warnings="+warnings+" / pulses="+pulses+"\n");
        }
    }
}
