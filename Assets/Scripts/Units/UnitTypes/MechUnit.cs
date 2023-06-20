using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Frontiers.Content;

public class MechUnit : Unit {
    public new MechUnitType Type { get => (MechUnitType)base.Type; protected set => base.Type = value; }
}
