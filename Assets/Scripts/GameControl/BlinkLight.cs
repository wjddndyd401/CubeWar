using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class BlinkLight : MonoBehaviour
{
    bool lightIncrease = true;

    void Update()
    {
        Light light = GetComponent<Light>();
        if(light.type == LightType.Point)
        {
            if (lightIncrease) light.range += Time.deltaTime * 50;
            else light.range -= Time.deltaTime * 50;

            if (light.range >= 10) lightIncrease = false;
            if (light.range <= 1) lightIncrease = true;
        }
    }
}
