using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class FluidConduit : ItemBlock {
    public override void SetFluidLinkedComponents(List<ItemBlock> adjacentBlocks) {
        for (int i = adjacentBlocks.Count - 1; i >= 0; i--) {
            ItemBlock adjacentBlock = adjacentBlocks[i];
            if (adjacentBlock.Type.hasOrientation && adjacentBlock.GetFacingBlock() == this) adjacentBlocks.Remove(adjacentBlock);
        }

        base.SetFluidLinkedComponents(adjacentBlocks);
    }
}