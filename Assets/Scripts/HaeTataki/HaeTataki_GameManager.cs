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
        [SerializeField] private float syncTolerance = 0.15f; // 許容差 150ms (0.15秒)

        private float _p1Time = -1f;
        private float _p2Time = -1f;
        private bool _isResolved = false;
        private CancellationTokenSource _cts;

        private void Start() => StartNewGame();

        private void Update()
        {
            // Rキー: 即時リトライ（デバッグ・練習用）
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
                handLeft.Swing();
                CheckInputTiming();
            }

            // Player 2 (Lキー)
            if (Input.GetKeyDown(KeyCode.L) && _p2Time < 0f)
            {
                _p2Time = Time.time;
                handRight.Swing();
                CheckInputTiming();
            }

            // Spaceキー: 一人で検証できるデバッグ同時押し
            #if UNITY_EDITOR || DEVELOPMENT_BUILD
            if (Input.GetKeyDown(KeyCode.Space) && _p1Time < 0f && _p2Time < 0f)
            {
                _p1Time = Time.time;
                _p2Time = Time.time;
                handLeft.Swing();
                handRight.Swing();
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

            // チャンス中に叩かれなかった場合は時間切れ
            if (!_isResolved)
            {
                _isResolved = true;
                await OnTimeOutAsync(ct);
            }
        }

        private void CheckInputTiming()
        {
            // 両方のキーが押されたら判定
            if (_p1Time > 0f && _p2Time > 0f)
            {
                _isResolved = true;
                float diff = Mathf.Abs(_p1Time - _p2Time);
                float diffMs = diff * 1000f;

                if (mosquito.IsCatchable && diff <= syncTolerance)
                {
                    OnPerfectAsync(_cts.Token).Forget();
                }
                else
                {
                    OnTimingMissAsync(diffMs, _cts.Token).Forget();
                }
            }
        }

        private async UniTask OnPerfectAsync(CancellationToken ct)
        {
            mosquito.Squash();
            ui.ShowResult(true);
            await UniTask.Delay(1500, cancellationToken: ct);
        }

        private async UniTask OnTimingMissAsync(float diffMs, CancellationToken ct)
        {
            mosquito.Escape();
            // 入力が遅かった方の手が叩かれて痛がる
            if (_p1Time < _p2Time) handRight.PlayHurt();
            else handLeft.PlayHurt();

            ui.ShowResult(false, diffMs);
            await UniTask.Delay(1500, cancellationToken: ct);
        }

        private async UniTask OnTimeOutAsync(CancellationToken ct)
        {
            await mosquito.CameraAttackAsync(ct);
            ui.ShowTimeOut();
            await UniTask.Delay(1500, cancellationToken: ct);
        }

        private void OnDestroy()
        {
            _cts?.Cancel();
            _cts?.Dispose();
        }
    }
}