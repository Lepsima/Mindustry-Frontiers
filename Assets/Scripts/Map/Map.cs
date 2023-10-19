using System.Collections.Generic;
using UnityEngine;
using Frontiers.Settings;
using System;
using System.Linq;

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

        public event EventHandler<Unit> OnUnitCreated;
        public event EventHandler<Unit> OnUnitRemoved;
        public event EventHandler<Block> OnBlockCreated;
        public event EventHandler<Block> OnBlockRemoved;

        // All the layers on the map, the last enum should have as number the amount of layers without counting itself
        public enum MapLayer {
            Ground = 0,
            Ore = 1,
            Solid = 2,
            Total = 3
        }

        public Map(string name, int width, int height) {
            // Create a map
            this.name = name;
            size = new(width, height);

            // Create an empty tilemap
            tilemap = new(size, Main.Map_RegionSize);

            // End loading
            loaded = true;
        }

        public Map(string name, MapData mapData) {
            // Create a map with the given data

            // Store basic values
            this.name = name;
            this.mapData = mapData;
            size = mapData.size;

            // Create an empty tilemap
            tilemap = new(size, Main.Map_RegionSize);

            // Fill the tilemap with the given tile data
            LoadTilemapData(mapData.tilemapData.DecodeThis());

            // End loading
            loaded = true;
        }

        public Map(string name, Vector2Int size, byte[] tilemap) {
            // Create tilemap
            this.name = name;
            this.size = size;
            this.tilemap = new(size, Main.Map_RegionSize);

            // Load data
            TilemapFromBytes(tilemap);

            // End loading
            loaded = true;
        }

        public void LoadTilemapData(string[,,] tileNameArray) {
            // Load the tilemap from an array of the names of the tiles 

            // Get the amount of layers
            int layers = (int)MapLayer.Total;

            for (int x = 0; x < size.x; x++) {
                for (int y = 0; y < size.y; y++) {
                    for (int z = 0; z < layers; z++) {
                        // Get the tile corresponding to the name and set on the tilemap
                        string name = tileNameArray[x, y, z];
                        if (name != null) tilemap.SetTile(GetTileType(name), new Vector2Int(x, y), (MapLayer)z);
                    }
                }
            }

            tilemap.UpdateMesh();
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

        public byte[] TilemapToBytes() {
            // Split Strings (value not scalable) => 552.75ms, 20 samples
            // Char array => 120.78ms, 100 samples
            // Alt char array (current method) => 13.51ms :O, 100 samples

            char[] tileMapData = new char[size.x * size.y * (int)MapLayer.Total];
            int layers = (int)MapLayer.Total;
            int i = 0;

            for (int x = 0; x < size.x; x++) {
                for (int y = 0; y < size.y; y++) {
                    Tile tile = tilemap.GetTile(new Vector2Int(x, y));

                    for (int c = 0; c < layers; c++) {
                        tileMapData[i] = tile.tiles[c].ToChar();
                        i++;
                    }
                }
            }

            return DataCompressor.Zip(new string(tileMapData));
        }

        public byte[] BlocksToBytes(bool includeSyncID) {
            // Compress block data
            string blockData = "";

            foreach (Block block in blocks) {
                blockData += block.SaveDataToString(includeSyncID) + ",";
            }

            return DataCompressor.Zip(blockData);
        }

        public byte[] UnitsToBytes(bool includeSyncID) {
            // Compress unit data
            string unitData = "";

            foreach (Unit unit in units) {
                unitData += unit.SaveDataToString(includeSyncID) + ",";
            }

            return DataCompressor.Zip(unitData);
        }

        public void TilemapFromBytes(byte[] bytes) {
            // Decompress
            char[] tilemapData = DataCompressor.Unzip(bytes).ToCharArray();

            // Initialize vars
            int layers = (int)MapLayer.Total;
            int i = 0;

            // Load each tile
            for (int x = 0; x < size.x; x++) {
                for (int y = 0; y < size.y; y++) {
                    Tile tile = tilemap.GetTile(new Vector2Int(x, y));

                    for (int c = 0; c < layers; c++) {
                        tile.tiles[c] = tilemapData[i].ToType();
                        i++;
                    }
                }
            }

            // Refresh mesh
            tilemap.UpdateMesh();
        }

        public void BlocksFromBytes(byte[] bytes) {
            // Delete all blocks
            foreach (Block block in blocks) {
                MapManager.Instance.DeleteBlock(block, false);
            }

            // Decompress and split into chunks
            string data = DataCompressor.Unzip(bytes);
            if (string.IsNullOrEmpty(data)) return;

            // Split per block
            string[] blockDataArray = data.Split(',');

            for (int i = 0; i < blockDataArray.Length - 1; i++) {
                string blockData = blockDataArray[i];
                if (string.IsNullOrEmpty(blockData)) continue;

                string[] blockValues = blockData.Split(':');

                // Entity parameters
                short syncID = short.Parse(blockValues[0]);
                short contentID = short.Parse(blockValues[1]);
                byte teamCode = byte.Parse(blockValues[2]);

                // Block parameters
                int positionIndex = int.Parse(blockValues[4]);
                byte orientation = byte.Parse(blockValues[5]);

                Vector2 position = new(positionIndex / size.x, positionIndex % size.y);

                Block block = MapManager.Instance.InstantiateBlock(position, orientation, contentID, syncID, teamCode);
                block.ApplySaveData(blockValues);
            }    
        }

        public void UnitsFromBytes(byte[] bytes) {
            // Delete all units
            foreach (Unit unit in units) {
                MapManager.Instance.DeleteUnit(unit, false);
            }

            // Decompress and split into chunks
            string data = DataCompressor.Unzip(bytes);
            if (string.IsNullOrEmpty(data)) return;

            string[] unitDataArray = data.Split(',');

            foreach(string unitData in unitDataArray) {
                string[] unitValues = unitData.Split(':');

                // Entity parameters
                short syncID = short.Parse(unitValues[0]);
                short contentID = short.Parse(unitValues[1]);
                byte teamCode = byte.Parse(unitValues[2]);

                // Unit parameters
                Vector2 position = new(int.Parse(unitValues[5]) / 1000f, int.Parse(unitValues[6]) / 1000f);
                float rotation = short.Parse(unitValues[7]) / 1000f;

                Unit unit = MapManager.Instance.InstantiateUnit(position, rotation, contentID, syncID, teamCode);
                unit.ApplySaveData(unitValues);
            }
        } 

        public void Save() {
            mapData = new MapData(this);
        }

        public MapData GetMapData() {
            return mapData;
        }

        #region - Tilemaps -

        public bool InBounds(Vector2 position) {
            return tilemap.InBounds(position);
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
            return tile.AllowsBuildings() && !tile.IsSolid();
        }

        public void PlaceTile(MapLayer layer, Vector2Int position, TileType tile) {
            // Change a tile on the tilemap, this won't update the region mesh as of now
            tilemap.SetTile(tile, position, layer);
        }

        public void UpdatePlaceTile(MapLayer layer, Vector2Int position, TileType tile) {
            // Change a tile on the tilemap, this won't update the region mesh as of now
            tilemap.SetTile(tile, position, layer);
            tilemap.GetRegion(position).UpdateMesh();
        }

        public void UpdateRegion(Vector2Int position) {
            tilemap.GetRegion(position).UpdateMesh();
        }

        public void CreateUnitFromString(string data) {
            // Not now, please
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

        public List<Entity> GetAllEntitiesInRange(Vector2 position, float range) {
            List<Entity> entities = new();

            foreach (Entity entity in loadedEntities) {
                //If distance is lower than previous closest distance, set this as the closest content
                if (Vector2.Distance(position, entity.GetPosition()) <= range) entities.Add(entity);
            }

            return entities;
        }

        public List<Entity> GetAllEntitiesInRange(Vector2 position, float range, Type type) {
            List<Entity> entities = new();

            foreach (Entity entity in loadedEntities) {
                //If content doesn't match the filter, skip
                if (!TypeEquals(entity.GetType(), type)) continue;

                //Get distance to content
                float distance = Vector2.Distance(position, entity.GetPosition());

                //If distance is lower than previous closest distance, set this as the closest content
                if (distance <= range) entities.Add(entity);
            }

            return entities;
        }

        public List<Entity> GetAllEntitiesInRange(Vector2 position, float range, byte teamCode) {
            List<Entity> entities = new();

            foreach (Entity entity in loadedEntities) {
                //If content doesn't match the filter, skip
                if (entity.GetTeam() != teamCode) continue;

                //Get distance to content
                float distance = Vector2.Distance(position, entity.GetPosition());

                //If distance is lower than previous closest distance, set this as the closest content
                if (distance <= range) entities.Add(entity);
            }

            return entities;
        }

        public List<Entity> GetAllEntitiesInRange(Vector2 position, float range, Type type, byte teamCode) {
            List<Entity> entities = new();

            foreach (Entity entity in loadedEntities) {
                //If content doesn't match the filter, skip
                if (entity.GetTeam() != teamCode || !TypeEquals(entity.GetType(), type)) continue;

                //Get distance to content
                float distance = Vector2.Distance(position, entity.GetPosition());

                //If distance is lower than previous closest distance, set this as the closest content
                if (distance <= range) entities.Add(entity);
            }

            return entities;
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

        public (List<ItemBlock>, List<int>) GetAdjacentItemBlocks(ItemBlock from) {
            // Create the adjacent block list
            List<ItemBlock> adjacentBlocks = new();
            List<int> adjacentBlockOrientations = new();

            int size = (int)from.size;

            // Get the block's position
            Vector2Int position = from.GetGridPosition();

            // Check for adjacent blocks in all 4 sides
            for (int y = 0; y < size; y++) Handle(-1, y, 2);
            for (int y = 0; y < size; y++) Handle(size, y, 0);
            for (int x = 0; x < size; x++) Handle(x, -1, 3);
            for (int x = 0; x < size; x++) Handle(x, size, 1);

            // Check if a block exists in (x, y)
            void Handle(int x, int y, int o) {
                Vector2Int offset = new(x, y);

                if (GetBlockAt(offset + position) is ItemBlock block) {
                    if (block == null || from == block || adjacentBlocks.Contains(block)) return;
                    adjacentBlocks.Add(block);
                    adjacentBlockOrientations.Add(o);
                }
            }

            return (adjacentBlocks, adjacentBlockOrientations);
        }

        public List<IPowerable> GetAdjacentPowerBlocks(Block from) {
            // Create the adjacent block list
            List<IPowerable> adjacentBlocks = new();
            int size = (int)from.size;

            // Get the block's position
            Vector2Int position = from.GetGridPosition();

            // Check for adjacent blocks in all 4 sides
            for (int y = 0; y < size; y++) Handle(-1, y);
            for (int y = 0; y < size; y++) Handle(size, y);
            for (int x = 0; x < size; x++) Handle(x, -1);
            for (int x = 0; x < size; x++) Handle(x, size);

            // Check if a block exists in (x, y)
            void Handle(int x, int y) {
                Block block = GetBlockAt(new Vector2Int(x, y) + position);

                bool validBlock = !(block == null || from == block || !block.UsesPower() || adjacentBlocks.Contains(block));
                bool validConnection = from.TransfersPower() || block.TransfersPower();

                if (validBlock && validConnection) adjacentBlocks.Add(block);
            }

            return adjacentBlocks;
        }

        public void AddBlock(Block block) {
            // Add block to all lists
            blocks.Add(block);
            loadedEntities.Add(block);
            Client.syncObjects.Add(block.SyncID, block);

            // Set the given block to all the tiles it occupies
            int size = block.Type.size;
            Vector2Int position = block.GetGridPosition();

            for (int x = 0; x < size; x++) {
                for (int y = 0; y < size; y++) {
                    Vector2Int sizePosition = position + new Vector2Int(x, y);

                    // Link position and tile to the given block
                    blockPositions.Add(sizePosition, block);
                    tilemap.SetBlock(sizePosition, block);
                }
            }

            OnBlockCreated?.Invoke(this, block);
        }

        public void RemoveBlock(Block block) {
            // Remove block from all lists
            blocks.Remove(block);
            loadedEntities.Remove(block);
            Client.syncObjects.Remove(block.SyncID);

            // Set null the block to all the tiles it occupied
            int size = block.Type.size;
            Vector2Int position = block.GetGridPosition();

            for (int x = 0; x < size; x++) {
                for (int y = 0; y < size; y++) {
                    Vector2Int sizePosition = position + new Vector2Int(x, y);

                    // Remove the position and tile link of the given block
                    blockPositions.Remove(sizePosition);
                    tilemap.SetBlock(sizePosition, null);
                }
            }

            OnBlockRemoved?.Invoke(this, block);
        }

        #endregion

        #region - Units -

        public void AddUnit(Unit unit) {
            units.Add(unit);
            loadedEntities.Add(unit);
            OnUnitCreated?.Invoke(this, unit);
        }

        public void RemoveUnit(Unit unit) {
            units.Remove(unit);
            loadedEntities.Remove(unit);
            OnUnitRemoved?.Invoke(this, unit);
        }

        #endregion
    }

}