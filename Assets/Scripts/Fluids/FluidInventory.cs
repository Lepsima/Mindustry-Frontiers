using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Frontiers.Content;
using System;
using System.Linq;

namespace Frontiers.FluidSystem {
    public class FluidStack {
        public Fluid fluid;
        public float liters;

        public FluidStack(Fluid fluid, float liters) {
            this.fluid = fluid;
            this.liters = liters;
        }

        public static FluidStack Multiply(FluidStack stack, float amount) {
            if (stack == null) return null;
            return new FluidStack(stack.fluid, stack.liters * amount);
        }

        public static FluidStack[] Multiply(FluidStack[] stacks, float amount) {
            if (stacks == null) return null;

            FluidStack[] copy = new FluidStack[stacks.Length];
            for (int i = 0; i < copy.Length; i++) {
                copy[i] = new FluidStack(stacks[i].fluid, stacks[i].liters * amount);
            }

            return copy;
        }

        public static FluidStack[] With(params object[] items) {
            FluidStack[] stacks = new FluidStack[items.Length / 2];
            for (int i = 0; i < items.Length; i += 2) stacks[i / 2] = new FluidStack((Fluid)items[i], (float)items[i + 1]);
            return stacks;
        }

        public static Fluid[] ToFluids(FluidStack[] stacks) {
            Fluid[] fluids = new Fluid[stacks.Length];
            for (int i = 0; i < stacks.Length; i++) fluids[i] = stacks[i].fluid;
            return fluids;
        }
    }


    // Pressure formula = Mathf.Min((Mathf.Max(liters / data.maxVolume, Fluids.atmPressure) - 1f) / fluid.volumePressureRatio + 1f, fluid.maxPressure, data.maxPressure);

    // Volume formula = liters / ((pressure - 1f) * fluid.volumePressureRatio + 1f);

    // Liters formula = return Mathf.Min(liters, MaxLiters());

    // pressure * fluid.volumePressureRatio * data.maxVolume;
    public class FluidInventory {
        public ItemBlockType Type;
        public ItemBlock block;

        public event EventHandler OnVolumeChanged;

        public bool pressurized, canReciveLowerPressures;
        public float pressure, usedVolume, volumePercent;

        public float maxVolumePerFluid;
        public bool fixedFluidSpace;

        // Fluid type, (x = liters, y = volume)
        public Dictionary<Fluid, Vector2> fluids = new();
        public FluidInventory[] linkedInventories;

        public Fluid displayFluid;

        public FluidInventory(ItemBlock block) {
            this.block = block;
            Type = block.Type;

            UpdatePressure();

            maxVolumePerFluid = Type.fixedSpace ? Type.maxVolume / Type.maxFluids : Type.maxVolume;
        }

        private void OnVolumeChange() {
            volumePercent = usedVolume / Type.maxVolume;
            OnVolumeChanged?.Invoke(this, EventArgs.Empty);

            if (fluids.Count == 0) displayFluid = null;
            else displayFluid = fluids.Keys.ElementAt(0);
        }

        public void Update() {
            UpdatePressure();

            // Skip update if possible
            if (volumePercent == 0 || Type.fluidInputOnly || fluids.Count == 0) return;

            foreach (FluidInventory other in linkedInventories) {
                // If cant transfer to this inventory, skip
                if (!CanTransfer(other) || fluids.Count == 0) continue;

                // Calculate the max transfer amount per fluid
                float maxTransfer = Mathf.Min(Type.maxOutput, other.Type.maxInput) / fluids.Count;

                for (int i = fluids.Count - 1; i >= 0; i--) {
                    // Get fluid
                    Fluid fluid = fluids.ElementAt(i).Key;

                    // If cant be transfered, skip
                    if (!(block.CanOutputFluid(fluid) && other.CanRecive(fluid))) continue;

                    // Calculate transfer amount
                    Vector2 value = fluids.ElementAt(i).Value;
                    float transferAmount = Mathf.Min(other.EmptyLitersFor(fluid), value.x, maxTransfer * Time.deltaTime);

                    // Transfer amount
                    other.AddLiters(fluid, transferAmount);
                    SubLiters(fluid, transferAmount);
                }
            }
        }

        public void AddProductLiters(FluidStack[] stacks) {
            foreach (FluidStack stack in stacks) {
                AddProductLiters(stack.fluid, stack.liters);
            }
        }

        public void AddProductLiters(Fluid fluid, float liters) {
            if (liters == 0) return;

            float volume = fluid.Volume(liters, pressure);

            // Add fluid to list
            if (!fluids.ContainsKey(fluid)) {
                if (Type.maxFluids != -1 && Type.maxFluids <= fluids.Count && !fluids.ContainsKey(fluid)) return;

                // Add new fluid and order by density
                fluids.Add(fluid, Vector2.zero);
                fluids = fluids.OrderBy(x => x.Key.density).ToDictionary(pair => pair.Key, pair => pair.Value);
            }

            // Add volume
            float maxAdd = Mathf.Min(volume, EmptyVolumeFor(fluid));
            usedVolume += maxAdd;
            OnVolumeChange();
            volume = fluids[fluid].y + maxAdd;

            // Calculate liters and set new values
            fluids[fluid] = new Vector2(fluid.Liters(volume, pressure), volume);
        }

        public void AddLiters(FluidStack[] stacks) {
            foreach (FluidStack stack in stacks) {
                AddLiters(stack.fluid, stack.liters);
            }
        }

        public void AddLiters(Fluid fluid, float liters) {
            if (liters == 0) return;

            float volume = fluid.Volume(liters, pressure);

            // Add fluid to list
            if (!fluids.ContainsKey(fluid)) {
                if (!CanRecive(fluid)) return; 

                // Add new fluid and order by density
                fluids.Add(fluid, Vector2.zero);
                fluids = fluids.OrderBy(x => x.Key.density).ToDictionary(pair => pair.Key, pair => pair.Value);
            }

            // Add volume
            float maxAdd = Mathf.Min(volume, EmptyVolumeFor(fluid));
            usedVolume += maxAdd;
            OnVolumeChange();
            volume = fluids[fluid].y + maxAdd;

            // Calculate liters and set new values
            fluids[fluid] = new Vector2(fluid.Liters(volume, pressure), volume);
        }

        public void SubLiters(FluidStack[] stacks) {
            foreach(FluidStack stack in stacks) {
                SubLiters(stack.fluid, stack.liters);
            }
        }

        public void SubLiters(Fluid fluid, float liters) {
            if (!fluids.ContainsKey(fluid) || liters == 0f) return;

            float volume = fluid.Volume(liters, pressure);

            // Get the max substract amount and apply it
            float maxSubstract = Mathf.Min(fluids[fluid].y, volume);
            usedVolume -= maxSubstract;
            OnVolumeChange();
            volume = fluids[fluid].y - maxSubstract;

            if (volume > 0f) {
                // Calculate liters and set new values
                fluids[fluid] = new Vector2(fluid.Liters(volume, pressure), volume);
            } else {
                // If 0, remove from fluid list
                fluids.Remove(fluid);
            }
        }

        public bool CanTransfer(FluidInventory other) {
            int pressureDiff = PressureDifference(other);
            return pressureDiff == 1 || other.canReciveLowerPressures || (pressureDiff == 0 && other.volumePercent <= volumePercent);
        }

        public bool CanBePressurized() {
            // Cant be pressurized if is too damaged
            return Type.pressurizable && block.GetHealthPercent() > Type.minHealthPressurizable;
        }

        public float EmptyVolume() {
            return Type.maxVolume - usedVolume;
        }

        public float EmptyVolumeFor(Fluid fluid) {
            return fixedFluidSpace ? maxVolumePerFluid - fluids[fluid].y : Type.maxVolume - usedVolume;
        }

        public float EmptyLitersFor(Fluid fluid) {
            return fluid.Liters(EmptyVolumeFor(fluid), pressure);
        }

        public bool Full() {
            return volumePercent >= 1f;
        }

        public bool CanRecive(FluidStack[] stacks) {
            foreach(FluidStack stack in stacks) if (!CanRecive(stack)) return false;
            return true;
        }

        public bool CanRecive(FluidStack stack) {
            return CanRecive(stack.fluid, stack.liters);
        }

        public bool CanRecive(Fluid fluid, float liters) {
            return EmptyVolume() >= fluid.Volume(liters, pressure);
        }

        public bool CanRecive(Fluid fluid) {
            bool allowed = block.CanReciveFluid(fluid);
            bool fits = Type.maxFluids == -1 || Type.maxFluids > fluids.Count || fluids.ContainsKey(fluid);
            return allowed && fits;
        }

        public bool Has(FluidStack[] stacks) {
            foreach (FluidStack stack in stacks) if (!Has(stack)) return false;
            return true;
        }

        public bool Has(FluidStack stack) {
            return Has(stack.fluid, stack.liters);
        }

        public bool Has(Fluid fluid, float liters) {
            return fluids.ContainsKey(fluid) && fluids[fluid].x >= liters;
        }

        // 0 if both are equal, 1 if the other is lower, -1 if the other is higher
        public int PressureDifference(FluidInventory other) {
            if (other.pressure == pressure) return 0;
            else if (other.pressure > pressure) return -1;
            else return 1;
        }

        public void UpdatePressure() {
            if (CanBePressurized() == pressurized) return;
            pressurized = !pressurized;

            pressure = pressurized ? Type.maxPressure : Fluids.atmosphericPressure;
            float totalVolume = 0f;

            // Loop though each fluid in density order, heavier first
            for (int i = 0; i < fluids.Count; i++) {
                // Get the current fluid
                Fluid fluid = fluids.Keys.ElementAt(i);

                // Get the volume that ocupies this fluid
                float liters = fluids[fluid].x;
                float volume = fluid.Volume(liters, pressure);

                // Clamp volume to the max it can fit
                if (totalVolume + volume > Type.maxVolume) {
                    volume = Type.maxVolume - totalVolume;
                    liters = fluid.Liters(volume, pressure);
                }

                // Add to total
                totalVolume += volume;

                // Set the calculated value
                fluids[fluid] = new Vector2(liters, volume);
            }

            usedVolume = totalVolume;
            OnVolumeChange();
        }

        public void SetLinkedComponents(FluidInventory[] inventories) {
            linkedInventories = inventories;
        }
    }
}