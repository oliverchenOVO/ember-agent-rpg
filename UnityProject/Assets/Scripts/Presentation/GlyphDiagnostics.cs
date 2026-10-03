using System;
using System.IO;
using Ember.Core;
using UnityEngine;

namespace Ember.Presentation
{
    public sealed partial class WitnessGame
    {
        StreamWriter glyphTrace; int glyphDrawIndex;
        void StartGlyphDiagnostics(string[] args)
        {
            if(Array.IndexOf(args,"--glyph-trace")<0)return;
            Directory.CreateDirectory(artifactPath);glyphTrace=new StreamWriter(Path.Combine(artifactPath,"glyph-rebuilds.csv"));glyphTrace.WriteLine("utc,frame,event,locale,draw_index,texture_width,texture_height");Font.textureRebuilt+=GlyphRebuilt;
        }
        void GlyphRebuilt(Font font)
        {
            if(font!=uiFont||glyphTrace==null)return;var texture=font.material.mainTexture;
            glyphTrace.WriteLine(DateTime.UtcNow.ToString("O")+","+Time.frameCount+","+(Event.current==null?"OutsideGUI":Event.current.type.ToString())+","+Loc.Locale+","+glyphDrawIndex+","+texture.width+","+texture.height);glyphTrace.Flush();
        }
        void CloseGlyphDiagnostics(){Font.textureRebuilt-=GlyphRebuilt;glyphTrace?.Dispose();glyphTrace=null;}
    }
}
