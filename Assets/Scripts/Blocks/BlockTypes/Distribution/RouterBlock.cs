using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Frontiers.Content;

public class RouterBlock : DistributionBlock {
    public new RouterBlockType Type { get => (RouterBlockType)base.Type; protected set => base.Type = value; }

    protected override bool ForwardCondition() {
        return false;
    }

    public override void Set<T>(Vector2 position, Quaternion rotation, T type, int id, byte teamCode) {
        base.Set(position, rotation, type, id, teamCode);

        // Allows to send items forward when looping through side outputs
        linkedBlockLoopStart = 0;
    }
}