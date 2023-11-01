using System;
using System.Linq;
using UnityEngine;
using System.Collections.Generic;

namespace Frontiers.Content {
    public class Item : Element {
        public float hardness = 0, cost = 1;
        public bool lowPriority = false, buildable = true;

        public Item(string name) : base(name) { }

        public Item(string name, (Element, float)[] composition) : base(name, composition) { }

        public ItemStack ReturnStack(float mult = 1f) {
            return new ItemStack(this, Mathf.RoundToInt(returnAmount * mult));
        }
    }

    public class Items {
        public static Item
            coal, cocaine, copper, depletedThorium, gold, graphite, 
            heavyAlloy, iron, lightAlloy, lithium, magnesium, nickel, 
            plastanium, quartz, reflectiveFabric, resistor, salt, sand, 
            silicon, superconductor, thoriumDust, thoriumFuel, thorium;

        public static void Load() {
            coal = new("coal-item") {
                hardness = 0,
                cost = 1f,
            };

            cocaine = new("cocaine-item") {
                hardness = 0,
                cost = 1f,
            };

            copper = new("copper-item") {
                hardness = 1,
                cost = 1.5f,
            };

            depletedThorium = new("depletedThorium-item") {
                hardness = 3,
                cost = 3f,
            };

            gold = new("gold-item") {
                hardness = 3,
                cost = 4f,
            };

            graphite = new("graphite-item") {
                hardness = 2,
                cost = 2f,
            };

            heavyAlloy = new("heavyAlloy-item") {
                hardness = 4,
                cost = 2.5f,
            };

            iron = new("iron-item") {
                hardness = 2,
                cost = 2f,
            };

            lightAlloy = new("lightAlloy-item") {
                hardness = 3,
                cost = 1.75f,
            };

            lithium = new("lithium-item") {
                hardness = 2,
                cost = 2f,
            };

            magnesium = new("magnesium-item") {
                hardness = 2,
                cost = 2f,
            };

            nickel = new("nickel-item") {
                hardness = 1,
                cost = 2f,
            };

            plastanium = new("plastanium-item") {
                hardness = 3,
                cost = 3f,
            };

            quartz = new("quartz-item") {
                hardness = 4,
                cost = 4f,
            };

            reflectiveFabric = new("reflectiveFabric-item") {
                hardness = 3,
                cost = 4f,
            };

            resistor = new("resistor-item") {
                hardness = 3,
                cost = 3f,
            };

            salt = new("salt-item") {
                hardness = 0,
                cost = 1f,
            };

            sand = new("sand-item") {
                hardness = 0,
                cost = 1f,
            };

            silicon = new("silicon-item") {
                hardness = 1,
                cost = 1.5f,
            };

            superconductor = new("superconductor-item") {
                hardness = 3,
                cost = 4f,
            };

            thoriumDust = new("thoriumDust-item") {
                hardness = 1,
                cost = 2f,
            };

            thoriumFuel = new("thoriumFuel-item") {
                hardness = 3,
                cost = 3f,
            };

            thorium = new("thorium-item") {
                hardness = 3,
                cost = 2.5f,
            };
        }
    }

    /// <summary>
    /// Stores the amount of a defined item
    /// </summary>
    public class ItemStack {
        public Item item;

        public string itemName;
        public int amount;

        public ItemStack() {
            item = Items.copper;
        }

        public ItemStack(Item item, int amount = 0) {
            if (item == null) item = Items.copper;
            this.item = item;
            this.amount = amount;

            itemName = item.name;
        }

        public ItemStack Set(Item item, int amount) {
            this.item = item;
            this.amount = amount;
            return this;
        }

        public ItemStack Copy() {
            return new ItemStack(item, amount);
        }

        public bool Equals(ItemStack other) {
            return other != null && other.item == item && other.amount == amount;
        }

        public static Item[] ToItems(ItemStack[] stacks) {
            if (stacks == null) return null;

            Item[] items = new Item[stacks.Length];
            for (int i = 0; i < stacks.Length; i++) items[i] = stacks[i].item;
            return items;
        }

        public static ItemStack Multiply(ItemStack stack, float amount) {
            if (stack == null) return null;
            return new ItemStack(stack.item, Mathf.RoundToInt(stack.amount * amount));
        }

        public static ItemStack[] Multiply(ItemStack[] stacks, float amount) {
            if (stacks == null) return null;

            ItemStack[] copy = new ItemStack[stacks.Length];
            for (int i = 0; i < copy.Length; i++) copy[i] = new ItemStack(stacks[i].item, Mathf.RoundToInt(stacks[i].amount * amount));
            return copy;
        }

        public static ItemStack[] With(params object[] items) {
            ItemStack[] stacks = new ItemStack[items.Length / 2];
            for (int i = 0; i < items.Length; i += 2) {
                stacks[i / 2] = new ItemStack((Item)items[i], (int)items[i + 1]);
            }
            return stacks;
        }

        public static int[] Serialize(ItemStack[] stacks) {
            int[] serializedArray = new int[stacks.Length * 2];
            for (int i = 0; i < serializedArray.Length; i += 2) {
                serializedArray[i] = stacks[i].item.id;
                serializedArray[i + 1] = stacks[i].amount;
            }
            return serializedArray;
        }

        public static ItemStack[] DeSerialize(int[] serializedArray) {
            ItemStack[] stacks = new ItemStack[serializedArray.Length / 2];
            for (int i = 0; i < serializedArray.Length; i += 2) {
                stacks[i / 2] = new ItemStack((Item)ContentLoader.GetContentById((short)serializedArray[i]), (int)serializedArray[i + 1]);
            }
            return stacks;
        }

        public static List<ItemStack> List(params object[] items) {
            List<ItemStack> stacks = new(items.Length / 2);
            for (int i = 0; i < items.Length; i += 2) {
                stacks.Add(new ItemStack((Item)items[i], (int)items[i + 1]));
            }
            return stacks;
        }
    }

    public class Inventory {
        public event EventHandler OnAmountChanged;

        public Dictionary<Item, int> items;
        public Item[] allowedItems;

        public int amountCap;
        public float maxMass;

        public bool singleItem;

        public Inventory(int amountCap = -1, float maxMass = -1f, Item[] allowedItems = null) {
            items = new Dictionary<Item, int>();
            this.amountCap = amountCap;
            this.maxMass = maxMass;
            this.allowedItems = allowedItems;
            singleItem = false;
        }

        public Inventory(bool singleItem, int amountCap = -1, float maxMass = -1f) {
            items = new Dictionary<Item, int>();
            this.amountCap = amountCap;
            this.maxMass = maxMass;
            this.singleItem = singleItem;
        }

        public void Clear() {
            items = new Dictionary<Item, int>();
        }

        public ItemStack[] ToArray() {
            ItemStack[] stacks = new ItemStack[items.Count];
            int i = 0;

            foreach (KeyValuePair<Item, int> valuePair in items) {
                stacks[i] = new(valuePair.Key, valuePair.Value);
                i++;
            }

            return stacks;
        }

        public Item[] ToItems() {
            return items.Keys.ToArray();
        }

        public void SetAllowedItems(Item[] allowedItems) {
            this.allowedItems = allowedItems;
        }

        public Item First() {
            foreach (Item item in items.Keys) if (items[item] != 0) return item;
            return null;
        }

        public Item First(Item[] filter) {
            foreach (Item item in items.Keys) if (filter.Contains(item) && items[item] != 0) return item;
            return null;
        }

        public bool Empty() {
            foreach (int amount in items.Values) if (amount != 0) return false;
            return true;
        }

        public bool Empty(Item[] filter) {
            foreach (Item item in items.Keys) if (filter.Contains(item) && items[item] != 0) return false;
            return true;
        }

        public bool Contains(Item item) {
            return items.ContainsKey(item);
        }

        public bool Has(Item item, int amount) {
            if (!Contains(item)) return false;
            else return items[item] >= amount;
        }

        public bool Has(ItemStack stack) {
            return Has(stack.item, stack.amount);
        }

        public bool Has(ItemStack[] stacks) {
            foreach (ItemStack itemStack in stacks) if (!Has(itemStack)) return false;
            return true;
        }

        public bool HasToMax(Item item, int amount) {
            if (!Contains(item)) return false;
            else return items[item] >= amount || items[item] == amountCap;
        }

        public bool HasToMax(ItemStack stack) {
            return HasToMax(stack.item, stack.amount);
        }

        public bool HasToMax(ItemStack[] stacks) {
            foreach (ItemStack itemStack in stacks) if (!HasToMax(itemStack)) return false;
            return true;
        }

        public bool Allowed(Item item) {
            return (allowedItems == null || allowedItems.Contains(item)) && AllowedSingleItem(item);
        }

        private bool AllowedSingleItem(Item item) {
            return !singleItem || items.Count == 0 || items.ElementAt(0).Key == item;
        }

        public bool Allowed(Item[] items) {
            foreach (Item item in items) if (!Allowed(item)) return false;
            return true;
        }

        public bool Empty(Item item) {
            return Contains(item) && items[item] == 0;
        }

        public bool Full(Item item) {
            return amountCap != -1f && Has(item, amountCap);
        }

        public bool FullOfAny(Item[] items) {
            foreach (Item item in items) if (Full(item)) return true;
            return false;
        }

        public bool FullOfAll(Item[] items) {
            foreach (Item item in items) if (!Full(item)) return false;
            return true;
        }

        public int AmountToFull(Item item) {
            if (!Contains(item)) return amountCap;
            return amountCap - items[item];
        }

        public int Add(Item item, int amount, bool update = true) {
            if (Full(item) || amount == 0 || !Allowed(item)) return amount;
            if (!Contains(item)) items.Add(item, 0);

            int amountToReturn = amountCap == -1 ? 0 : Mathf.Clamp(items[item] + amount - amountCap, 0, amount);
            items[item] += amount - amountToReturn;

            if (update) AmountChanged();
            return amountToReturn;
        }

        public int Add(ItemStack stack, bool update = true) {
            int value = Add(stack.item, stack.amount, false);
            if (update) AmountChanged();
            return value;
        }

        public void Add(ItemStack[] stacks) {
            foreach (ItemStack itemStack in stacks) Add(itemStack, false);
            AmountChanged();
        }

        public int Max(Item item) {
            if (!Contains(item)) {
                if (!Allowed(item)) return 0;
                else return amountCap;
            }
            return amountCap - items[item];
        }

        public int[] Max(Item[] items) {
            int[] maxItems = new int[items.Length];
            for (int i = 0; i < items.Length; i++) maxItems[i] = Max(items[i]);
            return maxItems;
        }

        public bool Fits(Item item, int amount) {
            return amount <= Max(item) && Allowed(item);
        }

        public bool Fits(ItemStack stack) {
            return Fits(stack.item, stack.amount);
        }

        public bool Fits(ItemStack[] stacks) {
            if (stacks == null) return true;
            foreach (ItemStack stack in stacks) if (!Fits(stack)) return false;
            return true;
        }

        public int Substract(Item item, int amount, bool update = true) {
            if (!Contains(item) || Empty(item) || amount == 0 || !Allowed(item)) return amount;

            int amountToReturn = Mathf.Clamp(amount - items[item], 0, amount);
            items[item] -= amount - amountToReturn;

            if (items[item] == 0) items.Remove(item);

            if (update) AmountChanged();
            return amountToReturn;
        }

        public int Substract(ItemStack itemStack, bool update = true) {
            int value = Substract(itemStack.item, itemStack.amount, false);
            if (update) AmountChanged();
            return value;
        }

        public void Substract(ItemStack[] itemStacks) {
            foreach (ItemStack itemStack in itemStacks) Substract(itemStack, false);
            AmountChanged();
        }

        public int MaxTransferAmount(Inventory other, Item item) {
            if (!Contains(item) || !other.Allowed(item)) return 0;
            return Mathf.Min(items[item], other.Max(item));
        }

        public int MaxTransferAmount(Inventory other, ItemStack stack) {
            if (!Contains(stack.item) || !other.Allowed(stack.item)) return 0;
            return Mathf.Min(stack.amount, items[stack.item], other.Max(stack.item));
        }

        public int MaxTransferSubstractAmount(Inventory other, ItemStack stack) {
            if (!other.Contains(stack.item) || !Contains(stack.item)) return 0;
            return Mathf.Min(stack.amount, items[stack.item], other.items[stack.item]);
        }

        public void TransferAll(Inventory other) {
            ItemStack[] stacksToSend = ToArray();

            for (int i = 0; i < stacksToSend.Length; i++) {
                ItemStack stack = stacksToSend[i];
                stack.amount = MaxTransferAmount(other, stack.item);
            }

            Transfer(other, stacksToSend);
        }

        public void TransferAll(Inventory other, Item[] filter) {
            ItemStack[] stacksToSend = new ItemStack[filter.Length];

            for (int i = 0; i < filter.Length; i++) {
                Item item = filter[i];
                int amountToSend = MaxTransferAmount(other, item);
                stacksToSend[i] = new ItemStack(item, amountToSend);
            }

            Transfer(other, stacksToSend);
        }

        public void TransferAmount(Inventory other, ItemStack[] stacks) {
            ItemStack[] stacksToSend = new ItemStack[stacks.Length];

            for (int i = 0; i < stacks.Length; i++) {
                ItemStack stack = stacks[i];
                int amountToSend = MaxTransferAmount(other, stack);
                stacksToSend[i] = new ItemStack(stack.item, amountToSend);
            }

            Transfer(other, stacksToSend);
        }

        public void TransferSubstractAmount(Inventory other, ItemStack[] stacks) {
            ItemStack[] stacksToSend = new ItemStack[stacks.Length];

            for (int i = 0; i < stacks.Length; i++) {
                ItemStack stack = stacks[i];
                int amountToSend = MaxTransferSubstractAmount(other, stack);
                stacksToSend[i] = new ItemStack(stack.item, amountToSend);
            }

            TransferSubstract(other, stacksToSend);
        }

        public void Transfer(Inventory other, ItemStack[] stacks) {
            Substract(stacks);
            other.Add(stacks);
        }

        public void TransferSubstract(Inventory other, ItemStack[] stacks) {
            Substract(stacks);
            other.Substract(stacks);
        }

        public void AmountChanged() {
            OnAmountChanged?.Invoke(this, EventArgs.Empty);
        }
    }
}