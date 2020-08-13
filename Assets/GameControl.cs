using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;
using UnityEngine.UI;

public class GameControl : MonoBehaviour
{
    public static GameControl instance;

    public GameObject TargetContainer, hudContainer, gameOverPanel;
    public Text TargetCounter, TimeCounter;
    public bool gamePlaying { get; private set; }

    private int numTotalTargets, numSlayedTargets;

    private float startTime, elapesedTime;
    TimeSpan timePlaying;


    private void Awake()
    {
        instance = this;

    }


    private void Start()
    {
        numTotalTargets = TargetContainer.transform.childCount;
        numSlayedTargets = 0;
        TargetCounter.text = "Target : 0 / " + numTotalTargets;
        gamePlaying = false;

        BeginGame();

    }
    public void SlayTarget()
    {
        numSlayedTargets++;

        string targetCounterStr = "Target : " + numSlayedTargets + " / " + numTotalTargets;
        TargetCounter.text = targetCounterStr;

        if (numSlayedTargets >= numTotalTargets)
        {
            EndGame();
        
        }
    
    }

    private void BeginGame()
    {
        gamePlaying = true;
        startTime = Time.time;
    
    }

    private void Update()
    {
        if (gamePlaying)
        {

            elapesedTime = Time.time - startTime;
            timePlaying = TimeSpan.FromSeconds(elapesedTime);

            string timePlayingStr = "Time : " + timePlaying.ToString("mm' : 'ss' : 'ff");
            TimeCounter.text = timePlayingStr;
       
        }

    }




    private void EndGame()
    {

        gamePlaying = false;
        gameOverPanel.SetActive(true);
        hudContainer.SetActive(false);
        string timePlayingStr = "Time : " + timePlaying.ToString("mm' : 'ss' : 'ff");
        gameOverPanel.transform.Find("Time").GetComponent<Text>().text = timePlayingStr;
    }


}
