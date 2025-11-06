using UnityEngine;

public enum ItemType
{
    Empty,
    RedCube,
    GreenCube,
    BlueCube,
    YellowCube,
    RandomCube,
    HorizontalRocket,
    VerticalRocket,
    Box,
    Stone,
    Vase
}

public enum GameState
{
    MainMenu,
    Playing,
    GameWon,
    GameLost,
    Finished,
    Paused
}

public enum ObstacleState
{
    Full,
    Damaged,
    Destroyed
}

public enum CubeColor
{
    Red,
    Green,
    Blue,
    Yellow
}

public enum DamageSource
{
    AdjacentBlast,
    Rocket
} 