using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class GoalItem : MonoBehaviour, IPoolable
{
    [Header("Goal Item Elements")]
    [SerializeField] private Image iconImage;
    [SerializeField] private TextMeshProUGUI countText;
    [SerializeField] private Image checkmarkImage;
    
    private ItemType itemType;
    private int targetCount;
    private Sprite checkmarkSprite;

    public void Initialize(ItemType type, int count)
    {
        itemType = type;
        targetCount = count;
        
        iconImage = GetComponent<Image>();
        countText = GetComponentInChildren<TextMeshProUGUI>();
        
        Transform checkmarkTransform = transform.Find("CheckmarkImage");
        if (checkmarkTransform != null)
        {
            checkmarkImage = checkmarkTransform.GetComponent<Image>();
        }
        
        if (checkmarkImage == null)
        {
            SetupCheckmarkImage();
        }
        
        LoadCheckmarkSprite();
        SetupIcon();
        UpdateCount(count);
    }

    private void LoadCheckmarkSprite()
    {
        checkmarkSprite = Resources.Load<Sprite>("CaseStudyAssets2025/UI/Gameplay/Top/goal_check");
    }

    private void SetupIcon()
    {
        Sprite obstacleSprite = GetObstacleSprite(itemType);
        iconImage.sprite = obstacleSprite;
        iconImage.color = Color.white;
    }

    private Sprite GetObstacleSprite(ItemType itemType)
    {
        switch (itemType)
        {
            case ItemType.Box:
                return Resources.Load<Sprite>("CaseStudyAssets2025/Obstacles/Box/box");
            case ItemType.Stone:
                return Resources.Load<Sprite>("CaseStudyAssets2025/Obstacles/Stone/stone");
            case ItemType.Vase:
                return Resources.Load<Sprite>("CaseStudyAssets2025/Obstacles/Vase/vase_01");
            default:
                return null;
        }
    }

    public void UpdateCount(int count)
    {
        if (count <= 0)
        {
            countText.text = "";
            countText.gameObject.SetActive(false);
            
            checkmarkImage.sprite = checkmarkSprite;
            checkmarkImage.color = Color.white;
            checkmarkImage.gameObject.SetActive(true);
        }
        else
        {
            countText.text = count.ToString();
            countText.color = Color.white;
            countText.gameObject.SetActive(true);
            
            checkmarkImage.gameObject.SetActive(false);
        }
    }

    public void SetupCheckmarkImage()
    {
        if (checkmarkImage == null)
        {
            GameObject checkmarkGO = new GameObject("CheckmarkImage");
            checkmarkGO.transform.SetParent(transform, false);
            
            checkmarkImage = checkmarkGO.AddComponent<Image>();
            
            RectTransform rectTransform = checkmarkImage.rectTransform;
            rectTransform.anchorMin = new Vector2(0.7f, 0);
            rectTransform.anchorMax = new Vector2(1.17f, 0.43f);
            rectTransform.sizeDelta = Vector2.zero;
            rectTransform.offsetMin = Vector2.zero;
            rectTransform.offsetMax = Vector2.zero;
            rectTransform.anchoredPosition = Vector2.zero;
            
            checkmarkImage.gameObject.SetActive(true);
        }
    }

    public void OnSpawn()
    {
        gameObject.SetActive(true);
    }

    public void OnDespawn()
    {
        gameObject.SetActive(false);
        countText.text = "";
        countText.gameObject.SetActive(false);
        if (checkmarkImage != null)
        {
            checkmarkImage.gameObject.SetActive(false);
        }
    }
}

