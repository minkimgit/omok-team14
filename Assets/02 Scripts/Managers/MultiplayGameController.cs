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

        // 연속 클릭 방지: 서버 에코가 오기 전에 즉시 차단
        IsMyTurn = false;

        // 내 돌을 즉시 로컬에 그림 (네트워크 왕복을 기다리지 않음)
        bool placed = gameSceneController.PlaceStone(row, col);
        if (!placed)
        {
            // 금수/이미 놓인 자리 등 유효하지 않은 위치 → 턴을 되돌림
            IsMyTurn = true;
            return;
        }

        // 서버에 알림 (상대방 화면에 돌을 그리기 위해)
        NetworkManager.Instance.EmitPlaceStone(row, col);
    }

    // 서버에서 stonePlaced 이벤트가 오면 호출됨 (내 돌 에코 + 상대 돌 모두 처리)
    public void OnOpponentPlacedStone(int row, int col, int playerNum)
    {
        if (playerNum != MyPlayerNumber)
        {
            // 상대방의 돌: 서버가 알려준 playerNum을 직접 전달해 올바른 색상으로 그림
            gameSceneController.PlaceStone(row, col, playerNum);
        }
        // 내 돌 에코(playerNum == MyPlayerNumber)는 HandleBoardClick에서 이미 그렸으므로 생략

        // 승리가 발생했으면 WinRoutine이 이미 시작됐으므로 턴 업데이트 생략
        if (!gameSceneController.IsGameRunning) return;

        // 방금 둔 사람의 다음 플레이어로 턴 계산
        int nextPlayer = (playerNum == 1) ? 2 : 1;

        // 내 턴 여부 업데이트
        IsMyTurn = (nextPlayer == MyPlayerNumber);

        // [중요] GameSceneController의 내부 턴 상태와 UI를 강제 동기화
        gameSceneController.SyncMultiplayState(
            nextPlayer,
            gameSceneController.GetPlayerATime(),
            gameSceneController.GetPlayerBTime()
        );
    }

    // 싱글톤 추상 함수 구현
    protected override void OnSceneLoad(UnityEngine.SceneManagement.Scene scene, UnityEngine.SceneManagement.LoadSceneMode mode) { }
}