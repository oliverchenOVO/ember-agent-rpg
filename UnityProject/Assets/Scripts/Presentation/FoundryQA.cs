using System;
using System.Linq;
using Ember.Core;
using Ember.Core.Phase2;
using UnityEngine;
namespace Ember.Presentation
{
    public sealed partial class WitnessGame
    {
        void SetupFoundryQA(string scenario,GroupState g)
        {
            int floor=scenario.StartsWith("crossing")?1:scenario.StartsWith("foundry")&&int.TryParse(scenario.Substring(7),out var number)?number:scenario=="annulus"||scenario=="foundry_phase2"||scenario=="foundry_phase3"?10:scenario=="cross"?7:6;
            g.floor=floor;g.boss=BossRuntime.Create(tower.Data.Boss(tower.Data.Floor(floor).bossId));g.boss.visible.timer=0;
            // Give the genuine content enough anticipation to be visible in the frozen fixture.
            tower.Step();var b=g.boss;
            if(scenario=="cross"||scenario=="annulus"){var ability=tower.Data.Ability(scenario=="cross"?"rail_cross":"pulse_ring");b.ability=ability.id;b.cue=new CombatCue{shape=ability.shape,x=0,z=1,radius=ability.radius,innerRadius=ability.innerRadius,length=ability.length,interruptible=ability.interruptible,left=ability.windup};b.visible.windup=ability.windup;b.visible.targetX=0;b.visible.targetZ=1;}
            if(scenario=="foundry_phase2"||scenario=="foundry_phase3"){b.phase=scenario=="foundry_phase3"?2:1;b.visible.hp=b.visible.maxHp*(b.phase==2?.25f:.55f);b.visible.telegraph=false;}
            if(scenario=="foundry_refuge"||scenario=="foundry_split"||scenario=="foundry_saved"||scenario=="foundry_loaded"){b.visible.hp=0;tower.EnterRest(g);g.phaseClock=7;if(scenario=="foundry_split"){tower.Split(g.id,new[]{2,3});tab=4;}}
            if(scenario=="foundry_empty"||scenario=="foundry_partial"||scenario.StartsWith("crossing_"))
            {
                b.visible.hp=0;tower.EnterRest(g);g.phaseClock=7;g.restSitesRolled=true;g.availableRestSites.Clear();
                if(scenario.EndsWith("_partial"))g.availableRestSites.AddRange(new[]{"book","forge"});
            }
            if(scenario=="foundry_dead_mage")
            {
                var a=tower.State.world.agents[0];a.profession=Profession.Mage;a.weapon=a.Make("staff");a.alive=true;a.escaped=false;view.SetExpedition(tower.Data,g);
                var observed=tower.Observe(0);view.Update(observed,.5f,0);a.unlocked.Add("fireball");a.equipped.Clear();a.equipped.Add("fireball");a.mp=a.MaxMp;a.x=b.x;a.z=b.z;tower.Cast(a,g,"fireball");view.Update(observed,.01f,0);int emitted=view.EffectSerial;
                if(view.ActiveAgentEffects==0)throw new Exception("Living attack effect fixture failed");
                a.alive=false;a.hp=0;a.damage+=3;view.Update(observed,.01f,0);
                if(view.EffectSerial!=emitted||view.ActiveAgentEffects!=0)throw new Exception("Dead source emitted/retained attack effect");
                a.damage+=3;view.Update(observed,.01f,0);
                if(view.EffectSerial!=emitted)throw new Exception("Dead DOT tick emitted new projectile");
                System.IO.File.WriteAllText(System.IO.Path.Combine(artifactPath,"dead-source-check.txt"),"PASS: live effect, death cleanup, repeated dead-source damage increments; no new projectiles");
            }
            if(scenario=="holy_tooltip"){selected=3;tooltipQA=true;var healer=tower.State.world.agents[3];healer.weapon.infusion="holy";}
            else if(scenario.StartsWith("infusion"))
            {
                var a=tower.State.world.agents[0];a.weapon=a.Make(scenario=="infusion_bow"?"bow":scenario=="infusion_staff"?"staff":"greatsword",1.5f,"heal");a.weapon.affix="Astral";a.hp=a.MaxHp*.4f;a.mp=100;a.x=b.x;a.z=b.z;tower.Cast(a,g,"heal",0,true);tooltipQA=true;
            }
            else tooltipQA=false;
            if(scenario=="foundry_intel"||scenario=="foundry_english"){tower.State.Memory(0).Add(new Knowledge{key="intel:"+floor,text=Loc.Token(tower.Data.Floor(floor).intelKey),scope=MemoryScope.Run,source=KnowledgeSource.OwnExperience,confidence=.9f});tab=5;}
            if(scenario=="foundry_death"){b.visible.hp=0;tab=0;}
            simulation.Restore(tower.Observe(selected));if(scenario=="foundry_saved")SaveGame();if(scenario=="foundry_loaded"){SaveGame();LoadGame();}
        }
        bool tooltipQA;
        void DrawSkillTooltip(Agent a)
        {
            if(!tooltipQA&&!new Rect(47,468,258,134).Contains(Event.current.mousePosition))return;
            string id=!string.IsNullOrEmpty(a.weapon.infusion)?a.weapon.infusion:a.equipped.FirstOrDefault();if(string.IsNullOrEmpty(id))return;var s=simulation.Catalog.Skill(id);
            Box(335,505,465,id=="holy"?142:92,new Color(.035f,.062f,.075f,.96f));Text(350,516,435,26,Loc.T("p3.infusion",Loc.Skill(id),s.mana,s.cooldown),small);Text(350,549,435,34,Loc.T("p3.effects",Loc.T("p3.skill."+id)),small);if(id=="holy"&&tower!=null&&tower.Rules.holyRecoveryCost)Text(350,589,435,48,Loc.T("p31.skill.holy_hint"),small);
        }
    }
}
