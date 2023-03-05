using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;
using UnityEngine.UI;
using Frontiers.Content;

public class ItemDrop : MonoBehaviour {
    private TextMeshProUGUI itemAmountText;
    private SpriteRenderer itemSpriteRenderer;

    public TextMeshProUGUI AmountText {
        get {
            if (!itemAmountText) AmountText = GetComponentInChildren<TextMeshProUGUI>(true);
            return itemAmountText;
        }

        set => itemAmountText = value;
    }
    public SpriteRenderer Sprite {
        get {
            if (!itemSpriteRenderer) Sprite = GetComponentInChildren<SpriteRenderer>(true);
            return itemSpriteRenderer;
        }

        set => itemSpriteRenderer = value;
    }


    public void UpdateItemStack(ItemStack newItemStack) {
        gameObject.SetActive(newItemStack.amount != 0);
        if (newItemStack.IsEmpty()) return;

        AmountText.text = newItemStack.amount + "";
        Sprite.sprite = newItemStack.item.sprite;
    }
}
