// Assets/Scripts/Chess/Setup/GauntletScreen.cs
//
// Pre-battle "accept the Gauntlet?" screen, shown before PrepPanel becomes the active flow when the
// player lands on a map Encounter node flagged as a Gauntlet (GameSession.pendingGauntletAvailable).
// Mirrors the StartTroopPopup/PrepPanel precedent: a MonoBehaviour with a `panel` GameObject it
// activates, reading GameSession state, with a guard so it only ever acts once per battle scene load.
//
// EncounterRunner has [DefaultExecutionOrder(-5)], so by the time this script's own Start() runs,
// EncounterRunner.Start() has already resolved the encounter (field / SceneArgs.Payload /
// GameSession.selectedEncounter) into its PendingEncounter property - this script never needs to
// duplicate that resolution logic.
//
// Whether or not a Gauntlet is pending, this screen is the single entry point that calls
// EncounterRunner.StartWith(...) - EncounterRunner's own runOnStart must be set to false in the
// Inspector wherever this screen is present, or the encounter will start running before the player
// has had a chance to accept/decline.

using UnityEngine;
using UnityEngine.UI;
using TMPro;
using Chess;

public class GauntletScreen : MonoBehaviour
{
    [Header("Refs")]
    [SerializeField] EncounterRunner encounterRunner;
    [Tooltip("PrepPanel's root GameObject (or whatever should stay hidden until the Gauntlet is resolved). Optional - leave empty if PrepPanel should just stay active as normal.")]
    [SerializeField] GameObject prepPanelRoot;

    [Header("Panel")]
    [SerializeField] GameObject panel;

    [Header("Enemy Preview")]
    [Tooltip("Container the preview icons are instantiated into - cleared and rebuilt each time this screen shows.")]
    [SerializeField] Transform enemyPreviewContainer;
    [Tooltip("Simple prefab with an Image component, one instantiated per enemy SpawnSpec across the encounter's authored waves.")]
    [SerializeField] Image enemyIconPrefab;

    [Header("Accept / Decline")]
    [SerializeField] Toggle acceptToggle;
    [SerializeField] TMP_Text challengeText;
    [SerializeField] TMP_Text rewardText;
    [SerializeField] Button startEncounterButton;

    [Header("Badge Icons (placeholder until real art exists)")]
    [Tooltip("Indexed by GauntletRewardType (Gold, QueenMove, Relic).")]
    [SerializeField] Sprite[] rewardIconSprites = new Sprite[3];
    [Tooltip("Indexed by GauntletChallengeType (StatBoost, Swarm, HarderEnemy, ManaHandicap, EnergyHandicap).")]
    [SerializeField] Sprite[] challengeIconSprites = new Sprite[5];
    [SerializeField] Image rewardIconImage;
    [SerializeField] Image challengeIconImage;

    EncounterDefinition _resolvedEncounter;
    bool _isGauntlet;

    void Awake()
    {
        // Decide + hide PrepPanel (if assigned) as early as possible - Awake() for every object in the
        // scene completes before any Start() runs, so this reliably beats PrepPanel.Start() regardless
        // of script execution order between the two default-priority scripts.
        var gs = GameSession.I;
        _isGauntlet = gs != null && gs.pendingGauntletAvailable;

        if (panel != null)
            panel.SetActive(_isGauntlet);

        if (_isGauntlet && prepPanelRoot != null)
            prepPanelRoot.SetActive(false);
    }

    void Start()
    {
        _resolvedEncounter = encounterRunner != null ? encounterRunner.PendingEncounter : null;

        if (!_isGauntlet)
        {
            // Normal fight, no Gauntlet screen needed - just kick off the encounter exactly like
            // EncounterRunner.Start() used to when runOnStart was true.
            if (encounterRunner != null)
                encounterRunner.StartWith(_resolvedEncounter);
            return;
        }

        var gs = GameSession.I;

        PopulateEnemyPreview(_resolvedEncounter);
        PopulateChallengeRewardText(gs.pendingGauntletChallenge, gs.pendingGauntletReward);

        if (acceptToggle != null)
            acceptToggle.isOn = false;

        if (startEncounterButton != null)
        {
            startEncounterButton.onClick.RemoveListener(OnStartEncounterClicked);
            startEncounterButton.onClick.AddListener(OnStartEncounterClicked);
        }
    }

    void OnStartEncounterClicked()
    {
        var gs = GameSession.I;
        if (gs != null)
            gs.gauntletAccepted = acceptToggle != null && acceptToggle.isOn;

        if (panel != null)
            panel.SetActive(false);

        if (prepPanelRoot != null)
            prepPanelRoot.SetActive(true);

        if (encounterRunner != null)
            encounterRunner.StartWith(_resolvedEncounter);
    }

    void PopulateEnemyPreview(EncounterDefinition def)
    {
        if (enemyPreviewContainer == null || enemyIconPrefab == null)
            return;

        for (int i = enemyPreviewContainer.childCount - 1; i >= 0; i--)
            Destroy(enemyPreviewContainer.GetChild(i).gameObject);

        if (def == null || def.waves == null)
            return;

        foreach (var wave in def.waves)
        {
            if (wave == null || wave.spawns == null) continue;

            foreach (var spec in wave.spawns)
            {
                if (spec.piece == null) continue;

                var icon = Instantiate(enemyIconPrefab, enemyPreviewContainer);
                icon.sprite = spec.piece.icon;
                icon.gameObject.SetActive(true);
            }
        }
    }

    void PopulateChallengeRewardText(GauntletChallengeType challenge, GauntletRewardType reward)
    {
        if (rewardIconImage != null)
            rewardIconImage.sprite = GetSpriteForIndex(rewardIconSprites, (int)reward);

        if (challengeIconImage != null)
            challengeIconImage.sprite = GetSpriteForIndex(challengeIconSprites, (int)challenge);

        if (challengeText != null)
            challengeText.text = DescribeChallenge(challenge);

        if (rewardText != null)
            rewardText.text = DescribeReward(reward);
    }

    Sprite GetSpriteForIndex(Sprite[] sprites, int index)
    {
        if (sprites == null || index < 0 || index >= sprites.Length)
            return null;
        return sprites[index];
    }

    string DescribeChallenge(GauntletChallengeType challenge)
    {
        float boost = encounterRunner != null ? encounterRunner.GauntletStatBoostMultiplier : 1.25f;

        switch (challenge)
        {
            case GauntletChallengeType.StatBoost:
                return $"Challenge - Stat Boost: every enemy in this fight has HP and Attack multiplied by {boost:0.##}x.";
            case GauntletChallengeType.Swarm:
                return "Challenge - Swarm: two extra waves of reinforcements arrive, a round apart, after the normal fight would have ended.";
            case GauntletChallengeType.HarderEnemy:
                return "Challenge - Harder Enemy: a tougher reinforcement wave arrives shortly after the normal fight would have ended.";
            case GauntletChallengeType.ManaHandicap:
                return "Challenge - Mana Handicap: you have 1 less Mana per Spell Phase for this fight.";
            case GauntletChallengeType.EnergyHandicap:
                return "Challenge - Energy Handicap: you have 1 less Action Point per turn for this fight.";
            default:
                return string.Empty;
        }
    }

    string DescribeReward(GauntletRewardType reward)
    {
        int gold = GameSession.I != null ? GameSession.I.gauntletGoldReward : 150;

        switch (reward)
        {
            case GauntletRewardType.Gold:
                return $"Reward - Gold: +{gold} gold on victory.";
            case GauntletRewardType.QueenMove:
                return "Reward - Queen Move: +1 Queen map movement on victory.";
            case GauntletRewardType.Relic:
                return "Reward - Relic: a bonus relic (skewed toward Uncommon/Rare) on victory.";
            default:
                return string.Empty;
        }
    }
}
