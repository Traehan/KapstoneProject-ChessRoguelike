using System.Collections.Generic;
using TMPro;
using UnityEngine;

namespace Chess
{
    // Hover tooltip for owned-relic icons in the persistent relic bar. Mirrors CardInspectModal's
    // Instance/Awake/OnDestroy singleton pattern (isPrimaryInstance flag lets the PersistentUI copy
    // always reclaim Instance regardless of scene load order).
    public class RelicTooltipUI : MonoBehaviour
    {
        public static RelicTooltipUI Instance { get; private set; }

        [Header("Root")]
        [SerializeField] RectTransform rootPanel;

        [Header("Text")]
        [SerializeField] TMP_Text nameText;
        [SerializeField] TMP_Text descriptionText;

        [Header("Keyword Section")]
        [Tooltip("Parent that gets spawned KeywordToolTipRow instances. SetActive(false) entirely when no keywords are found.")]
        [SerializeField] RectTransform keywordContentRoot;
        [SerializeField] GameObject tooltipRowPrefab;
        [SerializeField] StatusDatabase statusDatabase;
        [Tooltip("Uniform scale applied to each spawned keyword row so the boxes read as a compact glossary, not full-size cards.")]
        [SerializeField] float keywordRowScale = 0.6f;

        [Header("Placement")]
        [Tooltip("Requires rootPanel's pivot to be top-left (0,1) - the panel then grows down-and-right from this offset point, i.e. to the bottom-right of the cursor.")]
        [SerializeField] Vector2 cursorOffset = new Vector2(14f, 14f);
        [SerializeField] Vector2 screenPadding = new Vector2(8f, 8f);

        [Header("Multi-scene Coexistence")]
        [Tooltip("Enable ONLY on the persistent-scene copy (PersistentUI) so it always reclaims Instance no matter " +
                 "what order scenes happen to load in this play session.")]
        [SerializeField] bool isPrimaryInstance = false;

        static readonly Color NameColor = Color.yellow;

        readonly List<GameObject> _spawnedRows = new();

        void Awake()
        {
            if (Instance == null || isPrimaryInstance)
                Instance = this;

            if (rootPanel != null)
                rootPanel.gameObject.SetActive(false);
        }

        void OnDestroy()
        {
            if (Instance == this)
                Instance = null;
        }

        public void Show(RelicSO relic)
        {
            if (relic == null || rootPanel == null)
                return;

            rootPanel.gameObject.SetActive(true);

            if (nameText != null)
            {
                nameText.text = string.IsNullOrEmpty(relic.displayName) ? relic.name : relic.displayName;
                nameText.color = NameColor;
            }

            if (descriptionText != null)
                descriptionText.text = relic.description;

            RebuildKeywords(relic);

            // Force layout to settle before we try to read the panel's size for on-screen clamping.
            Canvas.ForceUpdateCanvases();

            PositionAtCursor();
        }

        public void Hide()
        {
            if (rootPanel != null)
                rootPanel.gameObject.SetActive(false);
        }

        void RebuildKeywords(RelicSO relic)
        {
            ClearKeywordRows();

            if (keywordContentRoot == null)
                return;

            var text = (relic.displayName ?? "") + "\n" + (relic.description ?? "");
            var matches = KeywordGlossary.FindInText(text, statusDatabase);

            if (matches.Count == 0)
            {
                keywordContentRoot.gameObject.SetActive(false);
                return;
            }

            keywordContentRoot.gameObject.SetActive(true);

            foreach (var def in matches)
                SpawnRow(def);
        }

        void SpawnRow(StatusDefinition def)
        {
            if (keywordContentRoot == null || tooltipRowPrefab == null || def == null)
                return;

            var go = Instantiate(tooltipRowPrefab, keywordContentRoot);
            go.transform.localScale = Vector3.one * keywordRowScale;
            _spawnedRows.Add(go);

            var row = go.GetComponent<DeckKeywordTooltipRow>();
            if (row == null)
                row = go.AddComponent<DeckKeywordTooltipRow>();

            row.Bind(def);
        }

        void ClearKeywordRows()
        {
            for (int i = 0; i < _spawnedRows.Count; i++)
            {
                if (_spawnedRows[i] != null)
                    Destroy(_spawnedRows[i]);
            }

            _spawnedRows.Clear();
        }

        void PositionAtCursor()
        {
            // rootPanel lives under a screen-space-overlay Canvas, where a RectTransform's world
            // position maps 1:1 to screen pixels, same as Input.mousePosition. With pivot (0,1) -
            // top-left - the panel grows down-and-right from this point, landing to the bottom-right
            // of the cursor rather than centered on it.
            Vector3 cursor = Input.mousePosition;
            rootPanel.position = new Vector3(cursor.x + cursorOffset.x, cursor.y - cursorOffset.y, 0f);

            ClampOnScreen();
        }

        void ClampOnScreen()
        {
            var corners = new Vector3[4];
            rootPanel.GetWorldCorners(corners);

            float screenW = Screen.width;
            float screenH = Screen.height;

            float left = corners[0].x;
            float right = corners[2].x;
            float bottom = corners[0].y;
            float top = corners[1].y;

            Vector3 delta = Vector3.zero;

            if (left < screenPadding.x)
                delta.x += screenPadding.x - left;
            else if (right > screenW - screenPadding.x)
                delta.x -= right - (screenW - screenPadding.x);

            if (bottom < screenPadding.y)
                delta.y += screenPadding.y - bottom;
            else if (top > screenH - screenPadding.y)
                delta.y -= top - (screenH - screenPadding.y);

            if (delta != Vector3.zero)
                rootPanel.position += delta;
        }
    }
}
