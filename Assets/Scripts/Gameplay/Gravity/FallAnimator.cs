using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Linq;

public class FallAnimator : MonoBehaviour
{
    [Header("Physics-Based Fall Settings")]
    [SerializeField] private AnimationCurve gravityCurve = new AnimationCurve();
    
    [Header("Landing Effects")]
    [SerializeField] private bool enableLandingBounce = true;
    [SerializeField] private float bounceHeight = 0.05f;
    [SerializeField] private float bounceDuration = 0.12f;
    
    [Header("Visual Polish")]
    [SerializeField] public bool enableSubtleRotation = false;
    [SerializeField] public float maxRotationAngle = 2f;

    private HashSet<int> soundPlayedColumns = new HashSet<int>();

    private void Awake()
    {
        SetupDefaultGravityCurve();
    }

    private void SetupDefaultGravityCurve()
    {
        gravityCurve = new AnimationCurve(
            new Keyframe(0f, 0f, 0f, 1.2f),
            new Keyframe(0.3f, 0.15f, 1.8f, 2.2f),
            new Keyframe(0.7f, 0.6f, 3f, 2.8f),
            new Keyframe(1f, 1f, 1.8f, 0f)
        );
        
        for (int i = 0; i < gravityCurve.keys.Length; i++)
        {
            gravityCurve.SmoothTangents(i, 0.3f);
        }
    }

    public IEnumerator AnimateFalls(List<FallOperation> operations)
    {
        soundPlayedColumns.Clear();
        
        if (operations.Count == 0)
        {
            yield break;
        }
        
        float maxDuration = operations.Max(op => op.duration);
        float elapsed = 0f;
        
        while (elapsed < maxDuration)
        {
            elapsed += Time.deltaTime;
            
            foreach (var op in operations)
            {
                if (op.itemTransform == null) continue;
                
                float normalizedTime = Mathf.Clamp01(elapsed / op.duration);
                float curveValue = gravityCurve.Evaluate(normalizedTime);
                
                Vector2 currentPosition = Vector2.Lerp(op.startPosition, op.targetPosition, curveValue);
                op.itemTransform.anchoredPosition = currentPosition;
                
                if (enableSubtleRotation && op.targetRotation != op.startRotation)
                {
                    op.itemTransform.rotation = Quaternion.Lerp(
                        op.startRotation, 
                        op.targetRotation, 
                        normalizedTime * 0.5f
                    );
                }
                
                if (normalizedTime >= 0.95f && op.fallDistance >= 1 && 
                    op.item != null && op.item.currentCell != null &&
                    !soundPlayedColumns.Contains(op.item.currentCell.x))
                {
                    soundPlayedColumns.Add(op.item.currentCell.x);
                    AudioManager.Instance.PlayCubeFallSound();
                }
            }
            
            yield return null;
        }
        
        foreach (var op in operations)
        {
            if (op.itemTransform != null)
            {
                op.itemTransform.anchoredPosition = op.targetPosition;
                if (enableSubtleRotation)
                {
                    op.itemTransform.rotation = op.startRotation;
                }
                
                if (enableLandingBounce && op.fallDistance >= 2)
                {
                    StartCoroutine(ApplyLandingBounce(op));
                }
            }
        }
    }

    private IEnumerator ApplyLandingBounce(FallOperation operation)
    {
        if (operation.itemTransform == null) yield break;
        
        Vector2 landingPosition = operation.targetPosition;
        
        float elapsed = 0f;
        
        while (elapsed < bounceDuration)
        {
            elapsed += Time.deltaTime;
            float normalizedTime = Mathf.Clamp01(elapsed / bounceDuration);
            
            float bounceOffset = Mathf.Sin(normalizedTime * Mathf.PI) * bounceHeight * 100f * (1f - normalizedTime);
            Vector2 currentPosition = landingPosition + Vector2.up * bounceOffset;
            
            operation.itemTransform.anchoredPosition = currentPosition;
            yield return null;
        }
        
        operation.itemTransform.anchoredPosition = landingPosition;
    }
}

