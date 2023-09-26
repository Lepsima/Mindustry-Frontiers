using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using Frontiers.Content.Maps;

public class MapEditorMenuController : MonoBehaviour {
    public MapEditor mapEditor;

    [Header("Map parameter inputs")]

    public TMP_InputField mapLoadNameInputField;
    public TMP_InputField mapSaveNameInputField;
    public TMP_InputField mapSizeInputField;

    [Header("Replace parameter inputs")]

    // Tile type inputs
    public TMP_InputField replaceTile1InputField;
    public TMP_InputField replaceTile2InputField;

    [Header("Noise parameter inputs")]

    // Tile type inputs
    public TMP_InputField noiseTile1InputField;
    public TMP_InputField noiseTile2InputField;

    // Base noise inputs
    public TMP_InputField scaleInputField;
    public Slider thresholdSlider;
    public Slider octaveSlider;
    public TMP_InputField persistanceInputField;
    public TMP_InputField lacunarityInputField;

    // River inputs
    public TMP_InputField riverWidthInputField;
    public TMP_InputField shoreWidthInputField;


    public void StartEditor() {
        bool loadMap = !string.IsNullOrEmpty(mapLoadNameInputField.text);

        if (loadMap) {
            mapEditor.LoadMap(mapLoadNameInputField.text);
        } else {
            // Convert input text to vector
            string[] vectorComponents = mapSizeInputField.text.Split("/");
            Vector2Int size = new(int.Parse(vectorComponents[0]), int.Parse(vectorComponents[1]));

            // Create map
            mapEditor.CreateMap(mapSaveNameInputField.text, size);
        }
    }

    public void ConfirmNoise() {
        TileType tile1 = TileLoader.GetTileTypeByName(noiseTile1InputField.text);
        TileType tile2 = TileLoader.GetTileTypeByName(noiseTile2InputField.text);

        bool isRiver = !string.IsNullOrEmpty(riverWidthInputField.text);

        mapEditor.ApplyNoise(
            tile1, 
            tile2, 
            float.Parse(scaleInputField.text), 
            thresholdSlider.value, 
            (int)octaveSlider.value, 
            float.Parse(persistanceInputField.text),
            float.Parse(lacunarityInputField.text),
            isRiver ? float.Parse(riverWidthInputField.text) : -1f,
            isRiver ? float.Parse(shoreWidthInputField.text) : -1f
            );
    }

    public void ConfirmReplace() {
        TileType tile1 = TileLoader.GetTileTypeByName(replaceTile1InputField.text);
        TileType tile2 = TileLoader.GetTileTypeByName(replaceTile2InputField.text);

        mapEditor.ApplyReplace(tile1, tile2);
    }
}
