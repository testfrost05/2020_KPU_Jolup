using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class HealthManager : MonoBehaviour
{
    public float hitPoint = 100f;

    private Animator target;

    private void Start()
    {

        target = GetComponent<Animator>();

       
    }





    public void ApplyDamage(float damage)
    {
        hitPoint -= damage;
        if (hitPoint <= 0)
        {
            GameControl.instance.SlayTarget(); // 표적증가 추가 코드       
            hit();
            //Destroy(gameObject);
        }
    }

    public void hit()
    {

        target.SetBool("hit", true);
    
    }

   
}
