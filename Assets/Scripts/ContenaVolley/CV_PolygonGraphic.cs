using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 任意の凸多角形を描くUIグラフィック（ガラスの破片に使う）
/// </summary>
public class CV_PolygonGraphic : MaskableGraphic
{
    Vector2[] points;

    public void SetPoints(Vector2[] localPoints)
    {
        points = localPoints;
        SetVerticesDirty();
    }

    protected override void OnPopulateMesh(VertexHelper vh)
    {
        vh.Clear();
        if (points == null || points.Length < 3) return;

        foreach (var p in points) vh.AddVert(p, color, Vector2.zero);
        for (int i = 1; i < points.Length - 1; i++) vh.AddTriangle(0, i, i + 1);
    }
}
