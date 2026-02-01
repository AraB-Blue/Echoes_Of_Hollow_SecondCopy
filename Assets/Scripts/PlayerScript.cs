using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.AI;

public class PlayerScript : MonoBehaviour
{
    public static PlayerScript instance;
    private Rigidbody rb;
    private NavMeshAgent agent;


    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Awake()
    {

        rb = GetComponent<Rigidbody>();
        agent = GetComponent<NavMeshAgent>();
        
        if (instance !=null)
        {
            Destroy(gameObject);
            return;
        }

        instance = this;
        DontDestroyOnLoad(gameObject);

        SceneManager.sceneLoaded += OnSceneLoaded;
    }

    void OnSceneLoaded (Scene scene, LoadSceneMode mode)
    {
        agent.enabled = false;

        //transform.position = spawnPoint.position;

        agent.enabled = true;
    }

}
