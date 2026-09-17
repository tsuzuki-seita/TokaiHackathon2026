using UnityEngine;

namespace PoseGame
{
    // Splits the webcam frame into `ZoneCount` equal vertical slices (one per player), and keeps
    // one BlazePose estimate per zone fresh by cycling through zones over time (one zone's full
    // detect+landmark pass per tick, since each pass involves a blocking GPU readback).
    public class ZonePoseTracker : MonoBehaviour
    {
        public const int MaxZones = 4;

        [Range(1, MaxZones)] public int ZoneCount = 2;
        public float SecondsPerZoneUpdate = 0.12f;

        public WebcamProvider Webcam;

        public RenderTexture[] ZoneTextures { get; private set; } = new RenderTexture[MaxZones];
        public PersonPose[] Poses { get; private set; } = new PersonPose[MaxZones];

        BlazePoseEstimator m_Estimator;
        float m_Timer;
        int m_CurrentZone;

        void Awake()
        {
            m_Estimator = new BlazePoseEstimator();
            for (var i = 0; i < MaxZones; i++)
                Poses[i] = new PersonPose();
        }

        public void SetZoneCount(int count)
        {
            ZoneCount = Mathf.Clamp(count, 1, MaxZones);
            m_CurrentZone = 0;

            for (var i = 0; i < MaxZones; i++)
            {
                if (ZoneTextures[i] != null)
                {
                    ZoneTextures[i].Release();
                    ZoneTextures[i] = null;
                }
                Poses[i].Reset();
            }
        }

        void EnsureZoneTexture(int zone)
        {
            if (ZoneTextures[zone] != null)
                return;

            // A tall-ish fixed resolution per zone; BlazePose pads to a square internally so the
            // exact aspect isn't critical.
            ZoneTextures[zone] = new RenderTexture(360, 640, 0, RenderTextureFormat.ARGB32)
            {
                name = $"PoseZone_{zone}",
            };
        }

        void Update()
        {
            if (Webcam == null || !Webcam.IsReady || ZoneCount <= 0)
                return;

            // Sentis's Worker/Tensor objects aren't Unity-serializable, so a script hot-reload
            // triggered by editing code while already in Play mode wipes this reference (Awake
            // doesn't re-run). Rebuilding here makes the tracker self-heal instead of spamming
            // NullReferenceException every frame until the next full Stop/Play cycle.
            if (m_Estimator == null)
                m_Estimator = new BlazePoseEstimator();
            for (var i = 0; i < MaxZones; i++)
                if (Poses[i] == null)
                    Poses[i] = new PersonPose();

            m_Timer += Time.deltaTime;
            if (m_Timer < SecondsPerZoneUpdate)
                return;
            m_Timer = 0f;

            var zone = m_CurrentZone;
            m_CurrentZone = (m_CurrentZone + 1) % ZoneCount;

            EnsureZoneTexture(zone);
            BlitZone(Webcam.Texture, ZoneTextures[zone], zone, ZoneCount);
            m_Estimator.Estimate(ZoneTextures[zone], Poses[zone]);
        }

        // Crops+mirrors zone `index` of `N` out of the source webcam texture into `dest`,
        // producing a standard selfie-mirror view (left/right flipped) per zone.
        static void BlitZone(WebCamTexture source, RenderTexture dest, int index, int count)
        {
            var scaleX = -1f / count;
            var offsetX = 1f - (float)index / count;

            var flipY = source.videoVerticallyMirrored;
            var scaleY = flipY ? -1f : 1f;
            var offsetY = flipY ? 1f : 0f;

            Graphics.Blit(source, dest, new Vector2(scaleX, scaleY), new Vector2(offsetX, offsetY));
        }

        void OnDestroy()
        {
            m_Estimator?.Dispose();
            for (var i = 0; i < MaxZones; i++)
            {
                if (ZoneTextures[i] != null)
                    ZoneTextures[i].Release();
            }
        }
    }
}
