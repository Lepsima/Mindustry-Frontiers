using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SpriteRotaionShadowEffect : MonoBehaviour {
    public float rotation;

    public SpriteRenderer normal;
    public SpriteRenderer inverted;

    private void Update() {
        normal.transform.eulerAngles = new Vector3(0, 0, rotation);
        inverted.transform.eulerAngles = new Vector3(0, 0, rotation + 180f);

        float percent = GetPercent(rotation);

        SetAlpha(normal, percent);
        SetAlpha(inverted, 1f - percent);
    }

    public void SetAlpha(SpriteRenderer renderer, float alpha) {
        Color color = renderer.color;
        color.a = alpha;
        renderer.color = color;
    }

    public float GetPercent(float rotation) {
        float a = Mathf.Sign(rotation % 360f - 180f);
        float b = rotation % 180f - 90f;
        return (a * b + 90f) / 180;
    }
}