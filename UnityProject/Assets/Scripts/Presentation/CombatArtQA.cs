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
        bool combatArtQA;string artCaption="";
        void StartCombatArtQA(string[] args)
        {
            if(Array.IndexOf(args,"--combat-art-qa")<0)return;combatArtQA=true;towerQA=true;
            towerCases=Enumerable.Range(1,25).Select(i=>"art_boss_"+i).Concat(tower.Data.abilities.Select(a=>"art_move_"+a.id)).Concat(simulation.Catalog.skills.Select(s=>"art_skill_"+s.id)).Concat(new[]{"art_windup_slam","art_windup_bolt","art_windup_charge","art_windup_furnace_lane","art_windup_pulse_ring","art_multi","art_hud","art_rest"}).ToArray();
        }
        void SetupCombatArtQA(string scenario,GroupState g)
        {
            int priorSkills=view.SkillAnimationsPlayed,priorBoss=view.BossAnimationsPlayed;bool hero=scenario.StartsWith("art_boss_");int floor=hero?int.Parse(scenario.Substring(9)):1;
            string move=scenario.StartsWith("art_move_")?scenario.Substring(9):scenario.StartsWith("art_windup_")?scenario.Substring(11):"";
            if(move.Length>0)floor=tower.Data.floors.First(f=>tower.Data.Boss(f.bossId).phases.Any(p=>p.abilities.Contains(move))).floor;
            g.floor=floor;g.boss=BossRuntime.Create(tower.Data.Boss(tower.Data.Floor(floor).bossId));var b=g.boss;var members=tower.State.Members(g).ToList();
            view.ArtPreview=hero;view.SetExpedition(tower.Data,g);view.Update(tower.Observe(0),.01f,0);artCaption=Loc.T(tower.Data.Boss(b.definition).nameKey);
            if(move.Length>0)
            {
                var def=tower.Data.Boss(b.definition);b.phase=Array.FindIndex(def.phases,p=>p.abilities.Contains(move));b.casts=Array.IndexOf(def.phases[b.phase].abilities,move);b.visible.timer=0;
                b.Tick(tower.Data,members,tower.State.world,.05f,tower.Hurt);var ability=tower.Data.Ability(move);
                b.Tick(tower.Data,members,tower.State.world,ability.windup*(scenario.StartsWith("art_move_")?1.05f:.7f),tower.Hurt);artCaption=Loc.T(ability.nameKey);
            }
            if(scenario.StartsWith("art_skill_"))
            {
                string id=scenario.Substring(10);var skill=simulation.Catalog.Skill(id);var actor=members[(int)skill.profession];actor.profession=skill.profession;actor.weapon=actor.Make(skill.profession==Profession.Warrior?"greatsword":skill.profession==Profession.Archer?"bow":"staff");
                actor.unlocked.Add(id);actor.equipped.Clear();actor.equipped.Add(id);actor.mp=actor.MaxMp;actor.x=b.x-1;actor.z=b.z-1;
                foreach(var a in members)a.hp=a.MaxHp*.2f;
                int target=skill.effect=="Heal"?actor.id:-1;if(id=="cleanse")b.statuses.Add(new StatusState{agent=actor.id,kind="Burn",left=5,power=2});if(id=="revive"){var dead=members.First(a=>a!=actor);dead.alive=false;dead.hp=0;target=dead.id;}
                if(!tower.Cast(actor,g,id,target))throw new Exception("Visual skill fixture could not cast: "+id);artCaption=Loc.Skill(id);
            }
            if(scenario=="art_multi")
            {
                foreach(var a in members){string id=new[]{"wave","poison","meteor","heal"}[a.id];a.unlocked.Add(id);a.equipped.Clear();a.equipped.Add(id);a.mp=a.MaxMp;a.hp=a.MaxHp*.2f;a.x=-3+a.id*2;a.z=0;if(!tower.Cast(a,g,id,a.id==3?3:-1))throw new Exception("Concurrent visual fixture failed");}
                int before=view.SkillAnimationsPlayed;view.Update(tower.Observe(0),.01f,0);if(view.SkillAnimationsPlayed-before!=4)throw new Exception("Simultaneous skills overwritten");File.WriteAllText(Path.Combine(artifactPath,"concurrent-skills.txt"),"PASS: four actual casts between render frames produced four skill animations");artCaption=Loc.T("art.concurrent");
            }
            if(scenario=="art_hud"){b.visible.timer=0;tower.Step();artCaption=Loc.T("art.live");}
            if(scenario=="art_rest"){b.visible.hp=0;tower.EnterRest(g);g.availableRestSites.Clear();g.availableRestSites.AddRange(new[]{"book","forge","bed"}.Where(id=>tower.Data.Rest(g.restId).sites.Any(s=>s.id==id)));g.restSitesRolled=true;artCaption=Loc.T("ui.refuge_title");}
            view.SetExpedition(tower.Data,g);view.Update(tower.Observe(0),.01f,0);
            int skills=view.SkillAnimationsPlayed-priorSkills,bossMoves=view.BossAnimationsPlayed-priorBoss;
            if(scenario.StartsWith("art_skill_")&&skills!=1||scenario=="art_multi"&&skills!=4)throw new Exception("Actual cast did not reach animation renderer: "+scenario);
            if(move.Length>0&&bossMoves<(scenario.StartsWith("art_move_")?2:1))throw new Exception("Boss event did not reach animation renderer: "+scenario);
            File.AppendAllText(Path.Combine(artifactPath,"art-event-checks.csv"),scenario+","+skills+","+bossMoves+","+view.ActiveCombatAnimations+"\n");
            if(view.DetailedBossMeshCount<12)throw new Exception("Boss sculpture missing meshes");
            File.AppendAllText(Path.Combine(artifactPath,"art-mesh-counts.csv"),scenario+","+view.DetailedBossMeshCount+"\n");
            simulation.Restore(tower.Observe(selected));
        }
        void DrawArtCaption()
        {
            Box(30,795,800,75,new Color(.035f,.065f,.08f,.9f));Text(48,802,760,43,artCaption,title);Text(48,846,750,23,Loc.T("art.preview"),small);
        }
    }
}
