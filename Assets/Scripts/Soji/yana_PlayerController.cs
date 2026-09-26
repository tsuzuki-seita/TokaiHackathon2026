using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class yana_PlayerController : MonoBehaviour
{
    // 移動スピード
    public float moveSpeed = 5f;

    private Rigidbody rb;
    private Vector3 movement;

    void Start()
    {
        // 3D用のRigidbodyコンポーネントを取得
        rb = GetComponent<Rigidbody>();
    }

    void Update()
    {
        // WASDキーや矢印キーの入力を受け取る（X軸とY軸）
        movement.x = Input.GetAxisRaw("Horizontal"); // 左右
        movement.y = Input.GetAxisRaw("Vertical");   // 上下
        movement.z = 0f; // 奥行き方向は固定
    }

    void FixedUpdate()
    {
        // 3Dの物理演算を使って移動させる
        rb.MovePosition(rb.position + movement * moveSpeed * Time.fixedDeltaTime);
    }
}
