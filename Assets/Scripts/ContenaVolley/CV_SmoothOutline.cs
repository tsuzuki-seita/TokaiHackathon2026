using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 文字の周りを全方向に均一な太さで縁取る。
/// Unity 標準の Outline は斜め4方向にずらすだけなので、太くすると角がガタガタになる。
/// こちらは円周上の directions 方向に文字を複製して、丸みのあるきれいな縁取りにする
/// </summary>
public class CV_SmoothOutline : BaseMeshEffect
{
    public Color color = Color.black;
    public float thickness = 6f;
    [Range(4, 32)] public int directions = 16;

    static readonly List<UIVertex> verts = new List<UIVertex>();

    public override void ModifyMesh(VertexHelper vh)
    {
        if (!IsActive() || thickness <= 0f) return;

        verts.Clear();
        vh.GetUIVertexStream(verts);
        int count = verts.Count;
        if (count == 0) return;

        var result = new List<UIVertex>(count * (directions + 1));
        for (int d = 0; d < directions; d++)
        {
            float a = d * Mathf.PI * 2f / directions;
            var offset = new Vector3(Mathf.Cos(a), Mathf.Sin(a)) * thickness;
            for (int i = 0; i < count; i++)
            {
                var v = verts[i];
                v.position += offset;
                var c = color;
                c.a *= v.color.a / 255f;
                v.color = c;
                result.Add(v);
            }
        }
        // 本体の文字は最後（いちばん手前）に描く
        result.AddRange(verts);

        vh.Clear();
        vh.AddUIVertexTriangleStream(result);
    }

    public void Refresh()
    {
        if (graphic != null) graphic.SetVerticesDirty();
    }
}
