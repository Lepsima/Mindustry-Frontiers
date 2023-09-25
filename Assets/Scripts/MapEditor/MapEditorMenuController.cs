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

    public Slider scaleSlider;
    public Slider thresholdSlider;
    public Slider octaveSlider;
    public Slider persistanceSlider;
    public Slider lacunaritySlider;

    public void StartEditor() {
        bool loadMap = !string.IsNullOrEmpty(mapLoadName.text);

        if (loadMap) {

        } else {

        }
    }
}
