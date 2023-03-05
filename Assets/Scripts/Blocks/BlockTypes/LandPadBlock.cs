using Frontiers.Content;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class LandPadBlock : Block {
    public List<Unit> landedUnits = new List<Unit>();

    public bool IsFull() => landedUnits.Count >= Type.unitCapacity;

    public bool Land(Unit unit) {
        if (IsFull() || landedUnits.Contains(unit)) return false;
        unit.transform.position = GetLandPosition(landedUnits.Count);
        landedUnits.Add(unit);
        return true;
    }

    private Vector2 GetLandPosition(int index) {
        return Type.landPositions.Length <= index
            ? GetGridPosition() + (0.5f * Type.size * Vector2.one)
            : GetGridPosition() + Type.landPositions[index];
    }

    public void TakeOff(Unit unit) {
        if (landedUnits.Contains(unit)) landedUnits.Remove(unit);
    }
}