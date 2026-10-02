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
            int floor=scenario.StartsWith("foundry")&&int.TryParse(scenario.Substring(7),out var number)?number:scenario=="annulus"||scenario=="foundry_phase2"||scenario=="foundry_phase3"?10:scenario=="cross"?7:6;
            g.floor=floor;g.boss=BossRuntime.Create(tower.Data.Boss(tower.Data.Floor(floor).bossId));g.boss.visible.timer=0;
            // Give the genuine content enough anticipation to be visible in the frozen fixture.
            tower.Step();var b=g.boss;
            if(scenario=="cross"||scenario=="annulus"){var ability=tower.Data.Ability(scenario=="cross"?"rail_cross":"pulse_ring");b.ability=ability.id;b.cue=new CombatCue{shape=ability.shape,x=0,z=1,radius=ability.radius,innerRadius=ability.innerRadius,length=ability.length,interruptible=ability.interruptible,left=ability.windup};b.visible.windup=ability.windup;b.visible.targetX=0;b.visible.targetZ=1;}
            if(scenario=="foundry_phase2"||scenario=="foundry_phase3"){b.phase=scenario=="foundry_phase3"?2:1;b.visible.hp=b.visible.maxHp*(b.phase==2?.25f:.55f);b.visible.telegraph=false;}
            if(scenario=="foundry_refuge"||scenario=="foundry_split"||scenario=="foundry_saved"||scenario=="foundry_loaded"){b.visible.hp=0;tower.EnterRest(g);g.phaseClock=7;if(scenario=="foundry_split"){tower.Split(g.id,new[]{2,3});tab=4;}}
            if(scenario.StartsWith("infusion"))
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
            Box(335,505,465,92,new Color(.035f,.062f,.075f,.96f));Text(350,516,435,26,Loc.T("p3.infusion",Loc.Skill(id),s.mana,s.cooldown),small);Text(350,549,435,34,Loc.T("p3.effects",Loc.T("p3.skill."+id)),small);
        }
    }
}
