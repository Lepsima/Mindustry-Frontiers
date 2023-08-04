using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Frontiers.Content;
using Frontiers.FluidSystem;
using System.Linq;

public class FluidCollectorBlock : ItemBlock {
    public new FluidCollectorBlockType Type { get => (FluidCollectorBlockType)base.Type; protected set => base.Type = value; }
    public Fluid collectionFluid;

    public override void Set<T>(Vector2 position, Quaternion rotation, T type, int id, byte teamCode) {
        base.Set(position, rotation, type, id, teamCode);
        collectionFluid = Fluids.atmosphericFluid;
    }

    protected override void Update() {
        base.Update();

        if (Input.GetKeyDown(KeyCode.P)) {
            for (int i = 0; i < fluidInventory.fluids.Count; i++) {
                Fluid fluid = fluidInventory.fluids.Keys.ElementAt(i);
                Debug.Log($"{fluid.name} => liters: {fluidInventory.fluids[fluid].x}, volume: {fluidInventory.fluids[fluid].y}");
            }
        }

        float litersToAdd = Type.literCollectionRate * Time.deltaTime;
        foreach((Element element, float percent) in collectionFluid.composition) if (element is Fluid fluid) fluidInventory.AddLiters(fluid, litersToAdd * percent);
    }
}