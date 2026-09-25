using UnityEngine;
using Cysharp.Threading.Tasks;
using System.Threading;
using System.Collections.Generic;

namespace HaeTataki
{
    public enum GameState
    {
        Title,       // タイトル画面表示中（スタート待機）
        Playing,     // 3ラウンド進行中
        FinalResult  // 最終リザルト表示中（タイトル復帰待機）
    }

    [System.Serializable]
    public struct RoundRecord
    {
        public int roundNumber;
        public bool isSuccess;
        public float diffMs;
        public int score;
    }

    public class HaeTataki_GameManager : MonoBehaviour
    {
        [Header("Actor References")]
        [SerializeField] private HaeTataki_MosquitoActor mosquito;
        [SerializeField] private HaeTataki_HandActor handLeft;
        [SerializeField] private HaeTataki_HandActor handRight;
        [SerializeField] private HaeTataki_UI ui;

        [Header("Settings")]
        [SerializeField] [Range(0.3f, 3.0f)] private float suspenseDuration = 1.2f;

        private struct RoundDifficulty
        {
            public float flightDuration;
            public float warningDuration;
            public float catchableDuration;
            public float syncTolerance;
        }

        private readonly RoundDifficulty[] _roundSettings = new RoundDifficulty[]
        {
            new RoundDifficulty { flightDuration = 2.2f, warningDuration = 0.60f, catchableDuration = 0.60f, syncTolerance = 0.150f },
            new RoundDifficulty { flightDuration = 1.7f, warningDuration = 0.40f, catchableDuration = 0.45f, syncTolerance = 0.120f },
            new RoundDifficulty { flightDuration = 1.2f, warningDuration = 0.25f, catchableDuration = 0.30f, syncTolerance = 0.090f }
        };

        private GameState _currentState = GameState.Title;
        private int _currentRoundIndex = 0;
        private float _p1Time = -1f;
        private float _p2Time = -1f;
        private bool _isResolved = false;
        private CancellationTokenSource _cts;
        private UniTaskCompletionSource _roundEndTcs;

        public List<RoundRecord> Records { get; private set; } = new List<RoundRecord>();

        private void Start()
        {
            // 起動時はタイトル画面から開始
            ToTitle();
        }

        private void Update()
        {
            switch (_currentState)
            {
                case GameState.Title:
                    // Spaceキー または AキーとLキーの同時押しでスタート
                    if (Input.GetKeyDown(KeyCode.Space) || (Input.GetKey(KeyCode.A) && Input.GetKey(KeyCode.L)))
                    {
                        StartFullGame();
                    }
                    break;

                case GameState.Playing:
                    HandlePlayingInput();
                    break;

                case GameState.FinalResult:
                    // リザルト画面でSpaceまたはRキーでタイトル画面へ戻る
                    if (Input.GetKeyDown(KeyCode.Space) || Input.GetKeyDown(KeyCode.R))
                    {
                        SafePlaySE(SoundManager.SEData.SETYPE.Button);
                        ToTitle();
                    }
                    break;
            }
        }

        /// <summary>
        /// タイトル画面への遷移
        /// </summary>
        public void ToTitle()
        {
            _currentState = GameState.Title;
            _cts?.Cancel();
            _cts?.Dispose();
            _cts = null;

            handLeft.ResetState();
            handRight.ResetState();
            mosquito.ResetState();
            ui.ResetUI();
            ui.ShowTitle();
        }

        private void StartFullGame()
        {
            _currentState = GameState.Playing;
            SafePlaySE(SoundManager.SEData.SETYPE.Button);

            ui.HideTitle();

            _cts?.Cancel();
            _cts?.Dispose();
            _cts = new CancellationTokenSource();

            _currentRoundIndex = 0;
            Records.Clear();

            RunThreeRoundsLoopAsync(_cts.Token).Forget();
        }

        private void HandlePlayingInput()
        {
            // Rキー: 即座にタイトルへ戻す
            if (Input.GetKeyDown(KeyCode.R))
            {
                ToTitle();
                return;
            }

            if (_isResolved) return;

            // Player 1 (Aキー)
            if (Input.GetKeyDown(KeyCode.A) && _p1Time < 0f)
            {
                _p1Time = Time.time;
                handLeft.SwingToCenter();
                ui.PunchKeyBadge(isP1: true);
                CheckInputTiming();
            }

            // Player 2 (Lキー)
            if (Input.GetKeyDown(KeyCode.L) && _p2Time < 0f)
            {
                _p2Time = Time.time;
                handRight.SwingToCenter();
                ui.PunchKeyBadge(isP1: false);
                CheckInputTiming();
            }

            // Spaceキー（デバッグ同時押し）
            #if UNITY_EDITOR || DEVELOPMENT_BUILD
            if (Input.GetKeyDown(KeyCode.Space) && _p1Time < 0f && _p2Time < 0f)
            {
                _p1Time = Time.time;
                _p2Time = Time.time;
                handLeft.SwingToCenter();
                handRight.SwingToCenter();
                ui.PunchKeyBadge(isP1: true);
                ui.PunchKeyBadge(isP1: false);
                CheckInputTiming();
            }
            #endif
        }

        private async UniTask RunThreeRoundsLoopAsync(CancellationToken ct)
        {
            for (int r = 0; r < 3; r++)
            {
                _currentRoundIndex = r;
                _p1Time = -1f;
                _p2Time = -1f;
                _isResolved = false;
                _roundEndTcs = new UniTaskCompletionSource();

                handLeft.ResetState();
                handRight.ResetState();
                mosquito.ResetState();
                ui.ResetUI();

                string roundTitle = (r == 2) ? "<color=#FF2222><size=55>FINAL ROUND!!</size></color>" : $"<size=55>ROUND {r + 1}</size>";
                ui.ShowReady(roundTitle);
                await UniTask.Delay(1000, cancellationToken: ct);
                ui.HideInstruction();
                await UniTask.Delay(250, cancellationToken: ct);

                RoundDifficulty diff = _roundSettings[r];
                RunMosquitoFlightAsync(diff, ct).Forget();

                await _roundEndTcs.Task;

                if (r < 2)
                {
                    ui.AppendNextRoundGuide();
                    await UniTask.WaitUntil(() => Input.GetKeyDown(KeyCode.Space), cancellationToken: ct);
                    SafePlaySE(SoundManager.SEData.SETYPE.Button);

                    ui.ResetUI();
                    await UniTask.Delay(400, cancellationToken: ct);
                }
            }

            // 全3ラウンド終了 ➔ リザルト状態へ
            _currentState = GameState.FinalResult;
            ui.ShowFinalResult(Records);
        }

        private async UniTaskVoid RunMosquitoFlightAsync(RoundDifficulty diff, CancellationToken ct)
        {
            await mosquito.StartFlightSequenceAsync(
                diff.flightDuration,
                diff.warningDuration,
                diff.catchableDuration,
                ct);

            if (!_isResolved)
            {
                _isResolved = true;
                Records.Add(new RoundRecord
                {
                    roundNumber = _currentRoundIndex + 1,
                    isSuccess = false,
                    diffMs = 9999f,
                    score = 0
                });

                await OnTimeOutAsync(ct);
                _roundEndTcs?.TrySetResult();
            }
        }

        private void CheckInputTiming()
        {
            if (_p1Time > 0f && _p2Time > 0f)
            {
                _isResolved = true;
                float diff = Mathf.Abs(_p1Time - _p2Time);
                float diffMs = diff * 1000f;

                RoundDifficulty currentDiff = _roundSettings[_currentRoundIndex];
                bool isSynced = (diff <= currentDiff.syncTolerance);

                ProcessInputResolutionAsync(isSynced, diffMs, _cts.Token).Forget();
            }
        }

        private async UniTaskVoid ProcessInputResolutionAsync(bool isSynced, float diffMs, CancellationToken ct)
        {
            await ResolveWithSuspenseAsync(isSynced, diffMs, ct);
            _roundEndTcs?.TrySetResult();
        }

        private async UniTask ResolveWithSuspenseAsync(bool isSynced, float diffMs, CancellationToken ct)
        {
            await UniTask.Delay(80, cancellationToken: ct);

            bool isSuccess = mosquito.IsCatchable && isSynced;
            int score = CalculateScore(isSuccess, diffMs);

            mosquito.HideInHands();

            handLeft.ShiverSuspense(suspenseDuration);
            handRight.ShiverSuspense(suspenseDuration);
            await UniTask.Delay((int)(suspenseDuration * 1000f), cancellationToken: ct);

            Records.Add(new RoundRecord
            {
                roundNumber = _currentRoundIndex + 1,
                isSuccess = isSuccess,
                diffMs = diffMs,
                score = score
            });

            if (isSuccess)
            {
                await OnPerfectAsync(score, diffMs, ct);
            }
            else
            {
                await OnTimingMissAsync(diffMs, ct);
            }
        }

        private int CalculateScore(bool isSuccess, float diffMs)
        {
            if (!isSuccess) return 0;

            int score = 100;
            if (diffMs <= 30f) score += 30;
            else if (diffMs <= 80f) score += 15;
            return score;
        }

        private async UniTask OnPerfectAsync(int earnedScore, float diffMs, CancellationToken ct)
        {
            handLeft.ReturnToInitial(0.25f);
            handRight.ReturnToInitial(0.25f);
            mosquito.Squash();
            ui.ShowResult(true, diffMs);

            SafePlaySE(SoundManager.SEData.SETYPE.Winner);
            await UniTask.Delay(1500, cancellationToken: ct);
        }

        private async UniTask OnTimingMissAsync(float diffMs, CancellationToken ct)
        {
            mosquito.Escape();
            handLeft.ReturnToInitial(0.18f);
            handRight.ReturnToInitial(0.18f);

            await UniTask.Delay(180, cancellationToken: ct);
            if (_p1Time < _p2Time) handRight.PlayHurt();
            else handLeft.PlayHurt();

            ui.ShowResult(false, diffMs);

            SafePlaySE(SoundManager.SEData.SETYPE.PlayerDamage);
            await UniTask.Delay(1500, cancellationToken: ct);
        }

        private async UniTask OnTimeOutAsync(CancellationToken ct)
        {
            await mosquito.CameraAttackAsync(ct);
            ui.ShowTimeOut();

            SafePlaySE(SoundManager.SEData.SETYPE.Loser);
            await UniTask.Delay(1500, cancellationToken: ct);
        }

        private void SafePlaySE(SoundManager.SEData.SETYPE se)
        {
            if (SoundManager.Instance != null)
            {
                try
                {
                    SoundManager.Instance.PlaySE(se);
                }
                catch (System.Exception ex)
                {
                    Debug.LogWarning($"[HaeTataki] SE '{se}' は未登録のため再生をスキップしました: {ex.Message}");
                }
            }
        }

        private void OnDestroy()
        {
            _cts?.Cancel();
            _cts?.Dispose();
        }
    }
}