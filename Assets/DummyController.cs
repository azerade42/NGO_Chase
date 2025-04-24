using UnityEngine;

public class DummyController : MonoBehaviour
{
    [HideInInspector] public int CullingIndex = -1;
    [SerializeField] private float cullingUpdateInterval = 3f;

    private Animator animator;
    private float nextCullingTime = 0f;

    void Awake()
    {
        animator = GetComponent<Animator>();
    }

    // Called whenever the culling group's distance state is changed
    public void OnVisibilityChanged(bool isVisible)
    {
        if (animator != null)
            animator.enabled = isVisible;
    }

    void Update()
    {
        if (CullingIndex == -1)
            return;

        // Updates the bounding sphere for this dummy after a given time repeatedly
        if (Time.time >= nextCullingTime)
        {
            nextCullingTime = Time.time + cullingUpdateInterval;
            DummyCullingManager.Instance.UpdateBoundingSphere(CullingIndex, transform.position);
        }
    }
}