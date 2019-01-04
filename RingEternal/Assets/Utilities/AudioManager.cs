using UnityEngine.Audio;
using System;
using UnityEngine;
using System.Collections;

public class AudioManager : MonoBehaviour {

    public Sound[] sounds;
    // Use this for initialization
    public static AudioManager instance;
    float currFadeVol;


    void Awake () {
        if (instance == null)
        {
            instance = this;
        }
        else
        {
            Destroy(gameObject);
            return;
        }
        DontDestroyOnLoad(gameObject);
        foreach(Sound s in sounds)
        {
            s.source = gameObject.AddComponent<AudioSource>();
            s.source.clip = s.clip;
            s.source.volume = s.volume;
            s.source.loop = s.loop;
        }
	}

    public void Start()
    {
        Play("Theme");
    }

    public Sound FindValidAudio(string name)
    {
        Sound s = Array.Find(sounds, sound => sound.name == name);
        if (s!= null)
        {
            return s;
        }
        else
        {
            Debug.LogWarning("Could not find sound " + name);
            return sounds[0]; // Return the first item in sounds
        }
    }
    public void Play(string name)
    {
        Sound s = FindValidAudio(name);
        if(!s.source.isPlaying)
            s.source.Play();
    }

    public void StopPlaying(string name)
    {
        Sound s = FindValidAudio(name);
        s.source.Stop();
    }

    public void Pause(string name)
    {
        Sound s = FindValidAudio(name);
        if (s.source.isPlaying)
        {
            s.source.Pause();
        }
    }

    public void Unpause(string name)
    {
        Sound s = FindValidAudio(name);
        if (s.source.isPlaying)
        {
            s.source.UnPause();
        }
    }
    public IEnumerator FadeOut(string name, float FadeTime)
    {
        Sound s = FindValidAudio(name);
       
        float startVolume = s.source.volume;
        while(s.source.volume > 0)
        {
            s.source.volume -= startVolume * Time.deltaTime / FadeTime;
            yield return null;
        }
        s.source.Stop();
        s.source.volume = startVolume;

    }


    public IEnumerator FadeIn(string name, float FadeTime)
    {
        Sound s = FindValidAudio(name);

        float startVolume = s.volume;
        while (s.source.volume < s.volume)
        {
            s.source.volume += startVolume * Time.deltaTime / FadeTime;
            yield return null;
        }
        s.source.Play();
        //s.source.volume = startVolume;

    }

    public IEnumerator FadeOutAndIn(string fadeout, string fadein, float fadeTime)
    {
        Sound fadeOut = FindValidAudio(fadeout);
        Sound fadeIn = FindValidAudio(fadein);
        if (!fadeIn.source.isPlaying)
        {
            currFadeVol = 0;
            fadeIn.source.volume = currFadeVol;
            fadeIn.source.Play();
        }
        else
        {
            Debug.Log("Playing here");
            while (fadeIn.source.volume > currFadeVol || fadeOut.source.volume > currFadeVol)
            {
                Debug.Log("Current volume" + currFadeVol);
                currFadeVol += Time.deltaTime / fadeTime;
                fadeOut.source.volume -= currFadeVol;
                fadeIn.source.volume += currFadeVol;
                yield return null;
            }
            fadeOut.source.volume = 0f;
            //fadeIn.source.volume = origFadeInVol;
        }
    }
}


