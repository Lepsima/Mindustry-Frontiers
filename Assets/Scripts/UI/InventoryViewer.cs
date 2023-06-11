using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;
using Frontiers.Content;
using Frontiers.Assets;
using System.Linq;

public class InventoryViewer : MonoBehaviour {
    private ItemViewer[] itemViewers;
    public Inventory inventory;

    public static InventoryViewer Instance;

    private void Start() {
        int loadedItems = ContentLoader.GetContentCountOfType<Item>();
        InstantiateItemViewers(loadedItems);

        Instance = this;
        SetInventory(null);
    }

    private void InstantiateItemViewers(int amount) {
        itemViewers = new ItemViewer[amount];
        GameObject itemViewerPrefab = AssetLoader.GetPrefab("ItemViewerPrefab");

        for (int i = 0; i < amount; i++) {
            itemViewers[i] = Instantiate(itemViewerPrefab, transform).GetComponent<ItemViewer>();
        }
    }

    public void SetInventory(Inventory inventory) {
        // Remove previous event calls
        if (this.inventory != null) this.inventory.OnAmountChanged -= OnInventoryValueChanged;
        this.inventory = inventory;

        // Set the new inventory and add new event calls
        if (inventory != null) this.inventory.OnAmountChanged += OnInventoryValueChanged;
        OnInventoryValueChanged(this, System.EventArgs.Empty);
    }

    public void OnInventoryValueChanged(object sender, System.EventArgs e) {
        if (inventory == null) {
            foreach (ItemViewer itemViewer in itemViewers) itemViewer.gameObject.SetActive(false);
            return;
        }

        for (int i = 0; i < itemViewers.Length; i++) {
            itemViewers[i].gameObject.SetActive(i < inventory.items.Count);
            if (i < inventory.items.Count) itemViewers[i].UpdateItem(inventory.items.ElementAt(i).Key, inventory.items.ElementAt(i).Value);
        }
    }


}
