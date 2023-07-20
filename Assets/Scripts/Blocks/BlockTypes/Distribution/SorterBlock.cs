using Frontiers.Content;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SorterBlock : ItemBlock {
    public new SorterBlockType Type { get => (SorterBlockType)base.Type; protected set => base.Type = value; }

    public Item selectedItem;

    readonly Queue<DelayedItem> queuedItems = new();
    DelayedItem waiting;
    float travelTime;

    ItemBlock frontReciver;
    ItemBlock leftReciver;
    ItemBlock rightReciver;
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

        frontReciver = GetFacingBlock() as ItemBlock;
        leftReciver = GetFacingBlock(-1) as ItemBlock;
        rightReciver = GetFacingBlock(1) as ItemBlock;
    }

    public override void OutputItems() {
        if (waiting == null) {
            if (queuedItems.Count == 0)
                return;

            waiting = queuedItems.Dequeue();
        }

        if (waiting == null)
            return;

        if (selectedItem == waiting.item == !Type.inverted)
            Pass(frontReciver, waiting);
        else if (nextSide 
            ? Pass(leftReciver, waiting) || Pass(rightReciver, waiting) 
            : Pass(rightReciver, waiting) || Pass(leftReciver, waiting))
            nextSide = !nextSide;
    }

    public override bool CanReciveItem(Item item) {
        return queuedItems.Count < Type.itemCapacity;
    }

    public override void ReciveItem(Block sender, Item item) {
        queuedItems.Enqueue(new DelayedItem(item, Time.time + travelTime));
    }

    private bool Pass(ItemBlock reciver, DelayedItem delayedItem) {
        Item item = delayedItem.item;
        if (item == null || reciver == null || reciver.GetTeam() != GetTeam() || !reciver.CanReciveItem(this, item))
            return false;

        reciver.ReciveItem(this, item);
        waiting = null;
        return true;
    }
}