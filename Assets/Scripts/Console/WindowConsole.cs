using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

namespace Frontiers.Windows {
    public class WindowConsole : Window {
        public TMP_Text textRenderer;
        public TMP_InputField inputField;

        public int maxCharacters = 2500;
        public float characterTimeSpacing = 0.01f;

        public string queuedText = "";
        [HideInInspector] public string displayText = "";

        float prevTime = 0f;

        public override void Open(WindowHandler handler, short id, string name) {
            base.Open(handler, id, name);
        }

        protected override void Update() {
            base.Update();

            float time = Time.deltaTime + prevTime;
            int characters = Mathf.Min(queuedText.Length, Mathf.FloorToInt(time / characterTimeSpacing));

            prevTime = queuedText.Length == 0 ? 0 : Mathf.Clamp01(time - characters * characterTimeSpacing);

            if (characters > 0) {
                string textToDisplay = queuedText.Substring(0, characters);
                queuedText = queuedText.Remove(0, characters);

                displayText += textToDisplay;
            } 

            int excessText = Mathf.Clamp(displayText.Length - maxCharacters, 0, maxCharacters);
            if (excessText > 0) displayText = displayText.Remove(0, excessText);

            textRenderer.text = displayText;
        }

        public void UserInputText(string text) {
            string displayText = text;

            if (!EndsInNewLine()) displayText = "\n" + displayText;
            Queue(displayText + "\n");

            inputField.text = "";

            text = text.Trim();

            string[] parameters = text.Split(" ");
            string command = parameters[0];

            // This might be a bad way to implement commands, but i dont want to start debugging the debug console just for +1ms
            switch(command) {
                case "Hello":
                    Queue("Hi!");
                    break;

                case "Clear":
                    Clear();
                    break;

                case "ClearQueue":
                    ClearQueue();
                    break;

                case "CoolAnimation":
                    Queue("Loading " +
                        "\n.               " +
                        "\n.               " +
                        "\n.               " +
                        "\n.               " +
                        "\n.               " +
                        "\n.               " +
                        "\n.               " +
                        "\nFinished Loading!");
                    break;
            }
        }

        public void Queue(string line) {
            queuedText += line;
        }

        public void Clear() {
            queuedText = displayText = "";
        }

        public void ClearQueue() {
            queuedText = "";
        }

        public bool EndsInNewLine() {
            return queuedText.Length > 0 ? queuedText.EndsWith("\n") : displayText.EndsWith("\n");
        }
    }
}
