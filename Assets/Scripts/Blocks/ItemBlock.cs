using Frontiers.Content;
using System.Collections.Generic;
using UnityEngine;
using System.Linq;


public abstract class ItemBlock : Block {
    public new ItemBlockType Type { get => (ItemBlockType)base.Type; protected set => base.Type = value; }

    protected Queue<ItemBlock> adjacentBlocks;
    protected Item[] acceptedItems;
    protected Item[] outputItems;

    public override void Set<T>(Vector2 position, Quaternion rotation, T type, int id, byte teamCode) {
        base.Set(position, rotation, type, id, teamCode);
        hasInventory = true;

        GetAdjacentBlocks();
        UpdateAdjacentBlocks();

        Debug.Log(adjacentBlocks);
    }

    public override void SetInventory() {
        inventory = new Inventory(Type.itemCapacity, Type.itemMass);
        hasInventory = true;

        base.SetInventory();
    }

    public override bool CanReciveItem(Item item) { 
        return base.CanReciveItem(item) && IsAcceptedItem(item) && !inventory.Full(item); 
    }

    public virtual bool IsAcceptedItem(Item item) => acceptedItems == null || acceptedItems.Length == 0 || acceptedItems.Contains(item);

    public virtual void ReciveItem(Block sender, Item item) {
        inventory.Add(item, 1);
    }

    public virtual void OutputItems() {
        int adjacentBlockCount = adjacentBlocks.Count;
        if (adjacentBlockCount == 0 || outputItems.Length == 0 || inventory.Empty(outputItems)) return;

        Item currentItem = inventory.First(outputItems);

        for(int i = 0; i < adjacentBlockCount; i++) {
            ItemBlock itemBlock = adjacentBlocks.Dequeue();
            Item item = inventory.Has(currentItem, 1) ? currentItem : inventory.First(outputItems);

            if (item == null) {
                adjacentBlocks.Enqueue(itemBlock);
                break; 
            }

            if (itemBlock.CanReciveItem(item)) {
                itemBlock.ReciveItem(this, item);
                inventory.Substract(item, 1);
            }

            adjacentBlocks.Enqueue(itemBlock);
        }
    }

    public virtual void GetAdjacentBlocks() {
        List<ItemBlock> adjacentBlocks = MapManager.Map.GetAdjacentBlocks(this);
        this.adjacentBlocks = new Queue<ItemBlock>();

        foreach (ItemBlock itemBlock in adjacentBlocks) {
            // If the block is a conveyor and it's facing at this block, you dont want to give it any items back
            if (itemBlock is ConveyorBlock conveyor && conveyor.IsFacingAt(this)) continue;
            this.adjacentBlocks.Enqueue(itemBlock);
        }
    }

    public virtual void UpdateAdjacentBlocks() {
        for (int i = 0; i < adjacentBlocks.Count; i++) {
            ItemBlock adjacentBlock = adjacentBlocks.Dequeue();
            if (adjacentBlock == null) continue;

            adjacentBlock.GetAdjacentBlocks();
            adjacentBlocks.Enqueue(adjacentBlock);
        }
    }

    public virtual bool IsFlammable() {
        if (inventory == null) return false;
        foreach (KeyValuePair<Item, int> pair in inventory.items) if (pair.Key.flammability > 0f && pair.Value > 0) return true;
        return false;
    }

    public virtual bool IsExplosive() {
        if (inventory == null) return false;
        foreach (KeyValuePair<Item, int> pair in inventory.items) if (pair.Key.explosiveness > 0f && pair.Value > 0) return true;
        return false;
    }

    public override void OnDestroy() {
        base.OnDestroy();
        if (!gameObject.scene.isLoaded) return;

        UpdateAdjacentBlocks();
    }
}
