using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Frontiers.Content;

public class AircraftUnit : Unit {
    public new AircraftUnitType Type { get => (AircraftUnitType)base.Type; protected set => base.Type = value; }
}