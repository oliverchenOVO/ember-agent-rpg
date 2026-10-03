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
        bool refugeQA;string refugeCaption="";
        void StartRefugeQA(string[] args)
        {
            if(Array.IndexOf(args,"--refuge-qa")<0)return;refugeQA=true;towerQA=true;
            towerCases=new[]{"forest","foundry","castle","abyss","ice"}.SelectMany(t=>new[]{"refuge_overview_"+t,"refuge_interior_"+t}).Concat(new[]{"bed","forge","library","book","clinic","church","corpse","cache","resource"}.Select(id=>"refuge_object_"+id)).Concat(new[]{"refuge_empty","refuge_partial","refuge_collapse","refuge_hud"}).ToArray();
        }
        void SetupRefugeQA(string scenario,GroupState g)
        {
            string theme=scenario.Split('_').Last();g.floor=theme=="foundry"?6:theme=="castle"?11:theme=="abyss"?16:theme=="ice"?22:1;
            g.boss=BossRuntime.Create(tower.Data.Boss(tower.Data.Floor(g.floor).bossId));tower.EnterRest(g);g.refugeVariant=0;g.availableRestSites.Clear();g.availableRestSites.AddRange(tower.Data.Rest(g.restId).sites.Select(s=>s.id));
            view.ArtPreview=false;view.PreviewRefugeRoom=-1;view.PreviewRefugeObject=null;view.RefugeOverview=!scenario.Contains("interior")&&!scenario.Contains("object");
            refugeCaption=Loc.T("refuge.scene."+(new[]{"forest","foundry","castle","abyss","ice"}.Contains(theme)?theme:"forest"));
            if(scenario.Contains("interior"))view.PreviewRefugeRoom=0;
            if(scenario.Contains("object"))
            {
                var site=tower.SpatialSites(g).Single(s=>s.id==theme);view.PreviewRefugeObject=new Vector3(site.x,0,site.z);refugeCaption=Loc.T(site.nameKey);
            }
            if(scenario=="refuge_empty")g.availableRestSites.Clear();
            if(scenario=="refuge_partial"){g.availableRestSites.Clear();g.availableRestSites.AddRange(new[]{"bed","book","forge"});}
            if(scenario=="refuge_collapse")g.phaseClock=RefugeMap.Limit(tower.Data.Rest(g.restId),g)+12;
            var a=tower.State.world.agents[0];a.x=-20;a.z=-10;tower.State.Plan(0).knownRooms.Add(0);tower.State.Plan(0).decision=new HighDecision{intent="Explore",proposedAction="search:1"};
            if(scenario=="refuge_hud"){view.RefugeOverview=false;tower.State.Plan(0).discussionLeft=2;}
            view.SetExpedition(tower.Data,g);view.Update(tower.Observe(0),1,0);
            foreach(var site in tower.SpatialSites(g)){bool present=tower.Data.Rest(g.restId).HasSite(g,site.id);int meshes=view.RefugeFacilityMeshes(site.id);if(present?meshes<7:meshes!=0)throw new Exception("Facility visibility incorrect: "+site.id);}
            if(scenario=="refuge_collapse"&&!view.RefugeCollapseVisible)throw new Exception("Collapse visuals hidden by ancestor");
            if(view.VisibleRefugeMeshes<200)throw new Exception("Detailed refuge geometry missing");
            File.AppendAllText(Path.Combine(artifactPath,"refuge-mesh-checks.csv"),scenario+","+view.VisibleRefugeMeshes+","+g.availableRestSites.Count+"\n");
        }
        void DrawRefugeCaption()
        {Box(30,795,1000,75,new Color(.035f,.065f,.08f,.9f));Text(48,802,960,43,refugeCaption,title);Text(48,846,960,23,Loc.T("refuge.gallery"),small);}
    }
}
