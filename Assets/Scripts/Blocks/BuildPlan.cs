using Frontiers.Content;
using System;
using UnityEngine;

public class BuildPlan {
    public event EventHandler<EventArgs> OnPlanFinished;
    public event EventHandler<EventArgs> OnPlanCanceled;

    public BlockType blockType;
    public Inventory missingItems;
    public Vector2Int position;
    public int orientation;
    public bool breaking;

    public float progress;
    public bool hasStarted, isStuck;

    public BuildPlan(BlockType blockType, Vector2Int position, int orientation) {
        this.blockType = blockType;
        this.position = position;
        this.orientation = orientation;

        missingItems = new Inventory();
        missingItems.Add(blockType.buildCost);
    }

    public void AddItems(ItemStack[] stacks) {
        missingItems.Substract(stacks);
        float progress = BuildProgress();
        if (progress >= 1f) OnPlanFinished?.Invoke(this, EventArgs.Empty);
    }

    public float BuildProgress() {
        float total = 0;

        for (int i = 0; i < missingItems.items.Count; i++) {
            Item item = blockType.buildCost[i].item;
            int neededAmount = blockType.buildCost[i].amount;

            total += (neededAmount - missingItems.items[item]) / neededAmount;
        }

        total /= missingItems.items.Count;

        return total;
    }

    public void Cancel() {
        OnPlanCanceled?.Invoke(this, EventArgs.Empty);
    }
}