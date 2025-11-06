using System.Collections.Generic;
using UnityEngine;

public class RocketHintService
{
    private GridController gridController;
    private MatchDetectorService matchDetector;

    public void Initialize(GridController controller, MatchDetectorService detector)
    {
        gridController = controller;
        matchDetector = detector;
    }

    public void UpdateRocketHints()
    {
        ClearAllRocketHints();
        
        HashSet<GridCell> processedCells = new HashSet<GridCell>();
        List<CubeItem> rocketEligibleCubes = new List<CubeItem>();
        
        for (int x = 0; x < gridController.DataService.GridWidth; x++)
        {
            for (int y = 0; y < gridController.DataService.GridHeight; y++)
            {
                GridCell cell = gridController.GetCell(x, y);
                if (cell.currentItem is CubeItem && !processedCells.Contains(cell))
                {
                    List<GridCell> group = matchDetector.FindMatchingGroup(cell);
                    
                    foreach (GridCell groupCell in group)
                    {
                        processedCells.Add(groupCell);
                    }
                    
                    if (MatchValidator.CanCreateRocket(group))
                    {
                        foreach (GridCell groupCell in group)
                        {
                            if (groupCell.currentItem is CubeItem groupCube)
                            {
                                groupCube.SetRocketHint(true);
                                rocketEligibleCubes.Add(groupCube);
                            }
                        }
                    }
                }
            }
        }
        
        UpdateHintAnimator(rocketEligibleCubes);
    }

    private void ClearAllRocketHints()
    {
        for (int x = 0; x < gridController.DataService.GridWidth; x++)
        {
            for (int y = 0; y < gridController.DataService.GridHeight; y++)
            {
                if (gridController.GetItem(x, y) is CubeItem cube)
                {
                    cube.SetRocketHint(false);
                }
            }
        }
    }

    private void UpdateHintAnimator(List<CubeItem> hintCubes)
    {
        if (RocketHintAnimator.Instance != null)
        {
            if (hintCubes.Count > 0)
            {
                RocketHintAnimator.Instance.StartHintAnimation(hintCubes);
            }
            else
            {
                RocketHintAnimator.Instance.StopHintAnimation();
            }
        }
    }
}

