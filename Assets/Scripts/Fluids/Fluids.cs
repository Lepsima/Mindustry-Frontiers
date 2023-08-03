using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Frontiers.Content;
using System.Linq;

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
            return (Mathf.Clamp(pressure - 1f, Fluids.atmPressure - 1f, maxCompression) * compressionRatio + 1f);
        }

        public float Volume(float liters, float pressure) {
            return liters / Compression(pressure);
        }

        public float Liters(float volume, float pressure) {
            return volume * Compression(pressure);
        }
    }

    public class FluidComposite {
        // all fluids and their percentages
        public Dictionary<Fluid, float> fluids = new();
        public float density, maxCompression, compressionRatio;

        public FluidComposite((Fluid, float)[] fluids) {
            this.fluids = CreateDictionary(fluids);
            density = Density();
            maxCompression = MaxCompression();
            compressionRatio = CompressionRatio();
        }

        public Dictionary<Fluid, float> CreateDictionary((Fluid, float)[] fluids) {
            Dictionary<Fluid, float> dictionary = new();

            // Normalize dictionary values to prevent miscalculations
            float sum = fluids.Sum(x => x.Item2);
            for (int i = 0; i < fluids.Length; i++) dictionary.Add(fluids[i].Item1, fluids[i].Item2 / sum);

            return dictionary;
        }

        public float Density() {
            float density = 0;
            foreach (Fluid fluid in fluids.Keys) density += fluid.density * fluids[fluid];
            return density;
        }

        public float MaxCompression() {
            // Returns the lowest maxCompression of all fluids
            float compression = float.MaxValue;
            foreach (Fluid fluid in fluids.Keys) compression = Mathf.Min(compression, fluid.maxCompression);
            return compression;
        }

        public float CompressionRatio() {
            // Returns the lowest compressionRatio of all fluids
            float compressionRatio = float.MaxValue;
            foreach (Fluid fluid in fluids.Keys) compressionRatio = Mathf.Min(compressionRatio, fluid.compressionRatio);
            return compressionRatio;
        }
    }

    public class Fluids {
        public static Fluid air, water;

        public static FluidComposite atmFluid;
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

            atmFluid = new FluidComposite(new (Fluid, float)[1] { (air, 1) });
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