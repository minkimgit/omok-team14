using DG.Tweening;
using NUnit.Framework.Constraints;
using TMPro;
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
    
    [SerializeField] private Image playerAWhiteStoneImage;
    [SerializeField] private Image playerBBlackStoneImage;
    
    [SerializeField] private TextMeshProUGUI playerATimerText;
    [SerializeField] private TextMeshProUGUI playerBTimerText;
    
    [SerializeField] private BoardRenderer boardRenderer;
    
    [SerializeField] private float playerATimeRemaining = 300f;
    [SerializeField] private float playerBTimeRemaining = 300f;
    private bool _timerIsRunning = false;
    
    private string _surrenderText = "게임을 기권하시겠습니까? 기권시 패배로 처리됩니다.";
    private string _victoryText = "승리하였습니다.";
    private string _defeatText = "패배하였습니다.";
    
    private BoardData _boardData;
    private AudioManager _audioManager;
    private int _currentPlayer;  // 현재 턴인 플레이어 (1 or 2)
    private int _startingPlayer; // 이번 게임에서 흑돌(선공)을 맡은 플레이어 (1 or 2)
    private float setPlayerATimeRemaining;
    private float setPlayerBTimeRemaining;

    private void Start()
    {
         setPlayerATimeRemaining = playerATimeRemaining;
         setPlayerBTimeRemaining = playerBTimeRemaining;
        _audioManager = FindObjectOfType<AudioManager>();
        _boardData = new BoardData(BOARD_SIZE);
        boardRenderer.OnCellClicked += OnCellClicked;
        SetFirstPlayer();
    }

    private void LateUpdate()
    {
        TimerCountdown(_currentPlayer);
        DisplayTime();
    }

    // 매 게임 시작 시 선공 플레이어를 무작위로 결정
    private void SetFirstPlayer()
    {
        _startingPlayer = Random.Range(1, 3);
        _currentPlayer  = _startingPlayer;
        GameManager.Instance.SetGameTurn(
            _currentPlayer == 1 ? PlayerType.Player1 : PlayerType.Player2
        );

        // 타이머 세팅
        _timerIsRunning = true;
        playerATimeRemaining = setPlayerATimeRemaining;
        playerBTimeRemaining = setPlayerBTimeRemaining;
        
        // 턴 HUD 돌 색 설정
        if (_currentPlayer == 1)
        {
            playerAWhiteStoneImage.gameObject.SetActive(false);
            playerBBlackStoneImage.gameObject.SetActive(false);
        }
        else
        {
            playerAWhiteStoneImage.gameObject.SetActive(true);
            playerBBlackStoneImage.gameObject.SetActive(true);
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
            
            OpenGameOverPanel();
            return;
        }
        
        ChangeTurn();
    }
    
    // 턴 교체
    private void ChangeTurn()
    {
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
        GameManager.Instance.OpenConfirmPanel(_surrenderText, "네", "아니요",() =>
        {
            GameManager.Instance.ChangeToMainScene();
        });
    }

    // 게임오버 팝업
    public void OpenGameOverPanel()
    {
        _timerIsRunning = false;
        _audioManager.PlayWinSfx();
        GameManager.Instance.OpenConfirmPanel(($"플레이어 {_currentPlayer} 승리!"), "다시하기", "나가기", () =>
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

    private void TimerCountdown(int currentPlayer)
    {
        if (currentPlayer == 1)
        {
            if (playerATimeRemaining > 0 && _timerIsRunning)
            {
                playerATimeRemaining -= Time.deltaTime;
            }
            else if (playerATimeRemaining <= 0)
            {
                ChangeTurn();
                OpenGameOverPanel();
            }
        }
        else
        {
            if (playerBTimeRemaining > 0 && _timerIsRunning)
            {
                playerBTimeRemaining -= Time.deltaTime;
            }
            else if (playerBTimeRemaining <= 0)
            {
                ChangeTurn();
                OpenGameOverPanel();
            }
        }
    }

    void DisplayTime()
    {
        playerATimerText.text = FormatTime(playerATimeRemaining);
        playerBTimerText.text = FormatTime(playerBTimeRemaining);
    }

    string FormatTime(float timeRemaining)
    {
        timeRemaining = Mathf.Max(0f, timeRemaining);
        int minutes = Mathf.FloorToInt(timeRemaining / 60);
        int seconds = Mathf.FloorToInt(timeRemaining % 60);
        return $"{minutes}:{seconds:00}";
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
