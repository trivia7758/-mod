using UnityEngine;

namespace CaoCao.Core
{
    [System.Serializable]
    public class VersionData
    {
        public string version_name = "0.1.0";
        public int version_code = 100;
    }

    public class VersionManager
    {
        readonly SaveSystem _save;

        public VersionManager(SaveSystem save)
        {
            _save = save;
        }

        public string GetLocalVersionName()
        {
            var data = _save.LoadJson<VersionData>("version_local.json");
            return data.version_name;
        }

        public bool CheckForUpdate()
        {
            var local = _save.LoadJson<VersionData>("version_local.json");
            var remote = _save.LoadJson<VersionData>("version_remote.json");
            return remote.version_code > local.version_code;
        }
    }
}
