using System;
using Greyrose.Items;

namespace Greyrose.Items
{
    public class PlayerInfo
    {
        public Inventory Inventory;
        public Equipment Equipment;
        public int Hp;
        public int MaxHp;

        public PlayerInfo()
        {
            Inventory = new Inventory();
            Equipment = new Equipment();
            Hp = 100;
            MaxHp = 100;
        }

        public void AddItem(Item item)
        {
            if (Inventory.Add(item))
            {
                ServerLog.WriteLine(string.Format("Added {0} to inventory.", item.Name));
            }
            else
            {
                ServerLog.WriteLine("Inventory is full! Cannot add " + item.Name);
            }
        }

        public void RemoveItem(int index)
        {
            Item item = Inventory.Get(index);
            if (item != null)
            {
                Inventory.Remove(item);
                ServerLog.WriteLine(string.Format("Removed {0} from inventory.", item.Name));

                if (Equipment.IsEquipped(item))
                {
                    ServerLog.WriteLine("Item was equipped, automatically unequipped.");
                    if (item.Category == ItemCategory.Weapon)
                        Equipment.Weapon = null;
                    else if (item.Category == ItemCategory.Armor)
                        Equipment.Armor = null;
                    else if (item.Category == ItemCategory.Shield)
                        Equipment.Shield = null;
                }
            }
        }

        public void EquipItem(int index)
        {
            Item item = Inventory.Get(index);
            if (item == null || (item.Category != ItemCategory.Weapon && item.Category != ItemCategory.Armor && item.Category != ItemCategory.Shield))
                return;

            Item old = Equipment.Equip(item);
            Inventory.Remove(item);

            if (old != null)
                Inventory.Add(old);

            ServerLog.WriteLine(string.Format("Equipped {0}. Attack bonus: {1}, Defense bonus: {2}",
                item.Name, Equipment.TotalAttackBonus, Equipment.TotalDefenseBonus));
        }

        public void UseItem(int index)
        {
            Item item = Inventory.Get(index);
            if (item == null)
                return;

            switch (item.Category)
            {
                case ItemCategory.Potion:
                    ServerLog.WriteLine("Used potion: " + item.Name);
                    ServerLog.WriteLine("Restored 10 HP.");
                    World.Player.Hp = Math.Min(World.Player.MaxHp, World.Player.Hp + 10);
                    Consume(item);
                    break;
                case ItemCategory.Scroll:
                    ServerLog.WriteLine("Read scroll: " + item.Name);
                    Consume(item);
                    break;
                default:
                    ServerLog.WriteLine("Cannot use " + item.Name + " in this way.");
                    break;
            }
        }

        private void Consume(Item item)
        {
            item.Quantity--;
            if (item.Quantity <= 0)
                Inventory.Remove(item);
        }
    }
}
