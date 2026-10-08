using System.Collections;
using System.Collections.Generic;
//using System.Timers;
using UnityEngine;
using UnityEngine.UI;

[System.Serializable]
public class CategoryButtonData
{
    public string categoryName;         // ī�װ����
    public Image buttonImage;           // ��ư image ������Ʈ
    public Sprite normalSprite;         // �⺻ �̹���
    public Sprite selectedSprite;       // ���õǾ��� �� �̹���   
}
public class GameManager : MonoBehaviour
{
    // ���� ���� ���
    public GameMode currentMode = GameMode.Normal;

    
    [Header("UI Panels")]
    public GameObject normalUIPanel;
    public GameObject buildUIPanel;
    public GameObject editUIPanel;
    public GameObject moveUIPanel;
    public GameObject addUIPanel;
    public GameObject deleteConfirmPopUP;
    public GameObject restartConfirmPopUP;

    

    [Header("Furniture Spawn System")]
    public GameObject commonFurniturePrefab;

   

    [Header("Camera Reference")]
    public Transform cameraTransform; // ���� ī�޶� Transform
    public Vector3 cameraOffset = new Vector3(0, 5, -5); // ������Ʈ�� �ٶ� ī�޶��� ����� ��ġ��

    // ���� ���� ���� ������Ʈ transform ���
    private Transform selectedTarget;

    // ��庰 ī�޶� ����
    // normal
    private Vector3 normalCameraPosition;
    private Quaternion normalCameraRotation;
    // build
    private Vector3 buildCameraPosition;
    private Quaternion buildCameraRotation;

    private Coroutine cameraMoveCoroutine;

    // ������Ʈ ������ �������� ���� ������Ʈ�� ���
    private GameObject currentActiveFurniture;



    public static GameManager Instance { get; private set; }

    [Header("���� ���")]
    // ������Ʈ â�� ��� ���� �����͸� �־�� ����Ʈ
    public List<FurnitureData> allFurnitureDataList = new List<FurnitureData>();
    // ���� ������ ��ư ��ǰ
    public GameObject furnitureItemPrefab;
    // FurnitureContent �θ� ������Ʈ
    public Transform furnitureContentParent;

    [Header("���� ��ġ / ���� ��� ���� ���� ������Ʈ")]
    public GameObject currentSpawnedObject;


    [Header("UI �˾�")]
    public GameObject warningPopupPanel; // ��� �˾� ui ������Ʈ
    public TMPro.TMP_Text warningText; // (���û���) ��� ���� �ؽ�Ʈ

    private Coroutine warningCoroutine;



    [Header("ī�װ�� �� ��ư ����")]
    // �� ����Ʈ�� a, b, c, d, e ī�װ�� ��ư���� ����ϴ�.
    public List<CategoryButtonData> categoryButtonList = new List<CategoryButtonData>();


    
    private void Awake()
    {
        if (Instance == null) Instance = this;
        else Destroy(gameObject);
    }

    
    private void Update()
    {
        
        // build ����϶� ���� Ŭ�� �缱�� ����
        if (currentMode == GameMode.Build)
        {
            if (Input.GetMouseButtonDown(0))
            {
                // ui Ŭ�� ���̸� ����
                if (UnityEngine.EventSystems.EventSystem.current.IsPointerOverGameObject())
                    return;

                HandleFurnitureSelectionClick();
            }
        }
        

        // �׽�Ʈ
        if (Input.GetMouseButtonDown(0))
        {
            Debug.Log($"���콺 Ŭ����! ���� ���� ���: {currentMode}");

            if (UnityEngine.EventSystems.EventSystem.current != null &&
                UnityEngine.EventSystems.EventSystem.current.IsPointerOverGameObject())
            {
                Debug.LogWarning("���� ���콺 ��ġ �Ʒ� UI ��Ұ� �־� Ŭ���� ���ܵǾ����ϴ�!");
                return;
            }

            if (currentMode != GameMode.Build)
            {
                Debug.Log($"���� ��尡 Build�� �ƴ϶� '{currentMode}'�� ����ĳ��Ʈ�� ���� �ʾҽ��ϴ�.");
                return;
            }

            Debug.Log("��� ������ �����Ͽ� �������� �߻��մϴ�.");
            HandleFurnitureSelectionClick();
        }
    }

    // ���콺 �������� ������ �����ؼ� �����ϴ� �Լ�
    private void HandleFurnitureSelectionClick()
    {
        Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);
        RaycastHit hit;

        Debug.Log("������ �߻�");

        if (Physics.Raycast(ray, out hit, 500f))
        {
            Debug.Log($"�������� �ε��� ������Ʈ: {hit.transform.name}");

            // �ε��� �ڽ� �ݶ��̴��� �θ𿡼� SelectionManager�� ã���ϴ�
            SelectionManager selManager = hit.transform.GetComponentInParent<SelectionManager>();

            if (selManager != null)
            {
                Debug.Log($"[���� ���� ����] {selManager.gameObject.name} ������ �����մϴ�.");
                selManager.OnSelectedByClick();
            }
            else
            {
                Debug.LogWarning($"{hit.transform.name}�� �θ𿡼� SelectionManager�� ã�� ���߽��ϴ�.");
            }
        }
    }

    void Start()
    {
        

        // ī�޶� ����
        // normal
        normalCameraPosition = cameraTransform.position;
        normalCameraRotation = cameraTransform.rotation;
        // build
        buildCameraPosition = cameraTransform.position;
        buildCameraRotation = cameraTransform.rotation;


        // ���� ���� �� �⺻ ���� ����
        ChangeMode(GameMode.Normal);

        if (deleteConfirmPopUP != null) deleteConfirmPopUP.SetActive(false);
        if (moveUIPanel != null) moveUIPanel.SetActive(false);
        if (restartConfirmPopUP != null) restartConfirmPopUP.SetActive(false);

        

    }

    // ��� ���� �Լ� (UI��ư�� ����)
    public void ChangeMode(GameMode newMode)
    {
        currentMode = newMode;

        // ���¿� ���� UI �� �ý��� Ȱ��ȭ/��Ȱ��ȭ
        switch (currentMode)
        {
            case GameMode.Normal:
                normalUIPanel.SetActive(true);
                buildUIPanel.SetActive(false);
                editUIPanel.SetActive(false);
                addUIPanel.SetActive(false);
                

                // �Ϲ� ���� ���ƿ� �� ī�޶� ����ġ
                if (cameraMoveCoroutine != null) StopCoroutine(cameraMoveCoroutine);
                cameraMoveCoroutine = StartCoroutine(MoveCameraToCoords(normalCameraPosition, normalCameraRotation));
                break;

            case GameMode.Build:
                normalUIPanel.SetActive(false);
                buildUIPanel.SetActive(true);
                editUIPanel.SetActive(false);
                addUIPanel.SetActive(false);

                /*
                // ���� ���� �����ϴ� ������ ī�޶� ���¸� ���
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

                // A ī�װ�� �ٷ� ����
                FilterFurnitureMenu("A");
                break;
        }
    }


    // ������Ʈ ����
    public void ClickSpawnButtonAtCenter(FurnitureData data)
    {
        if (data == null) return;
  
        // �� �Ѱ�� ��ǥ �� ȸ�� ����
        Vector3 centerPos = new Vector3(0.5f, 0f, 0f);
        Quaternion targetRotation = Quaternion.Euler(-90f, 0f, 0f);

        // ���� ���� ���� ���� (Ȥ�ø���ϱ�)
        if (currentSpawnedObject != null) Destroy(currentSpawnedObject);

        // ���������� ��¥ ���� ����
        currentSpawnedObject = Instantiate(commonFurniturePrefab, centerPos, targetRotation);

        // ��Ʈ ���� ���� ����
        FurnitureSetup setup = currentSpawnedObject.GetComponent<FurnitureSetup>();
        if (setup != null)
        {
            setup.SetupFurniture(data);
        }

        // Ȯ�� ���̹Ƿ� ī�޶� ���� ���̾� ó�� ���� ����
        // ������ ��� ui ���� ������ ��� �� �ֵ��� ���

        // ��带 Add ���� ����
        ChangeMode(GameMode.Add);

    }

    



    // ī�װ�� ��ư Ŭ����
    public void FilterFurnitureMenu(string categoryToFilter)
    {
        Debug.Log($"{categoryToFilter} ī�װ���� ���õǾ����ϴ�! ����� �����մϴ�.");

        // 0. �̹��� ������Ʈ(����)
        UpdateCategoryButtonImages(categoryToFilter);

        // 1. ���� ȭ���� ���� ��� ������ ui�� ����
        foreach (Transform child in furnitureContentParent)
        {
            Destroy(child.gameObject);
        }

        // 2. ��ü ���� ������ �߿��� ��� Ŭ���� ī�װ���� ��ġ�ϴ� ������ ��ư���� ����
        foreach (FurnitureData data in allFurnitureDataList)
        {
            // ��ҹ��� ����, ��ĭ ���� �Ȱ����� ��
            if (data.categoryGroup.Trim().Equals(categoryToFilter.Trim(), System.StringComparison.OrdinalIgnoreCase))
            {
                // ��ư ������ ����
                GameObject newBtn = Instantiate(furnitureItemPrefab, furnitureContentParent);

                // ������ ��ư�� ���� ������(�̸�, ������) ����
                FurnitureItemUI itemUI = newBtn.GetComponent<FurnitureItemUI>();
                if (itemUI != null)
                {
                    itemUI.Setup(data);
                }
            }
        }
    }

    // �̹��� ���� �Լ�
    private void UpdateCategoryButtonImages(string activeCategory)
    {
        foreach (var btnData in categoryButtonList)
        {
            if (btnData.buttonImage == null)
            {
                Debug.LogWarning($"[{btnData.categoryName}] ��ư�� buttonImage�� �Ҵ���� �ʾҽ��ϴ�!");
                continue;
            }

            if (btnData.selectedSprite == null || btnData.normalSprite == null)
            {
                Debug.LogWarning($"[{btnData.categoryName}] ��ư�� ��������Ʈ(Normal/Selected)�� �Ҵ���� �ʾҽ��ϴ�!");
            }

            bool isMatch = btnData.categoryName.Trim().Equals(activeCategory.Trim(), System.StringComparison.OrdinalIgnoreCase);

            // ����Ʈ�� ��ϵ� ī�װ�� �̸��� ���� ���õ� ī�װ�� �̸��� ���ٸ�
            if (btnData.categoryName.Trim().Equals(activeCategory.Trim(), System.StringComparison.OrdinalIgnoreCase))
            {
                // �ڽ��� ���õ� �̹����� ����
                btnData.buttonImage.sprite = btnData.selectedSprite;
            }
            else
            {
                // �ٸ� ��ư���� ���� ���� ���·� ����
                btnData.buttonImage.sprite = btnData.normalSprite;
            }
        }
    }

    // ���� ��� ui���� �ٸ� ������ Ŭ�� ���� �� ȣ���� �Լ�
    public void SwitchFurnitureData(FurnitureData newData)
    {
        

        if (currentSpawnedObject == null)
        {
            Debug.LogWarning("���� ȭ�鿡 ���� ���� ���� ������Ʈ�� �����ϴ�!");
            return;
        }


        Debug.Log($"���� ������ '{newData.furnitureName}'(��)�� ��ȯ�մϴ�.");

        // FurnitureSetup���� �� ������ �Ѱ��ֱ�
        FurnitureSetup setup = currentSpawnedObject.GetComponent<FurnitureSetup>();
        if (setup != null)
        {
            setup.SetupFurniture(newData);
        }

    }

    // Ȯ�� �� ���� ��� ��ȯ
    public void ConfirmPlacement()
    {
        if (currentSpawnedObject == null) return;

        

        // ��¥ ��ġ�� ������ �ٲ���
        GameObject confirmedFurniture = currentSpawnedObject;

        // ���� ����
        currentSpawnedObject = null;

        // ���̾� ����
        SetLayerRecursively(confirmedFurniture, LayerMask.NameToLayer("Selected"));

        // �ܰ���
        SelectionManager selManager = confirmedFurniture.GetComponent<SelectionManager>();
        if (selManager != null)
        {
            selManager.SetStencilValue(15);
        }

        // ����(�������+����)��Ű��
        SelectionObject(confirmedFurniture.transform);

        // ��� ����
        currentMode = GameMode.Build;
    }

    // �ڽĵ� ����
    private void SetLayerRecursively(GameObject obj, int newLayer)
    {
        if (obj == null) return;

        // ���� ���� ������Ʈ�� ���̾ 'FurnitureSurface'��� ���̾ �ٲ��� �ʰ� ����
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

   // ���� ���� ���
   public void CancelSpawn()
    {
        // ���� ���� �����Ͽ� �������� ������ ����
        if (currentSpawnedObject != null)
        {
            Destroy(currentSpawnedObject);
            currentSpawnedObject = null; // ���� �ʱ�ȭ
        }
    }

   




    // ������Ʈ ���� ��
    public void SelectionObject(Transform targetTransform)
    {
        // ���� UI Ȱ��ȭ
        editUIPanel.SetActive(true);

        // ���� UI ��Ȱ��ȭ
        buildUIPanel.SetActive(false);

        // ���� UI ��Ȱ��ȭ
        addUIPanel.SetActive(false);

        // ���� ������Ʈ ���
        selectedTarget = targetTransform;

        

        // ī�޶� �̵�
        if (cameraMoveCoroutine != null ) StopCoroutine(cameraMoveCoroutine);
        cameraMoveCoroutine = StartCoroutine(MoveCameraToTarget(targetTransform.position));
    }

    // ������Ʈ ���� ���� ��
    public void DeselectObject()
    {
        // ���� UI ��Ȱ��ȭ
        editUIPanel.SetActive(false);

        // ���� UI Ȱ��ȭ
        buildUIPanel.SetActive(true);

        // ������� ������Ʈ �ر�
        selectedTarget = null;

        // ī�޶� ����
        if (cameraMoveCoroutine != null) StopCoroutine(cameraMoveCoroutine);
        cameraMoveCoroutine = StartCoroutine(MoveCameraToCoords(buildCameraPosition, buildCameraRotation));
    }

    // ī�޶� �ε巴�� Ÿ�� ��ġ(+������)�� �̵���Ű�� �ڷ�ƾ
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

            // �ε巯�� ����/���� ����
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

    // ���� �Ϸ�(����) ��ư
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
        // ���� ���� �� ī�޶� ���� ����
        DeselectObject();
        // ���� ��� build�� ����
        ChangeMode(GameMode.Build);
    }
    

    // ������Ʈ �̵�
    public void StartMoveMode()
    {
        if (selectedTarget == null) return;

        

        // ���� UI ��� �̵� UI �ѱ�
        editUIPanel.SetActive(false);
        if (moveUIPanel != null) moveUIPanel.SetActive(true);

        // ī�޶� ��ġ �ǵ�����
        if (cameraMoveCoroutine != null) StopCoroutine(cameraMoveCoroutine);
        cameraMoveCoroutine = StartCoroutine(MoveCameraToCoords(buildCameraPosition, buildCameraRotation));

        // �ش� ������Ʈ�� ObjectDrag ��ũ��Ʈ ã�� �̵� ���� ���·� ����
        ObjectDrag dragScript = selectedTarget.GetComponent<ObjectDrag>();
        if (dragScript != null) dragScript.isMoveMode = true;
        
    }

    // ȸ�� ��ư Ŭ�� ��
    public void RotateObject(float angle) // angle�� 45 Ȥ�� -45
    {
        if (selectedTarget != null)
        {
            /*
             * //������ �������� �ϴ°�
            
            selectedTarget.Rotate(Vector3.up, angle, Space.Self);
            Debug.Log($"{selectedTarget.name} ȸ����! ���� ����: {selectedTarget.eulerAngles.y}");
            */

            // ���� ��ǥ �������� �ϴ°�
            // ���� ȸ�������� Y�� �������� angle��ŭ �� ȸ��
            if (selectedTarget != null)
            {
                selectedTarget.Rotate(Vector3.up, angle, Space.World);
            }
        }
    }

    // �巡�� ������ �� (���콺���� �� ���� ��)
    public void CheckPlacementValidity()
    {
        
        bool canPlace = true;

        /* // ���߿� �߰��ؿ�:
         * if (���� �ε����ų� �ٸ� ������ ��ģ�ٸ�)
         * {
         *     canPlace = false
         * }
         */

        // ���� �� ������
        if (canPlace)
        {

            Debug.Log("�巡�� �ӽ� ��ġ �Ϸ� (�̵� ��� ���� ��)");
            
        }
        else
        {
            // ���� �� ���� ��� ���ڸ��� ƨ��� ��� ����
        }
    }

    public void EndMoveAndReturnToEdit()
    {
        if (selectedTarget == null) return;

        // �̵� ��带 �����ϰ� �ٽ� ���� ���·� ���
        ObjectDrag dragScript = selectedTarget.GetComponent<ObjectDrag>();
        if (dragScript != null) dragScript.isMoveMode = false; // �巡�� ���

        // �̵� UI ��� �ٽ� ���� UI �ѱ�
        if (moveUIPanel != null) moveUIPanel.SetActive(false);
        editUIPanel.SetActive(true);

        // �ٽ� �������� Ÿ������ ī�޶� ����
        if (cameraMoveCoroutine != null) StopCoroutine(cameraMoveCoroutine);
        cameraMoveCoroutine = StartCoroutine(MoveCameraToTarget(selectedTarget.position));
    }



    // ������Ʈ ����
    public void ClickDeleteButton()
    {
        // �˾�
        if (deleteConfirmPopUP != null) deleteConfirmPopUP.SetActive(true);
    }

    // [��] ��������
    public void ConfirmDelete()
    {
        if (selectedTarget != null)
        {
            // ������ ������Ʈ ����
            Destroy(selectedTarget.gameObject);
        }

        // �˾� �ݱ�
        if (deleteConfirmPopUP != null) deleteConfirmPopUP.SetActive(false);

        // ī�޶� ���� �� ���� ��� ����
        editUIPanel.SetActive(false);
        buildUIPanel.SetActive(true);
        selectedTarget = null;

        if (cameraMoveCoroutine != null) StopCoroutine(cameraMoveCoroutine);
        cameraMoveCoroutine = StartCoroutine(MoveCameraToCoords(buildCameraPosition, buildCameraRotation));
    }

    // [�ƴϿ�] ��������
    public void CancelDelete()
    {
        // �˾��� ���� (���� ���� ����)
        if (deleteConfirmPopUP != null) deleteConfirmPopUP.SetActive(false );
    }




    // ���� ���� ������ �Ǻ�
    public bool IsAlreadyEditing(Transform clickingObject)
    {
        // ��� Ŭ���� ������Ʈ�� ���� �Ǿ��ִ� ������Ʈ�� �ƴ϶�� true
        if (selectedTarget != null && selectedTarget != clickingObject)
        {
            return true;
        }
        return false;
    }


    public void SetNormalMode() => ChangeMode(GameMode.Normal);
    public void SetBuildMode() => ChangeMode(GameMode.Build);


    // ��� �˾� ȣ�� �Լ�
    public void ShowWarningPopup(string message = "�ش� ��ġ���� ������ ���� �� �����ϴ�.")
    {
        if (warningPopupPanel == null) return;

        if (warningText != null)
        {
            warningText.text = message;
        }

        if (warningCoroutine != null)
        {
            StopCoroutine(warningCoroutine);
        }

        warningCoroutine = StartCoroutine(HideWarningPopupRoutine(2.0f)); // 2�� �� �ڵ� ��Ȱ��ȭ
    }

    private IEnumerator HideWarningPopupRoutine(float delay)
    {
        warningPopupPanel.SetActive(true);
        yield return new WaitForSeconds(delay);
        warningPopupPanel.SetActive(false);
    }




    public void ClickRestartButton()
    {
        // �˾�
        if (restartConfirmPopUP != null) restartConfirmPopUP.SetActive(true);
    }

    

    // [�ƴϿ�] ��������
    public void CancelRestart()
    {
        // �˾��� ���� (���� ���� ����)
        if (restartConfirmPopUP != null) restartConfirmPopUP.SetActive(false);
    }
}

