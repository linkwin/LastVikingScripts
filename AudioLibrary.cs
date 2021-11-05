using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(menuName = "Audio Library")]
public class AudioLibrary : ScriptableObject
{
    public AudioClip[] library;

    public AudioClip GetRandomClip()
    {
        return library[Random.Range(0, library.Length - 1)];
    }

    public void PlayRandomClip(AudioSource source)
    {

        if (source)
            source.PlayOneShot(library[Random.Range(0, library.Length)]);

    }

}
