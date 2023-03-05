using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class MenuManager : MonoBehaviour {

    public static MenuManager Instance;

    [SerializeField] Menu[] menus;

    private void Awake() {
        Instance = this;
    }

    public void OpenMenu(string menuName) {
        for (int i = 0; i < menus.Length; i++) if (menus[i].name == menuName) OpenMenu(menus[i]);
    }

    public void OpenMenu(Menu menu) {
        for (int i = 0; i < menus.Length; i++) if (menus[i].isOpen) CloseMenu(menus[i]);
        menu.Open();
    }

    public void CloseMenu(Menu menu) {
        menu.Close();
    }
}