using Frontiers.Content;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Linq;
using System;

public class JunctionBlock : ItemBlock {
    public new JunctionBlockType Type { get => (JunctionBlockType)base.Type; protected set => base.Type = value; }
    public float travelTime;

    Queue<JunctionItem>[] queues;
    JunctionItem[] waiting;
    ItemBlock[] linkedBlocks;

    public override void SetInventory() {
        inventory = null;

        travelTime = 1f / Type.itemSpeed;

        queues = new Queue<JunctionItem>[4] { new(), new(), new(), new() };
        waiting = new JunctionItem[4];
        linkedBlocks = new ItemBlock[4];

        hasInventory = true;
    }

    protected override void Update() {
        base.Update();

        for (int i = 0; i < 4; i++) {
            if (linkedBlocks[i] == null) continue;

            // If the waiting item is empty, try get another, if also not, continue
            if (waiting[i] == null) {
                if (queues[i].Count == 0) continue;
                waiting[i] = queues[i].Dequeue();
            }

            JunctionItem junctionItem = waiting[i];
            if (!junctionItem.CanExit()) continue;

            if (Pass(linkedBlocks[i], junctionItem)) waiting[i] = null;
        }
    }

    public override void GetAdjacentBlocks() {
        base.GetAdjacentBlocks();
        for (int i = 0; i < 4; i++) linkedBlocks[i] = GetFacingBlock(i) as ItemBlock;
    }

    public override bool CanReciveItem(Block sender, Item item) {
        // If it's not a reciver, return false
        if (!linkedBlocks.Contains(sender)) return false;

        // Return if the queue can handle more items
        int index = Array.IndexOf(linkedBlocks, sender);
        return queues[(index + 2) % 4].Count < Type.itemCapacity;
    }

    public override bool CanReciveItem(Item item) {
        return false;
    }

    public override void ReciveItem(Block sender, Item item) {
        int index = Array.IndexOf(linkedBlocks, sender);
        queues[(index + 2) % 4].Enqueue(new JunctionItem(item, Time.time + travelTime));
    }

    private bool Pass(ItemBlock reciver, JunctionItem junctionItem) {
        Item item = junctionItem.item;
        if (item == null || reciver == null || reciver.GetTeam() != GetTeam() || !reciver.CanReciveItem(this, item)) return false;
        reciver.ReciveItem(this, item);
        return true;
    }

    private class JunctionItem {
        public Item item;
        public float exitTime;

        public JunctionItem(Item item, float exitTime) {
            this.item = item;
            this.exitTime = exitTime;
        }

        public bool CanExit() {
            return Time.time >= exitTime;
        }
    }
}