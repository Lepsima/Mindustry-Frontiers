using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Frontiers.Content;
using Frontiers.Squadrons;
using Frontiers.Assets;
using System.Linq;

public static class UnitMessageManager {
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
            message = "",
            priority = -1,
            displayTime = 0f,
            canCutOff = true,
            sender = null,
        };

        public string message;
        public int priority;
        public float displayTime;
        public bool canCutOff;
        public Unit sender;

        public Message(StatusEvent statusEvent, Unit unit) {
            message = statusEvent.RandomDialogue();
            priority = statusEvent.priority;
            displayTime = statusEvent.displayTime;
            canCutOff = statusEvent.canCutOff;
            sender = unit;
        }
    }

    public static StatusEvent
        Waiting = new(0, 3f, true, "Waiting for orders.", "Awaiting orders.", "Ready.", "Awaiting further instructions.", "Awaiting instructions.", "Standby for orders."),
        Moving = new(1, 2.5f, true, "On the way.", "Moving to objective.", "On the move.", "Advancing to position.", "Moving out"),
        Fleeing = new(2, 3.5f, false, "Moving away from objective.", "Moving away.", "Retreating!", "Evacuating!"),
        TakingOff = new(0, 2f, true, "Taking off.", "Leaving landing pad.", "Engaging thrusters.", "Clear for takeoff."),
        Landing = new(2, 3.5f, true, "Landing.", "Returned to base.", "On the landing zone.", "Touching down safely.", "Back on solid ground."),
        InTarget = new(1, 2f, true, "On target.", "Arrived.", "Arrived to destination.", "On station."),
        Damaged = new(3, 3f, false, "Got hit!", "Taking damage!", "Recieving Damage!"),
        Destroyed = new(5, 4f, false, "Going down!", "Reciving Critical Damage!", "Critical systems destroyed!"),
        TargetAdquired = new(4, 2f, false, "Engaging Target.", "Engaging.", "Target Found.", "New target adquired.", "Target in sigth.", "Locked in, ready to fire", "Target in bound"),
        TargetLost = new(2, 2f, false, "Target lost.", "Lost my target."),
        TargetDestroyed = new(3, 2f, false, "Target destroyed.", "Target eliminated.", "Eliminated");

    static int channels;

    static (Message, float)[] messages;
    static UnitMessageUI[] messageDisplayers;

    static List<Message> pendingMessages = new();

    public static void Init(Transform parent, int channels) {
        UnitMessageManager.channels = channels;

        // Initialize array
        messages = new (Message, float)[channels];

        // Create empty placeholders
        for (int i = 0; i < channels; i++) { 
            messages[i] = (Message.Empty, 0f);
            messageDisplayers[i] = Object.Instantiate(AssetLoader.GetPrefab("UnitMessageDisplayerPrefab"), parent).GetComponent<UnitMessageUI>();
        }
    }

    public static void Update() {
        if (pendingMessages.Count == 0) return;

        // (Index of channel, priority of current message)
        List<(int, int)> channelPriority = new();

        for (int i = 0; i < channels; i++) {
            // Hide if the display time has expired
            if (messages[i].Item2 >= Time.time) messages[i] = (Message.Empty, 0f);

            // Add this channel's message priority
            channelPriority.Add((i, messages[i].Item1.priority));
        }

        // Order from least to hihgest priority
        channelPriority = channelPriority.OrderByDescending(x => x.Item2).ToList();

        for (int i = 0; i < channels; i++) {
            Message message = pendingMessages[0];

        }
    }

    public static void HandleEvent(StatusEvent statusEvent, Unit unit) {
        // Add new message and order by priority
        pendingMessages.Add(new Message(statusEvent, unit));
        pendingMessages = pendingMessages.OrderBy(x => x.priority).ToList();
    }

    public static void DisplayMessage(int channelIndex, Message message) {
        // Calculate the time at wich the message should hide
        float endTime = message.displayTime + Time.time;

        // Set channel message
        messages[channelIndex] = new(message, endTime);
    }
}