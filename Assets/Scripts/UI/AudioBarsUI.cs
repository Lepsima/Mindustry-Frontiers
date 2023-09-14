using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class AudioBarsUI : MonoBehaviour {
    public RectTransform[] bars;
    public float minBarHeight = 10f, maxBarHeight = 37f;
    [Space]
    public float minNoise = 0.025f;
    public float maxNoise = 0.05f;
    public float maxMagnitude = 3f, magnitudeChangeRate = 1f, maxOffset = 0.5f, offsetChangeRate = 0.25f;

    float magChange, offChange;
    float magChangeDir, offChangeDir;
    float magnitude = 1, offset = 0;
    int barCount;

    float stopTimer = -1f, hideTimer = -1f;

    public void Play(float time) {
        stopTimer = Time.time + time;
        hideTimer = Time.time + Mathf.Min(0.5f, time / 5f);
    }

    private void Start() {
        barCount = transform.childCount;
        bars = new RectTransform[barCount];

        for (int i = 0; i < barCount; i++) {
            bars[i] = transform.GetChild(i).GetComponent<RectTransform>();
        }

        Play(5f);
    }

    private void Update() {
        // If stopped, reset values and return
        if (stopTimer < Time.time) {
            magnitude = 0.1f;
            offset = -maxOffset;
            return;
        }

        if (Time.time >= magChange) {
            magChange = Time.time + Random.Range(minNoise, maxNoise);
            magChangeDir = Random.Range(-1f, 1f);
        }

        if (Time.time >= offChange) {
            offChange = Time.time + Random.Range(minNoise, maxNoise);
            offChangeDir = Random.Range(-1f, 1f);
        }

        // If is hiding, start to lower values
        if (hideTimer < Time.time) {
            offChangeDir = -Mathf.Abs(offChangeDir);
            magChangeDir = -Mathf.Abs(magChangeDir);
        }

        magnitude = Mathf.Clamp(magnitude + magChangeDir * magnitudeChangeRate * Time.deltaTime, 0.1f, maxMagnitude);
        offset = Mathf.Clamp(offset + offChangeDir * offsetChangeRate * Time.deltaTime, -maxOffset, maxOffset);

        for (int i = 0; i < barCount; i++) {
            float value = GetValue(i, offset, magnitude);
            bars[i].sizeDelta = new Vector2(bars[i].sizeDelta.x, value);
        }
    }

    private float GetValue(float xPos, float offset, float magnitude) {
        float xVal = xPos * (Mathf.PI / barCount);
        float j = Mathf.Abs(Mathf.Sin(xVal)) * magnitude;
        float k = Mathf.Abs(Mathf.Sin(xVal - offset)) * magnitude;
        return (j + k) / 2f * maxBarHeight;
    }
}