using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class DestroyTimeLimit : MonoBehaviour
{
    public float timeLimit;

    private void Start()
    {
        Destroy(gameObject, timeLimit);
    }
}
