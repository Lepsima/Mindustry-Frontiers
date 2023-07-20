using Frontiers.Content;
using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

public class OverflowGateBlock : ItemBlock {
    public new OverflowGateBlockType Type { get => (OverflowGateBlockType)base.Type; protected set => base.Type = value; }

    Queue<DelayedItem> queuedItems = new();
    DelayedItem waiting;

    ItemBlock frontReciver;
    ItemBlock leftReciver;
    ItemBlock rightReciver;
    bool nextSide;

    protected override void Update() {
        base.Update();
        OutputItems();
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

        if (Type.inverted) {
            if (!PassSides())
                Pass(frontReciver, waiting);
        } else {
            if (!Pass(frontReciver, waiting))
                PassSides();
        }

        bool PassSides() {
            bool passed = nextSide
                ? Pass(leftReciver, waiting) || Pass(rightReciver, waiting)
                : Pass(rightReciver, waiting) || Pass(leftReciver, waiting);

            if (passed)
                nextSide = !nextSide;

            return passed;
        }
    }

    public override bool CanReciveItem(Item item) {
        return queuedItems.Count < Type.itemCapacity;
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