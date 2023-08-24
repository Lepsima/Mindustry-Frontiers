using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor.Search;
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
            return !tiles[(int)MapLayer.Ground].isWater;
        }

        public void LoadTile(string data) {
            // Loads all layers of the tile from a single string
            // Used to recive map data across the network 
            for (int i = 0; i < data.Length; i++) {
                int id = Convert.ToInt32(data[i]) - 32;
                if (id == 0) continue;
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
        public Region[,] regions;
        public Vector2Int regionSize;
        public Vector2Int regionCount;
        public Vector2Int size;

        public struct Region {
            public Tile[,] tilemap;
            public Vector2Int size;

            public Vector2Int offset;
            public RegionDisplayer displayer;

            public bool holdMeshUpdate;
            public bool wasChanged;

            public Region(Vector2Int size, Vector2Int offset) {
                // Store given parameters
                this.size = size;
                this.offset = offset;
                displayer = null;
                holdMeshUpdate = false;
                wasChanged = false;

                // Create a new tile array
                tilemap = new Tile[size.x, size.y];

                // Create all the tiles
                for (int x = 0; x < size.x; x++) {
                    for (int y = 0; y < size.y; y++) {
                        tilemap[x, y] = new Tile(new Vector2Int(x, y) + offset);
                    }
                }

                displayer = new RegionDisplayer(this);
            }

            public Tile GetTile(Vector2Int position) {
                // Get a local tile from a world position
                Vector2Int localPosition = position - offset;
                return tilemap[localPosition.x, localPosition.y];
            }

            public void SetTile(TileType tileType, Vector2Int position, MapLayer layer) {
                // Sets a tile type of a tile
                GetTile(position).Set(tileType, layer);

                if (holdMeshUpdate) wasChanged = true;
                else displayer.Update(this);
            }

            public void SetTile(string data, Vector2Int position) {
                // Load a tile from the given string data
                GetTile(position).LoadTile(data);
            }

            public void HoldMeshUpdate(bool state) {
                holdMeshUpdate = state;

                if (state) {
                    wasChanged = false;
                } else {
                    if (wasChanged) displayer.Update(this);
                    wasChanged = false;
                }
            }

            public int GetRenderedTileCount() {
                // Get the total tile count that should be renderer;
                int counter = 0;

                for (int x = 0; x < size.x; x++) {
                    for (int y = 0; y < size.y; y++) {
                        Tile tile = tilemap[x, y];

                        if (tile.Layer(MapLayer.Solid) != null) counter++;
                        else {
                            if (tile.Layer(MapLayer.Ground) != null) counter++;
                            if (tile.Layer(MapLayer.Ore) != null) counter++;
                        }
                    }
                }

                return counter;
            }
        }

        public Tilemap(Vector2Int size, Vector2Int regionSize) {
            // Store given parameters
            this.size = size;
            this.regionSize = regionSize;

            // Create the region array
            regionCount = new(size.x / regionSize.x, size.y / regionSize.y);
            regions = new Region[regionCount.x, regionCount.y];


            // Create all the region structs
            for (int x = 0; x < regionCount.x; x++) {
                for (int y = 0; y < regionCount.y; y++) {
                    regions[x, y] = new Region(regionSize, new Vector2Int(x, y) * regionSize);
                }
            }
        }

        class CollisionNode {
            public CollisionNode next;
            public Vector2Int position;
            public int depth;

            public CollisionNode(Vector2Int position, CollisionNode next = null) {
                this.position = position;
                Next(next);
            }

            public void Next(CollisionNode next) {
                this.next = next;
                depth = next == null ? 0 : next.depth + 1;
            }
        }

        public bool InBounds(Vector2 position) {
            return position.x >= 0 && position.x < size.x && position.y >= 0 && position.y < size.y;
        }

        public void GenerateColliders() {
            List<Vector2[]> outlines = GetWallOutlines();
            List<EdgeCollider2D> edgeColliders = new();

            foreach(Vector2[] outline in outlines) {
                EdgeCollider2D edgeCollider = new GameObject("Outline", typeof(EdgeCollider2D)).GetComponent<EdgeCollider2D>();
                edgeCollider.SetPoints(outline.ToList());
            }
        }

        private List<Vector2[]> GetWallOutlines() {
            List<Tile> tiles = GetAllSolidPositions();
            Vector2Int[] nearOffsets = new Vector2Int[8] { new(0, 1), new(1, 0), new(0, -1), new(-1, 0), new(1, 1), new(-1, -1), new(-1, 1), new(1, -1) };

            Debug.Log(tiles.Count);

            // First iteration to discard blocked tiles
            for (int i = tiles.Count - 1; i >= 0; i--) {
                Tile tile = tiles[i];

                // Check if is surrounded by other tiles
                bool isBlocked = true;

                for (int j = 0; j < 4; j++) {
                    // Add an offset to the current tile position
                    Vector2Int pos = tile.position + nearOffsets[j];

                    // If is out of bounds or blocked by another wall, shouldnt need collider
                    if (InBounds(pos) && !GetTile(pos).IsWall()) {
                        isBlocked = false;
                        break;
                    }
                }

                // If is blocked, remove from list
                if (isBlocked) tiles.Remove(tile);                  
            }
            Debug.Log(tiles.Count);

            List<CollisionNode> allNodes = new();

            foreach(Tile tile in tiles) {
                allNodes.AddRange(GetTileNodes(tile));
            }

            List<CollisionNode> toRemoveList = new();

            foreach(CollisionNode node in allNodes) {
                CollisionNode next = node.next;

                if (next != null && next.next == null) {
                    node.Next(allNodes.Find(x => x.position == node.next.position && x != next));
                    toRemoveList.Add(next);
                }
            }

            foreach(CollisionNode node in toRemoveList) {
                allNodes.Remove(node);
            }

            allNodes = allNodes.OrderBy(x => x.depth).ToList();
            Debug.Log(allNodes[0].depth);

            // A list of all the outline paths
            List<Vector2[]> paths = new();

            while (allNodes.Count > 0) {
                CollisionNode node = allNodes[0];
                allNodes.Remove(node);

                List<Vector2> outline = new() { node.position };
                CollisionNode next = node.next;

                while (true) {
                    if (next == null || !allNodes.Contains(next)) break;

                    outline.Add(next.position);
                    allNodes.Remove(next);

                    next = next.next;
                }

                if (outline.Count <= 1) continue;
                paths.Add(outline.ToArray());
            }

            return paths;

            /*

            while (tiles.Count > 0) {
                // Init tile list
                Tile tile = tiles[^1];
                List<Tile> path = new() { tile };

                // Search for a path
                Search(path, tile, null, -1);

                // Convert path to an array of positions
                Vector2[] pathPositions = new Vector2[path.Count];

                // Loop thru path and add offset to each position to be in the middle of the tile
                for (int i = 0; i < path.Count; i++) pathPositions[i] = path[i].position;

                // Add to path list
                paths.Add(pathPositions);

                Debug.Log(pathPositions.Length);
            }

            // Return found paths
            return paths;

            void Search(List<Tile> path, Tile tile, Tile last, int lastDir) {
                if (path.Count >= 1000)
                    return;

                Tile[] nearTiles = new Tile[8];

                for (int i = 0; i < 8; i++) {
                    // Add an offset to the current tile position
                    Vector2Int pos = tile.position + nearOffsets[i];

                    // Add to near tiles
                    Tile nearTile = TryFind(pos);
                    nearTiles[i] = nearTile;

                    // If there is no tile, is going back, is already on the path or is used, skip
                    if (nearTile == null || last == nearTile || path.Contains(nearTile))
                        continue;

                    // If line is going in the same direction, remove last
                    //if (lastDir == i && path.Count > 0) path.Remove(last);

                    // Add tile to the path and remove from main list
                    path.Add(nearTile);
                    tiles.Remove(nearTile);

                    // Continue search
                    Search(path, nearTile, tile, i);
                    return;
                }


                // If there is no last tile, this is a single block path
                if (last == null)
                    return;

                // If couldn't find any tiles, try to close loop or go backwards
                for (int i = 0; i < 8; i++) {
                    Tile nearTile = nearTiles[i];

                    // If is invalid, skip
                    if (nearTile == last || nearTile == null)
                        continue;

                    // If near is the start, close loop
                    if (nearTile == path[0]) {
                        path.Add(nearTile);
                        break;
                    }

                    // Add tile to path, thus going backwards
                    path.Add(nearTile);
                    Search(path, nearTile, tile, i);
                    return;
                }
            }
            */

            bool MissingWallAt(Vector2Int pos) {
                return InBounds(pos) && !GetTile(pos).IsWall();
            }
            

            List<CollisionNode> GetTileNodes(Tile tile) {
                bool
                    nearA = MissingWallAt(tile.position + new Vector2Int(0, 1)),
                    nearB = MissingWallAt(tile.position + new Vector2Int(1, 0)),
                    nearC = MissingWallAt(tile.position + new Vector2Int(0, -1)),
                    nearD = MissingWallAt(tile.position + new Vector2Int(-1, 0));

                List<CollisionNode> nodes = new();

                if (nearD || nearA) { 
                    nodes.Add(new CollisionNode(tile.position + new Vector2Int(0, 1))); 
                }

                if (nearA || nearB) {
                    CollisionNode prev = nearA ? nodes[^1] : null;
                    nodes.Add(new CollisionNode(tile.position + new Vector2Int(1, 1), prev));
                }

                if (nearB || nearC) {
                    CollisionNode prev = nearB ? nodes[^1] : null;
                    nodes.Add(new CollisionNode(tile.position + new Vector2Int(1, 0), prev));
                }

                if (nearC || nearD) {
                    CollisionNode prev = nearC ? nodes[^1] : null;
                    nodes.Add(new CollisionNode(tile.position, prev));
                }

                if (nearD && nearA) nodes[0].Next(nodes[^1]);

                return nodes;
            }
        }

        public void HoldMeshUpdate(bool state) {
            // Used when updating the whole map is needed, to not call update 5000000 times, only once finished
            for (int x = 0; x < regions.GetLength(0); x++) {
                for (int y = 0; y < regions.GetLength(1); y++) {
                    regions[x, y].HoldMeshUpdate(state);
                }
            }
        }

        public List<Tile> GetAllSolidPositions() {
            List<Tile> solidPoses = new();

            for (int x = 0; x < size.x; x++) {
                for (int y = 0; y < size.y; y++) {
                    Tile tile = GetTile(new(x, y));
                    if (tile.IsWall()) solidPoses.Add(tile);
                }
            }

            return solidPoses;
        }

        public Tile GetTile(Vector2Int position) {
            // Get a tile from a region
            int x = position.x / regionSize.x;
            int y = position.y / regionSize.y;
            return regions[x, y].GetTile(position);
        }

        public TileType GetTile(Vector2Int position, MapLayer layer) {
            // Get a tile type from a region
            return GetTile(position).Layer(layer);
        }

        public void SetTile(TileType tileType, Vector2Int position, MapLayer layer) {
            // Set a region's tile
            regions[position.x / regionSize.x, position.y / regionSize.y].SetTile(tileType, position, layer);
        }

        public void SetBlock(Vector2Int position, Block block) {
            // Set a block on a tile
            GetTile(position).Set(block);
        }

        public void SetTile(Vector2Int position, string data) {
            // Load a tile from the given string data
            GetTile(position).LoadTile(data);
        }
    }
}