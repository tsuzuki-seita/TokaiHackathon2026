using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class yana_MouseFollower : MonoBehaviour
{
    private Camera mainCamera;

    void Start()
    {
        // メインカメラを取得しておく
        mainCamera = Camera.main;
    }

    void Update()
    {
        // マウスの画面上の位置を取得し、ゲームの世界の座標（3D空間）に変換する
        Vector3 mouseScreenPosition = Input.mousePosition;
        
        // カメラからオブジェクトまでの距離（Z軸の調整）を指定
        mouseScreenPosition.z = 10f; 
        Vector3 mouseWorldPosition = mainCamera.ScreenToWorldPoint(mouseScreenPosition);

        // マウスの位置にこのオブジェクトを移動させる
        transform.position = mouseWorldPosition;
    }
}
