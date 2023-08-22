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
                        tilemap[x, y] = new Tile(new Vector2Int(x, y));
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

            public CollisionNode(Vector2Int position) {
                this.position = position;
            }

            public CollisionNode(CollisionNode next, Vector2Int position) {
                this.next = next;
                this.position = position;
            }
        }

        public void GenerateColliders() {
            List<Vector2[]> outlines = GetSolidOutlines();
            List<EdgeCollider2D> edgeColliders = new();

            foreach(Vector2[] outline in outlines) {
                EdgeCollider2D edgeCollider = new GameObject("Outline", typeof(EdgeCollider2D)).GetComponent<EdgeCollider2D>();
                edgeCollider.SetPoints(outline.ToList());
            }
        }

        private List<Vector2[]> GetSolidOutlines() {
            List<Tile> solidTiles = GetAllSolidTiles();
            Vector2Int[] offsetPoints = new Vector2Int[8] { new(0, 1), new(1, 0), new(-1, 0), new(0, -1), new(1, 1), new(-1, -1), new(-1, 1), new(1, -1) };

            List<Vector2[]> outlines = new();

            while (solidTiles.Count > 0) {
                Tile tile = solidTiles[0];
                solidTiles.Remove(tile);

                List<CollisionNode> nodes = new();

                New(tile.position);
                New(tile.position + new Vector2Int(0, 1));
                New(tile.position + new Vector2Int(1, 1));
                New(tile.position + new Vector2Int(1, 0));

                nodes[0].next = nodes[3];
                nodes[1].next = nodes[0];
                nodes[2].next = nodes[1];
                nodes[3].next = nodes[2];

                //CollisionNode Get(Vector2Int position) => nodeList.ContainsKey(position) ? nodeList[position] : new(position);

                CollisionNode New(Vector2Int position, CollisionNode next = null) {
                    CollisionNode newNode = new(next, position);
                    nodes.Add(newNode);
                    return newNode;
                }

                CalculateTile(tile);

                //The current node list should be the outline of a group of blocks
                Vector2[] outlinePoints = new Vector2[nodes.Count];
                int i = 0;
                AddToPointList(nodes[0]);

                void AddToPointList(CollisionNode node) {
                    outlinePoints[i] = node.position;
                    i++;
                    if (i < outlinePoints.Length) AddToPointList(node.next);
                }

                void CalculateTile(Tile cTile) {
                    if (!solidTiles.Contains(cTile) || !cTile.IsWall()) return;
                    solidTiles.Remove(cTile);

                    List<Tile> neighbourTiles = new();

                    for (int i = 0; i < 8; i++) {
                        Vector2Int neighbourPos = cTile.position + offsetPoints[i];
                        if (neighbourPos.x < 0 || neighbourPos.y < 0 || neighbourPos.x >= size.x || neighbourPos.y >= size.y) continue;

                        Tile neighbourTile = GetTile(neighbourPos);
                        if (!neighbourTile.IsWall()) continue;
                        neighbourTiles.Add(neighbourTile);
                        Debug.Log("CHK");

                        switch (i) {
                            case 0:
                                nodes[1].next = New(cTile.position + new Vector2Int(0, 2), New(cTile.position + new Vector2Int(1, 2), nodes[2]));
                                break;

                            case 1:
                                nodes[2].next = New(cTile.position + new Vector2Int(2, 1), New(cTile.position + new Vector2Int(2, 0), nodes[3]));
                                break;

                            case 2:
                                nodes[0].next = New(cTile.position + new Vector2Int(-1, 0), New(cTile.position + new Vector2Int(-1, 1), nodes[1]));
                                break;

                            case 3:
                                nodes[3].next = New(cTile.position + new Vector2Int(0, -1), New(cTile.position + new Vector2Int(1, -1), nodes[0]));
                                break;

                            case 4:
                                nodes[2].position += new Vector2Int(0, 1);
                                nodes[1].next.next = nodes[2];
                                nodes[2].next = nodes[2].next.next;
                                break;

                            case 5:
                                nodes[0].position += new Vector2Int(0, -1);
                                nodes[3].next.next = nodes[0];
                                nodes[0].next = nodes[0].next.next;
                                break;

                            case 6:
                                nodes[1].position += new Vector2Int(-1, 0);
                                nodes[0].next.next = nodes[1];
                                nodes[1].next = nodes[1].next.next;
                                break;

                            case 7:
                                nodes[3].position += new Vector2Int(1, 0);
                                nodes[2].next.next = nodes[3];
                                nodes[3].next = nodes[3].next.next;
                                break;

                            default:
                                break;
                        }
                    }

                    foreach(Tile neighbourTile in neighbourTiles) {
                        CalculateTile(neighbourTile);
                    }
                }

                outlines.Add(outlinePoints);
            }

            return outlines;
        }

        public void HoldMeshUpdate(bool state) {
            // Used when updating the whole map is needed, to not call update 5000000 times, only once finished
            for (int x = 0; x < regions.GetLength(0); x++) {
                for (int y = 0; y < regions.GetLength(1); y++) {
                    regions[x, y].HoldMeshUpdate(state);
                }
            }
        }

        public List<Tile> GetAllSolidTiles() {
            List<Tile> solidTiles = new();

            for (int x = 0; x < size.x; x++) {
                for (int y = 0; y < size.y; y++) {
                    Tile tile = GetTile(new(x, y));
                    if (tile.IsWall()) solidTiles.Add(tile);
                }
            }

            return solidTiles;
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