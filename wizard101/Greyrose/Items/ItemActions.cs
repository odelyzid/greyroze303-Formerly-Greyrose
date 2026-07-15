using System;
using Greyrose.Items;

namespace Greyrose.Items
{
    public static class ItemActions
    {
        public static bool Pickup()
        {
            var items = World.GetItemsAt(World.PlayerX, World.PlayerY);
            if (items == null || items.Count == 0)
            {
                ServerLog.WriteLine("Nothing to pick up here.");
                return false;
            }

            int picked = 0;
            for (int i = items.Count - 1; i >= 0; i--)
            {
                Item item = items[i];
                if (World.Player.Inventory.Add(item))
                {
                    items.RemoveAt(i);
                    picked++;
                    ServerLog.WriteLine(string.Format("You pick up {0}.", item.Name));
                }
                else
                {
                    ServerLog.WriteLine("Your inventory is full!");
                    break;
                }
            }

            if (items.Count == 0)
                World.FloorItems.Remove(World.PositionKey(World.PlayerX, World.PlayerY));

            return picked > 0;
        }

        public static void Drop(int inventoryIndex)
        {
            Item item = World.Player.Inventory.Get(inventoryIndex);
            if (item == null)
                return;

            if (World.Player.Equipment.IsEquipped(item))
            {
                ServerLog.WriteLine("You cannot drop an equipped item. Unequip it first.");
                return;
            }

            World.Player.Inventory.Remove(item);
            World.AddFloorItem(World.PlayerX, World.PlayerY, item);
            ServerLog.WriteLine(string.Format("You drop {0}.", item.Name));
        }

        public static void Consume(Item item)
        {
            item.Quantity--;
            if (item.Quantity <= 0)
                World.Player.Inventory.Remove(item);
        }
    }
}
