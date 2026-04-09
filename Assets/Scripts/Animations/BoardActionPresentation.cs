using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Chess
{
    [DisallowMultipleComponent]
    public class BoardActionPresentation : MonoBehaviour
    {
        [Header("Refs")]
        [SerializeField] ChessBoard board;

        [Header("Fallback Impact VFX")]
        [SerializeField] GameObject hitImpactPrefab;
        [SerializeField, Min(0f)] float impactLifetime = 1.5f;
        [SerializeField] Vector3 impactOffset = new Vector3(0f, 0.15f, 0f);
        [SerializeField] Transform impactParent;

        [Header("Impact Travel")]
        [SerializeField, Min(0.01f)] float impactTravelDuration = 0.08f;
        [SerializeField] AnimationCurve impactTravelEase = AnimationCurve.EaseInOut(0f, 0f, 1f, 1f);
        [SerializeField, Min(0f)] float defenderImpactStopOffset = 0.05f;

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
                Piece attackerPiece = piece;
                motion.PlayAttackKillAdvance(
                    worldFrom,
                    worldTo,
                    worldTo,
                    hitPoint => SpawnImpactToward(attackerPiece, hitPoint, worldTo));
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

            Vector3 attackerWorld = board.BoardToWorldCenter(r.attacker.Coord);
            Vector3 defenderWorld = board.BoardToWorldCenter(r.defender.Coord);

            var attackerMotion = r.attacker.GetComponent<PieceMotionController>();
            var defenderMotion = r.defender.GetComponent<PieceMotionController>();

            // SPECIAL CASE: boss ray attacks should launch impact VFX directly
            // instead of using attacker bump, because the boss can attack many
            // targets in one turn and repeated bumps cancel each other.
            if (r.isBossAttack)
            {
                SpawnImpactToward(r.attacker, attackerWorld, defenderWorld);

                if (!r.defenderDied && defenderMotion != null)
                {
                    Vector3 recoilDir = defenderWorld - attackerWorld;
                    recoilDir.y = 0f;

                    if (recoilDir.sqrMagnitude > 0.0001f)
                        recoilDir.Normalize();
                    else
                        recoilDir = Vector3.zero;

                    defenderMotion.PlayHitRecoil(defenderWorld, recoilDir, returnToOrigin: true);
                }

                return;
            }

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
            }
            else if (attackerMotion != null)
            {
                Piece attackerPiece = r.attacker;
                attackerMotion.PlayAttackBump(
                    attackerWorld,
                    defenderWorld,
                    hitPoint => SpawnImpactToward(attackerPiece, hitPoint, defenderWorld));
            }

            if (r.defenderDied)
                return;

            if (defenderMotion != null)
            {
                Vector3 recoilDir = defenderWorld - attackerWorld;
                recoilDir.y = 0f;

                if (recoilDir.sqrMagnitude > 0.0001f)
                    recoilDir.Normalize();
                else
                    recoilDir = Vector3.zero;

                defenderMotion.PlayHitRecoil(defenderWorld, recoilDir, returnToOrigin: true);
            }
        }

        void SpawnImpactToward(Piece attacker, Vector3 hitPoint, Vector3 defenderWorld)
        {
            GameObject impactPrefab = ResolveImpactPrefab(attacker);
            if (impactPrefab == null)
                return;

            Vector3 spawnPos = hitPoint + impactOffset;
            Vector3 targetPos = defenderWorld + impactOffset;

            Vector3 travelDir = targetPos - spawnPos;
            travelDir.y = 0f;

            if (travelDir.sqrMagnitude > 0.0001f)
            {
                travelDir.Normalize();
                targetPos -= travelDir * defenderImpactStopOffset;
            }

            Transform parent = impactParent != null ? impactParent : null;
            var instance = Instantiate(impactPrefab, spawnPos, Quaternion.identity, parent);

            StartCoroutine(AnimateImpactTravel(instance.transform, spawnPos, targetPos));

            if (impactLifetime > 0f)
                Destroy(instance, impactLifetime);
        }

        GameObject ResolveImpactPrefab(Piece attacker)
        {
            if (attacker != null && attacker.Definition != null && attacker.Definition.attackImpactPrefab != null)
                return attacker.Definition.attackImpactPrefab;

            return hitImpactPrefab;
        }

        IEnumerator AnimateImpactTravel(Transform impactTransform, Vector3 from, Vector3 to)
        {
            if (impactTransform == null)
                yield break;

            if (impactTravelDuration <= 0f)
            {
                impactTransform.position = to;
                yield break;
            }

            float t = 0f;

            while (t < impactTravelDuration)
            {
                if (impactTransform == null)
                    yield break;

                t += Time.deltaTime;
                float u = Mathf.Clamp01(t / impactTravelDuration);
                float eased = impactTravelEase != null ? impactTravelEase.Evaluate(u) : u;
                impactTransform.position = Vector3.LerpUnclamped(from, to, eased);
                yield return null;
            }

            if (impactTransform != null)
                impactTransform.position = to;
        }
    }
}