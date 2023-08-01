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
                if (fluid == null) {
                    fluid = Fluids.atmFluid;
                    value = GetMaxLiters();
                }

                liters = value;
                volume = Mathf.Min(liters / (maxPressure / fluid.volumePressureRatio), maxVolume);
                pressure = Mathf.Min(liters / maxVolume, maxPressure);
                unusedLiters = maxPressure * fluid.volumePressureRatio * (maxVolume - volume);
            }
        }

        public Fluid fluid;
        [NonSerialized] public FluidComponent[] linkedComponents;

        public FluidComponent(float maxVolume, float maxPressure, float maxInput, float maxOutput) {
            this.maxVolume = maxVolume;
            this.maxPressure = maxPressure;
            this.maxInput = maxInput;
            this.maxOutput = maxOutput;
        }

        public FluidComponent(FluidComponentData data) {
            maxVolume = data.maxVolume;
            maxPressure = data.maxPressure;
            maxInput = data.maxInput;
            maxOutput = data.maxOutput;
        }

        public float GetMaxLiters() {
            return maxPressure * fluid.volumePressureRatio * maxVolume;
        }

        public float GetLiters() {
            return maxPressure * fluid.volumePressureRatio * volume;
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
                if (other.fluid != fluid || other.pressure >= pressure) continue;

                float maxTransfer = Mathf.Min(other.maxInput, maxOutput) * Time.deltaTime;
                float transferAmount = Mathf.Min(maxTransfer, Liters, other.unusedLiters);

                other.Add(transferAmount);
                Sub(transferAmount);
            }

            if (Liters <= 0f) fluid = null; 
        }
    }
}