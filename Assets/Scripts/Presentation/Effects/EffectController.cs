using UnityEngine;

public class EffectController : MonoBehaviour
{
    public static EffectController Instance { get; private set; }

    private TransitionController transitionController;
    private RocketHintAnimator rocketHintAnimator;

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            MarkParentAsDontDestroy();
            InitializeControllers();
        }
        else
        {
            Destroy(gameObject);
        }
    }

    private void MarkParentAsDontDestroy()
    {
        if (transform.parent != null)
        {
            DontDestroyOnLoad(transform.parent.gameObject);
        }
        else
        {
            DontDestroyOnLoad(gameObject);
        }
    }

    private void InitializeControllers()
    {
        transitionController = GetComponentInChildren<TransitionController>() ?? FindFirstObjectByType<TransitionController>();
        if (transitionController == null)
        {
            GameObject transitionObj = new GameObject("TransitionController");
            transitionObj.transform.SetParent(transform);
            transitionObj.transform.localPosition = Vector3.zero;
            transitionObj.transform.localRotation = Quaternion.identity;
            transitionObj.transform.localScale = Vector3.one;
            transitionController = transitionObj.AddComponent<TransitionController>();
        }
        
        rocketHintAnimator = GetComponentInChildren<RocketHintAnimator>() ?? FindFirstObjectByType<RocketHintAnimator>();
        if (rocketHintAnimator == null)
        {
            GameObject hintObj = new GameObject("RocketHintAnimator");
            hintObj.transform.SetParent(transform);
            hintObj.transform.localPosition = Vector3.zero;
            hintObj.transform.localRotation = Quaternion.identity;
            hintObj.transform.localScale = Vector3.one;
            rocketHintAnimator = hintObj.AddComponent<RocketHintAnimator>();
        }
    }

}

