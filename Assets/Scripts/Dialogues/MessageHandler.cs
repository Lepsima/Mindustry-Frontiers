using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Frontiers.Content;
using Frontiers.Squadrons;
using Frontiers.Assets;
using System.Linq;

public class MessageHandler : MonoBehaviour {
    public struct StatusEvent {
        public string[] dialogues;
        public int priority;
        public float displayTime;
        public bool canCutOff;

        public StatusEvent(int priority, float displayTime, bool canCutOff, params string[] dialogues) {
            this.priority = priority;
            this.displayTime = displayTime;
            this.canCutOff = canCutOff;
            this.dialogues = dialogues;
        }

        public string RandomDialogue() {
            int randomIndex = Random.Range(0, dialogues.Length - 1);
            return string.Format(dialogues[randomIndex]);
        }
    }

    public struct Message {
        public static Message Empty = new() {
            sender = "",
            message = "",
            priority = -1,
            displayTime = 0f,
            canCutOff = true,
        };

        public string sender, message;
        public int priority;
        public float displayTime, validTimeSpan;
        public bool canCutOff;

        public Message(StatusEvent statusEvent, IMessager sender) {
            this.sender = sender.GetName();
            message = statusEvent.RandomDialogue();
            priority = statusEvent.priority;
            displayTime = statusEvent.displayTime;
            canCutOff = statusEvent.canCutOff;
            validTimeSpan = Time.time + statusEvent.displayTime * 2f;
        }

        public bool IsValid() => Time.time < validTimeSpan;
    }

    public int channels;

    // An array with all the avilable message displayers
    MessageDisplayerUI[] displayers;

    // A list with all the messages that need to be displayed
    List<Message> messages = new();

    public void Awake() {
        // Initialize array
        displayers = new MessageDisplayerUI[channels];
        GameObject displayerPrefab = AssetLoader.GetPrefab("UnitMessageDisplayerPrefab");

        // Create displayers
        for (int i = 0; i < channels; i++) displayers[i] = Instantiate(displayerPrefab, transform.parent).GetComponent<MessageDisplayerUI>();      
    }

    public void Update() {
        if (messages.Count == 0) return;

        // A list of each displayer's priority (Index of channel, priority of current message)
        List<(int, int)> channelPriority = new();

        for (int i = 0; i < channels; i++) {
            // Add this channel's message priority
            channelPriority.Add((i, displayers[i].Priority()));
        }

        // Order from least to hihgest displayer's priority
        channelPriority = channelPriority.OrderByDescending(x => x.Item2).ToList();

        for (int i = 0; i < channels; i++) {
            // Get the waiting message with the most priority (sorts when added to the list)
            Message message = messages[0];
            if (message.priority == -1) return;

            // Gets the displayer with the lowest priority message
            MessageDisplayerUI displayer = displayers[channelPriority[0].Item1];

            // If the highest waiting message isn't greater than the current displaying message, skip all
            if (message.priority < displayer.Priority()) return;

            // If the waiting message has higher priority, override the current message
            displayer.Show(message);

            // Remove used
            messages.RemoveAt(0);
            channelPriority.RemoveAt(0);
        }
    }

    public void HandleEvent(StatusEvent statusEvent, IMessager sender) {
        // Add new message and order by priority, and set the valid time span to double the display time
        messages.Add(new Message(statusEvent, sender));
        messages = messages.OrderBy(x => x.priority).ToList();

        // Remove all non-valid messages
        for (int i = messages.Count - 1; i >= 0; i--) if (!messages[i].IsValid()) messages.RemoveAt(i);
    }

    public Message GetNextMessage() {
        Message message = messages[0];

        while (messages.Count > 1 || !message.IsValid()) {
            messages.Remove(message);
            message = messages[0];
        }

        if (message.IsValid()) return message;
        return Message.Empty;
    }
}