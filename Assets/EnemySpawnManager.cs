using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using Photon.Pun;
using UnityEngine.SceneManagement;

public class EnemySpawnManager : MonoBehaviourPunCallbacks
{
    public static EnemySpawnManager Instance
    {
        get
        {
            if (Instance == null) instance = FindObjectOfType<EnemySpawnManager>();


            return Instance;
        }

    }
    private static EnemySpawnManager instance;

    public Transform[] spawnPositions;
    public GameObject playerPrefab1;









    private void Start()
    {
        SpawnPlayer();

    }

    public void SpawnPlayer()
    {
        var localPlayerIndex = PhotonNetwork.LocalPlayer.ActorNumber - 1;
        var spawnPosition = spawnPositions[localPlayerIndex % spawnPositions.Length];

        PhotonNetwork.Instantiate(playerPrefab1.name, spawnPosition.position, spawnPosition.rotation);





    }
    public override void OnLeftRoom()
    {
        SceneManager.LoadScene("MainMenu");
    }

    // Update is called once per frame
    void Update()
    {

    }
}
