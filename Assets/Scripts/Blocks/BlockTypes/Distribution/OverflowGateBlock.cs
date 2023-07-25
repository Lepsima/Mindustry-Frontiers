using Frontiers.Content;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

public class OverflowGateBlock : ItemBlock {
    public new OverflowGateBlockType Type { get => (OverflowGateBlockType)base.Type; protected set => base.Type = value; }

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
            if (linkedBlocks[i] != null && linkedBlocks[i].Type.hasOrientation && linkedBlocks[i].GetFacingBlock() == this) linkedBlocks[i] = null;
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

        if (Type.inverted) {
            if (!PassSides())
                Pass(0, waiting);
        } else {
            if (!Pass(0, waiting))
                PassSides();
        }

        bool PassSides() {
            bool passed = nextSide
                ? Pass(3, waiting) || Pass(1, waiting)
                : Pass(1, waiting) || Pass(3, waiting);

            if (passed)
                nextSide = !nextSide;

            return passed;
        }
    }

    public override bool CanReciveItem(Item item) {
        return queuedItems.Count < Type.itemCapacity;
    }

    public override void ReciveItem(Block sender, Item item) {
        queuedItems.Enqueue(new DelayedItem(item, Time.time + travelTime, sender.GetOrientation()));
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