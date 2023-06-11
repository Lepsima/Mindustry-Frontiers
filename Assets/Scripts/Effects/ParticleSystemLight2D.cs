using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ParticleSystemLight2D : MonoBehaviour {
    [SerializeField] ParticleSystem triggerParticleSystem;
    [SerializeField] float fadeInTime, fadeOutTime;
    [SerializeField] float maxSize;


    public void OnPlay() {
        StartCoroutine(nameof(FadeCoroutine));
    }

    public IEnumerator FadeCoroutine() {
        float fadeInEndTime = fadeInTime + Time.time;
        float fadeOutEndTime = fadeOutTime + Time.time;
        float progress;

        while(Time.time < fadeInEndTime) {
            progress = fadeInEndTime - Time.time / fadeInTime;
            transform.localScale = Vector3.one * Mathf.Lerp(0, maxSize, progress);
            yield return null;
        }

        while (Time.time < fadeOutEndTime) {
            progress = fadeOutEndTime - Time.time / fadeOutTime;
            transform.localScale = Vector3.one * Mathf.Lerp(maxSize, 0, progress);
            yield return null;
        }
    }
}
