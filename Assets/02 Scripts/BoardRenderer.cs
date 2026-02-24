using System;
using DG.Tweening;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using static Constants;

[RequireComponent(typeof(BoxCollider2D))]
public class BoardRenderer : MonoBehaviour
{
    [Header("돌 프리팹")]
    [SerializeField] private GameObject blackStonePrefab;
    [SerializeField] private GameObject whiteStonePrefab;

    [Header("호버 프리팹")]
    [SerializeField] private GameObject hoverIndicatorPrefab;

    [Header("보드 세팅")]
    [SerializeField] private Camera gameCamera;
    // 보드 테두리에서 첫/마지막 격자선까지의 거리 (월드 단위)
    [SerializeField] private float boardPadding = 0.5f;

    // 셀 클릭 시 호출되는 콜백: (row, col)
    public Action<int, int> OnCellClicked;

    private SpriteRenderer _spriteRenderer;
    private float _cellWidth;
    private float _cellHeight;
    private GameObject _hoverIndicator;

    // 놓인 돌 오브젝트 추적
    private GameObject[,] _stones;

    private void Awake()
    {
        _stones = new GameObject[BOARD_SIZE, BOARD_SIZE];
        _spriteRenderer = GetComponent<SpriteRenderer>();

        if (gameCamera == null)
            gameCamera = Camera.main;
    }

    private void Start()
    {
        CalculateCellSize();

        if (hoverIndicatorPrefab != null)
        {
            _hoverIndicator = Instantiate(hoverIndicatorPrefab, transform);
            _hoverIndicator.SetActive(false);
        }
    }

    private void CalculateCellSize()
    {
        // 실제 플레이 가능한 영역 = 스프라이트 크기 - 양쪽 패딩
        float playableWidth  = _spriteRenderer.bounds.size.x - (boardPadding * 2f);
        float playableHeight = _spriteRenderer.bounds.size.y - (boardPadding * 2f);

        // 15줄 = 14칸 간격
        _cellWidth  = playableWidth  / (BOARD_SIZE - 1);
        _cellHeight = playableHeight / (BOARD_SIZE - 1);
    }

    private void Update()
    {
        HandleHover();

        if (Mouse.current.leftButton.wasPressedThisFrame)
            HandleClick();
    }

    private void HandleHover()
    {
        if (EventSystem.current.IsPointerOverGameObject()) return;
        
        if (_hoverIndicator == null) return;

        Vector2 mouseWorldPos = gameCamera.ScreenToWorldPoint(Mouse.current.position.ReadValue());
        RaycastHit2D hit = Physics2D.Raycast(mouseWorldPos, Vector2.zero);

        if (hit.collider == null || hit.collider.gameObject != gameObject)
        {
            _hoverIndicator.SetActive(false);
            return;
        }

        (int row, int col) = WorldToGrid(hit.point);

        if (_stones[row, col] != null)
        {
            _hoverIndicator.SetActive(false);
            return;
        }

        _hoverIndicator.transform.position = GridToWorldPosition(row, col);
        _hoverIndicator.SetActive(true);
    }

    private void HandleClick()
    {
        Vector2 mouseWorldPos = gameCamera.ScreenToWorldPoint(Mouse.current.position.ReadValue());
        
        if (EventSystem.current.IsPointerOverGameObject()) return;

        // 보드 콜라이더에만 반응
        RaycastHit2D hit = Physics2D.Raycast(mouseWorldPos, Vector2.zero);

        if (hit.collider == null || hit.collider.gameObject != gameObject)
            return;

        (int row, int col) = WorldToGrid(hit.point);
        OnCellClicked?.Invoke(row, col);
    }

    // 돌 놓기: player (1 = 흑, 2 = 백)
    public void PlaceStoneAt(int row, int col, int player)
    {
        if (!IsValidPosition(row, col) || _stones[row, col] != null) return;

        GameObject prefab = (player == 1) ? blackStonePrefab : whiteStonePrefab;
        if (prefab == null) return;

        Vector3 worldPos = GridToWorldPosition(row, col);
        _stones[row, col] = Instantiate(prefab, worldPos, Quaternion.identity, transform);

        _stones[row, col].transform.localScale = Vector3.zero;
        _stones[row, col].transform.DOScale(1, .3f).SetEase(Ease.OutBack);
    }

    // 특정 위치의 돌 제거
    public void RemoveStoneAt(int row, int col)
    {
        if (!IsValidPosition(row, col) || _stones[row, col] == null) return;
        Destroy(_stones[row, col]);
        _stones[row, col] = null;
    }

    // 모든 돌 제거
    public void ClearBoard()
    {
        for (int row = 0; row < BOARD_SIZE; row++)
            for (int col = 0; col < BOARD_SIZE; col++)
                RemoveStoneAt(row, col);
    }

    // 그리드의 [0,0] 시작점 (스프라이트 중앙 기준으로 계산)
    private Vector2 GetGridOrigin()
    {
        Vector2 center = _spriteRenderer.bounds.center;
        float startX = center.x - (BOARD_SIZE - 1) * _cellWidth  / 2f;
        float startY = center.y + (BOARD_SIZE - 1) * _cellHeight / 2f;
        return new Vector2(startX, startY);
    }

    // [row, col] → 월드 좌표
    // [0, 0] = 좌상단, [14, 14] = 우하단
    public Vector3 GridToWorldPosition(int row, int col)
    {
        Vector2 origin = GetGridOrigin();

        float x = origin.x + col * _cellWidth;
        float y = origin.y - row * _cellHeight;

        // 돌이 보드 위에 렌더링되도록 z축 약간 앞으로
        return new Vector3(x, y, transform.position.z - 0.1f);
    }

    // 월드 좌표 → 가장 가까운 [row, col]
    public (int row, int col) WorldToGrid(Vector2 worldPos)
    {
        Vector2 origin = GetGridOrigin();

        float relX = worldPos.x - origin.x;
        float relY = origin.y - worldPos.y;

        int col = Mathf.RoundToInt(relX / _cellWidth);
        int row = Mathf.RoundToInt(relY / _cellHeight);

        return (Mathf.Clamp(row, 0, BOARD_SIZE - 1), Mathf.Clamp(col, 0, BOARD_SIZE - 1));
    }

    // 특정 위치의 돌 오브젝트 가져오기
    public GameObject GetStoneAt(int row, int col)
    {
        if (!IsValidPosition(row, col)) return null;
        return _stones[row, col];
    }

    private bool IsValidPosition(int row, int col)
    {
        return row >= 0 && row < BOARD_SIZE && col >= 0 && col < BOARD_SIZE;
    }

    // 씬 뷰에서 교차점 위치를 노란 점으로 시각화 (디버그용)
    private void OnDrawGizmosSelected()
    {
        SpriteRenderer sr = GetComponent<SpriteRenderer>();
        if (sr == null) return;

        float playableWidth  = sr.bounds.size.x - (boardPadding * 2f);
        float playableHeight = sr.bounds.size.y - (boardPadding * 2f);
        float cellW = playableWidth  / (BOARD_SIZE - 1);
        float cellH = playableHeight / (BOARD_SIZE - 1);

        Vector2 center = sr.bounds.center;
        float startX = center.x - (BOARD_SIZE - 1) * cellW / 2f;
        float startY = center.y + (BOARD_SIZE - 1) * cellH / 2f;

        Gizmos.color = Color.yellow;
        for (int row = 0; row < BOARD_SIZE; row++)
        {
            for (int col = 0; col < BOARD_SIZE; col++)
            {
                float x = startX + col * cellW;
                float y = startY - row * cellH;
                Gizmos.DrawSphere(new Vector3(x, y, transform.position.z), 0.05f);
            }
        }
    }
}
