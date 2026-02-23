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

    public Vector2Int FindBestMove(OmokBoard board, int player)
    {
        int bestScore = int.MinValue;
        Vector2Int bestMove = new Vector2Int(7, 7); // 기본 중앙값

        List<Vector2Int> candidates = GetCandidateMoves(board);
        if (candidates.Count == 0) return bestMove;

        foreach (var move in candidates)
        {
            OmokBoard boardCopy = board.Clone();
            boardCopy.PlaceStone(move.x, move.y, player);

            // 미니맥스 알고리즘으로 가치 평가
            int score = Minimax(boardCopy, 3, false, player, int.MinValue, int.MaxValue);
            
            if (score > bestScore)
            {
                bestScore = score;
                bestMove = move;
            }
        }
        return bestMove;
    }

    //미니맥스 알고리즘 (알파베타 가지치기 적용 - 이전에 찾은 다른 길보다 나쁘다는 것이 확실해지면 계산을 즉시 중단)
    private int Minimax(OmokBoard board, int depth, bool isMaximizing, int player, int alpha, int beta)
    {
        if (depth == 0) return EvaluateBoard(board, player);    //death(몇 수까지 내다볼 것인지)

        int opponent = (player == 1) ? 2 : 1;   //흑백 진영 확인

        List<Vector2Int> candidates = GetCandidateMoves(board);

        if (isMaximizing)   //AI 차례 - 최대 점수 찾기
        {
            int maxScore = int.MinValue;
            foreach (var move in candidates)
            {
                OmokBoard boardCopy = board.Clone();
                boardCopy.PlaceStone(move.x, move.y, player);

                int score = Minimax(boardCopy, depth - 1, false, player, alpha, beta);
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
                OmokBoard boardCopy = board.Clone();
                boardCopy.PlaceStone(move.x, move.y, opponent);

                int score = Minimax(boardCopy, depth - 1, true, player, alpha, beta);
                minScore = Mathf.Min(minScore, score);
                beta = Mathf.Min(beta, score);
                if (beta <= alpha) break;   //알파-베타 가지치기
            }
            return minScore;
        }
    }

    // 전체 보드 상태 평가 (휴리스틱)
    private int EvaluateBoard(OmokBoard board, int player)
    {
        int totalScore = 0;
        int opponent = (player == 1) ? 2 : 1;   //흑백 진영

        for (int i = 0; i < OmokBoard.SIZE; i++)
        {
            for (int j = 0; j < OmokBoard.SIZE; j++)
            {
                int stone = board.GetCell(i, j);
                if (stone != 0)
                {
                    int score = EvaluateMove(board, i, j, stone);
                    // 가중치 휴리스틱: 중앙 점수 추가 (중앙에 가까울수록 높은 점수 구석으로 갈수록 낮은 점수)
                    score += (7 - Mathf.Abs(7 - i)) + (7 - Mathf.Abs(7 - j));

                    if (stone == player) totalScore += score;
                    else totalScore -= (int)(score * 1.5); // 방어에 가중치 부여
                }
            }
        }
        return totalScore;
    }

    //종합 점수 평가(4방향 모두 평가 후 최종 판단)
    public int EvaluateMove(OmokBoard board, int x, int y, int player)
    {
        int score = 0;
        score += EvaluateLine(board, x, y, 1, 0, player);  // 가로
        score += EvaluateLine(board, x, y, 0, 1, player);  // 세로
        score += EvaluateLine(board, x, y, 1, 1, player);  // 대각선 ↘
        score += EvaluateLine(board, x, y, 1, -1, player); // 대각선 ↗
        return score;
    }

    //점수 평가 - 연속된 돌의 개수와 열린 공간 여부에 따라 점수 부여
    private int EvaluateLine(OmokBoard board, int x, int y, int dx, int dy, int player)
    {
        int count = 1;  //연속되는 돌 개수
        int openEnds = 0;   //양 끝 막혀 있는지 여부

        // 양방향 탐색으로 연속된 돌과 열린 공간 확인
        void Check(int dxx, int dyy)
        {
            int nx = x + dxx, ny = y + dyy;
            while (nx >= 0 && nx < 15 && ny >= 0 && ny < 15 && board.GetCell(nx, ny) == player)
            {
                count++; nx += dxx; ny += dyy;
            }
            if (nx >= 0 && nx < 15 && ny >= 0 && ny < 15 && board.GetCell(nx, ny) == 0)
                openEnds++;
        }

        Check(dx, dy);
        Check(-dx, -dy);

        if (count >= 5) return FIVE;
        if (count == 4) return (openEnds == 2) ? OPEN_FOUR : (openEnds == 1 ? FOUR : 0);
        if (count == 3) return (openEnds == 2) ? OPEN_THREE : (openEnds == 1 ? THREE : 0);
        if (count == 2) return (openEnds == 2) ? OPEN_TWO : (openEnds == 1 ? TWO : 0);
        return 0;
    }


    // 최적화 - 돌 주변 2칸 이내의 후보 수 줄이기
    private List<Vector2Int> GetCandidateMoves(OmokBoard board)
    {
        List<Vector2Int> candidates = new List<Vector2Int>();
        bool[,] considered = new bool[15, 15];

        // 이미 놓인 돌 주변 탐색
        for (int i = 0; i < 15; i++)
        {
            for (int j = 0; j < 15; j++)
            {
                if (board.GetCell(i, j) != 0)   // 돌이 있는 곳 발견
                {
                    for (int dx = -2; dx <= 2; dx++)    // 주변 2칸 이내 조사
                    {
                        for (int dy = -2; dy <= 2; dy++)
                        {
                            int nx = i + dx; int ny = j + dy;
                            if (nx >= 0 && nx < 15 && ny >= 0 && ny < 15 && 
                                board.GetCell(nx, ny) == 0 && !considered[nx, ny])  // 보드 안쪽이고, 빈칸이며, 중복이 아닐 때만 추가
                            {
                                candidates.Add(new Vector2Int(nx, ny));
                                considered[nx, ny] = true;
                            }
                        }
                    }
                }
            }
        }
        return candidates;
    }
}
