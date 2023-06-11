using Frontiers.Content;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CrafterBlock : ItemBlock {
    public new CrafterBlockType Type { get => (CrafterBlockType)base.Type; protected set => base.Type = value; }

    private float nextCraftTime = -1f;
    private Transform topTransform;

    private bool hasTop = false;

    private float warmup;
    private float progress;

    protected override void SetSprites() {
        base.SetSprites();

        if (Type.topSprite != null) { 
            topTransform = transform.Find("Top");
            hasTop = true;
        }
    }

    protected override void Update() {
        base.Update();

        bool isCrafting = IsCrafting();
        warmup = Mathf.Clamp01(warmup + (isCrafting ? 0.019f : -0.019f));

        if (hasTop) {
            topTransform.localScale = Vector3.one * (isCrafting ? Mathf.Sin(Time.time) + 1f : warmup);
        }

        if (isCrafting && Time.time >= nextCraftTime) {
            progress += warmup * Time.deltaTime;
            if (progress >= 1) Craft();
        }

        OutputItems();
    }

    public bool CanCraft() => inventory.Has(Type.craftPlan.materialList) && !inventory.Full(Type.craftPlan.productStack.item);

    public virtual void Craft() {
        nextCraftTime = -1f;
        inventory.Substract(Type.craftPlan.materialList);
        inventory.Add(Type.craftPlan.productStack);
    }

    public bool IsCrafting() => nextCraftTime != -1f;

    public override void SetInventory() {
        base.SetInventory();

        // Set allowed input items
        Item[] allowedItems = new Item[Type.craftPlan.materialList.Length];
        for (int i = 0; i < allowedItems.Length; i++) allowedItems[i] = Type.craftPlan.materialList[i].item;

        inventory.SetAllowedItems(allowedItems);
        acceptedItems = ItemStack.ToItems(Type.craftPlan.materialList);
        outputItems = new Item[1] { Type.craftPlan.productStack.item };
    }

    public override void OnInventoryValueChange(object sender, System.EventArgs e) {
        if (!CanCraft()) nextCraftTime = -1f;
        else if (!IsCrafting()) nextCraftTime = Type.craftPlan.craftTime + Time.time;
    }
}
