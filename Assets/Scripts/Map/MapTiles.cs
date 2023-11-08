using Frontiers.Assets;
using Frontiers.FluidSystem;
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Random = UnityEngine.Random;

namespace Frontiers.Content.Maps {
    public static class TileLoader {
        public static Dictionary<string, TileType> loadedTiles = new();

        public static void HandleTile(TileType tileType) {
            tileType.id = (short)loadedTiles.Count;
            if (tileType.name == null) tileType.name = "content num. " + tileType.id;

            if (GetTileTypeByName(tileType.name) != null) throw new ArgumentException("Two tile types cannot have the same name! (issue: '" + tileType.name + "')");
            loadedTiles.Add(tileType.name, tileType);
        }

        public static TileType GetTileTypeById(short id) {
            if (id == 255 || loadedTiles.Count <= id) return null;
            return loadedTiles.ElementAt(id).Value;
        }

        public static TileType GetTileTypeByName(string name) {
            if (name == null || !loadedTiles.ContainsKey(name)) return null;
            return loadedTiles[name];
        }

        public static TileType[] GetLoadedTiles() {
            return loadedTiles.Values.ToArray();
        }

        public static T[] GetTileTypeByType<T>() where T : TileType {
            List<T> foundMatches = new();
            foreach (TileType tileType in loadedTiles.Values) if (ContentLoader.TypeEquals(tileType.GetType(), typeof(T))) foundMatches.Add(tileType as T);
            return foundMatches.ToArray();
        }

        public static int GetTileTypeCountOfType<T>() where T : TileType {
            int count = 0;
            foreach (TileType tileType in loadedTiles.Values) if (ContentLoader.TypeEquals(tileType.GetType(), typeof(T))) count++;
            return count;
        }
    }

    public class TileType {
        public string name;
        public short id;
        public Sprite sprite;

        public Sprite[] allVariantSprites;
        private Vector4[] allVariantSpriteUVs;

        public Element drop;
        public Color color;

        public int variants;
        public bool allowBuildings = true, flammable = false, isWater = false;

        public TileType(string name, int variants = 1, Element drop = null) {
            this.name = name;
            TileLoader.HandleTile(this);
            sprite = AssetLoader.GetSprite(name);

            this.drop = drop;
            this.variants = variants;

            if (this.variants < 1) this.variants = 1;

            allVariantSprites = new Sprite[variants];
            allVariantSpriteUVs = new Vector4[variants];

            allVariantSprites[0] = sprite;
            for (int i = 1; i < this.variants; i++) allVariantSprites[i] = AssetLoader.GetAsset<Sprite>(name + (i + 1));
        }

        public virtual Sprite[] GetAllTiles() {
            if (variants == 1) return new Sprite[1] { sprite };
            else return allVariantSprites;
        }

        public virtual Sprite GetRandomTileVariant() {
            if (variants == 1) return sprite;
            return allVariantSprites[Random.Range(0, variants - 1)];
        }

        public void SetSpriteUV(int index, Vector2 uv00, Vector2 uv11) {
            allVariantSpriteUVs[index] = new Vector4(uv00.x, uv00.y, uv11.x, uv11.y);
        }

        public Vector4 GetUV() {
            return GetSpriteVariantUV(0);
        }

        public Vector4 GetSpriteVariantUV(int index) {
            return allVariantSpriteUVs[index];
        }
    }

    public class OreTileType : TileType {
        public float oreThreshold, oreScale;

        public OreTileType(string name, int variants, Item itemDrop) : base(name, variants, itemDrop) {

        }
    }

    public class Tiles {
        public const TileType none = null;
        //Base tiles
        public static TileType darksandWater, darksand, deepWater, grass, ice, metalFloor, metalFloor2, metalFloorWarning, metalFloorDamaged, sandFloor, sandWater, shale, snow, stone, water;

        //Ore tiles
        public static TileType coalOre, copperOre, goldOre, ironOre, lithiumOre, magnesiumOre, nickelOre, thoriumOre;

        //Wall tiles
        public static TileType daciteWall, dirtWall, duneWall, iceWall, saltWall, sandWall, shaleWall, grassWall, snowWall, stoneWall;

        public static void Load() {
            darksandWater = new TileType("darksand-water", 1, Fluids.water) {
                isWater = true,
            };

            darksand = new TileType("darksand", 3, Items.sand);

            deepWater = new TileType("deep-water", 1, Fluids.water) {
                allowBuildings = false,
                isWater = true,
            };

            grass = new TileType("grass", 3);

            // Testing only
            ice = new TileType("ice", 3, Fluids.water);

            metalFloor = new TileType("metal-floor");

            metalFloor2 = new TileType("metal-floor-2");

            metalFloorWarning = new TileType("metal-floor-warning");

            metalFloorDamaged = new TileType("metal-floor-damaged", 3);

            sandFloor = new TileType("sand-floor", 3, Items.sand);

            sandWater = new TileType("sand-water", 1, Fluids.water) {
                isWater = true,
            };

            // This makes a bit more sense, but it's only for testing
            shale = new TileType("shale", 3, Fluids.petroleum);

            snow = new TileType("snow", 3);

            stone = new TileType("stone", 3);

            water = new TileType("water", 1, Fluids.water) {
                allowBuildings = false,
                isWater = true,
            };

            // Ores
            coalOre = new OreTileType("ore-coal", 3, Items.coal);
            copperOre = new OreTileType("ore-copper", 3, Items.copper);
            goldOre = new OreTileType("ore-gold", 3, Items.gold);
            ironOre = new OreTileType("ore-iron", 3, Items.iron);
            lithiumOre = new OreTileType("ore-lithium", 3, Items.lithium);
            magnesiumOre = new OreTileType("ore-magnesium", 3, Items.magnesium);
            nickelOre = new OreTileType("ore-nickel", 3, Items.nickel);
            thoriumOre = new OreTileType("ore-thorium", 3, Items.thorium);

            // Walls
            daciteWall = new TileType("dacite-wall", 2);
            dirtWall = new TileType("dirt-wall", 2);
            duneWall = new TileType("dune-wall", 2);
            iceWall = new TileType("ice-wall", 2);
            saltWall = new TileType("salt-wall", 2);
            sandWall = new TileType("sand-wall", 2);
            shaleWall = new TileType("shale-wall", 2);
            grassWall = new TileType("shrubs", 2);
            snowWall = new TileType("snow-wall", 2);
            stoneWall = new TileType("stone-wall", 2);
        }
    }
}