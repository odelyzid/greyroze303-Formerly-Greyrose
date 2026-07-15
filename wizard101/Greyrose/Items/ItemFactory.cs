using System;
using Greyrose.Items;

namespace Greyrose.Items
{
    public static class ItemFactory
    {
        public static Item CreateWeapon(WeaponType type, int plus)
        {
            string[] names = { "dagger", "sword", "mace", "axe", "bow", "staff" };
            char[] symbols = { ')', '/', '\\', '|', '}', '(' };
            Item item = new Item();
            item.Category = ItemCategory.Weapon;
            item.SubType = (int)type;
            item.Plus = plus;
            item.Name = names[(int)type];
            if (plus > 0)
                item.Name = item.Name + " +" + plus;
            item.Symbol = symbols[(int)type];
            item.Color = ConsoleColor.Cyan;
            return item;
        }

        public static Item CreateArmor(ArmorType type, int plus)
        {
            string[] names = { "robe", "leather armour", "chain mail", "plate mail" };
            Item item = new Item();
            item.Category = ItemCategory.Armor;
            item.SubType = (int)type;
            item.Plus = plus;
            item.Name = names[(int)type];
            if (plus > 0)
                item.Name = item.Name + " +" + plus;
            item.Symbol = '[';
            item.Color = ConsoleColor.Blue;
            return item;
        }

        public static Item CreateShield(ShieldType type, int plus)
        {
            string[] names = { "buckler", "kite shield", "tower shield" };
            Item item = new Item();
            item.Category = ItemCategory.Shield;
            item.SubType = (int)type;
            item.Plus = plus;
            item.Name = names[(int)type];
            if (plus > 0)
                item.Name = item.Name + " +" + plus;
            item.Symbol = ']';
            item.Color = ConsoleColor.DarkCyan;
            return item;
        }

        public static Item CreatePotion(PotionType type, int quantity)
        {
            string[] names = { "potion of healing", "potion of mana", "potion of poison", "potion of strength", "potion of speed" };
            Item item = new Item();
            item.Category = ItemCategory.Potion;
            item.SubType = (int)type;
            item.Quantity = quantity;
            item.Name = names[(int)type];
            item.Symbol = '!';
            item.Color = ConsoleColor.Magenta;
            return item;
        }

        public static Item CreateScroll(ScrollType type)
        {
            string[] names = { "scroll of identify", "scroll of teleport", "scroll of mapping", "scroll of fireball", "scroll of confusion" };
            Item item = new Item();
            item.Category = ItemCategory.Scroll;
            item.SubType = (int)type;
            item.Name = names[(int)type];
            item.Symbol = '?';
            item.Color = ConsoleColor.Yellow;
            return item;
        }

        public static Item CreateGold(int amount)
        {
            Item item = new Item();
            item.Category = ItemCategory.Gold;
            item.Quantity = amount;
            item.Name = amount + " gold";
            item.Symbol = '$';
            item.Color = ConsoleColor.Yellow;
            return item;
        }

        public static Item CreateFood()
        {
            Item item = new Item();
            item.Category = ItemCategory.Food;
            item.Name = "ration";
            item.Symbol = '%';
            item.Color = ConsoleColor.DarkYellow;
            return item;
        }
    }
}
