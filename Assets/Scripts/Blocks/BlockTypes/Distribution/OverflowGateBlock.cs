using Frontiers.Content;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

public class OverflowGateBlock : DistributionBlock {
    public new OverflowGateBlockType Type { get => (OverflowGateBlockType)base.Type; protected set => base.Type = value; }

    protected override bool ForwardCondition() {
        return Type.inverted ? !(CanPass(1) || CanPass(3)) : CanPass(0);
    }
}