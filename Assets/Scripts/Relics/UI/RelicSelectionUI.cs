using System;
using System.Collections.Generic;
using UnityEngine;

namespace Chess
{
    // The "beginning of the run" relic-pick window: shows N random common relics
    // (pooled from general relics + the chosen clan's exclusive relics), lets the
    // player pick one, and shows what's owned so far in a bar at the top.
    public class RelicSelectionUI : MonoBehaviour
    {
        [Header("Window")]
        public GameObject window;

        [Header("Options")]
        public Transform optionContainer;
        public RelicOptionCardUI optionCardPrefab;
        [Min(1)] public int optionCount = 2;
        public RelicRarity rarityToOffer = RelicRarity.Common;

        [Header("Owned Relics Bar")]
        public Transform ownedRelicsContainer;
        public RelicIconUI ownedRelicIconPrefab;

        readonly List<RelicOptionCardUI> _spawnedOptions = new();
        Action _onComplete;

        public void Show(ClanDefinition clan, Action onComplete)
        {
            _onComplete = onComplete;

            if (window != null) window.SetActive(true);
            RefreshOwnedRelicsBar();
            PopulateOptions(clan);
        }

        void PopulateOptions(ClanDefinition clan)
        {
            ClearOptions();

            var database = GameSession.I != null ? GameSession.I.relicDatabase : null;
            if (database == null || optionContainer == null || optionCardPrefab == null)
            {
                Debug.LogError("[RelicSelectionUI] Missing relicDatabase or UI references.");
                return;
            }

            var choices = database.DrawRandom(rarityToOffer, clan, optionCount);
            foreach (var relic in choices)
            {
                var card = Instantiate(optionCardPrefab, optionContainer);
                card.Bind(relic, OnRelicPicked);
                _spawnedOptions.Add(card);
            }
        }

        void OnRelicPicked(RelicSO relic)
        {
            if (GameSession.I == null || relic == null) return;

            GameSession.I.AddRelic(relic);
            RefreshOwnedRelicsBar();
            ClearOptions();

            if (window != null) window.SetActive(false);

            var callback = _onComplete;
            _onComplete = null;
            callback?.Invoke();
        }

        void RefreshOwnedRelicsBar()
        {
            if (ownedRelicsContainer == null || ownedRelicIconPrefab == null || GameSession.I == null)
                return;

            for (int i = ownedRelicsContainer.childCount - 1; i >= 0; i--)
                Destroy(ownedRelicsContainer.GetChild(i).gameObject);

            foreach (var relic in GameSession.I.ownedRelics)
            {
                var icon = Instantiate(ownedRelicIconPrefab, ownedRelicsContainer);
                icon.Bind(relic);
            }
        }

        void ClearOptions()
        {
            foreach (var card in _spawnedOptions)
                if (card != null) Destroy(card.gameObject);
            _spawnedOptions.Clear();
        }
    }
}
