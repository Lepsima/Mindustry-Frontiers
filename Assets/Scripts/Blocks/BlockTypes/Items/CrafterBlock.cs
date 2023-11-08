using Frontiers.Content;
using Frontiers.Content.Upgrades;
using Frontiers.FluidSystem;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class CrafterBlock : ItemBlock {
    public new CrafterBlockType Type { get => (CrafterBlockType)base.Type; protected set => base.Type = value; }

    private float craftTimer = -1f;
    private Transform topTransform;
    private Rotor rotor;

    private bool hasTop = false, isMulticrafter = false;
    private float warmup, powerPercent;

    private bool itemPass = true, fluidPass = true;
    private bool checkItemPass = true, checkFluidPass = true;

    #region - Upgradable Stats -

    protected float craftTime;
    protected float craftPowerUsage;
    protected float craftProductionMult = 1f;
    protected float craftConsumptionMult = 1f;
    protected MaterialList craftProduction;
    protected MaterialList craftConsumption;

    #endregion

    public override void Set<T>(Vector2 position, Quaternion rotation, T type, int id, byte teamCode) {
        base.Set(position, rotation, type, id, teamCode);

        if (Type.rotator.IsValid()) {
            rotor = new Rotor(transform, Type.rotator, "Blocks");
        }
    }

    protected override void ApplyUpgrageMultiplier(UpgradeType upgrade) {
        base.ApplyUpgrageMultiplier(upgrade);

        BlockUpgradeMultipliers mult = upgrade.properties as BlockUpgradeMultipliers;

        craftTime += craftTime * mult.crafter_craftTime;
        craftPowerUsage += craftPowerUsage * mult.crafter_powerUsage;
        craftProductionMult += craftProductionMult * mult.crafter_craftReturn;
        craftConsumptionMult += craftConsumptionMult * mult.crafter_craftCost;

        craftProduction = MaterialList.Multiply(craftProduction, craftProductionMult);
        craftConsumption = MaterialList.Multiply(craftConsumption, craftConsumptionMult);
    }

    public virtual void SetCraftPlan(CraftPlan newCraftPlan) {
        craftTime = newCraftPlan.craftTime;
        craftPowerUsage = newCraftPlan.powerUsage;
        craftProduction = MaterialList.Multiply(newCraftPlan.production, craftProductionMult);
        craftConsumption = MaterialList.Multiply(newCraftPlan.consumption, craftConsumptionMult);

        checkItemPass = craftConsumption.items != null || craftProduction.items != null;
        itemPass = !checkItemPass;
        UpdateItemPass();

        checkFluidPass = craftConsumption.fluids != null || craftProduction.fluids != null;
        fluidPass = !checkFluidPass;
        UpdateFluidPass();

        // If doesnt use power or generates it, set the power requirement to 100% archived
        if (newCraftPlan.powerUsage >= 0) {
            powerPercent = 1f;
            powerModule.powerUsage = 0f;
        } else { 
            // If consumes power, set the target power consumption to the power module
            powerModule.powerUsage = newCraftPlan.powerUsage; 
        }
    }

    /// <summary>
    /// Check if the current craft plan is invalid and replace it with a valid one
    /// </summary>
    public void UpdateMultiCraftPlan() {
        if (IsValidConsumption(craftConsumption)) return;

        foreach (CraftPlan craftPlan in Type.craftPlans) {
            if (IsValidConsumption(craftPlan.consumption)) { 
                SetCraftPlan(craftPlan);
                return;
            }
        }
    }

    private bool IsValidConsumption(MaterialList materialList) {
        // Return false if empty
        if (materialList.items == null && materialList.fluids == null) return false;

        // Pass if doesnt use of that or has in inventory
        bool itemPass = materialList.items == null || inventory.Has(materialList.items);
        bool fluidPass = materialList.fluids == null || fluidInventory.Has(materialList.fluids);

        // Return true if both are true
        return itemPass && fluidPass;
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
        
        // Get the power consumption value
        if (craftPowerUsage < 0f) {
            powerPercent = powerModule.powerPercent;
        }

        // Warmup
        bool isCrafting = IsCrafting();
        warmup = Mathf.Clamp01((isCrafting && powerPercent > 0 ? 0.5f : -0.5f) * Time.deltaTime + warmup);
        float mult = warmup * powerPercent;
        
        // Update top sprite
        if (hasTop) topTransform.localScale = (Mathf.Abs(Mathf.Sin(Time.time * 1.3f) * mult) + 0.5f * mult) * Vector3.one;

        // Update rotor
        if (rotor != null) rotor.Update(isCrafting ? 1 : -1);

        // Update craft progress
        if (isCrafting) {
            craftTimer = Mathf.Clamp(craftTimer - (mult * Time.deltaTime), 0, craftTime);
            if (craftTimer <= 0) Craft();
        }

        if (craftPowerUsage > 0f) {
            // Set the power generation value
            powerModule.powerUsage = isCrafting ? craftPowerUsage * mult : 0f;
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

        isMulticrafter = Type.craftPlans != null;

        if (isMulticrafter && Type.craftPlans.Length == 0) Debug.Log("You set this crafter as a multicrafter but not provided any craft plans, make sure to type the craft plans in the array: \"craftPlans\"");

        if (isMulticrafter) {
            List<Item> allowedInputItems = new();
            List<Item> allowedOutputItems = new();
            List<Fluid> allowedInputFluids = new();
            List<Fluid> allowedOutputFluids = new();

            foreach (CraftPlan craftPlan in Type.craftPlans) {
                if (hasItemInventory) {
                    if (craftPlan.consumption.items != null) allowedInputItems.AddRange(ItemStack.ToItems(craftPlan.consumption.items).ToList());
                    if (craftPlan.production.items != null) allowedOutputItems.AddRange(ItemStack.ToItems(craftPlan.production.items).ToList());

                    itemPass = false;
                }

                if (hasFluidInventory) {
                    if (craftPlan.consumption.fluids != null) allowedInputFluids.AddRange(FluidStack.ToFluids(craftPlan.consumption.fluids).ToList());
                    if (craftPlan.production.fluids != null) allowedOutputFluids.AddRange(FluidStack.ToFluids(craftPlan.production.fluids).ToList());

                    fluidPass = false;
                }
            }

            allowedInputItems.AddRange(allowedOutputItems);
            inventory.SetAllowedItems(allowedInputItems.ToArray());

            this.allowedInputItems = allowedInputItems.ToArray();
            this.allowedOutputItems = allowedOutputItems.ToArray();
            this.allowedInputFluids = allowedInputFluids.ToArray();
            this.allowedOutputFluids = allowedOutputFluids.ToArray();

            SetCraftPlan(Type.craftPlans[0]);

        } else {
            SetCraftPlan(Type.craftPlan);

            if (hasItemInventory) {
                if (Type.craftPlan.consumption.items != null) allowedInputItems = ItemStack.ToItems(Type.craftPlan.consumption.items);
                if (Type.craftPlan.production.items != null) allowedOutputItems = ItemStack.ToItems(Type.craftPlan.production.items);

                List<Item> temp = new(allowedInputItems);
                temp.AddRange(allowedOutputItems);
                inventory.SetAllowedItems(temp.ToArray());

                itemPass = false;
            }

            if (hasFluidInventory) {
                if (Type.craftPlan.consumption.fluids != null) allowedInputFluids = FluidStack.ToFluids(Type.craftPlan.consumption.fluids);
                if (Type.craftPlan.production.fluids != null) allowedOutputFluids = FluidStack.ToFluids(Type.craftPlan.production.fluids);

                fluidPass = false;
            }
        }
    }

    public override void OnInventoryValueChange(object sender, EventArgs e) {
        if (isMulticrafter) UpdateMultiCraftPlan();
        UpdateItemPass();
    }

    public override void OnVolumeChanged(object sender, EventArgs e) {
        if (isMulticrafter) UpdateMultiCraftPlan();
        UpdateFluidPass();
    }

    private void UpdateItemPass() {
        if (!checkItemPass) return;
        itemPass = !hasItemInventory || inventory.Has(craftConsumption.items) && inventory.Fits(craftProduction.items);
        CraftState(itemPass && fluidPass);
    }

    private void UpdateFluidPass() {
        if (!checkFluidPass) return;
        fluidPass = !hasFluidInventory || fluidInventory.Has(craftConsumption.fluids) && fluidInventory.CanRecive(craftProduction.fluids);
        CraftState(itemPass && fluidPass);
    }

    private void CraftState(bool pass) {
        if (!pass) craftTimer = -1f;
        else if (!IsCrafting()) craftTimer = craftTime;
    }
}
