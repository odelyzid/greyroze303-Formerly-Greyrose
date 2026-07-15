using System;
using Greyrose.Items;

namespace Greyrose.Items
{
    public class Equipment
    {
        public Item Weapon;
        public Item Armor;
        public Item Shield;

        public int TotalAttackBonus
        {
            get
            {
                int bonus = 0;
                if (Weapon != null) bonus += Weapon.AttackBonus;
                return bonus;
            }
        }

        public int TotalDefenseBonus
        {
            get
            {
                int bonus = 0;
                if (Armor != null) bonus += Armor.DefenseBonus;
                if (Shield != null) bonus += Shield.DefenseBonus;
                return bonus;
            }
        }

        public Item Equip(Item item)
        {
            Item old = null;
            switch (item.Category)
            {
                case ItemCategory.Weapon:
                    old = Weapon;
                    Weapon = item;
                    break;
                case ItemCategory.Armor:
                    old = Armor;
                    Armor = item;
                    break;
                case ItemCategory.Shield:
                    old = Shield;
                    Shield = item;
                    break;
            }
            return old;
        }

        public Item UnequipSlot(ItemCategory cat)
        {
            Item old = null;
            switch (cat)
            {
                case ItemCategory.Weapon:
                    old = Weapon; Weapon = null; break;
                case ItemCategory.Armor:
                    old = Armor; Armor = null; break;
                case ItemCategory.Shield:
                    old = Shield; Shield = null; break;
            }
            return old;
        }

        public bool IsEquipped(Item item)
        {
            return Weapon == item || Armor == item || Shield == item;
        }
    }
}
