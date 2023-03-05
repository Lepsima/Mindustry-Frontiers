using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;
using Frontiers.Content;
using System.Linq;

public class InventoryViewer : MonoBehaviour {
    private ItemDrop[] itemDrops;
    public ItemList itemList;

    private void Awake() {
        itemDrops = GetComponentsInChildren<ItemDrop>(true);
        gameObject.SetActive(false);
    }

    public void SetItemList(ItemList itemList, Vector2 position) {
        if (this.itemList != null) this.itemList.OnItemListUpdated -= OnItemListUpdated;
        this.itemList = itemList;

        if (itemList != null) this.itemList.OnItemListUpdated += OnItemListUpdated;
        else gameObject.SetActive(false);

        transform.position = position;
        OnItemListUpdated(this, System.EventArgs.Empty);
    }

    public void OnItemListUpdated(object sender, System.EventArgs e) {
        gameObject.SetActive(itemList != null && !itemList.IsEmpty());

        if (itemList == null || itemList.IsEmpty()) { 
            foreach (ItemDrop itemDrop in itemDrops) itemDrop.gameObject.SetActive(false);
            return;
        }

        for (int i = 0; i < itemDrops.Length; i++) {
            itemDrops[i].gameObject.SetActive(i < itemList.itemStacks.Count);
            if (i < itemList.itemStacks.Count) itemDrops[i].UpdateItemStack(itemList.itemStacks.ElementAt(i).Value);
        }
    }
}
