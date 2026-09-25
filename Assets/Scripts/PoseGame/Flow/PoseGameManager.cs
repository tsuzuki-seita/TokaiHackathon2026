using System.Collections.Generic;
using System.Linq;
using System.Threading;
using Cysharp.Threading.Tasks;
using PoseGame.UI;
using UnityEngine;

namespace PoseGame
{
    public class PoseGameManager : MonoBehaviour
    {
        // The unique key this mini-game reports its score under (see MiniGameScoreBoard).
        public const string MiniGameId = "PoseMatch";

        public const int TotalRounds = 5;
        public const int MaxPlayers = 3;
        const float ThemeAnnounceSeconds = 0.6f;
        const float PoseHoldSeconds = 6f; // the whole hold counts toward the score — players already got into position during the ready prompt + countdown
        const float RoundResultSeconds = 3.5f;

        public WebcamProvider Webcam;
        public ZonePoseTracker Tracker;
        public PoseGameUI Ui;

        readonly List<float> m_RoundTeamScores = new List<float>();
        readonly List<PoseTheme> m_ThemeBag = new List<PoseTheme>();
        System.Random m_Random = new System.Random();

        CancellationTokenSource m_RunCts;

        void Start()
        {
            Ui.OnStartRequested += HandleStartRequested;
            Ui.OnPlayAgainRequested += HandlePlayAgainRequested;
            Ui.OnBackToTitleRequested += HandleBackToTitleRequested;
        }

        void HandleStartRequested() => RestartRun();

        void HandlePlayAgainRequested() => RestartRun();

        void HandleBackToTitleRequested()
        {
            m_RunCts?.Cancel();
            Ui.ShowTitle();
        }

        void RestartRun()
        {
            m_RunCts?.Cancel();
            m_RunCts = new CancellationTokenSource();
            m_RoundTeamScores.Clear();

            RunGameAsync(m_RunCts.Token).Forget();
        }

        // The team starts small and grows each round (up to MaxPlayers) so newcomers only need
        // to join in when it's their turn, instead of everyone crowding into frame from round 1.
        static int ZoneCountForRound(int round) => Mathf.Min(round + 1, MaxPlayers);

        PoseTheme NextTheme()
        {
            if (m_ThemeBag.Count == 0)
                m_ThemeBag.AddRange(PoseThemeLibrary.Themes);
            var index = m_Random.Next(m_ThemeBag.Count);
            var theme = m_ThemeBag[index];
            m_ThemeBag.RemoveAt(index);
            return theme;
        }

        async UniTaskVoid RunGameAsync(CancellationToken ct)
        {
            try
            {
                await RunGameLoopAsync(ct);
            }
            catch (System.OperationCanceledException)
            {
                // Expected when a new run starts (Play Again / Start) or the player backs out
                // to the title screen while this run's round loop was mid-flight.
            }
        }

        async UniTask RunGameLoopAsync(CancellationToken ct)
        {
            for (var round = 1; round <= TotalRounds; round++)
            {
                if (ct.IsCancellationRequested)
                    return;

                var zoneCount = ZoneCountForRound(round);
                Tracker.SetZoneCount(zoneCount);
                Ui.ShowPlay(zoneCount);

                var theme = NextTheme();
                Ui.SetRoundText(round, TotalRounds);
                Ui.AnnounceTheme(theme);

                await UniTask.Delay(System.TimeSpan.FromSeconds(ThemeAnnounceSeconds), cancellationToken: ct);

                // Keep the zone panels live (video + skeleton) through the ready prompt and
                // countdown, so "カメラに映ってる？" is something players can actually check
                // instead of staring at a frozen/empty preview.
                using (var previewCts = CancellationTokenSource.CreateLinkedTokenSource(ct))
                {
                    PreviewZonesAsync(zoneCount, previewCts.Token).Forget();

                    // Let everyone see themselves in their camera zone and get into position
                    // before the round actually starts, rather than starting on a fixed timer.
                    await WaitForReadyAsync(ct);

                    await CountdownAsync(ct);

                    previewCts.Cancel();
                }

                var samples = new List<float>[zoneCount];
                for (var i = 0; i < zoneCount; i++)
                    samples[i] = new List<float>();

                // Players already got into position during the ready prompt + countdown, so the
                // whole hold counts toward the score rather than hiding an extra grace period
                // inside it — simpler to reason about and to show players.
                Ui.ShowScoringHint();

                var elapsed = 0f;
                while (elapsed < PoseHoldSeconds)
                {
                    if (ct.IsCancellationRequested)
                        return;

                    // Everyone is scored against each other (how well the group is in sync),
                    // not against a fixed target — so this is computed once per frame for the
                    // whole group, not per zone.
                    var groupScores = GroupSyncScorer.Score(Tracker.Poses, theme);

                    Ui.UpdateScoringHint(PoseHoldSeconds - elapsed);

                    for (var zone = 0; zone < zoneCount; zone++)
                    {
                        var pose = Tracker.Poses[zone];
                        var score = groupScores[zone] / 100f;
                        Ui.UpdateZone(zone, Tracker.ZoneTextures[zone], pose, score);

                        if (pose.IsDetected)
                            samples[zone].Add(score);
                    }

                    await UniTask.Yield(PlayerLoopTiming.Update, ct);
                    elapsed += Time.deltaTime;
                }
                Ui.HideScoringHint();

                // The team's score for the round is everyone's agreement averaged together —
                // one shared number for "how in sync was the whole group", not a ranking, so we
                // don't keep or display the per-player breakdown.
                var perPlayerAverages = new List<float>();
                for (var zone = 0; zone < zoneCount; zone++)
                    perPlayerAverages.Add(samples[zone].Count > 0 ? samples[zone].Average() : 0f);
                var teamScore = perPlayerAverages.Count > 0 ? perPlayerAverages.Average() : 0f;
                m_RoundTeamScores.Add(teamScore);

                if (ct.IsCancellationRequested)
                    return;

                // Capture the decisive moment (poses + skeletons + scores, exactly as shown on
                // screen) before flashing, so the flash overlay isn't in the saved photo.
                await UniTask.WaitForEndOfFrame(this, ct);
                var photo = PhotoCapture.CaptureScreen();
                Ui.PlayPhotoFlash();
                string photoPath = null;
                try
                {
                    photoPath = PhotoCapture.Save(photo, $"round{round}_{theme.Id}");
                }
                catch (System.Exception e)
                {
                    Debug.LogWarning($"写真の保存に失敗しました: {e.Message}");
                }

                var isFinalRound = round == TotalRounds;
                Ui.ShowResults(theme, teamScore, isFinalRound, photo, photoPath);

                if (!isFinalRound)
                {
                    await UniTask.Delay(System.TimeSpan.FromSeconds(RoundResultSeconds), cancellationToken: ct);
                }
                else
                {
                    ReportFinalScore();
                }
            }
        }

        void ReportFinalScore()
        {
            var overallTeamScore = m_RoundTeamScores.Count > 0 ? m_RoundTeamScores.Average() : 0f;
            MiniGameScoreBoard.ReportScore(MiniGameId, overallTeamScore);
        }

        async UniTaskVoid PreviewZonesAsync(int zoneCount, CancellationToken ct)
        {
            try
            {
                while (true)
                {
                    for (var zone = 0; zone < zoneCount; zone++)
                        Ui.UpdateZone(zone, Tracker.ZoneTextures[zone], Tracker.Poses[zone], 0f);

                    await UniTask.Yield(PlayerLoopTiming.Update, ct);
                }
            }
            catch (System.OperationCanceledException)
            {
                // Expected once the round moves past the ready/countdown phase, or the run is cancelled.
            }
        }

        async UniTask WaitForReadyAsync(CancellationToken ct)
        {
            var tcs = new UniTaskCompletionSource();
            Ui.ShowReadyPrompt(() => tcs.TrySetResult());

            using (ct.Register(() => tcs.TrySetCanceled()))
            {
                await tcs.Task;
            }

            Ui.HideReadyPrompt();
        }

        async UniTask CountdownAsync(CancellationToken ct)
        {
            foreach (var label in new[] { "3", "2", "1", "ポーズ！" })
            {
                Ui.ShowCountdown(label);
                await UniTask.Delay(System.TimeSpan.FromSeconds(0.7f), cancellationToken: ct);
            }
            Ui.HideCountdown();
        }
    }
}
