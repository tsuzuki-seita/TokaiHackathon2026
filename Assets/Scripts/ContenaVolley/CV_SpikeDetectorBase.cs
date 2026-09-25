using UnityEngine;

/// <summary>
/// スパイク検出器の共通インターフェース。
/// 座標はすべて「画面に表示しているカメラ映像のUV」(0~1, 左下原点, 鏡像表示後)で扱う。
/// フレーム差分版・骨格検出(MediaPipe等)版はこれを継承して差し替える。
/// </summary>
public abstract class CV_SpikeDetectorBase : MonoBehaviour
{
    /// <summary>検出できる状態か（カメラ起動済みなど）</summary>
    public abstract bool IsReady { get; }

    /// <summary>
    /// 目標位置の円内でスパイクが起きたかを判定する。毎フレーム呼ばれる。
    /// </summary>
    /// <param name="targetUV">カーソルの中心</param>
    /// <param name="radius">判定半径（映像の高さに対する割合）</param>
    /// <param name="hitUV">スパイクが検出された位置</param>
    public abstract bool Detect(Vector2 targetUV, float radius, out Vector2 hitUV);

    /// <summary>新しいカーソルを出す直前に呼ばれる</summary>
    public virtual void ResetState() { }

    /// <summary>デバッグ表示用の画像（なければnull）</summary>
    public virtual Texture DebugTexture => null;

    /// <summary>デバッグ表示用の文字列</summary>
    public virtual string DebugInfo => "";
}
