using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Frontiers.Content;
using System.Linq;

namespace Frontiers.FluidSystem {

    public class Fluid : Element {
        // The atmospheres needed to half the volume (1 = neutral)
        public float compressionRatio = 1f;

        // The max amount this fluid can be compressed
        public float maxCompression = 1f;

        public Fluid(string name) : base(name) {

        }

        public Fluid(string name, (Element, float)[] composition) : base(name, composition) {

        }

        public float Compression(float pressure) {
            return Mathf.Min(Mathf.Max(pressure - 1f, Fluids.atmosphericPressure - 1f) * compressionRatio + 1f, maxCompression);
        }

        public float Volume(float liters, float pressure) {
            return liters / Compression(pressure);
        }

        public float Liters(float volume, float pressure) {
            return volume * Compression(pressure);
        }
    }

    // Class to handle and create in run-time custom molecules
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
        // Single atom type fluids
        public static Fluid hydrogen, oxigen, nitrogen;

        // Mulit atom / molecules type fluids
        public static Fluid air, water, co2, petroleum, kerosene, fuel;

        public static Fluid atmosphericFluid;
        public static float atmosphericPressure = 1f;

        public static void Load() {
            hydrogen = new Fluid("fluid-hydrogen") {
                color = new Color(0xd1, 0xe4, 0xff),
                density = 0.08375f,
                compressionRatio = 0.55f,
                maxCompression = 5.4f,
            };

            oxigen = new Fluid("fluid-oxigen") {
                color = new Color(0xff, 0xbd, 0xd4),
                density = 1.428f,
                compressionRatio = 1.1f,
                maxCompression = 7.2f,
            };

            nitrogen = new Fluid("fluid-nitrogen") {
                color = new Color(0xc9, 0xc9, 0xc9),
                density = 1.2506f,
                compressionRatio = 1.05f,
                maxCompression = 8.9f,
            };

            co2 = new Fluid("fluid-carbonDioxide", Element.With(Items.coal, 1f, oxigen, 2f)) {
                color = new Color(0x64, 0x65, 0x67),
                density = 1.87f,
                compressionRatio = 1.3f,
                maxCompression = 4f,
            };

            air = new Fluid("fluid-air", Element.With(nitrogen, 0.75f, oxigen, 0.2f, co2, 0.05f)) {
                color = new Color(0x8d, 0xeb, 0xbb),
                compressionRatio = 1.1f,
                maxCompression = 7.2f,
            };

            water = new Fluid("fluid-water", Element.With(hydrogen, 2f, oxigen, 1f)) {
                color = new Color(0x48, 0x6a, 0xcd),
                density = 997f,
                compressionRatio = 2f,
                maxCompression = 1.1f,
            };

            petroleum = new Fluid("fluid-petroleum", Element.With(Items.coal, 0.75f, hydrogen, 0.115f, Items.sulfur, 0.06f, nitrogen, 0.04f, oxigen, 0.035f)) {
                color = new Color(0x31, 0x31, 0x31),
                density = 850f,
                compressionRatio = 3f,
                maxCompression = 1.5f,
            };

            kerosene = new Fluid("fluid-kerosene") {
                color = new Color(0x84, 0xa9, 0x4b),
                density = 800f,
                compressionRatio = 2.5f,
                maxCompression = 2f,
            };

            fuel = new Fluid("fluid-fuel", Element.With(kerosene, 0.7f, Items.coal, 0.3f)) {
                color = new Color(0xff, 0xcd, 0x66),
                density = 804f,
                compressionRatio = 3.5f,
                maxCompression = 1.2f,
            };

            atmosphericFluid = air;
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

        // Whether this block can only output or input
        public bool outputOnly, inputOnly;

        // The allowed fluids
        public Fluid[] allowedFluids;
    }
}