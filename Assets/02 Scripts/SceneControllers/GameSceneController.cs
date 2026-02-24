using DG.Tweening;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.UI;
using static Constants;

public class GameSceneController : MonoBehaviour
{
    [SerializeField] private OmokAI omokAI;
    [SerializeField] private Image playerATurnImage;
    [SerializeField] private Image playerBTurnImage;
    [SerializeField] private RectTransform playerATurnRect;
    [SerializeField] private RectTransform playerBTurnRect;
    [SerializeField] private BoardRenderer boardRenderer;
    private string surrenderText = "게임을 기권하시겠습니까? 기권시 패배로 처리됩니다.";
    private string victoryText = "승리하였습니다.";
    private string defeatText = "패배하였습니다.";
    

    
    private AudioManager _audioManager;
    private BoardData _boardData;
    private GameType _currentGameType;
    private int _currentPlayer = 1; // 1 = 흑, 2 = 백
    private bool _aiTurn = false; // AI의 턴 여부

    private void Start()
    {
        _audioManager = FindObjectOfType<AudioManager>();
        _boardData = new BoardData(BOARD_SIZE);
        _currentGameType = GameManager.Instance.CurrentGameType;
        
        boardRenderer.OnCellClicked += OnCellClicked;
        GameManager.Instance.SetGameTurn(PlayerType.Player1);
    }

    // int 플레이어 번호를 Cell 열거형으로 변환
    private Cell PlayerToCell(int player) => player == 1 ? Cell.Human : Cell.AI;
    
    //**돌 배치**
    private void OnCellClicked(int row, int col)
    {
        if (_aiTurn) return; // AI의 턴인 경우 클릭 무시
        if (!PlaceStone(row, col)) return;  // 유효하지 않은 위치에 돌을 놓으려는 경우 무시

        // 싱글 플레이고 현재 턴이 AI라면 AI 실행
        if (_currentGameType == GameType.SinglePlay && _currentPlayer == 2)
        {
            // 플레이어가 바로 클릭하지 못하게 하거나 AI의 생각을 표현하기 위해 코루틴 사용
            StartCoroutine(AIDelayRoutine());
        }
    }

    private bool PlaceStone(int row, int col)
    {
        Cell currentCell = PlayerToCell(_currentPlayer);

        if (currentCell == Cell.Human) // 흑돌인가
        {
            if (OmokRule.IsForbiddenMove(_boardData, col, row, currentCell))
            {
                Debug.Log("금수(쌍삼/사사) 위치입니다!");
                return false; 
            }
        }

        if (!_boardData.TryPlace(col, row, PlayerToCell(_currentPlayer))) return false;

        boardRenderer.PlaceStoneAt(row, col, _currentPlayer);
        _audioManager.PlayStonePlaceSfx();
        
        // 승패 확인
        if (OmokRule.CheckWin(_boardData, col, row, PlayerToCell(_currentPlayer)))
        {
            Debug.Log($"플레이어 {_currentPlayer} 승리!");
            _audioManager.PlayWinSfx();
            OpenGameOverPanel();
            return true;
        }

        // 턴 교체
        _currentPlayer = (_currentPlayer == 1) ? 2 : 1;
        GameManager.Instance.SetGameTurn(
            _currentPlayer == 1 ? PlayerType.Player1 : PlayerType.Player2
        );
        return true;
    }
    
    private void OnDestroy()
    {
        boardRenderer.OnCellClicked -= OnCellClicked;
    }
    
    public void OnClickSurrenderButton()
    {
        GameManager.Instance.OpenConfirmPanel(surrenderText, "네", "아니요",() =>
        {
            GameManager.Instance.ChangeToMainScene();
        });
    }

    public void OpenGameOverPanel()
    {
        GameManager.Instance.OpenConfirmPanel(victoryText, "다시하기", "나가기", () =>
        {
            ResetGame();
        },
        () =>
        {
            GameManager.Instance.ChangeToMainScene();
        });
    }

    private void ResetGame()
    {
        boardRenderer.ClearBoard();
        _boardData.Clear();
        _currentPlayer = 1;
        GameManager.Instance.SetGameTurn(PlayerType.Player1);
    }
    
    public void SetPlayerTurnPanel(PlayerType playerTurnType)
    {
        switch (playerTurnType)
        {
            case PlayerType.None:
                playerATurnImage.color = Color.white;
                playerBTurnImage.color = Color.white;
                break;
            case PlayerType.Player1:
                playerATurnImage.color = Color.lightGreen;
                playerBTurnImage.color = Color.white;
                playerATurnRect.DOLocalMove(new Vector3(0, 25f, 0), 1).SetEase(Ease.OutBack);
                playerATurnRect.DOScale(1.05f, 1).SetEase(Ease.OutBack);
                playerBTurnRect.DOLocalMove(Vector3.zero, 1).SetEase(Ease.OutBack);
                playerBTurnRect.DOScale(1f, 1).SetEase(Ease.OutBack);
                break;
            case PlayerType.Player2:
                playerATurnImage.color = Color.white;
                playerBTurnImage.color = Color.lightGreen;
                playerBTurnRect.DOLocalMove(new Vector3(0, 25f, 0), 1).SetEase(Ease.OutBack);
                playerBTurnRect.DOScale(1.05f, 1).SetEase(Ease.OutBack);
                playerATurnRect.DOLocalMove(Vector3.zero, 1).SetEase(Ease.OutBack);
                playerATurnRect.DOScale(1f, 1).SetEase(Ease.OutBack);
                break;
        }
    }

    private System.Collections.IEnumerator AIDelayRoutine()
    {
        _aiTurn = true; // AI의 턴 시작
        Debug.Log("AI가 생각 중...");
        yield return new WaitForSeconds(0.5f);

        // AI로부터 최선의 수 계산 (Cell.AI 진영으로 계산)
        Vector2Int aiMove = omokAI.FindBestMove(_boardData, Cell.AI);
        
        // AI의 수를 보드에 배치 (row = y, col = x 매칭 주의)
        PlaceStone(aiMove.y, aiMove.x);
        _aiTurn = false; // AI의 턴 종료
        Debug.Log("AI가 수를 두었습니다");
    }
}
