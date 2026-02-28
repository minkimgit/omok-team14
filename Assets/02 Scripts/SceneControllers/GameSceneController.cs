using DG.Tweening;
using NUnit.Framework.Constraints;
using TMPro;
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
    private GameType _currentGameType;
    private int _currentPlayer;  // 현재 턴인 플레이어 (1 or 2)
    private int _startingPlayer; // 이번 게임에서 흑돌(선공)을 맡은 플레이어 (1 or 2)
    private float setPlayerATimeRemaining;
    private float setPlayerBTimeRemaining;
    private bool _aiTurn = false; // AI의 턴 여부
    
    public float GetPlayerATime() => playerATimeRemaining;
    public float GetPlayerBTime() => playerBTimeRemaining;
    
    // 멀티 플레이 시에만 이용
    [Header("Player Name UI")]
    [SerializeField] private TextMeshProUGUI player1NameText;
    [SerializeField] private TextMeshProUGUI player2NameText;

    [Header("Player 1 Stone UI (Top)")]
    [SerializeField] private GameObject p1BlackStoneObj;
    [SerializeField] private GameObject p1WhiteStoneObj;

    [Header("Player 2 Stone UI (Bottom)")]
    [SerializeField] private GameObject p2BlackStoneObj;
    [SerializeField] private GameObject p2WhiteStoneObj;

    private void Start()
    {
         setPlayerATimeRemaining = playerATimeRemaining;
         setPlayerBTimeRemaining = playerBTimeRemaining;
        _audioManager = FindObjectOfType<AudioManager>();
        _boardData = new BoardData(BOARD_SIZE);
        _currentGameType = GameManager.Instance.CurrentGameType;
        
        boardRenderer.OnCellClicked += OnCellClicked;
        
        // 멀티 플레이 시에는 선공 결정을 서버에 맡김
        if (_currentGameType != GameType.MultiPlay)
        {
            SetFirstPlayer();
        }
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

        // 싱글 플레이에서 AI가 선공이면 첫 수를 자동으로 실행
        if (_currentGameType == GameType.SinglePlay && _currentPlayer == 2)
            StartCoroutine(AIDelayRoutine());
            
    }

    // 흑/백 돌 타입 반환: 선공 플레이어 → 흑(1), 후공 플레이어 → 백(2)
    private int GetStoneType(int player) => player == _startingPlayer ? 1 : 2;

    // int 플레이어 번호를 Cell 열거형으로 변환
    private Cell PlayerToCell(int player) => player == _startingPlayer ? Cell.Human : Cell.AI;
    
    //**돌 배치**
    private void OnCellClicked(int row, int col)
    {
        if (_aiTurn) return; // AI의 턴인 경우 클릭 무시
        
        // 멀티플레이 모드일 경우
        if (_currentGameType == GameType.MultiPlay)
        {
            // MultiplayGameController에게 "클릭 이벤트"가 발생했음을 알림
            // 여기서 직접 PlaceStone을 호출하지 않고, 내 턴인지 확인 후 서버로 보냄
            MultiplayGameController.Instance.HandleBoardClick(row, col);
            return;
        }
        
        if (!PlaceStone(row, col)) return;  // 유효하지 않은 위치에 돌을 놓으려는 경우 무시

        // 싱글 플레이고 현재 턴이 AI라면 AI 실행
        if (_currentGameType == GameType.SinglePlay && _currentPlayer == 2)
        {
            // 플레이어가 바로 클릭하지 못하게 하거나 AI의 생각을 표현하기 위해 코루틴 사용
            StartCoroutine(AIDelayRoutine());
        }
    }

    public bool PlaceStone(int row, int col)
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

        boardRenderer.PlaceStoneAt(row, col, GetStoneType(_currentPlayer));
        _audioManager.PlayStonePlaceSfx();
        
        // 승패 확인
        if (OmokRule.CheckWin(_boardData, col, row, PlayerToCell(_currentPlayer)))
        {
            Debug.Log($"플레이어 {_currentPlayer} 승리!");
            
            OpenGameOverPanel();
            return true;
        }
        
        // 멀티플레이가 아닐 때만 로컬에서 턴을 바꿈
        if (GameManager.Instance.CurrentGameType != GameType.MultiPlay)
        {
            ChangeTurn();
        }
        
        return true;
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

    private System.Collections.IEnumerator AIDelayRoutine()
    {
        _aiTurn = true;
        boardRenderer.SetHoverEnabled(false); // AI 턴 중 호버 숨김
        Debug.Log("AI가 생각 중...");
        yield return new WaitForSeconds(0.5f);

        // FindBestMove를 백그라운드 스레드에서 실행해 메인 스레드(타이머 등)가 멈추지 않도록 함
        Vector2Int aiMove = Vector2Int.zero;
        bool isDone = false;
        System.Threading.Tasks.Task.Run(() =>
        {
            aiMove = omokAI.FindBestMove(_boardData, Cell.AI);
            isDone = true;
        });

        // AI 계산이 끝날 때까지 프레임마다 양보 (타이머는 계속 동작)
        yield return new WaitUntil(() => isDone);

        // AI의 수를 보드에 배치 (row = y, col = x 매칭 주의)
        PlaceStone(aiMove.y, aiMove.x);
        _aiTurn = false;
        boardRenderer.SetHoverEnabled(true); // AI 턴 종료 후 호버 복원
        Debug.Log("AI가 수를 두었습니다");
    }
    
    // 멀티플레이 시 초기화 로직
    public void InitializeMultiplay(int startingPlayer, int myPlayerNum, bool isMyTurn)
    {
        _startingPlayer = startingPlayer; // 1이면 Player1이 흑돌, 2이면 Player2가 흑돌
        _currentPlayer = _startingPlayer; // 게임 시작은 무조건 흑돌부터
        _timerIsRunning = true;

        // 1. "나"와 "상대" 텍스트 설정
        // 내 번호(myPlayerNum)가 1이면 위쪽이 "나", 아니면 아래쪽이 "나"
        if (myPlayerNum == 1) {
            player1NameText.text = "나";
            player2NameText.text = "상대";
        } else {
            player1NameText.text = "상대";
            player2NameText.text = "나";
        }

        // 2. 흑/백 돌 이미지 강제 설정
        // startingPlayer(흑돌 번호)를 기준으로 양쪽 클라이언트 UI를 통일시킵니다.
        if (_startingPlayer == 1) 
        {
            // Player 1 패널: 흑돌 활성, 백돌 비활성
            p1BlackStoneObj.SetActive(true);
            p1WhiteStoneObj.SetActive(false);
            // Player 2 패널: 백돌 활성, 흑돌 비활성
            p2BlackStoneObj.SetActive(false);
            p2WhiteStoneObj.SetActive(true);
        } 
        else 
        {
            // Player 1 패널: 백돌 활성, 흑돌 비활성
            p1BlackStoneObj.SetActive(false);
            p1WhiteStoneObj.SetActive(true);
            // Player 2 패널: 흑돌 활성, 백돌 비활성
            p2BlackStoneObj.SetActive(true);
            p2WhiteStoneObj.SetActive(false);
        }

        UpdateTurnUI();
    }

    // 3. 턴이 바뀔 때 UI를 갱신하는 공통 로직
    public void UpdateTurnUI()
    {
        // GameManager에도 현재 턴을 알려줘야 타이머 로직이 정상 작동함
        GameManager.Instance.SetGameTurn(
            _currentPlayer == 1 ? PlayerType.Player1 : PlayerType.Player2
        );

        // 실제 UI 강조 효과 실행 (DOTween 애니메이션)
        SetPlayerTurnPanel(_currentPlayer == 1 ? PlayerType.Player1 : PlayerType.Player2);
    }
    
    // 멀티플레이용 턴 및 타이머 동기화 함수
    public void SyncMultiplayState(int currentTurnPlayer, float p1Time, float p2Time)
    {
        // 1. 현재 턴 정보 업데이트
        _currentPlayer = currentTurnPlayer;
    
        // 2. 타이머 동기화
        playerATimeRemaining = p1Time;
        playerBTimeRemaining = p2Time;

        // 3. [핵심] 강조 UI 갱신 (초록색 변경 및 크기 조절)
        UpdateTurnUI();
    }
}
