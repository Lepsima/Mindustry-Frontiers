using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class MapEditorMenuController : MonoBehaviour {
    public TMP_InputField mapLoadName;
    public TMP_InputField mapSaveName;
    public TMP_InputField mapSize;

    [Space]

    public Dropdown n_mainTile;
    public Dropdown n_secTile;
    public Slider n_scaleSlider;
    public Slider n_thresholdSlider;
    public Slider n_octaveSlider;
    public Slider n_persistanceSlider;
    public Slider lacunaritySlider;

    [Space]

    public Dropdown r_mainTile;
    public Dropdown r_secTile;

    public void StartEditor() {
        bool loadMap = !string.IsNullOrEmpty(mapLoadName.text);

        if (loadMap) {

        } else {

        }
    }

    public void ConfirmNoise() {

    }

    public void ConfirmReplace() {

    }
}
