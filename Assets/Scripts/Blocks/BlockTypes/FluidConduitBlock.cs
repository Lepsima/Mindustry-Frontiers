using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Frontiers.Content;

public class FluidConduitBlock : ItemBlock {
    public new FluidConduitBlockType Type { get => (FluidConduitBlockType)base.Type; protected set => base.Type = value; }

    public override void Set<T>(Vector2 position, Quaternion rotation, T type, int id, byte teamCode) {
        base.Set(position, rotation, type, id, teamCode);
        UpdateVariant();
    }

    public override void GetAdjacentBlocks() {
        base.GetAdjacentBlocks();
        UpdateVariant();
    }

    private void UpdateVariant() {
        bool front = GetFacingBlock(0) as ItemBlock;
        bool left = GetFacingBlock(1) as ItemBlock;
        bool back = GetFacingBlock(2) as ItemBlock;
        bool right = GetFacingBlock(3) as ItemBlock;

        int variant = FluidConduitBlockType.GetVariant(front, right, left, back, out bool mirroredSprite);
        GetComponent<SpriteRenderer>().sprite = Type.allConduitSprites[variant];  
    }
}