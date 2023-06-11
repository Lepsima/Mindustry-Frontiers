using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Tilemaps;
using Frontiers.Content.Maps;
using Frontiers.Content;

public class MapSaver : MonoBehaviour {
    public string saveName;
    public Tilemap[] tilemaps;
    public Map map;

    private void Start() {
        Invoke(nameof(SaveMap), 5f);
    }

    public void SaveMap() {
        map = new Map(saveName, tilemaps[1].size.x, tilemaps[1].size.y, tilemaps);
        map.Save();
        MapLoader.SaveMap(map);
    }
}