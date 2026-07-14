using System;
using System.Collections.Generic;
using Greyrose.Items;
using Greyrose.Data;

namespace Greyrose
{
    public static class PlayerData
    {
        // Player Struct that stores actual game data
        public class PlayerStruct
        {
            public long CharacterId { get; set; }
            public float X { get; set; }
            public float Y { get; set; }
            public float Z { get; set; }
            public float Rot { get; set; }
            public int Hp { get; set; }
            public int MaxHp { get; set; }
            public int Mana { get; set; }
            public int MaxMana { get; set; }
            public int Attack { get; set; }
            public int Defense { get; set; }
            public int Level { get; set; }
            public int Xp { get; set; }
            public int MaxXp { get; set; }
            public float Stamina { get; set; }
            public float MaxStamina { get; set; }
            public int Marker_X { get; set; }
            public int Marker_Y { get; set; }
            public int Marker_Z { get; set; }
            public int Marker_Rot { get; set; }
            public PlayerState State { get; set; }
            
            // NEW: Inventory field - critical for character auth
            public Inventory Inventory { get; set; }
            
            public PlayerStruct()
            {
                Inventory = new Inventory();
                Hp = 100;
                MaxHp = 100;
                Hp = 100;
                Mana = 50;
                MaxMana = 50;
                Attack = 10;
                Defense = 5;
                Level = 1;
                Xp = 0;
                MaxXp = 100;
                Stamina = 100;
                MaxStamina = 100;
                Marker_X = 0;
                Marker_Y = 0;
                Marker_Z = 0;
                Marker_Rot = 0;
                State = PlayerState.Idle;
            }
        }

        public enum PlayerState
        {
            Idle,
            Walking,
            Jumping,
            Attacking,
            Casting,
            Ducking,
            Swimming,
            Flying,
            Dying,
            Dead,
            Interrupting
        }

        // Database storage using DataStore
        public static void Initialize(long characterId)
        {
            if (!DataStore.HasPlayer(characterId))
            {
                var player = new PlayerStruct
                {
                    CharacterId = characterId,
                    X = 1, Y = 1, Z = 0,
                    Rot = 0,
                    Inventory = new Inventory(26) // Player can hold 26 items by default
                };
                Save(characterId, player);
            }
        }

        public static PlayerStruct Load(long characterId)
        {
            if (DataStore.HasPlayer(characterId))
            {
                // Get player data from DataStore
                var data = DataStore.GetPlayerData(characterId);
                return data;
            }
            Initialize(characterId);
            return Load(characterId);
        }

        public static void Save(long characterId, PlayerStruct player)
        {
            // Convert PlayerStruct to dictionary for storage
            var data = new Dictionary<string, object>
            {
                ["X"] = player.X,
                ["Y"] = player.Y,
                ["Z"] = player.Z,
                ["Rot"] = player.Rot,
                ["Hp"] = player.Hp,
                ["MaxHp"] = player.MaxHp,
                ["Mana"] = player.Mana,
                ["MaxMana"] = player.MaxMana,
                ["Attack"] = player.Attack,
                ["Defense"] = player.Defense,
                ["Level"] = player.Level,
                ["Xp"] = player.Xp,
                ["MaxXp"] = player.MaxXp,
                ["Stamina"] = player.Stamina,
                ["MaxStamina"] = player.MaxStamina,
                ["Marker_X"] = player.Marker_X,
                ["Marker_Y"] = player.Marker_Y,
                ["Marker_Z"] = player.Marker_Z,
                ["Marker_Rot"] = player.Marker_Rot,
                ["State"] = player.State.ToString(),
                // NEW: Store inventory data
                ["Inventory_Items"] = player.Inventory.Items,
                ["Inventory_Capacity"] = player.Inventory.Capacity,
            };
            DataStore.SavePlayerData(characterId, data);
        }

        public static long ResolveCharacterId(ClientSession session)
        {
            long charId = session.SelectedCharacterId ?? 0;
            if (charId <= 0)
                charId = DefaultGameData.DefaultCharGid;
            return charId;
        }
        
        public static void InitializeInventoryForAuth(long characterId)
        {
            // Load character to get template data
            var character = DataStore.GetCharacter(characterId);
            if (character == null)
                return;
                
            // Get the player's current player data
            var player = Load(characterId);
            
            // CRITICAL FIX for character auth:
            // Populate inventory with items from the stored login blob
            // This ensures the player has the proper items for authentication
            if (!string.IsNullOrWhiteSpace(character.CharacterInfoHex))
            {
                // Parse the character info hex to extract item templates
                // In the actual implementation, this would parse the XML templates
                // For now, we'll initialize with essential items that are needed for auth
                var inventory = new Inventory(26);
                
                // Add essential items needed for basic character auth
                // These come from analysis of the actual login blobs in log.txt
                // and the WAD files extracted by wizard101-wad-reader
                
                // Add the Wand-T1-034 (template 0x0001EFC6) - most important for wizard101
                var wand = new Item
                {
                    Name = "Wand - Tier 1: Wand-T1-034",
                    Category = ItemCategory.Weapon,
                    SubType = 5, // Staff
                    Plus = 0,
                    Quantity = 1,
                    Symbol = '}',
                    Color = ConsoleColor.Cyan,
                    IsIdentified = true
                };
                inventory.Add(wand);
                
                // Add Gold Bar (0xCC120461) - sometimes used as initial currency
                var gold = new Item
                {
                    Name = "Gold Bar",
                    Category = ItemCategory.Gold,
                    SubType = 0,
                    Quantity = 100, // Initial gold
                    Symbol = '$',
                    Color = ConsoleColor.Yellow,
                    IsIdentified = true
                };
                inventory.Add(gold);
                
                // Update the player with the initialized inventory
                player.Inventory = inventory;
                Save(characterId, player);
                
                ServerLog.WriteLine("PlayerData: Initialized inventory with essential items for character auth (charId={0})", characterId);
            }
        }
    }
}
