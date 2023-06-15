using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Tilemaps;
using Frontiers.Content.Maps;
using Frontiers.Content;

public class MapSaver : MonoBehaviour {
    public string saveName;
    public Map map;

    private void Start() {
        Invoke(nameof(SaveMap), 5f);
    }

    public void SaveMap() {
        // Here starts the point where i have to guess how the fuck to replace the unity tilemaps with custom ones
        //map = new Map(saveName, tilemaps[1].size.x, tilemaps[1].size.y, tilemaps);
        map.Save();
        MapLoader.SaveMap(map);
    }
}