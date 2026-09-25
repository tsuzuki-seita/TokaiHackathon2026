using UnityEngine;
using UnityEngine.UI; // ← TMPro の代わりに標準UIを使用

namespace HaeTataki
{
    public class HaeTataki_UI : MonoBehaviour
    {
        [SerializeField] private Text instructionText; // ← TextMeshProUGUI から Text に変更
        [SerializeField] private GameObject bloodSplashOverlay;

        public void ShowReady(string text = "挟め！")
        {
            if (instructionText != null)
            {
                instructionText.gameObject.SetActive(true);
                instructionText.text = text;
            }
        }

        public void HideInstruction()
        {
            if (instructionText != null) instructionText.gameObject.SetActive(false);
        }

        public void ShowResult(bool isSuccess, float diffMs = 0f)
        {
            if (instructionText != null)
            {
                instructionText.gameObject.SetActive(true);
                // レガシーTextでも color や size タグはそのまま有効です
                instructionText.text = isSuccess
                    ? "<color=#FFDD00>PERFECT!!</color>\n<size=40>完全撃退！ +100pt</size>"
                    : $"<color=#FF4444>MISS!!</color>\n<size=35>ズレ: {diffMs:F0}ms</size>";
            }
        }

        public void ShowTimeOut()
        {
            if (instructionText != null)
            {
                instructionText.gameObject.SetActive(true);
                instructionText.text = "<color=#990000>TIME OVER...</color>\n<size=35>血を吸われてしまった！</size>";
            }
            if (bloodSplashOverlay != null) bloodSplashOverlay.SetActive(true);
        }

        public void ResetUI()
        {
            if (instructionText != null) instructionText.gameObject.SetActive(false);
            if (bloodSplashOverlay != null) bloodSplashOverlay.SetActive(false);
        }
    }
}