using Frontiers.Content;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SorterBlock : DistributionBlock {
    public new SorterBlockType Type { get => (SorterBlockType)base.Type; protected set => base.Type = value; }

    public Item filterItem;

    protected override bool ForwardCondition() {
        return (filterItem.id == waitingItem.item.id) != Type.inverted;
    }
}