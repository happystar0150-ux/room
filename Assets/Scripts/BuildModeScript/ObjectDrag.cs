using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;

public class ObjectDrag : MonoBehaviour
{
    [HideInInspector]
    public bool isMoveMode = false;

    private bool isDraggingAllowed = false;

    // 클릭 당시 마우스와 가구 위치의 차이
    private Vector3 offset;

    // 드래그 시작 위치 / 회전
    private Vector3 startPosition;
    private Quaternion startRotation;

    // 현재 겹치는지 여부
    public bool isOverlapping = false;

    [Header("배치 레이어 설정")]

    [Tooltip("가구를 올려놓을 수 있는 표면 레이어")]
    public LayerMask placementLayerMask;

    [Tooltip("겹침 검사에서 무시할 레이어")]
    public LayerMask ignoreOverlapMask;

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
    }

    // -------------------------------------------------
    // Renderer 캐시
    // -------------------------------------------------

    public void CacheRenderers()
    {
        renderers = GetComponentsInChildren<Renderer>();

        originalColors.Clear();

        foreach (var rend in renderers)
        {
            if (rend == null)
                continue;

            MaterialColors[] colors =
                new MaterialColors[rend.materials.Length];

            for (int i = 0; i < rend.materials.Length; i++)
            {
                Material mat = rend.materials[i];

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

            // 시작 위치 / 회전 저장
            startPosition = transform.position;
            startRotation = transform.rotation;

            // 가구 변경 후 Renderer가 바뀔 수 있으므로 다시 캐시
            CacheRenderers();

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

            // ---------------------------------------------
            // 표면 찾기
            // ---------------------------------------------

            foreach (var hit in hits)
            {
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
            }

            transform.position =
                targetPos;

            Physics.SyncTransforms();

            CheckOverlapAndApplyVisuals();
        }

        // =================================================
        // 3. 마우스 클릭 종료
        // =================================================

        if (Input.GetMouseButtonUp(0))
        {
            if (isDraggingAllowed)
            {
                if (isOverlapping)
                {
                    // 잘못된 위치 → 원래 위치 복구
                    transform.position =
                        startPosition;

                    transform.rotation =
                        startRotation;

                    if (GameManager.Instance != null)
                    {
                        GameManager.Instance.ShowWarningPopup(
                            "다른 가구와 겹치는 위치에는 놓을 수 없습니다!"
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

                    if (GameManager.Instance != null)
                    {
                        GameManager.Instance
                            .CheckPlacementValidity();
                    }
                }

                ResetVisuals();
            }

            isDraggingAllowed = false;
        }
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

                // 자기 자신 무시
                if (other.transform == transform ||
                    other.transform.IsChildOf(transform))
                {
                    continue;
                }

                // 무시 레이어
                if (((1 << other.gameObject.layer) &
                    ignoreOverlapMask) != 0)
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
