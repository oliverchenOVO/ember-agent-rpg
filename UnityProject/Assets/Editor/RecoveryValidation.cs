using System;
using System.IO;
using System.Linq;
using Ember.Core;
using Ember.Core.Phase2;
using UnityEngine;

namespace Ember.Editor
{
    public static class RecoveryValidation
    {
        public static void Run()
        {
            Phase31Validation.ShortGate();
            var c=JsonUtility.FromJson<Catalog>(Resources.Load<TextAsset>("catalog").text);var data=TowerContent.Load();int checks=0;
            using(var s=new TowerSimulation(c,data,1729,true){DiagnosticsEnabled=true,TelemetryEnabled=true})
            {
                var g=s.State.groups[0];var healer=s.State.world.agents.Single(a=>a.profession==Profession.Healer);var dead=s.State.world.agents[0];
                healer.unlocked.Add("revive");healer.equipped.Add("revive");healer.mp=healer.MaxMp;healer.x=dead.x;healer.z=dead.z;
                void Observe(string expected,bool danger=false){s.ObserveRevive(healer,g,.1f,danger);if(!s.Telemetry(g).reviveOpportunities.Any(v=>v.reasonNotCast==expected))throw new Exception("Revive observation missing "+expected);checks++;}
                Observe("NO_TARGET");s.Die(dead,"fixture");Observe("PRIORITY");Observe("DANGER",true);
                healer.x=100;Observe("OUT_OF_RANGE");healer.x=dead.x;healer.mp=0;Observe("NO_MANA");healer.mp=healer.MaxMp;
                healer.cooldowns.Add(new Cooldown{id="revive",left=1});Observe("COOLDOWN");healer.cooldowns.RemoveAll(v=>v.id=="revive");
                healer.equipped.Remove("revive");Observe("NOT_EQUIPPED");healer.equipped.Add("revive");
                healer.intent=new Intent{kind=ActionKind.Skill,skill="revive",target=dead.id};Observe("ATTEMPT");
                if(!s.Cast(healer,g,"revive",dead.id)||!dead.alive)throw new Exception("Eligible revive did not restore target");checks++;
                s.Die(dead,"fixture");Observe("EXPIRED");
                if(s.Cast(healer,g,"revive",dead.id))throw new Exception("Second revive must be rejected");checks++;
            }
            Debug.Log("RECOVERY DIAGNOSTICS GATE PASSED / "+checks);
        }
    }
}
