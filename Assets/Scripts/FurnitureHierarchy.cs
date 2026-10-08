using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[DisallowMultipleComponent]
public class FurnitureHierarchy : MonoBehaviour
{
    // =========================================================
    // 부모 가구에서 분리
    // =========================================================

    public void DetachFromParent()
    {
        // true를 사용해서 현재 월드 위치 / 회전 / 스케일 유지
        transform.SetParent(null, true);

        Physics.SyncTransforms();
    }

    // =========================================================
    // 아래 가구에 부모 연결
    // =========================================================

    public bool AttachTo(FurnitureHierarchy belowFurniture)
    {
        // 자기 자신 위에는 놓을 수 없음
        if (belowFurniture == this)
        {
            Debug.LogWarning(
                "자기 자신을 자기 자신 위에 놓을 수 없습니다."
            );

            return false;
        }

        // 자신의 자식 위에는 놓을 수 없음
        //
        // 예:
        // A
        // └ B
        //    └ C
        //
        // A를 C 위에 놓는 경우를 방지
        if (belowFurniture != null &&
            belowFurniture.transform.IsChildOf(transform))
        {
            Debug.LogWarning(
                "자신의 자식 가구 위에는 놓을 수 없습니다."
            );

            return false;
        }

        // 아래에 가구가 있으면 그 가구를 부모로 설정
        if (belowFurniture != null)
        {
            transform.SetParent(
                belowFurniture.transform,
                true
            );
        }
        else
        {
            // 아래에 가구가 없으면 바닥에 놓인 것으로 처리
            transform.SetParent(
                null,
                true
            );
        }

        Physics.SyncTransforms();

        return true;
    }
}