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

    float mChange, oChange;
    float mChangeDir, oChangeDir;
    float magnitude = 1, offset = 0;
    int barCount;

    private void OnEnable() {
        barCount = transform.childCount;
        bars = new RectTransform[barCount];

        for (int i = 0; i < barCount; i++) {
            bars[i] = transform.GetChild(i).GetComponent<RectTransform>();
        }
    }

    private void Update() {
        if (Time.time >= mChange) {
            mChange = Time.time + Random.Range(minNoise, maxNoise);
            mChangeDir = Random.Range(-1f, 1f);
        }

        if (Time.time >= oChange) {
            oChange = Time.time + Random.Range(minNoise, maxNoise);
            oChangeDir = Random.Range(-1f, 1f);
        }

        magnitude = Mathf.Clamp(magnitude + mChangeDir * magnitudeChangeRate * Time.deltaTime, 0.1f, maxMagnitude);
        offset = Mathf.Clamp(offset + oChangeDir * offsetChangeRate * Time.deltaTime, -maxOffset, maxOffset);

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