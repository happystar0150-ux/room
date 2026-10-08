using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;

public class CategoryButtonData
{
    public string categoryName;
    public Image buttonImage;
    public Sprite normalSprite;
    public Sprite selectedSprite;
}

public class GameManager : MonoBehaviour
{
    // =========================================================
    // 현재 모드
    // =========================================================

    public GameMode currentMode =
        GameMode.Normal;

    // =========================================================
    // UI Panels
    // =========================================================

    [Header("UI Panels")]

    public GameObject normalUIPanel;
    public GameObject buildUIPanel;
    public GameObject editUIPanel;
    public GameObject moveUIPanel;
    public GameObject addUIPanel;
    public GameObject deleteConfirmPopUP;
    public GameObject restartConfirmPopUP;

    // =========================================================
    // Furniture Spawn System
    // =========================================================

    [Header("Furniture Spawn System")]

    public GameObject commonFurniturePrefab;

    // 현재 미리보기 가구
    [Header("현재 가구")]

    public GameObject currentSpawnedObject;

    // 현재 선택된 FurnitureData
    private FurnitureData currentFurnitureData;

    // =========================================================
    // Camera
    // =========================================================

    [Header("Camera Reference")]

    public Transform cameraTransform;

    public Vector3 cameraOffset =
        new Vector3(0f, 5f, -5f);

    // 현재 선택된 실제 가구
    private Transform selectedTarget;

    // 카메라 위치
    private Vector3 normalCameraPosition;
    private Quaternion normalCameraRotation;

    private Vector3 buildCameraPosition;
    private Quaternion buildCameraRotation;

    private Coroutine cameraMoveCoroutine;

    // =========================================================
    // Singleton
    // =========================================================

    public static GameManager Instance
    {
        get;
        private set;
    }

    // =========================================================
    // Furniture Data
    // =========================================================

    [Header("가구 데이터")]

    public List<FurnitureData> allFurnitureDataList =
        new List<FurnitureData>();

    public GameObject furnitureItemPrefab;

    public Transform furnitureContentParent;

    // =========================================================
    // Warning Popup
    // =========================================================

    [Header("UI 팝업")]

    public GameObject warningPopupPanel;

    public TMPro.TMP_Text warningText;

    private Coroutine warningCoroutine;

    // =========================================================
    // Category Button
    // =========================================================

    [Header("카테고리 버튼")]

    public List<CategoryButtonData> categoryButtonList =
        new List<CategoryButtonData>();

    // =========================================================
    // 자동 배치
    // =========================================================

    [Header("자동 가구 배치")]

    [SerializeField]
    private BoxCollider placementSearchArea;

    [Tooltip("자동 위치 탐색 간격")]
    [SerializeField]
    private float autoSearchStep = 0.5f;

    [Tooltip("표면을 찾기 위해 위에서 쏘는 Ray 높이")]
    [SerializeField]
    private float surfaceRayHeight = 20f;

    [Tooltip("이 값 이상이면 충분히 평평한 윗면으로 인정")]
    [SerializeField]
    private float minSurfaceNormalY = 0.7f;

    // =========================================================
    // Awake
    // =========================================================

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
        }
        else
        {
            Destroy(gameObject);
        }
    }

    // =========================================================
    // Update
    // =========================================================

    private void Update()
    {
        // Build 모드가 아니면 선택 처리 안 함
        if (currentMode != GameMode.Build)
            return;

        if (!Input.GetMouseButtonDown(0))
            return;

        // UI 클릭은 무시
        if (EventSystem.current != null &&
            EventSystem.current.IsPointerOverGameObject())
        {
            return;
        }

        // 가구 선택
        HandleFurnitureSelectionClick();
    }

    // =========================================================
    // 가구 선택 클릭
    // =========================================================

    private void HandleFurnitureSelectionClick()
    {
        if (Camera.main == null)
            return;

        Ray ray =
            Camera.main.ScreenPointToRay(
                Input.mousePosition
            );

        RaycastHit[] hits =
            Physics.RaycastAll(
                ray,
                500f
            );

        // 카메라에서 가까운 순서로 정렬
        System.Array.Sort(
            hits,
            (a, b) =>
                a.distance.CompareTo(b.distance)
        );

        foreach (RaycastHit hit in hits)
        {
            if (hit.transform == null)
                continue;

            Debug.Log(
                $"클릭 Ray가 맞은 오브젝트: {hit.transform.name}"
            );

            SelectionManager selManager =
                hit.transform.GetComponentInParent<SelectionManager>();

            if (selManager == null)
                continue;

            Debug.Log(
                $"가구 선택 성공: {selManager.gameObject.name}"
            );

            selManager.OnSelectedByClick();

            return;
        }

        Debug.Log(
            "Raycast에 SelectionManager가 있는 가구가 없습니다."
        );
    }

    // =========================================================
    // Start
    // =========================================================

    private void Start()
    {
        if (cameraTransform != null)
        {
            normalCameraPosition =
                cameraTransform.position;

            normalCameraRotation =
                cameraTransform.rotation;

            buildCameraPosition =
                cameraTransform.position;

            buildCameraRotation =
                cameraTransform.rotation;
        }

        ChangeMode(GameMode.Normal);

        if (deleteConfirmPopUP != null)
            deleteConfirmPopUP.SetActive(false);

        if (moveUIPanel != null)
            moveUIPanel.SetActive(false);

        if (restartConfirmPopUP != null)
            restartConfirmPopUP.SetActive(false);
    }

    // =========================================================
    // 모드 변경
    // =========================================================

    public void ChangeMode(GameMode newMode)
    {
        currentMode = newMode;

        switch (currentMode)
        {
            case GameMode.Normal:

                if (normalUIPanel != null)
                    normalUIPanel.SetActive(true);

                if (buildUIPanel != null)
                    buildUIPanel.SetActive(false);

                if (editUIPanel != null)
                    editUIPanel.SetActive(false);

                if (addUIPanel != null)
                    addUIPanel.SetActive(false);

                if (cameraMoveCoroutine != null)
                    StopCoroutine(cameraMoveCoroutine);

                cameraMoveCoroutine =
                    StartCoroutine(
                        MoveCameraToCoords(
                            normalCameraPosition,
                            normalCameraRotation
                        )
                    );

                break;

            case GameMode.Build:

                if (normalUIPanel != null)
                    normalUIPanel.SetActive(false);

                if (buildUIPanel != null)
                    buildUIPanel.SetActive(true);

                if (editUIPanel != null)
                    editUIPanel.SetActive(false);

                if (addUIPanel != null)
                    addUIPanel.SetActive(false);

                break;

            case GameMode.Add:

                if (normalUIPanel != null)
                    normalUIPanel.SetActive(false);

                if (buildUIPanel != null)
                    buildUIPanel.SetActive(false);

                if (editUIPanel != null)
                    editUIPanel.SetActive(false);

                if (addUIPanel != null)
                    addUIPanel.SetActive(true);

                // A 카테고리
                FilterFurnitureMenu("A");

                break;
        }
    }

    // =========================================================
    // 새 가구 생성
    // =========================================================

    public void ClickSpawnButtonAtCenter(
        FurnitureData data)
    {
        if (data == null)
            return;

        if (commonFurniturePrefab == null)
        {
            Debug.LogError(
                "commonFurniturePrefab이 설정되지 않았습니다."
            );

            return;
        }

        // -----------------------------------------------------
        // 기본 위치 / 회전
        // -----------------------------------------------------

        Vector3 preferredPosition =
            new Vector3(
                0.5f,
                0f,
                0f
            );

        Quaternion targetRotation =
            Quaternion.Euler(
                -90f,
                0f,
                0f
            );

        // 기존 미리보기 가구가 있다면
        // 현재 위치에서 가구 종류만 바꾸기 위해 위치를 기억
        GameObject oldObject =
            currentSpawnedObject;

        FurnitureData oldData =
            currentFurnitureData;

        if (oldObject != null)
        {
            preferredPosition =
                oldObject.transform.position;

            targetRotation =
                oldObject.transform.rotation;

            // 기존 미리보기 비활성화
            oldObject.SetActive(false);
        }

        // -----------------------------------------------------
        // 새 가구 생성
        // -----------------------------------------------------

        currentSpawnedObject =
            Instantiate(
                commonFurniturePrefab,
                preferredPosition,
                targetRotation
            );

        FurnitureSetup setup =
            currentSpawnedObject
                .GetComponent<FurnitureSetup>();

        if (setup == null)
        {
            Debug.LogError(
                "commonFurniturePrefab에 FurnitureSetup이 없습니다."
            );

            Destroy(currentSpawnedObject);

            currentSpawnedObject = null;

            if (oldObject != null)
            {
                oldObject.SetActive(true);
                currentSpawnedObject =
                    oldObject;
            }

            return;
        }

        setup.SetupFurniture(data);

        // -----------------------------------------------------
        // ObjectDrag 찾기
        // -----------------------------------------------------

        ObjectDrag drag =
            currentSpawnedObject
                .GetComponent<ObjectDrag>();

        if (drag == null)
        {
            Debug.LogError(
                "commonFurniturePrefab에 ObjectDrag가 없습니다."
            );

            Destroy(currentSpawnedObject);

            currentSpawnedObject = null;

            if (oldObject != null)
            {
                oldObject.SetActive(true);

                currentSpawnedObject =
                    oldObject;

                currentFurnitureData =
                    oldData;
            }

            return;
        }

        // 새 가구의 Renderer 다시 캐시
        drag.CacheRenderers();

        Physics.SyncTransforms();

        // -----------------------------------------------------
        // 빈 위치 탐색
        // -----------------------------------------------------

        bool foundPosition =
            TryFindSpawnPosition(
                drag,
                preferredPosition,
                targetRotation,
                out Vector3 spawnPosition
            );

        // -----------------------------------------------------
        // 성공
        // -----------------------------------------------------

        if (foundPosition)
        {
            currentSpawnedObject.transform.position =
                spawnPosition;

            currentSpawnedObject.transform.rotation =
                targetRotation;

            Physics.SyncTransforms();

            // 기존 미리보기 삭제
            if (oldObject != null)
            {
                Destroy(oldObject);
            }

            currentFurnitureData =
                data;

            ChangeMode(GameMode.Add);

            return;
        }

        // -----------------------------------------------------
        // 실패
        // -----------------------------------------------------

        Destroy(currentSpawnedObject);

        currentSpawnedObject = null;

        // 기존 가구가 있었다면 복구
        if (oldObject != null)
        {
            oldObject.SetActive(true);

            currentSpawnedObject =
                oldObject;

            currentFurnitureData =
                oldData;

            Debug.Log(
                "새 가구를 놓을 수 없어 기존 가구를 유지합니다."
            );
        }
        else
        {
            currentFurnitureData = null;
        }

        ShowWarningPopup(
            "이 가구를 놓을 수 있는 공간이 없습니다."
        );
    }

    // =========================================================
    // 가구 자동 위치 탐색
    // =========================================================

    private bool TryFindSpawnPosition(
        ObjectDrag furniture,
        Vector3 preferredPosition,
        Quaternion targetRotation,
        out Vector3 result)
    {
        result = preferredPosition;

        if (placementSearchArea == null)
        {
            Debug.LogError(
                "Placement Search Area가 설정되지 않았습니다."
            );

            return false;
        }

        if (autoSearchStep <= 0f)
        {
            autoSearchStep = 0.5f;
        }

        Bounds bounds =
            placementSearchArea.bounds;

        // 검색 시작점이 방 범위를 벗어났다면
        // 가장 가까운 지점으로 보정
        Vector3 searchOrigin =
            preferredPosition;

        searchOrigin.x =
            Mathf.Clamp(
                searchOrigin.x,
                bounds.min.x,
                bounds.max.x
            );

        searchOrigin.z =
            Mathf.Clamp(
                searchOrigin.z,
                bounds.min.z,
                bounds.max.z
            );

        // 시작점에서 방 전체를 탐색할 수 있도록
        // 가장 먼 방향까지 필요한 ring 계산
        float maxDistanceX =
            Mathf.Max(
                Mathf.Abs(
                    searchOrigin.x - bounds.min.x
                ),
                Mathf.Abs(
                    bounds.max.x - searchOrigin.x
                )
            );

        float maxDistanceZ =
            Mathf.Max(
                Mathf.Abs(
                    searchOrigin.z - bounds.min.z
                ),
                Mathf.Abs(
                    bounds.max.z - searchOrigin.z
                )
            );

        int maxRing =
            Mathf.CeilToInt(
                Mathf.Max(
                    maxDistanceX,
                    maxDistanceZ
                ) / autoSearchStep
            );

        // -----------------------------------------------------
        // 중앙 → 주변 순서로 탐색
        // -----------------------------------------------------

        for (int ring = 0;
             ring <= maxRing;
             ring++)
        {
            for (int x = -ring;
                 x <= ring;
                 x++)
            {
                for (int z = -ring;
                     z <= ring;
                     z++)
                {
                    // 해당 ring의 테두리만 검사
                    if (Mathf.Max(
                        Mathf.Abs(x),
                        Mathf.Abs(z)
                    ) != ring)
                    {
                        continue;
                    }

                    float candidateX =
                        searchOrigin.x +
                        x * autoSearchStep;

                    float candidateZ =
                        searchOrigin.z +
                        z * autoSearchStep;

                    // 방 범위 밖이면 무시
                    if (candidateX < bounds.min.x ||
                        candidateX > bounds.max.x ||
                        candidateZ < bounds.min.z ||
                        candidateZ > bounds.max.z)
                    {
                        continue;
                    }

                    // 아래에 놓을 수 있는 표면이 있는가?
                    if (!TryGetSurfacePosition(
                        furniture,
                        candidateX,
                        candidateZ,
                        out Vector3 candidatePosition))
                    {
                        continue;
                    }

                    // 다른 가구 / 벽과 겹치는가?
                    if (furniture.IsPositionBlocked(
                        candidatePosition,
                        targetRotation))
                    {
                        continue;
                    }

                    // 모든 조건 통과
                    result =
                        candidatePosition;

                    return true;
                }
            }
        }

        return false;
    }

    // =========================================================
    // 표면 찾기
    // =========================================================

    private bool TryGetSurfacePosition(
        ObjectDrag furniture,
        float x,
        float z,
        out Vector3 result)
    {
        result = Vector3.zero;

        Bounds areaBounds =
            placementSearchArea.bounds;

        // 검색 영역 위쪽에서 아래로 Ray
        Vector3 rayStart =
            new Vector3(
                x,
                areaBounds.max.y +
                    surfaceRayHeight,
                z
            );

        RaycastHit[] hits =
            Physics.RaycastAll(
                rayStart,
                Vector3.down,
                surfaceRayHeight * 2f,
                furniture.placementLayerMask,
                QueryTriggerInteraction.Collide
            );

        Array.Sort(
            hits,
            (a, b) =>
                a.distance.CompareTo(b.distance)
        );

        foreach (var hit in hits)
        {
            // 자기 자신 무시
            if (hit.transform == furniture.transform ||
                hit.transform.IsChildOf(
                    furniture.transform))
            {
                continue;
            }

            // 위쪽을 바라보는 표면만 허용
            if (hit.normal.y <
                minSurfaceNormalY)
            {
                continue;
            }

            result =
                new Vector3(
                    x,
                    hit.point.y,
                    z
                );

            return true;
        }

        return false;
    }

    // =========================================================
    // 가구 메뉴
    // =========================================================

    public void FilterFurnitureMenu(
        string categoryToFilter)
    {
        UpdateCategoryButtonImages(
            categoryToFilter
        );

        // 기존 버튼 삭제
        foreach (Transform child
                 in furnitureContentParent)
        {
            Destroy(child.gameObject);
        }

        // 해당 카테고리 버튼 생성
        foreach (FurnitureData data
                 in allFurnitureDataList)
        {
            if (data == null)
                continue;

            if (data.categoryGroup
                .Trim()
                .Equals(
                    categoryToFilter.Trim(),
                    StringComparison.OrdinalIgnoreCase
                ))
            {
                GameObject newBtn =
                    Instantiate(
                        furnitureItemPrefab,
                        furnitureContentParent
                    );

                FurnitureItemUI itemUI =
                    newBtn.GetComponent<FurnitureItemUI>();

                if (itemUI == null)
                {
                    itemUI =
                        newBtn.GetComponentInChildren<
                            FurnitureItemUI>();
                }

                if (itemUI != null)
                {
                    itemUI.Setup(data);
                }
            }
        }
    }

    // =========================================================
    // 카테고리 이미지
    // =========================================================

    private void UpdateCategoryButtonImages(
        string activeCategory)
    {
        foreach (var btnData
                 in categoryButtonList)
        {
            if (btnData.buttonImage == null)
                continue;

            bool isMatch =
                btnData.categoryName
                    .Trim()
                    .Equals(
                        activeCategory.Trim(),
                        StringComparison.OrdinalIgnoreCase
                    );

            if (isMatch)
            {
                btnData.buttonImage.sprite =
                    btnData.selectedSprite;
            }
            else
            {
                btnData.buttonImage.sprite =
                    btnData.normalSprite;
            }
        }
    }

    // =========================================================
    // 가구 종류 변경
    // =========================================================

    public void SwitchFurnitureData(
        FurnitureData newData)
    {
        if (newData == null)
            return;

        // -----------------------------------------------------
        // 현재 미리보기 가구가 없다면
        // 새 가구를 처음 생성
        // -----------------------------------------------------

        if (currentSpawnedObject == null)
        {
            ClickSpawnButtonAtCenter(
                newData
            );

            return;
        }

        FurnitureSetup setup =
            currentSpawnedObject
                .GetComponent<FurnitureSetup>();

        ObjectDrag drag =
            currentSpawnedObject
                .GetComponent<ObjectDrag>();

        if (setup == null ||
            drag == null)
        {
            Debug.LogWarning(
                "현재 가구에 FurnitureSetup 또는 ObjectDrag가 없습니다."
            );

            return;
        }

        // -----------------------------------------------------
        // 기존 상태 저장
        // -----------------------------------------------------

        FurnitureData oldData =
            setup.CurrentData;

        Vector3 oldPosition =
            currentSpawnedObject.transform.position;

        Quaternion oldRotation =
            currentSpawnedObject.transform.rotation;

        // -----------------------------------------------------
        // 새 가구 적용
        // -----------------------------------------------------

        setup.SetupFurniture(
            newData
        );

        drag.CacheRenderers();

        Physics.SyncTransforms();

        // -----------------------------------------------------
        // 현재 위치부터 새 크기로 다시 판정
        // 안 되면 주변 빈 공간 탐색
        // -----------------------------------------------------

        bool foundPosition =
            TryFindSpawnPosition(
                drag,
                oldPosition,
                oldRotation,
                out Vector3 newPosition
            );

        if (foundPosition)
        {
            currentSpawnedObject.transform.position =
                newPosition;

            currentSpawnedObject.transform.rotation =
                oldRotation;

            Physics.SyncTransforms();

            currentFurnitureData =
                newData;

            Debug.Log(
                $"가구가 '{newData.furnitureName}'으로 변경되었습니다."
            );

            return;
        }

        // -----------------------------------------------------
        // 새 가구가 어디에도 들어가지 않음
        // → 이전 가구 복구
        // -----------------------------------------------------

        if (oldData != null)
        {
            setup.SetupFurniture(
                oldData
            );

            drag.CacheRenderers();

            currentSpawnedObject.transform.position =
                oldPosition;

            currentSpawnedObject.transform.rotation =
                oldRotation;

            Physics.SyncTransforms();

            currentFurnitureData =
                oldData;
        }

        ShowWarningPopup(
            "이 가구는 현재 주변에 놓을 수 있는 공간이 없습니다."
        );
    }

    // =========================================================
    // 배치 확정
    // =========================================================

    public void ConfirmPlacement()
    {
        if (currentSpawnedObject == null)
            return;

        GameObject confirmedFurniture =
            currentSpawnedObject;

        currentSpawnedObject = null;

        // Build 모드로 먼저 변경
        currentMode =
            GameMode.Build;

        // SelectionManager를 사용해서
        // 원래 Layer 저장 + Selected 적용
        SelectionManager selManager =
            confirmedFurniture
                .GetComponent<SelectionManager>();

        if (selManager != null)
        {
            selManager.ApplySelection();
            selManager.SetStencilValue(15);
        }

        // 현재 선택 대상으로 설정
        SelectionObject(
            confirmedFurniture.transform
        );
    }

    // =========================================================
    // 가구 배치 취소
    // =========================================================

    public void CancelSpawn()
    {
        if (currentSpawnedObject != null)
        {
            Destroy(
                currentSpawnedObject
            );

            currentSpawnedObject = null;
        }

        currentFurnitureData = null;
    }

    // =========================================================
    // 가구 선택
    // =========================================================

    public void SelectionObject(
        Transform targetTransform)
    {
        if (targetTransform == null)
            return;

        // UI
        if (editUIPanel != null)
            editUIPanel.SetActive(true);

        if (buildUIPanel != null)
            buildUIPanel.SetActive(false);

        if (addUIPanel != null)
            addUIPanel.SetActive(false);

        // 현재 선택 대상
        selectedTarget =
            targetTransform;

        // 카메라 이동
        if (cameraMoveCoroutine != null)
            StopCoroutine(
                cameraMoveCoroutine
            );

        cameraMoveCoroutine =
            StartCoroutine(
                MoveCameraToTarget(
                    targetTransform.position
                )
            );
    }

    // =========================================================
    // 선택 해제
    // =========================================================

    public void DeselectObject()
    {
        if (editUIPanel != null)
            editUIPanel.SetActive(false);

        if (buildUIPanel != null)
            buildUIPanel.SetActive(true);

        selectedTarget = null;

        if (cameraMoveCoroutine != null)
            StopCoroutine(
                cameraMoveCoroutine
            );

        cameraMoveCoroutine =
            StartCoroutine(
                MoveCameraToCoords(
                    buildCameraPosition,
                    buildCameraRotation
                )
            );
    }

    // =========================================================
    // 카메라 이동
    // =========================================================

    private IEnumerator MoveCameraToTarget(
        Vector3 targetPosition)
    {
        Vector3 desiredPosition =
            targetPosition +
            cameraOffset;

        float duration = 0.5f;
        float elapsed = 0f;

        Vector3 startPosition =
            cameraTransform.position;

        Quaternion startRotation =
            cameraTransform.rotation;

        Vector3 directionToTarget =
            targetPosition -
            desiredPosition;

        Quaternion desiredRotation =
            Quaternion.LookRotation(
                directionToTarget
            );

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;

            float t =
                elapsed / duration;

            t =
                Mathf.SmoothStep(
                    0f,
                    1f,
                    t
                );

            cameraTransform.position =
                Vector3.Lerp(
                    startPosition,
                    desiredPosition,
                    t
                );

            cameraTransform.rotation =
                Quaternion.Lerp(
                    startRotation,
                    desiredRotation,
                    t
                );

            yield return null;
        }

        cameraTransform.position =
            desiredPosition;

        cameraTransform.rotation =
            desiredRotation;
    }

    private IEnumerator MoveCameraToCoords(
        Vector3 targetPos,
        Quaternion targetRot)
    {
        float duration = 0.5f;
        float elapsed = 0f;

        Vector3 startPosition =
            cameraTransform.position;

        Quaternion startRotation =
            cameraTransform.rotation;

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;

            float t =
                elapsed / duration;

            t =
                Mathf.SmoothStep(
                    0f,
                    1f,
                    t
                );

            cameraTransform.position =
                Vector3.Lerp(
                    startPosition,
                    targetPos,
                    t
                );

            cameraTransform.rotation =
                Quaternion.Slerp(
                    startRotation,
                    targetRot,
                    t
                );

            yield return null;
        }

        cameraTransform.position =
            targetPos;

        cameraTransform.rotation =
            targetRot;
    }

    // =========================================================
    // 편집 완료
    // =========================================================

    public void CompleteEditing()
    {
        if (selectedTarget != null)
        {
            SelectionManager selManager =
                selectedTarget
                    .GetComponent<SelectionManager>();

            if (selManager != null)
            {
                selManager.ResetSelection();
            }
        }

        DeselectObject();

        ChangeMode(
            GameMode.Build
        );
    }

    // =========================================================
    // 이동 시작
    // =========================================================

    public void StartMoveMode()
    {
        if (selectedTarget == null)
            return;

        if (editUIPanel != null)
            editUIPanel.SetActive(false);

        if (moveUIPanel != null)
            moveUIPanel.SetActive(true);

        if (cameraMoveCoroutine != null)
            StopCoroutine(
                cameraMoveCoroutine
            );

        cameraMoveCoroutine =
            StartCoroutine(
                MoveCameraToCoords(
                    buildCameraPosition,
                    buildCameraRotation
                )
            );

        ObjectDrag dragScript =
            selectedTarget
                .GetComponent<ObjectDrag>();

        if (dragScript != null)
        {
            dragScript.isMoveMode =
                true;
        }
    }

    // =========================================================
    // 회전
    // =========================================================

    public void RotateObject(
        float angle)
    {
        if (selectedTarget == null)
            return;

        selectedTarget.Rotate(
            Vector3.up,
            angle,
            Space.World
        );
    }

    // =========================================================
    // 배치 검사
    // =========================================================

    public void CheckPlacementValidity()
    {
        bool canPlace = true;

        if (canPlace)
        {
            Debug.Log(
                "드래그 임시 위치 적용 완료"
            );
        }
    }

    // =========================================================
    // 이동 종료
    // =========================================================

    public void EndMoveAndReturnToEdit()
    {
        if (selectedTarget == null)
            return;

        ObjectDrag dragScript =
            selectedTarget
                .GetComponent<ObjectDrag>();

        if (dragScript != null)
        {
            dragScript.isMoveMode =
                false;
        }

        if (moveUIPanel != null)
            moveUIPanel.SetActive(false);

        if (editUIPanel != null)
            editUIPanel.SetActive(true);

        if (cameraMoveCoroutine != null)
            StopCoroutine(
                cameraMoveCoroutine
            );

        cameraMoveCoroutine =
            StartCoroutine(
                MoveCameraToTarget(
                    selectedTarget.position
                )
            );
    }

    // =========================================================
    // 삭제
    // =========================================================

    public void ClickDeleteButton()
    {
        if (deleteConfirmPopUP != null)
            deleteConfirmPopUP.SetActive(true);
    }

    public void ConfirmDelete()
    {
        if (selectedTarget != null)
        {
            Destroy(
                selectedTarget.gameObject
            );
        }

        if (deleteConfirmPopUP != null)
            deleteConfirmPopUP.SetActive(false);

        if (editUIPanel != null)
            editUIPanel.SetActive(false);

        if (buildUIPanel != null)
            buildUIPanel.SetActive(true);

        selectedTarget = null;

        if (cameraMoveCoroutine != null)
            StopCoroutine(
                cameraMoveCoroutine
            );

        cameraMoveCoroutine =
            StartCoroutine(
                MoveCameraToCoords(
                    buildCameraPosition,
                    buildCameraRotation
                )
            );
    }

    public void CancelDelete()
    {
        if (deleteConfirmPopUP != null)
            deleteConfirmPopUP.SetActive(false);
    }

    // =========================================================
    // 현재 다른 가구를 편집 중인지
    // =========================================================

    public bool IsAlreadyEditing(
        Transform clickingObject)
    {
        if (selectedTarget != null &&
            selectedTarget != clickingObject)
        {
            return true;
        }

        return false;
    }

    // =========================================================
    // 모드 버튼
    // =========================================================

    public void SetNormalMode()
    {
        ChangeMode(
            GameMode.Normal
        );
    }

    public void SetBuildMode()
    {
        ChangeMode(
            GameMode.Build
        );
    }

    // =========================================================
    // Warning Popup
    // =========================================================

    public void ShowWarningPopup(
        string message =
            "해당 위치에는 가구를 놓을 수 없습니다.")
    {
        if (warningPopupPanel == null)
            return;

        if (warningText != null)
        {
            warningText.text =
                message;
        }

        if (warningCoroutine != null)
        {
            StopCoroutine(
                warningCoroutine
            );
        }

        warningCoroutine =
            StartCoroutine(
                HideWarningPopupRoutine(
                    2f
                )
            );
    }

    private IEnumerator HideWarningPopupRoutine(
        float delay)
    {
        warningPopupPanel.SetActive(true);

        yield return new WaitForSeconds(
            delay
        );

        warningPopupPanel.SetActive(false);
    }

    // =========================================================
    // 재시작
    // =========================================================

    public void ClickRestartButton()
    {
        if (restartConfirmPopUP != null)
            restartConfirmPopUP.SetActive(true);
    }

    public void CancelRestart()
    {
        if (restartConfirmPopUP != null)
            restartConfirmPopUP.SetActive(false);
    }
}