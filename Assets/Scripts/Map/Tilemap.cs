using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using static Frontiers.Content.Maps.Map;

namespace Frontiers.Content.Maps {
    public class Tile {
        public Vector2Int position;
        public TileType[] tiles;
        public Block block;

        public Tile(Vector2Int position) {
            this.position = position;
            tiles = new TileType[(int)MapLayer.Total];
            block = null;
        }

        public void Set(TileType tileType, MapLayer layer) {
            tiles[(int)layer] = tileType;
        }

        public void Set(Block block) {
            this.block = block;
        }

        public TileType Layer(MapLayer layer) {
            return tiles[(int)layer];
        }

        public bool HasTile(MapLayer layer) {
            return tiles[(int)layer] != null;
        }

        public bool HasBlock() {
            return block != null;
        }

        public bool IsSolid() {
            return HasBlock() || HasTile(MapLayer.Solid);
        }

        public bool IsWall() {
            return HasTile(MapLayer.Solid);
        }

        public bool IsWalkable() {
            return tiles[(int)MapLayer.Ground] != null && !tiles[(int)MapLayer.Ground].isWater;
        }

        public bool AllowsBuildings() {
            return tiles[(int)MapLayer.Ground] != null && tiles[(int)MapLayer.Ground].allowBuildings;
        }

        public void Set(string data) {
            // Loads all layers of the tile from a single string
            // Used to recive map data across the network 
            for (int i = 0; i < data.Length; i++) {
                int id = Convert.ToInt32(data[i]) - 32;
                Set(TileLoader.GetTileTypeById((short)id), (MapLayer)i);
            }
        }

        public override string ToString() {
            // Writes all the ids of the tiles on all layers in a single string
            // Used to transfer the map data across the network
            string data = "";
            
            for (int i = 0; i < (int)MapLayer.Total; i++) {
                TileType tileType = tiles[i];
                data += tileType == null ? (char)32 : (char)(tileType.id + 32);
            }

            return data;
        }

        public char[] ToCharArray() {
            char[] data = new char[(int)MapLayer.Total];

            for (int i = 0; i < (int)MapLayer.Total; i++) {
                data[i] = tiles[i].ToChar();
            }

            return data;
        }

        public string[] ToNames() {
            // Saves the names of all the tiles to a list of strings
            // Used for faster tile management but ineficient storage
            string[] names = new string[(int)MapLayer.Total];

            for (int i = 0; i < names.Length; i++) {
                TileType tileType = tiles[i];
                names[i] = tileType == null ? null : tiles[i].name;
            }

            return names;
        }
    }

    public class Tilemap {
        public Tile[,] tilemap;
        public Region[,] regions;
        public int regionSize;
        public Vector2Int regionCount;
        public Vector2Int size;

        public struct Region {
            public Vector2Int offset;

            public RegionDisplayer displayer;
            public Tilemap tilemap;

            public Region(Tilemap tilemap, Vector2Int offset) {
                this.tilemap = tilemap;
                this.offset = offset;

                displayer = null;
                displayer = new RegionDisplayer(this);
            }

            public Vector2Int ToLocal(Vector2Int position) {
                return position - offset;
            }

            public Vector2Int ToWorld(Vector2Int position) {
                return position += offset;
            }

            public Tile GetTile(Vector2Int local) {
                Vector2Int world = ToWorld(local);
                return tilemap.GetTile(world);
            }

            public Tile[,] GetTiles() {
                int size = tilemap.regionSize;
                Tile[,] tiles = new Tile[size, size];

                for (int x = 0; x < size; x++) {
                    for (int y = 0; y < size; y++) {
                        tiles[x, y] = tilemap.GetTile(ToWorld(new Vector2Int(x, y)));
                    }
                }

                return tiles;
            }

            public int GetRenderedTileCount() {
                // Get the total tile count that should be renderer;
                int counter = 0;
                int size = tilemap.regionSize;

                for (int x = 0; x < size; x++) {
                    for (int y = 0; y < size; y++) {
                        Tile tile = tilemap.GetTile(ToWorld(new Vector2Int(x, y)));
                        if (tile == null) continue;

                        if (tile.Layer(MapLayer.Solid) != null) counter++;
                        else {
                            if (tile.Layer(MapLayer.Ground) != null) counter++;
                            if (tile.Layer(MapLayer.Ore) != null) counter++;
                        }
                    }
                }

                return counter;
            }

            public void UpdateMesh() {
                displayer.Update();
            }
        }

        public Tilemap(Vector2Int size, int regionSize) {
            // Store given parameters
            this.size = size;
            this.regionSize = regionSize;

            // Create empty tilemap
            tilemap = new Tile[size.x, size.y];
            for (int x = 0; x < size.x; x++) {
                for (int y = 0; y < size.y; y++) {
                    tilemap[x, y] = new Tile(new Vector2Int(x, y));
                }
            }

            // Create the region array
            regionCount = new(Mathf.CeilToInt((float)size.x / regionSize), Mathf.CeilToInt((float)size.y / regionSize));
            regions = new Region[regionCount.x, regionCount.y];

            // Create all the region structs
            for (int x = 0; x < regionCount.x; x++) {
                for (int y = 0; y < regionCount.y; y++) {
                    regions[x, y] = new Region(this, new Vector2Int(x, y) * regionSize);
                }
            }
        }

        public bool InBounds(Vector2 position) {
            return position.x >= 0 && position.x < size.x && position.y >= 0 && position.y < size.y;
        }

        public void UpdateMesh() {
            // Used when updating the whole map is needed, to not call update 5000000 times, only once finished
            for (int x = 0; x < regions.GetLength(0); x++) {
                for (int y = 0; y < regions.GetLength(1); y++) {
                    regions[x, y].UpdateMesh();
                }
            }
        }

        public Tile GetTile(Vector2Int position) {
            return tilemap[position.x, position.y];
        }

        public TileType GetTileType(Vector2Int position, MapLayer layer) {
            // Get a tile type from a region
            return GetTile(position).Layer(layer);
        }

        public void SetTile(TileType tileType, Vector2Int position, MapLayer layer) {
            // Set a region's tile
            GetTile(position).Set(tileType, layer);
        }

        public void SetBlock(Vector2Int position, Block block) {
            // Set a block on a tile
            GetTile(position).Set(block);
        }

        public void SetTile(Vector2Int position, string data) {
            // Load a tile from the given string data
            GetTile(position).Set(data);
        }
    }
}