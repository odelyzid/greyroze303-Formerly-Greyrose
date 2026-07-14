using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;

namespace Greyrose.Data
{
    static class CharacterInfoCodec
    {
        // Char GID lives inside the KingsIsle object graph, not at a fixed offset.
        public const int DisplayBlobSize = 168;

        public static byte[] Latin1Bytes(string value)
        {
            if (string.IsNullOrEmpty(value))
                return Array.Empty<byte>();
            return Encoding.Latin1.GetBytes(value);
        }

        public static string BytesToHex(byte[] data)
        {
            if (data == null || data.Length == 0)
                return "";
            return BitConverter.ToString(data).Replace("-", " ");
        }

        public static byte[] HexToBytes(string hex)
        {
            if (string.IsNullOrWhiteSpace(hex))
                return Array.Empty<byte>();

            string compact = NormalizeHex(hex);
            if (compact.Length % 2 != 0)
                compact += "0";

            var data = new byte[compact.Length / 2];
            for (int i = 0; i < data.Length; i++)
                data[i] = byte.Parse(compact.Substring(i * 2, 2), NumberStyles.HexNumber, CultureInfo.InvariantCulture);
            return data;
        }

        public static long? TryExtractCharGid(byte[] blob)
        {
            if (blob == null || blob.Length < 8)
                return null;

            // Scan for a KingsIsle-style character GID embedded in the serialized blob.
            for (int i = 0; i <= blob.Length - 8; i++)
            {
                long value = BitConverter.ToInt64(blob, i);
                if (value > 100_000_000_000_000L && value < 10_000_000_000_000_000L)
                    return value;
            }

            return null;
        }

        public static byte[] PatchCharGid(byte[] blob, long charGid)
        {
            // Do not patch — the client-owned creation blob must be stored and echoed verbatim.
            return blob;
        }

        public static byte[] PatchNameHash(byte[] blob, string name)
        {
            // Name is encoded inside the KingsIsle object graph, not at a fixed offset.
            // Patching a guessed offset corrupts the blob and breaks character select.
            return blob;
        }

        public static string NormalizeHex(string hex)
        {
            if (string.IsNullOrWhiteSpace(hex))
                return "";
            return string.Concat(hex.Where(c => !char.IsWhiteSpace(c)));
        }

        public static bool IsDefaultTemplate(string hex)
        {
            return string.Equals(
                NormalizeHex(hex),
                NormalizeHex(DefaultGameData.DefaultCharacterInfoHex),
                StringComparison.OrdinalIgnoreCase);
        }

        /// <summary>
        /// True when MSG_ATTACH should use the zone-login player capture (equipment, no inventory),
        /// not the stock seed stats-only prefix.
        /// </summary>
        public static bool UsesZoneLoginCapture(CharacterRecord character)
        {
            if (character == null)
                return false;
            if (!IsDefaultTemplate(character.CharacterInfoHex))
                return true;
            return character.CharGid != DefaultGameData.DefaultCharGid;
        }

        /// <summary>
        /// Expands short CREATECHARACTER payloads (typically ~72 bytes) into the full
        /// ~168-byte display blob the client expects on the character select screen.
        /// </summary>
        public static byte[] NormalizeDisplayBlob(byte[] blob)
        {
            byte[] template = HexToBytes(DefaultGameData.DefaultCharacterInfoHex);
            if (blob == null || blob.Length == 0)
                return (byte[])template.Clone();

            if (blob.Length >= 2)
            {
                ushort declaredSize = BitConverter.ToUInt16(blob, 0);
                if (declaredSize == blob.Length && blob.Length >= DisplayBlobSize)
                    return blob;
            }

            var merged = (byte[])template.Clone();
            int srcOffset = 0;
            if (blob.Length >= 2 && blob[0] == template[0] && blob[1] == template[1])
                srcOffset = 2;

            int copyLen = Math.Min(blob.Length - srcOffset, merged.Length - 2);
            if (copyLen > 0)
                Array.Copy(blob, srcOffset, merged, 2, copyLen);

            BitConverter.GetBytes((ushort)merged.Length).CopyTo(merged, 0);
            return merged;
        }

        /// <summary>
        /// Returns the character info blob for the character list packet.
        /// Strips equipment items (class marker 44 64 73 43) that reference templates
        /// the client cannot resolve, causing "Failed to find type by name hash" errors.
        /// </summary>
        public static byte[] PrepareForClient(CharacterRecord character)
        {
            if (character == null)
                return Array.Empty<byte>();

            byte[] blob = HexToBytes(character.CharacterInfoHex);
            return StripEquipmentItems(blob);
        }

        /// <summary>
        /// Removes equipment item entries from a character info blob.
        /// Equipment class markers (44 64 73 43) appear after the item-count field
        /// at offset 60. Each item is 18 bytes. Stripping them and zeroing the count
        /// produces a clean creation-format blob the character list can parse.
        /// </summary>
        static byte[] StripEquipmentItems(byte[] blob)
        {
            if (blob == null || blob.Length < 70)
                return blob ?? Array.Empty<byte>();

            int firstEquip = -1;
            for (int i = 22; i <= blob.Length - 4; i++)
            {
                if (blob[i] == 0x44 && blob[i + 1] == 0x64 && blob[i + 2] == 0x73 && blob[i + 3] == 0x43)
                {
                    firstEquip = i;
                    break;
                }
            }

            if (firstEquip < 0)
                return blob;

            int icOffset = firstEquip - 4;
            if (icOffset < 2 || icOffset + 4 > blob.Length)
                return blob;

            int itemCount = BitConverter.ToInt32(blob, icOffset);
            if (itemCount <= 0 || itemCount > 16)
                return blob;

            int equipEnd = firstEquip + itemCount * 18;
            if (equipEnd > blob.Length)
                return blob;

            int tailLen = blob.Length - equipEnd;
            int newLen = firstEquip + tailLen;
            var result = new byte[newLen];

            Array.Copy(blob, 0, result, 0, firstEquip);
            result[icOffset] = 0;
            result[icOffset + 1] = 0;
            result[icOffset + 2] = 0;
            result[icOffset + 3] = 0;
            if (tailLen > 0)
                Array.Copy(blob, equipEnd, result, firstEquip, tailLen);

            int payloadSize = newLen - 2;
            if (payloadSize > 0 && payloadSize <= 0xFFFF)
            {
                result[0] = (byte)(payloadSize & 0xFF);
                result[1] = (byte)((payloadSize >> 8) & 0xFF);
            }

            return result;
        }

        public static string PrepareForClientHex(CharacterRecord character)
        {
            return BytesToHex(PrepareForClient(character));
        }

        public static string TryExtractName(byte[] blob)
        {
            if (blob == null || blob.Length < 3)
                return null;

            string best = null;
            for (int i = 0; i + 2 < blob.Length; i++)
            {
                int length = BitConverter.ToUInt16(blob, i);
                if (length < 3 || length > 32 || i + 2 + length > blob.Length)
                    continue;

                bool printable = true;
                for (int j = 0; j < length; j++)
                {
                    byte b = blob[i + 2 + j];
                    if (b < 0x20 || b > 0x7E)
                    {
                        printable = false;
                        break;
                    }
                }
                if (!printable)
                    continue;

                string candidate = Encoding.ASCII.GetString(blob, i + 2, length);
                if (candidate.Contains('/') || candidate.StartsWith("WizardZone", StringComparison.OrdinalIgnoreCase))
                    continue;

                best = candidate;
            }

            return best;
        }

        public static long AllocateCharGid(long accountId)
        {
            long maxGid = DefaultGameData.DefaultCharGid;
            foreach (var account in DataStore.GetAllAccounts())
            {
                foreach (var ch in DataStore.GetCharactersByAccountId(account.Id))
                    if (ch.CharGid > maxGid)
                        maxGid = ch.CharGid;
            }

            long next = maxGid + 1;
            if (next <= 0)
                next = DefaultGameData.DefaultCharGid + accountId + DateTime.UtcNow.Ticks % 1000;
            return next;
        }

        public static int FindNextSlot(IEnumerable<CharacterRecord> existing)
        {
            var used = new HashSet<int>(existing.Select(c => c.Slot));
            for (int slot = 0; slot < 128; slot++)
            {
                if (!used.Contains(slot))
                    return slot;
            }
            return existing.Count();
        }
    }
}
