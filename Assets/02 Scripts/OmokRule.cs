// OmokRule.cs
// 목적:오목의 규칙 판정(최소: 5목 승리)만 담당한다.

using System.Collections.Generic;
using UnityEngine;

public static class OmokRule
{
    // ===== 상수/방향 =====
    static readonly (int dx, int dy)[] DIRS =
    {
        (1, 0),   //가로 
        (0, 1),   //세로 
        (1, 1),   //윗대각 
        (1, -1)   //아래대각
    };

    // ===== 핵심 API =====
    public static bool CheckWin(BoardData board, int lastX, int lastY, Cell stone)
    {
        if(stone == Cell.Empty) return false; // 빈 칸에 놓인 돌은 승리 조건 검사할 필요 없음
        
        foreach(var(dx,dy) in DIRS)
        {
            int connected = CountConnected(board, lastX, lastY, dx, dy, stone);
            if(connected >= 5) return true;
        }
        return false;
    }
    //  - 마지막 수(lastX,lastY)에 놓인 stone 기준으로 4방향 검사
    //  - 한 방향에서 연속 개수 >= 5면 true

    // 승리한 방향의 연속된 돌 좌표 목록 반환 (x=col, y=row)
    public static List<Vector2Int> GetWinningLine(BoardData board, int lastX, int lastY, Cell stone)
    {
        foreach (var (dx, dy) in DIRS)
        {
            if (CountConnected(board, lastX, lastY, dx, dy, stone) < 5) continue;

            var line = new List<Vector2Int>();

            // 역방향으로 라인의 시작점까지 이동
            int sx = lastX, sy = lastY;
            while (board.InBounds(sx - dx, sy - dy) && board.Get(sx - dx, sy - dy) == stone)
            {
                sx -= dx;
                sy -= dy;
            }

            // 시작점부터 순방향으로 돌 좌표 수집
            int nx = sx, ny = sy;
            while (board.InBounds(nx, ny) && board.Get(nx, ny) == stone)
            {
                line.Add(new Vector2Int(nx, ny));
                nx += dx;
                ny += dy;
            }

            return line;
        }

        return new List<Vector2Int>(); // 승리 라인 없음
    }

    // ===== 내부 유틸 =====
    public static int CountConnected(BoardData board, int x, int y, int dx, int dy, Cell stone)
    {
        return 1 + CountOneDir(board,x,y,dx,dy,stone) + CountOneDir(board,x,y,-dx,-dy,stone);
    }
    //  - (x,y) 포함해서 양방향으로 stone 연속 개수 카운트
    //  - count = 1 + CountOneDir(+dx,+dy) + CountOneDir(-dx,-dy)

    public static int CountOneDir(BoardData board, int x, int y, int dx, int dy, Cell stone)
    {
        int count = 0;
        int nx = x + dx;
        int ny = y + dy;

        while(board.InBounds(nx,ny) && board.Get(nx, ny) == stone)
        {
            count++;
            nx += dx;
            ny += dy;
        }
        return count;
    }
    //  - 한 방향으로만 연속 stone 개수 카운트
    //  - 범위 밖 또는 다른 돌 나오면 종료
    public static bool IsForbiddenMove(BoardData board, int x, int y, Cell mover)
    {
        board.PlaceTemp(x, y, mover);
        if(IsOverline(board,x,y,mover))
        {
            board.UndoTemp();
            return true;
        }
       // 2) 44 (열린4가 2개 이상)
        int fours = CountOpenFours(board, x, y, mover);
        if (fours >= 2)
        {
            board.UndoTemp();
            return true;
        }

        // 3) 33 (열린3이 2개 이상)
        int threes = CountOpenThrees(board, x, y, mover);
        if (threes >= 2)
        {
            board.UndoTemp();
            return true;
        }
        board.UndoTemp();
        return false;
    }

    public static bool IsOverline(BoardData board, int x, int y, Cell mover)
    {
        foreach(var(dx,dy) in DIRS)
        {
            int connected = CountConnected(board, x, y, dx, dy, mover);
            if(connected >= 6) return true;
        }
        return false;
    }

    private static string GetLine(BoardData board, int cx, int cy, int dx, int dy, int radius, Cell me)
    {
        char[] buf = new char[2 * radius + 1];
        int idx = 0;

        for (int i = -radius; i <= radius; i++)
        {
            int x = cx + i * dx;
            int y = cy + i * dy;

            if (!board.InBounds(x, y))
            {
                buf[idx++] = '#';
                continue;
            }

            var c = board.Get(x, y);
            if (c == Cell.Empty) buf[idx++] = '.';
            else if (c == me)    buf[idx++] = 'O';
            else                 buf[idx++] = 'X';
        }

        return new string(buf);
    }

    // ===== Open Four(열린4) 카운트 =====
    //양끝 열림 + 4개 형성
    private static readonly string[] OPEN_FOUR_PATTERNS =
    {
        ".OOOO.",     // 직선 4
        ".OOO.O.",    // 끊긴 4
        ".OO.OO.",
        ".O.OOO."
    };

    public static int CountOpenFours(BoardData board, int x, int y, Cell me)
    {
        int total = 0;
        const int R = 5; // 11칸 라인(여유)
        
        foreach (var (dx, dy) in DIRS)
        {
            string line = GetLine(board, x, y, dx, dy, R, me);

            bool hasOpenFour = false;
            foreach (var pat in OPEN_FOUR_PATTERNS)
            {
                if (line.Contains(pat))
                {
                    hasOpenFour = true;
                    break;
                }
            }

            if (hasOpenFour) total += 1; // 이 방향에서 열린4가 있으면 1만 카운트
        }

        return total;
    }

    // ===== Open Three(열린3 / 자유3) 카운트 =====
    private static readonly string[] OPEN_THREE_PATTERNS =
    {
        "..OOO..",    // 직선 3 (양끝 2칸 여유)
        ".OOO..",
        "..OOO.",
        ".OO.O.",     // 끊긴 3
        ".O.OO.",
        ".OO..O.",
        ".O..OO."
    };

    public static int CountOpenThrees(BoardData board, int x, int y, Cell me)
    {
        int total = 0;
        const int R = 5;

        foreach (var (dx, dy) in DIRS)
        {
            string line = GetLine(board, x, y, dx, dy, R, me);

            // 열린4가 있으면 이 방향은 3으로 세지 않음(팀 합의 유지)
            bool hasFour = false;
            foreach (var pat4 in OPEN_FOUR_PATTERNS)
            {
                if (line.Contains(pat4)) { hasFour = true; break; }
            }
            if (hasFour) continue;

            bool hasOpenThree = false;
            foreach (var pat in OPEN_THREE_PATTERNS)
            {
                if (line.Contains(pat))
                {
                    hasOpenThree = true;
                    break;
                }
            }

            if (hasOpenThree) total += 1;
        }

        return total;
    }


    // ===== (선택) 무승부 판정 =====
    public static bool IsDraw(BoardData board)
    {
        for(int i = 0; i< board.Size; i++)
        {
            for(int j = 0; j < board.Size; j++)
            {
                if(board.Get(i, j) == Cell.Empty) return false;
            }
        }
        return true;
    }
}
