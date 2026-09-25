using UnityEngine;
using Cysharp.Threading.Tasks;
using System.Threading;
using System.Collections.Generic;

namespace HaeTataki
{
    public class HaeTataki_GameManager : MonoBehaviour
    {
        [Header("Actor References")]
        [SerializeField] private HaeTataki_MosquitoActor mosquito;
        [SerializeField] private HaeTataki_HandActor handLeft;
        [SerializeField] private HaeTataki_HandActor handRight;
        [SerializeField] private HaeTataki_UI ui;

        [Header("Settings")]
        [SerializeField] [Range(0.3f, 3.0f)] private float suspenseDuration = 1.2f;

        // ラウンドごとの難易度パラメータ
        private struct RoundDifficulty
        {
            public float flightDuration;     // 旋回時間
            public float warningDuration;    // 予兆赤点滅時間
            public float catchableDuration;  // チャンス時間
            public float syncTolerance;      // 許容ズレ秒数
        }

        private readonly RoundDifficulty[] _roundSettings = new RoundDifficulty[]
        {
            // Round 1 (初級): ゆったり旋回、許容 150ms
            new RoundDifficulty { flightDuration = 2.2f, warningDuration = 0.60f, catchableDuration = 0.60f, syncTolerance = 0.150f },
            // Round 2 (中級): 速度1.3倍、許容 120ms
            new RoundDifficulty { flightDuration = 1.7f, warningDuration = 0.40f, catchableDuration = 0.45f, syncTolerance = 0.120f },
            // Round 3 (上級): 超特急、予兆一瞬、許容 90ms（激ムズ）
            new RoundDifficulty { flightDuration = 1.2f, warningDuration = 0.25f, catchableDuration = 0.30f, syncTolerance = 0.090f }
        };

        private int _currentRoundIndex = 0;
        private float _p1Time = -1f;
        private float _p2Time = -1f;
        private bool _isResolved = false;
        private CancellationTokenSource _cts;

        // 各ラウンドの演出完了を待機するための同期シグナル
        private UniTaskCompletionSource _roundEndTcs;

        // 各ラウンドの結果記録（Task 2のスコア合算で使用）
        public List<bool> RoundResults { get; private set; } = new List<bool>();

        private void Start() => StartFullGame();

        private void Update()
        {
            // Rキー: 即時リトライ（Round 1からやり直し）
            if (Input.GetKeyDown(KeyCode.R))
            {
                StartFullGame();
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

        private void StartFullGame()
        {
            _cts?.Cancel();
            _cts?.Dispose();
            _cts = new CancellationTokenSource();

            _currentRoundIndex = 0;
            RoundResults.Clear();

            RunThreeRoundsLoopAsync(_cts.Token).Forget();
        }

        /// <summary>
        /// 3ラウンド進行ループ（各ラウンド終了後にスペースキー入力を待つ）
        /// </summary>
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

                // 1. ラウンド開始コールの表示
                string roundTitle = (r == 2) ? "<color=#FF2222><size=55>FINAL ROUND!!</size></color>" : $"<size=55>ROUND {r + 1}</size>";
                ui.ShowReady(roundTitle);
                await UniTask.Delay(1000, cancellationToken: ct);
                ui.HideInstruction();
                await UniTask.Delay(250, cancellationToken: ct);

                // 2. 蚊の飛行開始
                RoundDifficulty diff = _roundSettings[r];
                RunMosquitoFlightAsync(diff, ct).Forget();

                // 3. 判定・タメ・成功/失敗演出が完全に終わるまで待機
                await _roundEndTcs.Task;

                // 4. 次のラウンドがある場合（Round 1, Round 2 終了時）
                if (r < 2)
                {
                    // 「PRESS [SPACE] FOR NEXT ROUND」のガイドを表示
                    ui.AppendNextRoundGuide();

                    // プレイヤーが [SPACE] キーを押すまで待機！
                    await UniTask.WaitUntil(() => Input.GetKeyDown(KeyCode.Space), cancellationToken: ct);

                    // 決定音（安全再生）
                    SafePlaySE(SoundManager.SEData.SETYPE.Button);

                    // 画面をリセットして一呼吸（0.4秒）
                    ui.ResetUI();
                    await UniTask.Delay(400, cancellationToken: ct);
                }
            }

            Debug.Log($"【全3ラウンド終了！】 勝敗: {string.Join(", ", RoundResults)}");
        }

        /// <summary>
        /// 蚊の飛行と、叩かれなかった場合の時間切れ処理
        /// </summary>
        private async UniTaskVoid RunMosquitoFlightAsync(RoundDifficulty diff, CancellationToken ct)
        {
            await mosquito.StartFlightSequenceAsync(
                diff.flightDuration,
                diff.warningDuration,
                diff.catchableDuration,
                ct);

            // プレイヤーが叩かずに飛行が終わった場合（時間切れ）
            if (!_isResolved)
            {
                _isResolved = true;
                RoundResults.Add(false);
                await OnTimeOutAsync(ct);
                _roundEndTcs?.TrySetResult(); // 演出完了を通知！
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
            _roundEndTcs?.TrySetResult(); // 演出完了を通知！
        }

        private async UniTask ResolveWithSuspenseAsync(bool isSynced, float diffMs, CancellationToken ct)
        {
            await UniTask.Delay(80, cancellationToken: ct);

            bool isSuccess = mosquito.IsCatchable && isSynced;

            mosquito.HideInHands();

            handLeft.ShiverSuspense(suspenseDuration);
            handRight.ShiverSuspense(suspenseDuration);
            await UniTask.Delay((int)(suspenseDuration * 1000f), cancellationToken: ct);

            if (isSuccess)
            {
                RoundResults.Add(true);
                await OnPerfectAsync(ct);
            }
            else
            {
                RoundResults.Add(false);
                await OnTimingMissAsync(diffMs, ct);
            }
        }

        private async UniTask OnPerfectAsync(CancellationToken ct)
        {
            handLeft.ReturnToInitial(0.25f);
            handRight.ReturnToInitial(0.25f);
            mosquito.Squash();
            ui.ShowResult(true);

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

        /// <summary>
        /// SoundManagerにSEが未登録でもエラー落ちさせない安全再生メソッド
        /// </summary>
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