// BoardData.cs
// 목적: 오목 보드의 "순수 데이터"만 관리한다.


public enum Cell
{
    Empty = 0,
    Human = 1,  
    AI = 2      
}

public class BoardData
{
    /*===== 필드/프로퍼티 =====*/
    public int Size { get; private set; }
    private Cell[,] grid; // 보드 배열

    //===== 생성/초기화 =====
    public BoardData(int size)
    {
        this.Size = size;
        grid = new Cell[size, size];
    }

    public void Clear()
    {
        for (int i=0; i<Size; i++)
            for (int j=0; j<Size; j++)
                grid[i,j] = Cell.Empty;
    }

    //  - 좌표가 보드 범위 내인지 확인
    public bool InBounds(int x, int y)
    {
        return x >= 0 && x < Size && y >= 0 && y < Size;
    }

    //  - 해당 좌표의 상태 반환 (범위 밖이면 예외 or Empty 처리 방식 팀 합의)
    public Cell Get(int x, int y)
    {
        if(InBounds(x, y))
            return grid[x, y];
        else
            return Cell.Empty;
    }
    
     //  - (주의) "실제 착수"가 아니라 내부/테스트/AI 임시용
    private void SetTemp(int x, int y, Cell value)
    {
        if(!InBounds(x, y))
            return;
        grid[x, y] = value;
    }


    // ===== 착수(게임에서 쓰는 핵심 API) =====

    //  - 비어있는지 확인
    public bool IsEmpty(int x, int y)
    {
        return InBounds(x,y) && grid[x,y] == Cell.Empty;
    }

    //  - InBounds + IsEmpty 체크
    //  - (금수/특수룰은 여기서 하지 말고 OmokRule에서 처리하는 편이 깔끔)
    public bool CanPlace(int x, int y)
    {
        return IsEmpty(x, y);
    }


    public bool TryPlace(int x, int y, Cell stone)
    {
        if(CanPlace(x,y))
        {
            grid[x,y] = stone;
            return true;
        }
        else
            return false;
    }
    //  - CanPlace 통과 시 grid[x,y]=stone 하고 true
    //  - 실패하면 false

    // ===== AI/룰 판정 지원 =====
    private Stack<(int x, int y, Cell prev)> tempStack = new Stack<(int, int, Cell)>();
    public void PlaceTemp(int x, int y, Cell stone)
    {
        if(!InBounds(x, y)) return;
        var prev = grid[x,y];
        if (prev != Cell.Empty) return;

        tempStack.Push((x,y,prev));
        SetTemp(x,y,stone);
    }
    public void UndoTemp()
    {
        if(tempStack.Count == 0)
            return;
        var(px, py, prev) = tempStack.Pop();
        SetTemp(px, py, prev);

    }
}
