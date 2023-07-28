using Frontiers.Content;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Linq;
using System;

public class JunctionBlock : DistributionBlock {
    public new JunctionBlockType Type { get => (JunctionBlockType)base.Type; protected set => base.Type = value; }

    // Allow multi-direction item delay
    protected new readonly Queue<DelayedItem>[] queuedItems = new Queue<DelayedItem>[4] { new(), new(), new(), new() };
    protected new readonly DelayedItem[] waitingItem = new DelayedItem[4];

    public override void OutputItems() {
        for (int i = 0; i < 4; i++) {
            if (linkedBlocks[i] == null)
                continue;

            // If the waiting item is empty, try get another, if also not, continue
            if (waitingItem[i] == null) {
                if (queuedItems[i].Count == 0)
                    continue;
                waitingItem[i] = queuedItems[i].Dequeue();
            }

            DelayedItem junctionItem = waitingItem[i];
            if (!junctionItem.CanExit())
                continue;

            if (Pass(i, junctionItem))
                waitingItem[i] = null;
        }
    }

    public override void GetAdjacentBlocks() {
        base.GetAdjacentBlocks();
        for (int i = 0; i < 4; i++) linkedBlocks[i] = GetFacingBlock(i) as ItemBlock;
    }

    public override bool CanReciveItem(Item item, int orientation = 0) {
        return queuedItems[(orientation + 2) % 4].Count < Type.itemCapacity;
    }

    public override void ReciveItems(Item item, int amount = 1, int orientation = 0) {
        queuedItems[(orientation + 2) % 4].Enqueue(new DelayedItem(item, Time.time + travelTime));
    }

    private bool Pass(int orientation, DelayedItem delayedItem) {
        Item item = delayedItem.item;
        ItemBlock reciver = linkedBlocks[orientation];

        if (item == null || reciver == null || reciver.GetTeam() != GetTeam() || !reciver.CanReciveItem(item, orientation)) return false;
        reciver.ReciveItems(item, 1, orientation);
        return true;
    }
}