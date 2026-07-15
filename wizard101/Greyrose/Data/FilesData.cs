using System.Collections.Generic;
using Greyrose.Items;

namespace Greyrose.Data
{
    // Load test items for item/Wizard connection ID support
    public static class FilesData
    {
        // Test for item/Wizard connection IDs from the browser screenshot
        // The user showed 0x0001efc6 (Wand - Tier 1: Wand-T1-034) and 0xF12E0161 (Potion Boots)
        public static Dictionary<uint, ItemInfo> ItemDatabase = new Dictionary<uint, ItemInfo>
        {
            // Wand from User screenshot
            [0x0001EFC6] = new ItemInfo
            {
                TemplateId = 0x0001EFC6,
                TemplateName = "Wand-T1-034",
                DisplayName = "Wand - Tier 1: Wand-T1-034",
                Category = ItemCategory.Weapon,
                SubType = 5, // Staff
                LevelRequired = 1,
                Description = "Tier 1 Wand from ObjectData/Tier1/Wands/Wand-T1-034.xml:126918",
                IconPath = "ObjectData/Tier1/Wands/Wand-T1-034.png"
            },
            
            // Wand template from earlier code analysis
            [0x6F6E696A] = new ItemInfo
            {
                TemplateId = 0x6F6E696A,
                TemplateName = "default_template",
                DisplayName = "Default Wand Template",
                Category = ItemCategory.Weapon,
                SubType = 0, // Dagger
                LevelRequired = 1,
                Description = "Default character wand template (0x6F6E696A)",
                IconPath = "ObjectData/Template/default_wand.png"
            },
            
            // Potion Boots from screenshot
            [0xF12E0161] = new ItemInfo
            {
                TemplateId = 0xF12E0161,
                TemplateName = "Potion-Boots",
                DisplayName = "Potion Boots",
                Category = ItemCategory.Armor,
                SubType = 0, // Robe
                LevelRequired = 2,
                Description = "Potion Boots from ObjectData/Potions/Potion-Boots.xml",
                IconPath = "ObjectData/Potions/Potion-Boots.png"
            },
            
            // Gold from screenshot
            [0xCC120461] = new ItemInfo
            {
                TemplateId = 0xCC120461,
                TemplateName = "GoldBar",
                DisplayName = "Gold Bar",
                Category = ItemCategory.Gold,
                SubType = 0,
                LevelRequired = 1,
                Description = "Gold Bar (stackable) from ObjectData/Materials/GoldBar.xml",
                IconPath = "ObjectData/Materials/GoldBar.png"
            },
            
            // Wand template for Galen capture (different template, break before inventory)
            [0xC6EF0100] = new ItemInfo
            {
                TemplateId = 0xC6EF0100,
                TemplateName = "Wand-T1-034-C6EF",
                DisplayName = "Wand - Tier 1: T1-034 (Galen Capture)",
                Category = ItemCategory.Weapon,
                SubType = 5, // Staff
                LevelRequired = 1,
                Description = "Wand template captured in zone login blob (different from default)",
                IconPath = "ObjectData/Tier1/Wands/Wand-T1-034.png"
            }
        };
        
        // Helper method to lookup items by template ID
        public static ItemInfo FindItem(uint templateId)
        {
            if (ItemDatabase.TryGetValue(templateId, out var item))
                return item;
            return null;
        }
        
        // Helper method to get all items by category
        public static List<ItemInfo> GetItemsByCategory(ItemCategory category)
        {
            var result = new List<ItemInfo>();
            foreach (var item in ItemDatabase.Values)
                if (item.Category == category)
                    result.Add(item);
            return result;
        }
        
        // Helper method to get all items (for export)
        public static List<ItemInfo> GetAllItems()
        {
            return new List<ItemInfo>(ItemDatabase.Values);
        }
    }

    // Item information structure for item/Wizard connection IDs
    public class ItemInfo
    {
        public uint TemplateId { get; set; }
        public string TemplateName { get; set; }
        public string DisplayName { get; set; }
        public ItemCategory Category { get; set; }
        public int SubType { get; set; }
        public int LevelRequired { get; set; }
        public string Description { get; set; }
        public string IconPath { get; set; }
    }
}
