using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class FurnitureSetup : MonoBehaviour
{
    // 현재 실제로 표시되고 있는 가구 프리팹
    private GameObject currentVisualPrefab;

    // 현재 사용 중인 FurnitureData
    private FurnitureData currentData;

    public FurnitureData CurrentData
    {
        get { return currentData; }
    }

    public void SetupFurniture(FurnitureData data)
    {
        if (data == null || data.furniturePrefab == null)
            return;

        // -------------------------------------------------
        // 기존 가구 제거
        // -------------------------------------------------

        if (currentVisualPrefab != null)
        {
            // 새 가구를 만드는 순간 기존 가구의 Collider가
            // 배치 판정에 영향을 주지 않도록 비활성화
            currentVisualPrefab.SetActive(false);

            Destroy(currentVisualPrefab);
            currentVisualPrefab = null;
        }

        // -------------------------------------------------
        // 새 가구 생성
        // -------------------------------------------------

        currentVisualPrefab = Instantiate(
            data.furniturePrefab,
            transform.position,
            transform.rotation
        );

        // FurnitureSetup의 자식으로 설정
        currentVisualPrefab.transform.SetParent(transform);

        // 부모의 위치/회전에 정확하게 맞춤
        currentVisualPrefab.transform.localPosition =
            Vector3.zero;

        currentVisualPrefab.transform.localRotation =
            Quaternion.identity;

        // 현재 데이터 저장
        currentData = data;
    }
}
