using System.Collections;
using System.Collections.Generic;
//using System.Timers;
using UnityEngine;

public class GameManager : MonoBehaviour
{
    // 현재 게임 모드
    public GameMode currentMode = GameMode.Normal;

    
    [Header("UI Panels")]
    public GameObject normalUIPanel;
    public GameObject buildUIPanel;
    public GameObject editUIPanel;
    public GameObject moveUIPanel;
    public GameObject addUIPanel;
    public GameObject deleteConfirmPopUP;
    

    [Header("Furniture Spawn System")]
    public GameObject commonFurniturePrefab;

   

    [Header("Camera Reference")]
    public Transform cameraTransform; // 메인 카메라 Transform
    public Vector3 cameraOffset = new Vector3(0, 5, -5); // 오브젝트를 바라볼 카메라의 상대적 위치값

    // 현재 선택 중인 오브젝트 transform 기억
    private Transform selectedTarget;

    // 모드별 카메라 정보
    // normal
    private Vector3 normalCameraPosition;
    private Quaternion normalCameraRotation;
    // build
    private Vector3 buildCameraPosition;
    private Quaternion buildCameraRotation;

    private Coroutine cameraMoveCoroutine;

    // 오브젝트 생성때 조작중인 가구 오브젝트를 기억
    private GameObject currentActiveFurniture;



    public static GameManager Instance { get; private set; }

    [Header("가구 목록")]
    // 프로젝트 창의 모든 가구 데이터를 넣어둘 리스트
    public List<FurnitureData> allFurnitureDataList = new List<FurnitureData>();
    // 가구 아이템 버튼 부품
    public GameObject furnitureItemPrefab;
    // FurnitureContent 부모 오브젝트
    public Transform furnitureContentParent;

    [Header("현재 배치 / 수정 대기 중인 가구 오브젝트")]
    public GameObject currentSpawnedObject;
    
    private void Awake()
    {
        if (Instance == null) Instance = this;
        else Destroy(gameObject);
    }

    private void Update()
    {
        
        // build 모드일때 가구 클릭 재선택 감지
        if (currentMode == GameMode.Build)
        {
            if (Input.GetMouseButtonDown(0))
            {
                // ui 클릭 중이면 무시
                if (UnityEngine.EventSystems.EventSystem.current.IsPointerOverGameObject())
                    return;

                HandleFurnitureSelectionClick();
            }
        }
        

        // 테스트
        if (Input.GetMouseButtonDown(0))
        {
            Debug.Log($"마우스 클릭됨! 현재 게임 모드: {currentMode}");

            if (UnityEngine.EventSystems.EventSystem.current != null &&
                UnityEngine.EventSystems.EventSystem.current.IsPointerOverGameObject())
            {
                Debug.LogWarning("현재 마우스 위치 아래 UI 요소가 있어 클릭이 차단되었습니다!");
                return;
            }

            if (currentMode != GameMode.Build)
            {
                Debug.Log($"현재 모드가 Build가 아니라 '{currentMode}'라서 레이캐스트를 쏘지 않았습니다.");
                return;
            }

            Debug.Log("모든 조건을 만족하여 레이저를 발사합니다.");
            HandleFurnitureSelectionClick();
        }
    }

    // 마우스 레이저로 가구를 조준해서 선택하는 함수
    private void HandleFurnitureSelectionClick()
    {
        Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);
        RaycastHit hit;

        Debug.Log("레이저 발사");

        if (Physics.Raycast(ray, out hit, 500f))
        {
            Debug.Log($"레이저가 부딪힌 오브젝트: {hit.transform.name}");

            // 부딪힌 자식 콜라이더의 부모에서 SelectionManager를 찾습니다
            SelectionManager selManager = hit.transform.GetComponentInParent<SelectionManager>();

            if (selManager != null)
            {
                Debug.Log($"[가구 선택 성공] {selManager.gameObject.name} 편집을 시작합니다.");
                selManager.OnSelectedByClick();
            }
            else
            {
                Debug.LogWarning($"{hit.transform.name}의 부모에서 SelectionManager를 찾지 못했습니다.");
            }
        }
    }

    void Start()
    {
        

        // 카메라 세팅
        // normal
        normalCameraPosition = cameraTransform.position;
        normalCameraRotation = cameraTransform.rotation;
        // build
        buildCameraPosition = cameraTransform.position;
        buildCameraRotation = cameraTransform.rotation;


        // 게임 시작 시 기본 모드로 설정
        ChangeMode(GameMode.Normal);

        if (deleteConfirmPopUP != null) deleteConfirmPopUP.SetActive(false);
        if (moveUIPanel != null) moveUIPanel.SetActive(false);
    }

    // 모드 변경 함수 (UI버튼에 연결)
    public void ChangeMode(GameMode newMode)
    {
        currentMode = newMode;

        // 상태에 따른 UI 및 시스템 활성화/비활성화
        switch (currentMode)
        {
            case GameMode.Normal:
                normalUIPanel.SetActive(true);
                buildUIPanel.SetActive(false);
                editUIPanel.SetActive(false);
                addUIPanel.SetActive(false);
                

                // 일반 모드로 돌아올 때 카메라 원위치
                if (cameraMoveCoroutine != null) StopCoroutine(cameraMoveCoroutine);
                cameraMoveCoroutine = StartCoroutine(MoveCameraToCoords(normalCameraPosition, normalCameraRotation));
                break;

            case GameMode.Build:
                normalUIPanel.SetActive(false);
                buildUIPanel.SetActive(true);
                editUIPanel.SetActive(false);
                addUIPanel.SetActive(false);

                /*
                // 건축 모드로 진입하는 순간의 카메라 상태를 백업
                if (buildCameraPosition == normalCameraPosition || buildCameraPosition == Vector3.zero)
                {
                    buildCameraPosition = cameraTransform.position;
                    buildCameraRotation = cameraTransform.rotation;
                }
                */
                break;
                

            case GameMode.Add:
                normalUIPanel.SetActive(false);
                buildUIPanel.SetActive(false);
                editUIPanel.SetActive(false);
                addUIPanel.SetActive(true);

                // A 카테고리 바로 오픈
                FilterFurnitureMenu("A");
                break;
        }
    }


    // 오브젝트 생성
    public void ClickSpawnButtonAtCenter(FurnitureData data)
    {
        if (data == null) return;
  
        // 방 한가운데 좌표 및 회전 설정
        Vector3 centerPos = new Vector3(0.5f, 0f, 0f);
        Quaternion targetRotation = Quaternion.Euler(-90f, 0f, 0f);

        // 기존 조작 가구 제거 (혹시몰르니까)
        if (currentSpawnedObject != null) Destroy(currentSpawnedObject);

        // 프리팹으로 진짜 가구 생성
        currentSpawnedObject = Instantiate(commonFurniturePrefab, centerPos, targetRotation);

        // 민트 소파 외형 적용
        FurnitureSetup setup = currentSpawnedObject.GetComponent<FurnitureSetup>();
        if (setup != null)
        {
            setup.SetupFurniture(data);
        }

        // 확정 전이므로 카메라나 선택 레이어 처리 하지 않음
        // 유저가 목록 ui 보고 가구를 고를 수 있도록 대기

        // 모드를 Add 모드로 변경
        ChangeMode(GameMode.Add);

    }



    // 카테고리 버튼 클릭시 해당 가구만 골라내는 필터 함수
    public void FilterFurnitureMenu(string categoryToFilter)
    {
        Debug.Log($"{categoryToFilter} 카테고리가 선택되었습니다! 목록을 변경합니다.");

        // 1. 현재 화면의 가구 목록 아이템 ui를 삭제
        foreach (Transform child in furnitureContentParent)
        {
            Destroy(child.gameObject);
        }

        // 2. 전체 가구 데이터 중에서 방금 클릭한 카테고리와 일치하는 가구만 버튼으로 생성
        foreach (FurnitureData data in allFurnitureDataList)
        {
            // 대소문자 구분, 빈칸 없이 똑같은지 비교
            if (data.categoryGroup.Trim().Equals(categoryToFilter.Trim(), System.StringComparison.OrdinalIgnoreCase))
            {
                // 버튼 프리팹 생성
                GameObject newBtn = Instantiate(furnitureItemPrefab, furnitureContentParent);

                // 생성된 버튼에 가구 데이터(이름, 아이콘) 주입
                FurnitureItemUI itemUI = newBtn.GetComponent<FurnitureItemUI>();
                if (itemUI != null)
                {
                    itemUI.Setup(data);
                }
            }
        }
    }

    // 가구 목록 ui에서 다른 가구를 클릭 했을 때 호출할 함수
    public void SwitchFurnitureData(FurnitureData newData)
    {
        

        if (currentSpawnedObject == null)
        {
            Debug.LogWarning("현재 화면에 조작 중인 가구 오브젝트가 없습니다!");
            return;
        }


        Debug.Log($"가구 외형을 '{newData.furnitureName}'(으)로 전환합니다.");

        // FurnitureSetup에게 새 데이터 넘겨주기
        FurnitureSetup setup = currentSpawnedObject.GetComponent<FurnitureSetup>();
        if (setup != null)
        {
            setup.SetupFurniture(newData);
        }

    }

    // 확정 후 편집 모드 전환
    public void ConfirmPlacement()
    {
        if (currentSpawnedObject == null) return;

        

        // 진짜 배치된 가구로 바꿔줌
        GameObject confirmedFurniture = currentSpawnedObject;

        // 변수 비우기
        currentSpawnedObject = null;

        // 레이어 변경
        SetLayerRecursively(confirmedFurniture, LayerMask.NameToLayer("Selected"));

        // 외곽선
        SelectionManager selManager = confirmedFurniture.GetComponent<SelectionManager>();
        if (selManager != null)
        {
            selManager.SetStencilValue(15);
        }

        // 선택(편집모드+줌인)시키기
        SelectionObject(confirmedFurniture.transform);

        // 모드 변경
        currentMode = GameMode.Build;
    }

    // 자식도 적용
    private void SetLayerRecursively(GameObject obj, int newLayer)
    {
        if (obj == null) return;

        // 만약 현재 오프젝트의 레이어가 'FurnitureSurface'라면 레이어를 바꾸지 않고 유지
        int surfaceLayer = LayerMask.NameToLayer("FurnitureSurface");
        if (obj.layer != surfaceLayer)
        {
            obj.layer = newLayer;
        }

        foreach (Transform child in obj.transform)
        {
            if(child == null) continue;
            SetLayerRecursively(child.gameObject, newLayer);
        }
    }

   

   




    // 오브젝트 선택 시
    public void SelectionObject(Transform targetTransform)
    {
        // 편집 UI 활성화
        editUIPanel.SetActive(true);

        // 건축 UI 비활성화
        buildUIPanel.SetActive(false);

        // 생성 UI 비활성화
        addUIPanel.SetActive(false);

        // 선택 오브젝트 기억
        selectedTarget = targetTransform;

        

        // 카메라 이동
        if (cameraMoveCoroutine != null ) StopCoroutine(cameraMoveCoroutine);
        cameraMoveCoroutine = StartCoroutine(MoveCameraToTarget(targetTransform.position));
    }

    // 오브젝트 선택 해제 시
    public void DeselectObject()
    {
        // 편집 UI 비활성화
        editUIPanel.SetActive(false);

        // 건축 UI 활성화
        buildUIPanel.SetActive(true);

        // 기억중인 오브젝트 잊기
        selectedTarget = null;

        // 카메라 복귀
        if (cameraMoveCoroutine != null) StopCoroutine(cameraMoveCoroutine);
        cameraMoveCoroutine = StartCoroutine(MoveCameraToCoords(buildCameraPosition, buildCameraRotation));
    }

    // 카메라를 부드럽게 타겟 위치(+오프셋)로 이동시키는 코루틴
    private IEnumerator MoveCameraToTarget(Vector3 targetPosition)
    {
        Vector3 desiredPosition = targetPosition + cameraOffset;
        float duration = 0.5f;
        float elapsed = 0f;

        Vector3 startPosition = cameraTransform.position;
        Quaternion startRotation = cameraTransform.rotation;

        Vector3 directionToTarget = targetPosition - desiredPosition;
        Quaternion desiredRotation = Quaternion.LookRotation(directionToTarget);

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / duration;

            // 부드러운 가속/감속 연출
            t = Mathf.SmoothStep(0f, 1f, t);

            cameraTransform.position = Vector3.Lerp(startPosition, desiredPosition, t);
            cameraTransform.rotation = Quaternion.Lerp(startRotation, desiredRotation, t);
            yield return null;
        }

        cameraTransform.position = desiredPosition;
        cameraTransform.rotation = desiredRotation;

    }

    private IEnumerator MoveCameraToCoords(Vector3 targetPos, Quaternion targetRot)
    {
        float duration = 0.5f;
        float elapsed = 0f;

        Vector3 startPosition = cameraTransform.position;
        Quaternion startRotation = cameraTransform.rotation;

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / duration;
            t = Mathf.SmoothStep(0f, 1f, t);

            cameraTransform.position = Vector3.Lerp(startPosition, targetPos, t);
            cameraTransform.rotation = Quaternion.Slerp(startRotation, targetRot, t);
            yield return null;
        }

        cameraTransform.position = targetPos;
        cameraTransform.rotation = targetRot;
    }

    // 편집 완료(컨펌) 버튼
    public void CompleteEditing()
    {
        SelectionManager[] selectedObjects = FindObjectsOfType<SelectionManager>();
        foreach(var obj in selectedObjects)
        {
            if (obj.gameObject.layer == LayerMask.NameToLayer("Selected"))
            {
                obj.ResetSelection();
            }
        }
        // 선택 해제 및 카메라 복구 로직
        DeselectObject();
        // 게임 모드 build로 변경
        ChangeMode(GameMode.Build);
    }
    

    // 오브젝트 이동
    public void StartMoveMode()
    {
        if (selectedTarget == null) return;

        

        // 편집 UI 끄고 이동 UI 켜기
        editUIPanel.SetActive(false);
        if (moveUIPanel != null) moveUIPanel.SetActive(true);

        // 카메라 위치 되돌리기
        if (cameraMoveCoroutine != null) StopCoroutine(cameraMoveCoroutine);
        cameraMoveCoroutine = StartCoroutine(MoveCameraToCoords(buildCameraPosition, buildCameraRotation));

        // 해당 오브젝트의 ObjectDrag 스크립트 찾아 이동 가능 상태로 만듦
        ObjectDrag dragScript = selectedTarget.GetComponent<ObjectDrag>();
        if (dragScript != null) dragScript.isMoveMode = true;
        
    }

    // 회전 버튼 클릭 시
    public void RotateObject(float angle) // angle에 45 혹은 -45
    {
        if (selectedTarget != null)
        {
            /*
             * //본인을 기준으로 하는거
            
            selectedTarget.Rotate(Vector3.up, angle, Space.Self);
            Debug.Log($"{selectedTarget.name} 회전됨! 현재 각도: {selectedTarget.eulerAngles.y}");
            */

            // 월드 좌표 기준으로 하는거
            // 현재 회전값에서 Y축 기준으로 angle만큼 더 회전
            if (selectedTarget != null)
            {
                selectedTarget.Rotate(Vector3.up, angle, Space.World);
            }
        }
    }

    // 드래그 끝났을 때 (마우스에서 손 뗐을 때)
    public void CheckPlacementValidity()
    {
        
        bool canPlace = true;

        /* // 나중에 추가해요:
         * if (벽에 부딪히거나 다른 가구와 겹친다면)
         * {
         *     canPlace = false
         * }
         */

        // 놓을 수 있으면
        if (canPlace)
        {

            Debug.Log("드래그 임시 배치 완료 (이동 모드 유지 중)");
            
        }
        else
        {
            // 놓을 수 없는 경우 제자리로 튕기고 경고 연출
        }
    }

    public void EndMoveAndReturnToEdit()
    {
        if (selectedTarget == null) return;

        // 이동 모드를 종료하고 다시 선택 상태로 백업
        ObjectDrag dragScript = selectedTarget.GetComponent<ObjectDrag>();
        if (dragScript != null) dragScript.isMoveMode = false; // 드래그 잠금

        // 이동 UI 끄고 다시 편집 UI 켜기
        if (moveUIPanel != null) moveUIPanel.SetActive(false);
        editUIPanel.SetActive(true);

        // 다시 아이템을 타겟으로 카메라 줌인
        if (cameraMoveCoroutine != null) StopCoroutine(cameraMoveCoroutine);
        cameraMoveCoroutine = StartCoroutine(MoveCameraToTarget(selectedTarget.position));
    }



    // 오브젝트 삭제
    public void ClickDeleteButton()
    {
        // 팝업
        if (deleteConfirmPopUP != null) deleteConfirmPopUP.SetActive(true);
    }

    // [예] 눌렀을때
    public void ConfirmDelete()
    {
        if (selectedTarget != null)
        {
            // 씬에서 오브젝트 제거
            Destroy(selectedTarget.gameObject);
        }

        // 팝업 닫기
        if (deleteConfirmPopUP != null) deleteConfirmPopUP.SetActive(false);

        // 카메라 복구 및 편집 모드 종료
        editUIPanel.SetActive(false);
        buildUIPanel.SetActive(true);
        selectedTarget = null;

        if (cameraMoveCoroutine != null) StopCoroutine(cameraMoveCoroutine);
        cameraMoveCoroutine = StartCoroutine(MoveCameraToCoords(buildCameraPosition, buildCameraRotation));
    }

    // [아니오] 눌렀을때
    public void CancelDelete()
    {
        // 팝업만 닫음 (편집 상태 유지)
        if (deleteConfirmPopUP != null) deleteConfirmPopUP.SetActive(false );
    }




    // 가구 편집 중인지 판별
    public bool IsAlreadyEditing(Transform clickingObject)
    {
        // 방금 클릭한 오브젝트가 선택 되어있는 오브젝트가 아니라면 true
        if (selectedTarget != null && selectedTarget != clickingObject)
        {
            return true;
        }
        return false;
    }


    public void SetNormalMode() => ChangeMode(GameMode.Normal);
    public void SetBuildMode() => ChangeMode(GameMode.Build);
}

