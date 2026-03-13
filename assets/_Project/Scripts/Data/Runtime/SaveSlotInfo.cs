using System;

namespace CaoCao.Data
{
    /// <summary>
    /// Lightweight metadata for a save slot, used by the save/load UI.
    /// Does not contain actual game state data.
    /// </summary>
    [Serializable]
    public class SaveSlotInfo
    {
        public int slot;
        public string chapterName = "";
        public string timestamp = "";
        public bool exists;
    }
}
