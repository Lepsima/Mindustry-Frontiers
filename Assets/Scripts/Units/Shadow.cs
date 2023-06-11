using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Shadow : MonoBehaviour {

    float distance;

    void Update() {
        transform.position = -Vector3.one * distance + transform.parent.position;
    }

    public void SetSprite(Sprite sprite) {
        gameObject.GetComponent<SpriteRenderer>().sprite = sprite;
    }

    public void SetDistance(float distance) {
        this.distance = distance * 0.2f;
    }
}