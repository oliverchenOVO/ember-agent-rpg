using System.Collections;
using UnityEngine;
namespace Ember.Presentation
{
    public sealed partial class WitnessGame
    {
        bool quitting;
        bool CloseWindow(){if(quitting)return true;RequestQuit(0);return false;}
        void RequestQuit(int code)
        {
            if(quitting)return;quitting=true;paused=true;enabled=false;tower?.Dispose();llmTransport?.Dispose();view?.Dispose();
            if(audioSource!=null)audioSource.Stop();if(sound!=null)Destroy(sound);if(strike!=null)Destroy(strike);if(heal!=null)Destroy(heal);
            StartCoroutine(QuitAfterParticleDrain(code));
        }
        IEnumerator QuitAfterParticleDrain(int code){yield return new WaitForSecondsRealtime(1);Application.Quit(code);}
    }
}
