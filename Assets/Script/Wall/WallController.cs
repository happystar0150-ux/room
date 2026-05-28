using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class WallController : MonoBehaviour {
    [Header("벽 조각")]
    public GameObject wallFull;
    public GameObject wallTop;
    public GameObject wallBottom;
    public GameObject wallLeft;
    public GameObject wallRight;

    [Header("창문")]
    public WindowController window;

    private Bounds wallBounds;

    void Start() {
        wallBounds = wallFull.GetComponent<Renderer>().bounds;
    }

    public void SetWindowActive(bool active) {
        wallFull.SetActive(!active);
        wallTop.SetActive(active);
        wallBottom.SetActive(active);
        wallLeft.SetActive(active);
        wallRight.SetActive(active);
        window.gameObject.SetActive(active);


    }
}