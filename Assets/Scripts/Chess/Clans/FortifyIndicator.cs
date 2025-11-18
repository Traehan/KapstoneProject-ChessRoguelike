using System.Collections.Generic;
using UnityEngine;

namespace Chess
{
    [DisallowMultipleComponent]
    public class FortifyIndicator : MonoBehaviour
    {
        [Header("Visuals")]
        [SerializeField] Sprite shieldSprite;     // your Aseprite sprite
        [SerializeField] int maxPips = 3;
        [SerializeField] float spacing = 0.14f;
        [SerializeField] float scale = 0.12f;
        [SerializeField] Vector3 localOffset = new Vector3(0f, 0.05f, 0f); // start with Z=0
        [SerializeField] bool billboardToCamera = true;
        [SerializeField] string sortingLayerName = "Pieces";
        [SerializeField] int sortingOrder = 20;

        Piece _piece;                     // found in parent
        readonly List<SpriteRenderer> _pips = new();

        void Awake()
        {
            _piece = GetComponentInParent<Piece>();
            if (_piece == null)
            {
                Debug.LogWarning("FortifyIndicator: No Piece found in parents. Place this on a child of a Piece.");
                enabled = false;
                return;
            }
            BuildPips();
        }

        void OnEnable()
        {
            // place the indicator child at the desired local offset relative to the piece
            transform.localPosition = localOffset;
            SyncToStacks();
        }

        void LateUpdate()
        {
            // keep indicator anchored in local space
            transform.localPosition = localOffset;

            // read current stacks and toggle pips
            SyncToStacks();

            if (billboardToCamera && Camera.main != null)
            {
                // make only the indicator face the camera (doesn't rotate the piece)
                transform.forward = Camera.main.transform.forward;
            }
        }

        void BuildPips()
        {
            foreach (var r in _pips) if (r) Destroy(r.gameObject);
            _pips.Clear();

            for (int i = 0; i < maxPips; i++)
            {
                var go = new GameObject($"FortifyPip_{i}");
                go.transform.SetParent(transform, false);
                var sr = go.AddComponent<SpriteRenderer>();
                sr.sprite = shieldSprite;
                sr.sortingLayerName = sortingLayerName;
                sr.sortingOrder = sortingOrder;
                sr.enabled = false;
                go.transform.localScale = Vector3.one * scale;
                _pips.Add(sr);
            }
        }

        void SyncToStacks()
        {
            if (_piece == null) return;

            int stacks = Mathf.Clamp(_piece.fortifyStacks, 0, maxPips);

            float totalWidth = (maxPips - 1) * spacing;
            for (int i = 0; i < _pips.Count; i++)
            {
                var sr = _pips[i];
                if (!sr) continue;

                bool on = i < stacks;
                sr.enabled = on;

                float x = (i * spacing) - (totalWidth * 0.5f);
                sr.transform.localPosition = new Vector3(x, 0f, 0f);
            }
        }
    }
}
