using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class UITextDisplayTool : MonoBehaviour {
    public bool runOnEnable = false;
    public string text = "";
    public int effect = 0;
    public float duration = 3f;

    private TMP_Text textRenderer;
    const string AllowedChars = "0123456789ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz#@$^*()";

    private void Awake() {
        textRenderer = GetComponent<TMP_Text>();
    }

    private void OnEnable() {
        if (runOnEnable) ChangeDisplayText(text, effect, duration);
    }

    public void ChangeDisplayText(string text, int effect = 0, float duration = 3f) {
        if (effect != 0) StartCoroutine(TextDisplayCoroutine(text, effect, duration));
        else textRenderer.text = text;
    }

    public IEnumerator TextDisplayCoroutine(string text, int effect, float duration) {
        float transitionEndTime = Time.time + duration;

        while (transitionEndTime > Time.time) {
            float percentLeft = (transitionEndTime - Time.time) / duration;

            int effectTextLength = Mathf.RoundToInt(percentLeft * text.Length);
            textRenderer.text = text.Substring(0, text.Length - effectTextLength) + GetRandomString(effectTextLength);

            yield return null;
        }
    }

    public string GetRandomString(int lenght) {
        string returnString = "";
        for (int i = 0; i < lenght; i++) returnString += AllowedChars[Random.Range(0, AllowedChars.Length)];
        return returnString;
    }
}