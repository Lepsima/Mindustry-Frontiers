using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Frontiers.Content;
using Photon.Pun;

public class UnitFactoryBlock : ItemBlock {
    public new UnitFactoryBlockType Type { get => (UnitFactoryBlockType)base.Type; protected set => base.Type = value; }
    private bool isProducing;

    public override void SetInventory() {
        isProducing = false;

        // Set allowed input items
        Item[] allowedItems = new Item[Type.unitPlan.materialList.Length];
        for (int i = 0; i < allowedItems.Length; i++) allowedItems[i] = Type.unitPlan.materialList[i].item;
        inventory = new Inventory(Type.itemCapacity, -1f, allowedItems);
    }

    public override void OnInventoryValueChange(object sender, System.EventArgs e) {
        // If has not enough items for the craft, stop crafting
        if (isProducing && !inventory.Has(Type.unitPlan.materialList)) {
            StopProduction();
        }

        // If is not crafting and has enough items, start crafting
        if (!isProducing && inventory.Has(Type.unitPlan.materialList)) {
            StartProduction();
        }
    }

    protected virtual void StartProduction() {
        Invoke(nameof(FinishProduction), Type.unitPlan.craftTime);
        isProducing = true;
    }

    protected virtual void StopProduction() {
        CancelInvoke(nameof(FinishProduction));
    }

    protected virtual void FinishProduction() {
        isProducing = false;
        inventory.Substract(Type.unitPlan.materialList);
        Client.CreateUnit(GetPosition(), GetOrientation() * 90f, Type.unitPlan.GetUnit(), teamCode);
    }
}
