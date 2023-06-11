using System;
using System.Collections;
using System.Collections.Generic;
using Frontiers.Content;
using UnityEngine;

public class ConstructionBlock : Block {
    public static List<ConstructionBlock> blocksInConstruction = new();
    protected Item[] requiredItems;

    const float HealthMultiplier = 0.1f;
    public float progress;
    public bool hasEndedConstruction = false;

    public override void Set<T>(Vector2 position, Quaternion rotation, T type, int id, byte teamCode) {
        base.Set(position, rotation, type, id, teamCode);
        health = Type.health * HealthMultiplier;
        blocksInConstruction.Add(this);
    }

    public override void SetInventory() {
        hasInventory = false;
        requiredItems = ItemStack.ToItems(Type.buildCost);

        inventory = new Inventory(-1, -1, requiredItems);
        inventory.Add(Type.buildCost);

        base.SetInventory();
    }

    public ItemStack[] GetBuildCost() {
        return Type.buildCost;
    }

    public ItemStack[] GetRestantItems() {
        return inventory.ToArray();
    }
    
    public Item[] GetRequiredItems() {
        return requiredItems;
    }

    public override void OnInventoryValueChange(object sender, EventArgs e) {
        base.OnInventoryValueChange(sender, e);
        float totalProgress = 0f;

        for (int i = 0; i < GetBuildCost().Length; i++) {
            ItemStack stack = GetBuildCost()[i];
            totalProgress += inventory.items.ContainsKey(stack.item)? (stack.amount - inventory.items[stack.item]) / (float)stack.amount : 1;
        }

        progress = totalProgress / GetBuildCost().Length;
        if (progress == 1) EndConstruction();
    }

    public void CancelConstruction() {
        if (hasEndedConstruction || !IsMaster) return;
        hasEndedConstruction = true;
        Client.DestroyBlock(this);
    }

    public void EndConstruction() {
        if (hasEndedConstruction || !IsMaster) return;
        hasEndedConstruction = true;
        Client.BuildBlock(this);
    }

    public override void OnDestroy() {
        if (!gameObject.scene.isLoaded) return;
        hasEndedConstruction = true;
        blocksInConstruction.Remove(this);
    }
}