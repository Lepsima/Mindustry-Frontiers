using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlayerUnitController : MonoBehaviour {
    [SerializeField] float moveSpeed;
    [SerializeField] float zoomSpeed;

    private void Update() {
        transform.position += new Vector3(Input.GetAxis("Horizontal") * moveSpeed, Input.GetAxis("Vertical") * moveSpeed, 0) * Time.deltaTime;
    }
}