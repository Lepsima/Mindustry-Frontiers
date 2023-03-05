using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Frontiers.Content;
using Photon.Pun;

public class UnitFactoryBlock : ItemBlock {
    private bool isCrafting;

    public override void SetInventory() {
        inventory = new ItemList(Type.itemCapacity, true);
        isCrafting = false;

        // Set allowed input items
        Item[] allowedItems = new Item[Type.unitPlan.materialList.Length];
        for (int i = 0; i < allowedItems.Length; i++) allowedItems[i] = Type.unitPlan.materialList[i].item;
        inventory.SetAllowedItems((Item[])allowedItems.Clone());

        // Call base method
        base.SetInventory();
    }

    public bool ContainsEnoughCraftingItems() => inventory.ContainsItemAmount(Type.unitPlan.materialList);

    protected override void OnInventoryValueChange() {
        // If has not enough items for the craft, stop crafting
        if (isCrafting && !ContainsEnoughCraftingItems()) {
            StopCrafting();
        }

        // If is not crafting and has enough items, start crafting
        if (!isCrafting && ContainsEnoughCraftingItems()) {
            StartCrafting();
        }
    }

    protected virtual void StartCrafting() {
        Invoke(nameof(FinishCrafting), Type.unitPlan.craftTime);
        isCrafting = true;
    }

    protected virtual void StopCrafting() {
        CancelInvoke(nameof(FinishCrafting));
    }

    protected virtual void FinishCrafting() {
        isCrafting = false;

        SubstractItems(Type.unitPlan.materialList);

        // If is the master client, spawn unit
        if (PhotonNetwork.IsMasterClient) {
            int unitViewID = MapManager.Instance.CreateUnit(GetPosition(), Type.unitPlan.unit, teamCode);

            // Set unit mode to return
            Unit unit = PhotonNetwork.GetPhotonView(unitViewID).gameObject.GetComponent<Unit>();
            unit.SetMode(Unit.UnitMode.Return);
        }
    }
}
