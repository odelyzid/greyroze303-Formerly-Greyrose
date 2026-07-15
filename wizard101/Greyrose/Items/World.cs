using System.Collections.Generic;

namespace Greyrose.Items
{
    public static class World
    {
        public static int PlayerX;
        public static int PlayerY;
        public static PlayerInfo Player = new PlayerInfo();

        public static Dictionary<string, List<Item>> FloorItems = new Dictionary<string, List<Item>>();

        public static string PositionKey(int x, int y)
        {
            return x + "," + y;
        }

        public static List<Item> GetItemsAt(int x, int y)
        {
            List<Item> items;
            if (FloorItems.TryGetValue(PositionKey(x, y), out items))
                return items;
            return null;
        }

        public static void AddFloorItem(int x, int y, Item item)
        {
            string key = PositionKey(x, y);
            List<Item> items;
            if (!FloorItems.TryGetValue(key, out items))
            {
                items = new List<Item>();
                FloorItems[key] = items;
            }
            items.Add(item);
        }
    }
}
