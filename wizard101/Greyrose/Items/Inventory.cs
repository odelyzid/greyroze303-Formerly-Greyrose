using System;
using System.Collections.Generic;

namespace Greyrose.Items
{
    public class Inventory
    {
        public List<Item> Items;
        public int Capacity;

        public Inventory(int capacity = 26)
        {
            Items = new List<Item>(capacity);
            Capacity = capacity;
        }

        public bool IsFull => Items.Count >= Capacity;

        public bool Add(Item item)
        {
            if (item.Category == ItemCategory.Gold)
            {
                Item gold = Items.Find(i => i.Category == ItemCategory.Gold);
                if (gold != null)
                {
                    gold.Quantity += item.Quantity;
                    return true;
                }
            }

            if (IsFull)
                return false;

            Items.Add(item);
            return true;
        }

        public void Remove(Item item)
        {
            Items.Remove(item);
        }

        public Item Get(int index)
        {
            if (index < 0 || index >= Items.Count)
                return null;
            return Items[index];
        }
    }
}
