using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class yana_Sweeper : MonoBehaviour
{
    private Camera mainCamera;
    private Rigidbody rb;
    private Vector3 cursorVelocity; // カーソルの動く方向と速度

    void Start()
    {
        mainCamera = Camera.main;
        rb = GetComponent<Rigidbody>();
    }

    void FixedUpdate()
    {
        // 1. マウスの位置に滑らかに追従する処理
        Vector3 mouseScreenPosition = Input.mousePosition;
        mouseScreenPosition.z = 10f;
        Vector3 targetPosition = mainCamera.ScreenToWorldPoint(mouseScreenPosition);

        // カーソルが「どの方向に、どれくらいの勢いで動いているか」を計算する
        cursorVelocity = (targetPosition - transform.position) / Time.fixedDeltaTime;

        Vector3 moveDirection = (targetPosition - transform.position);
        rb.velocity = moveDirection * 20f; 
    }

    // 2. マウスの動いた方向に向かってゴミを弾き飛ばす処理
    void OnTriggerStay(Collider other)
    {
        if (other.gameObject.name.Contains("TrashItem"))
        {
            Rigidbody trashRb = other.gameObject.GetComponent<Rigidbody>();
            if (trashRb != null)
            {
                // カーソルの動いている方向を飛ばす方向にする（動いていないときは外側へ逃がす）
                Vector3 sweepDirection = cursorVelocity.normalized;
                if (sweepDirection == Vector3.zero)
                {
                    sweepDirection = (other.transform.position - transform.position).normalized;
                }

                // マウスを動かす速さに応じて飛ばす強さを変える（強すぎないように制限）
                float power = Mathf.Min(cursorVelocity.magnitude * 0.4f, 12f);
                power = Mathf.Max(power, 3f); // 最低限の力

                trashRb.AddForce(sweepDirection * power, ForceMode.Impulse);
            }
        }
    }
}
