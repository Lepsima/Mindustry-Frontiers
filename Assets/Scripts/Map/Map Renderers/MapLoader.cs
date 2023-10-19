using CI.QuickSave;
using Frontiers.Assets;
using System;
using System.IO;
using System.Linq;
using UnityEngine;

namespace Frontiers.Content.Maps {
    public class MapLoader {
        public const int TilesPerString = 1000;
        public static MapFile[] foundMaps;

        public static QuickSaveSettings QuickSaveSettings = new() { CompressionMode = CompressionMode.Gzip };

        public class MapLoadedEventArgs {
            public Map loadedMap;
        }

        public static void GenerateDefaultMapFiles() {
            // Get all found maps, and instantiate the default maps
            MapFile[] maps = FindMaps();
            DefaultMaps.Load();

            foreach (DefaultMaps.JsonMapData jsonMapData in DefaultMaps.maps) {
                // If map file does not exist, create file
                if (!Contains(jsonMapData.name)) QuickSaveRaw.SaveString(Path.Combine("Maps", jsonMapData.name + ".json"), jsonMapData.data);
            }

            bool Contains(string name) {
                foreach (MapFile map in maps) if (map.name == name) return true;
                return false;
            }
        }

        public static MapFile[] FindMaps() {
            DirectoryInfo dir = new(Directories.maps);
            FileInfo[] info = dir.GetFiles("*.json");
            foundMaps = new MapFile[info.Length];

            for (int i = 0; i < info.Length; i++) {
                string name = Path.GetFileNameWithoutExtension(info[i].Name);
                foundMaps[i] = new MapFile(name, info[i].FullName);
            }

            return foundMaps;
        }

        public static void ReciveMap(string name, Vector2 size, string[] tileData) {
            MapData mapData = CreateMap(Vector2Int.CeilToInt(size), tileData);
            Map map = new(name, mapData);
            OnLoadMap(map);
        }

        public static void LoadMap(string name) {
            MapData mapData = ReadMap(name);
            Map map = new(name, mapData);
            OnLoadMap(map);
        }

        public static void LoadMap(MapFile mapFile) {
            MapData mapData = mapFile.Read();
            Map map = new(mapFile.name, mapData);
            OnLoadMap(map);
        }

        public static void LoadMap(string name, Vector2Int size, byte[] tilemap) {
            Map map = new(name, size, tilemap);
            OnLoadMap(map);
        }

        public static void CreateMap(string name, Vector2Int size) {
            Map map = new(name, size.x, size.y);
            OnLoadMap(map);
        }

        public static void OnLoadMap(Map map) {
            MapManager.Map = map;

            MapManager.OnMapLoaded(map);
            Client.OnMapLoaded(map);
            /*
            int count = 100;
            long time = 0;

            for (int i = 0; i < count; i++) {
                System.Diagnostics.Stopwatch sw = new();
                sw.Start();
                map.TilemapToBytes();
                sw.Stop();
                time += sw.ElapsedMilliseconds;
            }

            Debug.Log((double)time / count / 1000f);
            */
        }

        public static void SaveMap(Map map) {
            StoreMap(map.name, map.GetMapData());
        }

        public static void StoreMap(string name, MapData mapData) {
            string mapName = Path.Combine("Maps", name);
            QuickSaveRaw.Delete(mapName + ".json");
            QuickSaveWriter writer = QuickSaveWriter.Create(mapName, QuickSaveSettings);

            writer.Write("data", mapData);
            writer.Commit();
        }

        public static MapData ReadMap(string name) {
            name = Path.Combine("Maps", name);
            QuickSaveReader reader = QuickSaveReader.Create(name, QuickSaveSettings);

            return reader.Read<MapData>("data");
        }

        public static MapData CreateMap(Vector2Int size, string[] tileData) {
            return new MapData(size, tileData);
        }
    }

    public class MapFile {
        public string name;
        public string path;
        public string quickSavePath;

        public MapFile(string name, string path) {
            this.name = name;
            this.path = path;
            quickSavePath = Path.Combine("Maps", name);
        }

        public MapData Read() {
            QuickSaveReader reader = QuickSaveReader.Create(quickSavePath, MapLoader.QuickSaveSettings);
            return reader.Read<MapData>("data");
        }
    }
}