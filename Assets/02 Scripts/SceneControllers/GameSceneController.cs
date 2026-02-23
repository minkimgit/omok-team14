using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.UI;

public class GameSceneController : MonoBehaviour
{
    [SerializeField] private Image playerATurnImage;
    [SerializeField] private Image playerBTurnImage;
    private string surrenderText = "게임을 기권하시겠습니까? 기권시 패배로 처리됩니다.";

    public void OnClickSurrenderButton()
    {
        GameManager.Instance.OpenConfirmPanel(surrenderText, () =>
        {
            GameManager.Instance.ChangeToMainScene();
        });
    }
    public void SetPlayerTurnPanel(Constants.PlayerType playerTurnType)
    {
        switch (playerTurnType)
        {
            case Constants.PlayerType.None:
                playerATurnImage.color = Color.white;
                playerBTurnImage.color = Color.white;
                break;
            case Constants.PlayerType.Player1:
                playerATurnImage.color = Color.lightGreen;
                playerBTurnImage.color = Color.white;
                break;
            case Constants.PlayerType.Player2:
                playerATurnImage.color = Color.white;
                playerBTurnImage.color = Color.lightGreen;
                break;
        }
    }
}
