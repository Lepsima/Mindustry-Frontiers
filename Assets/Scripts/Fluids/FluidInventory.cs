using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Frontiers.Content;
using System;
using System.Linq;
using UnityEditor.Purchasing;

namespace Frontiers.FluidSystem {
    public class FluidStack {
        public Fluid fluid;
        public float liters;

        public FluidStack(Fluid fluid, float liters) {
            this.fluid = fluid;
            this.liters = liters;
        }

        public static FluidStack Multiply(FluidStack stack, float amount) {
            return new FluidStack(stack.fluid, stack.liters * amount);
        }

        public static FluidStack[] Multiply(FluidStack[] stacks, float amount) {
            FluidStack[] copy = new FluidStack[stacks.Length];
            for (int i = 0; i < copy.Length; i++) {
                copy[i] = new FluidStack(stacks[i].fluid, stacks[i].liters * amount);
            }

            return copy;
        }
    }
    

    // Pressure formula = Mathf.Min((Mathf.Max(liters / data.maxVolume, Fluids.atmPressure) - 1f) / fluid.volumePressureRatio + 1f, fluid.maxPressure, data.maxPressure);

    // Volume formula = liters / ((pressure - 1f) * fluid.volumePressureRatio + 1f);

    // Liters formula = return Mathf.Min(liters, MaxLiters());

    // pressure * fluid.volumePressureRatio * data.maxVolume;

    public class FluidInventory {
        public FluidInventoryData data;
        public ItemBlock block;

        public event EventHandler OnVolumeChanged;

        public bool pressurized, canReciveLowerPressures;
        public float pressure, usedVolume, volumePercent;

        // Fluid type, (x = liters, y = volume)
        public Dictionary<Fluid, Vector2> fluids = new();
        public FluidInventory[] linkedInventories;

        public FluidInventory(ItemBlock block, FluidInventoryData data) {
            this.block = block;
            this.data = data;

            UpdatePressure(CanBePressurized() ? data.maxPressure : Fluids.atmosphericPressure);
        }

        private void OnVolumeChange() {
            volumePercent = usedVolume / data.maxVolume;
            OnVolumeChanged?.Invoke(this, EventArgs.Empty);
        }

        public void Update() {
            bool canBePressurized = CanBePressurized();

            // If pressurization state changed, update pressure
            // TODO: change pressure slowly
            if (canBePressurized != pressurized) {
                UpdatePressure(canBePressurized ? data.maxPressure : Fluids.atmosphericPressure);
            }

            foreach (FluidInventory other in linkedInventories) {
                int pressureDiff = PressureDifference(other);

                // Get max output volume
                float maxVolumePerFluid = Mathf.Min(data.maxOutput, other.data.maxInput, other.EmptyVolume());
                float transferAmount = maxVolumePerFluid / fluids.Count * Time.deltaTime;

                if (pressureDiff == 1 || other.canReciveLowerPressures) {
                    for (int i = fluids.Count - 1; i >= 0; i--) {
                        Fluid fluid = fluids.Keys.ElementAt(i);

                        // Transfer
                        other.AddVolume(fluid, transferAmount);
                        SubVolume(fluid, transferAmount);
                    }
                } else if (pressureDiff == 0) {
                    for (int i = fluids.Count - 1; i >= 0; i--) {
                        Fluid fluid = fluids.Keys.ElementAt(i);

                        // If other's volume percent is higher, skip
                        if (other.volumePercent > volumePercent) continue;

                        // Transfer
                        other.AddVolume(fluid, transferAmount);
                        SubVolume(fluid, transferAmount);
                    }
                } 
            }
        }

        public void AddVolume(Fluid fluid, float volume) {
            if (volume == 0f) return;

            // Add fluid to list
            if (!fluids.ContainsKey(fluid)) {
                if (data.allowedFluids != null && !data.allowedFluids.Contains(fluid)) return;

                // Add new fluid and order by density
                fluids.Add(fluid, Vector2.zero);
                fluids.OrderBy(x => x.Key.density);
            }

            // Add volume
            float maxAdd = Mathf.Min(volume, data.maxVolume - usedVolume);
            usedVolume += maxAdd;
            OnVolumeChange();
            volume = fluids[fluid].y + maxAdd;

            // Calculate liters and set new values
            float liters = fluid.Liters(volume, pressure);
            fluids[fluid] = new Vector2(liters, volume);
        }

        public void AddLiters(FluidStack[] stacks) {
            foreach (FluidStack stack in stacks) {
                AddLiters(stack.fluid, stack.liters);
            }
        }

        public void AddLiters(Fluid fluid, float liters) {
            float volume = fluid.Volume(liters, pressure);
            AddVolume(fluid, volume);
        }

        public void SubVolume(Fluid fluid, float volume) {
            // If this inventory doesnt contain this fluid, skip
            if (!fluids.ContainsKey(fluid) || volume == 0f) return;

            // Get the max substract amount and apply it
            float maxSubstract = Mathf.Min(fluids[fluid].y, volume);
            usedVolume -= maxSubstract;
            OnVolumeChange();
            volume = fluids[fluid].y - maxSubstract;

            if (volume > 0f) {
                // Calculate liters and set new values
                float liters = fluid.Liters(volume, pressure);
                fluids[fluid] = new Vector2(liters, volume);
            } else {
                // If 0, remove from fluid list
                fluids.Remove(fluid);
            }
        }

        public void SubLiters(FluidStack[] stacks) {
            foreach(FluidStack stack in stacks) {
                SubLiters(stack.fluid, stack.liters);
            }
        }

        public void SubLiters(Fluid fluid, float liters) {
            float volume = fluid.Volume(liters, pressure);
            SubVolume(fluid, volume);
        }

        public bool CanBePressurized() {
            // Cant be pressurized if is too damaged
            return data.pressurizable && block.GetHealthPercent() > data.minHealthPressurizable;
        }

        public float EmptyVolume() {
            return data.maxVolume - usedVolume;
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

        public int PressureDifference(FluidInventory other) {
            if (other.pressure == pressure) return 0;
            else if (other.pressure > pressure) return -1;
            else return 1;
        }

        public void UpdatePressure(float value) {
            pressure = Mathf.Max(value, Fluids.atmosphericPressure);
            float totalVolume = 0f;

            // Loop though each fluid in density order, heavier first
            for (int i = 0; i < fluids.Count; i++) {
                // Get the current fluid
                Fluid fluid = fluids.Keys.ElementAt(i);

                // Get the volume that ocupies this fluid
                float liters = fluids[fluid].x;
                float volume = fluid.Volume(liters, pressure);

                // Clamp volume to the max it can fit
                if (totalVolume + volume > data.maxVolume) {
                    volume = data.maxVolume - totalVolume;
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
            Debug.Log(linkedInventories.Length);
        }
    }
}