using Frontiers.Content;
using Frontiers.Content.Upgrades;
using Frontiers.FluidSystem;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CrafterBlock : ItemBlock {
    public new CrafterBlockType Type { get => (CrafterBlockType)base.Type; protected set => base.Type = value; }

    private float craftTimer = -1f;
    private Transform topTransform;

    private bool hasTop = false;

    private float warmup;

    private bool itemPass = true, fluidPass = true;

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
        warmup = Mathf.Clamp01((isCrafting ? 0.5f : -0.5f) * Time.deltaTime + warmup);

        if (hasTop) {
            topTransform.localScale = (Mathf.Abs(Mathf.Sin(Time.time * 1.3f) * warmup) + 0.5f * warmup) * Vector3.one;
        }

        if (isCrafting) {
            craftTimer = Mathf.Clamp(craftTimer - (warmup * Time.deltaTime), 0, craftTime);
            if (craftTimer <= 0) Craft();
        }

        OutputItems();
    }

    public virtual void Craft() {
        craftTimer = -1f;

        if (hasItemInventory) {
            inventory.Substract(craftCost.items);
            inventory.Add(craftReturn.items);
        }
 
        if (hasFluidInventory) {
            fluidInventory.SubLiters(craftCost.fluids);
            fluidInventory.AddProductLiters(craftReturn.fluids);
        }
    }

    public bool IsCrafting() => craftTimer != -1f;

    public override void SetInventory() {
        base.SetInventory();

        // Copy the craft plan to a local array
        craftTime = Type.craftPlan.craftTime;
        craftReturn = Type.craftPlan.output;
        craftCost = Type.craftPlan.cost;

        if (hasItemInventory) {
            // Set allowed input items
            List<Item> allowedItems = new();
            if (craftCost.items != null) for (int i = 0; i < craftCost.items.Length; i++) allowedItems.Add(craftCost.items[i].item);
            if (craftReturn.items != null) for (int i = 0; i < craftReturn.items.Length; i++) allowedItems.Add(craftReturn.items[i].item);

            inventory.SetAllowedItems(allowedItems.ToArray());

            acceptedItems = ItemStack.ToItems(craftCost.items);
            outputItems = ItemStack.ToItems(craftReturn.items);

            itemPass = false;
        }

        if (hasFluidInventory) {
            FluidStack[] inputStacks = Type.craftPlan.cost.fluids;
            if (inputStacks != null) allowedInputFluids = FluidStack.ToFluids(inputStacks);

            FluidStack[] outputStacks = Type.craftPlan.output.fluids;
            if (outputStacks != null) allowedOutputFluids = FluidStack.ToFluids(outputStacks);

            fluidPass = false;
        }
    }

    public override void OnInventoryValueChange(object sender, System.EventArgs e) {
        itemPass = !hasItemInventory || inventory.Has(craftCost.items) && !inventory.FullOfAny(ItemStack.ToItems(craftReturn.items));
        CraftState(itemPass && fluidPass);
    }

    public override void OnVolumeChanged(object sender, EventArgs e) {
        fluidPass = !hasFluidInventory || fluidInventory.Has(craftCost.fluids) && fluidInventory.CanRecive(craftReturn.fluids);
        CraftState(itemPass && fluidPass);
    }

    private void CraftState(bool pass) {
        if (!pass) craftTimer = -1f;
        else if (!IsCrafting()) craftTimer = craftTime;
    }
}
