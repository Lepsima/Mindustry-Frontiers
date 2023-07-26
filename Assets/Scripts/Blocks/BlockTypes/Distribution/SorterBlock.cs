using Frontiers.Content;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SorterBlock : DistributionBlock {
    public new SorterBlockType Type { get => (SorterBlockType)base.Type; protected set => base.Type = value; }

    public Item filterItem;

    public override void Set<T>(Vector2 position, Quaternion rotation, T type, int id, byte teamCode) {
        base.Set(position, rotation, type, id, teamCode);
        filterItem = Items.sand;
    }

    protected override bool ForwardCondition() {
        return (filterItem.id == waitingItem.item.id) != Type.inverted;
    }
}