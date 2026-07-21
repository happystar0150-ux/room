using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class FurnitureSetup : MonoBehaviour
{
    // 현재 생성되어 있는 가구 외형을 기억할 변수
    private GameObject currentVisualPrefab;

    public void SetupFurniture(FurnitureData data)
    {
        if (data == null || data.furniturePrefab == null) return;

        // 기존 가구 외형 삭제
        foreach (Transform child in transform)
        {
            Destroy(child.gameObject);
        }

        // 새로운 가구 프리팹을 자식으로 생성
        GameObject spawnedPrefab = Instantiate(data.furniturePrefab, transform.position, transform.rotation);
        spawnedPrefab.transform.SetParent(transform);

        /*
        // 새로운 프리팹을 뼈대와 같은 위치 / 회전값 으로 생성
        currentVisualPrefab = Instantiate(data.furniturePrefab, transform.position, transform.rotation);

        // 생성한 프리팹을 자식으로 넣어쥼
        currentVisualPrefab.transform.SetParent(transform);
        */

    }
}
