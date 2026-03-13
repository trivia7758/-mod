using System;

namespace CaoCao.Data
{
    /// <summary>
    /// Represents a stack of identical items in the inventory.
    /// Serialized for save/load.
    /// </summary>
    [Serializable]
    public class ItemStack
    {
        public string itemId;    // links to ItemDefinition.id
        public int count = 1;

        public ItemStack() { }

        public ItemStack(string itemId, int count)
        {
            this.itemId = itemId;
            this.count = count;
        }
    }
}
