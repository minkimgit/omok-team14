using DG.Tweening;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.UI;
using static Constants;

public class GameSceneController : MonoBehaviour
{
    [SerializeField] private Image playerATurnImage;
    [SerializeField] private Image playerBTurnImage;
    
    [SerializeField] private RectTransform playerATurnRect;
    [SerializeField] private RectTransform playerBTurnRect;
    
    [SerializeField] private Image PlayerAWhiteStoneImage;
    [SerializeField] private Image PlayerBBlackStoneImage;
    
    [SerializeField] private BoardRenderer boardRenderer;
    
    private string surrenderText = "게임을 기권하시겠습니까? 기권시 패배로 처리됩니다.";
    private string victoryText = "승리하였습니다.";
    private string defeatText = "패배하였습니다.";
    

    private AudioManager _audioManager;
    private BoardData _boardData;
    private int _currentPlayer;  // 현재 턴인 플레이어 (1 or 2)
    private int _startingPlayer; // 이번 게임에서 흑돌(선공)을 맡은 플레이어 (1 or 2)

    private void Start()
    {
        _audioManager = FindObjectOfType<AudioManager>();
        _boardData = new BoardData(BOARD_SIZE);
        boardRenderer.OnCellClicked += OnCellClicked;
        SetFirstPlayer();
    }

    // 매 게임 시작 시 선공 플레이어를 무작위로 결정
    private void SetFirstPlayer()
    {
        _startingPlayer = Random.Range(1, 3);
        _currentPlayer  = _startingPlayer;
        GameManager.Instance.SetGameTurn(
            _currentPlayer == 1 ? PlayerType.Player1 : PlayerType.Player2
        );
        if (_currentPlayer == 1)
        {
            PlayerAWhiteStoneImage.gameObject.SetActive(false);
            PlayerBBlackStoneImage.gameObject.SetActive(false);
        }
        else
        {
            PlayerAWhiteStoneImage.gameObject.SetActive(true);
            PlayerBBlackStoneImage.gameObject.SetActive(true);
        }
            
    }

    // 흑/백 돌 타입 반환: 선공 플레이어 → 흑(1), 후공 플레이어 → 백(2)
    private int GetStoneType(int player) => player == _startingPlayer ? 1 : 2;

    // int 플레이어 번호를 Cell 열거형으로 변환
    private Cell PlayerToCell(int player) => player == _startingPlayer ? Cell.Human : Cell.AI;
    
    //**돌 배치**
    private void OnCellClicked(int row, int col)
    {
        if (!_boardData.TryPlace(col, row, PlayerToCell(_currentPlayer))) return;

        boardRenderer.PlaceStoneAt(row, col, GetStoneType(_currentPlayer));
        _audioManager.PlayStonePlaceSfx();
        
        //승패 확인
        if (OmokRule.CheckWin(_boardData, col, row, PlayerToCell(_currentPlayer)))
        {
            Debug.Log($"플레이어 {_currentPlayer} 승리!");
            _audioManager.PlayWinSfx();
            OpenGameOverPanel();
            return;
        }

        // 턴 교체
        _currentPlayer = (_currentPlayer == 1) ? 2 : 1;
        GameManager.Instance.SetGameTurn(
            _currentPlayer == 1 ? PlayerType.Player1 : PlayerType.Player2
        );
    }
    
    private void OnDestroy()
    {
        boardRenderer.OnCellClicked -= OnCellClicked;
    }
    
    // 기권 팝업
    public void OnClickSurrenderButton()
    {
        GameManager.Instance.OpenConfirmPanel(surrenderText, "네", "아니요",() =>
        {
            GameManager.Instance.ChangeToMainScene();
        });
    }

    // 게임오버 팝업
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

    //게임 리셋
    private void ResetGame()
    {
        boardRenderer.ClearBoard();
        _boardData.Clear();
        SetFirstPlayer();
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
}
