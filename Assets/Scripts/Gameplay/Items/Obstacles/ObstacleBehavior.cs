using UnityEngine;

public abstract class ObstacleBehavior : MonoBehaviour
{
    protected ObstacleItem obstacleItem;
    protected int currentHealth;
    protected int maxHealth;

    public int CurrentHealth => currentHealth;
    public int MaxHealth => maxHealth;
    public bool IsDestroyed => currentHealth <= 0;

    public virtual void Initialize(ObstacleItem item, int health)
    {
        obstacleItem = item;
        maxHealth = health;
        currentHealth = health;
    }

    public abstract bool CanTakeDamageFrom(DamageSource source);
    public abstract bool CanFall();
    public abstract Sprite GetSprite();
    
    public virtual void TakeDamage(int damage = 1)
    {
        int previousHealth = currentHealth;
        currentHealth -= damage;
        
        UpdateVisuals(previousHealth);
        
        if (currentHealth <= 0)
        {
            DestroyObstacle();
        }
    }

    protected virtual void UpdateVisuals(int previousHealth)
    {
        if (obstacleItem.itemImage != null)
        {
            obstacleItem.itemImage.sprite = GetSprite();
        }
    }

    protected virtual void DestroyObstacle()
    {
        RectTransform rectTransform = obstacleItem.GetComponent<RectTransform>();
        ParticleEffectManager.Instance.GetPositionAndSize(rectTransform, out Vector2 position, out Vector2 size);
        
        ItemType itemType = obstacleItem.itemType;
        
        ParticleEffectManager.Instance.SpawnObstacleParticles(position, size, itemType, true);
        AudioManager.Instance.PlayObstacleSound(itemType, true);
        
        EventBus.Publish(new ObstacleDestroyedEvent
        {
            ObstacleType = itemType
        });
        
        obstacleItem.OnDestroyed();
        Destroy(obstacleItem.gameObject);
    }
}




