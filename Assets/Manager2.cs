using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Photon.Pun;

public class Manager2 : MonoBehaviourPunCallbacks
{
    public static Manager Instance
    {
        get
        {
            if (Instance == null) instance = FindObjectOfType<Manager2>();


            return Instance;
        }

    }
    private static Manager2 instance;

    public Transform[] spawnPositions;
    public GameObject playerPrefab1;
    public GameObject Vrplayer;









    private void Start()
    {
        VrSpawn();




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
