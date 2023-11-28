using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

namespace Frontiers.Windows {
    public class WindowConsole : Window {
        public TMP_Text textRenderer;
        public TMP_InputField textInputField;

        public int maxCharacters = 400;
        public float characterTimeSpacing = 0.01f;

        [HideInInspector] public string queuedText = "";
        [HideInInspector] public string displayText = "";

        float prevTime = 0f;

        public override void Open(WindowHandler handler, short id, string name) {
            base.Open(handler, id, name);
        }

        protected override void Update() {
            base.Update();

            float time = Time.deltaTime + prevTime;
            int characters = Mathf.FloorToInt(time / characterTimeSpacing);

            prevTime = Mathf.Clamp01(time - characters * characterTimeSpacing);

            string textToDisplay = queuedText.Substring(0, characters);
            queuedText.Remove(0, characters);

            displayText += textToDisplay;

            int excessText = Mathf.Clamp(maxCharacters - displayText.Length, 0, maxCharacters);
            if (excessText > 0) displayText = displayText.Remove(displayText.Length - excessText, excessText - 1);

            textRenderer.text = displayText;
        }

        public void UserInputText(string text) {
            // Do some processing for commands and other things
            Queue(text);
        }

        public void Queue(string line) {
            queuedText += line;
        }

        public void Clear() {
            queuedText = displayText = "";
        }
    }
}
