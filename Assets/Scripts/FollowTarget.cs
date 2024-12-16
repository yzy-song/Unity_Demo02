using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class FollowTarget : MonoBehaviour
{
    private Transform player;
    private Vector3 offset;

    // Start is called before the first frame update
    void Start()
    {
        player = GameObject.FindGameObjectWithTag("Player").transform;
        offset = transform.position - player.position;
    }

    private void FixedUpdate()
    {
        if (transform != null && player != null)
        {

            transform.position = Vector3.Lerp(transform.position, player.position + offset, Time.deltaTime);
        }

    }

}
