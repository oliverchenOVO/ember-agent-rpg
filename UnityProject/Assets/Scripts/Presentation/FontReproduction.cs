using System;
using System.IO;
using System.Linq;
using Ember.Core;
using UnityEngine;

namespace Ember.Presentation
{
    // A bounded, isolated IMGUI reproduction; no world, particles, combat or save system.
    public sealed class FontReproduction : MonoBehaviour
    {
        public string mode,path;Font font;GUIStyle large,small;StreamWriter trace;float started;int stage,last=-1,draw;string corpus;
        void Start()
        {
            var camera=new GameObject("Font reproduction camera").AddComponent<Camera>();camera.clearFlags=CameraClearFlags.SolidColor;camera.backgroundColor=new Color(.12f,.12f,.12f);Directory.CreateDirectory(path);font=Resources.Load<Font>("Fonts/NotoSansCJKtc-Regular");started=Time.realtimeSinceStartup;Debug.Log("FONT DIAGNOSTIC / dynamic="+font.dynamic+" / material="+font.material.name);Application.targetFrameRate=60;
            large=new GUIStyle{font=font,fontSize=27,fontStyle=FontStyle.Bold,normal={textColor=Color.white}};
            small=new GUIStyle{font=font,fontSize=14,normal={textColor=Color.white}};
            corpus=string.Concat(new[]{"zh-TW","en"}.Select(locale=>Resources.Load<TextAsset>("Localization/"+locale).text).ToArray());
            corpus=new string(corpus.Distinct().ToArray());trace=new StreamWriter(Path.Combine(path,"rebuilds.csv"));trace.WriteLine("frame,event,stage,draw,width,height");Font.textureRebuilt+=Rebuilt;
            if(mode=="prewarm"){font.RequestCharactersInTexture(corpus,27,FontStyle.Bold);font.RequestCharactersInTexture(corpus,14,FontStyle.Normal);}
        }
        void Rebuilt(Font f){if(f!=font)return;var t=font.material.mainTexture;trace.WriteLine(Time.frameCount+","+(Event.current==null?"OutsideGUI":Event.current.type.ToString())+","+stage+","+draw+","+t.width+","+t.height);trace.Flush();}
        void Update()
        {
            stage=Mathf.Min(3,(int)((Time.realtimeSinceStartup-started)/2));
            if(last!=stage&&Time.realtimeSinceStartup-started>stage*2+1){last=stage;ScreenCapture.CaptureScreenshot(Path.Combine(path,"stage-"+stage+".png"));}
            if(Time.realtimeSinceStartup-started>9){Font.textureRebuilt-=Rebuilt;trace.Dispose();trace=null;Application.Quit(0);}
        }
        void OnGUI()
        {
            draw=0;GUI.matrix=Matrix4x4.Scale(new Vector3(Screen.width/1600f,Screen.height/900f,1));
            GUI.Label(new Rect(30,30,1200,60),"001    09 / 25    00:00    生命 154 / 魔力 47",large);draw++;
            GUI.Label(new Rect(30,100,1400,80),stage%2==0?"目前輪迴 / 挑戰時間 / 職業選擇 / 死者之書 / 根冠之主":"CURRENT LIFE / FLOOR / RUN TIME / BOOK OF THE DEAD",small);draw++;
            // Introduce the same localized glyph corpus over successive states, matching locale/content changes.
            string text=corpus.Substring(0,Mathf.Min(corpus.Length,100+stage*250));
            small.wordWrap=true;GUI.Label(new Rect(30,200,1400,600),text,small);draw++;
        }
        void OnDestroy(){Font.textureRebuilt-=Rebuilt;trace?.Dispose();}
    }
}
