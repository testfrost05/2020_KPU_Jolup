using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using Photon.Pun;
using Photon.Realtime;
using UnityEngine.SceneManagement;
using ExitGames.Client.Photon;




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



    public GameObject player_prefab;
    public GameObject Vrplayer;
    public Transform[] spawn_points;







    private void Start()
    {
      
            SpawnPlayer();
        
    }

   


    public void SpawnPlayer()
    {
      
        var spawnPosition = spawn_points[Random.Range(0, spawn_points.Length)];

        PhotonNetwork.Instantiate(player_prefab.name, spawnPosition.position, spawnPosition.rotation);
    }

    
    public void VrSpawn()
    {

        var spawnPosition = spawn_points[Random.Range(0, spawn_points.Length)];
        PhotonNetwork.Instantiate(Vrplayer.name, spawnPosition.position, spawnPosition.rotation);


    }
    
    



   
}
