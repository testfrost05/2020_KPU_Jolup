using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class GameQuitScript : MonoBehaviour
{
    public void ExitGame()
    {
        Application.Quit();
        Debug.Log("Button Click");
    }
}
