using System;
using Ember.Core;
using Ember.Core.Phase2;
using UnityEngine;
namespace Ember.Presentation
{
    public sealed partial class WitnessGame
    {
        float refugeEstimate,refugeEstimateUntil;
        void DrawRefugeObservation(World w)
        {
            if(tower==null||w.phase!=Phase.Rest)return;var g=tower.State.GroupOf(selected);if(g.refugeVersion!=1)return;
            if(Button(700,843,175,Loc.T(view.RefugeManual?"inspect.reset_camera":view.RefugeOverview?"refuge.follow":"refuge.overview"))){if(view.RefugeManual){view.ResetRefugeCamera();view.RefugeOverview=false;}else view.RefugeOverview=!view.RefugeOverview;}
            var plan=tower.State.Plan(selected);var map=RefugeMap.For(g);float scale=Mathf.Min(Screen.width/1600f,Screen.height/900f),ox=(Screen.width-1600*scale)/2,oy=(Screen.height-900*scale)/2;
            if(Time.realtimeSinceStartup>=refugeEstimateUntil){refugeEstimate=map.Distance(new Vector2(w.agents[selected].x,w.agents[selected].z),RefugeMap.Exit)/Simulation.MoveSpeed;refugeEstimateUntil=Time.realtimeSinceStartup+.5f;}
            Box(425,199,590,52,new Color(.035f,.062f,.075f,.88f));Text(438,203,565,23,Loc.T("refuge.budget",plan.knownRooms.Count,refugeEstimate.ToString("F1")),small);
            Text(438,225,565,23,Loc.T(plan.discussionLeft>0?"refuge.discussing":plan.workLeft>0&&plan.site.StartsWith("search:")?"refuge.searching":"refuge.navigation"),small);
            // Observation-only map: displayed buildings do not grant knowledge to Agents.
            Box(1014,462,244,183,new Color(.035f,.062f,.075f,.92f));Text(1022,467,228,22,Loc.T("refuge.map"),small);
            Vector2 Mini(Vector2 p)=>new Vector2(1022+(p.x+30)*3.8f,493+(20-p.y)*3.55f);
            foreach(var wall in map.obstacles){var p=Mini(new Vector2(wall.xMin,wall.yMax));Box(p.x,p.y,Mathf.Max(2,wall.width*3.8f),Mathf.Max(2,wall.height*3.55f),new Color(.35f,.4f,.4f));}
            for(int i=0;i<6;i++){var p=Mini(map.rooms[i]);Box(p.x-11,p.y-7,22,14,plan.knownRooms.Contains(i)?new Color(.16f,.54f,.45f):new Color(.3f,.3f,.3f));}
            var departure=Mini(RefugeMap.Exit);Box(departure.x-3,departure.y-4,6,8,view.colors[1]);
            foreach(int member in g.members){var a=w.agents[member];if(!a.alive||a.escaped)continue;var p=Mini(new Vector2(a.x,a.z));Box(p.x-3,p.y-3,6,6,view.colors[member]);}
            if(g.phaseClock>=RefugeMap.Limit(tower.Data.Rest(g.restId),g)){float front=Mathf.Clamp(RefugeMap.Front(tower.Data.Rest(g.restId),g),-30,30);Box(1022,493,(front+30)*3.8f,142,new Color(.65f,.14f,.06f,.6f));}
            foreach(var site in tower.SpatialSites(g))
            {
                if(view.RefugeOverview||!tower.Data.Rest(g.restId).HasSite(g,site.id)||Simulation.Distance(w.agents[selected].x,w.agents[selected].z,site.x,site.z)>13)continue;var p=view.ScreenPoint(new Vector3(site.x,1.5f,site.z));if(p.z<=0)continue;
                float x=(p.x-ox)/scale,y=(Screen.height-p.y-oy)/scale;if(x<385||x>980||y<260||y>620)continue;
                var plate=new Rect(x-85,y,170,24);Box(plate.x,plate.y,plate.width,plate.height,new Color(.03f,.06f,.07f,.85f));Text(plate.x+4,plate.y+1,162,22,Loc.T(site.nameKey),small);
                if(plate.Contains(Event.current.mousePosition)){Box(425,260,590,60,new Color(.03f,.06f,.07f,.96f));Text(438,265,565,48,Loc.T("refuge.facility",Loc.T(site.nameKey),site.seconds.ToString("F1"),site.materialCost,Mathf.RoundToInt(site.risk*100)),small);}
            }
        }
    }
}
