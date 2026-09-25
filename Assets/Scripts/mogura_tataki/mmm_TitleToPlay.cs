using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class mmm_TitleToPlay: MonoBehaviour
{
    public GameObject titlescreen;
    public GameObject play;
    // Start is called before the first frame update
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        if (Input.anyKeyDown)
        {
            titlescreen.SetActive(false);
            play.SetActive(true);
        }
    }
}
