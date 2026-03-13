using UnityEngine;

namespace CaoCao.Core
{
    public enum SpeedSetting { Fast, Mid, Slow }

    [CreateAssetMenu(fileName = "GameSettings", menuName = "CaoCao/GameSettings")]
    public class GameSettingsData : ScriptableObject
    {
        public SpeedSetting messageSpeed = SpeedSetting.Mid;
        public SpeedSetting moveSpeed = SpeedSetting.Mid;
        public bool autoPlay = true;
        public string language = "zh";

        public float GetCharsPerSec()
        {
            return messageSpeed switch
            {
                SpeedSetting.Fast => 80f,
                SpeedSetting.Mid => 40f,
                SpeedSetting.Slow => 20f,
                _ => 40f
            };
        }

        public float GetMoveStepDuration()
        {
            return moveSpeed switch
            {
                SpeedSetting.Fast => 0.1f,
                SpeedSetting.Mid => 0.167f,
                SpeedSetting.Slow => 0.25f,
                _ => 0.167f
            };
        }
    }
}
