using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using Photon.Realtime;
using Photon.Pun;
using Frontiers.Teams;
using System.Linq;

namespace Frontiers.Windows {
    public class WindowConsole : Window {
        public TMP_Text textRenderer;
        public TMP_InputField inputField;

        public int maxCharacters = 2500;
        public float characterTimeSpacing = 0.015f;

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

            text = text.Trim().ToLower();

            string[] parameters = text.Split(" ");
            string command = parameters[0];

            // This might be a bad way to implement commands, but i dont want to start debugging the debug console just for +1ms
            switch(command) {
                case "help":
                    Queue("List of commands (case insensitive):" +
                        "\n Help: shows a list of commands" +
                        "\n ImConnected: returns the network status" +
                        "\n ListPlayers: returns the list of players in the same room" +
                        "\n ExitMatch: alt + F4 but cooler, mostly for some laptop users" +
                        "\n InstantConsole [boolean]: enables/disables the text animation" +
                        "\n Clear: clears all the text in the console and queue" +
                        "\n ClearQueue: clears all the text that is waiting to be displayed");
                    break;

                case "imconnected":
                    Queue(PhotonNetwork.IsConnected ? "You are connected!" : "You are offline!");
                    break;

                case "listplayers":
                    if (!PhotonNetwork.IsConnected || !PhotonNetwork.InRoom) {
                        Queue("ERROR: You are not in a room, join or create one to see the list of players");
                        break;
                    }

                    foreach (Player player in PhotonNetwork.PlayerList) {
                        Queue(player.NickName + " => " + (TeamUtilities.TryGetTeamMembers(0).Contains(player) ? "Team 0" : "Team 1") + "\n");
                    }
                    break;

                case "exitmatch":
                    break;

                case "instantconsole":
                    bool state = bool.Parse(parameters[1]);
                    characterTimeSpacing = state ? 0.00000000001f : 0.015f;
                    break;

                case "clear":
                    Clear();
                    break;

                case "clearqueue":
                    ClearQueue();
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
