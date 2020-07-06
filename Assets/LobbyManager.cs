using Photon.Pun;
using Photon.Realtime;
using UnityEngine;
using UnityEngine.UI;

public class LobbyManager : MonoBehaviourPunCallbacks
{
    private string gameVersion = "1";

    public Text connectionInfoText;
    public Button JoinButton;


    // Start is called before the first frame update
    private void Start()
    {
        PhotonNetwork.GameVersion = gameVersion;
        PhotonNetwork.ConnectUsingSettings();

        JoinButton.interactable = false;
        connectionInfoText.text = "마스터 서버에 연결중...";
        
    }

    public override void OnConnectedToMaster()
    {
        JoinButton.interactable = true;
        connectionInfoText.text = "온라인 : 마스터서버에 연결되었습니다.";
    }

    public override void OnDisconnected(DisconnectCause cause)
    {
        JoinButton.interactable = false;
        connectionInfoText.text = $"오프라인 : 연결 끊김 {cause.ToString()} - 재접속 시도...";

        PhotonNetwork.ConnectUsingSettings();
    }

    public void Connect()
    {
        JoinButton.interactable = false;

        if (PhotonNetwork.IsConnected)
        {
            connectionInfoText.text = "랜덤 방에 접속중입니다...";
            PhotonNetwork.JoinRandomRoom();
        }
        else
        {
            connectionInfoText.text = "오프라인 : 연결 끊김 - 재접속 시도...";
            PhotonNetwork.ConnectUsingSettings();
        }
    }

    public override void OnJoinRandomFailed(short returnCode, string message)
    {
        connectionInfoText.text = "방이 없습니다. 새로운 방을 생성합니다.";
        PhotonNetwork.CreateRoom(roomName: null, new RoomOptions { MaxPlayers = 2 });
    }

    public override void OnJoinedRoom()
    {
        connectionInfoText.text = "방에 접속중입니다.";
        PhotonNetwork.LoadLevel("Ingametestscene");
    }
}


