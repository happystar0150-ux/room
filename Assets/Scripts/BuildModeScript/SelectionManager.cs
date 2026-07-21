using System.Collections;
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.EventSystems;

public class SelectionManager : MonoBehaviour
{
    private List<Material> targetMaterials = new List<Material>();
    private int originalLayer;
    private const int SELECTED_STENCIL_VALUE = 15;
    private const int DEFAULT_STENCIL_VALUE = 0;

    void Awake()
    {
        
        
        //기본 레이어 기억
        originalLayer = gameObject.layer;
    }

    public void RefreshMaterials()
    {
        targetMaterials.Clear();

        Renderer[] renderer = GetComponentsInChildren<Renderer>();
        
        foreach (Renderer rend in renderer)
        {
            if (rend != null)
            {
                // 자식 가구들이 가진 머티리얼을 리스트에 담아요
                targetMaterials.AddRange(rend.materials);
            }
        }
    }

    public void OnSelectedByClick()
    {
        // 현재 마우스 커서가 UI 위에 있다면 3D 클릭을 무시
        if (EventSystem.current.IsPointerOverGameObject())
            return;

        // 건축 모드일 때만 작동
        if (GameManager.Instance == null || GameManager.Instance.currentMode != GameMode.Build)
            return;

        // 이미 선택 중이라면 새로운 클릭 차단
        if (GameManager.Instance.IsAlreadyEditing(this.transform))
            return;

        // 이동 모드일 때는 최초 선택 로직 타지 않고 드래고 허용
        ObjectDrag dragScript = GetComponent<ObjectDrag>();
        if (dragScript != null && dragScript.isMoveMode)
            return;

        // 이미 선택된 오브젝트 중복 처리 방지
        if (gameObject.layer == LayerMask.NameToLayer("Selected"))
            return;

        // 클릭 되었으므로 머티리얼 리스트를 최신 가구 외형 기준으로 갱신
        RefreshMaterials();


        // 처음 선택시 작동
        // 레이어 변경
        SetLayerRecursively(this.gameObject, LayerMask.NameToLayer("Selected"));
        // 외곽라인 생성
        SetStencilValue(SELECTED_STENCIL_VALUE);

        // GameManager에 선택 알림
        GameManager.Instance.SelectionObject(this.transform);


    }

    public void SetStencilValue(int value)
    {
        // 머티리얼이 비어있다면 한 번 갱신
        if (targetMaterials.Count == 0)
            RefreshMaterials();

        foreach (var mat in targetMaterials)
        {
            if (mat != null)
            {
                // 쉐이더에서 설정한 스텐실 변수 이름 (_StencilRef)을 확인하세요.
                mat.SetInt("_StencilNo", value);
            }
        }
    }

    public void ResetSelection()
    {
        //선택 해제 시 원래 레이어로 복구
        SetLayerRecursively(this.gameObject, originalLayer);
        SetStencilValue(DEFAULT_STENCIL_VALUE);
    }

    // 오브젝트의 하위 자식들까지 레이어를 싹 바꿔주는 함수
    private void SetLayerRecursively(GameObject obj, int newLayer)
    {
        if (obj == null) return;
        obj.layer = newLayer;
        foreach (Transform child in obj.transform)
        {
            if (child == null) continue;
            SetLayerRecursively(child.gameObject, newLayer);
        }
    }
}
