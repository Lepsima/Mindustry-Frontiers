using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Frontiers.Content;
using Frontiers.FluidSystem;
using System.Linq;

public class AtmosphericCollectorBlock : ItemBlock {
    public new AtmosphericCollectorBlockType Type { get => (AtmosphericCollectorBlockType)base.Type; protected set => base.Type = value; }
    public Fluid collectionFluid;

    public override void Set<T>(Vector2 position, Quaternion rotation, T type, int id, byte teamCode) {
        base.Set(position, rotation, type, id, teamCode);
        collectionFluid = Fluids.atmosphericFluid;
    }

    protected override void Update() {
        base.Update();

        float litersToAdd = Type.fluidInventoryData.maxInput * Time.deltaTime;
        foreach((Element element, float percent) in collectionFluid.composition) if (element is Fluid fluid) fluidInventory.AddLiters(fluid, litersToAdd * percent);
    }
}