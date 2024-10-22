using Frontiers.Assets;
using UnityEngine;
using static Frontiers.Content.Maps.Tilemap;

namespace Frontiers.Content.Maps {
    public static class MapDisplayer {
        public static Material atlasMaterial;
        public static Texture2D atlas;

        public static void SetupAtlas() {
            Launcher.SetState("Loading Map Atlas...");

            // Generate tile texture atlas
            Texture2D atlas = MapTextureGenerator.GenerateTileTextureAtlas();
            MapDisplayer.atlas = atlas;

            // Create atlas material
            atlasMaterial = AssetLoader.GetAsset<Material>("Atlas Material");
            atlasMaterial.mainTexture = atlas;

            Launcher.SetState("Map Atlas Loaded");
        }
    }

    public class RegionDisplayer {
        public static Transform mapParent;

        readonly MeshFilter meshFilter;
        readonly Region region;

        public RegionDisplayer(Region region) {
            // Create parent object
            if (!mapParent) mapParent = new GameObject("Map").transform;
            this.region = region;

            // Create world object
            Transform transform = new GameObject("Map Region", typeof(MeshRenderer), typeof(MeshFilter)).transform;
            transform.parent = mapParent;
            transform.position = (Vector2)region.offset;

            // Get the mesh renderer component and apply the atlas material
            MeshRenderer meshRenderer = transform.GetComponent<MeshRenderer>();
            meshRenderer.material = MapDisplayer.atlasMaterial;

            // Get the mesh filter component and apply the region mesh to it
            meshFilter = transform.GetComponent<MeshFilter>();
            meshFilter.mesh = MapMeshGenerator.GenerateMesh(region);
        }

        public void Update() {
            // Update the region's mesh
            meshFilter.mesh = MapMeshGenerator.GenerateMesh(region);
        }
    }
}