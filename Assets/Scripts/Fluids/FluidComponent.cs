using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Frontiers.Content;
using System;

namespace Frontiers.FluidSystem {

    [Serializable]
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
                    pressure = fluid.volumePressureRatio * Mathf.Max(liters / maxVolume, Fluids.atmPressure) - fluid.volumePressureRatio + 1f;
                    volume = Mathf.Min(maxVolume, liters);
                    unusedLiters = MaxCapacity() - liters;
                }
            }
        }

        public Fluid fluid;
        [NonSerialized] public FluidComponent[] linkedComponents;

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
                fluid = data.fluid;
                Liters = MaxCapacity();
            }
        }

        public float MaxCapacity() {
            return maxPressure * fluid.volumePressureRatio * maxVolume;
        }

        public void Add(float liters) {
            Liters += liters;
        }

        public void Sub(float liters) {
            Liters = Mathf.Max(Liters - liters, 0);
        }

        public void SetLinkedComponents(FluidComponent[] components) {
            linkedComponents = components;
        }

        public void Update() {
            foreach (FluidComponent other in linkedComponents) {
                if ((other.fluid != null && other.fluid != fluid)|| other.pressure >= pressure) continue;
                if (other.fluid == null) other.fluid = fluid;

                float maxTransfer = Mathf.Min(other.maxInput, maxOutput) * Time.deltaTime;
                float transferAmount = Mathf.Min(maxTransfer, Liters, other.unusedLiters);

                other.Add(transferAmount);
                Sub(transferAmount);
            }

            if (Liters <= 0f) fluid = null; 
        }
    }
}