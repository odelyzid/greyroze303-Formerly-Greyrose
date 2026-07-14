namespace Greyrose.Data
{
    static class SeedData
    {
        public static void Apply()
        {
            var accountId = DataStore.InsertAccount(new AccountRecord
            {
                UserGid = DefaultGameData.DefaultUserGid,
                Username = DefaultGameData.DefaultUsername,
                PassKey = "",
                PurchasedSlots = 5
            });

            var charId = DataStore.InsertCharacter(new CharacterRecord
            {
                AccountId = accountId,
                CharGid = DefaultGameData.DefaultCharGid,
                Name = DefaultGameData.DefaultCharacterName,
                Slot = 0,
                ZoneName = DefaultGameData.DefaultZoneName,
                ZoneGid = DefaultGameData.DefaultZoneGid,
                Location = DefaultGameData.DefaultLocation,
                CharacterInfoHex = DefaultGameData.DefaultCharacterInfoHex
            });

            var seedCharacter = new CharacterRecord
            {
                CharGid = DefaultGameData.DefaultCharGid,
                ZoneGid = DefaultGameData.DefaultZoneGid,
                CharacterInfoHex = DefaultGameData.DefaultCharacterInfoHex
            };
            var build = LoginBlobBuilder.BuildLoginBlobWithInfo(seedCharacter, null, DefaultLoginBlob.GetBytes());

            DataStore.SavePlayerState(new PlayerStateRecord
            {
                CharacterId = charId,
                X = 2572,
                Y = 4376,
                Z = -28,
                Rot = 5.55f,
                LoginBlobHex = CharacterInfoCodec.BytesToHex(build.Blob),
                ZoneBlobHex = DefaultZoneBlob.GetHex()
            });

            ServerLog.WriteLine("Database seeded with default account and character (login blob {0} bytes, source={1}).",
                build.Blob.Length, build.Source);
        }
    }
}
