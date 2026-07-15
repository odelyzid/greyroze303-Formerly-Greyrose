using System;
using Greyrose.Items;

namespace Greyrose.Items
{
    public class Item
    {
        public string Name;
        public ItemCategory Category;
        public int SubType;
        public int Plus;
        public int Quantity;
        public char Symbol;
        public ConsoleColor Color;
        public bool IsIdentified;

        public int AttackBonus
        {
            get
            {
                if (Category == ItemCategory.Weapon)
                    return (SubType + 1) * 2 + Plus;
                return 0;
            }
        }

        public int DefenseBonus
        {
            get
            {
                if (Category == ItemCategory.Armor)
                    return (SubType + 1) * 2 + Plus;
                if (Category == ItemCategory.Shield)
                    return (SubType + 1) + Plus;
                return 0;
            }
        }

        public Item()
        {
            IsIdentified = true;
            Quantity = 1;
            Symbol = ',';
            Color = ConsoleColor.Gray;
        }

        public Item Clone()
        {
            return new Item
            {
                Name = Name,
                Category = Category,
                SubType = SubType,
                Plus = Plus,
                Quantity = Quantity,
                Symbol = Symbol,
                Color = Color,
                IsIdentified = IsIdentified
            };
        }
    }
}
