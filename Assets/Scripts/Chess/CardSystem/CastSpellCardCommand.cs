using System.Collections.Generic;
using UnityEngine;
using Card;

namespace Chess
{
    public class CastSpellCardCommand : IGameCommand
    {
        readonly TurnManager _tm;
        readonly ChessBoard _board;
        readonly DeckManager _deck;
        readonly Card.Card _card;
        readonly object _target;
        readonly Team _casterTeam;

        SpellCardDefinitionSO _spellDef;
        SpellContext _context;
        readonly List<SpellEffectSO> _resolvedEffects = new();

        bool _removedFromHand;
        bool _movedToPlayed;
        bool _movedToDiscard;
        bool _spentMana;
        bool _resolved;
        bool _exhausted;

        public CastSpellCardCommand(
            TurnManager tm,
            ChessBoard board,
            DeckManager deck,
            Card.Card card,
            Team casterTeam,
            object target = null)
        {
            _tm = tm;
            _board = board;
            _deck = deck;
            _card = card;
            _casterTeam = casterTeam;
            _target = target;
        }

        public bool Execute()
        {
            if (_tm == null || _board == null || _deck == null || _card == null)
                return false;

            if (_tm.Phase != TurnPhase.SpellPhase)
                return false;

            if (!_deck.IsInHand(_card))
                return false;

            if (!_card.IsSpellCard())
                return false;

            _spellDef = _card.Definition as SpellCardDefinitionSO;
            if (_spellDef == null)
            {
                Debug.LogWarning("[CastSpellCardCommand] Card is marked as spell but has no SpellCardDefinitionSO.");
                return false;
            }

            if (_spellDef.effects == null || _spellDef.effects.Length == 0)
            {
                Debug.LogWarning("[CastSpellCardCommand] Spell has no effects assigned.");
                return false;
            }

            GameEvents.OnSpellCastStarted?.Invoke(_card);

            if (!_tm.TrySpendMana(_card.ManaCost))
                return false;

            _spentMana = true;

            if (_target != null)
                GameEvents.OnSpellTargetSelected?.Invoke(_card, _target);

            if (!_deck.RemoveFromHand(_card))
            {
                RollbackMana();
                GameEvents.OnSpellCastCancelled?.Invoke(_card);
                return false;
            }

            _removedFromHand = true;
            GameEvents.OnCardRemovedFromHand?.Invoke(_card);

            _deck.MoveToPlayedThisBattle(_card);
            _movedToPlayed = true;

            _context = new SpellContext(
                _tm,
                _board,
                _deck,
                _card,
                _spellDef,
                _casterTeam,
                _target
            );

            foreach (var effect in _spellDef.effects)
            {
                if (effect == null) continue;

                bool ok = effect.Resolve(_context);
                if (!ok)
                {
                    RollbackResolvedEffects();
                    RollbackCardMovement();
                    RollbackMana();

                    GameEvents.OnSpellCastCancelled?.Invoke(_card);
                    return false;
                }

                _resolvedEffects.Add(effect);
            }

            if (_spellDef.exhaustOnCast)
            {
                _exhausted = true;
                GameEvents.OnCardExhausted?.Invoke(_card);
            }
            else if (_spellDef.discardOnCast)
            {
                if (!_deck.Discard.Contains(_card))
                    _deck.Discard.Add(_card);

                _movedToDiscard = true;
                GameEvents.OnCardDiscarded?.Invoke(_card);
            }

            var report = BuildReport();

            GameEvents.OnCardPlayed?.Invoke(_card);
            GameEvents.RaiseSpellCardPlayed(_card, report);
            GameEvents.OnSpellResolved?.Invoke(_card, report);

            _resolved = true;
            return true;
        }

        public void Undo()
        {
            if (!_resolved)
                return;

            for (int i = _resolvedEffects.Count - 1; i >= 0; i--)
            {
                var effect = _resolvedEffects[i];
                if (effect == null) continue;
                effect.Undo(_context);
            }

            if (_movedToDiscard)
            {
                _deck.Discard.Remove(_card);
                _movedToDiscard = false;
            }

            if (_movedToPlayed)
            {
                _deck.RemoveFromPlayed(_card);
                _movedToPlayed = false;
            }

            if (_removedFromHand)
            {
                _deck.ReturnToHand(_card);
                GameEvents.OnCardReturnedToHand?.Invoke(_card);
                _removedFromHand = false;
            }

            if (_spentMana)
            {
                _tm.RefundMana(_card.ManaCost);
                _spentMana = false;
            }

            _resolvedEffects.Clear();
            _exhausted = false;
            _resolved = false;
        }

        SpellCardPlayReport BuildReport()
        {
            var report = new SpellCardPlayReport
            {
                card = _card,
                ownerTeam = _casterTeam,
                manaSpent = _card.ManaCost,
                resolved = true,
                targetPiece = ExtractTargetPiece(_target),
                targetCoord = ExtractTargetCoord(_target)
            };

            return report;
        }

        Piece ExtractTargetPiece(object target)
        {
            if (target is Piece piece)
                return piece;

            return null;
        }

        Vector2Int ExtractTargetCoord(object target)
        {
            if (target is Vector2Int coord)
                return coord;

            if (target is Piece piece)
                return piece.Coord;

            // For wrapped multi-step targets like FortifyShiftTarget / TransferFortifyTarget /
            // PhalanxRotateTarget, leave default unless the effect itself needs more detail.
            return default;
        }

        void RollbackResolvedEffects()
        {
            for (int i = _resolvedEffects.Count - 1; i >= 0; i--)
            {
                var effect = _resolvedEffects[i];
                if (effect == null) continue;
                effect.Undo(_context);
            }

            _resolvedEffects.Clear();
        }

        void RollbackCardMovement()
        {
            if (_movedToDiscard)
            {
                _deck.Discard.Remove(_card);
                _movedToDiscard = false;
            }

            if (_movedToPlayed)
            {
                _deck.RemoveFromPlayed(_card);
                _movedToPlayed = false;
            }

            if (_removedFromHand)
            {
                _deck.ReturnToHand(_card);
                GameEvents.OnCardReturnedToHand?.Invoke(_card);
                _removedFromHand = false;
            }
        }

        void RollbackMana()
        {
            if (_spentMana)
            {
                _tm.RefundMana(_card.ManaCost);
                _spentMana = false;
            }
        }
    }
}