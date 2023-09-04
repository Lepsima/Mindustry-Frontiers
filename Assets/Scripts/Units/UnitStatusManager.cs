using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Frontiers.Content;

public static class UnitStatusManager {
    public struct StatusEvent {
        public string[] dialogues;
        public int priority;

        public StatusEvent(int priority, params string[] dialogues) {
            this.priority = priority;
            this.dialogues = dialogues;
        }

        public string RandomDialogue(params string[] args) {
            int randomIndex = Random.Range(0, dialogues.Length - 1);
            return string.Format(dialogues[randomIndex], args);
        }
    }

    public static StatusEvent
        Waiting = new(0, "Waiting for orders.", "Awaiting orders.", "Ready.", "Awaiting further instructions.", "Awaiting instructions.", "Standby for orders."),
        Moving = new(0, "On the way.", "Moving to objective.", "On the move.", "Advancing to position.", "Moving out"),
        Fleeing = new(0, "Moving away from objective.", "Moving away.", "Retreating!", "Evacuating!"),
        TakingOff = new(0, "Taking off.", "Leaving landing pad.", "Engaging thrusters.", "Clear for takeoff."),
        Landing = new(0, "Landing.", "Returned to base.", "On the landing zone.", "Touching down safely.", "Back on solid ground."),
        InTarget = new(0, "Arrived to destination.", "On station."),
        Damaged = new(0, "Got hit!", "Taking damage!", "Recieving Damage!"),
        Destroyed = new(0, "Going down!", "Reciving Critical Damage!", "Critical systems destroyed!"),
        TargetAdquired = new(0, "Engaging Target.", "Engaging.", "Target Found.", "New target adquired.", "Target in sigth.", "Locked in, ready to fire", "Target in bound"),
        TargetLost = new(0, "Target lost.", "Lost my target."),
        TargetDestroyed = new(0, "Target destroyed.", "Target eliminated.", "Eliminated");
} 