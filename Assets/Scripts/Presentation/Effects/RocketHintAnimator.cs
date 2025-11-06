using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class RocketHintAnimator : MonoBehaviour
{
    public static RocketHintAnimator Instance { get; private set; }
    
    [Header("Animation Settings")]
    [SerializeField] private float blinkDuration = 0.8f;
    [SerializeField] private AnimationCurve blinkCurve = AnimationCurve.EaseInOut(0, 0, 1, 1);
    
    private List<CubeItem> hintCubes = new List<CubeItem>();
    private Coroutine blinkCoroutine;

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
        }
        else
        {
            Destroy(gameObject);
        }
    }

    public void StartHintAnimation(List<CubeItem> cubes)
    {
        StopHintAnimation();
        
        hintCubes.Clear();
        hintCubes.AddRange(cubes);
        
        if (hintCubes.Count > 0)
        {
            blinkCoroutine = StartCoroutine(BlinkHintLoop());
        }
    }

    public void StopHintAnimation()
    {
        if (blinkCoroutine != null)
        {
            StopCoroutine(blinkCoroutine);
            blinkCoroutine = null;
        }
        
        foreach (CubeItem cube in hintCubes)
        {
            if (cube != null)
            {
                cube.SetHintBlend(0f);
            }
        }
        
        hintCubes.Clear();
    }

    private IEnumerator BlinkHintLoop()
    {
        while (hintCubes.Count > 0)
        {
            yield return StartCoroutine(BlinkPhase(0f, 1f));
            yield return StartCoroutine(BlinkPhase(1f, 0f));
        }
    }

    private IEnumerator BlinkPhase(float startBlend, float endBlend)
    {
        float elapsed = 0f;
        
        while (elapsed < blinkDuration)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / blinkDuration;
            float blendValue = Mathf.Lerp(startBlend, endBlend, blinkCurve.Evaluate(t));
            
            for (int i = hintCubes.Count - 1; i >= 0; i--)
            {
                if (hintCubes[i] != null)
                {
                    hintCubes[i].SetHintBlend(blendValue);
                }
                else
                {
                    hintCubes.RemoveAt(i);
                }
            }
            
            yield return null;
        }
        
        foreach (CubeItem cube in hintCubes)
        {
            if (cube != null)
            {
                cube.SetHintBlend(endBlend);
            }
        }
    }

    public void RemoveCubeFromHints(CubeItem cube)
    {
        hintCubes.Remove(cube);
        
        if (hintCubes.Count == 0)
        {
            StopHintAnimation();
        }
    }
}

