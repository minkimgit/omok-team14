using System.Collections;
using System.Collections.Generic;
using System.Threading.Tasks;
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
    // MultiplayGameController가 PlaceStone 후 게임 종료 여부를 확인하는 데 사용
    public bool IsGameRunning => _timerIsRunning;
    // 멀티플레이에서 내 턴 여부에 따라 호버 표시를 켜고 끔
    public void SetBoardHoverEnabled(bool enabled) => boardRenderer?.SetHoverEnabled(enabled);
    
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

    // forPlayerNum: 멀티플레이에서 서버가 알려준 플레이어 번호를 직접 지정.
    //               0이면 기존처럼 _currentPlayer 사용 (싱글/듀얼플레이 호환).
    public bool PlaceStone(int row, int col, int forPlayerNum = 0)
    {
        int effectivePlayer = forPlayerNum > 0 ? forPlayerNum : _currentPlayer;
        Cell currentCell = PlayerToCell(effectivePlayer);

        if (currentCell == Cell.Human) // 흑돌인가
        {
            if (OmokRule.IsForbiddenMove(_boardData, col, row, currentCell))
            {
                Debug.Log("금수(쌍삼/사사) 위치입니다!");
                return false;
            }
        }

        if (!_boardData.TryPlace(col, row, PlayerToCell(effectivePlayer))) return false;

        boardRenderer.PlaceStoneAt(row, col, GetStoneType(effectivePlayer));
        _audioManager.PlayStonePlaceSfx();

        // 승패 확인
        if (OmokRule.CheckWin(_boardData, col, row, PlayerToCell(effectivePlayer)))
        {
            List<Vector2Int> winLine = OmokRule.GetWinningLine(_boardData, col, row, PlayerToCell(effectivePlayer));
            StartCoroutine(WinRoutine(winLine, effectivePlayer));
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
        GameManager.Instance.OpenConfirmPanel(_surrenderText, "네", "아니요", () =>
        {
            if (_currentGameType == GameType.MultiPlay)
                MultiplayGameController.Instance.ExitGame(); // 상대방에게도 forceExit 전송
            else
                GameManager.Instance.ChangeToMainScene();
        });
    }

    // 게임오버 팝업
    public void OpenVictoryPanel()
    {
        _timerIsRunning = false;
        _audioManager.PlayWinSfx();
        if (_currentGameType == GameType.MultiPlay)
        {
            // 녹색: 다시하기 투표 요청 (양쪽이 모두 눌러야 리셋)
            // 빨간: 즉시 나가기 (상대방도 자동으로 메인 화면으로 이동)
            GameManager.Instance.OpenConfirmPanel(_victoryText, "다시하기", "나가기",
                () => MultiplayGameController.Instance.RequestReset(),
                () => MultiplayGameController.Instance.ExitGame());
        }
        else
        {
            GameManager.Instance.OpenConfirmPanel($"플레이어 {_currentPlayer} 승리!", "다시하기", "나가기",
                () => ResetGame(), () => GameManager.Instance.ChangeToMainScene());
        }
    }
    public void OpenDefeatPanel()
    {
        _timerIsRunning = false;
        _audioManager.PlayLoseSfx();
        if (_currentGameType == GameType.MultiPlay)
        {
            // 녹색: 다시하기 투표 요청 (양쪽이 모두 눌러야 리셋)
            // 빨간: 즉시 나가기 (상대방도 자동으로 메인 화면으로 이동)
            GameManager.Instance.OpenConfirmPanel(_defeatText, "다시하기", "나가기",
                () => MultiplayGameController.Instance.RequestReset(),
                () => MultiplayGameController.Instance.ExitGame());
        }
        else
        {
            GameManager.Instance.OpenConfirmPanel("패배하였습니다", "다시하기", "나가기",
                () => ResetGame(), () => GameManager.Instance.ChangeToMainScene());
        }
    }

    //게임 리셋 (싱글/듀얼 플레이 전용)
    private void ResetGame()
    {
        boardRenderer.ClearBoard();
        _boardData.Clear();
        SetFirstPlayer();
    }

    // 멀티플레이 전용 리셋 — 서버가 결정한 새 선공으로 게임 재시작
    public void ResetMultiplay(int newStartingPlayer, bool isMyTurn)
    {
        boardRenderer.ClearBoard();
        _boardData.Clear();
        playerATimeRemaining = setPlayerATimeRemaining;
        playerBTimeRemaining = setPlayerBTimeRemaining;
        InitializeMultiplay(newStartingPlayer, MultiplayGameController.Instance.MyPlayerNumber, isMyTurn);
    }

    private void TimerCountdown(int currentPlayer)
    {
        if (currentPlayer == 1)
        {
            if (playerATimeRemaining > 0 && _timerIsRunning)
            {
                playerATimeRemaining -= Time.deltaTime;
            }
            else if (playerATimeRemaining <= 0 && _timerIsRunning)
            {
                // Player 1의 시간 초과
                ChangeTurn();
                if (_currentGameType == GameType.MultiPlay)
                {
                    // 각 클라이언트가 자신의 번호를 기준으로 승패 결정
                    if (MultiplayGameController.Instance.MyPlayerNumber == 1) OpenDefeatPanel();
                    else OpenVictoryPanel();
                }
                else if (_currentGameType == GameType.DualPlay)
                    OpenVictoryPanel();
                else
                    OpenDefeatPanel(); // SinglePlay: 인간(P1) 시간 초과 → 패배
            }
        }
        else
        {
            if (playerBTimeRemaining > 0 && _timerIsRunning)
            {
                playerBTimeRemaining -= Time.deltaTime;
            }
            else if (playerBTimeRemaining <= 0 && _timerIsRunning)
            {
                // Player 2의 시간 초과
                ChangeTurn();
                if (_currentGameType == GameType.MultiPlay)
                {
                    // 각 클라이언트가 자신의 번호를 기준으로 승패 결정
                    if (MultiplayGameController.Instance.MyPlayerNumber == 2) OpenDefeatPanel();
                    else OpenVictoryPanel();
                }
                else
                    OpenVictoryPanel(); // DualPlay 또는 SinglePlay(AI 시간 초과 → 인간 승리)
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

    // 승리 돌 애니메이션 후 게임오버 패널 표시
    // winnerPlayerNum: PlaceStone에서 전달받은 effectivePlayer — _currentPlayer보다 신뢰도 높음
    private IEnumerator WinRoutine(List<Vector2Int> winLine, int winnerPlayerNum = 0)
    {
        _timerIsRunning = false;
        // PlaceStone이 넘겨준 effectivePlayer를 우선 사용, 없으면 _currentPlayer 폴백
        int winnerPlayerNumber = winnerPlayerNum > 0 ? winnerPlayerNum : _currentPlayer;

        _audioManager.PlayConnectFiveSfx();

        const float punchDelay = 0.08f;  // 돌 사이 딜레이
        const float punchDuration = 0.4f;  // 펀치 애니메이션 지속 시간

        // 승리 돌 애니메이션
        foreach (var pos in winLine)
        {
            GameObject stone = boardRenderer.GetStoneAt(pos.y, pos.x);
            if (stone != null)
            {
                stone.transform.DOComplete();
                stone.transform.DOPunchScale(Vector3.one * 0.4f, punchDuration, 6, 0.5f);
                _audioManager.PlayStonePlaceSfx();
            }

            yield return new WaitForSeconds(punchDelay);
        }

        // 마지막 돌 애니메이션이 끝날 때까지 대기 후 패널 표시
        yield return new WaitForSeconds(punchDuration + 0.2f);

        if (_currentGameType == GameType.MultiPlay)
        {
            // 내 번호와 승자 번호를 비교해 각 클라이언트가 알맞은 패널을 표시
            bool iWon = winnerPlayerNumber == MultiplayGameController.Instance.MyPlayerNumber;
            if (iWon) OpenVictoryPanel();
            else      OpenDefeatPanel();
        }
        else if (_currentGameType == GameType.SinglePlay && winnerPlayerNumber == 2)
            OpenDefeatPanel();
        else
            OpenVictoryPanel();
    }

    private IEnumerator AIDelayRoutine()
    {
        _aiTurn = true;
        boardRenderer.SetHoverEnabled(false); // AI 턴 중 호버 숨김
        Debug.Log("AI가 생각 중...");
        yield return new WaitForSeconds(0.5f);

        // FindBestMove를 백그라운드 스레드에서 실행해 타이머가 멈추지 않도록 함
        // Task<Vector2Int>로 결과를 받아 메모리 가시성(memory visibility) 문제를 방지
        var aiTask = Task.Run(() => omokAI.FindBestMove(_boardData, Cell.AI));

        // AI 계산이 끝날 때까지 프레임마다 양보 (타이머는 계속 동작)
        yield return new WaitUntil(() => aiTask.IsCompleted);

        if (_timerIsRunning)
        {
            // task.Result는 메모리 배리어를 보장하므로 항상 올바른 결과를 읽음
            Vector2Int aiMove = aiTask.Result;
            PlaceStone(aiMove.y, aiMove.x);
            Debug.Log("AI가 수를 두었습니다");
        }

        boardRenderer.SetHoverEnabled(true); // AI 턴 종료 후 호버 복원
        _aiTurn = false;
    }
    
    public void OnClickSettingsButton()
    {
        GameManager.Instance.OpenSettingsPanel();
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

        // 내 턴이 아닐 때는 호버 비활성 (상대방 돌 위에 호버 표시 방지)
        boardRenderer.SetHoverEnabled(isMyTurn);
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
