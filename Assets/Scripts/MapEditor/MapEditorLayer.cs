using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Frontiers.Content.Maps;

[CreateAssetMenu(menuName = "Map Editor/New Layer", fileName = "New Editor Layer")]

public class MapEditorLayer : ScriptableObject {

    public enum LayerAction {
        Noise,
        River,
    }
    public LayerAction layerAction;

    [Header("Noise Settings")]
    [Tooltip("Leave to -1 to auto generate each time")]public int seed = -1;

    [Space]

    [Range(0f, 999f)] public float scale = 3.25f;
    [Range(0f, 1f)] public float threshold = 0.5f;
    [Range(0, 8)] public int octaves = 4;
    [Range(0f, 1f)] public float persistance = 0.5f;
    [Range(0f, 10f)] public float lacunarity = 1f;

    [Space]

    public Map.MapLayer noiseLayer;
    public bool overrideIfNull;
    public string tile1Name;
    public string tile2Name;


    [Header("River Settings")]
    public float riverWidth;
    public float shoreWidth;
}