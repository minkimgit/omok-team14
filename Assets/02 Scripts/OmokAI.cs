using System.Collections.Generic;
using UnityEngine;

public class OmokAI : MonoBehaviour
{
    //점수 가중치
    private const int FIVE = 100000;
    private const int OPEN_FOUR = 10000;
    private const int FOUR = 1000;
    private const int OPEN_THREE = 500;
    private const int THREE = 100;
    private const int OPEN_TWO = 50;
    private const int TWO = 10;

    public Vector2Int FindBestMove(BoardData board, Cell aiCell)
    {
        int bestScore = int.MinValue;
        Vector2Int bestMove = new Vector2Int(board.Size / 2, board.Size / 2); // 기본 중앙값
        
        List<Vector2Int> candidates = GetCandidateMoves(board);
        if (candidates.Count == 0) return bestMove;

        foreach (var move in candidates)
        {
            board.PlaceTemp(move.x, move.y, aiCell);

            // 미니맥스 호출 (depth 3)
            int score = Minimax(board, 3, false, aiCell, int.MinValue, int.MaxValue);
            
            board.UndoTemp(); // 착수 취소 (원상복구)

            if (score > bestScore)
            {
                bestScore = score;
                bestMove = move;
            }
        }
        return bestMove;
    }

    //미니맥스 알고리즘 (알파베타 가지치기 적용 - 이전에 찾은 다른 길보다 나쁘다는 것이 확실해지면 계산을 즉시 중단)
    private int Minimax(BoardData board, int depth, bool isMaximizing, Cell aiCell, int alpha, int beta)
    {
        if (depth == 0) return EvaluateBoard(board, aiCell);    //death(몇 수까지 내다볼 것인지)

        Cell opponent = (aiCell == Cell.AI) ? Cell.Human : Cell.AI;   //흑백 진영 확인

        List<Vector2Int> candidates = GetCandidateMoves(board);

        if (isMaximizing)   //AI 차례 - 최대 점수 찾기
        {
            int maxScore = int.MinValue;
            foreach (var move in candidates)
            {
                board.PlaceTemp(move.x, move.y, aiCell);
                int score = Minimax(board, depth - 1, false, aiCell, alpha, beta);
                board.UndoTemp();

                maxScore = Mathf.Max(maxScore, score);
                alpha = Mathf.Max(alpha, score);
                if (beta <= alpha) break;   //알파-베타 가지치기
            }
            return maxScore;
        }
        else    //플레이어 차례 - 최소 점수 찾기
        {
            int minScore = int.MaxValue;
            foreach (var move in candidates)
            {
                board.PlaceTemp(move.x, move.y, opponent);
                int score = Minimax(board, depth - 1, true, aiCell, alpha, beta);
                board.UndoTemp();

                minScore = Mathf.Min(minScore, score);
                beta = Mathf.Min(beta, score);
                if (beta <= alpha) break;   //알파-베타 가지치기
            }
            return minScore;
        }
    }

    // 전체 보드 상태 평가 (휴리스틱)
    private int EvaluateBoard(BoardData board, Cell aiCell)
    {
        int totalScore = 0;
        Cell opponent = (aiCell == Cell.AI) ? Cell.Human : Cell.AI;  //흑백 진영
        int size = board.Size;

        for (int i = 0; i < size; i++)
        {
            for (int j = 0; j < size; j++)
            {
                Cell stone = board.Get(i, j);
                if (stone == Cell.Empty) continue;  // 빈 칸 무시
                
                int score = EvaluateMove(board, i, j, stone);

                // 가중치 휴리스틱 - 중앙 점수 추가 (중앙에 가까울수록 높은 점수 구석으로 갈수록 낮은 점수)
                int center = size / 2;
                score += (center - Mathf.Abs(center - i)) + (center - Mathf.Abs(center - j));

                if (stone == aiCell) totalScore += score;

                else totalScore -= (int)(score * 1.5); // 방어에 가중치 부여
            }
        }
        return totalScore;
    }

    //종합 점수 평가(4방향 모두 평가 후 최종 판단)
    public int EvaluateMove(BoardData board, int x, int y, Cell stone)
    {
        int score = 0;
        score += EvaluateLine(board, x, y, 1, 0, stone);  // 가로
        score += EvaluateLine(board, x, y, 0, 1, stone);  // 세로
        score += EvaluateLine(board, x, y, 1, 1, stone);  // 대각선 ↘
        score += EvaluateLine(board, x, y, 1, -1, stone); // 대각선 ↗
        return score;
    }

    //점수 평가 - 연속된 돌의 개수와 열린 공간 여부에 따라 점수 부여
    private int EvaluateLine(BoardData board, int x, int y, int dx, int dy, Cell stone)
    {
        int count = 1 + OmokRule.CountOneDir(board, x, y, dx, dy, stone) 
                      + OmokRule.CountOneDir(board, x, y, -dx, -dy, stone);

        int openEnds = 0;
        // 양 끝이 비어있는지 확인
        if (CheckOpen(board, x, y, dx, dy, stone)) openEnds++;
        if (CheckOpen(board, x, y, -dx, -dy, stone)) openEnds++;

        if (count >= 5) return FIVE;
        if (count == 4) return (openEnds == 2) ? OPEN_FOUR : (openEnds == 1 ? FOUR : 0);
        if (count == 3) return (openEnds == 2) ? OPEN_THREE : (openEnds == 1 ? THREE : 0);
        if (count == 2) return (openEnds == 2) ? OPEN_TWO : (openEnds == 1 ? TWO : 0);
        return 0;
    }

    private bool CheckOpen(BoardData board, int x, int y, int dx, int dy, Cell stone)
    {
        int nx = x;
        int ny = y;
        while (board.InBounds(nx, ny) && board.Get(nx, ny) == stone)
        {
            nx += dx;
            ny += dy;
        }
        return board.InBounds(nx, ny) && board.Get(nx, ny) == Cell.Empty;
    }


    // 최적화 - 돌 주변 2칸 이내의 후보 수 줄이기
    private List<Vector2Int> GetCandidateMoves(BoardData board)
    {
        List<Vector2Int> candidates = new List<Vector2Int>();
        int size = board.Size;
        bool[,] considered = new bool[size, size];

        // 이미 놓인 돌 주변 탐색
        for (int i = 0; i < size; i++)
        {
            for (int j = 0; j < size; j++)
            {
                if (board.Get(i, j) != Cell.Empty)   // 돌이 있는 곳 발견
                {
                    for (int dx = -1; dx <= 1; dx++)    // 주변 2칸 이내 조사
                    {
                        for (int dy = -1; dy <= 1; dy++)
                        {
                            int nx = i + dx; int ny = j + dy;
                            if (nx >= 0 && nx < size && ny >= 0 && ny < size && 
                                board.Get(nx, ny) == Cell.Empty && !considered[nx, ny])  // 보드 안쪽이고, 빈칸이며, 중복이 아닐 때만 추가
                            {
                                candidates.Add(new Vector2Int(nx, ny));
                                considered[nx, ny] = true;
                            }
                        }
                    }
                }
            }
        }
        if (candidates.Count == 0) candidates.Add(new Vector2Int(size / 2, size / 2));  //AI 첫 수는 중앙

        return candidates;
    }
}
