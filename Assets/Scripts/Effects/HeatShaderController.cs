using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class HeatShaderController : MonoBehaviour {
    public Material heatMaterial;
    float lastSize;

    private void Update() {
        // If the size of the camera hasn't changed, skip update
        float size = Camera.main.orthographicSize;
        if (size == lastSize) return;

        // Update material property
        heatMaterial.SetFloat("_CameraSize", size);
        lastSize = size;
    }
}