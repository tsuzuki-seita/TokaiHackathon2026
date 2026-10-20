using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class mmm_MoguraMoveController : MonoBehaviour
{
    // 26匹のモグラを入れる配列
    public GameObject[] moguras = new GameObject[26];

    // 数字を受け取ってモグラを上げる
    public void MoguraUp(int number)
    {
        StartCoroutine(MoguraMove(number));
    }

    void Update()
{
   
}

    // モグラを上げて、0.6秒後に戻す
    IEnumerator MoguraMove(int number)
    {
        GameObject mogura = moguras[number];

        // 元の位置を保存
        Vector3 originalPosition = mogura.transform.position;

        // モグラを上に移動
        mogura.transform.position += new Vector3(0, 1, 0);

        // 0.6秒待つ
        yield return new WaitForSeconds(0.6f);

        // 元の位置に戻す
        mogura.transform.position = originalPosition;
    }
}