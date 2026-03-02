using UnityEngine;

public class MultiplayGameController : Singleton<MultiplayGameController>
{
    [SerializeField] private GameSceneController gameSceneController;
    
    public bool IsMyTurn { get; private set; }
    public int MyPlayerNumber { get; private set; } // 1: 흑, 2: 백

    // 게임 시작 시 서버 정보를 받아 설정하는 함수
    public void SetGameStart(int myNum, bool startsFirst, int startingPlayer)
    {
        MyPlayerNumber = myNum;
        IsMyTurn = startsFirst;

        // 만약 인스펙터 연결이 빠졌다면, 현재 씬에서 직접 찾음
        if (gameSceneController == null)
        {
            gameSceneController = FindObjectOfType<GameSceneController>();
        }

        if (gameSceneController != null)
        {
            gameSceneController.InitializeMultiplay(startingPlayer, MyPlayerNumber, IsMyTurn);
        }
        else
        {
            Debug.LogError("[Multiplay] GameSceneController를 씬에서 찾을 수 없습니다!");
        }
    }

    // [내가 클릭했을 때]
    public void HandleBoardClick(int row, int col)
    {
        if (!IsMyTurn) return;  // 내 턴이 아닌 경우 착수 금지

        // 연속 클릭 방지 + 호버 즉시 숨김
        IsMyTurn = false;
        gameSceneController.SetBoardHoverEnabled(false);

        // 내 돌을 즉시 로컬에 그림 (네트워크 왕복을 기다리지 않음)
        bool placed = gameSceneController.PlaceStone(row, col);
        if (!placed)
        {
            // 금수/이미 놓인 자리 등 유효하지 않은 위치 → 턴 복원
            IsMyTurn = true;
            gameSceneController.SetBoardHoverEnabled(true);
            return;
        }

        // 착수 직후: 에코를 기다리지 않고 즉시 턴 패널과 타이머를 전환
        // (서버 에코가 도착해도 같은 값을 다시 설정하므로 안전함)
        if (gameSceneController.IsGameRunning)
        {
            int nextPlayer = MyPlayerNumber == 1 ? 2 : 1;
            // 타이머 스냅샷을 SyncMultiplayState에 전달 (타이머 값은 그대로, _currentPlayer와 UI만 전환)
            gameSceneController.SyncMultiplayState(nextPlayer,
                gameSceneController.GetPlayerATime(),
                gameSceneController.GetPlayerBTime());
        }

        // 서버에 착수 정보 + 타이머 스냅샷 전송 (상대방 화면 동기화용)
        NetworkManager.Instance.EmitPlaceStone(row, col,
            gameSceneController.GetPlayerATime(),
            gameSceneController.GetPlayerBTime());
    }

    // 서버에서 stonePlaced 이벤트가 오면 호출됨 (내 돌 에코 + 상대 돌 모두 처리)
    // playerATime / playerBTime: 착수 순간의 타이머 스냅샷 (타이머 동기화용)
    public void OnOpponentPlacedStone(int row, int col, int playerNum, float playerATime, float playerBTime)
    {
        if (playerNum == MyPlayerNumber)
        {
            // 내 돌 에코: 돌은 HandleBoardClick에서 이미 그렸고 턴도 즉시 전환했으므로 생략
            return;
        }

        // ── 상대방의 돌 ──
        // 서버가 알려준 playerNum을 직접 전달해 올바른 색상으로 그림
        gameSceneController.PlaceStone(row, col, playerNum);

        // 상대방의 착수로 승리가 발생했으면 WinRoutine이 이미 시작됐으므로 턴 업데이트 생략
        if (!gameSceneController.IsGameRunning) return;

        // 이제 내 턴
        IsMyTurn = true;
        gameSceneController.SetBoardHoverEnabled(true);

        // 상대방의 타이머 스냅샷으로 동기화 + 턴 패널 전환
        int nextPlayer = (playerNum == 1) ? 2 : 1;  // nextPlayer == MyPlayerNumber
        gameSceneController.SyncMultiplayState(nextPlayer, playerATime, playerBTime);
    }

    // ── 게임오버 후 리셋/나가기 ──

    // [다시하기] 버튼 — 리셋 투표 전송 후 상대방 응답 대기 패널 표시
    public void RequestReset()
    {
        NetworkManager.Instance.EmitRequestReset();
        // 대기 패널: 녹색/빨간 모두 누르면 대기를 취소하고 게임을 나감
        GameManager.Instance.OpenConfirmPanel(
            "상대방의 응답을 기다리는 중...",
            "대기 취소",             // 녹색 버튼
            "나가기",                // 빨간 버튼
            () => ExitGame(),
            () => ExitGame()
        );
    }

    // [나가기] 버튼 — 서버에 알린 뒤 즉시 메인 씬으로 이동
    // 서버는 상대방에게 forceExit를 전송하므로 상대방도 자동으로 나가게 됨
    public void ExitGame()
    {
        NetworkManager.Instance.EmitExitGame();
        GameManager.Instance.ChangeToMainScene();
    }

    // 서버로부터 resetConfirmed 수신 — 양쪽이 모두 리셋에 동의한 경우
    public void OnResetConfirmed(int newStartingPlayer)
    {
        IsMyTurn = (MyPlayerNumber == newStartingPlayer);
        GameManager.Instance.CloseActiveConfirmPanel(() =>
        {
            gameSceneController.ResetMultiplay(newStartingPlayer, IsMyTurn);
        });
    }

    // 서버로부터 forceExit 수신 — 상대방이 나갔거나 연결이 끊긴 경우
    public void OnForceExit()
    {
        // 게임이 아직 진행 중일 때 상대방이 나가면 → 내가 승리 (기권/연결 종료)
        // IsGameRunning(_timerIsRunning)이 false이면 이미 WinRoutine에서 결과가 보고됐으므로 생략
        if (gameSceneController != null && gameSceneController.IsGameRunning)
        {
            NetworkManager.Instance.EmitReportWin();
        }

        GameManager.Instance.CloseActiveConfirmPanel(() =>
        {
            GameManager.Instance.ChangeToMainScene();
        });
    }

    // 싱글톤 추상 함수 구현
    protected override void OnSceneLoad(UnityEngine.SceneManagement.Scene scene, UnityEngine.SceneManagement.LoadSceneMode mode) { }
}