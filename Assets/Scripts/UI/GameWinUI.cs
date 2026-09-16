using UnityEngine;
using TMPro;
using DG.Tweening;

namespace Chess
{
    public class GameWinUI : MonoBehaviour
    {
        [SerializeField] GameObject winPanel;
        [SerializeField] TextMeshProUGUI winText;

        [Header("Reveal")]
        [SerializeField] float panelFadeDuration = 0.45f;
        [SerializeField] float panelStartScale = 0.92f;
        [SerializeField] Ease panelEase = Ease.OutCubic;

        CanvasGroup _panelCanvasGroup;
        RectTransform _panelRect;

        void Awake()
        {
            if (winPanel != null) winPanel.SetActive(false);
        }

        public void ShowWin()
        {
            if (winPanel == null) return;

            EnsurePanelRefs();

            if (winText != null) winText.text = "YOU WIN!";

            if (_panelCanvasGroup != null)
            {
                _panelCanvasGroup.DOKill();
                _panelCanvasGroup.alpha = 0f;
            }

            if (_panelRect != null)
            {
                _panelRect.DOKill();
                _panelRect.localScale = Vector3.one * panelStartScale;
            }

            winPanel.SetActive(true);

            // Win-screen reveal is deliberately floored well above zero even at the "Instant" speed
            // preset — this is a one-time celebratory beat, not combat pacing, so it should stay
            // satisfying regardless of how fast the player wants regular turns to resolve.
            float duration = panelFadeDuration * Mathf.Max(0.5f, JuiceSettings.AnimationDurationMultiplier);

            if (_panelCanvasGroup != null)
                _panelCanvasGroup.DOFade(1f, duration).SetEase(panelEase).SetUpdate(true);

            if (_panelRect != null)
                _panelRect.DOScale(1f, duration).SetEase(panelEase).SetUpdate(true);
        }

        void EnsurePanelRefs()
        {
            if (_panelCanvasGroup == null)
            {
                _panelCanvasGroup = winPanel.GetComponent<CanvasGroup>();
                if (_panelCanvasGroup == null)
                    _panelCanvasGroup = winPanel.AddComponent<CanvasGroup>();
            }

            if (_panelRect == null)
                _panelRect = winPanel.GetComponent<RectTransform>();
        }
    }
}