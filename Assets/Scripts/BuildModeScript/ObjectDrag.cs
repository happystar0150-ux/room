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

    // 가구 본인과 자식들의 콜라이더 목록
    private Collider[] myColliders;

    [Header("배치 설정")]
    [Tooltip("가구를 올려놓을 수 있는 레이어")]
    public LayerMask placementLayerMask;

    private void Awake()
    {
        // 내 몸통과 자식에 달린 모든 콜라이더를 찾아둠
        myColliders = GetComponentsInChildren<Collider>();
    }

    private void Update()
    {
        // 이동 모드가 아닐 때 작동하지 않음
        if (!isMoveMode) return;

        // 1. 마우스를 처음 클릭하는 순간
        if (Input.GetMouseButtonDown(0))
        {
            // 클릭 위치가 UI 위라면 드래그 차단
            if (EventSystem.current != null && EventSystem.current.IsPointerOverGameObject())
            {
                //Debug.LogWarning("[ObjectDrag] UI 위를 클릭하여 드래그가 차단되었습니다!");
                isDraggingAllowed = false;
                return;
            }

            // 빈공간이나 가구를 제대로 눌렀다면 드래그 허용
            isDraggingAllowed = true;
            Debug.Log("[ObjectDrag] 드래그 시작됨!");

            // 현재 가구가 위치한 Y 높이를 기준으로 평면을 만들어 시작 오프셋 계산
            Plane currentPlane = new Plane(Vector3.up, new Vector3(0, transform.position.y, 0));
            Vector3 mouseWorldPos = GetMouseWorldPositionOnPlane(currentPlane);

            // X, Z 축의 상대적 거리차만 오프셋으로 저장
            offset = transform.position - mouseWorldPos;
            offset.y = 0;
        }

        // 2. 드래그 중일 때
        if (Input.GetMouseButton(0) && isDraggingAllowed)
        {
            // 레이저가 자기 자신에게 맞지 않게 콜라이더 비활성화
            SetMyCollidersEnabled(false);

            Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);
            RaycastHit hit;

            Vector3 targetPos;
            
            // 마우스 커서 아래에 '배치 가능한 표면(바닥 or 다른 가구 상단)'이 있는지 레이저로 확인
            // (placementLayerMask를 설정하여 자기 자신에게 레이저가 걸리는 것 방지)
            if (placementLayerMask.value != 0 && Physics.Raycast(ray, out hit, 500f, placementLayerMask))
            {
                // 마우스가 가리키는 지점 + 오프셋
                targetPos = hit.point + offset;

                // Y 높이는 마우스 아래 부딪힌 표면(바닥 or 가구 상단)의 높이로 실시간 변경
                targetPos.y = hit.point.y;

                
            }
            else
            {
                // 표면을 벗어났다면 현재 높이 평면 기준으로 이동
                Plane currentPlane = new Plane(Vector3.up, new Vector3(0, transform.position.y, 0));
                Vector3 currentMouseWorldPos = GetMouseWorldPositionOnPlane(currentPlane);

                targetPos = currentMouseWorldPos + offset;
                targetPos.y = transform.position.y; // 현재 높이 유지
                
            }

            // 레이저 발사 후 콜라이더 다시 켜기
            SetMyCollidersEnabled(true);

            // 위치 이동 적용
            transform.position = targetPos;

            Debug.Log($"[ObjectDrag] 드래그 중... 좌표: {targetPos}");
          
        }

        // 3. 마우스 클릭 뗐을 때
        if (Input.GetMouseButtonUp(0))
        {
            if(isDraggingAllowed)
            {
                Debug.Log("[ObjectDrag] 드래그 종료");
                if (GameManager.Instance != null)
                {
                    GameManager.Instance.CheckPlacementValidity();
                }
                
            }

            // 변수 초기화
            isDraggingAllowed = false ;
        }
    }

    // 내 콜라이더 일괄 켜고 끄는 함수
    private void SetMyCollidersEnabled(bool enabled)
    {
        if (myColliders == null) return;
        foreach (var col in myColliders)
        {
            if (col != null) col.enabled = enabled;
        }
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
