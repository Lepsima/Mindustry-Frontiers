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
            return new FluidStack(stack.fluid, stack.liters * amount);
        }

        public static FluidStack[] Multiply(FluidStack[] stacks, float amount) {
            FluidStack[] copy = new FluidStack[stacks.Length];
            for (int i = 0; i < copy.Length; i++) {
                copy[i] = new FluidStack(stacks[i].fluid, stacks[i].liters * amount);
            }

            return copy;
        }

        public float Pressure(float maxVolume) {
            return 
        }

        public float Volume(float pressure) {
            return 
        }
    }

    public class FluidMixture {
        public Dictionary<Fluid, float> fluids = new();

        public void Add(Fluid fluid, float liters) {
            if (!fluids.ContainsKey(fluid)) fluids.Add(fluid, liters);
            else fluids[fluid] = liters;
        }


    }

    public struct FluidContainerStruct {
        public Fluid[] allowedFluids;
        public float maxVolume, maxPressure;
    }

    public class FluidContainer {
        public Fluid fluid;
        public float maxVolume, maxPressure;
        public float liters, volume, pressure;

        public FluidContainer(Fluid fluid) {
            this.fluid = fluid;
        }

        public FluidContainer(Fluid fluid, float liters, float maxVolume) {
            this.fluid = fluid;
            SetLiters(liters, maxVolume);
        }

        // Returns unused liters
        public float SetLiters(float liters) {
            // Calculate the pressure of the fluid inside the max volume, completely clamped value
            pressure = GetPressure();

            // Calculate the volume of the liters at the current pressure
            // No need to clamp since the pressure is already clamped
            // volume = GetVolume(pressure);

            // Clamp to max liters in case there were extra
            this.liters = GetLiters();

            // Return excess
            return liters - this.liters;
        }

        public float GetPressure() {
            return Mathf.Min((Mathf.Max(liters / maxVolume, Fluids.atmPressure) - 1f) / fluid.volumePressureRatio + 1f, fluid.maxPressure, maxPressure);
        }

        public float GetVolume() {
            return liters / ((pressure - 1f) * fluid.volumePressureRatio + 1f);
        }

        public float GetLiters() {
            return Mathf.Min(liters, pressure * fluid.volumePressureRatio * maxVolume);
        }
    }

    public class FluidComponent {

        public float maxInput, maxOutput;

        public float maxVolume, maxPressure;
        public float volume = 0f, pressure = 1f, liters = 0f, unusedLiters = 0f;
 
        public float Liters {
            get {
                return liters;
            }

            set {
                if (fluid != null) {
                    liters = value;
                    pressure =
                    volume = Mathf.Min(maxVolume, liters);
                    unusedLiters = MaxCapacity() - liters;
                }
            }
        }

        public Dictionary<Fluid, FluidContainer> fluids = new();
        public FluidComponent[] linkedComponents;



        public FluidComponent(float maxVolume, float maxPressure, float maxInput, float maxOutput) {
            this.maxVolume = maxVolume;
            this.maxPressure = maxPressure;
            this.maxInput = maxInput;
            this.maxOutput = maxOutput;
            Liters = 0;
        }

        public FluidComponent(FluidComponentData data) {
            maxVolume = data.maxVolume;
            maxPressure = data.maxPressure;
            maxInput = data.maxInput;
            maxOutput = data.maxOutput;

            if (data.fluid != Fluids.air) {
                fluids.Add(data.fluid, new FluidContainer(data.fluid, MaxCapacity(), maxVolume));
            }
        }

        public void UpdateValue() {

        }

        public void Calc() {
            float maxPressure = Fluids.atmPressure;

            foreach(FluidContainer fluid in fluids.Values) {
                maxPressure = Mathf.Max(fluid.pressure, maxPressure);
            }

            foreach (FluidContainer fluid in fluids.Values) {
                float volume = fluid.GetVolume(maxPressure);
            }
        }

        public void SetLiters(Fluid fluid, float liters) {
            fluids[fluid].SetLiters(liters, maxVolume);
        }

        public float MaxCapacity() {
            return maxPressure * fluid.volumePressureRatio * maxVolume;
        }

        public void Add(float liters) {
            Liters += liters;
        }

        public void Substract(float liters) {
            Liters = Mathf.Max(Liters - liters, 0);
        }

        public void Substract()

        public void SetLinkedComponents(FluidComponent[] components) {
            linkedComponents = components;
        }

        public void Update() {
            if (fluid == null || liters == 0) return;

            foreach (FluidComponent other in linkedComponents) {
                if ((other.fluid != null && other.fluid != fluid)|| other.pressure >= pressure) continue;
                if (other.fluid == null) other.fluid = fluid;

                float maxTransfer = Mathf.Min(other.maxInput, maxOutput) * Time.deltaTime;
                float transferAmount = Mathf.Min(maxTransfer, Liters, other.unusedLiters);

                other.Add(transferAmount);
                Substract(transferAmount);
            }

            if (Liters <= 0f) fluid = null; 
        }
    }
}