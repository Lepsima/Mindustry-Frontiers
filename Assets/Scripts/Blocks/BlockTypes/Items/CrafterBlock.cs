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
    protected MaterialList craftProduction;
    protected MaterialList craftConsumption;

    #endregion

    protected override void ApplyUpgrageMultiplier(UpgradeType upgrade) {
        base.ApplyUpgrageMultiplier(upgrade);

        BlockUpgradeMultipliers mult = upgrade.properties as BlockUpgradeMultipliers;

        craftTime += craftTime * mult.crafter_craftTime;
        craftProduction = MaterialList.Multiply(craftProduction, 1f + mult.crafter_craftReturn);
        craftConsumption = MaterialList.Multiply(craftConsumption, 1f + mult.crafter_craftCost);
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
            if (craftConsumption.items != null) inventory.Substract(craftConsumption.items);
            if (craftProduction.items != null) inventory.Add(craftProduction.items);
        }
 
        if (hasFluidInventory) {
            if (craftConsumption.fluids != null) fluidInventory.SubLiters(craftConsumption.fluids);
            if (craftProduction.fluids != null) fluidInventory.AddProductLiters(craftProduction.fluids);
        }
    }

    public bool IsCrafting() => craftTimer != -1f;

    public override void SetInventory() {
        base.SetInventory();

        // Copy the craft plan to a local array
        craftTime = Type.craftPlan.craftTime;
        craftProduction = Type.craftPlan.production;
        craftConsumption = Type.craftPlan.consumption;

        if (hasItemInventory) {
            // Set allowed input items
            List<Item> allowedItems = new();
            if (craftConsumption.items != null) for (int i = 0; i < craftConsumption.items.Length; i++) allowedItems.Add(craftConsumption.items[i].item);
            if (craftProduction.items != null) for (int i = 0; i < craftProduction.items.Length; i++) allowedItems.Add(craftProduction.items[i].item);

            inventory.SetAllowedItems(allowedItems.ToArray());

            acceptedItems = ItemStack.ToItems(craftConsumption.items);
            outputItems = ItemStack.ToItems(craftProduction.items);

            itemPass = false;
        }

        if (hasFluidInventory) {
            if (Type.craftPlan.consumption.fluids != null) allowedInputFluids = FluidStack.ToFluids(Type.craftPlan.consumption.fluids);
            if (Type.craftPlan.production.fluids != null) allowedOutputFluids = FluidStack.ToFluids(Type.craftPlan.production.fluids);

            fluidPass = false;
        }
    }

    public override void OnInventoryValueChange(object sender, EventArgs e) {
        itemPass = !hasItemInventory || inventory.Has(craftConsumption.items) && inventory.Fits(craftProduction.items);
        CraftState(itemPass && fluidPass);
    }

    public override void OnVolumeChanged(object sender, EventArgs e) {
        fluidPass = !hasFluidInventory || fluidInventory.Has(craftConsumption.fluids) && fluidInventory.CanRecive(craftProduction.fluids);
        CraftState(itemPass && fluidPass);
    }

    private void CraftState(bool pass) {
        if (!pass) craftTimer = -1f;
        else if (!IsCrafting()) craftTimer = craftTime;
    }
}
