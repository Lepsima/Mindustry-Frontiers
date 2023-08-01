using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Frontiers.Content;

namespace Frontiers.FluidSystem {
    public static class FluidHandler {
    }

    public class FluidComponent {

        public float maxInput, maxOutput;

        public float maxVolume, maxPressure;
        public float volume = 0f, pressure = 1f, liters = 0;

        public float Liters {
            get {
                return liters;
            }

            set {
                liters = value;
                volume = Volume();
                pressure = Pressure();
            }
        }

        public Fluid fluid;
        public FluidComponent[] linkedComponents;

        public FluidComponent(float maxVolume, float maxPressure, float maxInput, float maxOutput) {
            this.maxVolume = maxVolume;
            this.maxPressure = maxPressure;
            this.maxInput = maxInput;
            this.maxOutput = maxOutput;
        }

        public float UnusedLiters() {
            if (fluid == null) return 0;
            return maxPressure * fluid.volumePressureRatio / (maxVolume - volume);
        }

        public float Volume() {
            if (fluid == null) return 0;
            return liters / (maxPressure / fluid.volumePressureRatio);
        }

        public float Pressure() {
            if (fluid == null) return 1;
            return liters / maxVolume * fluid.volumePressureRatio;
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
            foreach (FluidComponent fluidComponent in linkedComponents) {
                float maxTransfer = Mathf.Min(fluidComponent.maxInput, maxOutput);

                
            }
        }
    }
}