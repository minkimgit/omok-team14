using UnityEngine;

public class OmokBoard : MonoBehaviour
{
    public const int SIZE = 15;
    public int[,] board; // 0: 빈칸, 1: 흑, 2: 백

    public OmokBoard()
    {
        board = new int[SIZE, SIZE];
    }

    // 보드 상태 가져오기
    public int GetCell(int x, int y) => board[x, y];

    //돌 놓기
    public bool PlaceStone(int x, int y, int player)
    {
        if (x < 0 || x >= SIZE || y < 0 || y >= SIZE || board[x, y] != 0)
            return false;
        board[x, y] = player;
        return true;
    }

    // 승리 확인
    public bool CheckWin(int x, int y, int player)
    {
        int[] dx = { 1, 0, 1, 1 }; // 가로, 세로, 대각선 \, 대각선 /
        int[] dy = { 0, 1, 1, -1 };

        for (int i = 0; i < 4; i++)
        {
            int count = 1;
            // 양방향으로 돌의 개수를 셉니다.
            count += CountDirection(x, y, dx[i], dy[i], player);
            count += CountDirection(x, y, -dx[i], -dy[i], player);
            if (count >= 5) return true;
        }
        return false;
    }

    private int CountDirection(int x, int y, int dx, int dy, int player)
    {
        int count = 0;

        // (x, y) 위치에서 (dx, dy) 방향으로 한 칸 이동한 위치부터 시작
        int nx = x + dx, ny = y + dy;

        // 보드 범위를 벗어나지 않았는지 확인 && 그 위치에 놓인 돌이 내가 찾는 플레이어의 돌인지 확인
        while (nx >= 0 && nx < SIZE && ny >= 0 && ny < SIZE && board[nx, ny] == player)
        {
            count++;
            nx += dx; ny += dy; //같은 방향으로 한 칸 이동
        }
        return count;
    }

    // 보드 복사 (AI 계산용)
    public OmokBoard Clone()
    {
        OmokBoard clone = new OmokBoard();
        for (int i = 0; i < SIZE; i++)
            for (int j = 0; j < SIZE; j++)
                clone.board[i, j] = this.board[i, j];
        return clone;
    }
}
