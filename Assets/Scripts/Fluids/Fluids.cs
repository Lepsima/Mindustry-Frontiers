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

        // If this is a molecule ex: water = 0.6667% hidrogen, 0.3333% oxigen / setting to 2 hid and 1 oxigen will normalize to the previous values
        public (Element, float)[] composition;

        public Fluid(string name) : base(name) {

        }

        public Fluid(string name, (Element, float)[] composition) : base(name) {
            this.composition = composition;

            density = 0;
            float sum = composition.Sum(x => x.Item2);

            for (int i = 0; i < composition.Length; i++) {
                // Normalize
                composition[i] = (composition[i].Item1, composition[i].Item2 / sum);

                // Calculate total density
                density += composition[i].Item1.density * composition[i].Item2;
            }
        }

        public static (Element, float)[] With(params object[] items) {
            (Element, float)[] composite = new (Element, float)[items.Length / 2];
            for (int i = 0; i < items.Length; i += 2) composite[i / 2] = ((Element)items[i], (float)items[i + 1]);
            return composite;
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
        public static Fluid hidrogen, oxigen, nitrogen;

        // Mulit atom / molecules type fluids
        public static Fluid air, water, petroleum, kerosene, gasoline, jetFuel;

        public static Fluid atmosphericFluid;
        public static float atmosphericPressure = 1f;

        public static void Load() {
            hidrogen = new Fluid("fluid-hidrogen") {
                density = 0.08375f,
                compressionRatio = 0.55f,
                maxCompression = 5.4f,
            };

            oxigen = new Fluid("fluid-oxigen") {
                density = 1.428f,
                compressionRatio = 1.1f,
                maxCompression = 7.2f,
            };

            nitrogen = new Fluid("fluid-nitrogen") {
                density = 1.2506f,
                compressionRatio = 1.05f,
                maxCompression = 8.9f,
            };

            air = new Fluid("fluid-air", Fluid.With(nitrogen, 78f, oxigen, 22f)) {
                compressionRatio = 1.1f,
                maxCompression = 7.2f,
            };

            water = new Fluid("fluid-water", Fluid.With(hidrogen, 2f, oxigen, 1f)) {
                density = 997f,
                compressionRatio = 2f,
                maxCompression = 1.1f,
            };

            // I spent too much time searching that
            petroleum = new Fluid("fluid-petroleum", Fluid.With(Items.carbon, 75f, hidrogen, 11.5f, Items.sulfur, 6f, nitrogen, 4f, oxigen, 3.5f)) {
                density = 850f,
                compressionRatio = 3f,
                maxCompression = 1.5f,
            };

            kerosene = new Fluid("fluid-kerosene") {
                density = 800f,
                compressionRatio = 2.5f,
                maxCompression = 2f,
            };

            gasoline = new Fluid("fluid-gasoline") {
                density = 750f,
                compressionRatio = 2.5f,
                maxCompression = 1.4f,
            };

            jetFuel = new Fluid("fluid-jetFuel", Fluid.With(gasoline, 6f, kerosene, 4f)) {
                density = 804f,
                compressionRatio = 3.5f,
                maxCompression = 3f,
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

        // The allowed fluids
        public Fluid[] allowedFluids;
    }
}