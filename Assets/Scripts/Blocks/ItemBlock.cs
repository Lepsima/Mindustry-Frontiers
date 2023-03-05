using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Frontiers.Content;

public abstract class ItemBlock : Block, IInventory {
    public ItemList inventory;

    public override void Set(Vector2Int gridPosition, BlockType blockType, float timeCode, byte teamCode) {
        base.Set(gridPosition, blockType, timeCode, teamCode);
        hasInventory = true;
        SetInventory();
    }

    public virtual void SetInventory() {
        OnInventoryValueChange();
    }

    public ItemStack AddItems(ItemStack value) {
        ItemStack itemStack = inventory.AddItem(value);
        OnInventoryValueChange();
        return itemStack;
    }

    public ItemList AddItems(ItemStack[] value) {
        ItemList itemList = inventory.AddItems(value);
        OnInventoryValueChange();
        return itemList;
    }

    public ItemStack SubstractItems(ItemStack value) {
        ItemStack itemStack = inventory.SubstractItem(value);
        OnInventoryValueChange();
        return itemStack;
    }

    public ItemList SubstractItems(ItemStack[] value) {
        ItemList itemList = inventory.SubstractItems(value);
        OnInventoryValueChange();
        return itemList;
    }

    public ItemList GetItemList() {
        return inventory;
    }

    protected virtual void OnInventoryValueChange() {
        // Do nothing
    }
}
