using Frontiers.Content;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SorterBlock : ItemBlock {
    public new SorterBlockType Type { get => (SorterBlockType)base.Type; protected set => base.Type = value; }

    public Item selectedItem;

    readonly Queue<DelayedItem> queuedItems = new();
    readonly ItemBlock[] linkedBlocks = new ItemBlock[4];

    DelayedItem waiting;
    float travelTime;
    bool nextSide;

    protected override void Update() {
        base.Update();
        OutputItems();
    }

    public override void SetInventory() {
        inventory = null;
        travelTime = 1f / Type.itemSpeed;
        hasInventory = true;
    }

    public override void GetAdjacentBlocks() {
        base.GetAdjacentBlocks();
        for (int i = 0; i < 4; i++) {
            linkedBlocks[i] = GetFacingBlock(i) as ItemBlock;
            if (linkedBlocks[i].Type.hasOrientation && linkedBlocks[i].GetFacingBlock() == this) linkedBlocks[i] = null;
        }
    }

    public override void OutputItems() {
        if (waiting == null) {
            if (queuedItems.Count == 0)
                return;

            waiting = queuedItems.Dequeue();
        }

        if (waiting == null || !waiting.CanExit())
            return;

        if (selectedItem == waiting.item == !Type.inverted)
            Pass(0, waiting);
        else if (nextSide 
            ? Pass(3, waiting) || Pass(1, waiting) 
            : Pass(1, waiting) || Pass(3, waiting))
            nextSide = !nextSide;
    }

    public override bool CanReciveItem(Item item) {
        return queuedItems.Count < Type.itemCapacity;
    }

    public override void ReciveItem(Block sender, Item item) {
        queuedItems.Enqueue(new DelayedItem(item, Time.time + travelTime));
    }

    private bool Pass(int localOrientation, DelayedItem delayedItem) {
        Item item = delayedItem.item;
        Debug.Log((localOrientation + delayedItem.enterOrientation) % 4);
        ItemBlock reciver = linkedBlocks[(localOrientation + delayedItem.enterOrientation) % 4];

        if (item == null || reciver == null || reciver.GetTeam() != GetTeam() || !reciver.CanReciveItem(this, item))
            return false;

        reciver.ReciveItem(this, item);
        waiting = null;
        return true;
    }
}