using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlayerDamage : MonoBehaviour
{
    // ゴミやカーソルに接触したときの判定（Is Trigger用）
    void OnTriggerEnter(Collider other)
    {
        // 触れた相手が「TrashItem（ゴミ）」または「CleanerCursor（掃除カーソル）」の場合
        if (other.gameObject.CompareTag("TrashItem") || other.gameObject.CompareTag("CleanerCursor"))
        {
            Debug.Log("【ゲームオーバー】ゴミかカーソルが会社員に当たってしまいました！");
            
            // ゲームを一時停止する
            Time.timeScale = 0f;
        }
    }
}
