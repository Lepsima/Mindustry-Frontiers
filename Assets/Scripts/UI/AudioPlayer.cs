using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class AudioPlayer : MonoBehaviour {
    public AudioClip[] audioClips;
    public AudioSource audioSource;

    public void PlayAudio(int index) {
        AudioClip clip = audioClips[index];
        audioSource.clip = clip;
        audioSource.Play();
    }
}