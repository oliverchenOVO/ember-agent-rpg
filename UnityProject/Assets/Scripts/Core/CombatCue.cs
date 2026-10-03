using System;
using UnityEngine;
namespace Ember.Core.Phase2
{
    [Serializable] public class CombatCue
    {
        public CueShape shape;public float x,z,angle,radius,length,innerRadius,left;public bool interruptible;public string element;
        // Signed distance: negative inside the damaging shape, positive in safety.
        public float SignedDistance(float px,float pz)
        {
            float dx=px-x,dz=pz-z,c=Mathf.Cos(angle),s=Mathf.Sin(angle),u=dx*c+dz*s,v=-dx*s+dz*c;
            float Box(float a,float b){float qx=Mathf.Abs(a)-length*.5f,qz=Mathf.Abs(b)-radius;return Mathf.Sqrt(Mathf.Max(0,qx)*Mathf.Max(0,qx)+Mathf.Max(0,qz)*Mathf.Max(0,qz))+Mathf.Min(Mathf.Max(qx,qz),0);}
            if(shape==CueShape.Lane)return Box(u,v);if(shape==CueShape.Cross)return Mathf.Min(Box(u,v),Box(v,u));
            float d=Mathf.Sqrt(dx*dx+dz*dz);return shape==CueShape.Annulus?Mathf.Max(innerRadius-d,d-radius):d-radius;
        }
        public bool Contains(float px,float pz,float margin=0)
        {
            float dx=px-x,dz=pz-z,c=Mathf.Cos(angle),s=Mathf.Sin(angle),u=dx*c+dz*s,v=-dx*s+dz*c;
            switch(shape)
            {
                case CueShape.Lane:return Mathf.Abs(v)<radius+margin&&Mathf.Abs(u)<length*.5f+margin;
                case CueShape.Cross:return (Mathf.Abs(u)<radius+margin&&Mathf.Abs(v)<length*.5f+margin)||(Mathf.Abs(v)<radius+margin&&Mathf.Abs(u)<length*.5f+margin);
                case CueShape.Annulus:float d=Mathf.Sqrt(dx*dx+dz*dz);return d<radius+margin&&d>Mathf.Max(0,innerRadius-margin);
                default:return dx*dx+dz*dz<(radius+margin)*(radius+margin);
            }
        }
    }
}
