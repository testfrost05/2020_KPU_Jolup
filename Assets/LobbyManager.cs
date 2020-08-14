using UnityEngine;
using UnityEngine.SceneManagement;

public class LobbyManager : MonoBehaviour
{

    public void Update()
    {
        bool Start = Input.GetKeyDown(KeyCode.E);

        if (Start)
        {

            SceneManager.LoadScene("New IngameScene");
        }
    }

  
    
}

