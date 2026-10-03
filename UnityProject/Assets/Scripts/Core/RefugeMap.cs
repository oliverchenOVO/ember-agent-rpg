using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
namespace Ember.Core.Phase2
{
    // One authoritative footprint for both navigation and rendered walls. Old saves retain the crossing.
    public sealed class RefugeMap
    {
        public const float MinX=-30,MaxX=30,MinZ=-20,MaxZ=20,Radius=.38f;
        public static readonly Vector2 Entry=new Vector2(-28,0),Exit=new Vector2(28,0);
        public readonly List<Rect> obstacles=new List<Rect>();
        public readonly Vector2[] rooms=new Vector2[6];
        readonly bool[] open=new bool[31*21];
        static readonly RefugeMap[] maps=new RefugeMap[2];
        public static RefugeMap For(GroupState g){int k=g.refugeVariant%2;return maps[k]??(maps[k]=new RefugeMap(k));}
        public static float Limit(RestDefinition r,GroupState g)=>g.refugeVersion==1?52+g.refugeVariant%3*4:r.collapseAfter;
        public static float Front(RestDefinition r,GroupState g)=> (g.refugeVersion==1?MinX:-10)+Mathf.Max(0,g.phaseClock-Limit(r,g))*r.collapseSpeed;
        RefugeMap(int variant)
        {
            for(int i=0;i<6;i++)
            {
                var c=rooms[i]=new Vector2(-20+i%3*16,i<3?-12:12);float door=c.y<0?c.y+4:c.y-4,back=c.y<0?c.y-4:c.y+4;
                obstacles.Add(new Rect(c.x-6,c.y-4,.6f,8));obstacles.Add(new Rect(c.x+5.4f,c.y-4,.6f,8));
                obstacles.Add(new Rect(c.x-6,back-.3f,12,.6f));
                obstacles.Add(new Rect(c.x-6,door-.3f,4,.6f));obstacles.Add(new Rect(c.x+2,door-.3f,4,.6f));
            }
            obstacles.Add(new Rect(-11,-6,2,10));obstacles.Add(new Rect(5,-4,2,10));
            obstacles.Add(new Rect(variant==0?-26:20,variant==0?7:-7,3,3));
            for(int i=0;i<open.Length;i++)open[i]=Walkable(Point(i));
        }
        public bool Walkable(Vector2 p)
        {
            if(p.x<MinX+Radius||p.x>MaxX-Radius||p.y<MinZ+Radius||p.y>MaxZ-Radius)return false;
            foreach(var r in obstacles)if(p.x>=r.xMin-Radius&&p.x<=r.xMax+Radius&&p.y>=r.yMin-Radius&&p.y<=r.yMax+Radius)return false;
            return true;
        }
        public bool Clear(Vector2 a,Vector2 b)
        {
            if(!Walkable(a)||!Walkable(b))return false;var delta=b-a;
            foreach(var r in obstacles)
            {
                float lo=0,hi=1;
                if(Slab(a.x,delta.x,r.xMin-Radius,r.xMax+Radius,ref lo,ref hi)&&Slab(a.y,delta.y,r.yMin-Radius,r.yMax+Radius,ref lo,ref hi))return false;
            }
            return true;
        }
        static bool Slab(float origin,float direction,float min,float max,ref float lo,ref float hi)
        {
            if(Mathf.Abs(direction)<.00001f)return origin>=min&&origin<=max;
            float a=(min-origin)/direction,b=(max-origin)/direction;
            lo=Mathf.Max(lo,Mathf.Min(a,b));hi=Mathf.Min(hi,Mathf.Max(a,b));return lo<=hi;
        }
        static Vector2 Point(int i)=>new Vector2(MinX+i%31*2,MinZ+i/31*2);
        int Nearest(Vector2 p)
        {
            int best=-1;float d=float.MaxValue;
            for(int i=0;i<open.Length;i++)if(open[i]){float q=(Point(i)-p).sqrMagnitude;if(q<d&&Clear(p,Point(i))){best=i;d=q;}}
            return best;
        }
        public List<Vector2> Path(Vector2 from,Vector2 to)
        {
            if(!Walkable(from)||!Walkable(to))return new List<Vector2>();
            if(Clear(from,to))return new List<Vector2>{to};
            int start=Nearest(from),end=Nearest(to);if(start<0||end<0)return new List<Vector2>();
            var previous=Enumerable.Repeat(-1,open.Length).ToArray();var queue=new Queue<int>();queue.Enqueue(start);previous[start]=start;
            while(queue.Count>0&&previous[end]<0)
            {int p=queue.Dequeue();foreach(int n in new[]{p%31>0?p-1:-1,p%31<30?p+1:-1,p>=31?p-31:-1,p<open.Length-31?p+31:-1})if(n>=0&&open[n]&&previous[n]<0&&Clear(Point(p),Point(n))){previous[n]=p;queue.Enqueue(n);}}
            if(previous[end]<0)return new List<Vector2>();var path=new List<Vector2>{to};for(int p=end;p!=start;p=previous[p])path.Add(Point(p));path.Add(Point(start));path.Reverse();
            // Smooth only segments that are collision-free for the Agent radius.
            for(int i=0;i<path.Count-2;i++)while(i+2<path.Count&&Clear(path[i],path[i+2]))path.RemoveAt(i+1);
            return path;
        }
        public float Distance(Vector2 from,Vector2 to){var path=Path(from,to);if(path.Count==0)return float.PositiveInfinity;float d=0;foreach(var p in path){d+=Vector2.Distance(from,p);from=p;}return d;}
        public int RoomForSite(int site,int variant)=>((site+variant)%9)%6;
        public Vector2 SitePosition(int site,int variant){int slot=(site+variant)%9;return rooms[slot%6]+new Vector2(slot<6?-2:2,0);}
        public RestSite[] Sites(RestDefinition r,GroupState g)
        {return r.sites.Select((s,i)=>{var p=SitePosition(i,g.refugeVariant);return new RestSite{id=s.id,nameKey=s.nameKey,effect=s.effect,x=p.x,z=p.y,seconds=s.seconds,risk=s.risk,reward=s.reward,materialCost=s.materialCost};}).ToArray();}
    }
}
