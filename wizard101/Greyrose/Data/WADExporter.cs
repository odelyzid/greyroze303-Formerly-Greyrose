using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using Greyrose.Items;

namespace Greyrose.Data
{
    /// <summary>
    /// Creates a CSV representation of WAD file item templates for use in item/Wizard connection IDs.
    /// </summary>
    public static class WADExporter
    {
        public static void ExportItems(string outputPath = "items.csv")
        {
            var exportItems = new List<ItemExport>();
            
            // Add all hardcoded items
            exportItems.Add(new ItemExport(0x6F6E696A, "default_template", ItemCategory.Weapon, "Default Weapon", "Tier1/Basic Weapon", 1));
            exportItems.Add(new ItemExport(0x0001EFC6, "Wand-T1-034", ItemCategory.Weapon, "Wand - Tier 1: Wand-T1-034", "ObjectData/Tier1/Wands/Wand-T1-034.xml:126918", 1));
            exportItems.Add(new ItemExport(0xF12E0161, "Potion-Boots", ItemCategory.Armor, "Potion Boots", "ObjectData/Potions/Potion-Boots.xml", 1));
            exportItems.Add(new ItemExport(0xCC120461, "GoldBar", ItemCategory.Gold, "Gold Bar", "ObjectData/Materials/GoldBar.xml", 1));
            
            // Write to CSV
            using (var writer = new StreamWriter(outputPath, false, Encoding.UTF8))
            {
                writer.WriteLine("TemplateID,TemplateName,Category,DisplayName,SourcePath,LevelRequired");
                foreach (var item in exportItems)
                {
                    writer.WriteLine("{0},{1},{2},{3},{4},{5}",
                        item.TemplateId.ToString("X8"),
                        item.TemplateName,
                        item.Category.ToString(),
                        item.DisplayName.Replace('"', ' '),
                        item.SourcePath.Replace('"', ' '),
                        item.LevelRequired);
                }
            }
            
            Console.WriteLine("Exported {0} items to {1}", exportItems.Count, outputPath);
        }
    }

    public class ItemExport
    {
        public uint TemplateId { get; set; }
        public string TemplateName { get; set; }
        public ItemCategory Category { get; set; }
        public string DisplayName { get; set; }
        public string SourcePath { get; set; }
        public int LevelRequired { get; set; }
        
        public ItemExport(uint templateId, string templateName, ItemCategory category, 
                         string displayName, string sourcePath, int levelRequired)
        {
            TemplateId = templateId;
            TemplateName = templateName;
            Category = category;
            DisplayName = displayName;
            SourcePath = sourcePath;
            LevelRequired = levelRequired;
        }
    }
}
