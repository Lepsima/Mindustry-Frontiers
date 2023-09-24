using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using static MessageHandler;

public static class UnitMessages {
    public static StatusEvent
         Waiting = new(0, 3f, true, "Waiting for orders.", "Awaiting orders.", "Ready.", "Awaiting further instructions.", "Awaiting instructions.", "Standby for orders."),
         Moving = new(1, 2.5f, true, "On the way.", "Moving to objective.", "On the move.", "Advancing to position.", "Moving out"),
         Refuel = new(2, 3.5f, false, "Fuel low, coming back", "Going to refuel!", "Low fuel, going to refuel"),
         TakingOff = new(0, 2f, true, "Taking off.", "Leaving landing pad.", "Engaging thrusters.", "Clear for takeoff."),
         Landing = new(2, 3.5f, true, "Landing.", "Returned to base.", "On the landing zone.", "Touching down safely.", "Back on solid ground."),
         InTarget = new(1, 2f, true, "On target.", "Arrived.", "Arrived to destination.", "On station."),
         Damaged = new(3, 3f, false, "Got hit!", "Taking damage!", "Recieving Damage!"),
         Destroyed = new(5, 4f, false, "Going down!", "Reciving Critical Damage!", "Critical systems destroyed!"),
         TargetAdquired = new(4, 2f, false, "Engaging Target.", "Engaging.", "Target Found.", "New target adquired.", "Target in sigth.", "Locked in, ready to fire", "Target in bound"),
         TargetLost = new(2, 2f, false, "Target lost.", "Lost my target."),
         TargetDestroyed = new(3, 2f, false, "Target destroyed.", "Target eliminated.", "Eliminated");
}

public static class RadarMessages {

    // Contact: a threat detected by a radar, sometimes expressed as N-Contacts: 1c, 2c, 3c, etc
    public static StatusEvent
         TEMP_contact = new(2, 2f, false, "Threat in bound!", "Target approaching!", "Contact!"),
         TEMP_multiContact = new(2, 2f, false, "Threats in bound!", "Targets approaching!", "Contact!"),
         TEMP_noContact = new(1, 2f, false, "No targets on sight", "No threats detected");

    /* All purpose messages
     
     * Contact (1c)
     * Multi-contact (2c - 7c)
     * Wave (+8c) 
     
     * Fast contact increase (+1c in 2 seconds, min: +3c in 6sec)
     * Fast contact decrease (-1c in 2 seconds, min: -3c in 6sec)
     
     * Clean(0c)
    */


    /* Specific triggers that need to be enabled manually
     
     * Fast ally decrease (-1c in 2 seconds, min: -3c in 6sec)
     * No Allies (requirements => +1c enemy, 0c ally)
     
     * Bombers (+1c bomber type)
     * 
    */
}