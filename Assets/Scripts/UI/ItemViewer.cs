using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;
using UnityEngine.UI;
using Frontiers.Content;

public class ItemViewer : MonoBehaviour {
    private TextMeshProUGUI itemText;
    private Image itemImage;

    public TextMeshProUGUI Text {
        get {
            if (!itemText) Text = GetComponentInChildren<TextMeshProUGUI>(true);
            return itemText;
        }

        set => itemText = value;
    }
    public Image Image {
        get {
            if (!itemImage) Image = GetComponentInChildren<Image>(true);
            return itemImage;
        }

        set => itemImage = value;
    }


    public void UpdateItem(Item item, int amount) {
        gameObject.SetActive(amount != 0);
        if (amount == 0) return;

        Text.text = amount + "";
        Image.sprite = item.sprite;
    }
}
