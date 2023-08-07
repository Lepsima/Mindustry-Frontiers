using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Frontiers.Content;

public class FluidPipeBlock : ItemBlock {
    public new FluidPipeBlockType Type { get => (FluidPipeBlockType)base.Type; protected set => base.Type = value; }

    public override void Set<T>(Vector2 position, Quaternion rotation, T type, int id, byte teamCode) {
        base.Set(position, rotation, type, id, teamCode);
        UpdateVariant();
    }

    public override void GetAdjacentBlocks() {
        base.GetAdjacentBlocks();
        UpdateVariant();
    }

    private void UpdateVariant() {
        int front = CheckFacingBlock(0) ? 1 : 0;
        int left = CheckFacingBlock(1) ? 2 : 0;
        int back = CheckFacingBlock(2) ? 4 : 0;
        int right = CheckFacingBlock(3) ? 8 : 0;

        int bitmask = front | left | back | right;
        GetComponent<SpriteRenderer>().sprite = Type.allPipeSprites[bitmask];
    }

    private bool CheckFacingBlock(int orientation) {
        ItemBlock itemBlock = GetFacingBlock(orientation) as ItemBlock;
        return itemBlock != null && itemBlock.hasFluidInventory;
    }
}