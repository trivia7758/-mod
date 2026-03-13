namespace CaoCao.Story
{
    public static class StoryMapHelper
    {
        /// <summary>
        /// Extract a normalized map key from a Godot background path.
        /// "res://assets/ui/backgrounds/story/home.png" -> "story/home"
        /// "res://assets/ui/backgrounds/xuanwo.jpg" -> "xuanwo"
        /// </summary>
        public static string ExtractMapKey(string godotPath)
        {
            string p = godotPath.Replace("res://assets/ui/backgrounds/", "");
            int dotIdx = p.LastIndexOf('.');
            if (dotIdx >= 0) p = p.Substring(0, dotIdx);
            return p;
        }
    }
}
