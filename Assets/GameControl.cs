using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;
using UnityEngine.UI;

public class GameControl : MonoBehaviour
{
    public static GameControl instance;

    public GameObject TargetContainer, hudContainer;
    public Text TargetCounter, TimeCounter;

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
        TargetCounter.text = "표적 : 0 / " + numTotalTargets;

    }
    public void SlayTarget()
    {
        numSlayedTargets++;

        string targetCounterStr = "표적 : " + numSlayedTargets + " / " + numTotalTargets;
        TargetCounter.text = targetCounterStr;
    
    }

     

}
