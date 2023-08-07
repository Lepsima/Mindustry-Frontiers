using Frontiers.Content;
using Frontiers.Content.Upgrades;
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

    #region - Upgradable Stats -

    protected float craftTime;
    protected MaterialList craftReturn;
    protected MaterialList craftCost;

    #endregion

    protected override void ApplyUpgrageMultiplier(UpgradeType upgrade) {
        base.ApplyUpgrageMultiplier(upgrade);

        BlockUpgradeMultipliers mult = upgrade.properties as BlockUpgradeMultipliers;

        craftTime += craftTime * mult.crafter_craftTime;
        craftReturn = MaterialList.Multiply(craftReturn, 1f + mult.crafter_craftReturn);
        craftCost = MaterialList.Multiply(craftCost, 1f + mult.crafter_craftCost);
    }

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
        warmup = Mathf.Clamp01((isCrafting ? 2f : -2f) * Time.deltaTime + warmup);

        if (hasTop) {
            topTransform.localScale = (Mathf.Abs(Mathf.Sin(Time.time * 1.3f) * warmup) + 0.5f * warmup) * Vector3.one;
        }

        if (isCrafting && Time.time >= nextCraftTime) {
            progress += warmup * Time.deltaTime;
            if (progress >= 1) Craft();
        }

        OutputItems();
    }

    public bool CanCraft() {
        bool itemPass = !hasItemInventory || inventory.Has(craftCost.items) && !inventory.FullOfAny(ItemStack.ToItems(craftReturn.items));
        bool fluidPass = !hasFluidInventory || fluidInventory.Has(craftCost.fluids) && fluidInventory.CanRecive(craftReturn.fluids);
        return itemPass && fluidPass;
    }

    public virtual void Craft() {
        nextCraftTime = -1f;

        if (hasItemInventory) {
            inventory.Substract(craftCost.items);
            inventory.Add(craftReturn.items);
        }
 
        if (hasFluidInventory) {
            fluidInventory.SubLiters(craftCost.fluids);
            fluidInventory.AddLiters(craftReturn.fluids);
        }
    }

    public bool IsCrafting() => nextCraftTime != -1f;

    public override void SetInventory() {
        base.SetInventory();

        // Copy the craft plan to a local array
        craftTime = Type.craftPlan.craftTime;
        craftReturn = Type.craftPlan.product;
        craftCost = Type.craftPlan.cost;

        if (hasItemInventory) {
            // Set allowed input items
            Item[] allowedItems = new Item[craftCost.items.Length + craftReturn.items.Length];
            for (int i = 0; i < craftCost.items.Length; i++) allowedItems[i] = craftCost.items[i].item;
            for (int i = 0; i < craftReturn.items.Length; i++) allowedItems[i + craftCost.items.Length - 1] = craftReturn.items[i].item;

            inventory.SetAllowedItems(allowedItems);
            acceptedItems = ItemStack.ToItems(craftCost.items);
            outputItems = ItemStack.ToItems(craftReturn.items);
        }

        if (hasFluidInventory) {

        }
    }

    public override void OnInventoryValueChange(object sender, System.EventArgs e) {
        if (!CanCraft()) nextCraftTime = -1f;
        else if (!IsCrafting()) nextCraftTime = craftTime + Time.time;
    }
}
