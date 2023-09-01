using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Frontiers.Content;
public class BridgeBlock : DistributionBlock {
    public new BridgeBlockType Type { get => (BridgeBlockType)base.Type; protected set => base.Type = value; }

    public BridgeBlock nextBridge;

    protected override bool ForwardCondition() {
        return false;
    }

    public override void Set<T>(Vector2 position, Quaternion rotation, T type, int id, byte teamCode) {
        base.Set(position, rotation, type, id, teamCode);

        // Allows to send items forward when looping through side outputs
        forwardCanBeLooped = true;
    }

    public override void OutputItems() {
        // If this is the last bridge, try outputting items
        if (nextBridge == null) base.OutputItems();
        else {
            if (waitingItem == null) {
                if (queuedItems.Count == 0) return;
                waitingItem = queuedItems.Dequeue();
            }

            // Else pass to next bridge;
            TryBridgePass();
        }
    }


    protected bool TryBridgePass() {
        Item item = waitingItem.item;
        bool canPass = item != null && nextBridge != null && nextBridge.GetTeam() == GetTeam() && nextBridge.CanReciveItem(item);

        if (canPass) BridgePass();
        return canPass;
    }

    protected bool CanBridgePass() {
        Item item = waitingItem.item;
        return item != null && nextBridge != null && nextBridge.GetTeam() == GetTeam() && nextBridge.CanReciveItem(item);
    }

    protected void BridgePass() {
        Item item = waitingItem.item;
        nextBridge.ReciveItems(item, 1);
        waitingItem = null;
    }

    public override bool CanReciveItem(Item item, int orientation = 0) {
        return nextBridge == null && queuedItems.Count < Type.itemCapacity;
    }

    public override void ReciveItems(Item item, int amount = 1, int orientation = 0) {
        queuedItems.Enqueue(new DelayedItem(item, Time.time + travelTime, orientation));
    }

    public bool CanConnectTo(BridgeBlock other) {
        Vector2Int difference = other.GetGridPosition() - GetGridPosition();

        // If the offset acts on both axis, return false
        if (difference.x != 0 && difference.y != 0) return false;

        // If the distance is in range, return true
        return Mathf.Abs(difference.x + difference.y) <= Type.connectionRange;
    }
}