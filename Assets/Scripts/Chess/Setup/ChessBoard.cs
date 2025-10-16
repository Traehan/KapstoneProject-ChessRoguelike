// Assets/Scripts/Chess/ChessBoard.cs

using System.Collections.Generic;
using System.Linq;
using UnityEngine;

// for Team, Piece, Pawn

namespace Chess
{
    public class ChessBoard : MonoBehaviour
    {
    
        Dictionary<Vector2Int, Piece> _pieces = new Dictionary<Vector2Int, Piece>();
        [Header("Dimensions")]
        [Min(1)] public int columns = 8;
        [Min(1)] public int rows = 8;
        [Min(0.1f)] public float tileSize = 1f;
        public bool IsOccupied(Vector2Int c)
        {
            if (_pieces.TryGetValue(c, out var p))
            {
                if (p == null || p.Coord != c) { _pieces.Remove(c); return false; } // prune ghost/stale
                return true;
            }
            return false;
        }
        public Piece GetPiece(Vector2Int c)
        {
            if (_pieces.TryGetValue(c, out var p))
            {
                if (p == null || p.Coord != c) { _pieces.Remove(c); return null; } // prune ghost/stale
                return p;
            }
            return null;
        }

        [Header("Appearance")]
        public GameObject tilePrefab;
        public Color lightColor = new Color(0.9f, 0.9f, 0.9f);
        public Color darkColor  = new Color(0.2f, 0.2f, 0.2f);

        [Header("Behavior")]
        public bool autoBuildOnStart = true;
        public bool centerPivot = true; // center the board around this transform

        Tile[,] _tiles;
        Vector3 _origin; // bottom-left world position of the board

        void Start()
        {
            if (autoBuildOnStart) Rebuild();
        }

        [ContextMenu("Rebuild Board")]
        public void Rebuild()
        {
            if (tilePrefab == null) { Debug.LogError("Assign a Tile Prefab."); return; }

            // clear old children (tiles + any pieces under this transform)
            var children = new List<Transform>();
            foreach (Transform t in transform) children.Add(t);
            foreach (var t in children) DestroyImmediate(t.gameObject);

            // IMPORTANT: also clear occupancy map to remove “ghost” pieces
            _pieces.Clear();

            _tiles = new Tile[columns, rows];

            var sizeX = columns * tileSize;
            var sizeZ = rows    * tileSize;
            var offset = centerPivot ? new Vector3(-sizeX * 0.5f, 0f, -sizeZ * 0.5f) : Vector3.zero;
            _origin = transform.position + offset;

            for (int x = 0; x < columns; x++)
            for (int y = 0; y < rows; y++)
            {
                Vector3 pos = _origin + new Vector3((x + 0.5f) * tileSize, 0f, (y + 0.5f) * tileSize);
                var go = Instantiate(tilePrefab, pos, Quaternion.identity, transform);
                go.transform.localScale = new Vector3(tileSize, go.transform.localScale.y, tileSize);

                var tile = go.GetComponent<Tile>() ?? go.AddComponent<Tile>();
                bool isLight = ((x + y) & 1) == 0;
                tile.Init(new Vector2Int(x, y), isLight ? lightColor : darkColor);
                _tiles[x, y] = tile;
            }
        }

        // ======= Public API =======

        public static ChessBoard Spawn(ChessBoard boardPrefab, Vector3 position, Transform parent = null,
            int columns = 8, int rows = 8, float tileSize = 1f)
        {
            var board = Instantiate(boardPrefab, position, Quaternion.identity, parent);
            board.columns = columns;
            board.rows = rows;
            board.tileSize = tileSize;
            board.Rebuild();
            return board;
        }

        public Tile GetTile(Vector2Int c) => InBounds(c) ? _tiles[c.x, c.y] : null;

        public Vector3 BoardToWorldCenter(Vector2Int c)
        {
            return _origin + new Vector3((c.x + 0.5f) * tileSize, 0f, (c.y + 0.5f) * tileSize);
        }

        public bool WorldToBoard(Vector3 worldPos, out Vector2Int c)
        {
            var local = worldPos - _origin;
            int x = Mathf.FloorToInt(local.x / tileSize);
            int y = Mathf.FloorToInt(local.z / tileSize);
            c = new Vector2Int(x, y);
            return InBounds(c);
        }

        public void Highlight(IEnumerable<Vector2Int> coords, Color color)
        {
            foreach (var c in coords)
            {
                var t = GetTile(c);
                if (t != null) t.SetHighlight(true, color);
            }
        }

        public void ClearHighlights()
        {
            for (int x = 0; x < columns; x++)
            for (int y = 0; y < rows; y++)
                _tiles[x, y]?.SetHighlight(false, default);
        }

        void OnDrawGizmosSelected()
        {
            if (columns <= 0 || rows <= 0) return;

            var sizeX = columns * tileSize;
            var sizeZ = rows * tileSize;
            var offset = centerPivot ? new Vector3(-sizeX * 0.5f, 0f, -sizeZ * 0.5f) : Vector3.zero;
            var origin = transform.position + offset;

            Gizmos.color = Color.yellow;
            Gizmos.DrawWireCube(origin + new Vector3(sizeX * 0.5f, 0f, sizeZ * 0.5f),
                new Vector3(sizeX, 0.05f, sizeZ));
        }
    
        public Piece PlacePiece(Piece piecePrefab, Vector2Int c, Team team)
        {
            if (!InBounds(c)) { Debug.LogWarning($"Out of bounds {c}"); return null; }
            if (IsOccupied(c)) { Debug.LogWarning($"Tile {c} already occupied"); return null; }

            Vector3 world   = BoardToWorldCenter(c);
            Transform parent = gameObject.scene.IsValid() ? transform : null;

            var piece = Instantiate(piecePrefab, world, Quaternion.identity, parent);

            // Either all positional:
            piece.Init(this, team, c);
            // or all named:
            // piece.Init(board: this, team: team, start: c);

            _pieces[c] = piece;
            piece.OnPlaced();
            return piece;
        }


        public bool MovePiece(Piece piece, Vector2Int to)
        {
            if (piece == null) return false;

            var legal = new List<Vector2Int>();
            piece.GetLegalMoves(legal);
            if (!legal.Contains(to)) return false; //cancels if the 2d coord is not legal

            // capture if enemy there
            if (IsOccupied(to))
            {
                var enemy = GetPiece(to);
                if (enemy != null && enemy.Team != piece.Team)
                {
                    enemy.OnCaptured(); //will change to an encounter with health/damage calculations
                    _pieces.Remove(to);
                }
                else return false; // blocked by ally
            }

            // update map
            _pieces.Remove(piece.Coord);
            piece.transform.position = BoardToWorldCenter(to);
            typeof(Piece).GetProperty("Coord")
                .SetValue(piece, to, null); // set protected via reflection not ideal, but we’ll re-set below anyway

            // cleaner: re-init coord only
            var pawn = piece as Pawn;
            if (pawn != null && !pawn.hasMoved) pawn.MarkMoved();

            // force-set private via helper
            var setter = piece.GetType().BaseType
                .GetMethod("SetCoord", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic);
            setter.Invoke(piece, new object[] { to, false });

            _pieces[to] = piece;
            return true;
        }
    
        public void ClearPieces()
        {
            foreach (var kv in _pieces.ToList())
                if (kv.Value != null) DestroyImmediate(kv.Value.gameObject);
            _pieces.Clear();
        }
    
        public bool InBounds(Vector2Int c)
        {
            return c.x >= 0 && c.x < columns && c.y >= 0 && c.y < rows;
        }

        public IEnumerable<Piece> GetAllPieces() => _pieces.Values.Where(v => v != null);

        public bool TryGetPiece(Vector2Int c, out Piece p)
        {
            p = GetPiece(c);
            return p != null;
        }

        /// <summary>Non-destructive check that a piece currently belongs to the board.</summary>
        public bool ContainsPiece(Piece p)
        {
            return p != null && _pieces.TryGetValue(p.Coord, out var current) && current == p;
        }

        /// <summary>Move without captures; must also update occupancy. Return true if actually moved.</summary>
        public bool TryMovePiece(Piece piece, Vector2Int to)
        {
            if (piece == null || !InBounds(to)) return false;
            if (IsOccupied(to)) return false;

            // remove old entry (if any)
            if (_pieces.TryGetValue(piece.Coord, out var cur) && cur == piece)
                _pieces.Remove(piece.Coord);

            // snap + set piece coord
            piece.ApplyBoardMove(to);

            // add new entry
            _pieces[to] = piece;
            return true;
        }

        /// <summary>Remove a piece from board + occupancy and destroy its GO.</summary>
        public void RemovePiece(Piece piece)
        {
            if (piece == null) return;
            if (_pieces.TryGetValue(piece.Coord, out var cur) && cur == piece)
                _pieces.Remove(piece.Coord);
            Destroy(piece.gameObject);
        }
        
        public void RemovePiecesOfTeam(Chess.Team team)
        {
            foreach (var kv in _pieces.Where(kv => kv.Value != null && kv.Value.Team == team).ToList())
            {
                _pieces.Remove(kv.Key);
                if (kv.Value != null)
                {
                    if (Application.isPlaying) Destroy(kv.Value.gameObject);
                    else DestroyImmediate(kv.Value.gameObject);
                }
            }
        }
        
        public bool TryRemovePieceAt(Vector2Int c)
        {
            if (!InBounds(c)) return false;
            if (_pieces.TryGetValue(c, out var p))
            {
                _pieces.Remove(c);                       // keep occupancy correct
                if (p != null)
                {
                    if (Application.isPlaying) Destroy(p.gameObject);
                    else DestroyImmediate(p.gameObject);
                }
                return true;
            }
            return false;
        }

// Remove by instance + destroy (used elsewhere too)
        

        public bool HasPieceAt(Vector2Int c) => _pieces.ContainsKey(c);
        
        

    }
}
