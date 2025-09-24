// Assets/Scripts/Chess/Tile.cs
using UnityEngine;

[RequireComponent(typeof(Renderer), typeof(Collider))]
public class Tile : MonoBehaviour
{
    public Vector2Int Coord { get; private set; }
    Renderer _renderer;
    Color _baseColor;

    void Awake()
    {
        _renderer = GetComponent<Renderer>();
    }

    public void Init(Vector2Int coord, Color baseColor)
    {
        Coord = coord;
        _baseColor = baseColor;
        SetColor(baseColor);
        name = CoordToName(coord);
    }

    public void SetColor(Color c)
    {
        if (_renderer == null) _renderer = GetComponent<Renderer>();

        var mpb = new MaterialPropertyBlock();
        _renderer.GetPropertyBlock(mpb);

        // Built-in/Standard:
        mpb.SetColor("_Color", c);
        // URP/HDRP Lit:
        mpb.SetColor("_BaseColor", c);

        _renderer.SetPropertyBlock(mpb);
    }


    public void SetHighlight(bool on, Color highlight)
    {
        SetColor(on ? highlight : _baseColor);
    }

    static string CoordToName(Vector2Int c)
    {
        // A..Z then AA.. etc if you ever exceed 26 columns
        string colName = "";
        int x = c.x;
        do { colName = (char)('A' + (x % 26)) + colName; x = x / 26 - 1; } while (x >= 0);
        return $"{colName}{c.y + 1}";
    }
}