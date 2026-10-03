using UnityEngine;
using Ember.Core;
namespace Ember.Presentation
{
    public sealed partial class WitnessGame
    {
        int cameraDrag=-1;Vector2 cameraMouse;
        void UpdateRefugeMouse(World w)
        {
            if(inspectionCameraQA&&towerQA)return;
            if(w.phase!=Phase.Rest||tower==null||tower.State.GroupOf(selected).refugeVersion!=1){cameraDrag=-1;view.RefugeManual=false;return;}
            if(inspection!=Inspection.None){cameraDrag=-1;return;}
            float scale=Mathf.Min(Screen.width/1600f,Screen.height/900f);Vector2 p=new Vector2((Input.mousePosition.x-(Screen.width-1600*scale)/2)/scale,(Screen.height-Input.mousePosition.y-(Screen.height-900*scale)/2)/scale);
            bool area=new Rect(330,253,943,400).Contains(p)&&!new Rect(1014,462,244,183).Contains(p);
            if(cameraDrag>=0&&!Input.GetMouseButton(cameraDrag))cameraDrag=-1;
            if(area&&Input.GetMouseButtonDown(0)){cameraDrag=0;cameraMouse=p;}
            else if(area&&Input.GetMouseButtonDown(1)){cameraDrag=1;cameraMouse=p;}
            Vector2 delta=cameraDrag>=0?p-cameraMouse:Vector2.zero;cameraMouse=p;
            view.ObserveRefugeCamera(area?Input.mouseScrollDelta.y:0,cameraDrag==0?delta:Vector2.zero,cameraDrag==1?delta:Vector2.zero);
        }
    }
    public sealed partial class WorldView
    {
        public bool RefugeManual;public Vector3 RefugePivot;public float RefugeYaw,RefugePitch,RefugeDistance;
        public void ResetRefugeCamera(){RefugeManual=false;}
        public void ObserveRefugeCamera(float wheel,Vector2 rotate,Vector2 pan)
        {
            if(wheel==0&&rotate==Vector2.zero&&pan==Vector2.zero)return;
            if(!RefugeManual){RefugePivot=RefugeOverview?new Vector3(-1,.5f,0):new Vector3(camera.transform.position.x,0,camera.transform.position.z)+camera.transform.forward*(-camera.transform.position.y/camera.transform.forward.y);RefugePivot.y=.5f;Vector3 offset=camera.transform.position-RefugePivot;RefugeDistance=offset.magnitude;RefugeYaw=Mathf.Atan2(offset.x,-offset.z)*Mathf.Rad2Deg;RefugePitch=Mathf.Asin(Mathf.Clamp(offset.y/RefugeDistance,-1,1))*Mathf.Rad2Deg;RefugeManual=true;}
            RefugeYaw=Mathf.Repeat(RefugeYaw+rotate.x*.3f,360);RefugePitch=Mathf.Clamp(RefugePitch+rotate.y*.25f,20,80);RefugeDistance=Mathf.Clamp(RefugeDistance*Mathf.Exp(-wheel*.12f),8,100);
            var right=Quaternion.Euler(0,-RefugeYaw,0)*Vector3.right;var forward=Vector3.Cross(right,Vector3.up);RefugePivot+=(-right*pan.x+forward*pan.y)*(RefugeDistance*.0015f);RefugePivot=new Vector3(Mathf.Clamp(RefugePivot.x,-30,30),.5f,Mathf.Clamp(RefugePivot.z,-20,20));
        }
        void DrawManualRefugeCamera(){float yaw=RefugeYaw*Mathf.Deg2Rad,pitch=RefugePitch*Mathf.Deg2Rad;camera.transform.position=RefugePivot+new Vector3(Mathf.Sin(yaw)*Mathf.Cos(pitch),Mathf.Sin(pitch),-Mathf.Cos(yaw)*Mathf.Cos(pitch))*RefugeDistance;camera.transform.LookAt(RefugePivot);camera.fieldOfView=48;}
    }
}
