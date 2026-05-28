using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SelectionManager : MonoBehaviour
{
    private Material[] targetMaterials;
    private int originalLayer;
    private const int SELECTED_STENCIL_VALUE = 15;
    private const int DEFAULT_STENCIL_VALUE = 0;

    void Awake()
    {
        // 렌더러에 할당된 모든 머티리얼을 가져옵니다.
        targetMaterials = GetComponent<Renderer>().materials;
        originalLayer = gameObject.layer;
    }

    void OnMouseDown()
    {
        if (gameObject.layer != LayerMask.NameToLayer("Selected"))
        {
            // 선택됨: 레이어를 바꾸고 스텐실 값을 15로 변경
            gameObject.layer = LayerMask.NameToLayer("Selected");
            SetStencilValue(SELECTED_STENCIL_VALUE);
        }
        else
        {
            // 해제됨: 레이어를 되돌리고 스텐실 값을 0으로 변경
            gameObject.layer = originalLayer;
            SetStencilValue(DEFAULT_STENCIL_VALUE);
        }
    }

    void SetStencilValue(int value)
    {
        foreach (var mat in targetMaterials)
        {
            // 쉐이더에서 설정한 스텐실 변수 이름 (_StencilRef)을 확인하세요.
            mat.SetInt("_StencilNo", value);
        }
    }
}
