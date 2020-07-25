using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;

public class EnemyComtroller : MonoBehaviour
{
    public enum CurrentState { idle, trace, attack, dead};
    public CurrentState curState = CurrentState.idle;

    private Transform _transform;
    private Transform playerTransform;
    private NavMeshAgent nvAgent;
    private Animator _animator;

    public float traceDist = 12.0f;
    public float attackDist = 3.0f;

    private bool isDead = false;
    private int EnemyHP = 100;

    void Start()
    {
        _transform = this.gameObject.GetComponent<Transform>();
        playerTransform = GameObject.FindWithTag("Player").GetComponent<Transform>();
        nvAgent = this.gameObject.GetComponent<NavMeshAgent>();
        _animator = this.gameObject.GetComponent<Animator>();

        StartCoroutine(this.CheckState());
        StartCoroutine(this.CheckStateAction());
    }

    IEnumerator CheckState()
    {
        while(!isDead)
        {
            yield return new WaitForSeconds(3.0f);

            float dist = Vector3.Distance(playerTransform.position, _transform.position);

            if (dist <= attackDist)
                curState = CurrentState.attack;
            else if (dist <= traceDist)
                curState = CurrentState.trace;
            else
                curState = CurrentState.idle;
        }
    }

    IEnumerator CheckStateAction()
    {
        while(!isDead)
        {
            switch (curState)
            {
                case CurrentState.idle:
                    nvAgent.Stop();
                    _animator.SetBool("isTrace", false);
                    break;
                case CurrentState.trace:
                    nvAgent.destination = playerTransform.position;
                    nvAgent.Resume();
                    _animator.SetBool("isTrace", true);
                    break;
                case CurrentState.attack:
                    
                    _animator.SetBool("isAttack", true);
                    break;
            }
            yield return null;
        }
    }
}
