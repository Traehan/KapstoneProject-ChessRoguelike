using UnityEngine;
using Card;

namespace Chess
{
    public class PlayCardPlaceCommand : IGameCommand
    {
        readonly TurnManager _tm;
        readonly ChessBoard _board;
        readonly PlacementManager _placer;
        readonly DeckManager _deck;
        readonly Card.Card _card;
        readonly Vector2Int _coord;

        Piece _placedInstance;

        public PlayCardPlaceCommand(
            TurnManager tm,
            ChessBoard board,
            PlacementManager placer,
            DeckManager deck,
            Card.Card card,
            Vector2Int coord)
        {
            _tm = tm;
            _board = board;
            _placer = placer;
            _deck = deck;
            _card = card;
            _coord = coord;
        }

        public bool Execute()
        {
            if (_tm == null || _board == null || _placer == null || _deck == null || _card == null)
                return false;

            if (_tm.Phase != TurnPhase.SpellPhase)
                return false;

            if (!_board.InBounds(_coord))
                return false;

            if (!_deck.IsInHand(_card))
                return false;

            var summonDef = _card.GetSummonPieceDefinition();
            if (summonDef == null)
            {
                Debug.LogWarning("[PlayCardPlaceCommand] Tried to place a non-unit card.");
                return false;
            }

            if (!_tm.TrySpendMana(_card.ManaCost))
                return false;

            if (!_placer.TryPlace(summonDef, _coord))
            {
                _tm.RefundMana(_card.ManaCost);
                return false;
            }

            if (!_board.TryGetPiece(_coord, out _placedInstance) || _placedInstance == null)
            {
                _tm.RefundMana(_card.ManaCost);
                return false;
            }

            if (!_deck.RemoveFromHand(_card))
            {
                _board.TryRemovePieceAt(_coord);
                _tm.RefundMana(_card.ManaCost);
                return false;
            }

            GameEvents.OnCardRemovedFromHand?.Invoke(_card);

            _deck.MoveToPlayedThisBattle(_card);

            var report = new UnitCardPlayReport
            {
                card = _card,
                summonedDefinition = summonDef,
                spawnedPiece = _placedInstance,
                coord = _coord,
                ownerTeam = _placedInstance.Team,
                manaSpent = _card.ManaCost,
                resolved = true
            };

            GameEvents.OnCardPlayed?.Invoke(_card);
            GameEvents.RaiseUnitCardPlayed(_card, report);

            return true;
        }

        public void Undo()
        {
            if (_tm == null || _board == null || _deck == null || _card == null)
                return;

            if (_placedInstance != null)
            {
                if (!_board.TryRemovePieceAt(_placedInstance.Coord))
                    Object.Destroy(_placedInstance.gameObject);
            }
            else
            {
                _board.TryRemovePieceAt(_coord);
            }

            _tm.RefundMana(_card.ManaCost);
            _deck.RemoveFromPlayed(_card);
            _deck.ReturnToHand(_card);
            GameEvents.OnCardReturnedToHand?.Invoke(_card);
        }
    }
}