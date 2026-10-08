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

    public GameMode currentMode = GameMode.Normal;

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

    // 현재 생성되어 있는 가구
    [Header("현재 가구")]

    public GameObject currentSpawnedObject;

    // 현재 미리보기/선택에 사용 중인 FurnitureData
    private FurnitureData currentFurnitureData;

    // =========================================================
    // Camera Reference
    // =========================================================

    [Header("Camera Reference")]

    public Transform cameraTransform;

    public Vector3 cameraOffset =
        new Vector3(0f, 5f, -5f);

    private Transform selectedTarget;

    // Normal 카메라
    private Vector3 normalCameraPosition;
    private Quaternion normalCameraRotation;

    // Build 카메라
    private Vector3 buildCameraPosition;
    private Quaternion buildCameraRotation;

    private Coroutine cameraMoveCoroutine;

    // =========================================================
    // Singleton
    // =========================================================

    public static GameManager Instance { get; private set; }

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
    // 자동 가구 배치
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
    // 기본 가구 배치 실패 여부
    // =========================================================

    // 기본 가구가 공간 부족으로 생성되지 않았고
    // 사용자가 메뉴에서 다른 가구를 선택해야 하는 상태인지
    private bool waitingForFurnitureSelection = false;

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
        // Build 모드가 아니면 선택하지 않음
        if (currentMode != GameMode.Build)
            return;

        if (!Input.GetMouseButtonDown(0))
            return;

        // UI 클릭이면 무시
        if (EventSystem.current != null &&
            EventSystem.current.IsPointerOverGameObject())
        {
            return;
        }

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

        Array.Sort(
            hits,
            (a, b) =>
                a.distance.CompareTo(b.distance)
        );

        foreach (RaycastHit hit in hits)
        {
            if (hit.transform == null)
                continue;

            SelectionManager selManager =
                hit.transform.GetComponentInParent<SelectionManager>();

            if (selManager == null)
                continue;

            selManager.OnSelectedByClick();

            return;
        }
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

                // 기본 A 카테고리 표시
                FilterFurnitureMenu("A");

                break;
        }
    }

    // =========================================================
    // 기본 가구 추가 버튼
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
        // 기본 위치
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

        // -----------------------------------------------------
        // 기존 미리보기 가구가 있다면
        // 그 위치를 새 가구의 우선 위치로 사용
        // -----------------------------------------------------

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

            oldObject.SetActive(false);
        }

        // -----------------------------------------------------
        // 새 가구를 넣을 수 있는지 검사하고 생성
        // -----------------------------------------------------

        bool spawned =
            TrySpawnFurniture(
                data,
                preferredPosition,
                targetRotation,
                out GameObject newObject,
                out Vector3 spawnPosition
            );

        // =====================================================
        // 성공
        // =====================================================

        if (spawned)
        {
            currentSpawnedObject =
                newObject;

            currentSpawnedObject.transform.position =
                spawnPosition;

            currentSpawnedObject.transform.rotation =
                targetRotation;

            currentFurnitureData =
                data;

            Physics.SyncTransforms();

            // 기존 미리보기 가구가 있었다면 삭제
            if (oldObject != null)
            {
                Destroy(oldObject);
            }

            waitingForFurnitureSelection = false;

            ChangeMode(GameMode.Add);

            return;
        }

        // =====================================================
        // 실패
        // =====================================================

        // 새 가구가 생성되었다면 제거
        if (newObject != null)
        {
            Destroy(newObject);
        }

        currentSpawnedObject = null;
        currentFurnitureData = null;

        // 기존 가구가 있었다면 복구
        if (oldObject != null)
        {
            oldObject.SetActive(true);

            currentSpawnedObject =
                oldObject;

            currentFurnitureData =
                oldData;

            // 기존 가구 교체가 실패한 경우이므로
            // 기존 가구는 그대로 유지
            waitingForFurnitureSelection = false;

            ShowWarningPopup(
                "선택한 가구를 현재 위치에 놓을 수 없습니다."
            );

            return;
        }

        // -----------------------------------------------------
        // ★ 여기서 중요한 부분
        //
        // 기본 가구가 아예 들어가지 않는 경우에는
        // 가구를 생성하지 않고 메뉴만 열어준다.
        // -----------------------------------------------------

        waitingForFurnitureSelection = true;

        ChangeMode(GameMode.Add);

        // 여기서는 경고창을 띄우지 않는다.
        // 사용자가 다른 가구를 선택할 수 있게 메뉴를 열어둔다.
    }

    // =========================================================
    // 가구 생성 + 자동 배치
    // =========================================================

    private bool TrySpawnFurniture(
        FurnitureData data,
        Vector3 preferredPosition,
        Quaternion targetRotation,
        out GameObject spawnedObject,
        out Vector3 spawnPosition)
    {
        spawnedObject = null;
        spawnPosition = preferredPosition;

        if (data == null)
            return false;

        if (commonFurniturePrefab == null)
            return false;

        // -----------------------------------------------------
        // 임시 가구 생성
        // -----------------------------------------------------

        spawnedObject =
            Instantiate(
                commonFurniturePrefab,
                preferredPosition,
                targetRotation
            );

        FurnitureSetup setup =
            spawnedObject.GetComponent<FurnitureSetup>();

        if (setup == null)
        {
            Debug.LogError(
                "commonFurniturePrefab에 FurnitureSetup이 없습니다."
            );

            Destroy(spawnedObject);
            spawnedObject = null;

            return false;
        }

        // 실제 가구 프리팹 생성
        setup.SetupFurniture(data);

        // -----------------------------------------------------
        // ObjectDrag
        // -----------------------------------------------------

        ObjectDrag drag =
            spawnedObject.GetComponent<ObjectDrag>();

        if (drag == null)
        {
            Debug.LogError(
                "commonFurniturePrefab에 ObjectDrag가 없습니다."
            );

            Destroy(spawnedObject);
            spawnedObject = null;

            return false;
        }

        // 새 가구의 Renderer 캐시
        drag.CacheRenderers();

        Physics.SyncTransforms();

        // -----------------------------------------------------
        // 빈 공간 탐색
        // -----------------------------------------------------

        bool foundPosition =
            TryFindSpawnPosition(
                drag,
                preferredPosition,
                targetRotation,
                out spawnPosition
            );

        if (!foundPosition)
        {
            Destroy(spawnedObject);
            spawnedObject = null;

            return false;
        }

        return true;
    }

    // =========================================================
    // 자동 위치 탐색
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

        // 검색 시작점이 방 밖이면 가장 가까운 곳으로 보정
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

        float maxDistanceX =
            Mathf.Max(
                Mathf.Abs(
                    searchOrigin.x -
                    bounds.min.x
                ),
                Mathf.Abs(
                    bounds.max.x -
                    searchOrigin.x
                )
            );

        float maxDistanceZ =
            Mathf.Max(
                Mathf.Abs(
                    searchOrigin.z -
                    bounds.min.z
                ),
                Mathf.Abs(
                    bounds.max.z -
                    searchOrigin.z
                )
            );

        int maxRing =
            Mathf.CeilToInt(
                Mathf.Max(
                    maxDistanceX,
                    maxDistanceZ
                ) /
                autoSearchStep
            );

        // -----------------------------------------------------
        // 중앙 → 바깥쪽 순서로 검사
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
                    // 현재 ring의 테두리만 검사
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

                    // 방 범위 밖이면 제외
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

        // 검색 영역 위에서 아래로 Ray
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

            // 충분히 위를 바라보는 면만 허용
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

        if (furnitureContentParent == null)
            return;

        // 기존 버튼 제거
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
    // 가구 종류 변경 / 새 가구 선택
    // =========================================================

    public void SwitchFurnitureData(
        FurnitureData newData)
    {
        if (newData == null)
            return;

        // =====================================================
        // 현재 가구가 없는 경우
        //
        // ★ 기본 가구가 공간 부족으로 생성되지 않아
        //    메뉴만 열려 있는 경우가 여기로 들어옴
        // =====================================================

        if (currentSpawnedObject == null)
        {
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

            // 혹시 선택 대기 상태라면 기본 위치에서 찾음
            if (waitingForFurnitureSelection)
            {
                bool spawned =
                    TrySpawnFurniture(
                        newData,
                        preferredPosition,
                        targetRotation,
                        out GameObject newObject,
                        out Vector3 spawnPosition
                    );

                // ---------------------------------------------
                // 선택한 가구가 들어감
                // ---------------------------------------------

                if (spawned)
                {
                    currentSpawnedObject =
                        newObject;

                    currentSpawnedObject.transform.position =
                        spawnPosition;

                    currentSpawnedObject.transform.rotation =
                        targetRotation;

                    currentFurnitureData =
                        newData;

                    waitingForFurnitureSelection =
                        false;

                    Physics.SyncTransforms();

                    Debug.Log(
                        $"'{newData.furnitureName}' 가구를 생성했습니다."
                    );

                    return;
                }

                // ---------------------------------------------
                // 선택한 가구도 들어가지 않음
                // ---------------------------------------------

                ShowWarningPopup(
                    "선택한 가구는 현재 방에 놓을 공간이 없습니다."
                );

                return;
            }

            // 선택 대기 상태가 아니더라도
            // 현재 가구가 없으면 그냥 새 가구 생성 시도
            bool normalSpawn =
                TrySpawnFurniture(
                    newData,
                    preferredPosition,
                    targetRotation,
                    out GameObject normalObject,
                    out Vector3 normalPosition
                );

            if (normalSpawn)
            {
                currentSpawnedObject =
                    normalObject;

                currentSpawnedObject.transform.position =
                    normalPosition;

                currentSpawnedObject.transform.rotation =
                    targetRotation;

                currentFurnitureData =
                    newData;

                Physics.SyncTransforms();

                return;
            }

            ShowWarningPopup(
                "선택한 가구는 현재 방에 놓을 공간이 없습니다."
            );

            return;
        }

        // =====================================================
        // 이미 가구가 있는 상태
        //
        // → 기존 가구를 다른 가구로 변경
        // =====================================================

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

        // 기존 데이터
        FurnitureData oldData =
            setup.CurrentData;

        // 기존 위치 / 회전
        Vector3 oldPosition =
            currentSpawnedObject.transform.position;

        Quaternion oldRotation =
            currentSpawnedObject.transform.rotation;

        // =====================================================
        // 새 가구 적용
        // =====================================================

        setup.SetupFurniture(
            newData
        );

        // 새 프리팹의 Renderer 캐시
        drag.CacheRenderers();

        Physics.SyncTransforms();

        // =====================================================
        // 새 가구의 실제 크기로 다시 위치 검사
        // =====================================================

        bool foundPosition =
            TryFindSpawnPosition(
                drag,
                oldPosition,
                oldRotation,
                out Vector3 newPosition
            );

        // =====================================================
        // 새 가구도 놓을 수 있음
        // =====================================================

        if (foundPosition)
        {
            currentSpawnedObject.transform.position =
                newPosition;

            currentSpawnedObject.transform.rotation =
                oldRotation;

            currentFurnitureData =
                newData;

            Physics.SyncTransforms();

            Debug.Log(
                $"가구를 '{newData.furnitureName}'으로 변경했습니다."
            );

            return;
        }

        // =====================================================
        // 새 가구를 어디에도 놓을 수 없음
        // → 기존 가구 복구
        // =====================================================

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

            currentFurnitureData =
                oldData;

            Physics.SyncTransforms();
        }

        ShowWarningPopup(
            "선택한 가구는 현재 위치에 놓을 수 없고, 주변에도 놓을 공간이 없습니다."
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
        currentFurnitureData = null;

        waitingForFurnitureSelection = false;

        // Build 모드
        currentMode =
            GameMode.Build;

        // 선택 상태 적용
        SelectionManager selManager =
            confirmedFurniture
                .GetComponent<SelectionManager>();

        if (selManager != null)
        {
            selManager.ApplySelection();
            selManager.SetStencilValue(15);
        }

        // 현재 선택 대상
        SelectionObject(
            confirmedFurniture.transform
        );
    }

    // =========================================================
    // 가구 추가 취소
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

        waitingForFurnitureSelection = false;
    }

    // =========================================================
    // 가구 선택
    // =========================================================

    public void SelectionObject(
        Transform targetTransform)
    {
        if (targetTransform == null)
            return;

        if (editUIPanel != null)
            editUIPanel.SetActive(true);

        if (buildUIPanel != null)
            buildUIPanel.SetActive(false);

        if (addUIPanel != null)
            addUIPanel.SetActive(false);

        selectedTarget =
            targetTransform;

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
    // 다른 가구를 편집 중인지
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