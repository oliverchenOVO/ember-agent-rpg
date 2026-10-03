using System.Collections.Generic;
using System.Linq;
using Ember.Core;
using Ember.Core.Phase2;
using UnityEngine;
namespace Ember.Presentation
{
    public sealed partial class WitnessGame
    {
        int legacyPreview,legacyPreviewAgent=-1;
        void DrawBookPreview(World w)
        {
            if(legacyPreviewAgent!=selected){legacyPreviewAgent=selected;legacyPreview=0;}
            var pages=w.book.Where(e=>e.author==selected).Reverse().ToArray();legacyPreview=Mathf.Clamp(legacyPreview,0,Mathf.Max(0,pages.Length-1));
            if(Button(1020,713,201,Loc.T("codex.open_book")))OpenInspection(Inspection.Book);
            if(pages.Length==0){Text(46,755,1140,58,Loc.T("readability.no_legacy",w.agents[selected].name));return;}
            if(pages.Length>1){if(Button(690,713,145,Loc.T("readability.previous")))legacyPreview=Mathf.Max(0,legacyPreview-1);if(Button(845,713,145,Loc.T("readability.next")))legacyPreview=Mathf.Min(pages.Length-1,legacyPreview+1);}
            var e=pages[legacyPreview];Text(46,718,625,25,Loc.T("readability.book_entry",e.run,w.agents[e.author].name,legacyPreview+1,pages.Length),subtitle);Text(46,755,1140,68,RenderForUI(e.text).Split('\n')[0],small);
        }
        internal static List<string> DamageDescriptions(TowerContent data,GroupState g,Agent a)
        {
            var lines=new List<string>();if(g.phase!=Phase.Battle||!a.alive||a.escaped)return lines;var b=g.boss;var d=data.Boss(b.definition);var p=d.phases[b.phase];
            float pressure=d.ambientPressure*(b.recoveryWindow>0?.2f:1)*(1+Mathf.Max(0,b.elapsed-p.enrageAfter)/20);
            if(pressure>0)lines.Add(Loc.T("readability.pressure",pressure.ToString("F1")));
            if(b.elapsed>240)lines.Add(Loc.T("readability.attrition",(a.MaxHp*(b.elapsed-240)*.015f).ToString("F1")));
            var burns=b.statuses.Where(s=>s.agent==a.id&&s.left>0&&s.kind=="Burn").ToArray();if(burns.Length>0)lines.Add(Loc.T("readability.burn",burns.Sum(s=>s.power).ToString("F1"),burns.Max(s=>s.left).ToString("F1")));
            var doom=b.statuses.Where(s=>s.agent==a.id&&s.left>0&&s.kind=="Doom").OrderBy(s=>s.left).FirstOrDefault();if(doom!=null)lines.Add(Loc.T("readability.doom",doom.left.ToString("F1")));

            if(b.hazardLeft>0&&Simulation.Distance(a.x,a.z,b.hazardX,b.hazardZ)<3)lines.Add(Loc.T("readability.hazard",b.hazardLeft.ToString("F1")));
            return lines;
        }
        void DrawOngoingDamage(World w)
        {
            if(tower==null||w.phase!=Phase.Battle||inspection!=Inspection.None)return;var g=tower.State.GroupOf(selected);var a=w.agents[selected];var lines=DamageDescriptions(tower.Data,g,a);
            // Summons attack the first living, non-escaped member, matching BossRuntime.Tick.
            if(g.boss.adds>0&&tower.State.Members(g).FirstOrDefault(v=>v.alive&&!v.escaped)==a)lines.Add(Loc.T("readability.adds",g.boss.adds,g.boss.adds*2));
            if(lines.Count==0)return;string text=Loc.T("readability.damage_header",a.name)+"\n"+string.Join(" / ",lines)+"\n"+Loc.T("readability.raw_note");float height=Mathf.Max(64,small.CalcHeight(new GUIContent(text),565)+12);
            Box(425,258,590,height,new Color(.16f,.06f,.045f,.92f));Text(438,264,565,height-12,text,small);
        }
    }
    public sealed partial class WorldView
    {
        Transform pressureAura;readonly Transform[] burningMarkers=new Transform[4];
        public bool PressureAuraVisible=>pressureAura!=null&&pressureAura.gameObject.activeInHierarchy;
        void UpdateDamageReadability(World w)
        {
            bool battle=observedGroup!=null&&w.phase==Phase.Battle&&observedGroup.boss.visible.hp>0;var b=observedGroup?.boss;
            if(pressureAura==null)pressureAura=Ring("Persistent arena pressure aura (not a dodge cue)",arena,Vector3.zero,2.5f,.12f,soulArt??red);
            pressureAura.gameObject.SetActive(battle&&artData.Boss(b.definition).ambientPressure>0);if(battle){pressureAura.localPosition=new Vector3(b.x,1.2f,b.z);pressureAura.localScale=Vector3.one*(1+.08f*Mathf.Sin(Time.unscaledTime*3));}
            for(int i=0;i<4;i++)
            {
                if(burningMarkers[i]==null)burningMarkers[i]=Ring("Ongoing damage marker",world,Vector3.zero,.7f,.07f,red);
                bool burning=battle&&w.agents[i].alive&&!w.agents[i].escaped&&observedGroup.members.Contains(i)&&b.statuses.Exists(s=>s.agent==i&&s.kind=="Burn"&&s.left>0);
                burningMarkers[i].gameObject.SetActive(burning);if(burning){burningMarkers[i].position=new Vector3(w.agents[i].x,.6f+.2f*Mathf.Sin(Time.unscaledTime*5),w.agents[i].z);burningMarkers[i].localRotation=Quaternion.Euler(25,Time.unscaledTime*60,0);}
            }
        }
    }
}
