using Frontiers.Content;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CrafterBlock : ItemBlock {
    public ItemList outputInventory;
    private bool isCrafting;

    public override void SetInventory() {
        inventory = new ItemList(Type.itemCapacity, true);
        isCrafting = false;

        // Set allowed input items
        Item[] allowedItems = new Item[Type.craftPlan.materialList.Length];
        for (int i = 0; i < allowedItems.Length; i++)  allowedItems[i] = Type.craftPlan.materialList[i].item;
        inventory.SetAllowedItems((Item[])allowedItems.Clone());

        // Call base method
        base.SetInventory();
    }

    public bool ContainsEnoughCraftingItems() => inventory.ContainsItemAmount(Type.craftPlan.materialList);

    public bool IsOutputItemFull() {
        ItemStack product = Type.craftPlan.productStack;
        return !(outputInventory.ContainsItemAmount(product) && outputInventory.itemStacks[product.item].amount + product.amount <= Type.itemCapacity);
    }

    protected override void OnInventoryValueChange() {
        // If has not enough items for the craft, stop crafting
        if (isCrafting && (!ContainsEnoughCraftingItems() || IsOutputItemFull())) {
            StopCrafting();
        }

        // If is not crafting and has enough items, start crafting
        if (!isCrafting && ContainsEnoughCraftingItems() && !IsOutputItemFull()) {
            StartCrafting();
        }
    }

    protected virtual void StartCrafting() {
        Invoke(nameof(FinishCrafting), Type.craftPlan.craftTime);
        isCrafting = true;
    }

    protected virtual void StopCrafting() {
        CancelInvoke(nameof(FinishCrafting));
    }

    protected virtual void FinishCrafting() {
        isCrafting = false;

        SubstractItems(Type.craftPlan.materialList);
        outputInventory.AddItem(Type.craftPlan.productStack);
    }
}
