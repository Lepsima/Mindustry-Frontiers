using System.Collections.Generic;
using UnityEngine;
using Frontiers.Settings;
using System;

namespace Frontiers.Content.Maps {
    public class Map {
        public string name;

        public List<Entity> loadedEntities = new();
        public Tilemap tilemap;

        public Dictionary<Vector2Int, Block> blockPositions = new();
        public List<Block> blocks = new();
        public List<Unit> units = new();

        public MapData mapData;

        public Vector2Int size;
        public bool loaded;

        // All the layers on the map, the last enum should have as number the amount of layers without counting itself
        public enum MapLayer {
            Ground = 0,
            Ore = 1,
            Solid = 2,
            Total = 3
        }

        public Map(string name, int width, int height, Tilemap tilemap) {
            // Create a map
            this.name = name;
            this.tilemap = tilemap;
            size = new(width, height);

            loaded = true;
        }

        public Map(string name, MapData mapData) {
            // Create a map with the given data

            // Store basic values
            this.name = name;
            this.mapData = mapData;
            size = mapData.size;

            // Create an empty tilemap
            tilemap = new(size, Vector2Int.one * Main.RegionSize);

            // Fill the tilemap with the given tile data
            LoadTilemapData(mapData.tilemapData.DecodeThis());

            // End loading
            loaded = true;

            /*for (int x = 0; x < size.x; x++) {
                for (int y = 0; y < size.y; y++) {
                    Debug.Log(GetMapTileTypeAt(MapLayer.Ground, new Vector2(x, y)));
                }
            }
            */
        }

        public void LoadTilemapData(string[,,] tileNameArray) {
            // Load the tilemap from an array of the names of the tiles 

            // Get the amount of layers
            int layers = (int)MapLayer.Total;

            tilemap.HoldMeshUpdate(true);

            for (int x = 0; x < size.x; x++) {
                for (int y = 0; y < size.y; y++) {
                    for (int z = 0; z < layers; z++) {
                        // Get the tile corresponding to the name and set on the tilemap
                        string name = tileNameArray[x, y, z];
                        if (name != null) tilemap.SetTile(GetTileType(name), new Vector2Int(x, y), (MapLayer)z);
                    }
                }
            }

            tilemap.HoldMeshUpdate(false);
        }

        public string[,,] SaveTilemapData() {
            // Save the current state of the tilemap into an array of strings with the names of all the tiles

            // Initialize arrays
            int layers = (int)MapLayer.Total;
            string[,,] returnArray = new string[size.x, size.y, layers];

            for (int x = 0; x < size.x; x++) {
                for (int y = 0; y < size.y; y++) {

                    // Get the names of the current tile
                    string[] names = tilemap.GetTile(new Vector2Int(x, y)).ToNames();

                    // Store each given name on the main array
                    for (int layer = 0; layer < layers; layer++) {
                        string name = names[layer];
                        returnArray[x, y, layer] = name;
                    }
                }
            }
            return returnArray;
        }


        public string[] TilemapsToStringArray() {
            // Encode the current state of the tilemap to a single string array
            // Used for network transmission

            // Initialize arrays

            // Split into various strings to pass over the string character limit
            string[] tileData = new string[Mathf.CeilToInt(size.x * size.y * (int)MapLayer.Total / MapLoader.TilesPerString) + 1];
            int i = 0;

            for (int x = 0; x < size.x; x++) {
                for (int y = 0; y < size.y; y++) {
                    // Get the tile and encode it's tile types id's
                    int stringIndex = Mathf.FloorToInt(i / MapLoader.TilesPerString);
                    tileData[stringIndex] += tilemap.GetTile(new Vector2Int(x, y)).ToString();
                    i++;
                }
            }

            return tileData;
        }

        public void SetTilemapsFromStringArray(Vector2Int size, string[] tileData) {
            // Load the given encoded string array

            // Initialize vars
            int layers = (int)MapLayer.Total;
            int i = 0;

            for (int x = 0; x < size.x; x++) {
                for (int y = 0; y < size.y; y++) {
                    // Get the current substring index
                    int stringIndex = Mathf.FloorToInt(i / MapLoader.TilesPerString);

                    // Split the substring into smaller string that each contain a single's tile data
                    string[] subTileData = (string[])tileData[stringIndex].Split(layers);

                    // Load each data string into a tile
                    for (int z = 0; z < subTileData.Length; z++) {
                        string data = subTileData[z];
                        tilemap.SetTile(new Vector2Int(x, y), data);
                    }

                    i++;
                }
            }
        }

        public void Save() {
            mapData = new MapData(this);
        }

        public MapData GetMapData() {
            return mapData;
        }

        #region - Tilemaps -

        public bool IsInBounds(Vector2 position) {
            return position.x >= 0 && position.x < size.x && position.y >= 0 && position.y < size.y;
        }

        public TileType GetTileType(string name) {
            // Get a tile type from a tile name
            return TileLoader.GetTileTypeByName(name);
        }

        public TileType GetMapTileTypeAt(MapLayer layer, Vector2 position) {
            // Get a tile on a certain layer
            return tilemap.GetTile(Vector2Int.CeilToInt(position)).Layer(layer);
        }

        public bool CanPlaceBlockAt(Vector2Int position, int size) {
            // Check if a block could be placed at the given position

            // If the block is 1-sized, check only the given pos.
            if (size == 1) return CanPlaceBlockAt(position);

            // If the block is multiple-sized, check each tile it would occupy
            for (int x = 0; x < size; x++) {
                for (int y = 0; y < size; y++) {
                    Vector2Int sizePosition = position + new Vector2Int(x, y);
                    if (!CanPlaceBlockAt(sizePosition)) return false;
                }
            }

            return true;
        }

        public bool CanPlaceBlockAt(Vector2Int position) {
            // Check if a block could be placed on a tile
            Tile tile = tilemap.GetTile(position);

            if (!tile.Layer(MapLayer.Ground).allowBuildings) return false;
            else if (tile.IsSolid()) return false;

            return true;
        }

        public void PlaceTile(MapLayer layer, Vector2Int position, TileType tile, int size) {
            // Place a square of the given size of tiles (i think i wont use it until a long time)

            for (int x = 0; x < size; x++) {
                for (int y = 0; y < size; y++) {
                    Vector2Int sizePosition = position + new Vector2Int(x, y);
                    PlaceTile(layer, sizePosition, tile);
                }
            }
        }

        public void PlaceTile(MapLayer layer, Vector2Int position, TileType tile) {
            // Change a tile on the tilemap, this won't update the region mesh as of now
            tilemap.SetTile(tile, position, layer);
        }

        public void PlaceBlock(Block block, Vector2Int position) {
            // Set the given block to all the tiles it occupies
            int size = block.Type.size;

            for (int x = 0; x < size; x++) {
                for (int y = 0; y < size; y++) {
                    Vector2Int sizePosition = position + new Vector2Int(x, y);

                    // Link position and tile to the given block
                    blockPositions.Add(sizePosition, block);
                    tilemap.SetBlock(sizePosition, block);
                }
            }
        }

        public void RemoveBlock(Block block, Vector2Int position) {
            // Set null the block to all the tiles it occupied
            int size = block.Type.size;

            for (int x = 0; x < size; x++) {
                for (int y = 0; y < size; y++) {
                    Vector2Int sizePosition = position + new Vector2Int(x, y);

                    // Remove the position and tile link of the given block
                    blockPositions.Remove(sizePosition);
                    tilemap.SetBlock(sizePosition, null);
                }
            }
        }

        public string[] BlocksToStringArray() {
            // Store all the blocks into a string array
            // Used for network transfer
            string[] blockArray = new string[Mathf.Max(blocks.Count, 2)];
            for (int i = 0; i < blockArray.Length; i++) blockArray[i] = blocks.Count <= i ? "null" : blocks[i].ToString();
            return blockArray;
        }

        public void SetBlocksFromStringArray(string[] blockData) {
            // Da fuk
            for (int i = 0; i < blockData.Length; i++) {
                string data = blockData[i];
                if (data == "null") continue;

                string[] values = data.Split(':');

                int syncID = int.Parse(values[0]);
                short contentID = short.Parse(values[1]);
                byte teamCode = byte.Parse(values[2]);
                float health = float.Parse(values[3]);
                Vector2 position = new(float.Parse(values[4]), float.Parse(values[5]));
                int orientation = int.Parse(values[6]);

                Block block = MapManager.Instance.InstantiateBlock(position, orientation, contentID, syncID, teamCode);
                block.SetHealth(health);
            }
        }

        #endregion

        public Entity GetClosestEntity(Vector2 position, byte teamCode) {
            Entity closestEntity = null;
            float closestDistance = 99999f;

            foreach (Entity entity in loadedEntities) {
                //If content doesn't match the filter, skip
                if (!(entity.GetTeam() == teamCode)) continue;

                //Get distance to content
                float distance = Vector2.Distance(position, entity.GetPosition());

                //If distance is lower than previous closest distance, set this as the closest content
                if (distance < closestDistance) {
                    closestDistance = distance;
                    closestEntity = entity;
                }
            }

            return closestEntity;
        }

        public Entity GetClosestEntity(Vector2 position, Type type, byte teamCode) {
            Entity closestEntity = null;
            float closestDistance = 99999f;

            foreach (Entity entity in loadedEntities) {
                //If content doesn't match the filter, skip
                if (entity.GetTeam() != teamCode || !TypeEquals(entity.GetType(), type)) continue;

                //Get distance to content
                float distance = Vector2.Distance(position, entity.GetPosition());

                //If distance is lower than previous closest distance, set this as the closest content
                if (distance < closestDistance) {
                    closestDistance = distance;
                    closestEntity = entity;
                }
            }

            return closestEntity;
        }

        public Entity GetClosestEntityStrict(Vector2 position, Type type, byte teamCode) {
            Entity closestEntity = null;
            float closestDistance = 99999f;

            foreach (Entity entity in loadedEntities) {
                //If content doesn't match the filter, skip
                if (entity.GetTeam() != teamCode || entity.GetType() != type) continue;

                //Get distance to content
                float distance = Vector2.Distance(position, entity.GetPosition());

                //If distance is lower than previous closest distance, set this as the closest content
                if (distance < closestDistance) {
                    closestDistance = distance;
                    closestEntity = entity;
                }
            }

            return closestEntity;
        }

        public Entity GetClosestEntityInView(Vector2 position, Vector2 direction, float fov, Type type, byte teamCode) {
            Entity closestEntity = null;
            float closestDistance = 99999f;

            foreach (Entity entity in loadedEntities) {
                //If content doesn't match the filter, skip
                if (entity.GetTeam() != teamCode || !TypeEquals(entity.GetType(), type)) continue;

                //If is not in view range continue to next
                float cosAngle = Vector2.Dot((entity.GetPosition() - position).normalized, direction);
                float angle = Mathf.Acos(cosAngle) * Mathf.Rad2Deg;


                //Get distance to content
                float distance = Vector2.Distance(position, entity.GetPosition());
                if (angle > fov) continue;

                //If distance is lower than previous closest distance, set this as the closest content
                if (distance < closestDistance) {
                    closestDistance = distance;
                    closestEntity = entity;
                }
            }

            return closestEntity;
        }

        #region - Blocks -

        public static bool TypeEquals(Type target, Type reference) => target == reference || target.IsSubclassOf(reference);

        public Block GetClosestBlock(Vector2 position, Type type, byte teamCode) {
            Block closestBlock = null;
            float closestDistance = 99999f;

            foreach (Block block in blocks) {
                //If block doesn't match the filter, skip
                if (block.GetTeam() != teamCode || !TypeEquals(block.GetType(), type)) continue;

                //Get distance to block
                float distance = Vector2.Distance(position, block.GetPosition());

                //If distance is lower than previous closest distance, set this as the closest block
                if (distance < closestDistance) {
                    closestDistance = distance;
                    closestBlock = block;
                }
            }

            return closestBlock;
        }

        public LandPadBlock GetBestAvilableLandPad(Unit forUnit) {
            LandPadBlock bestLandPad = null;
            float smallestSize = 999999f;
            float closestDistance = 999999f;

            foreach (Block block in blocks) {
                if (block is LandPadBlock landPad) {

                    //If block doesn't match the filter, skip
                    if (!landPad.CanLand(forUnit)) continue;

                    //Get size to block
                    float size = landPad.Type.unitSize;

                    //If size is lower than previous closest distance, set this as the closest block
                    // If the landpad isn't smaller but is equally sized, check if it's closer
                    if (size < smallestSize) {
                        smallestSize = size;
                        bestLandPad = landPad;

                    } else if (size == smallestSize) {
                        //Get distance to block
                        float distance = Vector2.Distance(forUnit.GetPosition(), landPad.GetPosition());

                        //If distance is lower than previous closest distance, set this as the closest block
                        if (distance < closestDistance) {
                            closestDistance = distance;
                            bestLandPad = landPad;
                        }
                    }
                }
            }

            return bestLandPad;
        }

        public Block GetBlockAt(Vector2Int position) {
            return blockPositions.TryGetValue(position, out Block block) ? block : null;
        }

        public (List<ItemBlock>, List<int>) GetAdjacentBlocks(ItemBlock itemBlock) {
            // Create the adjacent block list
            List<ItemBlock> adjacentBlocks = new();
            List<int> adjacentBlockOrientations = new();

            int size = (int)itemBlock.size;

            // Get the block's position
            Vector2Int position = itemBlock.GetGridPosition();

            // Check for adjacent blocks in all 4 sides
            for (int y = 0; y < size; y++) Handle(-1, y, 2);
            for (int y = 0; y < size; y++) Handle(size, y, 0);
            for (int x = 0; x < size; x++) Handle(x, -1, 3);
            for (int x = 0; x < size; x++) Handle(x, size, 1);

            // Check if a block exists in (x, y)
            void Handle(int x, int y, int o) {
                Vector2Int offset = new(x, y);
                ItemBlock block = (ItemBlock)GetBlockAt(offset + position);

                if (block != null && itemBlock != block && !adjacentBlocks.Contains(block)) {
                    adjacentBlocks.Add(block);
                    adjacentBlockOrientations.Add(o);
                }
            }

            return (adjacentBlocks, adjacentBlockOrientations);
        }

        public void AddBlock(Block block) {
            // Add block to all lists
            blocks.Add(block);
            loadedEntities.Add(block);
            Client.syncObjects.Add(block.SyncID, block);

            // Place the block
            PlaceBlock(block, block.GetGridPosition());
        }

        public void RemoveBlock(Block block) {
            // Remove block from all lists
            blocks.Remove(block);
            loadedEntities.Remove(block);
            Client.syncObjects.Remove(block.SyncID);

            // Remove the block
            RemoveBlock(block, block.GetGridPosition());
        }

        #endregion

        #region - Units -

        public void AddUnit(Unit unit) {
            units.Add(unit);
            loadedEntities.Add(unit);
        }

        public void RemoveUnit(Unit unit) {
            units.Remove(unit);
            loadedEntities.Remove(unit);
        }

        #endregion
    }

}