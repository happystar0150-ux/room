using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;

public class ObjectDrag : MonoBehaviour
{
    [HideInInspector] public bool isMoveMode = false;

    private bool isDraggingAllowed = false;

    // Ŭ�� ������ ���� ��ġ�� ���콺 ��ġ ����(������)
    private Vector3 offset;

    // �巡�� ���� �� ��ġ�� ȸ���� ����
    private Vector3 startPosition;
    private Quaternion startRotation;

    // ���� ��ħ ����
    public bool isOverlapping = false;

    [Header("��ġ �� ���̾� ����")]
    [Tooltip("������ �÷����� �� �ִ� ���̾�")]
    public LayerMask placementLayerMask;

    [Tooltip("���� ��ħ ���� �� ������ ���̾�")]
    public LayerMask ignoreOverlapMask;

    // ��Ƽ���� ���� �����(����)
    private Renderer[] renderers;
    private Dictionary<Renderer, MaterialColors[]> originalColors = new Dictionary<Renderer, MaterialColors[]>();

    // ��Ƽ���� �� ���� ����� �׸��� ������ ��� �����ϱ� ���� ����ü
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

    // ���� ������ �ٲ� �� �����Ƿ� ��Ƽ���� �� ���� ���� ĳ��
    public void CacheRenderers()
    {
        renderers = GetComponentsInChildren<Renderer>();
        originalColors.Clear();

        foreach (var rend in renderers)
        {
            if(rend == null) continue;

            // ��Ƽ���� ������ŭ ����ü �迭 ����
            MaterialColors[] colors = new MaterialColors[rend.materials.Length];

            for (int i = 0; i < rend.materials.Length; i++)
            {
                Material mat = rend.materials[i];

                // 1. �⺻ ���� ����
                if (mat.HasProperty("_BaseColor"))
                    colors[i].baseColor = mat.GetColor("_BaseColor");
                else if (mat.HasProperty("_Color"))
                    colors[i].baseColor = mat.color;

                // 2. (����Ƽ¯ �����̴���) �׸��� ���� ����
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
        // �̵� ��尡 �ƴ� �� �۵����� ����
        if (!isMoveMode) return;

        // 1. ���콺 Ŭ�� ����
        if (Input.GetMouseButtonDown(0))
        {
            // Ŭ�� ��ġ�� UI ����� �巡�� ����
            if (EventSystem.current != null && EventSystem.current.IsPointerOverGameObject())
            {

                isDraggingAllowed = false;
                return;

            }

            // ������̳� ������ ����� �����ٸ� �巡�� ���
            isDraggingAllowed = true;
            
            // �巡�� ���� �� ��ġ/ȸ�� ���
            startPosition = transform.position;
            startRotation = transform.rotation;

            // ���� ���� ���ɼ��� ����� ������ ��ĳ��
            CacheRenderers();

            // ���� ������ ��ġ�� Y ���̸� �������� ����� ����� ���� ������ ���
            Plane currentPlane = new Plane(Vector3.up, new Vector3(0, transform.position.y, 0));
            Vector3 mouseWorldPos = GetMouseWorldPositionOnPlane(currentPlane);

            // X, Z ���� ����� �Ÿ����� ���������� ����
            offset = transform.position - mouseWorldPos;
            offset.y = 0;
        }

        // 2. �巡�� ��
        if (Input.GetMouseButton(0) && isDraggingAllowed)
        {
            

            Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);

            // RaycastAll�� ������ ���� ��� ��ü ����
            RaycastHit[] hits = Physics.RaycastAll(ray, 500f, placementLayerMask);
            System.Array.Sort(hits, (x, y) => x.distance.CompareTo(y.distance));


            bool foundValidSurface = false;
            Vector3 targetPos = transform.position;
            
            foreach (var hit in hits)
            {
                // �ڱ� �ڽ� �� �ڽ� �ݶ��̴��� ���� ���� ����
                if (hit.transform.IsChildOf(this.transform)) continue;

                targetPos = hit.point + offset;
                targetPos.y = hit.point.y; // �ε��� ǥ�� ���̷� ����
                foundValidSurface = true;
                break;
            }

            // �ٴ�/ǥ�� ������ ��� ��� ���� ��� ���� ����
            if (!foundValidSurface)
            {
                Plane currentPlane = new Plane(Vector3.up, new Vector3(0, transform.position.y, 0));
                Vector3 currentMouseWorldPos = GetMouseWorldPositionOnPlane(currentPlane);
                targetPos = currentMouseWorldPos + offset;
                targetPos.y = transform.position.y;
            }

            transform.position = targetPos;

            // �ǽð� ��ħ ���� �� ������ ����
            CheckOverlapAndApplyVisuals();
          
        }

        // 3. ���콺 Ŭ�� ���� ��
        if (Input.GetMouseButtonUp(0))
        {
            if(isDraggingAllowed)
            {
                if (isOverlapping)
                {
                    // ��ģ ���¶�� �巡�� ���� �� ��ġ�� ����
                    transform.position = startPosition;
                    transform.rotation = startRotation;

                    // ��� �˾� ����
                    if (GameManager.Instance != null)
                    {
                        GameManager.Instance.ShowWarningPopup("�ٸ� ������ ��ġ�� ��ġ���� ���� �� �����ϴ�!");
                    }
                }
                else
                {
                    // ��ġ ���� �� ���� ��ġ�� ���ο� ���� ��ġ�� ����
                    startPosition = transform.position;
                    startRotation = transform.rotation;

                    if (GameManager.Instance != null)
                    {
                        GameManager.Instance.CheckPlacementValidity();
                    }
                }

                // ��Ƽ���� ���� ������� ����
                ResetVisuals();

                
                
            }

            // ���� �ʱ�ȭ
            isDraggingAllowed = false ;
        }
    }

    // OverlapBox�� Ȱ���� �ǽð� ���� ��ħ ����
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
                // �ڱ� �ڽ� �� �ڽ� �ݶ��̴� ����
                if (other.transform.IsChildOf(this.transform)) continue;

                // [����� ��] ���� ������ ��ħ ������ ���ٸ� �Ʒ� �ּ��� Ǯ�� �α׸� Ȯ��
                // Debug.Log($"��ģ ������Ʈ: {other.gameObject.name} (���̾�: {LayerMask.LayerToName(other.gameObject.layer)})");

                // �ٴ�/ǥ�� ���̾� ����
                if (((1 << other.gameObject.layer) & placementLayerMask) != 0) continue;

                // ignoreOverlapMask�� �ش�Ǵ� ���̾� ����
                if (((1 << other.gameObject.layer) & ignoreOverlapMask) != 0) continue;

                // �ٸ� ������ ���� ��ħ Ȯ��
                isOverlapping = true;
                break;
            }

            if (isOverlapping) break;
        }

        // ��ġ�� ������, �� ��ġ�� ���� ���� ����
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
                // URP�� Standard ���̴� ��� ����
                bool hasBaseColor = rend.materials[i].HasProperty("_BaseColor");
                bool hasColor = rend.materials[i].HasProperty("_Color");

                if (hasBaseColor || hasColor)
                {
                    if (tintColor == Color.clear) // ���󺹱�
                    {
                        
                        if (originalColors.ContainsKey(rend) && originalColors[rend].Length > i)
                        {
                            MaterialColors orig = originalColors[rend][i];

                            // �⺻ ���� ����
                            if (hasBaseColor) mat.SetColor("_BaseColor", orig.baseColor);
                            else if (hasColor) mat.color = orig.baseColor;

                            // �׸��� ���� ����
                            if (mat.HasProperty("_1st_ShadeColor"))
                                mat.SetColor("_1st_ShadeColor", orig.shade1Color);
                            if (mat.HasProperty("_2nd_ShadeColor"))
                                mat.SetColor("_2nd_ShadeColor", orig.shade2Color);
                        }
                    }
                    else // ������ ����
                    {
                        if (hasBaseColor)
                        {
                            mat.SetColor("_BaseColor", tintColor);

                            // �� ���̴� �׸��� ������ ���� ������ ������
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

    // ������ Y ������ ���� �������� ������ ���� ���ϱ�
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
