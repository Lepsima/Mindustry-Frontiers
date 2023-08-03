using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Frontiers.Content;

namespace Frontiers.FluidSystem {

    public class Fluid : Content.Content {
        public Color color;

        // Mass in tons per litre
        public float density = 0.01f;

        // The atmospheres needed to half the volume (1 = neutral)
        public float compressionRatio = 1f;

        // The max amount this fluid can be compressed
        public float maxCompression = 1f;

        public Fluid(string name) : base(name) {

        }

        public float Compression(float pressure) {
            return (Mathf.Min(pressure - 1f, maxCompression) * compressionRatio + 1f);
        }

        public float Volume(float liters, float pressure) {
            return liters / Compression(pressure);
        }

        public float Liters(float volume, float pressure) {
            return volume * Compression(pressure);
        }
    }

    public class Fluids {
        public static Fluid air, water;

        public static Fluid atmFluid;
        public static float atmPressure = 1f;

        public static void Load() {
            // temporal, should be replaced with oxigen/nitrogen/co2 or whatever
            air = new Fluid("air") {
                density = 0.00000129f, // Really small numbers, maybe i should measure in kg/l instead of t/l
                compressionRatio = 1.1f,
                maxCompression = 10.2f,
            };

            water = new Fluid("liquid-water") {
                density = 0.001f,
            };

            atmFluid = air;
        }
    }

    public struct FluidInventoryData {
        // Max volume per second this block can output/recive from/to each other block
        public float maxInput, maxOutput;

        // Volume = liters at 1 atmosphere, pressure in atm
        public float maxVolume, maxPressure;

        // The minimum percent of health at wich the object is pressurizable
        public float minHealthPressurizable;

        // Whether the block can be pressurized to a custom pressure
        public bool pressurizable;

        // The allowed fluids
        public Fluid[] allowedFluids;
    }
}