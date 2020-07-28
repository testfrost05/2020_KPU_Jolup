using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using Photon.Pun;
using Photon.Realtime;
using UnityEngine.SceneManagement;
using ExitGames.Client.Photon;


public enum GameState
{
    Waiting = 0,
    Starting = 1,
    Playing = 2,
    Ending = 3
}



public class Manager : MonoBehaviourPunCallbacks
{
    /*
    public static Manager Instance
    {
        get
        {
            if (Instance == null) instance = FindObjectOfType<Manager>();


            return Instance;
        }

    }
    private static Manager instance;

    public Transform[] spawnPositions;
    public GameObject playerPrefab1;
    public GameObject Vrplayer;
    */

    public int mainmenu = 0;

    public GameObject mapcam;

    public string player_prefab_string;

    public GameObject player_prefab;
    public Transform[] spawn_points;

    private Transform ui_endgame;


    private GameState state = GameState.Waiting;





    private void Start()
    {
        ValidateConnection();
        InitializeUI();

        SpawnPlayer();
    }

    private void Update()
    {

    }



    public override void OnLeftRoom()
    {
        base.OnLeftRoom();
        SceneManager.LoadScene(mainmenu);
    }


    public void SpawnPlayer()
    {
        var localPlayerIndex = PhotonNetwork.LocalPlayer.ActorNumber - 1;
        var spawnPosition = spawn_points[localPlayerIndex % spawn_points.Length];

        PhotonNetwork.Instantiate(player_prefab.name, spawnPosition.position, spawnPosition.rotation);
    }

    /*
    public void VrSpawn()
    {
        var localPlayerIndex = PhotonNetwork.LocalPlayer.ActorNumber - 1;
        var spawnPosition = spawnPositions[localPlayerIndex % spawnPositions.Length];
        PhotonNetwork.Instantiate(Vrplayer.name, spawnPosition.position, spawnPosition.rotation);


    }
    */
    private void InitializeUI()
    {
        ui_endgame = GameObject.Find("Canvas").transform.Find("End Game").transform;
   
    }

    private void ValidateConnection()
    {
        if (PhotonNetwork.IsConnected) return;
        SceneManager.LoadScene(mainmenu); 
    }

    private void EndGame()
    {
        state = GameState.Ending;

        //방 파괴
        if (PhotonNetwork.IsMasterClient)
        {
            PhotonNetwork.DestroyAll();
            PhotonNetwork.CurrentRoom.IsVisible = false;
            PhotonNetwork.CurrentRoom.IsOpen = false;
        
        }

        //게임오버 씬 보여주기
        ui_endgame.gameObject.SetActive(true);

        //6초후 메인메뉴
        StartCoroutine(End(6f));

    }

    private IEnumerator End(float p_wait)
    {
        yield return new WaitForSeconds(p_wait);

        //연결해제
        PhotonNetwork.AutomaticallySyncScene = false;
        PhotonNetwork.LeaveRoom();
        
    }

    private void StateCheck()
    {
        if (state == GameState.Ending)
        {
            EndGame();
       
        }
    
    
    }

   
}
