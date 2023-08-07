using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Frontiers.Content;
using Frontiers.FluidSystem;
using System.Linq;

public class FluidExhaustBlock : ItemBlock {
    public new FluidExhaustBlockType Type { get => (FluidExhaustBlockType)base.Type; protected set => base.Type = value; }

    public override void Set<T>(Vector2 position, Quaternion rotation, T type, int id, byte teamCode) {
        base.Set(position, rotation, type, id, teamCode);
    }

    protected override void Update() {
        base.Update();

        int count = fluidInventory.fluids.Count;
        if (count <= 0) return;

        float substractVolume = Mathf.Min(Type.fluidInventoryData.maxOutput, fluidInventory.usedVolume) * Time.deltaTime / count;
        for (int i = count - 1; i >= 0; i--) fluidInventory.SubVolume(fluidInventory.fluids.Keys.ElementAt(i), substractVolume);
    }
}
