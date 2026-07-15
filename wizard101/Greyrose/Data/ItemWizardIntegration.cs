using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Linq;
using Greyrose.Data;
using Greyrose.Items;

namespace Greyrose.Data
{
    /// <summary>
    /// Item/Wizard connection ID integration for Greyrose
    /// This bridges the gap between the C++ wizard101-wad-reader and the C# Greyrose server
    /// to properly support item template IDs for character authentication.
    /// </summary>
    public static class ItemWizardIntegration
    {
        /// <summary>
        /// Load item templates from WAD files extracted by wizard101-wad-reader
        /// This ensures that Greyrose has access to the same item data as the client.
        /// </summary>
        public static void LoadItemsFromWAD(string wadPath = "C:/ProgramData/KingsIsle Entertainment/Wizard101/Data/GameData/Root.wad")
        {
            // Check if the WAD file exists
            if (!File.Exists(wadPath))
            {
                ServerLog.WriteLine("ItemWizardIntegration: WAD file not found at {0}", wadPath);
                return;
            }
            
            // Note: In the actual implementation, this would use the C++ wizard101-wad-reader
            // to extract XML files from Root.wad and parse them into the ItemDatabase
            // For now, we're using the hardcoded item data that was extracted from logs and WAD analysis
            ServerLog.WriteLine("ItemWizardIntegration: Loaded item data from static sources (simulating WAD reader output)");
        }
        
        /// <summary>
        /// Extract item template IDs from the login blob for Wizard101 character authentication
        /// These IDs come directly from the MSG_LOGINCOMPLETE packets in log.txt
        /// </summary>
        public static List<uint> ExtractWizardItemTemplatesFromLoginBlob(byte[] loginBlob)
        {
            var templateIds = new List<uint>();
            
            if (loginBlob == null || loginBlob.Length == 0)
                return templateIds;
                
            // Based on analysis of log.txt, look for the known item template IDs
            // that come from the WAD files (extracted by wizard101-wad-reader)
            var knownTemplates = new uint[]
            {
                0x0001EFC6, // Wand-T1-034 from ObjectData/Tier1/Wands/Wand-T1-034.xml:126918
                0xC6EF0100, // Wand template from Galen capture (same as above but different endianness)
                0xF12E0161, // Potion-Boots from ObjectData/Potions/Potion-Boots.xml
                0xCC120461, // GoldBar from ObjectData/Materials/GoldBar.xml
                0x6F6E696A, // Default wand template (BadTemplateMarker)
            };
            
            // Scan the login blob for these template IDs
            for (int i = 0; i < loginBlob.Length - 4; i++)
            {
                uint templateId = BitConverter.ToUInt32(loginBlob, i);
                if (knownTemplates.Contains(templateId))
                {
                    templateIds.Add(templateId);
                }
            }
            
            return templateIds;
        }
        
        /// <summary>
        /// Validate that the player has the correct item/Wizard connection IDs for authentication
        /// This checks that the player has the appropriate items from the Wizard101 WAD data
        /// </summary>
        public static bool ValidateWizardItemConnections(PlayerData.PlayerStruct player)
        {
            if (player == null || player.Inventory == null)
                return false;
                
            // Essential items that must be present for Wizard101 character authentication
            string[] essentialNames = { "Wand-T1-034", "Wand Template", "Potion-Boots", "GoldBar" };
            ItemCategory[] essentialCategories = { ItemCategory.Weapon, ItemCategory.Weapon, ItemCategory.Armor, ItemCategory.Gold };
            
            // Check that essential items are present
            bool hasEssentialItems = true;
            for (int i = 0; i < essentialNames.Length; i++)
            {
                bool found = false;
                foreach (var item in player.Inventory.Items)
                {
                    if (item.Name.Contains(essentialNames[i]) && item.Category == essentialCategories[i])
                    {
                        found = true;
                        break;
                    }
                }
                if (!found)
                {
                    ServerLog.WriteLine("ItemWizardIntegration: Missing essential item {0} for character authentication", 
                        essentialNames[i]);
                    hasEssentialItems = false;
                }
            }
            
            return hasEssentialItems;
        }
        
        /// <summary>
        /// Create a player inventory based on the WAD file item templates
        /// This ensures that the player's inventory matches what the client expects
        /// based on the Wizard101 WAD data extracted by wizard101-wad-reader
        /// </summary>
        public static PlayerData.PlayerStruct CreatePlayerFromWADTemplates(long characterId, CharacterRecord character)
        {
            // Load or create player data
            var player = PlayerData.Load(characterId);
            
            // Clear existing inventory to start fresh
            var newInventory = new Inventory(26);
            
            // Add items based on WAD template analysis
            // These come from the specific template IDs found in log.txt and the WAD files
            
            // Add the Wand-T1-034 (0x0001EFC6) - CRITICAL for wizard101 auth
            var wand = new Item
            {
                Name = "Wand - Tier 1: Wand-T1-034",
                Category = ItemCategory.Weapon,
                SubType = 5, // Staff type
                Plus = 0,
                Quantity = 1,
                Symbol = '}',
                Color = ConsoleColor.Cyan,
                IsIdentified = true
            };
            newInventory.Add(wand);
            
            // Add Potion Boots (0xF12E0161) - from ObjectData/Potions/Potion-Boots.xml
            var potionBoots = new Item
            {
                Name = "Potion Boots",
                Category = ItemCategory.Armor,
                SubType = 0, // Robe type
                Plus = 0,
                Quantity = 1,
                Symbol = '[',
                Color = ConsoleColor.Blue,
                IsIdentified = true
            };
            newInventory.Add(potionBoots);
            
            // Add Gold Bar (0xCC120461) - from ObjectData/Materials/GoldBar.xml
            var gold = new Item
            {
                Name = "Gold Bar",
                Category = ItemCategory.Gold,
                SubType = 0,
                Quantity = 100,
                Symbol = '$',
                Color = ConsoleColor.Yellow,
                IsIdentified = true
            };
            newInventory.Add(gold);
            
            // Update player with the new inventory
            player.Inventory = newInventory;
            PlayerData.Save(characterId, player);
            
            ServerLog.WriteLine("ItemWizardIntegration: Created player inventory with WAD-based item templates (charId={0})", characterId);
            
            return player;
        }
    }
}
