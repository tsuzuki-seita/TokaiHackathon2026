using PoseGame.UI;
using UnityEngine;

namespace PoseGame
{
    // Single entry point for the whole pose-matching game. Attach this to one empty GameObject
    // in the scene; everything else (webcam, Sentis pose tracker, UI, game flow) is created at
    // runtime so the scene itself needs no further manual wiring.
    public class PoseGameBootstrap : MonoBehaviour
    {
        void Awake()
        {
            var webcam = gameObject.AddComponent<WebcamProvider>();

            var tracker = gameObject.AddComponent<ZonePoseTracker>();
            tracker.Webcam = webcam;

            var uiGo = new GameObject("PoseGameUI");
            uiGo.transform.SetParent(transform, false);
            var ui = uiGo.AddComponent<PoseGameUI>();

            var manager = gameObject.AddComponent<PoseGameManager>();
            manager.Webcam = webcam;
            manager.Tracker = tracker;
            manager.Ui = ui;
        }
    }
}
