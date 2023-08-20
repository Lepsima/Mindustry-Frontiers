using Frontiers.Content.Maps;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;

public class MapFileItem : MonoBehaviour {
    public MapFile mapFile;
    public TMP_Text text;

    public void SetUp(MapFile mapFile) {
        this.mapFile = mapFile;
        text.text = mapFile.name;
    }

    public void OnClick() {
        Launcher.Instance.ChangeSelectedMap(mapFile);
    }
}
