using UnityEngine;
using System;
using Frontiers.Assets;

namespace Frontiers.Content.Maps {
    [Serializable]
    public struct MapData {
        public Vector2Int size;
        public TilemapData tilemapData;

        public MapData(Vector2Int size, TilemapData tileData) {
            this.size = size;
            this.tilemapData = tileData;
        }

        public MapData(Map map) {
            size = map.size;
            tilemapData = new TilemapData(map.SaveTilemapData());
        }

        public MapData(Vector2Int size, string[] tileData) {
            this.size = size;
            this.tilemapData = new TilemapData(size, tileData);
        }

        /*
        public BlockArrayData blockData;
        public UnitArrayData unitData;

        public MapData(TileMapData tileData, BlockArrayData blockData, UnitArrayData unitData) {
            this.tileData = tileData;
            this.blockData = blockData;
            this.unitData = unitData;
        }*/

        [Serializable]
        public struct TilemapData {
            public Wrapper2D<string> tileReferenceGrid;

            public TilemapData(string[,] tileReferenceGrid) {
                this.tileReferenceGrid = new Wrapper2D<string>(tileReferenceGrid);
            }

            public TilemapData(string[,,] tileNameGrid) {
                tileReferenceGrid = new Wrapper2D<string>(Encode(tileNameGrid));
            }

            public TilemapData(Vector2Int size, string[] tileData) {
                tileReferenceGrid = new Wrapper2D<string>(ReAssemble(size, tileData));
            }

            public string[,,] DecodeThis() => Decode(tileReferenceGrid.array2D);

            public static string[,] Encode(string[,,] tileNameGrid) {
                Vector3Int size = new(tileNameGrid.GetLength(0), tileNameGrid.GetLength(1), tileNameGrid.GetLength(2));
                string[,] returnGrid = new string[size.x, size.y];

                for (int x = 0; x < size.x; x++) {
                    for (int y = 0; y < size.y; y++) {
                        for (int z = 0; z < size.z; z++) {
                            returnGrid[x, y] += TileLoader.GetTileTypeByName(tileNameGrid[x, y, z]).ToChar();
                        }
                    }
                }

                return returnGrid;
            }

            public static string[,] ReAssemble(Vector2Int size, string[] tileData) {
                int layers = (int)Map.MapLayer.Total;
                string[,] returnGrid = new string[size.x, size.y];
                int i = 0;

                for (int z = 0; z < layers; z++) {
                    for (int x = 0; x < size.x; x++) {
                        for (int y = 0; y < size.y; y++) {

                            int stringIndex = Mathf.FloorToInt(i / MapLoader.TilesPerString);
                            int tile = i - (stringIndex * MapLoader.TilesPerString);

                            returnGrid[x, y] += tileData[stringIndex][tile];
                            i++;
                        }
                    }
                }

                return returnGrid;
            }

            public static string[,,] Decode(string[,] tileReferenceGrid) {
                Vector2Int size = new(tileReferenceGrid.GetLength(0), tileReferenceGrid.GetLength(1));

                int layers = (int)Map.MapLayer.Total;
                string[,,] returnGrid = new string[size.x, size.y, layers];

                for (int x = 0; x < size.x; x++) {
                    for (int y = 0; y < size.y; y++) {
                        string tileData = tileReferenceGrid[x, y];

                        for (int z = 0; z < layers; z++) {
                            returnGrid[x, y, z] = tileData[z].ToType()?.name;
                        }
                    }
                }

                return returnGrid;
            }
        }
        /*
        [Serializable]
        public struct BlockArrayData {
            public BlockData[] array;

            public BlockArrayData(BlockData[] array) {
                this.array = array;
            }

            [Serializable]
            public struct BlockData {
                public Vector2Int position;
            }
        }

        [Serializable]
        public struct UnitArrayData {
            public UnitData[] array;

            public UnitArrayData(UnitData[] array) {
                this.array = array;
            }

            [Serializable]
            public struct UnitData {
                public Vector2 position;
            }
        }
        */
    }
}