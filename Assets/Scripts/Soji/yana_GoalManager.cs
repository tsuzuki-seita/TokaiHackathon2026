using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class yana_GoalManager : MonoBehaviour
{
    // 会社員がゴールに触れたときの処理
    void OnTriggerEnter(Collider other)
    {
        // 触ってきた相手の名前が「PlayerEmployee」の場合
        if (other.gameObject.CompareTag("Player"))
        {
            Debug.Log("【クリア！】おめでとうございます！会社に到着しました！");
            
            // ゲームを一時停止する（またはクリア演出）
            Time.timeScale = 0f; 
        }
    }
}
