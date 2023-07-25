using Frontiers.Content;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

/// <summary>
/// This class tries to generalize and siplify distribution blocks like routers, sorters and overflow gates
/// </summary>
public class DistributionBlock : ItemBlock {
    public new DistributionBlockType Type { get => (DistributionBlockType)base.Type; protected set => base.Type = value; }

    // A queue with items, the first item gets dequeued into "waitingItem"
    protected readonly Queue<DelayedItem> queuedItems = new();

    // The item that should be outputted next
    protected DelayedItem waitingItem;

    // An array with the index corresponding with the orientation the other block is relative to this
    protected readonly ItemBlock[] linkedBlocks = new ItemBlock[4];

    // The time that items take to cross this block
    protected float travelTime;

    // Used in sorters and overflow gates when 2 output blocks are avilable
    protected int nextSide;

    // If 0, Allows blocks to include the forward output to the pass loop function
    protected int linkedBlockLoopStart = 1;

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
            if (linkedBlocks[i] != null && linkedBlocks[i].Type.hasOrientation && linkedBlocks[i].GetFacingBlock() == this)
                linkedBlocks[i] = null;
        }
    }

    // If the returned value is true, items will flow forward, if its false, items will flow to the sides
    protected virtual bool ForwardCondition() {
        return !Type.inverted;
    }

    public override void OutputItems() {
        if (waitingItem == null) {
            if (queuedItems.Count == 0)
                return;

            waitingItem = queuedItems.Dequeue();
        }

        if (waitingItem == null || !waitingItem.CanExit())
            return;

        // If the condition to pass forward is met, pass forward, else loop trough all alternative outputs
        if (ForwardCondition())
            Pass(0);
        else
            PassLoop();
    }

    protected bool PassLoop() {
        // Loop trough the 4 sides with an offset "nextSide"
        for (int i = linkedBlockLoopStart + nextSide; i < 4 + nextSide; i++) {
            // Clamp the index
            int index = i % 4;

            // Don't go backwards
            if (index == 2)
                continue;

            // If this side is avilable, pass the item
            if (CanPass(index)) {
                Pass(index);
                nextSide = (nextSide + 1) % 4;
                return true;
            }
        }

        return false;
    }

    public override bool CanReciveItem(Item item) {
        return queuedItems.Count < Type.itemCapacity;
    }

    public override void ReciveItem(Block sender, Item item) {
        queuedItems.Enqueue(new DelayedItem(item, Time.time + travelTime, sender.GetOrientation()));
    }

    protected ItemBlock GetLinkedBlock(int orientation) {
        return linkedBlocks[orientation % 4];
    }

    protected bool CanPass(int orientation) {
        Item item = waitingItem.item;
        ItemBlock reciver = GetLinkedBlock(orientation + waitingItem.enterOrientation);

        return item != null && reciver != null && reciver.GetTeam() == GetTeam() && reciver.CanReciveItem(this, item);
    }

    protected void Pass(int orientation) {
        Item item = waitingItem.item;
        ItemBlock reciver = GetLinkedBlock(orientation + waitingItem.enterOrientation);

        reciver.ReciveItem(this, item);
        waitingItem = null;
    }
}