using UnityEngine;
using Cysharp.Threading.Tasks;
using System.Threading;

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
        [SerializeField] private float syncTolerance = 0.15f; // 許容差 150ms
        [SerializeField] [Range(0.3f, 3.0f)] private float suspenseDuration = 1.2f; // ← 追加！タメ秒

        private float _p1Time = -1f;
        private float _p2Time = -1f;
        private bool _isResolved = false;
        private CancellationTokenSource _cts;

        private void Start() => StartNewGame();

        private void Update()
        {
            // Rキー: 即時リトライ
            if (Input.GetKeyDown(KeyCode.R))
            {
                StartNewGame();
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

            // Spaceキー（デバッグ用同時押し）
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

        private void StartNewGame()
        {
            _cts?.Cancel();
            _cts?.Dispose();
            _cts = new CancellationTokenSource();

            _p1Time = -1f;
            _p2Time = -1f;
            _isResolved = false;

            handLeft.ResetState();
            handRight.ResetState();
            mosquito.ResetState();
            ui.ResetUI();

            RunGameLoopAsync(_cts.Token).Forget();
        }

        private async UniTask RunGameLoopAsync(CancellationToken ct)
        {
            ui.ShowReady("挟め！");
            await UniTask.Delay(800, cancellationToken: ct);
            ui.HideInstruction();

            await mosquito.StartFlightSequenceAsync(ct);

            if (!_isResolved)
            {
                _isResolved = true;
                await OnTimeOutAsync(ct);
            }
        }

        private void CheckInputTiming()
        {
            if (_p1Time > 0f && _p2Time > 0f)
            {
                _isResolved = true;
                float diff = Mathf.Abs(_p1Time - _p2Time);
                float diffMs = diff * 1000f;

                // ドキドキ判定シーケンスを開始！
                ResolveWithSuspenseAsync(diff <= syncTolerance, diffMs, _cts.Token).Forget();
            }
        }

                /// <summary>
        /// ドキドキのタメ時間を作ってから結果を開示する
        /// </summary>
        private async UniTask ResolveWithSuspenseAsync(bool isSynced, float diffMs, CancellationToken ct)
        {
            // 1. スウィングが中央に届くまで待機（約80ms）
            await UniTask.Delay(80, cancellationToken: ct);

            bool isSuccess = mosquito.IsCatchable && isSynced;

            // 2. 蚊を手の中に完全に隠し、羽音をピタッと消して静寂をつくる！
            mosquito.HideInHands();

            // 3. 設定秒数だけ両手がプルプル震えてタメる！
            handLeft.ShiverSuspense(suspenseDuration);
            handRight.ShiverSuspense(suspenseDuration);
            await UniTask.Delay((int)(suspenseDuration * 1000f), cancellationToken: ct);

            // 4. 結果開示！
            if (isSuccess)
            {
                await OnPerfectAsync(ct);
            }
            else
            {
                await OnTimingMissAsync(diffMs, ct);
            }
        }
        private async UniTask OnPerfectAsync(CancellationToken ct)
        {
            // 1. まず両手をパッと開いて離す（蚊がしっかり見えるように！）
            handLeft.ReturnToInitial(0.25f);
            handRight.ReturnToInitial(0.25f);

            // 2. 手の中から蚊が現れ、ヨロヨロと下に落ちていく！
            mosquito.Squash();
            ui.ShowResult(true);

            if (SoundManager.Instance != null)
            {
                SoundManager.Instance.PlaySE(SoundManager.SEData.SETYPE.Winner);
            }

            await UniTask.Delay(1500, cancellationToken: ct);
        }
                private async UniTask OnTimingMissAsync(float diffMs, CancellationToken ct)
        {
            mosquito.Escape(); // 蚊がドヤ顔で飛び去る！

            // 1. パッと手を左右に開いて離す（中央の重なりを解消！）
            handLeft.ReturnToInitial(0.18f);
            handRight.ReturnToInitial(0.18f);

            // 2. 手が離れた直後に、遅かった方の手だけが痛がって真っ赤に痺れる！
            await UniTask.Delay(180, cancellationToken: ct);
            if (_p1Time < _p2Time)
            {
                handRight.PlayHurt();
            }
            else
            {
                handLeft.PlayHurt();
            }

            ui.ShowResult(false, diffMs);

            if (SoundManager.Instance != null)
            {
                SoundManager.Instance.PlaySE(SoundManager.SEData.SETYPE.PlayerDamage);
            }

            await UniTask.Delay(1500, cancellationToken: ct);
        }

        private async UniTask OnTimeOutAsync(CancellationToken ct)
        {
            await mosquito.CameraAttackAsync(ct);
            ui.ShowTimeOut();

            if (SoundManager.Instance != null)
            {
                SoundManager.Instance.PlaySE(SoundManager.SEData.SETYPE.Loser);
            }

            await UniTask.Delay(1500, cancellationToken: ct);
        }

        private void OnDestroy()
        {
            _cts?.Cancel();
            _cts?.Dispose();
        }
    }
}