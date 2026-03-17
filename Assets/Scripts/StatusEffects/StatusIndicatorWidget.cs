using System.Collections.Generic;
using UnityEngine;
using TMPro;
using UnityEngine.UI;

namespace Chess
{
    [DisallowMultipleComponent]
    public sealed class StatusIndicatorWidget : MonoBehaviour
    {
        [Header("Refs")]
        [SerializeField] StatusDatabase database;
        [SerializeField] Transform anchor; // StatusAnchor (child of the piece root)

        [Header("Prefabs")]
        [SerializeField] GameObject iconPrefab; // StatusIcon_World (can use Image OR SpriteRenderer)
        [SerializeField] GameObject chipPrefab; // StatusChip_World (can use TMP_Text)

        [Header("Layout")]
        [SerializeField] int expandedMaxIcons = 2;
        [SerializeField] float iconSpacing = 0.15f;
        [SerializeField] Vector3 localOffset = Vector3.zero;
        [SerializeField] float worldScale = 0.075f;
        [SerializeField] Vector2 iconSize = new Vector2(64f, 64f);
        [SerializeField] Vector2 countSize = new Vector2(24f, 24f);
        [SerializeField] float countFontSize = 14f;

        StatusController _status;

        readonly List<GameObject> _iconPool = new();
        GameObject _chipInstance;
        TMP_Text _chipText; // <-- IMPORTANT: works for TextMeshPro AND TextMeshProUGUI

        void Awake()
        {
            _status = GetComponent<StatusController>();
            if (anchor == null) anchor = transform;
        }

        void OnEnable()
        {
            if (_status != null)
                _status.OnStatusesChanged += HandleStatusesChanged;

            Refresh();
        }

        void OnDisable()
        {
            if (_status != null)
                _status.OnStatusesChanged -= HandleStatusesChanged;
        }

        void HandleStatusesChanged(StatusController sc) => Refresh();
        
        void ApplyWorldScale(GameObject go)
        {
            if (go == null || anchor == null) return;

            Vector3 a = anchor.lossyScale;
            float invX = Mathf.Approximately(a.x, 0f) ? 1f : 1f / a.x;
            float invY = Mathf.Approximately(a.y, 0f) ? 1f : 1f / a.y;
            float invZ = Mathf.Approximately(a.z, 0f) ? 1f : 1f / a.z;

            go.transform.localScale = new Vector3(invX, invY, invZ) * worldScale;
        }
        
        void NormalizeIconVisuals(GameObject go)
        {
            if (go == null) return;

            var img = go.transform.Find("StatusPip_Image");
            if (img != null)
            {
                var rt = img.GetComponent<RectTransform>();
                if (rt != null)
                    rt.sizeDelta = iconSize;

                var image = img.GetComponent<UnityEngine.UI.Image>();
                if (image != null)
                    image.preserveAspect = true;
            }

            var count = go.transform.Find("StatusCount");
            if (count != null)
            {
                var rt = count.GetComponent<RectTransform>();
                if (rt != null)
                    rt.sizeDelta = countSize;

                var tmp = count.GetComponent<TMPro.TMP_Text>();
                if (tmp != null)
                {
                    tmp.fontSize = countFontSize;
                    tmp.alignment = TMPro.TextAlignmentOptions.Center;
                }
            }
        }

        public void Refresh()
        {
            if (_status == null) _status = GetComponent<StatusController>();
            if (_status == null) return;

            var all = _status.GetAll();
            if (all.Count == 0)
            {
                SetAllInactive();
                return;
            }

            // sort by definition priority (higher first)
            all.Sort((a, b) =>
            {
                int pa = database != null ? (database.Get(a.id)?.priority ?? 0) : 0;
                int pb = database != null ? (database.Get(b.id)?.priority ?? 0) : 0;
                int cmp = pb.CompareTo(pa);
                return (cmp != 0) ? cmp : a.id.CompareTo(b.id);
            });
            
            Debug.Log($"[StatusWidget] {name} count = {all.Count}"); //just to check and make sure this works
            for (int i = 0; i < all.Count; i++)
                Debug.Log($"[StatusWidget] {name} has {all[i].id} x{all[i].stacks}");

            if (all.Count <= expandedMaxIcons)
                ShowExpanded(all);
            else
                ShowCollapsed(all.Count);
        }

        void SetAllInactive()
        {
            for (int i = 0; i < _iconPool.Count; i++)
                if (_iconPool[i] != null) _iconPool[i].SetActive(false);

            if (_chipInstance != null) _chipInstance.SetActive(false);
        }

        void ShowExpanded(List<StatusController.StatusEntry> statuses)
        {
            EnsureChip();
            _chipInstance.SetActive(false);

            EnsureIconPool(statuses.Count);

            float totalWidth = (statuses.Count - 1) * iconSpacing;
            float startX = -totalWidth * 0.5f;

            for (int i = 0; i < _iconPool.Count; i++)
            {
                var go = _iconPool[i];
                if (go == null) continue;

                if (i >= statuses.Count)
                {
                    go.SetActive(false);
                    continue;
                }

                var entry = statuses[i];
                var def = database != null ? database.Get(entry.id) : null;

                go.SetActive(true);
                go.transform.SetParent(anchor, false);
                go.transform.localPosition = localOffset + new Vector3(startX + i * iconSpacing, 0f, 0f);
                go.transform.localRotation = Quaternion.identity;
                ApplyWorldScale(go);
                NormalizeIconVisuals(go);

                // --- Icon: support Image (UI) OR SpriteRenderer (world)
                var sr = go.GetComponentInChildren<SpriteRenderer>(true);
                var img = go.GetComponentInChildren<Image>(true);

                if (def != null)
                {
                    if (img != null) img.sprite = def.icon;
                    else if (sr != null) sr.sprite = def.icon;
                }

                // --- Count text: TMP_Text supports TextMeshProUGUI + TextMeshPro
                var tmp = go.GetComponentInChildren<TMP_Text>(true);

                if (tmp != null)
                {
                    bool showStacks = def == null ? true : def.showStacks;
                    tmp.gameObject.SetActive(showStacks);
                    if (showStacks) tmp.text = entry.stacks.ToString();
                }
            }
        }

        void ShowCollapsed(int count)
        {
            for (int i = 0; i < _iconPool.Count; i++)
                if (_iconPool[i] != null) _iconPool[i].SetActive(false);

            EnsureChip();
            _chipInstance.SetActive(true);

            _chipInstance.transform.SetParent(anchor, false);
            _chipInstance.transform.localPosition = localOffset;
            _chipInstance.transform.localRotation = Quaternion.identity;
            ApplyWorldScale(_chipInstance);
            NormalizeIconVisuals(_chipInstance);

            if (_chipText != null) _chipText.text = $"+{count}";
        }

        void EnsureIconPool(int needed)
        {
            while (_iconPool.Count < needed)
            {
                var go = Instantiate(iconPrefab);
                go.SetActive(false);
                _iconPool.Add(go);
            }
        }

        void EnsureChip()
        {
            if (_chipInstance != null) return;
            _chipInstance = Instantiate(chipPrefab);
            _chipText = _chipInstance.GetComponentInChildren<TMP_Text>(true);
            _chipInstance.SetActive(false);
        }
    }
}