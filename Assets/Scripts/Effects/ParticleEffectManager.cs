using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class ParticleEffectManager : MonoBehaviour
{
    public static ParticleEffectManager Instance { get; private set; }

    [Header("Cube Particle Sprites")]
    [SerializeField] private Sprite redCubeParticle;
    [SerializeField] private Sprite greenCubeParticle;
    [SerializeField] private Sprite blueCubeParticle;
    [SerializeField] private Sprite yellowCubeParticle;

    [Header("Obstacle Particle Sprites")]
    [SerializeField] private Sprite boxParticle1;
    [SerializeField] private Sprite boxParticle2;
    [SerializeField] private Sprite boxParticle3;
    [SerializeField] private Sprite vaseParticle1;
    [SerializeField] private Sprite vaseParticle2;
    [SerializeField] private Sprite vaseParticle3;
    [SerializeField] private Sprite stoneParticle1;
    [SerializeField] private Sprite stoneParticle2;
    [SerializeField] private Sprite stoneParticle3;

    private Sprite[] boxSprites;
    private Sprite[] vaseSprites;
    private Sprite[] stoneSprites;

    [Header("Animation Settings")]
    [SerializeField] private float particleLifetime = 1.5f;
    [SerializeField] private float explosionForce = 300f;
    [SerializeField] private float gravity = 800f;
    [SerializeField] private float rotationSpeed = 360f;

    private Transform container;
    private readonly List<ParticleElement> activeParticles = new List<ParticleElement>();

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            CreateContainer();
            LoadSprites();
        }
        else
        {
            Destroy(gameObject);
        }
    }

    private void Update()
    {
        UpdateParticles();
    }

    public void SpawnCubeParticles(Vector2 position, Vector2 size, ItemType type)
    {
        Sprite sprite = GetCubeSprite(type);
        
        for (int i = 0; i < 3; i++)
        {
            CreateParticle(position, sprite, size);
        }
        
        for (int i = 0; i < 3; i++)
        {
            CreateParticle(position, sprite, size * Random.Range(0.4f, 0.7f));
        }
    }

    public void SpawnCubeParticles(Transform src, ItemType type)
    {
        Vector2 position = UIPosition(src);
        Vector2 size = UISize(src);
        SpawnCubeParticles(position, size, type);
    }

    public void SpawnObstacleParticles(Vector2 position, Vector2 size, ItemType type, bool destroyed)
    {
        Sprite[] sprites = GetObstacleSprites(type);
        if (sprites.Length == 0) return;

        if (type == ItemType.Vase && !destroyed)
        {
            CreateParticle(position, sprites[Random.Range(0, sprites.Length)], size);
            return;
        }

        foreach (Sprite sprite in sprites)
        {
            CreateParticle(position, sprite, size);
        }
        
        for (int i = 0; i < 3; i++)
        {
            CreateParticle(position, sprites[Random.Range(0, sprites.Length)], size * Random.Range(0.4f, 0.7f));
        }
    }

    public void SpawnObstacleParticles(Transform src, ItemType type, bool destroyed)
    {
        Vector2 position = UIPosition(src);
        Vector2 size = UISize(src);
        SpawnObstacleParticles(position, size, type, destroyed);
    }

    private void CreateContainer()
    {
        GameObject go = new GameObject("ParticleContainer");
        go.transform.SetParent(transform, false);
        container = go.transform;
        CanvasGroup cg = go.AddComponent<CanvasGroup>();
        cg.interactable = false;
        cg.blocksRaycasts = false;
    }

    private void LoadSprites()
    {
        redCubeParticle = LoadSprite("CaseStudyAssets2025/Cubes/Particles/particle_red");
        greenCubeParticle = LoadSprite("CaseStudyAssets2025/Cubes/Particles/particle_green");
        blueCubeParticle = LoadSprite("CaseStudyAssets2025/Cubes/Particles/particle_blue");
        yellowCubeParticle = LoadSprite("CaseStudyAssets2025/Cubes/Particles/particle_yellow");

        boxParticle1 = LoadSprite("CaseStudyAssets2025/Obstacles/Box/Particles/particle_box_01");
        boxParticle2 = LoadSprite("CaseStudyAssets2025/Obstacles/Box/Particles/particle_box_02");
        boxParticle3 = LoadSprite("CaseStudyAssets2025/Obstacles/Box/Particles/particle_box_03");

        vaseParticle1 = LoadSprite("CaseStudyAssets2025/Obstacles/Vase/Particles/particle_vase_01");
        vaseParticle2 = LoadSprite("CaseStudyAssets2025/Obstacles/Vase/Particles/particle_vase_02");
        vaseParticle3 = LoadSprite("CaseStudyAssets2025/Obstacles/Vase/Particles/particle_vase_03");

        stoneParticle1 = LoadSprite("CaseStudyAssets2025/Obstacles/Stone/Particles/particle_stone_01");
        stoneParticle2 = LoadSprite("CaseStudyAssets2025/Obstacles/Stone/Particles/particle_stone_02");
        stoneParticle3 = LoadSprite("CaseStudyAssets2025/Obstacles/Stone/Particles/particle_stone_03");

        boxSprites = new[] { boxParticle1, boxParticle2, boxParticle3 };
        vaseSprites = new[] { vaseParticle1, vaseParticle2, vaseParticle3 };
        stoneSprites = new[] { stoneParticle1, stoneParticle2, stoneParticle3 };
    }

    private Sprite LoadSprite(string path)
    {
        return Resources.Load<Sprite>(path);
    }

    private void UpdateParticles()
    {
        for (int i = activeParticles.Count - 1; i >= 0; i--)
        {
            ParticleElement particle = activeParticles[i];
            if (particle == null)
            {
                activeParticles.RemoveAt(i);
                continue;
            }
            
            particle.UpdateParticle(Time.deltaTime, gravity, particleLifetime);
            
            if (particle.IsExpired())
            {
                activeParticles.RemoveAt(i);
                PoolManager.Instance.ReturnParticle(particle, container);
            }
        }
    }

    private void CreateParticle(Vector2 pos, Sprite sprite, Vector2 srcSize)
    {
        ParticleElement particle = PoolManager.Instance.GetParticle(container);
        
        Vector2 velocity = RandomVelocity();
        float rotation = Random.Range(0f, 360f);
        float rotSpeed = Random.Range(-rotationSpeed, rotationSpeed);
        
        particle.Setup(pos, sprite, srcSize * 0.8f, velocity, particleLifetime, rotation, rotSpeed);
        activeParticles.Add(particle);
    }

    private Vector2 RandomVelocity()
    {
        float angle = Random.Range(0f, Mathf.PI * 2f);
        float force = Random.Range(explosionForce * 0.7f, explosionForce * 1.3f);
        Vector2 velocity = new Vector2(Mathf.Cos(angle) * force, Mathf.Sin(angle) * force);
        velocity.y = Mathf.Abs(velocity.y) * Random.Range(0.5f, 1.5f);
        return velocity;
    }

    public void GetPositionAndSize(RectTransform rectTransform, out Vector2 position, out Vector2 size)
    {
        Vector3[] corners = new Vector3[4];
        rectTransform.GetWorldCorners(corners);
        Vector3 worldCenter = (corners[0] + corners[2]) * 0.5f;
        position = container.InverseTransformPoint(worldCenter);
        size = rectTransform.sizeDelta;
    }

    private Vector2 UIPosition(Transform t)
    {
        RectTransform r = t.GetComponent<RectTransform>();
        Vector3[] corners = new Vector3[4];
        r.GetWorldCorners(corners);
        Vector3 worldCenter = (corners[0] + corners[2]) * 0.5f;
        return container.InverseTransformPoint(worldCenter);
    }

    private static Vector2 UISize(Transform t)
    {
        RectTransform r = t?.GetComponent<RectTransform>();
        return r ? r.sizeDelta : new Vector2(60f, 60f);
    }

    private Sprite GetCubeSprite(ItemType type)
    {
        switch (type)
        {
            case ItemType.RedCube: return redCubeParticle;
            case ItemType.GreenCube: return greenCubeParticle;
            case ItemType.BlueCube: return blueCubeParticle;
            case ItemType.YellowCube: return yellowCubeParticle;
            default: return null;
        }
    }

    private Sprite[] GetObstacleSprites(ItemType type)
    {
        switch (type)
        {
            case ItemType.Box: return boxSprites;
            case ItemType.Vase: return vaseSprites;
            case ItemType.Stone: return stoneSprites;
            default: return null;
        }
    }
    
    public void CleanupAllParticles()
    {
        for (int i = activeParticles.Count - 1; i >= 0; i--)
        {
            ParticleElement particle = activeParticles[i];
            if (particle != null)
            {
                PoolManager.Instance.ReturnParticle(particle, container);
            }
        }
        activeParticles.Clear();
        
        for (int i = container.childCount - 1; i >= 0; i--)
        {
            Transform child = container.GetChild(i);
            Destroy(child.gameObject);
        }
    }
}
