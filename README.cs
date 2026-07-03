using System;
using UnityEngine;

[Serializable]
public class MediaVolumeConfig
{
    public float min = 0f;
    public float max = 0.8f;
    public float @default = 0.4f;
    public float step = 0.05f;

    public void Normalize()
    {
        min = Mathf.Clamp01(min);
        max = Mathf.Clamp01(max);

        if (max < min)
        {
            float temp = min;
            min = max;
            max = temp;
        }

        @default = Mathf.Clamp(@default, min, max);

        if (step <= 0f)
        {
            step = 0.05f;
        }

        step = Mathf.Clamp(step, 0.001f, 1f);
    }

    public static MediaVolumeConfig CreateDefault()
    {
        MediaVolumeConfig config = new MediaVolumeConfig
        {
            min = 0f,
            max = 0.8f,
            @default = 0.4f,
            step = 0.05f
        };

        config.Normalize();
        return config;
    }
}
