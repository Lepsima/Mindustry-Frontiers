using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Frontiers.Assets;
using Frontiers.Content;
using Frontiers.Teams;
using Frontiers.Content.Maps;

public class CoreUnit : Unit {
    public new CoreUnitType Type { get => (CoreUnitType)base.Type; protected set => base.Type = value; }

    protected CoreBlock Core { 
        set {
            if (value != null) { 
                homePosition = value.GetPosition();

                // Add inventory listeners
                if (_core != null) _core.GetInventory().OnAmountChanged -= OnCoreInventoryChangeValue;
                value.GetInventory().OnAmountChanged += OnCoreInventoryChangeValue;
            }
            _core = value;
        } 

        get {
            if (_core == null) Core = TeamUtilities.GetClosestAllyCoreBlock(GetPosition());
            return _core;
        } 
    }
    protected CoreBlock _core;

    protected ConstructionBlock constructingBlock;

    protected float nextConstructionSearchTime;
    protected AssistSubState assistSubState;

    protected bool itemUpdate;
    
    public enum AssistSubState {
        Waiting,
        Collect,
        Deposit,
    }

    public void UpdateSubBehaviour() {
        if (!constructingBlock && ConstructionBlock.blocksInConstruction.Count == 0) assistSubState = AssistSubState.Waiting; 

        // If has enough or the max amount of items to build the block, go directly to it, else go to the core to refill
        bool hasMaxItems = inventory.HasToMax(constructingBlock.GetRestantItems());
        bool hasUsefulItems = inventory.Empty(constructingBlock.GetRequiredItems()) && !inventory.Empty();
        assistSubState = hasMaxItems || hasUsefulItems ? AssistSubState.Deposit : AssistSubState.Collect;
    }

    private ConstructionBlock TryGetConstructionBlock() {
        if (nextConstructionSearchTime > Time.time) return null;
        nextConstructionSearchTime = Time.time + 1f;

        ConstructionBlock closestBlock = null;
        float closestDistance = 100000f;

        foreach (ConstructionBlock block in ConstructionBlock.blocksInConstruction) {
            if (!Core.GetInventory().Has(block.GetRestantItems())) continue;
            float distance = Vector2.Distance(block.GetPosition(), GetPosition());

            if (distance < closestDistance) {
                closestDistance = distance;
                closestBlock = block;
            }
        }

        return closestBlock;
    }

    public override void OnInventoryValueChange(object sender, EventArgs e) {
        if (Mode != UnitMode.Assist) return;
        UpdateSubBehaviour();
    }

    public void OnCoreInventoryChangeValue(object sender, EventArgs e) {
        if (Mode != UnitMode.Assist) return;
        UpdateSubBehaviour();
    }

    protected override void AssistBehaviour() {
        base.AssistBehaviour();     

        // If there is no block to build, search for any other blocks in the map
        if (constructingBlock == null) {
            ConstructionBlock found = null;

            if (ConstructionBlock.blocksInConstruction.Count != 0) found = TryGetConstructionBlock();
            else assistSubState = AssistSubState.Waiting;

            if (found != null) {
                constructingBlock = found;
                UpdateSubBehaviour();
            }
        }

        // If there's nothing to do, go back to core and deposit all items
        if (assistSubState == AssistSubState.Waiting) {
            if (!Core) {
                Client.UnitChangeMode(this, (int)UnitMode.Idling, true);
                return;
            }

            if (inventory.Empty()) return;
            float distanceToCore = Vector2.Distance(Core.GetPosition(), GetPosition());

            if (distanceToCore < Type.itemPickupDistance) {
                _move = false;

                // Drop items to core
                inventory.TransferAll(Core.GetInventory());
            } else {
                SetBehaviourPosition(Core.GetPosition());
            }
        }

        // If needs items to build block, go to core and pickup items
        if (assistSubState == AssistSubState.Collect) {
            if (!Core) { 
                Client.UnitChangeMode(this, (int)UnitMode.Idling, true);
                return;
            }

            float distanceToCore = Vector2.Distance(Core.GetPosition(), GetPosition());

            if (distanceToCore < Type.itemPickupDistance) {
                _move = false;

                // Drop items to core
                inventory.TransferAll(Core.GetInventory());

                // Get only useful items
                Core.GetInventory().TransferAll(inventory, ItemStack.ToItems(constructingBlock.GetRestantItems()));

            } else {
                SetBehaviourPosition(Core.GetPosition());
            }
        }

        // If has items to build block, go to the block and deposit them
        if (assistSubState == AssistSubState.Deposit) {
            float distanceToConstruction = Vector2.Distance(constructingBlock.GetPosition(), GetPosition());

            if (distanceToConstruction < Type.itemPickupDistance) {
                _move = false;

                // Drops items on the constructing block
                inventory.TransferSubstractAmount(constructingBlock.GetInventory(), constructingBlock.GetRestantItems());
            } else {
                SetBehaviourPosition(constructingBlock.GetPosition());
            }
        }
    }

    protected override void IdlingBehaviour() {
        base.IdlingBehaviour();
        SetBehaviourPosition(homePosition);
    }
}