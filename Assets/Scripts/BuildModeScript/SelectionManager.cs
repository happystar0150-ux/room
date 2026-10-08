using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;

public class SelectionManager : MonoBehaviour
{
    private List<Material> targetMaterials = new List<Material>();

    // 각 오브젝트의 원래 Layer를 저장
    private Dictionary<GameObject, int> originalLayers =
        new Dictionary<GameObject, int>();

    private const int SELECTED_STENCIL_VALUE = 15;
    private const int DEFAULT_STENCIL_VALUE = 0;

    private int selectedLayer;

    private void Awake()
    {
        selectedLayer = LayerMask.NameToLayer("Selected");
    }

    // =========================================================
    // Material 캐시
    // =========================================================

    public void RefreshMaterials()
    {
        targetMaterials.Clear();

        Renderer[] renderers = GetComponentsInChildren<Renderer>();

        foreach (Renderer rend in renderers)
        {
            if (rend == null)
            {
                continue;
            }

            targetMaterials.AddRange(rend.materials);
        }
    }

    // =========================================================
    // 클릭해서 선택
    // =========================================================

    public void OnSelectedByClick()
    {
        // UI 위를 클릭했다면 무시
        if (EventSystem.current != null &&
            EventSystem.current.IsPointerOverGameObject())
        {
            return;
        }

        // Build 모드에서만 선택
        if (GameManager.Instance == null ||
            GameManager.Instance.currentMode != GameMode.Build)
        {
            return;
        }

        // 다른 가구를 이미 편집 중이면 무시
        if (GameManager.Instance.IsAlreadyEditing(transform))
        {
            return;
        }

        // 이동 모드 중이면 무시
        ObjectDrag dragScript = GetComponent<ObjectDrag>();

        if (dragScript != null && dragScript.isMoveMode)
        {
            return;
        }

        // 선택 적용
        ApplySelection();

        // GameManager에 선택 알림
        GameManager.Instance.SelectionObject(transform);
    }

    // =========================================================
    // 선택 적용
    // =========================================================

    public void ApplySelection()
    {
        // 선택하기 전에 원래 Layer 저장
        if (originalLayers.Count == 0)
        {
            SaveOriginalLayers(gameObject);
        }

        // Material 다시 캐시
        RefreshMaterials();

        // Selected Layer 적용
        SetLayerRecursively(gameObject, selectedLayer);

        // Stencil 적용
        SetStencilValue(SELECTED_STENCIL_VALUE);
    }

    // =========================================================
    // 원래 Layer 저장
    // =========================================================

    private void SaveOriginalLayers(GameObject obj)
    {
        if (obj == null)
        {
            return;
        }

        if (!originalLayers.ContainsKey(obj))
        {
            originalLayers.Add(obj, obj.layer);
        }

        foreach (Transform child in obj.transform)
        {
            if (child == null)
            {
                continue;
            }

            SaveOriginalLayers(child.gameObject);
        }
    }

    // =========================================================
    // Stencil
    // =========================================================

    public void SetStencilValue(int value)
    {
        if (targetMaterials.Count == 0)
        {
            RefreshMaterials();
        }

        foreach (Material mat in targetMaterials)
        {
            if (mat == null)
            {
                continue;
            }

            mat.SetInt("_StencilNo", value);
        }
    }

    // =========================================================
    // 선택 해제
    // =========================================================

    public void ResetSelection()
    {
        // 각 오브젝트를 원래 Layer로 복구
        foreach (KeyValuePair<GameObject, int> pair in originalLayers)
        {
            if (pair.Key == null)
            {
                continue;
            }

            pair.Key.layer = pair.Value;
        }

        // Stencil 초기화
        SetStencilValue(DEFAULT_STENCIL_VALUE);

        // 저장된 Layer 정보 초기화
        originalLayers.Clear();
    }

    // =========================================================
    // Layer 변경
    // =========================================================

    private void SetLayerRecursively(GameObject obj, int newLayer)
    {
        if (obj == null)
        {
            return;
        }

        int surfaceLayer = LayerMask.NameToLayer("FurnitureSurface");

        // FurnitureSurface는 건드리지 않음
        if (obj.layer != surfaceLayer)
        {
            obj.layer = newLayer;
        }

        foreach (Transform child in obj.transform)
        {
            if (child == null)
            {
                continue;
            }

            SetLayerRecursively(child.gameObject, newLayer);
        }
    }
}