using UnityEngine;

public class BoxBehavior : ObstacleBehavior
{
    private Sprite boxSprite;

    public override void Initialize(ObstacleItem item, int health)
    {
        base.Initialize(item, health);
        boxSprite = Resources.Load<Sprite>("CaseStudyAssets2025/Obstacles/Box/box");
    }

    public override bool CanTakeDamageFrom(DamageSource source)
    {
        return source == DamageSource.AdjacentBlast || source == DamageSource.Rocket;
    }

    public override bool CanFall()
    {
        return false;
    }

    public override Sprite GetSprite()
    {
        return boxSprite;
    }
}




