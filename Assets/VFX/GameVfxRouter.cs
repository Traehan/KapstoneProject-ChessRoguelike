using UnityEngine;
using Card;

namespace Chess
{
    [DisallowMultipleComponent]
    public class GameVfxRouter : MonoBehaviour
    {
        [Header("Refs")]
        [SerializeField] ChessBoard board;

        [Header("Spawn VFX")]
        [SerializeField] Vector3 spawnOffset = new Vector3(0f, 0.05f, 0f);
        [SerializeField, Min(0f)] float spawnLifetime = 2f;
        
        [Header("Enemy Spawn Override")]
        [SerializeField] GameObject enemySpawnPrefab;

        [Header("Boost / Spell VFX")]
        [SerializeField] Vector3 boostOffset = new Vector3(0f, 0.15f, 0f);
        [SerializeField, Min(0f)] float boostLifetime = 1.5f;

        [Header("Optional Parents")]
        [SerializeField] Transform spawnVfxParent;
        [SerializeField] Transform boostVfxParent;

        void Awake()
        {
            if (board == null)
                board = FindObjectOfType<ChessBoard>();
        }

        void OnEnable()
        {
            GameEvents.OnPieceSpawned += HandlePieceSpawned;
            GameEvents.OnStatusApplied += HandleStatusApplied;
            GameEvents.OnPositiveStatChanged += HandlePositiveStatChanged;
            GameEvents.OnSpellResolved += HandleSpellResolved;
        }

        void OnDisable()
        {
            GameEvents.OnPieceSpawned -= HandlePieceSpawned;
            GameEvents.OnStatusApplied -= HandleStatusApplied;
            GameEvents.OnPositiveStatChanged -= HandlePositiveStatChanged;
            GameEvents.OnSpellResolved -= HandleSpellResolved;
        }

        void HandlePieceSpawned(Piece piece, Vector2Int at)
        {
            if (piece == null)
                return;

            GameObject prefab = ResolveSpawnPrefab(piece);
            if (prefab == null)
                return;

            SpawnPrefab(prefab, piece.transform.position + spawnOffset, spawnLifetime, spawnVfxParent);
        }

        void HandleStatusApplied(StatusChangeReport report)
        {
            if (report.piece == null)
                return;

            if (report.amountChanged <= 0)
                return;

            // For now, only use this for positive status gains like Fortify.
            // If you later want Bleed to NOT trigger this, just keep Fortify-only logic here.
            if (report.statusId == StatusId.Fortify)
            {
                SpawnBoostAtPiece(report.piece);
            }
        }

        void HandlePositiveStatChanged(Piece piece)
        {
            if (piece == null)
                return;

            SpawnBoostAtPiece(piece);
        }

        void HandleSpellResolved(Card.Card card, SpellCardPlayReport report)
        {
            GameObject prefab = ResolveBoostPrefab();
            if (prefab == null)
                return;

            // Best target first
            if (report.targetPiece != null)
            {
                SpawnPrefab(prefab, report.targetPiece.transform.position + boostOffset, boostLifetime, boostVfxParent);
                return;
            }

            // If spell had a board coordinate target
            if (board != null && report.targetCoord != default)
            {
                Vector3 world = board.BoardToWorldCenter(report.targetCoord);
                SpawnPrefab(prefab, world + boostOffset, boostLifetime, boostVfxParent);
                return;
            }

            // Fallback: center of board
            if (board != null)
            {
                Vector3 center = board.BoardToWorldCenter(new Vector2Int(board.columns / 2, board.rows / 2));
                SpawnPrefab(prefab, center + boostOffset, boostLifetime, boostVfxParent);
            }
        }

        void SpawnBoostAtPiece(Piece piece)
        {
            GameObject prefab = ResolveBoostPrefab();
            if (prefab == null || piece == null)
                return;

            SpawnPrefab(prefab, piece.transform.position + boostOffset, boostLifetime, boostVfxParent);
        }

        GameObject ResolveSpawnPrefab(Piece piece)
        {
            if (piece != null && piece.Team == Team.Black)
                return enemySpawnPrefab;

            ClanDefinition clan = GameSession.I != null ? GameSession.I.selectedClan : null;
            if (clan == null)
                return null;

            return clan.spawnRingPrefab;
        }

        GameObject ResolveBoostPrefab()
        {
            ClanDefinition clan = GameSession.I != null ? GameSession.I.selectedClan : null;
            if (clan == null)
                return null;

            return clan.boostSplashPrefab;
        }

        void SpawnPrefab(GameObject prefab, Vector3 position, float lifetime, Transform parent)
        {
            if (prefab == null)
                return;

            GameObject instance = Instantiate(prefab, position, Quaternion.identity, parent);

            if (lifetime > 0f)
                Destroy(instance, lifetime);
        }
    }
}