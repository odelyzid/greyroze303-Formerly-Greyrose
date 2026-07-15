using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Greyrose.Data;

namespace Greyrose
{
    class Program
    {
        static void Main(string[] args)
        {
            DataStore.Initialize(TryGetDbPath(args));

            if (args.Contains("--build-patch-only"))
            {
                PatchData.EnsurePatchFiles();
                var meta = PatchData.GetFileListMetadata();
                Console.WriteLine("Patch list built: {0} bytes, CRC {1:X8}",
                    meta.ListFileSize, meta.ListFileCRC);
                return;
            }

            if (args.Contains("--validate-patch-bin"))
            {
                string path = Path.Combine(PatchData.GetPatchDirectory(), DefaultPatchData.ListFileName);
                Environment.Exit(PatchListAudit.RunValidate(path));
                return;
            }

            if (args.Contains("--validate-login-blob"))
            {
                Environment.Exit(RunValidateLoginBlob(args));
                return;
            }

            if (args.Contains("--resanitize-player-blobs"))
            {
                Environment.Exit(ResanitizePlayerBlobs());
                return;
            }

            if (args.Contains("--import-zone-login-blob"))
            {
                Environment.Exit(RunImportZoneLoginBlob(args));
                return;
            }

            if (args.Contains("--dump-zone-login-blob"))
            {
                Environment.Exit(RunDumpZoneLoginBlob(args));
                return;
            }

            if (args.Contains("--inspect-login-blob"))
            {
                Environment.Exit(RunInspectLoginBlob(args));
                return;
            }

            if (args.Contains("--build-patch-minimal"))
            {
                PatchData.ForceRebuildMinimal();
                var meta = PatchData.GetFileListMetadata();
                Console.WriteLine("Minimal patch list: {0} bytes, CRC {1:X8}",
                    meta.ListFileSize, meta.ListFileCRC);
                return;
            }

            if (args.Contains("--hash-lookup"))
            {
                RunHashLookup(args);
                return;
            }

            if (args.Contains("--empty-player-blob"))
            {
                LoginBlobBuilder.EmptyPlayerBlobMode = true;
                Console.WriteLine("EMPTY PLAYER BLOB MODE ENABLED");
            }

            if (args.Contains("--minimal-player-blob"))
            {
                LoginBlobBuilder.MinimalPlayerBlobMode = true;
                Console.WriteLine("MINIMAL PLAYER BLOB MODE ENABLED");
            }

            if (args.Contains("--full-player-blob"))
            {
                LoginBlobBuilder.FullPlayerBlobMode = true;
                Console.WriteLine("FULL PLAYER BLOB MODE ENABLED (no sanitization)");
            }

            if (args.Contains("--raw-player-blob"))
            {
                LoginBlobBuilder.RawPlayerBlobMode = true;
                Console.WriteLine("RAW PLAYER BLOB MODE ENABLED (byte-for-byte DefaultLoginBlob.bin)");
            }

            if (args.Contains("--fix-prop-count"))
            {
                for (int i = 0; i < args.Length - 1; i++)
                {
                    if (args[i] == "--fix-prop-count" && int.TryParse(args[i + 1], out int pc))
                    {
                        LoginBlobBuilder.FixPropCount = pc;
                        Console.WriteLine("FIX PROP COUNT: {0}", pc);
                        break;
                    }
                }
            }

            if (args.Contains("--trunc-debug"))
            {
                for (int i = 0; i < args.Length - 1; i++)
                {
                    if (args[i] == "--trunc-debug" && int.TryParse(args[i + 1], out int truncLen))
                    {
                        LoginBlobBuilder.TruncateDebugLength = truncLen;
                        Console.WriteLine("TRUNCATE DEBUG MODE: {0} bytes", truncLen);
                        break;
                    }
                }
            }

            for (int i = 0; i < args.Length - 1; i++)
            {
                if (args[i] == "--patch-bytes")
                {
                    string spec = args[i + 1];
                    string[] parts = spec.Split(':');
                    if (parts.Length == 2 && int.TryParse(parts[0], out int offset))
                    {
                        byte[] value = Convert.FromHexString(parts[1]);
                        LoginBlobBuilder.PatchBytes.Add(Tuple.Create(offset, value));
                        Console.WriteLine("PATCH BYTES: offset {0} <- {1}", offset, spec);
                    }
                }
            }

#if GREYROSE_WINFORMS
            if (args.Contains("--create-ico"))
            {
                string png = Path.Combine(AppContext.BaseDirectory, "Assets", "greyrose303.png");
                if (!File.Exists(png))
                    png = Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "Assets", "greyrose303.png"));
                string ico = Path.ChangeExtension(png, ".ico");
                Branding.IconFactory.CreateIcoFromPng(png, ico);
                Console.WriteLine("Wrote {0}", ico);
                return;
            }

            if (args.Contains("--apply-branding"))
            {
                Environment.Exit(Branding.BrandingCommand.Run(args));
                return;
            }
#endif

#if GREYROSE_WINFORMS
            if (OperatingSystem.IsWindows() && !args.Contains("--console"))
            {
                UI.GuiEntry.Run();
                return;
            }
#endif

            if (OperatingSystem.IsWindows())
                Console.Title = "GreyrOze303";
            RunConsoleServer();
        }

        static string TryGetDbPath(string[] args)
        {
            for (int i = 0; i < args.Length - 1; i++)
            {
                if (args[i] == "--db")
                    return args[i + 1];
            }
            return null;
        }

        static void WriteDatabasePath()
        {
            Console.WriteLine("Database: {0}", Database.Path);
        }

        /// <summary>
        /// Validate the login blob for a character, checking for equipment and inventory markers, bad templates, and other issues.
        /// </summary>
        /// <param name="args"></param>
        /// <returns></returns>
        static int RunValidateLoginBlob(string[] args)
        {
            WriteDatabasePath();
            byte[] def = DefaultLoginBlob.GetBytes();
            CharacterRecord ch = null;
            for (int i = 0; i < args.Length - 1; i++)
            {
                if (args[i] == "--char-id" && long.TryParse(args[i + 1], out long charId))
                {
                    ch = DataStore.GetCharacter(charId);
                    Console.WriteLine("Character id {0} name '{1}' gid={2} defaultTemplate={3} zoneCapture={4}",
                        charId, ch?.Name, ch?.CharGid,
                        ch != null && CharacterInfoCodec.IsDefaultTemplate(ch.CharacterInfoHex),
                        ch != null && CharacterInfoCodec.UsesZoneLoginCapture(ch));
                    break;
                }
            }

            var build = LoginBlobBuilder.BuildLoginBlobWithInfo(ch, null, def);
            byte[] blob = build.Blob;

            if (blob == null || blob.Length == 0)
            {
                Console.WriteLine("No login blob available.");
                return 1;
            }

            var v = LoginBlobBuilder.Validate(blob, build.IsCreatedCharacter);
            Console.WriteLine("Login blob: {0} bytes (source={1}, created={2}, zoneCapture={3})",
                v.Length, build.Source, build.IsCreatedCharacter, CreatedZoneLoginBlob.IsAvailable());
            Console.WriteLine("  Equipment marker offset: {0}", v.EquipmentMarkerOffset);
            Console.WriteLine("  Inventory marker offset: {0}", v.InventoryMarkerOffset);
            Console.WriteLine("  Bad template offset:     {0}", v.BadTemplateOffset);
            Console.WriteLine("  Result: {0}", v.Message);
            return v.Ok ? 0 : 1;
        }

        /// <summary>
        /// Import a zone-login blob from a file, saving it to the data directory for use in the server.
        /// </summary>
        /// <param name="args"></param>
        /// <returns></returns>
        static int RunImportZoneLoginBlob(string[] args)
        {
            string path = null;
            for (int i = 0; i < args.Length - 1; i++)
            {
                if (args[i] == "--import-zone-login-blob")
                {
                    path = args[i + 1];
                    break;
                }
            }

            if (string.IsNullOrWhiteSpace(path))
            {
                Console.WriteLine("Usage: --import-zone-login-blob <file.bin|zone-data.bin>");
                return 1;
            }

            if (!ZoneLoginBlobImporter.TryReadPlayerBlobFile(path, out byte[] playerBlob, out string error))
            {
                Console.WriteLine("Import failed: {0}", error);
                return 1;
            }

            string dest = ZoneLoginBlobImporter.SaveToDataDirectory(playerBlob);
            Console.WriteLine("Imported {0} bytes -> {1}", playerBlob.Length, dest);
            return 0;
        }

        /// <summary>
        ///     Inspect a zone-login blob for a character, showing the parsed structure and any issues found.
        /// </summary>
        /// <param name="args"></param>
        /// <returns></returns>
        static int RunInspectLoginBlob(string[] args)
        {
            WriteDatabasePath();
            CharacterRecord ch = null;
            for (int i = 0; i < args.Length - 1; i++)
            {
                if (args[i] == "--char-id" && long.TryParse(args[i + 1], out long charId))
                {
                    ch = DataStore.GetCharacter(charId);
                    Console.WriteLine("Character id {0} name '{1}' gid={2} defaultTemplate={3} zoneCapture={4}",
                        charId, ch?.Name, ch?.CharGid,
                        ch != null && CharacterInfoCodec.IsDefaultTemplate(ch.CharacterInfoHex),
                        ch != null && CharacterInfoCodec.UsesZoneLoginCapture(ch));
                    break;
                }
            }

            if (ch == null)
            {
                Console.WriteLine("Character not found in this database.");
                return 1;
            }

            byte[] def = DefaultLoginBlob.GetBytes();
            var build = LoginBlobBuilder.BuildLoginBlobWithInfo(ch, null, def);
            byte[] blob = build.Blob;

            if (blob == null || blob.Length == 0)
            {
                Console.WriteLine("No login blob to inspect.");
                return 1;
            }

            bool created = build.IsCreatedCharacter;
            Console.WriteLine("Zone-login build: {0} bytes (source={1}, created={2})",
                blob.Length, build.Source, created);

            var state = ch != null ? DataStore.GetPlayerState(ch.Id) : null;
            if (state != null && !string.IsNullOrWhiteSpace(state.LoginBlobHex))
            {
                byte[] stored = CharacterInfoCodec.HexToBytes(state.LoginBlobHex);
                bool same = stored.Length == blob.Length;
                if (same)
                {
                    for (int i = 0; i < stored.Length; i++)
                    {
                        if (stored[i] != blob[i])
                        {
                            same = false;
                            break;
                        }
                    }
                }
                if (!same)
                {
                    Console.WriteLine("Stored DB blob: {0} bytes (differs — run --resanitize-player-blobs or Save in Inventory tab)",
                        stored.Length);
                    Console.Write(LoginBlobInspector.FormatInspectionReport(
                        LoginBlobInspector.Parse(stored), created));
                    Console.WriteLine();
                }
            }

            Console.WriteLine("--- MSG_ATTACH payload (fresh build) ---");
            Console.Write(LoginBlobInspector.FormatInspectionReport(LoginBlobInspector.Parse(blob), created));
            return 0;
        }

        /// <summary>
        ///     Dump a zone-login blob for a character, showing the raw hex bytes and some basic info.
        /// </summary>
        /// <param name="args"></param>
        /// <returns></returns>
        static int RunDumpZoneLoginBlob(string[] args)
        {
            byte[] def = DefaultLoginBlob.GetBytes();
            CharacterRecord ch = null;
            for (int i = 0; i < args.Length - 1; i++)
            {
                if (args[i] == "--char-id" && long.TryParse(args[i + 1], out long charId))
                    ch = DataStore.GetCharacter(charId);
            }

            var build = LoginBlobBuilder.BuildLoginBlobWithInfo(ch, null, def);
            if (build.Blob == null || build.Blob.Length == 0)
            {
                Console.WriteLine("No login blob to dump.");
                return 1;
            }

            Console.WriteLine("source={0} created={1} bytes={2}",
                build.Source, build.IsCreatedCharacter, build.Blob.Length);
            Console.WriteLine(CharacterInfoCodec.BytesToHex(build.Blob));
            return 0;
        }

        /// <summary>
        ///    Resanitize all player blobs in the database, rebuilding them from character info and default blobs.
        /// </summary>
        /// <returns></returns>
        static int ResanitizePlayerBlobs()
        {
            WriteDatabasePath();
            int updated = 0;
            byte[] def = DefaultLoginBlob.GetBytes();
            foreach (var ch in DataStore.GetAllCharacters())
            {
                var state = DataStore.GetPlayerState(ch.Id);
                if (state == null)
                    continue;

                byte[] fresh = LoginBlobBuilder.BuildLoginBlob(ch, null, def);
                string hex = CharacterInfoCodec.BytesToHex(fresh);
                string zoneHex = DefaultZoneBlob.GetHex();
                bool loginSame = string.Equals(state.LoginBlobHex, hex, StringComparison.OrdinalIgnoreCase);
                bool zoneSame = string.Equals(state.ZoneBlobHex, zoneHex, StringComparison.OrdinalIgnoreCase);
                if (loginSame && zoneSame)
                    continue;

                state.LoginBlobHex = hex;
                if (string.IsNullOrWhiteSpace(state.ZoneBlobHex))
                    state.ZoneBlobHex = zoneHex;
                DataStore.SavePlayerState(state);
                updated++;
                Console.WriteLine("Updated char {0} ({1}): login blob {2} bytes, zone blob {3} bytes",
                    ch.Id, ch.Name, fresh.Length,
                    CharacterInfoCodec.HexToBytes(state.ZoneBlobHex).Length);
            }

            Console.WriteLine("Resanitized {0} character(s).", updated);
            return 0;
        }

        /// <summary>
        ///     Brute-force WizHashString for a target hash value, checking known full names and prefix+suffix combinations.
        /// </summary>
        /// <param name="args"></param>
        static void RunHashLookup(string[] args)
        {
            uint target = 0x6F756F0A;
            Console.WriteLine("Brute-forcing WizHashString for target 0x{0:X8} ({1})...", target, target);

            string[] knownFull = {
                "ClientWizInventoryBehavior",
                "WizInventoryBehavior",
                "ClientInventoryBehavior",
                "InventoryBehavior",
                "ClientWizInventoryBehaviorItemList",
                "WizInventoryBehaviorItemList",
                "ClientInventoryItemList",
                "InventoryItemList",
                "ClientWizInventoryItemList",
                "WizInventoryItemList",
                "ClientItemList",
                "ItemList",
                "ObjectList",
                "WizObjectList",
                "ClientObjectList",
                "WizInventoryObject",
                "ClientWizInventoryObject",
                "WizItemObject",
                "ClientWizItemObject",
                "WizInventoryEntry",
                "ClientWizInventoryEntry",
                "InventoryEntry",
                "WizBehaviorList",
                "ClientWizBehaviorList",
                "BehaviorList",
                "WizInactiveBehavior",
                "ClientWizInactiveBehavior",
                "InactiveBehavior",
                "WizInactiveBehaviorList",
                "ClientWizInactiveBehaviorList",
                "InactiveBehaviorList",
                "m_itemList",
                "m_inactiveBehaviors",
                "itemList",
                "inactiveBehaviors",
                "ObjectVector",
                "WizObjectVector",
                "ClientObjectVector",
                "ObjectArray",
                "WizObjectArray",
                "ClientObjectArray",
                "SharedObject",
                "ClientSharedObject",
                "WizSharedObject",
                "SharedBehavior",
                "ClientSharedBehavior",
                "WizSharedBehavior",
                "WorldObject",
                "ClientWorldObject",
                "WizWorldObject",
                "CoreObject",
                "ClientCoreObject",
                "WizCoreObject",
                "ClientObject",
                "ClientBehavior",
                "WizBehavior",
                "WizItem",
                "ClientItem",
                "SharedInventory",
                "ClientSharedInventory",
                "WizSharedInventory",
                "WizGear",
                "ClientGear",
                "WizEquipment",
                "ClientEquipment",
                "SharedEquipment",
                "ClientSharedEquipment",
                "WizSharedEquipment",
                "WizEquipmentList",
                "ClientWizEquipmentList",
                "SharedEquipmentList",
                "ClientSharedEquipmentList",
                "WizInventoryList",
                "ClientWizInventoryList",
                "SharedInventoryList",
                "ClientSharedInventoryList",
                "WizGearList",
                "ClientWizGearList",
                "SharedGearList",
                "ClientSharedGearList",
                "WizItemData",
                "ClientWizItemData",
                "SharedItemData",
                "ClientSharedItemData",
                "WizItemInfo",
                "ClientWizItemInfo",
                "SharedItemInfo",
                "ClientSharedItemInfo",
                "WizObjectEntry",
                "ClientWizObjectEntry",
                "SharedObjectEntry",
                "ClientSharedObjectEntry",
                "WizObjectItem",
                "ClientWizObjectItem",
                "SharedObjectItem",
                "ClientSharedObjectItem",
                "ObjectEntry",
                "ObjectItem",
                "BehaviorObject",
                "ClientBehaviorObject",
                "WizBehaviorObject",
                "SharedBehaviorObject",
                "ClientSharedBehaviorObject",
                "WizSharedBehaviorObject",
                "WorldBehavior",
                "ClientWorldBehavior",
                "WizWorldBehavior",
                "SharedWorldBehavior",
                "ClientSharedWorldBehavior",
                "WizSharedWorldBehavior",
                "InventoryObject",
                "ClientInventoryObject",
                "SharedInventoryObject",
                "ClientSharedInventoryObject",
                "WizSharedInventoryObject",
                "EquipmentObject",
                "ClientEquipmentObject",
                "SharedEquipmentObject",
                "ClientSharedEquipmentObject",
                "WizSharedEquipmentObject",
                "WizInventoryBehaviorObject",
                "ClientWizInventoryBehaviorObject",
                "WizInventoryObjectList",
                "ClientWizInventoryObjectList",
                "WizInventoryObjectEntry",
                "ClientWizInventoryObjectEntry",
                "WizInventoryObjectItem",
                "ClientWizInventoryObjectItem",
                "WizInventoryObjectItemList",
                "ClientWizInventoryObjectItemList",
                "WizInventoryObjectVector",
                "ClientWizInventoryObjectVector",
                "WizInventoryObjectArray",
                "ClientWizInventoryObjectArray",
                "WizInventoryObjectSet",
                "ClientWizInventoryObjectSet",
                "ObjectStateSet",
                "CoreObjectStateSet",
                "ClientObjectStateSet",
                "WizObjectStateSet",
                "SharedObjectStateSet",
                "SerializerObjectState",
                "ClientSerializerObjectState",
                "WizSerializerObjectState",
                "SharedSerializerObjectState",
                "PreLoadObject",
                "CorePreLoadObject",
                "ClientPreLoadObject",
                "WizPreLoadObject",
                "SharedPreLoadObject",
                "ObjectTemplate",
                "CoreObjectTemplate",
                "ClientObjectTemplate",
                "WizObjectTemplate",
                "SharedObjectTemplate",
                "TemplateObject",
                "CoreTemplateObject",
                "ClientTemplateObject",
                "WizTemplateObject",
                "SharedTemplateObject",
            };

            int found = 0;
            foreach (string s in knownFull)
            {
                uint h = KiBinaryXml.WizHashString(s);
                if (h == target)
                {
                    Console.WriteLine("  MATCH: \"{0}\" -> 0x{1:X8}", s, h);
                    found++;
                }
            }
            Console.WriteLine("  Checked {0} full names, {1} matches", knownFull.Length, found);
            found = 0;

            string[] prefixes = {
                "Client", "Wiz", "Wizard", "Behavior", "BehaviorObject",
                "Inventory", "Item", "Object", "World", "Shared",
                "ClientWiz", "WizClient", "WizBehavior", "ClientBehavior",
                "ClientWizInventory", "WizInventory", "ClientInventory",
                "ObjectList", "ObjectVector", "Vector", "List", "Array",
                "Template", "State", "Core", "Serializer",
                "WizObject", "ClientObject", "SharedObject",
                "WizShared", "ClientShared", "Shared",
                "PreLoad", "CorePreLoad", "ClientPreLoad", "WizPreLoad",
                "TemplateObject", "CoreTemplate", "ClientTemplate", "WizTemplate",
                "ObjectState", "CoreObjectState", "ClientObjectState",
                "WizObjectState", "SharedObjectState",
                "SerializerObject", "CoreSerializerObject", "ClientSerializerObject",
                "WizSerializerObject", "SharedSerializerObject",
                "InventoryBehavior", "ClientInventoryBehavior", "WizInventoryBehavior",
                "SharedInventoryBehavior", "ClientSharedInventoryBehavior",
                "WizSharedInventoryBehavior",
                "EquipmentBehavior", "ClientEquipmentBehavior", "WizEquipmentBehavior",
                "SharedEquipmentBehavior", "ClientSharedEquipmentBehavior",
                "WizSharedEquipmentBehavior",
                "GearBehavior", "ClientGearBehavior", "WizGearBehavior",
                "SharedGearBehavior", "ClientSharedGearBehavior",
                "WizSharedGearBehavior",
                "ItemBehavior", "ClientItemBehavior", "WizItemBehavior",
                "SharedItemBehavior", "ClientSharedItemBehavior",
                "WizSharedItemBehavior",
                "BehaviorItem", "ClientBehaviorItem", "WizBehaviorItem",
                "SharedBehaviorItem", "ClientSharedBehaviorItem",
                "WizSharedBehaviorItem",
                "BehaviorList", "ClientBehaviorList", "WizBehaviorList",
                "SharedBehaviorList", "ClientSharedBehaviorList",
                "WizSharedBehaviorList",
                "BehaviorVector", "ClientBehaviorVector", "WizBehaviorVector",
                "SharedBehaviorVector", "ClientSharedBehaviorVector",
                "WizSharedBehaviorVector",
                "ObjectBehavior", "ClientObjectBehavior", "WizObjectBehavior",
                "SharedObjectBehavior", "ClientSharedObjectBehavior",
                "WizSharedObjectBehavior",
                "InventoryList", "ClientInventoryList", "WizInventoryList",
                "SharedInventoryList", "ClientSharedInventoryList",
                "WizSharedInventoryList",
                "InventoryVector", "ClientInventoryVector", "WizInventoryVector",
                "SharedInventoryVector", "ClientSharedInventoryVector",
                "WizSharedInventoryVector",
                "ItemList", "ClientItemList", "WizItemList",
                "SharedItemList", "ClientSharedItemList",
                "WizSharedItemList",
                "ItemVector", "ClientItemVector", "WizItemVector",
                "SharedItemVector", "ClientSharedItemVector",
                "WizSharedItemVector",
                "ObjectList", "ClientObjectList", "WizObjectList",
                "SharedObjectList", "ClientSharedObjectList",
                "WizSharedObjectList",
                "ObjectVector", "ClientObjectVector", "WizObjectVector",
                "SharedObjectVector", "ClientSharedObjectVector",
                "WizSharedObjectVector",
                "GearList", "ClientGearList", "WizGearList",
                "SharedGearList", "ClientSharedGearList",
                "WizSharedGearList",
                "GearVector", "ClientGearVector", "WizGearVector",
                "SharedGearVector", "ClientSharedGearVector",
                "WizSharedGearVector",
                "EquipmentList", "ClientEquipmentList", "WizEquipmentList",
                "SharedEquipmentList", "ClientSharedEquipmentList",
                "WizSharedEquipmentList",
                "EquipmentVector", "ClientEquipmentVector", "WizEquipmentVector",
                "SharedEquipmentVector", "ClientSharedEquipmentVector",
                "WizSharedEquipmentVector",
            };

            string[] suffixes = {
                "", "Item", "Items", "List", "Vector", "Array", "Set",
                "Behavior", "Object", "State", "Data", "Info", "Entry",
                "Type", "Class", "Template", "Config", "Manager",
                "Inventory", "Equipment", "Gear", "Wand", "Pet",
                "Client", "Server", "Core", "Base", "Root",
                "Vector", "Array", "Set", "Map", "Dict",
            };

            Console.WriteLine("  Trying prefix+suffix combos...");
            foreach (string p in prefixes)
            {
                foreach (string s in suffixes)
                {
                    string name = p + s;
                    uint h = KiBinaryXml.WizHashString(name);
                    if (h == target)
                    {
                        Console.WriteLine("  MATCH: \"{0}\" -> 0x{1:X8}", name, h);
                        found++;
                    }
                }
            }
            Console.WriteLine("  Checked {0} prefix+suffix combos, {1} matches",
                prefixes.Length * suffixes.Length, found);
        }

        static void RunConsoleServer()
        {
            Server.StartPatchFileServer();
            var task1 = Task.Run(Server.LS);
            var task2 = Task.Run(Server.PS);
            var task3 = Task.Run(Server.GS);
            Task.WhenAll(task1, task2, task3).GetAwaiter().GetResult();
        }
    }
}
