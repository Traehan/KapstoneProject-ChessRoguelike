using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Chess
{
    public class JuiceSettingsUIController : MonoBehaviour
    {
        [Header("Speed Preset")]
        [SerializeField] TMP_Text speedLabel;

        [Header("Toggles")]
        [SerializeField] Toggle hitStopToggle;
        [SerializeField] Toggle screenShakeToggle;

        void OnEnable()
        {
            RefreshUI();
        }

        public void CycleSpeed()
        {
            JuiceSettings.CycleSpeedPreset();
            RefreshUI();
        }

        public void SetHitStopEnabled(bool isEnabled)
        {
            JuiceSettings.HitStopEnabled = isEnabled;
        }

        public void SetScreenShakeEnabled(bool isEnabled)
        {
            JuiceSettings.ScreenShakeEnabled = isEnabled;
        }

        void RefreshUI()
        {
            if (speedLabel != null)
                speedLabel.text = $"Speed: {JuiceSettings.SpeedPreset}";

            if (hitStopToggle != null)
                hitStopToggle.SetIsOnWithoutNotify(JuiceSettings.HitStopEnabled);

            if (screenShakeToggle != null)
                screenShakeToggle.SetIsOnWithoutNotify(JuiceSettings.ScreenShakeEnabled);
        }
    }
}
