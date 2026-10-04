using Ember.Core;
using Ember.Core.Phase2;
using UnityEngine;
namespace Ember.Presentation
{
    public sealed partial class WorldView
    {
        readonly Transform[] pressureRoots=new Transform[3],pressureCrystals=new Transform[3],pressureWarnings=new Transform[3],pressurePulses=new Transform[3];
        public int VisiblePressureCores { get {int n=0;foreach(var t in pressureRoots)if(t!=null&&t.gameObject.activeInHierarchy)n++;return n;} }
        public int VisiblePressureWarnings { get {int n=0;foreach(var t in pressureWarnings)if(t!=null&&t.gameObject.activeInHierarchy)n++;return n;} }
        public int VisiblePressurePulses { get {int n=0;foreach(var t in pressurePulses)if(t!=null&&t.gameObject.activeInHierarchy)n++;return n;} }
        Material pressureAmber;
        void UpdatePressureArt(World w)
        {
            var b=observedGroup?.boss;bool battle=observedGroup!=null&&w.phase==Phase.Battle&&b.visible.hp>0;
            for(int i=0;i<3;i++)
            {
                if(pressureRoots[i]==null){
                    if(pressureAmber==null)pressureAmber=Mat(new Color(1,.65f,.06f),.2f,.5f,1.4f);
                    var r=pressureRoots[i]=Pivot("Destructible pressure source "+i,world,Vector3.zero);
                    Part("Hexagonal emitter base",PrimitiveType.Cylinder,r,new Vector3(0,.28f,0),new Vector3(1.3f,.28f,1.3f),dark);
                    Ring("Emitter rim",r,new Vector3(0,.58f,0),.6f,.09f,brass,36);
                    pressureCrystals[i]=Crystal("Power crystal",r,new Vector3(0,1.4f,0),new Vector3(.65f,1.25f,.65f),red);
                    for(int j=0;j<3;j++){float a=j*Mathf.PI*2/3;Part("Emitter brace",PrimitiveType.Cube,r,new Vector3(Mathf.Cos(a)*.62f,.9f,Mathf.Sin(a)*.62f),new Vector3(.16f,1.3f,.16f),brass);}
                    pressureWarnings[i]=Ring("Three-second warning: authoritative radius",r,new Vector3(0,.085f,0),PressureField.Radius,.09f,pressureAmber);
                    var pulse=pressurePulses[i]=Pivot("Active ground pulse",r,Vector3.zero);
                    Ring("Damage boundary: authoritative radius",pulse,new Vector3(0,.09f,0),PressureField.Radius,.14f,red);
                    for(int j=1;j<=3;j++)Ring("Pulse wave "+j,pulse,new Vector3(0,.075f,0),j*.75f,.055f,red,64);
                }
                bool exists=battle&&b.pressureCores!=null&&i<b.pressureCores.Count;pressureRoots[i].gameObject.SetActive(exists);if(!exists)continue;
                var n=b.pressureCores[i];pressureRoots[i].localPosition=new Vector3(n.x,0,n.z);
                bool live=n.hp>0;pressureWarnings[i].gameObject.SetActive(live&&n.stage==0);pressurePulses[i].gameObject.SetActive(live&&n.stage==1);
                // Only inner waves animate. The authoritative radius never expands or shrinks.
                for(int j=1;j<pressurePulses[i].childCount;j++)pressurePulses[i].GetChild(j).localScale=Vector3.one*(.15f+.85f*Mathf.Repeat(Time.unscaledTime*.8f+j/3f,1));
                pressureCrystals[i].localPosition=new Vector3(0,live?1.4f:.65f,0);pressureCrystals[i].localRotation=Quaternion.Euler(live?0:75,Time.unscaledTime*30,0);
                pressureCrystals[i].localScale=live?new Vector3(.65f,1.25f,.65f)*( .65f+.35f*n.hp/n.maxHp):new Vector3(.45f,.45f,.45f);
                pressureCrystals[i].GetComponent<MeshRenderer>().sharedMaterial=live?(n.stage==1?red:n.stage==0?pressureAmber:glow):stone;
            }
        }
    }
}
