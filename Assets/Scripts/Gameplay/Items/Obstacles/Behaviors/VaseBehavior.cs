using UnityEngine;

public class VaseBehavior : ObstacleBehavior
{
    private Sprite vaseFullSprite;
    private Sprite vaseDamagedSprite;

    public override void Initialize(ObstacleItem item, int health)
    {
        base.Initialize(item, health);
        vaseFullSprite = Resources.Load<Sprite>("CaseStudyAssets2025/Obstacles/Vase/vase_01");
        vaseDamagedSprite = Resources.Load<Sprite>("CaseStudyAssets2025/Obstacles/Vase/vase_02");
    }

    public override bool CanTakeDamageFrom(DamageSource source)
    {
        return source == DamageSource.AdjacentBlast || source == DamageSource.Rocket;
    }

    public override bool CanFall()
    {
        return true;
    }

    public override Sprite GetSprite()
    {
        return currentHealth >= maxHealth ? vaseFullSprite : vaseDamagedSprite;
    }

    protected override void UpdateVisuals(int previousHealth)
    {
        base.UpdateVisuals(previousHealth);
        
        if (previousHealth > 1 && currentHealth > 0)
        {
            RectTransform rectTransform = obstacleItem.GetComponent<RectTransform>();
            ParticleEffectManager.Instance.GetPositionAndSize(rectTransform, out Vector2 position, out Vector2 size);
            
            ParticleEffectManager.Instance.SpawnObstacleParticles(position, size, obstacleItem.itemType, false);
            AudioManager.Instance.PlayObstacleSound(obstacleItem.itemType, false);
        }
    }
}




