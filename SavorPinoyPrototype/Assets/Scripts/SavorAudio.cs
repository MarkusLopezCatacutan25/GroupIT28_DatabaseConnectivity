using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

// Background music for each scene and short sound effects. Clips are in Resources/Audio
// (music and SFX provided by Heleinna Gaile Dichoso, see Docs/AUDIO-CREDITS.md).
// The Settings volume slider controls everything through AudioListener.volume.
public sealed class SavorAudio : MonoBehaviour
{
    private const float MusicVolume=.45f,LoopVolume=.5f;
    private static SavorAudio instance;
    private readonly Dictionary<string,AudioClip> clips=new Dictionary<string,AudioClip>();
    private AudioSource music,sfx,sizzle,boil;
    private string currentMusic;
    private Coroutine fade;
    private bool firstScene=true;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    private static void Install()
    {
        if(instance)return;
        var host=new GameObject("SavorAudio");DontDestroyOnLoad(host);instance=host.AddComponent<SavorAudio>();
    }
    private void Awake()
    {
        AudioListener.volume=PlayerPrefs.GetFloat("SavorPinoy.Volume",1);
        music=Source(true,0);sfx=Source(false,1);sizzle=Source(true,0);boil=Source(true,0);
        SceneManager.activeSceneChanged+=SceneChanged;
    }
    private void OnDestroy(){SceneManager.activeSceneChanged-=SceneChanged;}
    private AudioSource Source(bool loop,float volume){var s=gameObject.AddComponent<AudioSource>();s.loop=loop;s.volume=volume;s.playOnAwake=false;s.spatialBlend=0;return s;}
    private AudioClip Clip(string name)
    {
        if(!clips.TryGetValue(name,out AudioClip clip)){clip=Resources.Load<AudioClip>("Audio/"+name);clips[name]=clip;if(!clip)Debug.LogWarning("[SavorPinoy] Missing audio clip: "+name);}
        return clip;
    }

    // ---- Music ----
    private void SceneChanged(Scene from,Scene to)
    {
        if(!firstScene)Play("SfxWhoosh",.35f);firstScene=false;
        sizzle.Stop();sizzle.volume=0;boil.Stop();boil.volume=0;
        string track=to.name=="MainMenu"?"MusicMainMenu":to.name=="RegionalTriviaHub"?"MusicMapTrivia":to.name=="CookingStage"?"MusicCooking":currentMusic;
        if(track==null||track==currentMusic)return;
        currentMusic=track;if(fade!=null)StopCoroutine(fade);fade=StartCoroutine(SwapMusic(Clip(track)));
    }
    private IEnumerator SwapMusic(AudioClip next)
    {
        for(float v=music.volume;v>0;v-=Time.unscaledDeltaTime*1.5f){music.volume=v;yield return null;}
        music.Stop();music.clip=next;if(!next)yield break;music.volume=0;music.Play();
        for(float v=0;v<MusicVolume;v+=Time.unscaledDeltaTime*.6f){music.volume=v;yield return null;}
        music.volume=MusicVolume;fade=null;
    }

    // ---- Sound effects ----
    public static void Play(string name,float volume=1)
    {
        if(!instance)return;AudioClip clip=instance.Clip(name);if(clip)instance.sfx.PlayOneShot(clip,volume);
    }
    public static void Click(){Play("SfxClick",.7f);}

    // Cooking loops: sizzling while ingredients fry, bubbling while the dish simmers or steams.
    public static void SetLoops(bool frying,bool simmering,float flame)
    {
        if(!instance)return;
        instance.Loop(instance.sizzle,"SfxSizzle",frying,LoopVolume*Mathf.Lerp(.45f,1,flame));
        instance.Loop(instance.boil,"SfxBoil",simmering,LoopVolume*Mathf.Lerp(.5f,1,flame));
    }
    private void Loop(AudioSource source,string name,bool on,float volume)
    {
        if(on){if(!source.clip)source.clip=Clip(name);if(!source.clip)return;source.volume=Mathf.MoveTowards(source.volume,volume,Time.unscaledDeltaTime*1.5f);if(!source.isPlaying)source.Play();}
        else if(source.isPlaying){source.volume=Mathf.MoveTowards(source.volume,0,Time.unscaledDeltaTime*3f);if(source.volume<=0.01f)source.Stop();}
    }
}
