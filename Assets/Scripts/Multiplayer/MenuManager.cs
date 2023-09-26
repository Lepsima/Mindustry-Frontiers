using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class MenuManager : MonoBehaviour {

    public static MenuManager Instance;

    [SerializeField] Menu[] menus;

    [Space] 

    [SerializeField] bool hasAnimations = false;
    [SerializeField] Animator backgroundAnimator;

    private void Awake() {
        Instance = this;
    }

    public void OpenMenu(string menuName) {
        foreach (Menu menu in menus) if (menu.name == menuName) OpenMenu(menu);       
    }

    public void OpenMenu(Menu menu) {
        for (int i = 0; i < menus.Length; i++) { 
            if (menus[i].isOpen) CloseMenu(menus[i]); 
            if (menus[i] == menu && hasAnimations) backgroundAnimator.SetInteger("state", i);    
        }
        menu.Open();
    }

    public void CloseMenu(Menu menu) {
        menu.Close();
    }
}