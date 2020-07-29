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
        
        SpawnPlayer();
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
    



   
}
