using CI.QuickSave;
using Frontiers.Assets;
using System;
using System.IO;
using System.Linq;
using UnityEngine;

namespace Frontiers.Content.Maps {
    public class MapLoader {
        public const int TilesPerString = 1000;
        public static string[] mapNames;

        public static event EventHandler<MapLoadedEventArgs> OnMapLoaded;
        public class MapLoadedEventArgs {
            public Map loadedMap;
        }

        public static void GenerateDefaultMaps() {
            MapFile[] maps = GetMaps();

            if (Contains("Default Map 02")) {

            }
            
            bool Contains(string name) {
                foreach(MapFile map in maps) if (map.name == name) return true;
                return false;
            }
        }

        public static MapFile[] GetMaps() {
            DirectoryInfo dir = new(Directories.maps);
            FileInfo[] info = dir.GetFiles("*.json");
            MapFile[] maps = new MapFile[info.Length];

            for (int i = 0; i < info.Length; i++) {
                string name = Path.GetFileNameWithoutExtension(info[i].Name);
                maps[i] = new MapFile(name, info[i].FullName);
            }

            return maps;
        }

        public static void RefreshMapNames() {
            string[] mapDirectories = Directory.GetDirectories(Directories.maps);
            for (int i = 0; i < mapDirectories.Length; i++) mapNames[i] = Path.GetDirectoryName(mapDirectories[i]);
        }

        public static void ReciveMap(string name, Vector2 size, string[] tileData) {
            MapData mapData = CreateMap(Vector2Int.CeilToInt(size), tileData);
            Map map = new(name, mapData);
            OnMapLoaded?.Invoke(null, new MapLoadedEventArgs() { loadedMap = map });
        }

        public static void LoadMap(string name) {
            MapData mapData = ReadMap(name);
            Map map = new(name, mapData);
            OnMapLoaded?.Invoke(null, new MapLoadedEventArgs() { loadedMap = map });
        }

        public static void LoadMap(MapFile mapFile) {
            MapData mapData = mapFile.Read();
            Map map = new(mapFile.name, mapData);
            OnMapLoaded?.Invoke(null, new MapLoadedEventArgs() { loadedMap = map });
        }

        public static void SaveMap(Map map) {
            StoreMap(map.name, map.GetMapData());
        }

        public static void StoreMap(string name, MapData mapData) {
            string mapName = Path.Combine("Maps", name);
            QuickSaveRaw.Delete(mapName + ".json");
            QuickSaveWriter writer = QuickSaveWriter.Create(mapName);

            writer.Write("data", mapData);
            writer.Commit();
        }

        public static MapData ReadMap(string name) {
            name = Path.Combine("Maps", name);
            QuickSaveReader reader = QuickSaveReader.Create(name);
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
            QuickSaveReader reader = QuickSaveReader.Create(quickSavePath);
            return reader.Read<MapData>("data");
        }
    }
}