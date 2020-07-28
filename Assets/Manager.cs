using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using Photon.Pun;
using UnityEngine.SceneManagement;

public class Manager : MonoBehaviourPunCallbacks
{
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


 







    private void Start()
    {

        if (PhotonNetwork.IsMasterClient)
        {
            VrSpawn();
        }
        else
        {

            SpawnPlayer();
        }
    }

    
    public void SpawnPlayer()
    {
        var localPlayerIndex = PhotonNetwork.LocalPlayer.ActorNumber - 1;
        var spawnPosition = spawnPositions[localPlayerIndex % spawnPositions.Length];

        PhotonNetwork.Instantiate(playerPrefab1.name, spawnPosition.position, spawnPosition.rotation);
    }
    
    public void VrSpawn()
    {
        var localPlayerIndex = PhotonNetwork.LocalPlayer.ActorNumber - 1;
        var spawnPosition = spawnPositions[localPlayerIndex % spawnPositions.Length];
        PhotonNetwork.Instantiate(Vrplayer.name, spawnPosition.position, spawnPosition.rotation);


    }


}
