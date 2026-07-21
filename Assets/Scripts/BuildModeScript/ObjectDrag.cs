using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;

public class ObjectDrag : MonoBehaviour
{
    [HideInInspector] public bool isMoveMode = false;

    private bool isDraggingAllowed = false;

    // 클릭 시점의 가구 위치와 마우스 위치 차이(오프셋)
    private Vector3 offset;

    // 드래그 시작 전 위치와 회전값 저장
    private Vector3 startPosition;
    private Quaternion startRotation;

    // 현재 겹침 상태
    public bool isOverlapping = false;

    [Header("배치 및 레이어 설정")]
    [Tooltip("가구를 올려놓을 수 있는 레이어")]
    public LayerMask placementLayerMask;

    [Tooltip("가구 겹침 감지 시 무시할 레이어")]
    public LayerMask ignoreOverlapMask;

    // 머티리얼 색상 변경용(경고용)
    private Renderer[] renderers;
    private Dictionary<Renderer, MaterialColors[]> originalColors = new Dictionary<Renderer, MaterialColors[]>();

    // 머티리얼 별 원본 색상과 그림자 색상을 모두 저장하기 위한 구조체
    private struct MaterialColors
    {
        public Color baseColor;
        public Color shade1Color;
        public Color shade2Color;
    }

    private void Awake()
    {
        CacheRenderers();
    }

    // 가구 외형이 바뀔 수 있으므로 머티리얼 및 원본 색상 캐싱
    public void CacheRenderers()
    {
        renderers = GetComponentsInChildren<Renderer>();
        originalColors.Clear();

        foreach (var rend in renderers)
        {
            if(rend == null) continue;

            // 머티리얼 개수만큼 구조체 배열 생성
            MaterialColors[] colors = new MaterialColors[rend.materials.Length];

            for (int i = 0; i < rend.materials.Length; i++)
            {
                Material mat = rend.materials[i];

                // 1. 기본 색상 저장
                if (mat.HasProperty("_BaseColor"))
                    colors[i].baseColor = mat.GetColor("_BaseColor");
                else if (mat.HasProperty("_Color"))
                    colors[i].baseColor = mat.color;

                // 2. (유니티짱 툰쉐이더용) 그림자 색상 저장
                if (mat.HasProperty("_1st_ShadeColor"))
                    colors[i].shade1Color = mat.GetColor("_1st_ShadeColor");
                if (mat.HasProperty("_2nd_ShadeColor"))
                    colors[i].shade2Color = mat.GetColor("_2nd_ShadeColor");
            }
            originalColors[rend] = colors;
        }
    }

    private void Update()
    {
        // 이동 모드가 아닐 때 작동하지 않음
        if (!isMoveMode) return;

        // 1. 마우스 클릭 시작
        if (Input.GetMouseButtonDown(0))
        {
            // 클릭 위치가 UI 위라면 드래그 차단
            if (EventSystem.current != null && EventSystem.current.IsPointerOverGameObject())
            {

                isDraggingAllowed = false;
                return;

            }

            // 빈공간이나 가구를 제대로 눌렀다면 드래그 허용
            isDraggingAllowed = true;
            
            // 드래그 시작 전 위치/회전 백업
            startPosition = transform.position;
            startRotation = transform.rotation;

            // 외형 변경 가능성을 대비해 렌더러 재캐싱
            CacheRenderers();

            // 현재 가구가 위치한 Y 높이를 기준으로 평면을 만들어 시작 오프셋 계산
            Plane currentPlane = new Plane(Vector3.up, new Vector3(0, transform.position.y, 0));
            Vector3 mouseWorldPos = GetMouseWorldPositionOnPlane(currentPlane);

            // X, Z 축의 상대적 거리차만 오프셋으로 저장
            offset = transform.position - mouseWorldPos;
            offset.y = 0;
        }

        // 2. 드래그 중
        if (Input.GetMouseButton(0) && isDraggingAllowed)
        {
            

            Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);

            // RaycastAll로 레이저 상의 모든 물체 감지
            RaycastHit[] hits = Physics.RaycastAll(ray, 500f, placementLayerMask);
            System.Array.Sort(hits, (x, y) => x.distance.CompareTo(y.distance));


            bool foundValidSurface = false;
            Vector3 targetPos = transform.position;
            
            foreach (var hit in hits)
            {
                // 자기 자신 및 자식 콜라이더에 맞은 것은 무시
                if (hit.transform.IsChildOf(this.transform)) continue;

                targetPos = hit.point + offset;
                targetPos.y = hit.point.y; // 부딪힌 표면 높이로 세팅
                foundValidSurface = true;
                break;
            }

            // 바닥/표면 범위를 벗어난 경우 현재 평면 높이 유지
            if (!foundValidSurface)
            {
                Plane currentPlane = new Plane(Vector3.up, new Vector3(0, transform.position.y, 0));
                Vector3 currentMouseWorldPos = GetMouseWorldPositionOnPlane(currentPlane);
                targetPos = currentMouseWorldPos + offset;
                targetPos.y = transform.position.y;
            }

            transform.position = targetPos;

            // 실시간 겹침 감지 및 빨간색 연출
            CheckOverlapAndApplyVisuals();
          
        }

        // 3. 마우스 클릭 뗐을 때
        if (Input.GetMouseButtonUp(0))
        {
            if(isDraggingAllowed)
            {
                if (isOverlapping)
                {
                    // 겹친 상태라면 드래그 시작 전 위치로 복귀
                    transform.position = startPosition;
                    transform.rotation = startRotation;

                    // 경고 팝업 띄우기
                    if (GameManager.Instance != null)
                    {
                        GameManager.Instance.ShowWarningPopup("다른 가구와 겹치는 위치에는 놓을 수 없습니다!");
                    }
                }
                else
                {
                    // 배치 성공 시 현재 위치를 새로운 시작 위치로 저장
                    startPosition = transform.position;
                    startRotation = transform.rotation;

                    if (GameManager.Instance != null)
                    {
                        GameManager.Instance.CheckPlacementValidity();
                    }
                }

                // 머티리얼 색상 원래대로 복구
                ResetVisuals();

                
                
            }

            // 변수 초기화
            isDraggingAllowed = false ;
        }
    }

    // OverlapBox를 활용한 실시간 가구 겹침 감지
    private void CheckOverlapAndApplyVisuals()
    {
        Collider[] myCols = GetComponentsInChildren<Collider>();
        isOverlapping = false ;

        foreach (var col in myCols)
        {
            if (col == null || !col.enabled) continue;

            Collider[] overlaps;

            if (col is BoxCollider boxCol)
            {
                Vector3 center = boxCol.transform.TransformPoint(boxCol.center);
                Vector3 halfExtents = Vector3.Scale(boxCol.size, boxCol.transform.lossyScale) * 0.5f;
                overlaps = Physics.OverlapBox(center, halfExtents, boxCol.transform.rotation);
            }
            else
            {
                overlaps = Physics.OverlapBox(col.bounds.center, col.bounds.extents, transform.rotation);
            }

            foreach (var other in overlaps)
            {
                // 자기 자신 및 자식 콜라이더 무시
                if (other.transform.IsChildOf(this.transform)) continue;

                // [디버그 팁] 만약 여전히 겹침 판정이 난다면 아래 주석을 풀고 로그를 확인
                // Debug.Log($"겹친 오브젝트: {other.gameObject.name} (레이어: {LayerMask.LayerToName(other.gameObject.layer)})");

                // 바닥/표면 레이어 무시
                if (((1 << other.gameObject.layer) & placementLayerMask) != 0) continue;

                // ignoreOverlapMask에 해당되는 레이어 무시
                if (((1 << other.gameObject.layer) & ignoreOverlapMask) != 0) continue;

                // 다른 가구나 벽과 겹침 확인
                isOverlapping = true;
                break;
            }

            if (isOverlapping) break;
        }

        // 겹치면 빨간색, 안 겹치면 원본 색상 적용
        SetVisualColor(isOverlapping ? new Color(1f, 0f, 0f, 0.5f) : Color.clear);
    }

    private void SetVisualColor(Color tintColor)
    {
        foreach (var rend in renderers)
        {
            if (rend == null) continue;
            for (int i = 0; i < rend.materials.Length; i++)
            {
                Material mat = rend.materials[i];
                // URP랑 Standard 셰이더 모두 지원
                bool hasBaseColor = rend.materials[i].HasProperty("_BaseColor");
                bool hasColor = rend.materials[i].HasProperty("_Color");

                if (hasBaseColor || hasColor)
                {
                    if (tintColor == Color.clear) // 원상복구
                    {
                        
                        if (originalColors.ContainsKey(rend) && originalColors[rend].Length > i)
                        {
                            MaterialColors orig = originalColors[rend][i];

                            // 기본 색상 복구
                            if (hasBaseColor) mat.SetColor("_BaseColor", orig.baseColor);
                            else if (hasColor) mat.color = orig.baseColor;

                            // 그림자 색상 복구
                            if (mat.HasProperty("_1st_ShadeColor"))
                                mat.SetColor("_1st_ShadeColor", orig.shade1Color);
                            if (mat.HasProperty("_2nd_ShadeColor"))
                                mat.SetColor("_2nd_ShadeColor", orig.shade2Color);
                        }
                    }
                    else // 빨간색 적용
                    {
                        if (hasBaseColor)
                        {
                            mat.SetColor("_BaseColor", tintColor);

                            // 툰 쉐이더 그림자 영역도 같이 빨갛게 덮어씌우기
                            if (rend.materials[i].HasProperty("_1st_ShadeColor"))
                                rend.materials[i].SetColor("_1st_ShadeColor", tintColor);
                            if (rend.materials[i].HasProperty("_2nd_ShadeColor"))
                                rend.materials[i].SetColor("_2nd_ShadeColor", tintColor);
                        }

                        else if (hasColor)
                        {
                            mat.color = tintColor;
                        }
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

    // 지정한 Y 높이의 평면과 레이저가 만나는 지점 구하기
    private Vector3 GetMouseWorldPositionOnPlane(Plane plane)
    {
        Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);
        if (plane.Raycast(ray, out float enter))
        {
            return ray.GetPoint(enter);
        }
        return transform.position;
    }
    
}
