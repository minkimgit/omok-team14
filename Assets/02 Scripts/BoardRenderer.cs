using UnityEngine;
using static Constants;

public class BoardRenderer : MonoBehaviour
{
    [SerializeField] private GameObject cellPrefab;
    [SerializeField] private RectTransform boardParent;

    private GameObject[] _cells;

    void Start()
    {
        GenerateBoard();
    }

    private void GenerateBoard()
    {
        int totalCells = BOARD_SIZE * BOARD_SIZE;
        _cells = new GameObject[totalCells];

        for (int i = 0; i < totalCells; i++)
        {
            GameObject cell = Instantiate(cellPrefab, boardParent);
            cell.name = i.ToString();
            _cells[i] = cell;
        }
    }

    // 인덱스로 셀 가져오기
    public GameObject GetCell(int index)
    {
        if (index < 0 || index >= _cells.Length) return null;
        return _cells[index];
    }

    // 행/열로 셀 가져오기
    public GameObject GetCell(int row, int col)
    {
        return GetCell(row * BOARD_SIZE + col);
    }

    // 셀의 인덱스를 행/열로 변환
    public (int row, int col) IndexToRowCol(int index)
    {
        return (index / BOARD_SIZE, index % BOARD_SIZE);
    }

    // 행/열을 인덱스로 변환
    public int RowColToIndex(int row, int col)
    {
        return row * BOARD_SIZE + col;
    }
}
