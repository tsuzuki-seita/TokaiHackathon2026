using System.Collections;
using UnityEngine;

/// <summary>
/// Webカメラを起動し、画面表示用の座標(鏡像)とカメラ画素の座標を相互変換する
/// </summary>
public class CV_WebCamSource : MonoBehaviour
{
    [Tooltip("使いたいカメラ名の一部（空なら最初のカメラ）。起動時にConsoleへ一覧を出します")]
    [SerializeField] string deviceNameContains = "";
    [SerializeField] int requestedWidth = 1280;
    [SerializeField] int requestedHeight = 720;
    [SerializeField] int requestedFps = 30;
    [Tooltip("鏡のように左右反転して表示する")]
    [SerializeField] bool mirror = true;

    public WebCamTexture Texture { get; private set; }
    public bool IsReady => Texture != null && Texture.isPlaying && Texture.width > 16;
    public float Aspect => IsReady ? (float)Texture.width / Texture.height : 16f / 9f;
    public string ErrorMessage { get; private set; }

    bool VerticalFlip => Texture != null && Texture.videoVerticallyMirrored;

    /// <summary>RawImage.uvRect に設定する値（鏡像・上下反転を反映）</summary>
    public Rect DisplayUVRect => new Rect(mirror ? 1 : 0, VerticalFlip ? 1 : 0, mirror ? -1 : 1, VerticalFlip ? -1 : 1);

    IEnumerator Start()
    {
        yield return Application.RequestUserAuthorization(UserAuthorization.WebCam);
        if (!Application.HasUserAuthorization(UserAuthorization.WebCam))
        {
            ErrorMessage = "カメラへのアクセスが許可されていません";
            Debug.LogError($"[CV] {ErrorMessage}");
            yield break;
        }

        var devices = WebCamTexture.devices;
        if (devices.Length == 0)
        {
            ErrorMessage = "カメラが見つかりません";
            Debug.LogError($"[CV] {ErrorMessage}");
            yield break;
        }

        string deviceName = devices[0].name;
        for (int i = 0; i < devices.Length; i++)
        {
            Debug.Log($"[CV] Camera {i}: {devices[i].name}");
            if (!string.IsNullOrEmpty(deviceNameContains) && devices[i].name.Contains(deviceNameContains))
            {
                deviceName = devices[i].name;
            }
        }

        Texture = new WebCamTexture(deviceName, requestedWidth, requestedHeight, requestedFps);
        Texture.Play();

        // 実際の解像度が確定するまで待つ
        while (Texture.width <= 16) yield return null;

        Debug.Log($"[CV] Using camera: {deviceName} ({Texture.width}x{Texture.height})");
    }

    void OnDestroy()
    {
        if (Texture != null)
        {
            Texture.Stop();
            Destroy(Texture);
        }
    }

    /// <summary>画面表示上のUV(0~1, 左下原点)をカメラ画素のUVに変換</summary>
    public Vector2 DisplayToTextureUV(Vector2 uv)
    {
        return new Vector2(mirror ? 1f - uv.x : uv.x, VerticalFlip ? 1f - uv.y : uv.y);
    }
}
