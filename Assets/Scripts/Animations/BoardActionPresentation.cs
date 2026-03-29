using System.Collections.Generic;
using UnityEngine;

namespace Chess
{
    [DisallowMultipleComponent]
    public class BoardActionPresentation : MonoBehaviour
    {
        [Header("Refs")]
        [SerializeField] ChessBoard board;

        [Header("Impact VFX")]
        [SerializeField] GameObject hitImpactPrefab;
        [SerializeField, Min(0f)] float impactLifetime = 1.5f;
        [SerializeField] Vector3 impactOffset = new Vector3(0f, 0.15f, 0f);
        [SerializeField] Transform impactParent;

        readonly HashSet<Piece> _pendingKillAdvance = new();

        void Awake()
        {
            if (board == null)
                board = FindObjectOfType<ChessBoard>();
        }

        void OnEnable()
        {
            GameEvents.OnPieceMoved += HandlePieceMoved;
            GameEvents.OnAttackResolved += HandleAttackResolved;
        }

        void OnDisable()
        {
            GameEvents.OnPieceMoved -= HandlePieceMoved;
            GameEvents.OnAttackResolved -= HandleAttackResolved;
        }

        void HandlePieceMoved(Piece piece, Vector2Int from, Vector2Int to, MoveReason reason)
        {
            if (piece == null || board == null)
                return;

            if (from == to)
                return;

            var motion = piece.GetComponent<PieceMotionController>();
            if (motion == null)
                return;

            Vector3 worldFrom = board.BoardToWorldCenter(from);
            Vector3 worldTo = board.BoardToWorldCenter(to);

            if (_pendingKillAdvance.Remove(piece))
            {
                motion.PlayAttackKillAdvance(worldFrom, worldTo, worldTo, SpawnImpactAt);
                return;
            }

            motion.PlayBoardSlide(worldFrom, worldTo, board.tileSize);
        }

        void HandleAttackResolved(AttackReport r)
        {
            if (board == null)
                return;

            if (r.attacker == null || r.defender == null)
                return;

            var motion = r.attacker.GetComponent<PieceMotionController>();
            if (motion == null)
                return;

            Vector3 attackerWorld = board.BoardToWorldCenter(r.attacker.Coord);
            Vector3 defenderWorld = board.BoardToWorldCenter(r.defender.Coord);

            bool attackerIsEnemy = false;
            if (TurnManager.Instance != null)
                attackerIsEnemy = r.attacker.Team != TurnManager.Instance.PlayerTeam;

            bool enemyKillAdvance =
                attackerIsEnemy &&
                !r.attackerDied &&
                r.defenderDied;

            if (enemyKillAdvance)
            {
                _pendingKillAdvance.Add(r.attacker);
                return;
            }

            motion.PlayAttackBump(attackerWorld, defenderWorld, SpawnImpactAt);
        }

        void SpawnImpactAt(Vector3 worldPos)
        {
            if (hitImpactPrefab == null)
                return;

            Vector3 spawnPos = worldPos + impactOffset;
            Transform parent = impactParent != null ? impactParent : null;

            var instance = Instantiate(hitImpactPrefab, spawnPos, Quaternion.identity, parent);

            if (impactLifetime > 0f)
                Destroy(instance, impactLifetime);
        }
    }
}