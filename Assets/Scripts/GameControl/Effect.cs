using System;
using UnityEngine;

public class Effect : MonoBehaviour
{
    // a simple script to scale the size, speed and lifetime of a particle system

    public float multiplier = 0.1f;
    public float playbackTime = 3;

    private void Start()
    {

    }

    private void Update()
    {
        if (transform.childCount == 0)
        {
            Destroy(gameObject);
        }
    }

    public void SetSize(float size)
    {
        size *= multiplier;
        ParticleSystem[] systems = GetComponentsInChildren<ParticleSystem>();
        foreach (ParticleSystem system in systems)
        {
            ParticleSystem.MainModule mainModule = system.main;
            ParticleSystem.MinMaxCurve startSize = mainModule.startSize;
            ParticleSystem.MinMaxCurve startSpeed = mainModule.startSpeed;

            ParticleSystem.ShapeModule shapeModule = system.shape;
            shapeModule.radius *= size;

            startSize.constantMax *= size;
            startSize.constantMin *= size;

            startSpeed.constantMax *= size;
            startSpeed.constantMin *= size;

            //mainModule.startLifetimeMultiplier *= Mathf.Lerp(multiplier, 1, 0.5f);

            mainModule.startSize = startSize;
            mainModule.startSpeed = startSpeed;
            mainModule.simulationSpeed /= playbackTime;

            system.Clear();
            system.Play();
        }
    }
}
