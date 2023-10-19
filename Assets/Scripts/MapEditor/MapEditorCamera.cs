using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class MapEditorCamera : MonoBehaviour {

    public float velocity, zoomVelocity;
    public float minZoom, maxZoom;

    public bool allowMove = false;

    void Update() {
        if (!allowMove) return;
        transform.position += Time.deltaTime * Camera.main.orthographicSize * new Vector3(Input.GetAxis("Horizontal") * velocity, Input.GetAxis("Vertical") * velocity, 0);
        Camera.main.orthographicSize = Mathf.Clamp(Camera.main.orthographicSize + Input.mouseScrollDelta.y * zoomVelocity * Time.deltaTime, minZoom, maxZoom);
    }

    public void SetPosition(Vector2 pos) {
        transform.position = pos;
    }

    public void AllowMove(bool value) {
        allowMove = value;
    }
}