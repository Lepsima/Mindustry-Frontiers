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
    ItemBlock[] recivers;

    public override void SetInventory() {
        inventory = null;

        travelTime = 1f / Type.itemSpeed;

        queues = new Queue<JunctionItem>[4];
        waiting = new JunctionItem[4];
        recivers = new ItemBlock[4];

        hasInventory = true;
    }

    protected override void Update() {
        base.Update();

        for (int i = 0; i < 4; i++) {
            if (recivers[i] == null) return;

            // If the waiting item is empty, try get another, if also not, continue
            if (waiting[i] == null) {
                if (queues[i].Count == 0) continue;
                waiting[i] = queues[i].Dequeue();
            }

            JunctionItem junctionItem = waiting[i];
            if (!junctionItem.CanExit()) continue;

            Pass(recivers[i], junctionItem);
        }
    }

    public override void GetAdjacentBlocks() {
        base.GetAdjacentBlocks();
        for (int i = 0; i < 4; i++) recivers[i] = GetFacingBlock(i) as ItemBlock;
    }

    public override bool CanReciveItem(Block sender, Item item) {
        // If it's not a reciver, return false
        if (!recivers.Contains(sender)) return false;

        // Return if the queue can handle more items
        int index = Array.IndexOf(recivers, sender);
        return queues[index].Count < Type.itemCapacity;
    }

    public override bool CanReciveItem(Item item) {
        return false;
    }

    public override void ReciveItem(Block sender, Item item) {
        int index = Array.IndexOf(recivers, sender);
        queues[index].Enqueue(new JunctionItem(item, Time.time + travelTime));
    }

    private void Pass(ItemBlock reciver, JunctionItem junctionItem) {
        Item item = junctionItem.item;
        if (item == null || reciver == null || reciver.GetTeam() != GetTeam() || !reciver.CanReciveItem(item)) return;
        reciver.ReciveItem(this, item);
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