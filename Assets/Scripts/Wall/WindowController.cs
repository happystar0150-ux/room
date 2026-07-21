using UnityEngine;

public class WindowController : MonoBehaviour {

    public enum WallAxis { XY, ZY }

    [Header("벽 방향")]
    public WallAxis wallAxis = WallAxis.XY;

    [Header("9-slice 설정")]
    public float cornerRadius = 0.2f;  // 블렌더 모서리 반경과 동일하게 설정

    [Header("벽 조각")]
    public GameObject wallFull;
    public GameObject wallTop;
    public GameObject wallBottom;
    public GameObject wallLeft;
    public GameObject wallRight;

    private Bounds wallBounds;
    private float halfW;
    private float halfH;
    private float wallDepth;
    private Vector3 dragOffset;
    private float minPieceSize = 0.05f;

    // 각 조각 원래 스케일 저장
    private Vector3 topOriginalScale;
    private Vector3 bottomOriginalScale;
    private Vector3 leftOriginalScale;
    private Vector3 rightOriginalScale;

    // 각 조각 원래 메시 크기 저장
    private Vector3 topOriginalMeshSize;
    private Vector3 bottomOriginalMeshSize;
    private Vector3 leftOriginalMeshSize;
    private Vector3 rightOriginalMeshSize;

    void Start() {
        wallBounds = wallFull.GetComponent<Renderer>().bounds;
        wallDepth = wallFull.transform.localScale.z;

        // 원래 스케일 저장
        topOriginalScale    = wallTop.transform.localScale;
        bottomOriginalScale = wallBottom.transform.localScale;
        leftOriginalScale   = wallLeft.transform.localScale;
        rightOriginalScale  = wallRight.transform.localScale;

        // 원래 메시 크기 저장 (스케일 1일 때 실제 메시 크기)
        topOriginalMeshSize    = GetMeshSize(wallTop);
        bottomOriginalMeshSize = GetMeshSize(wallBottom);
        leftOriginalMeshSize   = GetMeshSize(wallLeft);
        rightOriginalMeshSize  = GetMeshSize(wallRight);

        Bounds windowBounds = GetComponent<Renderer>().bounds;
        halfW = windowBounds.extents.x;
        halfH = windowBounds.extents.y;

        if (wallAxis == WallAxis.XY)
            halfW = windowBounds.extents.x;
        else
            halfW = windowBounds.extents.z;

        // wallFull.SetActive(true);
        // wallTop.SetActive(false);
        // wallBottom.SetActive(false);
        // wallLeft.SetActive(false);
        // wallRight.SetActive(false);
        UpdateWallPieces();
    }

    // 메시 자체 크기 반환 (스케일 제거)
    Vector3 GetMeshSize(GameObject obj) {
        Renderer r = obj.GetComponent<Renderer>();
        Vector3 size = r.bounds.size;
        Vector3 scale = obj.transform.lossyScale;

        if (wallAxis == WallAxis.ZY) {
            // ZY벽은 X랑 Z 뒤바꿔서 읽기
            return new Vector3(
                size.z / Mathf.Abs(scale.z),
                size.y / Mathf.Abs(scale.y),
                size.x / Mathf.Abs(scale.x));
        }
        return new Vector3(
            size.x / Mathf.Abs(scale.x),
            size.y / Mathf.Abs(scale.y),
            size.z / Mathf.Abs(scale.z));
    }

    public void ActivateWindow() {
        wallFull.SetActive(false);
        wallTop.SetActive(true);
        wallBottom.SetActive(true);
        wallLeft.SetActive(true);
        wallRight.SetActive(true);
        UpdateWallPieces();
    }

    void OnMouseDown() {
        dragOffset = transform.position - GetMouseWorldPos();
    }

    void OnMouseDrag() {
        Vector3 targetPos = GetMouseWorldPos() + dragOffset;

        if (wallAxis == WallAxis.XY) {
            float clampedX = Mathf.Clamp(targetPos.x,
                wallBounds.min.x + halfW + minPieceSize + cornerRadius,
                wallBounds.max.x - halfW - minPieceSize - cornerRadius);
            float clampedY = Mathf.Clamp(targetPos.y,
                wallBounds.min.y + halfH + minPieceSize + cornerRadius,
                wallBounds.max.y - halfH - minPieceSize - cornerRadius);
            transform.position = new Vector3(clampedX, clampedY, transform.position.z);
        } else {
            float clampedZ = Mathf.Clamp(targetPos.z,
                wallBounds.min.z + halfW + minPieceSize + cornerRadius,
                wallBounds.max.z - halfW - minPieceSize - cornerRadius);
            float clampedY = Mathf.Clamp(targetPos.y,
                wallBounds.min.y + halfH + minPieceSize + cornerRadius,
                wallBounds.max.y - halfH - minPieceSize - cornerRadius);
            transform.position = new Vector3(transform.position.x, clampedY, clampedZ);
        }

        UpdateWallPieces();
    }

    Vector3 GetMouseWorldPos() {
        float depth = Camera.main.WorldToScreenPoint(transform.position).z;
        Vector3 screenPos = new Vector3(Input.mousePosition.x, Input.mousePosition.y, depth);
        return Camera.main.ScreenToWorldPoint(screenPos);
    }

    void UpdateWallPieces() {
        if (wallAxis == WallAxis.XY)
            UpdatePiecesXY();
        else
            UpdatePiecesZY();
    }

    void UpdatePiecesXY() {
        float posX = transform.position.x;
        float posY = transform.position.y;
        float posZ = transform.position.z;

        float topH    = wallBounds.max.y - (posY + halfH);
        float bottomH = (posY - halfH) - wallBounds.min.y;
        float leftW   = (posX - halfW) - wallBounds.min.x;
        float rightW  = wallBounds.max.x - (posX + halfW);
        float fullW   = wallBounds.size.x;
        float holeH = halfH * 2f;

        SetPiece(wallTop,
            new Vector3(wallBounds.center.x, posY + halfH + topH / 2f, posZ),
            topOriginalScale, topOriginalMeshSize,
            new Vector2(fullW, topH));

        SetPiece(wallBottom,
            new Vector3(wallBounds.center.x, posY - halfH - bottomH / 2f, posZ),
            bottomOriginalScale, bottomOriginalMeshSize,
            new Vector2(fullW, bottomH));

        SetPiece(wallLeft,
            new Vector3(wallBounds.min.x + leftW / 2f, posY, posZ),
            leftOriginalScale, leftOriginalMeshSize,
            new Vector2(leftW, holeH));

        SetPiece(wallRight,
            new Vector3(wallBounds.max.x - rightW / 2f, posY, posZ),
            rightOriginalScale, rightOriginalMeshSize,
            new Vector2(rightW, holeH));
    }

    void UpdatePiecesZY() {
        float posZ = transform.position.z;
        float posY = transform.position.y;
        float posX = transform.position.x;

        float topH    = wallBounds.max.y - (posY + halfH);
        float bottomH = (posY - halfH) - wallBounds.min.y;
        float leftW   = (posZ - halfW) - wallBounds.min.z;
        float rightW  = wallBounds.max.z - (posZ + halfW);
        float fullW   = wallBounds.size.z;
        float holeH   = halfH * 2f;

        SetPiece(wallTop,
            new Vector3(posX, posY + halfH + topH / 2f, wallBounds.center.z),
            topOriginalScale, topOriginalMeshSize,
            new Vector2(fullW, topH));

        SetPiece(wallBottom,
            new Vector3(posX, posY - halfH - bottomH / 2f, wallBounds.center.z),
            bottomOriginalScale, bottomOriginalMeshSize,
            new Vector2(fullW, bottomH));

        SetPiece(wallLeft,
            new Vector3(posX, posY, wallBounds.min.z + leftW / 2f),
            leftOriginalScale, leftOriginalMeshSize,
            new Vector2(leftW, holeH));

        SetPiece(wallRight,
            new Vector3(posX, posY, wallBounds.max.z - rightW / 2f),
            rightOriginalScale, rightOriginalMeshSize,
            new Vector2(rightW, holeH));
    }

    void SetPiece(GameObject piece, Vector3 worldPos, Vector3 originalScale, Vector3 meshSize, Vector2 targetSize) {
    if (targetSize.x <= minPieceSize || targetSize.y <= minPieceSize) {
        piece.SetActive(false);
        return;
    }

    piece.SetActive(true);

    // 스케일 먼저 적용
    piece.transform.localScale = new Vector3(
        targetSize.x / meshSize.x,
        targetSize.y / meshSize.y,
        originalScale.z);

    // 스케일 적용 후 실제 bounds 중심으로 위치 보정
    Renderer r = piece.GetComponent<Renderer>();
    Vector3 boundsCenter = r.bounds.center;
    Vector3 offset = piece.transform.position - boundsCenter;
    piece.transform.position = worldPos + offset;
}
}