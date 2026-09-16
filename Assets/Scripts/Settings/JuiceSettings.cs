using System;
using UnityEngine;

public enum GameSpeedPreset { Normal, Fast, Faster, Instant }

public static class JuiceSettings
{
    const string SpeedPresetKey = "Juice_SpeedPreset";
    const string HitStopEnabledKey = "Juice_HitStopEnabled";
    const string ScreenShakeEnabledKey = "Juice_ScreenShakeEnabled";

    public static event Action OnSettingsChanged;

    static GameSpeedPreset? _speedPreset;
    static bool? _hitStopEnabled;
    static bool? _screenShakeEnabled;

    public static GameSpeedPreset SpeedPreset
    {
        get
        {
            if (_speedPreset == null)
                _speedPreset = (GameSpeedPreset)PlayerPrefs.GetInt(SpeedPresetKey, (int)GameSpeedPreset.Normal);

            return _speedPreset.Value;
        }
        set
        {
            if (_speedPreset == value)
                return;

            _speedPreset = value;
            PlayerPrefs.SetInt(SpeedPresetKey, (int)value);
            PlayerPrefs.Save();
            OnSettingsChanged?.Invoke();
        }
    }

    public static bool HitStopEnabled
    {
        get
        {
            if (_hitStopEnabled == null)
                _hitStopEnabled = PlayerPrefs.GetInt(HitStopEnabledKey, 1) != 0;

            return _hitStopEnabled.Value;
        }
        set
        {
            if (_hitStopEnabled == value)
                return;

            _hitStopEnabled = value;
            PlayerPrefs.SetInt(HitStopEnabledKey, value ? 1 : 0);
            PlayerPrefs.Save();
            OnSettingsChanged?.Invoke();
        }
    }

    public static bool ScreenShakeEnabled
    {
        get
        {
            if (_screenShakeEnabled == null)
                _screenShakeEnabled = PlayerPrefs.GetInt(ScreenShakeEnabledKey, 1) != 0;

            return _screenShakeEnabled.Value;
        }
        set
        {
            if (_screenShakeEnabled == value)
                return;

            _screenShakeEnabled = value;
            PlayerPrefs.SetInt(ScreenShakeEnabledKey, value ? 1 : 0);
            PlayerPrefs.Save();
            OnSettingsChanged?.Invoke();
        }
    }

    public static float AnimationDurationMultiplier => SpeedPreset switch
    {
        GameSpeedPreset.Normal => 1.0f,
        GameSpeedPreset.Fast => 0.65f,
        GameSpeedPreset.Faster => 0.35f,
        GameSpeedPreset.Instant => 0.0f,
        _ => 1.0f
    };

    public static float EnemyPacingMultiplier => SpeedPreset switch
    {
        GameSpeedPreset.Normal => 1.0f,
        GameSpeedPreset.Fast => 0.6f,
        GameSpeedPreset.Faster => 0.3f,
        GameSpeedPreset.Instant => 0.05f,
        _ => 1.0f
    };

    public static void CycleSpeedPreset() => SpeedPreset = (GameSpeedPreset)(((int)SpeedPreset + 1) % 4);
}
