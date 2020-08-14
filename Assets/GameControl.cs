using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
public class GameControl : MonoBehaviour
{
    public static GameControl instance;

    public GameObject TargetContainer, hudContainer, gameOverPanel;
    public Text TargetCounter, TimeCounter, countdownText;
    public bool gamePlaying { get; private set; }
    public int countdownTime;

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
        TargetCounter.text = "Target:0/"+numTotalTargets;
        TimeCounter.text = "Time:00:00:00";
        gamePlaying = false;

        StartCoroutine(CountdownToStart());

    }
    public void SlayTarget()
    {
        numSlayedTargets++;

        string targetCounterStr = "Target:" + numSlayedTargets + "/" + numTotalTargets;
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

            string timePlayingStr = "Time:" + timePlaying.ToString("mm':'ss':'ff");
            TimeCounter.text = timePlayingStr;

        }

        bool pause = Input.GetKeyDown(KeyCode.Escape);
        bool retry = Input.GetKeyDown(KeyCode.R);

        if (pause)
        {
            SceneManager.LoadScene(0);

        }
        if (retry)
        {
            SceneManager.LoadScene(1);
        
        }

       


    }




    private void EndGame()
    {

        gamePlaying = false;
        gameOverPanel.SetActive(true);
        hudContainer.SetActive(false);
        string timePlayingStr = "Time:" + timePlaying.ToString("mm':'ss':'ff");
        gameOverPanel.transform.Find("Time").GetComponent<Text>().text = timePlayingStr;
       
    }

    IEnumerator CountdownToStart()
    {
        while (countdownTime > 0)
        {
            countdownText.text = countdownTime.ToString();
            yield return new WaitForSeconds(1f);
            countdownTime--;
        }

        BeginGame();
        countdownText.text = "GO!";

        yield return new WaitForSeconds(1f);

        countdownText.gameObject.SetActive(false);
    
    }

  


}
