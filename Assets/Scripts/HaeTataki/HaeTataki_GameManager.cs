using UnityEngine;
using Cysharp.Threading.Tasks;
using System.Threading;
using System.Collections.Generic;
using System.Reflection;

namespace HaeTataki
{
    public enum GameState
    {
        Title,
        Playing,
        FinalResult
    }

    [System.Serializable]
    public struct RoundRecord
    {
        public int roundNumber;
        public bool isSuccess;
        public float diffMs;
        public int score;
        public string timingComment;
    }

    public class HaeTataki_GameManager : MonoBehaviour
    {
        [Header("Actor References")]
        [SerializeField] private HaeTataki_MosquitoActor mosquito;
        [SerializeField] private HaeTataki_HandActor handLeft;
        [SerializeField] private HaeTataki_HandActor handRight;
        [SerializeField] private HaeTataki_UI ui;
        [SerializeField] private HaeTataki_CameraCapture cameraCapture;

        [Header("Settings")]
        [SerializeField] [Range(0.3f, 3.0f)] private float suspenseDuration = 1.2f;
        [Tooltip("Round 3(最終ラウンド)の合掌タメ秒数")]
        [SerializeField] [Range(1.0f, 5.0f)] private float finalRoundSuspenseDuration = 2.4f;

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
            new RoundDifficulty { flightDuration = 1.8f, warningDuration = 0.45f, catchableDuration = 0.45f, syncTolerance = 0.120f },
            new RoundDifficulty { flightDuration = 3.2f, warningDuration = 1.10f, catchableDuration = 0.35f, syncTolerance = 0.090f }
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
            ToTitle();
        }

        private void Update()
        {
            switch (_currentState)
            {
                case GameState.Title:
                    if (Input.GetKeyDown(KeyCode.Space) || (Input.GetKey(KeyCode.A) && Input.GetKey(KeyCode.L)))
                    {
                        StartFullGame();
                    }
                    break;

                case GameState.Playing:
                    HandlePlayingInput();
                    break;

                case GameState.FinalResult:
                    if (Input.GetKeyDown(KeyCode.Space) || Input.GetKeyDown(KeyCode.R))
                    {
                        SafePlaySE(SoundManager.SEData.SETYPE.Button);
                        ToTitle();
                    }
                    break;
            }
        }

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

            SafePlayBGM(SoundManager.BGMData.BGMTYPE.Title, isLoop: true);
        }

        private void StartFullGame()
        {
            _currentState = GameState.Playing;
            SafePlaySE(SoundManager.SEData.SETYPE.Button);
            SafePlayBGM(SoundManager.BGMData.BGMTYPE.InGame, isLoop: true);

            ui.HideTitle();

            _cts?.Cancel();
            _cts?.Dispose();
            _cts = new CancellationTokenSource();

            _currentRoundIndex = 0;
            Records.Clear();

            if (cameraCapture != null)
            {
                cameraCapture.ResetPhotos();
            }

            RunThreeRoundsLoopAsync(_cts.Token).Forget();
        }

        private void HandlePlayingInput()
        {
            if (Input.GetKeyDown(KeyCode.R))
            {
                ToTitle();
                return;
            }

            if (_isResolved) return;

            if (Input.GetKeyDown(KeyCode.A) && _p1Time < 0f)
            {
                _p1Time = Time.time;
                handLeft.SwingToCenter();
                ui.PunchKeyBadge(isP1: true);
                CheckInputTiming();
            }

            if (Input.GetKeyDown(KeyCode.L) && _p2Time < 0f)
            {
                _p2Time = Time.time;
                handRight.SwingToCenter();
                ui.PunchKeyBadge(isP1: false);
                CheckInputTiming();
            }

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

                RoundDifficulty diff = _roundSettings[r];
                float totalRoundTime = diff.flightDuration + diff.warningDuration + diff.catchableDuration;

                string roundTitle = (r == 2) 
                    ? "<color=#FF2222><size=85>FINAL ROUND!!</size></color>" 
                    : $"<size=75>ROUND {r + 1}</size>";

                ui.ShowReady(roundTitle, totalRoundTime);
                SafePlaySE(SoundManager.SEData.SETYPE.SceneMove);

                await UniTask.Delay(1000, cancellationToken: ct);
                ui.HideInstruction();
                await UniTask.Delay(250, cancellationToken: ct);

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

            _currentState = GameState.FinalResult;
            SafePlayBGM(SoundManager.BGMData.BGMTYPE.Mating, isLoop: false);

            ui.ShowFinalResult(
                Records,
                cameraCapture != null ? cameraCapture.FocusFaceTexture : null,
                cameraCapture != null ? cameraCapture.ActionFaceTexture : null);
        }

        private async UniTaskVoid RunMosquitoFlightAsync(RoundDifficulty diff, CancellationToken ct)
        {
            await mosquito.StartFlightSequenceAsync(
                diff.flightDuration,
                diff.warningDuration,
                diff.catchableDuration,
                ct,
                onWarningCallback: () =>
                {
                    // Round 3のタメ予兆時に「集中顔」をキャプチャ
                    if (_currentRoundIndex == 2 && cameraCapture != null)
                    {
                        cameraCapture.CaptureFocusFace();
                    }
                });

            if (!_isResolved)
            {
                _isResolved = true;
                Records.Add(new RoundRecord
                {
                    roundNumber = _currentRoundIndex + 1,
                    isSuccess = false,
                    diffMs = 9999f,
                    score = 0,
                    timingComment = "2人とも反応できず…！"
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

            bool isCatchable = mosquito.IsCatchable;
            bool isSuccess = isCatchable && isSynced;
            int score = CalculateScore(isSuccess, diffMs);

            string comment = GenerateTimingComment(isSuccess, isCatchable, _p1Time, _p2Time);

            SafePlaySE(SoundManager.SEData.SETYPE.CrystalBreak);

            mosquito.HideInHands();
            SafeSetBGMVolume(0f);

            float currentSuspense = (_currentRoundIndex == 2) ? finalRoundSuspenseDuration : suspenseDuration;

            handLeft.ShiverSuspense(currentSuspense);
            handRight.ShiverSuspense(currentSuspense);
            await UniTask.Delay((int)(currentSuspense * 1000f), cancellationToken: ct);

            SafeSetBGMVolume(1f);

            Records.Add(new RoundRecord
            {
                roundNumber = _currentRoundIndex + 1,
                isSuccess = isSuccess,
                diffMs = diffMs,
                score = score,
                timingComment = comment
            });

            if (isSuccess)
            {
                await OnPerfectAsync(score, comment, ct);
            }
            else
            {
                await OnTimingMissAsync(comment, ct);
            }
        }

        private string GenerateTimingComment(bool isSuccess, bool isCatchable, float p1Time, float p2Time)
        {
            if (p1Time < 0 || p2Time < 0) return "2人とも反応できず…！";

            if (isSuccess)
            {
                return "バッチリ！";
            }
            else
            {
                float diff = Mathf.Abs(p1Time - p2Time);
                float diffMs = diff * 1000f;
                bool isP1Early = p1Time < p2Time;

                if (!isCatchable)
                {
                    if (diffMs <= 30f) return "息ピッタリなのに…蚊がいない！";
                    return "早すぎた！蚊がまだ来てない！";
                }

                if (diffMs <= 180f) return isP1Early ? "右が少し遅れた！" : "左が少し遅れた！";
                if (diffMs <= 350f) return isP1Early ? "右がめっちゃ遅い！" : "左がめっちゃ遅い！";
                return isP1Early ? "左のフライング！(右が大遅刻)" : "右のフライング！(左が大遅刻)";
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

        /// <summary>
        /// 撃退成功演出: 「VICTORY!!」が出てから0.2秒後に歓喜の顔を激写！
        /// </summary>
        private async UniTask OnPerfectAsync(int earnedScore, string comment, CancellationToken ct)
        {
            handLeft.ReturnToInitial(0.25f);
            handRight.ReturnToInitial(0.25f);
            mosquito.Squash();
            ui.ShowResult(true, comment);

            SafePlaySE(SoundManager.SEData.SETYPE.Winner);

            // ★ 「VICTORY!!」の文字が出てから0.2秒（200ms）待つ ★
            await UniTask.Delay(600, cancellationToken: ct);

            // ★ プレイヤーが勝利を認識して喜んだ最高の瞬間を撮影！ ★
            if (cameraCapture != null)
            {
                cameraCapture.CaptureSuccessFace();
            }

            // 残りのリザルト待機時間 (1500ms - 600ms = 900ms)
            await UniTask.Delay(900, cancellationToken: ct);
        }

        /// <summary>
        /// 撃退失敗演出: Round 3で失敗した時のみフォールバック撮影
        /// </summary>
        private async UniTask OnTimingMissAsync(string comment, CancellationToken ct)
        {
            mosquito.Escape();
            handLeft.ReturnToInitial(0.18f);
            handRight.ReturnToInitial(0.18f);

            await UniTask.Delay(180, cancellationToken: ct);
            if (_p1Time < _p2Time) handRight.PlayHurt();
            else handLeft.PlayHurt();

            ui.ShowResult(false, comment);

            SafePlaySE(SoundManager.SEData.SETYPE.PlayerDamage);

            // 一度も成功していない場合の予備撮影（MISS表示の0.2秒後）
            if (_currentRoundIndex == 2 && cameraCapture != null && !cameraCapture.HasSuccessPhoto)
            {
                await UniTask.Delay(200, cancellationToken: ct);
                cameraCapture.CaptureFallbackActionFace();
                await UniTask.Delay(1300, cancellationToken: ct);
            }
            else
            {
                await UniTask.Delay(1500, cancellationToken: ct);
            }
        }

        private async UniTask OnTimeOutAsync(CancellationToken ct)
        {
            await mosquito.CameraAttackAsync(ct);
            ui.ShowTimeOut();

            SafePlaySE(SoundManager.SEData.SETYPE.Loser);

            if (_currentRoundIndex == 2 && cameraCapture != null && !cameraCapture.HasSuccessPhoto)
            {
                await UniTask.Delay(200, cancellationToken: ct);
                cameraCapture.CaptureFallbackActionFace();
                await UniTask.Delay(1300, cancellationToken: ct);
            }
            else
            {
                await UniTask.Delay(1500, cancellationToken: ct);
            }
        }

        #region SoundManager Safe Callbacks
        private void SafePlayBGM(SoundManager.BGMData.BGMTYPE bgm, bool isLoop = true)
        {
            if (SoundManager.Instance == null) return;
            if (!IsBgmValid(bgm)) return;

            try
            {
                SoundManager.Instance.PlayBGM(bgm);
                var field = typeof(SoundManager).GetField("_bgmAudioSource", BindingFlags.NonPublic | BindingFlags.Instance);
                if (field != null)
                {
                    var source = field.GetValue(SoundManager.Instance) as AudioSource;
                    if (source != null) source.loop = isLoop;
                }
            }
            catch { }
        }

        private void SafePlaySE(SoundManager.SEData.SETYPE se)
        {
            if (SoundManager.Instance == null) return;
            if (!IsSeValid(se)) return;

            try
            {
                SoundManager.Instance.PlaySE(se);
            }
            catch { }
        }

        private void SafeSetBGMVolume(float volume)
        {
            if (SoundManager.Instance == null) return;
            try { SoundManager.Instance.SetBGMVolume(volume); } catch { }
        }

        private bool IsBgmValid(SoundManager.BGMData.BGMTYPE bgm)
        {
            try
            {
                var field = typeof(SoundManager).GetField("_bgmSoundDatas", BindingFlags.NonPublic | BindingFlags.Instance);
                if (field == null) return true;
                var list = field.GetValue(SoundManager.Instance) as List<SoundManager.BGMData>;
                if (list == null) return false;
                var item = list.Find(d => d != null && d.bgm == bgm);
                return item != null && item.audioClip != null;
            }
            catch { return false; }
        }

        private bool IsSeValid(SoundManager.SEData.SETYPE se)
        {
            try
            {
                var field = typeof(SoundManager).GetField("_seSoundDatas", BindingFlags.NonPublic | BindingFlags.Instance);
                if (field == null) return true;
                var list = field.GetValue(SoundManager.Instance) as List<SoundManager.SEData>;
                if (list == null) return false;
                var item = list.Find(d => d != null && d.se == se);
                return item != null && item.audioClip != null;
            }
            catch { return false; }
        }
        #endregion

        private void OnDestroy()
        {
            _cts?.Cancel();
            _cts?.Dispose();
        }
    }
}