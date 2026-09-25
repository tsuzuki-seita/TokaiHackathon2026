using UnityEngine;

namespace HaeTataki
{
    public class HaeTataki_CameraCapture : MonoBehaviour
    {
        [Header("Settings")]
        [SerializeField] private int requestedWidth = 640;
        [SerializeField] private int requestedHeight = 480;
        [SerializeField] private int requestedFPS = 30;

        private WebCamTexture _webCamTexture;

        public Texture2D FocusFaceTexture { get; private set; }
        public Texture2D ActionFaceTexture { get; private set; }

        public bool IsCameraAvailable => _webCamTexture != null && _webCamTexture.isPlaying;
        public bool HasSuccessPhoto { get; private set; } = false;

        private void Start()
        {
            InitializeCamera();
        }

        private void InitializeCamera()
        {
            WebCamDevice[] devices = WebCamTexture.devices;
            if (devices == null || devices.Length == 0)
            {
                Debug.LogWarning("[CameraCapture] Webカメラが見つかりません。撮影機能はスキップされます。");
                return;
            }

            _webCamTexture = new WebCamTexture(devices[0].name, requestedWidth, requestedHeight, requestedFPS);
            _webCamTexture.Play();
            Debug.Log($"<color=#00FF88>[CameraCapture] Webカメラ起動完了: {devices[0].name}</color>");
        }

        /// <summary>
        /// ゲーム再スタート時の写真リセット
        /// </summary>
        public void ResetPhotos()
        {
            HasSuccessPhoto = false;
            SafeDestroy(FocusFaceTexture);
            SafeDestroy(ActionFaceTexture);
            FocusFaceTexture = null;
            ActionFaceTexture = null;
        }

        /// <summary>
        /// Shot 1: 蚊が止まりかけて集中している瞬間（真剣な顔）
        /// </summary>
        public void CaptureFocusFace()
        {
            if (!IsCameraAvailable) return;
            SafeDestroy(FocusFaceTexture);
            FocusFaceTexture = TakeSnapshot();
            Debug.Log("[CameraCapture] Shot 1 (集中顔) をキャプチャしました！");
        }

        /// <summary>
        /// Shot 2: 【成功時限定】完全撃退したラウンドの合掌顔を激写！
        /// </summary>
        public void CaptureSuccessFace()
        {
            if (!IsCameraAvailable) return;
            SafeDestroy(ActionFaceTexture);
            ActionFaceTexture = TakeSnapshot();
            HasSuccessPhoto = true;
            Debug.Log("<color=#FFEE00>[CameraCapture] ★ 成功ラウンドの瞬間を激写しました！ ★</color>");
        }

        /// <summary>
        /// 全ラウンド失敗時の予備（一度も成功しなかった場合のみ空振り顔を撮る）
        /// </summary>
        public void CaptureFallbackActionFace()
        {
            if (!IsCameraAvailable || HasSuccessPhoto) return;
            SafeDestroy(ActionFaceTexture);
            ActionFaceTexture = TakeSnapshot();
            Debug.Log("[CameraCapture] フォールバック結果顔をキャプチャしました。");
        }

        private Texture2D TakeSnapshot()
        {
            Texture2D snap = new Texture2D(_webCamTexture.width, _webCamTexture.height, TextureFormat.RGB24, false);
            snap.SetPixels(_webCamTexture.GetPixels());
            snap.Apply();
            return snap;
        }

        private void SafeDestroy(Texture2D tex)
        {
            if (tex != null)
            {
                Destroy(tex);
            }
        }

        private void OnDestroy()
        {
            if (_webCamTexture != null)
            {
                _webCamTexture.Stop();
                Destroy(_webCamTexture);
            }
            SafeDestroy(FocusFaceTexture);
            SafeDestroy(ActionFaceTexture);
            FocusFaceTexture = null;
            ActionFaceTexture = null;
        }
    }
}