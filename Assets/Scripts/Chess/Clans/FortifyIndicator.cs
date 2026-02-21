using UnityEngine;
using TMPro;

namespace Chess
{
    [DisallowMultipleComponent]
    public class FortifyIndicator : MonoBehaviour
    {
        [Header("Sprite")]
        [SerializeField] Sprite shieldSprite;
        [SerializeField] int maxStacksShown = 99;

        [Header("Sorting")]
        [SerializeField] string sortingLayerName = "Pieces";
        [SerializeField] int sortingOrder = 20;

        [Header("Billboard")]
        [SerializeField] bool billboardToCamera = true;

        [Header("Icon Transform (LOCAL)")]
        [SerializeField] Vector3 iconLocalPosition = new Vector3(0.75f, 1.2f, -0.1f);
        [SerializeField] Vector3 iconLocalRotationEuler = new Vector3(10f, 0f, 0f);
        [SerializeField] Vector3 iconLocalScale = new Vector3(0.4f, 0.4f, 0.15f);

        [Header("Text Transform (LOCAL, relative to Icon)")]
        // Default: centered inside the shield (looks like your screenshot)
        [SerializeField] Vector3 textLocalPosition = Vector3.zero;
        [SerializeField] Vector3 textLocalRotationEuler = Vector3.zero;
        [SerializeField] Vector3 textLocalScale = Vector3.one;

        [Header("Text Style")]
        [SerializeField] bool showWhenZero = false;
        [SerializeField] bool showOnlyWhenGreaterThanOne = false; // you’re showing “3”, keep false if you want always show
        [SerializeField] float fontSize = 10f;
        [SerializeField] Color fontColor = Color.black;
        [SerializeField] TextAlignmentOptions alignment = TextAlignmentOptions.Center;

        Piece _piece;
        SpriteRenderer _icon;
        TextMeshPro _countText;

        void Awake()
        {
            _piece = GetComponentInParent<Piece>();
            if (_piece == null)
            {
                Debug.LogWarning("FortifyIndicator: No Piece found in parents. Place this on a child of a Piece.");
                enabled = false;
                return;
            }

            BuildVisual();
        }

        void OnEnable()
        {
            SyncToStacks();
        }

        void LateUpdate()
        {
            SyncToStacks();

            if (billboardToCamera && Camera.main != null)
                transform.forward = Camera.main.transform.forward;
        }

        void BuildVisual()
        {
            // wipe old children (ex: previous pip objects)
            for (int i = transform.childCount - 1; i >= 0; i--)
                Destroy(transform.GetChild(i).gameObject);

            // --- Icon ---
            var iconGO = new GameObject("FortifyIcon");
            iconGO.transform.SetParent(transform, false);
            iconGO.transform.localPosition = iconLocalPosition;
            iconGO.transform.localRotation = Quaternion.Euler(iconLocalRotationEuler);
            iconGO.transform.localScale = iconLocalScale;

            _icon = iconGO.AddComponent<SpriteRenderer>();
            _icon.sprite = shieldSprite;
            _icon.sortingLayerName = sortingLayerName;
            _icon.sortingOrder = sortingOrder;

            // --- Text ---
            var textGO = new GameObject("FortifyCount");
            textGO.transform.SetParent(iconGO.transform, false);
            textGO.transform.localPosition = new Vector3(0f, 1.2f, 0f);
            textGO.transform.localRotation = Quaternion.Euler(textLocalRotationEuler);
            textGO.transform.localScale = textLocalScale;

            _countText = textGO.AddComponent<TextMeshPro>();
            _countText.text = "";
            _countText.fontSize = fontSize;
            _countText.color = fontColor;
            _countText.alignment = alignment;

            // ensure it renders on top of the icon
            _countText.sortingLayerID = SortingLayer.NameToID(sortingLayerName);
            _countText.sortingOrder = sortingOrder + 1;
        }

        void SyncToStacks()
        {
            if (_piece == null || _icon == null || _countText == null) return;

            int stacks = Mathf.Clamp(_piece.fortifyStacks, 0, maxStacksShown);

            bool shouldShow = showWhenZero ? stacks >= 0 : stacks > 0;
            _icon.enabled = shouldShow;

            if (!shouldShow)
            {
                _countText.enabled = false;
                _countText.text = "";
                return;
            }

            if (showOnlyWhenGreaterThanOne && stacks <= 1)
            {
                _countText.enabled = false;
                _countText.text = "";
            }
            else
            {
                _countText.enabled = true;
                _countText.text = stacks.ToString();
            }
        }
    }
}
