using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class RocketAnimator : MonoBehaviour
{
    [Header("Rocket Creation Settings")]
    [Tooltip("How fast cubes gather together when creating a rocket (duration in seconds). Lower values = faster gathering.")]
    [SerializeField] [Range(0.1f, 2.0f)] private float creationAnimationDuration = 0.4f;

    public IEnumerator AnimateRocketCreation(List<GridCell> sourceGroup, GridCell targetCell, ItemType rocketType)
    {
        if (sourceGroup == null || targetCell == null || sourceGroup.Count == 0)
            yield break;

        var animatingObjects = CreateAnimatingCubes(sourceGroup);
        yield return AnimateToTarget(animatingObjects, targetCell);
        CleanupAnimatingCubes(animatingObjects);
        
        AudioManager.Instance.PlayRocketCreationSound();
        yield return new WaitForSeconds(0.1f);
    }

    private List<(GameObject cube, CubeItem original)> CreateAnimatingCubes(List<GridCell> sourceGroup)
    {
        var animatingObjects = new List<(GameObject, CubeItem)>();

        foreach (GridCell sourceCell in sourceGroup)
        {
            if (sourceCell.currentItem is CubeItem cube)
            {
                cube.gameObject.SetActive(false);

                GameObject animCube = CreateAnimatingCube(cube, sourceCell);
                animatingObjects.Add((animCube, cube));
            }
        }

        return animatingObjects;
    }

    private GameObject CreateAnimatingCube(CubeItem originalCube, GridCell sourceCell)
    {
        GridController gridController = FindFirstObjectByType<GridController>();
        
        GameObject animCube = new GameObject("AnimatingCube");
        animCube.transform.SetParent(gridController.GridContainer, false);

        RectTransform animRect = animCube.AddComponent<RectTransform>();
        RectTransform sourceRect = sourceCell.GetComponent<RectTransform>();
        
        CopyRectTransformProperties(animRect, sourceRect);

        Image cubeImage = animCube.AddComponent<Image>();
        cubeImage.sprite = originalCube.GetSprite();
        cubeImage.raycastTarget = false;
        cubeImage.preserveAspect = true;

        animCube.transform.SetSiblingIndex(gridController.GridContainer.childCount - 1);
        return animCube;
    }

    private void CopyRectTransformProperties(RectTransform target, RectTransform source)
    {
        target.anchorMin = source.anchorMin;
        target.anchorMax = source.anchorMax;
        target.pivot = source.pivot;
        target.sizeDelta = source.sizeDelta;
        target.anchoredPosition = source.anchoredPosition;
    }

    private IEnumerator AnimateToTarget(List<(GameObject cube, CubeItem original)> animatingObjects, GridCell targetCell)
    {
        RectTransform targetRect = targetCell.GetComponent<RectTransform>();
        Vector2 targetPosition = targetRect.anchoredPosition;

        var startPositions = new List<Vector2>();
        foreach (var (cube, _) in animatingObjects)
        {
            RectTransform animRect = cube.GetComponent<RectTransform>();
            startPositions.Add(animRect.anchoredPosition);
        }

        float elapsed = 0f;
        while (elapsed < creationAnimationDuration)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.SmoothStep(0f, 1f, elapsed / creationAnimationDuration);

            for (int i = 0; i < animatingObjects.Count; i++)
            {
                if (animatingObjects[i].cube != null)
                {
                    RectTransform animRect = animatingObjects[i].cube.GetComponent<RectTransform>();
                    animRect.anchoredPosition = Vector2.Lerp(startPositions[i], targetPosition, t);
                    
                    float scale = 1f + (0.2f * Mathf.Sin(t * Mathf.PI));
                    animRect.localScale = Vector3.one * scale;
                }
            }

            yield return null;
        }
    }

    private void CleanupAnimatingCubes(List<(GameObject cube, CubeItem original)> animatingObjects)
    {
        foreach (var (cube, original) in animatingObjects)
        {
            if (cube != null) Destroy(cube);
            
            if (original != null)
            {
                Destroy(original.gameObject);
                original.currentCell?.RemoveItem();
            }
        }
    }
}

