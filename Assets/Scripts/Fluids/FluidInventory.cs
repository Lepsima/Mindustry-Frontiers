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

    public class FluidMixture {
        public Dictionary<Fluid, float> fluids = new();

        public void Add(Fluid fluid, float liters) {
            if (!fluids.ContainsKey(fluid)) fluids.Add(fluid, liters);
            else fluids[fluid] = liters;
        }


    }

    public struct FluidContainerData {
        public Fluid[] allowedFluids;
        public float maxVolume, maxPressure;
        public float maxInput, maxOutput;
    }

    public class FluidContainer {
        public Fluid fluid;
        public FluidContainerData data;

        public FluidContainer[] linkedContainers;

        public float liters, unusedLiters, volume, pressure;

        public FluidContainer(FluidContainerData data) {
            this.data = data;
        }

        // Returns unused liters
        public float SetLiters(float liters) {
            // Calculate the pressure of the fluid inside the max volume, completely clamped value
            pressure = GetPressure();

            // Clamp to max liters in case there were extra
            this.liters = GetLiters();
            unusedLiters = this.liters - MaxLiters();

            // Return excess
            return liters - this.liters;
        }

        public float GetPressure() {
            return Mathf.Min((Mathf.Max(liters / data.maxVolume, Fluids.atmPressure) - 1f) / fluid.volumePressureRatio + 1f, fluid.maxPressure, data.maxPressure);
        }

        public float GetVolume() {
            return liters / ((pressure - 1f) * fluid.volumePressureRatio + 1f);
        }

        public float GetLiters() {
            return Mathf.Min(liters, MaxLiters());
        }

        public float MaxLiters() {
            return pressure * fluid.volumePressureRatio * data.maxVolume;
        }

        public void UpdateContainer() {
            if (fluid == null || liters == 0) 
                return;

            foreach (FluidContainer other in linkedContainers) {
                if ((other.fluid != null && other.fluid != fluid) || other.pressure >= pressure)
                    continue;

                other.fluid ??= fluid;

                float maxTransfer = Mathf.Min(other.data.maxInput, data.maxOutput) * Time.deltaTime;
                float transferAmount = Mathf.Min(maxTransfer, liters, other.unusedLiters);

                other.SetLiters(other.liters + transferAmount);
                SetLiters(liters - transferAmount);
            }
        }
    }

    public struct FluidInventoryData {
        // Max liters per second this block can output/recive from/to each other block
        public float maxInput, maxOutput;

        public FluidContainerData[] containerDatas;

        public FluidInventoryData(float maxInput, float maxOutput, FluidContainerData[] containerDatas) {
            this.maxInput = maxInput;
            this.maxOutput = maxOutput;
            this.containerDatas = containerDatas;
        }
    }

    public class FluidInventory {
        public FluidInventoryData data;

        public (Fluid, FluidContainer)[] fluidContainers;
        public FluidInventory[] linkedComponents;


        public FluidInventory(FluidInventoryData data) {
            this.data = data;
            fluidContainers = CreateContainers(data.containerDatas);
        }

        public static (Fluid, FluidContainer)[] CreateContainers(FluidContainerData[] datas) {
            List<(Fluid, FluidContainer)> containerList = new();
            foreach(FluidContainerData data in datas) containerList.Add(new(null, new FluidContainer(data)));
            return containerList.ToArray();
        }

        public void SetLinkedComponents(FluidInventory[] components) {
            List<FluidContainer> containers = new List<FluidContainer>();

            foreach(FluidInventory inventory in components) {
                foreach (FluidContainer fluidContainer in inventory.fluidContainers) {
                    containers.Add(fluidContainer);
                }
            }

            foreach((Fluid, FluidContainer) container in fluidContainers) {
                container.Item2.linkedContainers = 
            }
        }

        public void Update() {
            foreach()
            if (fluid == null || liters == 0) return;

            foreach (FluidInventory other in linkedComponents) {

            }

            if (Liters <= 0f) fluid = null; 
        }
    }
}