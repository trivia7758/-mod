using System.IO;
using UnityEngine;

namespace CaoCao.Core
{
    public class SaveSystem
    {
        public string DataPath => Path.Combine(Application.persistentDataPath, "data");
        public string SavesPath => Path.Combine(DataPath, "saves");

        public void EnsureDataDirectory()
        {
            if (!Directory.Exists(DataPath))
                Directory.CreateDirectory(DataPath);
            if (!Directory.Exists(SavesPath))
                Directory.CreateDirectory(SavesPath);
        }

        public void SaveJson<T>(string filename, T data)
        {
            string path = Path.Combine(DataPath, filename);
            string json = JsonUtility.ToJson(data, true);
            File.WriteAllText(path, json);
        }

        public T LoadJson<T>(string filename) where T : new()
        {
            string path = Path.Combine(DataPath, filename);
            if (!File.Exists(path))
                return new T();
            string json = File.ReadAllText(path);
            return JsonUtility.FromJson<T>(json);
        }

        public string LoadText(string filename)
        {
            string path = Path.Combine(DataPath, filename);
            if (!File.Exists(path))
                return "";
            return File.ReadAllText(path);
        }

        public void SaveText(string filename, string text)
        {
            string path = Path.Combine(DataPath, filename);
            File.WriteAllText(path, text);
        }

        public bool FileExists(string filename)
        {
            return File.Exists(Path.Combine(DataPath, filename));
        }

        public void CopyFromResources(string resourcePath, string targetFilename, bool overwrite = false)
        {
            string targetPath = Path.Combine(DataPath, targetFilename);
            if (!overwrite && File.Exists(targetPath))
                return;

            var textAsset = Resources.Load<TextAsset>(resourcePath);
            if (textAsset != null)
            {
                File.WriteAllText(targetPath, textAsset.text);
            }
        }
    }
}
