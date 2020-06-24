using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class PlayerNameText : MonoBehaviour
{
    private Text nameText;
    void Start()
    {
        nameText = GetComponent<Text>();

        if (AuthManager.User != null)
        {
            nameText.text = $"Welcome! {AuthManager.User.Email}";
        }
        else
        {
            nameText.text = "ERROR : AuthManager.User == null";
        }
        
    }

    
    
}
