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
        public GameObject playerPrefab2;








    private void Start()
        {
            SpawnPlayer();

        }

        public void SpawnPlayer()
        {
            var localPlayerIndex = PhotonNetwork.LocalPlayer.ActorNumber - 1;
            var spawnPosition = spawnPositions[localPlayerIndex % spawnPositions.Length];

        PhotonNetwork.Instantiate(playerPrefab1.name, spawnPosition.position, spawnPosition.rotation);
        PhotonNetwork.Instantiate(playerPrefab2.name, spawnPosition.position, spawnPosition.rotation);





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
