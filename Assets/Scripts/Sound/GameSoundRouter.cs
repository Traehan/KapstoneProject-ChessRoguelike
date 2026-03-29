using UnityEngine;
using Card;

namespace Chess
{
    [DisallowMultipleComponent]
    public class GameSoundRouter : MonoBehaviour
    {
        [Header("Music")]
        [SerializeField] SoundCueSO menuMusicCue;
        [SerializeField] SoundCueSO battleMusicCue;

        void OnEnable()
        {
            GameEvents.OnPieceMoved += HandlePieceMoved;
            GameEvents.OnAttackResolved += HandleAttackResolved;
            GameEvents.OnPieceCaptured += HandlePieceCaptured;
            GameEvents.OnSpellCastStarted += HandleSpellCastStarted;
            GameEvents.OnSpellResolved += HandleSpellResolved;
            GameEvents.OnSpellCastCancelled += HandleSpellCastCancelled;
            GameEvents.OnUnitCardPlayed += HandleUnitCardPlayed;
            GameEvents.OnCardDrawn += HandleCardDrawn;
            GameEvents.OnPhaseChanged += HandlePhaseChanged;

            GameEvents.OnStatusApplied += HandleStatusApplied;
            GameEvents.OnEncounterWon += HandleEncounterWon;
            GameEvents.OnEncounterLost += HandleEncounterLost;
            GameEvents.OnMusicRequested += HandleMusicRequested;
        }

        void OnDisable()
        {
            GameEvents.OnPieceMoved -= HandlePieceMoved;
            GameEvents.OnAttackResolved -= HandleAttackResolved;
            GameEvents.OnPieceCaptured -= HandlePieceCaptured;
            GameEvents.OnSpellCastStarted -= HandleSpellCastStarted;
            GameEvents.OnSpellResolved -= HandleSpellResolved;
            GameEvents.OnSpellCastCancelled -= HandleSpellCastCancelled;
            GameEvents.OnUnitCardPlayed -= HandleUnitCardPlayed;
            GameEvents.OnCardDrawn -= HandleCardDrawn;
            GameEvents.OnPhaseChanged -= HandlePhaseChanged;

            GameEvents.OnStatusApplied -= HandleStatusApplied;
            GameEvents.OnEncounterWon -= HandleEncounterWon;
            GameEvents.OnEncounterLost -= HandleEncounterLost;
            GameEvents.OnMusicRequested -= HandleMusicRequested;
        }

        void HandlePieceMoved(Piece piece, Vector2Int from, Vector2Int to, MoveReason reason)
        {
            if (piece == null || SoundManager.Instance == null)
                return;

            var profile = ResolvePieceProfile(piece);
            SoundManager.Instance.PlayAt(SoundEventId.PieceMove, piece.transform.position, profile);
        }

        void HandleAttackResolved(AttackReport r)
        {
            if (SoundManager.Instance == null)
                return;

            if (r.attacker != null)
            {
                var attackerProfile = ResolvePieceProfile(r.attacker);
                SoundManager.Instance.PlayAt(SoundEventId.PieceAttack, r.attacker.transform.position, attackerProfile);
            }

            if (r.defender != null)
            {
                var defenderProfile = ResolvePieceProfile(r.defender);
                SoundManager.Instance.PlayAt(SoundEventId.PieceHit, r.defender.transform.position, defenderProfile);
            }
        }

        void HandlePieceCaptured(Piece victim, Piece by, Vector2Int at)
        {
            if (victim == null || SoundManager.Instance == null)
                return;

            var profile = ResolvePieceProfile(victim);
            SoundManager.Instance.PlayAt(SoundEventId.PieceDeath, victim.transform.position, profile);
        }

        void HandleSpellCastStarted(Card.Card card)
        {
            if (card == null || SoundManager.Instance == null)
                return;

            var profile = ResolveCardProfile(card.Definition);
            SoundManager.Instance.PlayGlobal(SoundEventId.SpellCastStart, profile);
        }

        void HandleSpellResolved(Card.Card card, SpellCardPlayReport report)
        {
            if (card == null || SoundManager.Instance == null)
                return;

            var profile = ResolveCardProfile(card.Definition);

            if (report.targetPiece != null)
                SoundManager.Instance.PlayAt(SoundEventId.SpellCastResolve, report.targetPiece.transform.position, profile);
            else
                SoundManager.Instance.PlayGlobal(SoundEventId.SpellCastResolve, profile);
        }

        void HandleSpellCastCancelled(Card.Card card)
        {
            if (card == null || SoundManager.Instance == null)
                return;

            var profile = ResolveCardProfile(card.Definition);
            SoundManager.Instance.PlayGlobal(SoundEventId.SpellCastCancel, profile);
        }

        void HandleUnitCardPlayed(Card.Card card, UnitCardPlayReport report)
        {
            if (SoundManager.Instance == null)
                return;

            var profile = ResolveCardProfile(card != null ? card.Definition : null);

            if (report.spawnedPiece != null)
                SoundManager.Instance.PlayAt(SoundEventId.UnitCardPlayed, report.spawnedPiece.transform.position, profile);
            else
                SoundManager.Instance.PlayGlobal(SoundEventId.UnitCardPlayed, profile);
        }

        void HandleCardDrawn(Card.Card card)
        {
            if (SoundManager.Instance == null)
                return;

            var profile = ResolveCardProfile(card != null ? card.Definition : null);
            SoundManager.Instance.PlayGlobal(SoundEventId.CardDraw, profile);
        }

        void HandlePhaseChanged(TurnPhase phase)
        {
            if (SoundManager.Instance == null)
                return;

            switch (phase)
            {
                case TurnPhase.Preparation:
                    SoundManager.Instance.PlayGlobal(SoundEventId.PhasePreparation);
                    break;
                case TurnPhase.SpellPhase:
                    SoundManager.Instance.PlayGlobal(SoundEventId.PhaseSpell);
                    if (battleMusicCue != null)
                        SoundManager.Instance.PlayMusic(battleMusicCue);
                    break;
                case TurnPhase.PlayerTurn:
                    SoundManager.Instance.PlayGlobal(SoundEventId.PhasePlayerTurn);
                    break;
                case TurnPhase.EnemyTurn:
                    SoundManager.Instance.PlayGlobal(SoundEventId.PhaseEnemyTurn);
                    break;
            }
        }

        void HandleStatusApplied(StatusChangeReport report)
        {
            if (report.piece == null || SoundManager.Instance == null)
                return;

            var profile = ResolvePieceProfile(report.piece);

            switch (report.statusId)
            {
                case StatusId.Fortify:
                    SoundManager.Instance.PlayAt(SoundEventId.FortifyGain, report.piece.transform.position, profile);
                    break;

                case StatusId.Bleed:
                    SoundManager.Instance.PlayAt(SoundEventId.BleedApplied, report.piece.transform.position, profile);
                    break;
            }
        }

        void HandleEncounterWon()
        {
            if (SoundManager.Instance == null)
                return;

            SoundManager.Instance.PlayGlobal(SoundEventId.EncounterWin);
        }

        void HandleEncounterLost()
        {
            if (SoundManager.Instance == null)
                return;

            SoundManager.Instance.PlayGlobal(SoundEventId.EncounterLose);
        }

        void HandleMusicRequested(SoundEventId eventId)
        {
            if (SoundManager.Instance == null)
                return;

            switch (eventId)
            {
                case SoundEventId.MusicMenu:
                    if (menuMusicCue != null)
                        SoundManager.Instance.PlayMusic(menuMusicCue);
                    break;

                case SoundEventId.MusicBattle:
                    if (battleMusicCue != null)
                        SoundManager.Instance.PlayMusic(battleMusicCue);
                    break;
            }
        }

        SoundProfileSO ResolvePieceProfile(Piece piece)
        {
            if (piece == null || piece.Definition == null)
                return null;

            return piece.Definition.soundProfile;
        }

        SoundProfileSO ResolveCardProfile(CardDefinitionSO definition)
        {
            if (definition == null)
                return null;

            return definition.soundProfile;
        }
    }
}