using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

public class BoardShaker : MonoBehaviour
{
    public static BoardShaker Instance { get; private set; }

    [SerializeField] private RectTransform boardRoot;

    private readonly List<ShakeRequest> shakes = new();
    private Vector2 originalAnchoredPos;

    private void Awake()
    {
        Instance = this;

        if (boardRoot == null)
            boardRoot = GetComponent<RectTransform>();

        originalAnchoredPos = boardRoot.anchoredPosition;
    }

    public void RequestShake(BoardShakeData data)
    {
        if (data.Intensity <= 0f || data.Duration <= 0f)
            return;

        shakes.Add(new ShakeRequest(data.Intensity, data.Duration));
    }

    private void Update()
    {
        // Debug trigger
        if (Keyboard.current.sKey.wasPressedThisFrame)
        {
            Debug.Log("Requesting board shake (debug)");
            RequestShake(new BoardShakeData { Intensity = 20f, Duration = 0.25f });
        }

        boardRoot.anchoredPosition = originalAnchoredPos;

        // Apply shake offset
        Vector2 shakeOffset = GetShakeOffset();
        boardRoot.anchoredPosition += shakeOffset;
    }

    private Vector2 GetShakeOffset()
    {
        float totalIntensity = 0f;

        for (int i = shakes.Count - 1; i >= 0; i--)
        {
            var s = shakes[i];
            if (s.Elapsed > s.Duration)
            {
                shakes.RemoveAt(i);
                continue;
            }

            float falloff = 1f - (s.Elapsed / s.Duration);
            totalIntensity += s.Intensity * falloff;
            s.Elapsed += Time.deltaTime;
        }

        return UnityEngine.Random.insideUnitCircle * totalIntensity;
    }

    [Serializable]
    private class ShakeRequest
    {
        public float Intensity;
        public float Duration;
        public float Elapsed;

        public ShakeRequest(float intensity, float duration)
        {
            Intensity = intensity;
            Duration = duration;
            Elapsed = 0f;
        }
    }
}

[Serializable]
public class BoardShakeData
{
    public float Intensity;
    public float Duration;
}
