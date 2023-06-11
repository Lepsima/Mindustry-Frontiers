using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using Frontiers.Content;

public class UnitUI : MonoBehaviour {
    [SerializeField] private TextMeshProUGUI itemText;
    [SerializeField] private Image itemImage;
    [SerializeField] private Slider healthSlider, fuelSlider, ammoSlider;

    private void LateUpdate() {
        transform.rotation = Quaternion.identity;
    }

    public void ShowUI(bool value) {
        gameObject.SetActive(value);
    }

    public void ShowUI() {
        gameObject.SetActive(!gameObject.activeSelf);
    }

    public void UpdateItem(ItemStack itemStack) {
        itemText.text = itemStack.amount.ToString();
        itemImage.sprite = itemStack.item.sprite;
    }

    public void UpdateSliders(float health, float fuel, float ammo) {
        healthSlider.value = health;
        fuelSlider.value = fuel;
        ammoSlider.value = ammo;
    }

    public void UpdateUI(ItemStack itemStack, float health, float fuel, float ammo) {
        UpdateItem(itemStack);
        UpdateSliders(health, fuel, ammo);
    }
}
