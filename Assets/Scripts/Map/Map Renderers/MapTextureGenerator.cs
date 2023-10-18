using CI.QuickSave;
using Frontiers.Assets;
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using static Frontiers.Content.Maps.Map;
using static Frontiers.Content.Maps.Tilemap;
using Random = UnityEngine.Random;

namespace Frontiers.Content.Maps {
    public static class MapMeshGenerator {
        public static TileType[] allTiles;

        public static Mesh GenerateMesh(Region region) {
            // Initialize the needed variables
            int size = region.tilemap.regionSize;
            MeshData meshData = new(region.GetRenderedTileCount());

            for (int x = 0; x < size; x++) {
                for (int y = 0; y < size; y++) {
                    // Get the tile UVs corresponding to the region coords
                    Tile tile = region.GetTile(new Vector2Int(x, y));
                    if (tile == null) continue;

                    // Get solid tile
                    TileType tileType = tile.Layer(MapLayer.Solid);

                    // If is not solid, try to render the ground and ore layers
                    if (tileType == null) {
                        tileType = tile.Layer(MapLayer.Ground);
                        if (tileType != null) AddTileToMesh(x, y, meshData, tileType);

                        tileType = tile.Layer(MapLayer.Ore);
                        if (tileType != null) AddTileToMesh(x, y, meshData, tileType);
                    } else {
                        AddTileToMesh(x, y, meshData, tileType);
                    }
                }
            }

            return meshData.CreateMesh();
        }

        public static void AddTileToMesh(int x, int y, MeshData meshData, TileType tileType) {
            if (tileType == null) return;
            Vector4 UVs = tileType.GetSpriteVariantUV(Random.Range(0, tileType.variants));

            // Get the atlas UVs
            Vector2 uv00 = new(UVs.x, UVs.y);
            Vector2 uv11 = new(UVs.z, UVs.w);

            meshData.AddQuad(x, y, uv00, uv11);
        }

        public class MeshData {
            readonly Vector3[] vertices;
            readonly Vector2[] uvs;
            readonly int[] triangles;

            int index = 0;

            public MeshData(int tiles) {
                vertices = new Vector3[tiles * 4];
                uvs = new Vector2[tiles * 4];
                triangles = new int[tiles * 6];
            }

            public void AddQuad(int x, int y, Vector2 uv00, Vector2 uv11) {
                // Some funky shit that is needed to create a quad
                triangles[index * 6 + 0] = index * 4 + 0;
                triangles[index * 6 + 1] = index * 4 + 1;
                triangles[index * 6 + 2] = index * 4 + 2;

                triangles[index * 6 + 3] = index * 4 + 0;
                triangles[index * 6 + 4] = index * 4 + 2;
                triangles[index * 6 + 5] = index * 4 + 3;

                uvs[index * 4 + 0] = new Vector2(uv00.x, uv00.y);
                uvs[index * 4 + 1] = new Vector2(uv00.x, uv11.y);
                uvs[index * 4 + 2] = new Vector2(uv11.x, uv11.y);
                uvs[index * 4 + 3] = new Vector2(uv11.x, uv00.y);

                vertices[index * 4 + 0] = new Vector2(x, y);
                vertices[index * 4 + 1] = new Vector2(x, y + 1);
                vertices[index * 4 + 2] = new Vector2(x + 1, y + 1);
                vertices[index * 4 + 3] = new Vector2(x + 1, y);

                index++;
            }

            public Mesh CreateMesh() {
                // Create an actual mesh from the given data 
                Mesh mesh = new();
                mesh.vertices = vertices;
                mesh.uv = uvs;
                mesh.triangles = triangles;
                mesh.RecalculateNormals();
                return mesh;
            }
        }
    }

    public static class MapTextureGenerator {
        public static Texture2D GenerateTileTextureAtlas() {
            // Get all tiles loaded by the content loader
            TileType[] tiles = TileLoader.GetLoadedTiles();
            List<SpriteLink> links = new();

            // Foreach sprite and variant in each tile, add them to a list and link them to their parent tile
            for (int i = 0; i < tiles.Length; i++) {
                TileType tileType = tiles[i];

                for (int v = 0; v < tileType.variants; v++) {
                    links.Add(new SpriteLink(tileType, v));
                }
            }

            // All sprites in the array should be squares with the same size
            int spriteSize = links[0].sprite.texture.width;

            // Get the maximum side length the atlas can go to with the given sprites
            int atlasSize = Mathf.CeilToInt(Mathf.Sqrt(links.Count));

            // Create the texture with the given size
            Texture2D atlasTexture = new(atlasSize * spriteSize, atlasSize * spriteSize);
            int index = 0;

            for (int x = 0; x < atlasSize; x++) {
                for (int y = 0; y < atlasSize; y++) {

                    // If there are no more sprites, add a transparent gap
                    // This happens because the atlas is an exact square, so the shape often cant fit perfectly all the sprites
                    if (index >= links.Count) {
                        for (int px = 0; px < spriteSize; px++) {
                            for (int py = 0; py < spriteSize; py++) {

                                // Get the pixel coord and set it to transparent
                                Vector2Int atlasPixelPosition = new(x * spriteSize + px, y * spriteSize + py);
                                atlasTexture.SetPixel(atlasPixelPosition.x, atlasPixelPosition.y, new Color(0, 0, 0, 0));
                            }
                        }

                        continue;
                    }

                    // Get the current sprite that will be added to the atlas
                    SpriteLink link = links[index];
                    Texture2D spriteTexture = link.sprite.texture;

                    // Add each pixel from the sprite to the atlas
                    for (int px = 0; px < spriteSize; px++) {
                        for (int py = 0; py < spriteSize; py++) {

                            // Get the pixel position in the sprite and the one relative to the atlas
                            Vector2Int spritePixelPosition = new(px, py);
                            Vector2Int atlasPixelPosition = new(x * spriteSize + px, y * spriteSize + py);

                            // Apply the sprite pixel on the atlas
                            Color pixelColor = spriteTexture.GetPixel(spritePixelPosition.x, spritePixelPosition.y);
                            atlasTexture.SetPixel(atlasPixelPosition.x, atlasPixelPosition.y, pixelColor);
                        }
                    }

                    // Get the UVs of the sprite on the atlas (reminder for me: these are the sprite positions on the canvas)
                    Vector2 uv00 = new Vector2(x, y) / atlasSize;
                    Vector2 uv11 = new Vector2(x + 1, y + 1) / atlasSize;

                    // Assign the UVs to the tile type
                    TileType tileType = link.tileType;
                    tileType.SetSpriteUV(link.variant, uv00, uv11);

                    index++;
                }
            }

            // Some settings that need to be aplied to the texture2d for this case
            atlasTexture.filterMode = FilterMode.Point;
            atlasTexture.wrapMode = TextureWrapMode.Clamp;
            atlasTexture.Apply();

            return atlasTexture;
        }

        private class SpriteLink {
            public Sprite sprite;
            public TileType tileType;
            public int variant;

            // Links a sprite to a tile type, only used for the creation of the atlas
            public SpriteLink(TileType tileType, int variant) {
                sprite = tileType.allVariantSprites[variant];
                this.tileType = tileType;
                this.variant = variant;
            }
        }
    }
}