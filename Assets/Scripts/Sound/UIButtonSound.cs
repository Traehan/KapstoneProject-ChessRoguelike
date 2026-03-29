using UnityEngine;
using UnityEngine.UI;

namespace Chess
{
    [RequireComponent(typeof(Button))]
    public class UIButtonSound : MonoBehaviour
    {
        [SerializeField] SoundEventId soundId = SoundEventId.UIButtonClick;
        Button _button;

        void Awake()
        {
            _button = GetComponent<Button>();
        }

        void OnEnable()
        {
            if (_button != null)
                _button.onClick.AddListener(HandleClick);
        }

        void OnDisable()
        {
            if (_button != null)
                _button.onClick.RemoveListener(HandleClick);
        }

        void HandleClick()
        {
            if (SoundManager.Instance != null)
                SoundManager.Instance.PlayGlobal(soundId);
        }
    }
}