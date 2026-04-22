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
            Debug.Log($"[UIButtonSound] Awake on {gameObject.name}. Button found? {_button != null}");
        }

        void OnEnable()
        {
            if (_button != null)
            {
                _button.onClick.AddListener(HandleClick);
                Debug.Log($"[UIButtonSound] Listener added on {gameObject.name} for {soundId}");
            }
            else
            {
                Debug.LogError($"[UIButtonSound] No Button component found on {gameObject.name}");
            }
        }

        void OnDisable()
        {
            if (_button != null)
                _button.onClick.RemoveListener(HandleClick);
        }

        void HandleClick()
        {
            Debug.Log($"[UIButtonSound] Click detected on {gameObject.name}. soundId = {soundId}. interactable = {_button.interactable}, activeInHierarchy = {gameObject.activeInHierarchy}");

            if (SoundManager.Instance == null)
            {
                Debug.LogError("[UIButtonSound] SoundManager.Instance is NULL");
                return;
            }

            Debug.Log("[UIButtonSound] SoundManager found, trying to play sound");
            SoundManager.Instance.PlayGlobal(soundId);
        }
    }
}