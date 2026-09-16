using DG.Tweening;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace Chess
{
    // Small icon shown in the "owned relics" bar. Hovering pops the icon slightly and shows
    // RelicTooltipUI with the relic's name/description and any matched gameplay keywords.
    public class RelicIconUI : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
    {
        public Image iconImage;

        [Header("Hover Pop")]
        [SerializeField] float hoverScale = 1.12f;
        [SerializeField] float hoverDuration = 0.12f;

        RelicSO _relic;
        Vector3 _baseScale;
        Tween _scaleTween;

        void Awake()
        {
            _baseScale = transform.localScale;
        }

        public void Bind(RelicSO relic)
        {
            _relic = relic;

            if (iconImage == null || relic == null) return;
            iconImage.sprite = relic.icon;
            iconImage.enabled = relic.icon != null;
        }

        public void OnPointerEnter(PointerEventData eventData)
        {
            _scaleTween?.Kill();
            _scaleTween = transform.DOScale(_baseScale * hoverScale, hoverDuration).SetEase(Ease.OutBack);

            if (_relic != null)
                RelicTooltipUI.Instance?.Show(_relic);
        }

        public void OnPointerExit(PointerEventData eventData)
        {
            _scaleTween?.Kill();
            _scaleTween = transform.DOScale(_baseScale, hoverDuration).SetEase(Ease.OutQuad);

            RelicTooltipUI.Instance?.Hide();
        }

        void OnDisable()
        {
            _scaleTween?.Kill();
            transform.localScale = _baseScale;
            RelicTooltipUI.Instance?.Hide();
        }
    }
}
