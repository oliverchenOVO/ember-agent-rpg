using System;
using System.Collections.Generic;
using UnityEngine;
namespace Ember.Presentation
{
    public enum AudioEvent { Melee,Ranged,Spell,Heal,Buff,Debuff,BossCast,Phase,Warning,Death,Loot,Craft,Collapse,UI,Interrupt }
    public sealed class EmberAudio : MonoBehaviour
    {
        AudioSource events,ambient;readonly Dictionary<AudioEvent,AudioClip> clips=new Dictionary<AudioEvent,AudioClip>();AudioClip machine;bool foundry;
        void Awake()
        {
            events=gameObject.AddComponent<AudioSource>();events.volume=.06f;ambient=gameObject.AddComponent<AudioSource>();ambient.loop=true;ambient.volume=.025f;
            foreach(AudioEvent e in Enum.GetValues(typeof(AudioEvent)))clips.Add(e,Synthesize(e));
            int n=22050*8;var samples=new float[n];for(int i=0;i<n;i++){float t=i/22050f;samples[i]=Mathf.Sin(t*Mathf.PI*2*52)*.22f+Mathf.Sin(t*Mathf.PI*2*79)*.08f+Mathf.Sin(t*Mathf.PI*2*260)*Mathf.Pow(Mathf.Max(0,Mathf.Cos(t*Mathf.PI)),20)*.14f;}
            machine=AudioClip.Create("Foundry machine ambience",n,1,22050,false);machine.SetData(samples,0);
        }
        AudioClip Synthesize(AudioEvent e)
        {
            float frequency=e==AudioEvent.Heal?570:e==AudioEvent.Death?65:e==AudioEvent.Phase?90:e==AudioEvent.Warning?330:e==AudioEvent.Interrupt?720:120+(int)e*33;
            float seconds=e==AudioEvent.Phase?.7f:e==AudioEvent.Warning?.4f:.18f;int n=(int)(22050*seconds);var samples=new float[n];
            for(int i=0;i<n;i++){float t=i/22050f,env=Mathf.Exp(-t/seconds*6);float f=frequency*(e==AudioEvent.Interrupt?1-t/seconds*.7f:1);samples[i]=(Mathf.Sin(t*f*Mathf.PI*2)+Mathf.Sin(t*f*Mathf.PI*3)*.3f)*env*.3f;}
            var clip=AudioClip.Create("Ember event / "+e,n,1,22050,false);clip.SetData(samples,0);return clip;
        }
        public void Theme(bool isFoundry){if(foundry==isFoundry)return;foundry=isFoundry;if(foundry){ambient.clip=machine;ambient.Play();}else ambient.Stop();}
        public void Play(AudioEvent e){if(clips.TryGetValue(e,out var clip))events.PlayOneShot(clip);}
        void OnDestroy(){events.Stop();ambient.Stop();foreach(var clip in clips.Values)Destroy(clip);Destroy(machine);}
    }
    public sealed partial class WitnessGame
    {
        EmberAudio sound;int audioPhase=-1,audioCasts=-1,audioInterrupt=-1,audioGroup=-1;bool wasCasting;readonly bool[] aliveAudio={true,true,true,true};
        void UpdateAudio()
        {
            if(sound==null)sound=gameObject.AddComponent<EmberAudio>();
            if(tower==null)return;var g=tower.State.GroupOf(selected);var b=g.boss;sound.Theme(tower.Data.Floor(g.floor).theme=="astral_foundry");
            if(audioGroup!=g.id){audioGroup=g.id;audioPhase=b.phase;audioCasts=b.casts;audioInterrupt=b.interrupts;wasCasting=b.visible.telegraph;}
            if(audioPhase!=b.phase){sound.Play(AudioEvent.Phase);audioPhase=b.phase;}
            if(!wasCasting&&b.visible.telegraph)sound.Play(AudioEvent.Warning);wasCasting=b.visible.telegraph;
            if(audioCasts!=b.casts){sound.Play(AudioEvent.BossCast);audioCasts=b.casts;}
            if(audioInterrupt!=b.interrupts){sound.Play(AudioEvent.Interrupt);audioInterrupt=b.interrupts;}
            foreach(var a in tower.State.Members(g)){if(aliveAudio[a.id]&&!a.alive)sound.Play(AudioEvent.Death);aliveAudio[a.id]=a.alive;}
        }
    }
}
