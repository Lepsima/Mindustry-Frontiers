using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class UnitController {
    public Unit unit;


    /// <summary>
    /// Gets the direction the unit should move based on current situation
    /// </summary>
    /// <returns>The directon of movement of the unit</returns>
    public virtual Vector2 GetDirection() {
        return Vector2.zero;
    }

    /// <summary>
    /// Gets the aim direction the unit should aim at based on current situation
    /// </summary>
    /// <returns>The aim direction</returns>
    public virtual Vector2 AimDirection() {
        return Vector2.zero;
    }
}