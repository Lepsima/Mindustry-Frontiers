using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Menu : MonoBehaviour {

    [HideInInspector] public bool isOpen;

    private void Awake() {
        isOpen = gameObject.activeSelf;
    }

    public void Open() {
        isOpen = true;
        gameObject.SetActive(true);
    }

    public void Close() {
        isOpen = false;
        gameObject.SetActive(false);
    }
}