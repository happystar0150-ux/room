using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;

[RequireComponent(typeof(FurnitureHierarchy))]
public class ObjectDrag : MonoBehaviour
{
    [HideInInspector]
    public bool isMoveMode = false;

    private bool isDraggingAllowed = false;

    // 클릭 당시 마우스와 가구 위치의 차이
    private Vector3 offset;

    // 드래그 시작 위치 / 회전 / 부모
    private Vector3 startPosition;
    private Quaternion startRotation;
    private Transform startParent;

    // 현재 겹치는지 여부
    public bool isOverlapping = false;

    [Header("배치 레이어 설정")]

    [Tooltip("가구를 올려놓을 수 있는 표면 레이어")]
    public LayerMask placementLayerMask;

    [Tooltip("겹침 검사에서 무시할 레이어")]
    public LayerMask ignoreOverlapMask;

    // -------------------------------------------------
    // 가구 부모-자식 관계
    // -------------------------------------------------

    private FurnitureHierarchy furnitureHierarchy;

    // 현재 가구가 올라가려고 하는 아래 가구
    private FurnitureHierarchy currentSupportFurniture;

    // 현재 가구가 실제로 닿고 있는 아래 가구의 Collider
    //
    // 가구 위에 올려놓을 때
    // "정확히 아래 가구와 닿는 것"은 허용하기 위해 사용
    private Collider currentSupportCollider;

    // -------------------------------------------------
    // Renderer 캐시
    // -------------------------------------------------

    private Renderer[] renderers;

    private Dictionary<Renderer, MaterialColors[]> originalColors =
        new Dictionary<Renderer, MaterialColors[]>();

    private struct MaterialColors
    {
        public Color baseColor;
        public Color shade1Color;
        public Color shade2Color;
    }

    // -------------------------------------------------
    // 초기화
    // -------------------------------------------------

    private void Awake()
    {
        CacheRenderers();

        furnitureHierarchy =
            GetComponent<FurnitureHierarchy>();

        if (furnitureHierarchy == null)
        {
            Debug.LogError(
                "FurnitureHierarchy가 없습니다."
            );
        }
    }

    // -------------------------------------------------
    // Renderer 캐시
    // -------------------------------------------------

    public void CacheRenderers()
    {
        renderers =
            GetComponentsInChildren<Renderer>();

        originalColors.Clear();

        foreach (var rend in renderers)
        {
            if (rend == null)
                continue;

            MaterialColors[] colors =
                new MaterialColors[rend.materials.Length];

            for (int i = 0;
                 i < rend.materials.Length;
                 i++)
            {
                Material mat =
                    rend.materials[i];

                if (mat == null)
                    continue;

                // 기본 색상
                if (mat.HasProperty("_BaseColor"))
                {
                    colors[i].baseColor =
                        mat.GetColor("_BaseColor");
                }
                else if (mat.HasProperty("_Color"))
                {
                    colors[i].baseColor =
                        mat.color;
                }

                // Shade 색상
                if (mat.HasProperty("_1st_ShadeColor"))
                {
                    colors[i].shade1Color =
                        mat.GetColor("_1st_ShadeColor");
                }

                if (mat.HasProperty("_2nd_ShadeColor"))
                {
                    colors[i].shade2Color =
                        mat.GetColor("_2nd_ShadeColor");
                }
            }

            originalColors[rend] = colors;
        }
    }

    // -------------------------------------------------
    // Update
    // -------------------------------------------------

    private void Update()
    {
        if (!isMoveMode)
            return;

        // =================================================
        // 1. 마우스 클릭 시작
        // =================================================

        if (Input.GetMouseButtonDown(0))
        {
            // UI 클릭이면 무시
            if (EventSystem.current != null &&
                EventSystem.current.IsPointerOverGameObject())
            {
                isDraggingAllowed = false;
                return;
            }

            isDraggingAllowed = true;

            // 시작 위치 / 회전 / 부모 저장
            startPosition =
                transform.position;

            startRotation =
                transform.rotation;

            startParent =
                transform.parent;

            // 이전에 찾았던 아래 가구 초기화
            currentSupportFurniture = null;
            currentSupportCollider = null;

            // 가구 변경 후 Renderer가 바뀔 수 있으므로 다시 캐시
            CacheRenderers();

            // -------------------------------------------------
            // 드래그 시작 시 기존 부모에서 분리
            // -------------------------------------------------
            //
            // 예:
            //
            // A
            // └ B
            //    └ C
            //
            // B를 집으면:
            //
            // A
            //
            // B
            // └ C
            //
            // 이렇게 잠깐 분리됨
            //
            if (furnitureHierarchy != null)
            {
                furnitureHierarchy.DetachFromParent();
            }
            else
            {
                transform.SetParent(null, true);
            }

            Plane currentPlane =
                new Plane(
                    Vector3.up,
                    new Vector3(
                        0f,
                        transform.position.y,
                        0f
                    )
                );

            Vector3 mouseWorldPos =
                GetMouseWorldPositionOnPlane(
                    currentPlane
                );

            offset =
                transform.position - mouseWorldPos;

            offset.y = 0f;
        }

        // =================================================
        // 2. 드래그 중
        // =================================================

        if (Input.GetMouseButton(0) &&
            isDraggingAllowed)
        {
            Ray ray =
                Camera.main.ScreenPointToRay(
                    Input.mousePosition
                );

            // 놓을 수 있는 표면만 검색
            RaycastHit[] hits =
                Physics.RaycastAll(
                    ray,
                    500f,
                    placementLayerMask,
                    QueryTriggerInteraction.Collide
                );

            Array.Sort(
                hits,
                (x, y) =>
                    x.distance.CompareTo(y.distance)
            );

            bool foundValidSurface = false;

            Vector3 targetPos =
                transform.position;

            // 이번 프레임의 아래 가구 정보 초기화
            currentSupportFurniture = null;
            currentSupportCollider = null;

            // ---------------------------------------------
            // 표면 찾기
            // ---------------------------------------------

            foreach (var hit in hits)
            {
                if (hit.transform == null)
                    continue;

                // 자기 자신 무시
                if (hit.transform == transform ||
                    hit.transform.IsChildOf(transform))
                {
                    continue;
                }

                // 위쪽을 바라보는 면만 허용
                if (hit.normal.y < 0.7f)
                    continue;

                targetPos =
                    hit.point + offset;

                targetPos.y =
                    hit.point.y;

                // -----------------------------------------
                // 아래에 가구가 있는지 확인
                // -----------------------------------------

                FurnitureHierarchy supportFurniture =
                    hit.transform
                        .GetComponentInParent<FurnitureHierarchy>();

                if (supportFurniture != null)
                {
                    currentSupportFurniture =
                        supportFurniture;

                    currentSupportCollider =
                        hit.collider;
                }

                foundValidSurface = true;

                break;
            }

            // ---------------------------------------------
            // 표면이 없으면 현재 높이에서 이동
            // ---------------------------------------------

            if (!foundValidSurface)
            {
                Plane currentPlane =
                    new Plane(
                        Vector3.up,
                        new Vector3(
                            0f,
                            transform.position.y,
                            0f
                        )
                    );

                Vector3 mouseWorldPos =
                    GetMouseWorldPositionOnPlane(
                        currentPlane
                    );

                targetPos =
                    mouseWorldPos + offset;

                targetPos.y =
                    transform.position.y;

                // 아래 가구 없음
                currentSupportFurniture = null;
                currentSupportCollider = null;
            }

            // 실제 위치 이동
            transform.position =
                targetPos;

            Physics.SyncTransforms();

            // 겹침 검사
            CheckOverlapAndApplyVisuals();
        }

        // =================================================
        // 3. 마우스 클릭 종료
        // =================================================

        if (Input.GetMouseButtonUp(0))
        {
            if (isDraggingAllowed)
            {
                // =================================================
                // 잘못된 위치
                // =================================================

                if (isOverlapping)
                {
                    RestoreStartTransform();

                    if (GameManager.Instance != null)
                    {
                        GameManager.Instance.ShowWarningPopup(
                            "다른 가구와 겹치는 위치에는 놓을 수 없습니다!"
                        );
                    }
                }
                else
                {
                    // =================================================
                    // 정상적인 위치
                    // =================================================

                    bool attachSuccess = true;

                    if (furnitureHierarchy != null)
                    {
                        // 아래에 가구가 있으면 부모 연결
                        // 아래에 가구가 없으면 최상위로 이동
                        attachSuccess =
                            furnitureHierarchy.AttachTo(
                                currentSupportFurniture
                            );
                    }

                    // 부모 연결 실패
                    if (!attachSuccess)
                    {
                        RestoreStartTransform();

                        if (GameManager.Instance != null)
                        {
                            GameManager.Instance.ShowWarningPopup(
                                "이 가구 위에는 놓을 수 없습니다."
                            );
                        }
                    }
                    else
                    {
                        // 정상 위치 저장
                        startPosition =
                            transform.position;

                        startRotation =
                            transform.rotation;

                        startParent =
                            transform.parent;

                        Physics.SyncTransforms();

                        if (GameManager.Instance != null)
                        {
                            GameManager.Instance
                                .CheckPlacementValidity();
                        }
                    }
                }

                ResetVisuals();
            }

            // 다음 드래그를 위해 초기화
            isDraggingAllowed = false;

            currentSupportFurniture = null;
            currentSupportCollider = null;
        }
    }

    // =========================================================
    // 시작 상태로 복구
    // =========================================================

    private void RestoreStartTransform()
    {
        // 원래 부모 복구
        transform.SetParent(
            startParent,
            true
        );

        // 원래 위치 / 회전 복구
        transform.SetPositionAndRotation(
            startPosition,
            startRotation
        );

        Physics.SyncTransforms();
    }

    // =========================================================
    // 겹침 검사 + 색상
    // =========================================================

    private void CheckOverlapAndApplyVisuals()
    {
        CheckOverlapOnly();

        if (isOverlapping)
        {
            SetVisualColor(
                new Color(
                    1f,
                    0f,
                    0f,
                    0.5f
                )
            );
        }
        else
        {
            SetVisualColor(Color.clear);
        }
    }

    // =========================================================
    // 실제 겹침 검사
    // =========================================================

    private bool CheckOverlapOnly()
    {
        Collider[] myColliders =
            GetComponentsInChildren<Collider>();

        isOverlapping = false;

        foreach (var col in myColliders)
        {
            if (col == null ||
                !col.enabled ||
                !col.gameObject.activeInHierarchy)
            {
                continue;
            }

            Collider[] overlaps;

            if (col is BoxCollider boxCol)
            {
                Vector3 center =
                    boxCol.transform.TransformPoint(
                        boxCol.center
                    );

                Vector3 halfExtents =
                    Vector3.Scale(
                        boxCol.size,
                        boxCol.transform.lossyScale
                    ) * 0.5f;

                overlaps =
                    Physics.OverlapBox(
                        center,
                        halfExtents,
                        boxCol.transform.rotation,
                        ~ignoreOverlapMask.value,
                        QueryTriggerInteraction.Ignore
                    );
            }
            else
            {
                Bounds bounds =
                    col.bounds;

                overlaps =
                    Physics.OverlapBox(
                        bounds.center,
                        bounds.extents,
                        Quaternion.identity,
                        ~ignoreOverlapMask.value,
                        QueryTriggerInteraction.Ignore
                    );
            }

            foreach (var other in overlaps)
            {
                if (other == null)
                    continue;

                // -------------------------------------------------
                // 자기 자신 무시
                // -------------------------------------------------

                if (other.transform == transform ||
                    other.transform.IsChildOf(transform))
                {
                    continue;
                }

                // -------------------------------------------------
                // 무시 레이어
                // -------------------------------------------------

                if (((1 << other.gameObject.layer) &
                    ignoreOverlapMask) != 0)
                {
                    continue;
                }

                // -------------------------------------------------
                // 현재 바로 아래에서 받쳐주는 가구의
                // "Ray가 맞은 Collider"는 무시
                //
                // 이렇게 해야 가구와 가구가 딱 맞닿는
                // 정상적인 적재가 겹침으로 판정되지 않음
                // -------------------------------------------------

                if (currentSupportFurniture != null &&
                    currentSupportCollider != null &&
                    other == currentSupportCollider)
                {
                    continue;
                }

                // 다른 가구 / 벽과 겹침
                isOverlapping = true;

                break;
            }

            if (isOverlapping)
                break;
        }

        return isOverlapping;
    }

    // =========================================================
    // 자동 배치용
    // =========================================================

    public bool IsPositionBlocked(
        Vector3 targetPosition,
        Quaternion targetRotation)
    {
        Vector3 oldPosition =
            transform.position;

        Quaternion oldRotation =
            transform.rotation;

        // 검사할 위치로 임시 이동
        transform.SetPositionAndRotation(
            targetPosition,
            targetRotation
        );

        Physics.SyncTransforms();

        bool blocked =
            CheckOverlapOnly();

        // 원래 위치로 복구
        transform.SetPositionAndRotation(
            oldPosition,
            oldRotation
        );

        Physics.SyncTransforms();

        isOverlapping = false;

        return blocked;
    }

    // =========================================================
    // 색상 변경
    // =========================================================

    private void SetVisualColor(Color tintColor)
    {
        if (renderers == null)
            return;

        foreach (var rend in renderers)
        {
            if (rend == null)
                continue;

            for (int i = 0;
                 i < rend.materials.Length;
                 i++)
            {
                Material mat =
                    rend.materials[i];

                if (mat == null)
                    continue;

                bool hasBaseColor =
                    mat.HasProperty("_BaseColor");

                bool hasColor =
                    mat.HasProperty("_Color");

                if (!hasBaseColor &&
                    !hasColor)
                {
                    continue;
                }

                // ---------------------------------------------
                // 원래 색상
                // ---------------------------------------------

                if (tintColor == Color.clear)
                {
                    if (originalColors.ContainsKey(rend) &&
                        originalColors[rend].Length > i)
                    {
                        MaterialColors orig =
                            originalColors[rend][i];

                        if (hasBaseColor)
                        {
                            mat.SetColor(
                                "_BaseColor",
                                orig.baseColor
                            );
                        }
                        else if (hasColor)
                        {
                            mat.color =
                                orig.baseColor;
                        }

                        if (mat.HasProperty(
                            "_1st_ShadeColor"))
                        {
                            mat.SetColor(
                                "_1st_ShadeColor",
                                orig.shade1Color
                            );
                        }

                        if (mat.HasProperty(
                            "_2nd_ShadeColor"))
                        {
                            mat.SetColor(
                                "_2nd_ShadeColor",
                                orig.shade2Color
                            );
                        }
                    }
                }

                // ---------------------------------------------
                // 잘못된 위치 → 빨간색
                // ---------------------------------------------

                else
                {
                    if (hasBaseColor)
                    {
                        mat.SetColor(
                            "_BaseColor",
                            tintColor
                        );

                        if (mat.HasProperty(
                            "_1st_ShadeColor"))
                        {
                            mat.SetColor(
                                "_1st_ShadeColor",
                                tintColor
                            );
                        }

                        if (mat.HasProperty(
                            "_2nd_ShadeColor"))
                        {
                            mat.SetColor(
                                "_2nd_ShadeColor",
                                tintColor
                            );
                        }
                    }
                    else if (hasColor)
                    {
                        mat.color =
                            tintColor;
                    }
                }
            }
        }
    }

    private void ResetVisuals()
    {
        SetVisualColor(Color.clear);

        isOverlapping = false;
    }

    // =========================================================
    // 마우스 월드 좌표
    // =========================================================

    // =========================================================
    // 현재 가구 바로 아래에 있는 가구에 부모 연결
    // =========================================================
    //
    // 처음 가구를 생성했을 때
    // 이미 다른 가구 위에 올라가 있다면
    // ConfirmPlacement()에서 이 함수를 호출해서
    // 바로 부모-자식 관계를 만들어준다.
    //
    // 반환값:
    // true  = 정상적으로 처리됨
    // false = 부모 연결 실패
    // =========================================================

    public bool AttachToFurnitureBelow()
    {
        if (furnitureHierarchy == null)
        {
            furnitureHierarchy =
                GetComponent<FurnitureHierarchy>();
        }

        if (furnitureHierarchy == null)
        {
            Debug.LogWarning(
                "FurnitureHierarchy를 찾을 수 없습니다."
            );

            return false;
        }

        if (placementLayerMask.value == 0)
        {
            Debug.LogWarning(
                "ObjectDrag의 Placement Layer Mask가 설정되지 않았습니다."
            );

            return false;
        }

        // ---------------------------------------------------------
        // 현재 가구의 Collider들을 기준으로
        // 가장 높은 위치에서 위에서 아래로 Ray를 쏨
        // ---------------------------------------------------------

        Collider[] myColliders =
            GetComponentsInChildren<Collider>();

        float highestY =
            transform.position.y + 5f;

        foreach (Collider col in myColliders)
        {
            if (col == null ||
                !col.enabled)
            {
                continue;
            }

            if (col.bounds.max.y > highestY)
            {
                highestY = col.bounds.max.y;
            }
        }

        Vector3 rayStart =
            new Vector3(
                transform.position.x,
                highestY + 0.5f,
                transform.position.z
            );

        RaycastHit[] hits =
            Physics.RaycastAll(
                rayStart,
                Vector3.down,
                100f,
                placementLayerMask,
                QueryTriggerInteraction.Collide
            );

        Array.Sort(
            hits,
            (a, b) =>
                a.distance.CompareTo(b.distance)
        );

        // ---------------------------------------------------------
        // 가장 위에 있는 유효한 표면 찾기
        // ---------------------------------------------------------

        foreach (RaycastHit hit in hits)
        {
            if (hit.transform == null)
                continue;

            // 자기 자신 무시
            if (hit.transform == transform ||
                hit.transform.IsChildOf(transform))
            {
                continue;
            }

            // 위쪽 면이 아니면 무시
            if (hit.normal.y < 0.7f)
                continue;

            // -----------------------------------------------------
            // 해당 Collider가 가구에 속해 있는지 확인
            // -----------------------------------------------------

            FurnitureHierarchy belowFurniture =
                hit.transform
                    .GetComponentInParent<FurnitureHierarchy>();

            // -----------------------------------------------------
            // 가구가 아니면 바닥 / 방의 표면
            // → 부모 없음
            // -----------------------------------------------------

            if (belowFurniture == null)
            {
                furnitureHierarchy.AttachTo(null);
                return true;
            }

            // -----------------------------------------------------
            // 자신의 자식이면 연결 불가
            // -----------------------------------------------------

            if (belowFurniture.transform.IsChildOf(
                transform))
            {
                Debug.LogWarning(
                    "자신의 자식 가구 위에는 놓을 수 없습니다."
                );

                return false;
            }

            // -----------------------------------------------------
            // 아래 가구에 부모 연결
            // -----------------------------------------------------

            bool success =
                furnitureHierarchy.AttachTo(
                    belowFurniture
                );

            return success;
        }

        // ---------------------------------------------------------
        // 아래에 아무 표면도 없으면
        // 부모 없음으로 처리
        // ---------------------------------------------------------

        furnitureHierarchy.AttachTo(null);

        return true;
    }

    private Vector3 GetMouseWorldPositionOnPlane(
        Plane plane)
    {
        if (Camera.main == null)
            return transform.position;

        Ray ray =
            Camera.main.ScreenPointToRay(
                Input.mousePosition
            );

        if (plane.Raycast(
            ray,
            out float enter))
        {
            return ray.GetPoint(enter);
        }

        return transform.position;
    }
}