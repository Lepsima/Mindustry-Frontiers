using Frontiers.Content;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
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

    // If true, Allows blocks to include the forward output to the pass loop function
    protected bool forwardCanBeLooped = false;

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
            TryPass(0);
        else
            PassLoop();
    }

    protected bool PassLoop() {
        int offset = nextSide;

        // Loop trough the 4 sides with an offset "nextSide"
        for (int i = offset; i < 4 + offset; i++) {
            // Clamp the index
            int index = i % 4;

            // Don't go backwards and don't skip forward if desired
            if (!forwardCanBeLooped && index == 0 || index == 2) {
                nextSide++;
                continue;
            }

            // If this side is avilable, pass the item
            if (TryPass(index)) {
                nextSide = (nextSide + 1) % 4;
                return true;
            }
        }

        return false;
    }

    public override bool CanReciveItem(Item item, int orientation = 0) {
        return queuedItems.Count < Type.itemCapacity;
    }

    public override void ReciveItems(Item item, int amount = 1, int orientation = 0) {
        queuedItems.Enqueue(new DelayedItem(item, Time.time + travelTime, orientation));
    }

    protected ItemBlock GetLinkedBlock(int orientation) {
        return linkedBlocks[orientation % 4];
    }

    protected bool CanPass(int orientation) {
        Item item = waitingItem.item;
        ItemBlock reciver = GetLinkedBlock(orientation + waitingItem.enterOrientation);

        return item != null && reciver != null && reciver.GetTeam() == GetTeam() && reciver.CanReciveItem(item, orientation);
    }

    protected bool TryPass(int orientation) {
        if (!CanPass(orientation))
            return false;

        Pass(orientation);
        return true;
    }

    protected void Pass(int orientation) {
        Item item = waitingItem.item;
        ItemBlock reciver = GetLinkedBlock(orientation + waitingItem.enterOrientation);

        reciver.ReciveItems(item, 1, orientation + waitingItem.enterOrientation);
        waitingItem = null;
    }
}