using UnityEngine;
using System.Collections.Generic;

public class DummyCullingManager : MonoBehaviour
{
    public static DummyCullingManager Instance; // replace with singleton
    public Camera currentCamera;
    public float dummyCullingDistance = 50f;
    public Transform enemyHolder;

    private CullingGroup cullingGroup;
    private DummyController [] targets;
    private BoundingSphere [] boundingSpheres;

    void Awake()
    {
        Instance = this;
        cullingGroup = new CullingGroup();
    }

    private void OnEnable()
    {
        cullingGroup.onStateChanged += OnStateChanged;
        // subscribe to an event that listens for changes in load zone
    }

    private void OnDisable()
    {
        cullingGroup.onStateChanged -= OnStateChanged;
        // unsubscribe from an event that listens for changes in load zone
    }

    private void Start()
    {
        cullingGroup.targetCamera = currentCamera;
        Initialize();
    }

    private void Update()
    {
        cullingGroup.SetDistanceReferencePoint(currentCamera.transform);
    }

    void OnDestroy()
    {
        if (cullingGroup != null)
            cullingGroup.Dispose();
    }

    // Call this whenever a loading zone is switched and also change the enemyHolder to a new group
    // maybe make enemyHolder an array of enemy holders instead
    private void Initialize()
    {
        targets = enemyHolder.transform.GetComponentsInChildren<DummyController>();
        boundingSpheres = new BoundingSphere[targets.Length];

        for (int i = 0; i < targets.Length; i++)
        {
            targets[i].CullingIndex = i;
            boundingSpheres[i] = new BoundingSphere(targets[i].transform.position, 1f);
        }

        cullingGroup.SetBoundingSpheres(boundingSpheres);
        cullingGroup.SetBoundingSphereCount(targets.Length);

        cullingGroup.SetDistanceReferencePoint(currentCamera.transform);
        cullingGroup.SetBoundingDistances(new float[] { dummyCullingDistance });
    }

    // This is called whenever a change is made to the distance state of the culling group
    private void OnStateChanged(CullingGroupEvent @event)
    {
        if (@event.index < 0 || @event.index >= targets.Length) return;

        DummyController target = targets[@event.index];

        if (target != null)
        {
            bool isVisible = @event.currentDistance == 0;
            target.OnVisibilityChanged(isVisible);
        }
    }

    // Updates a specific bounding sphere's index to a new given position
    public void UpdateBoundingSphere(int index, Vector3 position)
    {
        if (index < 0 || index >= boundingSpheres.Length) return;

        boundingSpheres[index] = new BoundingSphere(position, 1f);
        cullingGroup.SetBoundingSpheres(boundingSpheres);
    }
}
