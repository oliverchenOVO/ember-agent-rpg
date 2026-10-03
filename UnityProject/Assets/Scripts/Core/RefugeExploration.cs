using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
namespace Ember.Core.Phase2
{
    public sealed partial class TowerSimulation
    {
        public RestSite[] SpatialSites(GroupState g)=>g.refugeVersion==1?RefugeMap.For(g).Sites(Data.Rest(g.restId),g):Data.Rest(g.restId).sites;
        bool KnownSite(Agent a,GroupState g,string id)
        {var r=Data.Rest(g.restId);if(!r.HasSite(g,id))return false;int index=Array.FindIndex(r.sites,s=>s.id==id);return index>=0&&(g.refugeVersion==0||State.Plan(a.id).knownRooms.Contains(RefugeMap.For(g).RoomForSite(index,g.refugeVariant)));}
        RestSite[] RestOptions(Agent a,GroupState g,bool estimate=false)
        {
            var list=SpatialSites(g).Where(s=>KnownSite(a,g,s.id)).ToList();
            if(g.refugeVersion==1)
            {var map=RefugeMap.For(g);for(int i=0;i<6;i++)if(!State.Plan(a.id).knownRooms.Contains(i)){var p=map.rooms[i];list.Add(new RestSite{id="search:"+i,nameKey="refuge.search",effect="Search",x=p.x,z=p.y,seconds=2.5f});}
                foreach(var site in list){if(site.effect!="Search")site.z+=site.z<0?2:-2;if(!estimate)continue;var p=new Vector2(site.x,site.z);site.travelSeconds=map.Distance(new Vector2(a.x,a.z),p)/Simulation.MoveSpeed;site.escapeSeconds=map.Distance(p,RefugeMap.Exit)/Simulation.MoveSpeed;}}
            return list.ToArray();
        }
        public void MoveInRefuge(Agent a,GroupState g,Vector2 target,float dt)
        {
            var map=RefugeMap.For(g);var start=new Vector2(a.x,a.z);
            var route=State.Plan(a.id).route;
            if(route.group!=g.id||route.floor!=g.floor||route.target!=target||route.index>=route.points.Count)
            {route=new RefugeRoute{group=g.id,floor=g.floor,target=target,points=map.Path(start,target)};State.Plan(a.id).route=route;}
            float budget=Simulation.MoveSpeed*dt;
            while(route.index<route.points.Count&&budget>0)
            {var next=route.points[route.index];float d=Vector2.Distance(start,next);var moved=Vector2.MoveTowards(start,next,budget);if(!map.Clear(start,moved)){route.points.Clear();return;}budget-=d;start=moved;if(d<=Simulation.MoveSpeed*dt&&start==next)route.index++;else break;}
            a.x=start.x;a.z=start.y;
        }
        void RevealRoom(Agent a,GroupState g,int room)
        {
            var plan=State.Plan(a.id);if(!plan.knownRooms.Contains(room))plan.knownRooms.Add(room);
            int count=Data.Rest(g.restId).sites.Select((s,i)=>new{site=s,index=i}).Count(v=>RefugeMap.For(g).RoomForSite(v.index,g.refugeVariant)==room&&Data.Rest(g.restId).HasSite(g,v.site.id));
            State.world.Say(a.id,Loc.Token(count==0?"refuge.found_empty":"refuge.found",room+1,count),"rest");
        }
        void ShareRooms(Agent a,GroupState g)
        {foreach(var other in State.Members(g).Where(o=>o!=a&&o.alive&&!o.escaped&&Simulation.Distance(a.x,a.z,o.x,o.z)<4))foreach(int room in State.Plan(a.id).knownRooms)if(!State.Plan(other.id).knownRooms.Contains(room))State.Plan(other.id).knownRooms.Add(room);}
    }
}
