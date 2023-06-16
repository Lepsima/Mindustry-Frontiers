using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlayerUnitController : MonoBehaviour {
    [SerializeField] float moveSpeed = 15f;
    [SerializeField] float zoomSpeed = 30f;
    [SerializeField] float zoomInMultiplier = 2f;
    [SerializeField] float zoomOutMultiplier = 1f;
    [SerializeField] [Range(1, 50)] float zoomClampMax = 40f;

    public event EventHandler<EventArgs> OnPlayerSpawned;
    public event EventHandler<EventArgs> OnPlayerKilled;

    public bool isAlive = false;

    private void Update() {
        transform.position += new Vector3(Input.GetAxis("Horizontal") * moveSpeed, Input.GetAxis("Vertical") * moveSpeed, 0) * Time.deltaTime;

        float delta = Input.mouseScrollDelta.y;
        float change = delta * zoomSpeed * (delta < 0f ? zoomOutMultiplier : zoomInMultiplier) * Time.deltaTime;
        Camera.main.orthographicSize = Mathf.Clamp(Camera.main.orthographicSize - change, 1, zoomClampMax);
    }
}