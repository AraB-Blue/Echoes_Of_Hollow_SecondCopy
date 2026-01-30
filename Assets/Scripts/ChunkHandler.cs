using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ChunkHandler : MonoBehaviour
{
    [SerializeField] GameObject player;
    [SerializeField] GameObject [] chunkParents;
    [SerializeField] float activationDistance = 50.0f;
    [SerializeField] float checkInterval = 1f;
    
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        StartCoroutine (CheckChunkDistance());
    }

    IEnumerator CheckChunkDistance()
    {
        while (true)
        {
            foreach (GameObject chunkParent in chunkParents)
            {
                float distanceToPlayer = Vector3.Distance (player.transform.position, chunkParent.transform.position);

                if (distanceToPlayer <= activationDistance)
                {
                    chunkParent.SetActive (true);
                }

                else
                {
                    chunkParent.SetActive (false);
                }
            }

            yield return new WaitForSeconds (checkInterval);
        }
    }
}
