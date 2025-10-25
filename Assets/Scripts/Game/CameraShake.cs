using System;
using System.Collections.Generic;
using UnityEngine;

public class CameraShake : MonoBehaviour
{
    public static CameraShake Instance { get; private set; }
    private readonly List<ShakeRequest> shakes = new();
    private Vector3 originalPosition;

    private void Awake()
    {
        Instance = this;
        originalPosition = transform.localPosition;
    }

    public void RequestShake(CameraShakeData data)
    {
        shakes.Add(new ShakeRequest(data.Intensity, data.Duration));
    }

    private void Update()
    {
        transform.localPosition = originalPosition;

        Vector3 shakeOffset = GetShakeOffset(transform.position);

        transform.localPosition += shakeOffset;
    }

    public Vector3 GetShakeOffset(Vector3 cameraPos)
    {
        float totalIntensity = 0f;
        for (int i = shakes.Count - 1; i >= 0; i--)
        {
            var s = shakes[i];
            if (s.Elapsed > s.Duration)
            {
                shakes.RemoveAt(i);
            }
            else
            {
                Debug.Log("Applying shake: " + s.Intensity);
                float falloff = 1f;
                totalIntensity += s.Intensity * falloff;
                s.Elapsed += Time.deltaTime;
            }
        }
        return UnityEngine.Random.insideUnitSphere * totalIntensity;
    }

    class ShakeRequest
    {
        public float Intensity;
        public float Duration;
        public float Elapsed;

        public ShakeRequest(float i, float d)
        {
            Intensity = i;
            Duration = d;
            Elapsed = 0f;
        }
    }
}

[Serializable]
public class CameraShakeData
{
    public float Intensity;
    public float Duration;
}
