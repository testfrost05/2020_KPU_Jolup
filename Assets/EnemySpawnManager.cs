using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using Photon.Pun;
using UnityEngine.SceneManagement;

public class EnemySpawnManager : MonoBehaviourPunCallbacks
{

    public Transform[] spawnPositions;
    public GameObject playerPrefab1;




    private void Start()
    {
        SpawnPlayer();

    }

    public void SpawnPlayer()
    {
        Transform spawnPosition = spawnPositions[Random.Range(0,spawnPositions.Length)];

        PhotonNetwork.Instantiate(playerPrefab1.name, spawnPosition.position, spawnPosition.rotation);





    }
    
    

}
