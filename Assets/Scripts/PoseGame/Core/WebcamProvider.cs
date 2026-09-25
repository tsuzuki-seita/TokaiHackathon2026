using UnityEngine;

namespace PoseGame
{
    // Starts the first available webcam and exposes it as a Texture, plus whether the feed
    // should be mirrored (selfie view) to match a standard "look in a mirror" UX.
    public class WebcamProvider : MonoBehaviour
    {
        public WebCamTexture Texture { get; private set; }
        public bool IsReady => Texture != null && Texture.width > 16;
        public string ErrorMessage { get; private set; }

        void OnEnable()
        {
            TryStart();
        }

        void TryStart()
        {
            if (WebCamTexture.devices.Length == 0)
            {
                ErrorMessage = "カメラが見つかりません";
                return;
            }

            var device = WebCamTexture.devices[0];
            Texture = new WebCamTexture(device.name, 1280, 720, 30);
            Texture.Play();
        }

        void OnDisable()
        {
            if (Texture != null)
            {
                Texture.Stop();
                Destroy(Texture);
                Texture = null;
            }
        }
    }
}
