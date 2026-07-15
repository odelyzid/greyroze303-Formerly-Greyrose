using System;

namespace Greyrose.Items
{
    public enum ItemCategory
    {
        Weapon,
        Armor,
        Shield,
        Potion,
        Scroll,
        Gold,
        Food
    }

    public enum WeaponType
    {
        Dagger,
        Sword,
        Mace,
        Axe,
        Bow,
        Staff
    }

    public enum ArmorType
    {
        Robe,
        Leather,
        Chain,
        Plate
    }

    public enum ShieldType
    {
        Buckler,
        Kite,
        Tower
    }

    public enum PotionType
    {
        Healing,
        Mana,
        Poison,
        Strength,
        Speed
    }

    public enum ScrollType
    {
        Identify,
        Teleport,
        Mapping,
        Fireball,
        Confusion
    }
}
