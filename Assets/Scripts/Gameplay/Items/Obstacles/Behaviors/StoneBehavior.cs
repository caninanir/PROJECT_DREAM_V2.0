using UnityEngine;

public class StoneBehavior : ObstacleBehavior
{
    private Sprite stoneSprite;

    public override void Initialize(ObstacleItem item, int health)
    {
        base.Initialize(item, health);
        stoneSprite = Resources.Load<Sprite>("CaseStudyAssets2025/Obstacles/Stone/stone");
    }

    public override bool CanTakeDamageFrom(DamageSource source)
    {
        return source == DamageSource.Rocket;
    }

    public override bool CanFall()
    {
        return false;
    }

    public override Sprite GetSprite()
    {
        return stoneSprite;
    }
}




