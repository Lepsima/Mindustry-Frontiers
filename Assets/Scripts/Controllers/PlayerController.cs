using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlayerController : UnitController {
    public override Vector2 GetDirection() {
        return new Vector2(Input.GetAxis("Horizontal"), Input.GetAxis("Vertical"));
    }

    public override Vector2 AimDirection() {
        return Camera.main.ScreenToWorldPoint(Input.mousePosition);
    }
}