using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

public class GruntScript : MonoBehaviour
{
    private enum MoveMode { None, SpinInPlace, PatrolLine }

    [Header("Detection Settings")]
    [Tooltip("Max distance the grunt can see the player.")]
    [SerializeField] private float viewDistance = 6f;

    [Tooltip("Full field-of-view angle in degrees (cone width).")]
    [Range(0f, 360f)]
    [SerializeField] private float viewAngle = 90f;

    [Tooltip("Optional transform used as the eye origin for raycasts. If null, uses this transform position.")]
    [SerializeField] private Transform eye;

    [Tooltip("Use Transform.up as facing direction instead of Transform.right.")]
    [SerializeField] private bool useUpAsForward = false;

    [Header("Layers")]
    [Tooltip("Layers that block vision (e.g., Walls). Should NOT include the player layer.")]
    [SerializeField] private LayerMask obstructionMask;

    [Tooltip("Layer of the player.")]
    [SerializeField] private LayerMask playerMask;

    [Header("Raycast Options")]
    [Tooltip("If true, triggers on obstruction layers will also block line of sight.")]
    [SerializeField] private bool triggersBlockVision = true;
    [Tooltip("Draw a debug line for the LOS ray and mark first hit point in scene view.")]
    [SerializeField] private bool debugDrawLOS = false;

    [Header("Player Reference")]
    [SerializeField] private Transform player; // If null, will try to find by tag "Player" on Start
    [SerializeField] private bool autoFindPlayerByTag = true;

    // Facing control
    [Header("Facing")]
    [Tooltip("If true, the grunt's facing (used by FOV and animations) is decoupled from the Transform rotation.")]
    [SerializeField] private bool decoupleFacingFromRotation = true;
    private float facingAngleDeg; // represents the angle for the 'right' vector

    // Cache
    private Vector2 Forward
    {
        get
        {
            if (!decoupleFacingFromRotation)
            {
                return useUpAsForward ? (Vector2)transform.up : (Vector2)transform.right;
            }
            // Build a direction from facingAngleDeg
            float rad = facingAngleDeg * Mathf.Deg2Rad;
            Vector2 rightDir = new Vector2(Mathf.Cos(rad), Mathf.Sin(rad));
            if (useUpAsForward)
            {
                // rotate right to up
                return new Vector2(-rightDir.y, rightDir.x);
            }
            return rightDir;
        }
    }

    // Gizmo state
    private bool lastHasLOS = false;

    [Header("FOV Visual (Optional)")]
    [Tooltip("Draw a visible cone in-game representing the grunt's field of view.")]
    [SerializeField] private bool showFOV = true;
    [Tooltip("How many rays to sample across the cone for the visual. Higher = smoother but more CPU.")]
    [Range(6, 256)]
    [SerializeField] private int fovRayCount = 60;
    [Tooltip("Material for the FOV mesh. If empty, a simple transparent material will be created at runtime.")]
    [SerializeField] private Material fovMaterial;
    [SerializeField] private Color fovColor = new Color(1f, 1f, 0f, 0.2f);
    [Tooltip("Sorting layer name for the FOV mesh (must exist in project).")]
    [SerializeField] private string fovSortingLayerName = "Default";
    [Tooltip("Sorting order for the FOV mesh (higher renders on top).")]
    [SerializeField] private int fovSortingOrder = 50;

    private GameObject fovGO;
    private Mesh fovMesh;
    private MeshFilter fovMeshFilter;
    private MeshRenderer fovMeshRenderer;

    [Header("Movement")]
    [SerializeField] private MoveMode moveMode = MoveMode.SpinInPlace;

    [Tooltip("RigidBody used for movement. If null, will GetComponent.")]
    [SerializeField] private Rigidbody2D rb;
    [Tooltip("Animator used for directional animations. If null, will GetComponent.")]
    [SerializeField] private Animator animator;

    [Header("Spin Settings")]
    [Tooltip("Seconds between 90° turns.")]
    [SerializeField] private float spinInterval = 1.0f;
    [SerializeField] private bool spinClockwise = true;
    private float spinTimer = 0f;
    [Tooltip("Small movement speed used to nudge the grunt in facing direction during spin. Should be > 0.1 to pass 0.01 sqrMagnitude threshold.")]
    [SerializeField] private float spinNudgeSpeed = 0.2f;
    [Tooltip("Duration of the nudge movement after each 90° step.")]
    [SerializeField] private float spinNudgeDuration = 0.1f;
    private float spinNudgeTimer = 0f;

    [Header("Patrol Line Settings")]
    [Tooltip("Start point of patrol.")]
    [SerializeField] private Transform pointA;
    [Tooltip("End point of patrol.")]
    [SerializeField] private Transform pointB;
    [SerializeField] private float patrolSpeed = 2f;
    [SerializeField] private float waitAtEnds = 0.25f;
    [SerializeField] private bool startAtA = true;
    private int patrolDir = 1; // 1: A->B, -1: B->A
    private float waitTimer = 0f;

    // Animation state
    private Vector2 currentMove;
    private float lastAngle;
    [Tooltip("When spinning in place, also update IdleAngle to face the new direction on each 90° step.")]
    [SerializeField] private bool updateIdleAngleOnSpin = true;

    private void Start()
    {
        if (player == null && autoFindPlayerByTag)
        {
            GameObject p = GameObject.FindGameObjectWithTag("Player");
            if (p != null) player = p.transform;
        }

        if (eye == null) eye = transform;

        if (rb == null) rb = GetComponent<Rigidbody2D>();
        if (animator == null) animator = GetComponent<Animator>();

    // Prevent gravity drift in top-down
    if (rb != null) rb.gravityScale = 0f;

    // Initialize facing from current transform
    facingAngleDeg = transform.eulerAngles.z;

        // Patrol init
        if (moveMode == MoveMode.PatrolLine)
        {
            if (pointA != null && pointB != null)
            {
                transform.position = (startAtA ? pointA.position : pointB.position);
                patrolDir = startAtA ? 1 : -1;
            }
        }

        // Setup FOV mesh if enabled
        if (showFOV)
        {
            EnsureFOVObjects();
        }

        if (obstructionMask == 0)
        {
            Debug.LogWarning("GruntScript: Obstruction Mask is not set. LOS rays will ignore walls. Assign your wall/obstacle layers.", this);
        }
    }

    private void Update()
    {
    // Check line of sight every frame (only if player exists)
    if (player != null && HasLineOfSightToPlayer())
        {
            ReloadScene();
        }

        if (showFOV)
        {
            UpdateFOVMesh();
        }

        UpdateMovement(Time.deltaTime);
        UpdateAnimator();
    }

    private bool HasLineOfSightToPlayer()
    {
        Vector2 origin = eye != null ? (Vector2)eye.position : (Vector2)transform.position;
        Vector2 toPlayer = (Vector2)player.position - origin;
        float distanceToPlayer = toPlayer.magnitude;

        // Distance check
        if (distanceToPlayer > viewDistance)
        {
            lastHasLOS = false;
            return false;
        }

        // Cone/angle check
        float angle = Vector2.Angle(Forward, toPlayer);
        if (angle > viewAngle * 0.5f)
        {
            lastHasLOS = false;
            return false;
        }

        // Raycast: collect hits and choose nearest; if nearest is obstruction, LOS is blocked
        Vector2 dir = toPlayer.normalized;
        var filter = BuildVisionFilter();
        int hitCount = Physics2D.Raycast(origin, dir, filter, s_Hits, viewDistance);
        if (hitCount > 0)
        {
            int nearestIdx = 0;
            float nearestDist = s_Hits[0].distance;
            for (int i = 1; i < hitCount; i++)
            {
                if (s_Hits[i].distance < nearestDist)
                {
                    nearestDist = s_Hits[i].distance;
                    nearestIdx = i;
                }
            }

            var first = s_Hits[nearestIdx];
            if (debugDrawLOS)
            {
                Debug.DrawLine(origin, origin + dir * nearestDist, Color.magenta, 0f, false);
            }
            bool hitIsPlayerLayer = IsLayerInMask(first.collider.gameObject.layer, playerMask);
            bool hitIsPlayerTag = first.collider.CompareTag("Player");
            lastHasLOS = hitIsPlayerLayer || hitIsPlayerTag;
            return lastHasLOS;
        }
        else if (debugDrawLOS)
        {
            Debug.DrawLine(origin, origin + dir * viewDistance, Color.gray, 0f, false);
        }

        lastHasLOS = false;
        return false;
    }

    private void ReloadScene()
    {
        // Reload current active scene
        Scene active = SceneManager.GetActiveScene();
        SceneManager.LoadScene(active.buildIndex);
    }

    // Touch = fail
    private void OnTriggerEnter2D(Collider2D other)
    {
        if (other.CompareTag("Player"))
        {
            ReloadScene();
        }
    }

    private void OnCollisionEnter2D(Collision2D collision)
    {
        if (collision.collider.CompareTag("Player"))
        {
            ReloadScene();
        }
    }

    private void OnDrawGizmosSelected()
    {
        // Visualize FOV
        Transform eyeT = eye != null ? eye : transform;
        Vector3 origin = eyeT.position;
    // Use runtime Forward if possible to match decoupled facing; fallback to transform
    Vector2 fwd = Forward;

        float half = viewAngle * 0.5f;
        Vector2 left = Quaternion.Euler(0, 0, half) * fwd;
        Vector2 right = Quaternion.Euler(0, 0, -half) * fwd;

        Gizmos.color = Color.yellow;
        Gizmos.DrawLine(origin, origin + (Vector3)(left.normalized * viewDistance));
        Gizmos.DrawLine(origin, origin + (Vector3)(right.normalized * viewDistance));
        Gizmos.DrawWireSphere(origin, 0.05f);

        // LOS ray (debug)
        if (player != null)
        {
            Gizmos.color = lastHasLOS ? Color.red : Color.green;
            Vector2 toPlayer = (Vector2)player.position - (Vector2)origin;
            if (toPlayer.sqrMagnitude > 0.0001f)
            {
                Vector2 dir = toPlayer.normalized;
                Gizmos.DrawLine(origin, origin + (Vector3)(dir * Mathf.Min(toPlayer.magnitude, viewDistance)));
            }
        }

        // Patrol gizmos
        if (pointA != null && pointB != null)
        {
            Gizmos.color = Color.cyan;
            Gizmos.DrawSphere(pointA.position, 0.07f);
            Gizmos.DrawSphere(pointB.position, 0.07f);
            Gizmos.DrawLine(pointA.position, pointB.position);
        }
    }

    #region FOV Visual Mesh
    private void EnsureFOVObjects()
    {
        if (fovGO == null)
        {
            fovGO = new GameObject("FOV");
            fovGO.transform.SetParent(eye != null ? eye : transform, false);
            fovGO.transform.localPosition = Vector3.zero;
            fovGO.transform.localRotation = Quaternion.identity;
            fovGO.transform.localScale = Vector3.one;

            fovMeshFilter = fovGO.AddComponent<MeshFilter>();
            fovMeshRenderer = fovGO.AddComponent<MeshRenderer>();
            fovMesh = new Mesh { name = "FOV Mesh" };
            fovMesh.MarkDynamic();
            fovMeshFilter.sharedMesh = fovMesh;

            // Ensure we have a material
            if (fovMaterial == null)
            {
                Shader shader = Shader.Find("Sprites/Default");
                if (shader == null) shader = Shader.Find("Unlit/Color");
                if (shader == null) shader = Shader.Find("Universal Render Pipeline/Unlit");
                fovMaterial = new Material(shader);
                // Try to disable depth so it overlays nicely in 2D
                fovMaterial.SetInt("_ZWrite", 0);
                fovMaterial.renderQueue = 3000; // Transparent
                if (fovMaterial.HasProperty("_Surface"))
                {
                    // URP Unlit support
                    fovMaterial.SetFloat("_Surface", 1); // 1=Transparent
                }
            }
            fovMeshRenderer.sharedMaterial = fovMaterial;
            // Set color if supported
            if (fovMeshRenderer.sharedMaterial.HasProperty("_Color"))
            {
                fovMeshRenderer.sharedMaterial.color = fovColor;
            }
            // Sorting
            fovMeshRenderer.sortingLayerName = fovSortingLayerName;
            fovMeshRenderer.sortingOrder = fovSortingOrder;
        }
    }

    private void UpdateFOVMesh()
    {
        if (fovGO == null) EnsureFOVObjects();
        if (fovMesh == null) return;

        // Origin in local space of FOV object
        Vector3 origin = Vector3.zero;
        Vector2 fwd = Forward.normalized;
        float half = viewAngle * 0.5f;
        int count = Mathf.Max(6, fovRayCount);
        float step = viewAngle / (count - 1);

        // Prepare arrays
        int vertexCount = count + 1; // center + arc points
        if (fovMesh.vertexCount != vertexCount)
        {
            fovMesh.Clear();
        }

        var vertices = new Vector3[vertexCount];
        var uvs = new Vector2[vertexCount];
        var triangles = new int[(count - 1) * 3];

        vertices[0] = origin;
        uvs[0] = Vector2.zero;

    Vector2 originWorld = (Vector2)(eye != null ? eye.position : transform.position);
    var filter = BuildVisionFilter();

        for (int i = 0; i < count; i++)
        {
            float angleDeg = -half + (i * step);
            Vector2 dir = (Vector2)(Quaternion.Euler(0, 0, angleDeg) * fwd);

            float dist = viewDistance;
            int hitCount = Physics2D.Raycast(originWorld, dir, filter, s_Hits, viewDistance);
            if (hitCount > 0)
            {
                int nearestIdx = 0;
                float nearestDist = s_Hits[0].distance;
                for (int h = 1; h < hitCount; h++)
                {
                    if (s_Hits[h].distance < nearestDist)
                    {
                        nearestDist = s_Hits[h].distance;
                        nearestIdx = h;
                    }
                }
                var first = s_Hits[nearestIdx];
                bool hitIsPlayerLayer = IsLayerInMask(first.collider.gameObject.layer, playerMask);
                bool hitIsPlayerTag = first.collider.CompareTag("Player");
                if (!(hitIsPlayerLayer || hitIsPlayerTag))
                {
                    dist = first.distance; // clipped by obstruction
                }
                else
                {
                    dist = Mathf.Min(first.distance, viewDistance);
                }
            }

            Vector2 pointLocal = dir.normalized * dist; // local since fovGO follows eye
            vertices[i + 1] = new Vector3(pointLocal.x, pointLocal.y, 0f);
            uvs[i + 1] = new Vector2((float)i / (count - 1), 1f);

            if (i < count - 1)
            {
                int triIndex = i * 3;
                triangles[triIndex + 0] = 0;
                triangles[triIndex + 1] = i + 1;
                triangles[triIndex + 2] = i + 2;
            }
        }

        fovMesh.vertices = vertices;
        fovMesh.uv = uvs;
        fovMesh.triangles = triangles;
        fovMesh.RecalculateBounds();
        // No normals/tangents needed for unlit
    }
    #endregion

    #region Movement Logic
    private void UpdateMovement(float dt)
    {
        currentMove = Vector2.zero;

        switch (moveMode)
        {
            case MoveMode.None:
                if (rb) rb.velocity = Vector2.zero;
                // Keep facing as-is; lastAngle not updated to match player logic
                break;

            case MoveMode.SpinInPlace:
                // Advance spin steps as 90° facing changes without rotating sprite, and nudge movement
                spinTimer += dt;
                if (spinTimer >= spinInterval)
                {
                    spinTimer -= spinInterval;
                    float delta = spinClockwise ? -90f : 90f;
                    if (decoupleFacingFromRotation)
                    {
                        facingAngleDeg = Mathf.Round((facingAngleDeg + delta) / 90f) * 90f;
                    }
                    else
                    {
                        Vector3 e = transform.eulerAngles;
                        e.z = Mathf.Round((e.z + delta) / 90f) * 90f;
                        transform.eulerAngles = e;
                        facingAngleDeg = transform.eulerAngles.z;
                    }
                    // Ensure IdleAngle reflects new facing even if movement is tiny
                    if (updateIdleAngleOnSpin)
                    {
                        Vector2 f = Forward.normalized;
                        lastAngle = Mathf.Atan2(f.x, f.y) * Mathf.Rad2Deg;
                    }
                    // start nudge
                    spinNudgeTimer = spinNudgeDuration;
                }

                if (spinNudgeTimer > 0f)
                {
                    spinNudgeTimer -= dt;
                    // Snap to perfect cardinal so Animator doesn't drift to adjacent blend
                    Vector2 dir = SnapToCardinal(Forward);
                    if (rb) rb.velocity = dir * spinNudgeSpeed;
                    currentMove = dir;
                }
                else
                {
                    if (rb) rb.velocity = Vector2.zero;
                    currentMove = Vector2.zero;
                }
                break;

            case MoveMode.PatrolLine:
                if (pointA == null || pointB == null)
                {
                    if (rb) rb.velocity = Vector2.zero;
                    return;
                }

                Vector2 from = patrolDir == 1 ? (Vector2)pointA.position : (Vector2)pointB.position;
                Vector2 to = patrolDir == 1 ? (Vector2)pointB.position : (Vector2)pointA.position;
                Vector2 pos = transform.position;
                Vector2 toTarget = to - pos;
                float dist = toTarget.magnitude;

                float arriveThreshold = 0.02f;
                if (dist <= arriveThreshold)
                {
                    if (rb) rb.velocity = Vector2.zero;
                    waitTimer += dt;
                    if (waitTimer >= waitAtEnds)
                    {
                        waitTimer = 0f;
                        patrolDir *= -1; // flip direction
                    }
                    // Face next direction

                    // Do not change lastAngle (no movement) to mirror player logic
                }
                else
                {
                    Vector2 dir = toTarget.normalized;
                    if (rb) rb.velocity = dir * patrolSpeed;
                    currentMove = dir;
                    // Face movement direction
                    FaceDirection(dir);
                    // Match player Movement.cs: angle from moveVector.x, moveVector.y
                    lastAngle = Mathf.Atan2(currentMove.x, currentMove.y) * Mathf.Rad2Deg;
                }
                break;
        }
    }

    private void FaceDirection(Vector2 dir)
    {
        if (dir.sqrMagnitude < 0.0001f) return;
        // Rotate facing so that Forward aligns with dir
        float angle = Mathf.Atan2(dir.y, dir.x) * Mathf.Rad2Deg; // angle for right-vector
        if (useUpAsForward) angle -= 90f; // so up points to dir

        if (decoupleFacingFromRotation)
        {
            facingAngleDeg = angle;
        }
        else
        {
            Vector3 e = transform.eulerAngles;
            e.z = angle;
            transform.eulerAngles = e;
            facingAngleDeg = transform.eulerAngles.z;
        }
    }

    private void UpdateAnimator()
    {
        if (animator == null) return;
        // Mirror Movement.cs behavior
        Vector2 moveVector = currentMove; // AI-determined direction
        Vector2 vel = rb ? rb.velocity : Vector2.zero;

        animator.SetFloat("Horizontal", moveVector.x);
        animator.SetFloat("Vertical", moveVector.y);
        animator.SetFloat("Speed", vel.sqrMagnitude);

        // Update lastAngle only while moving (like player script)
        if (vel.sqrMagnitude > 0.01f)
        {
            float angle = Mathf.Atan2(moveVector.x, moveVector.y) * Mathf.Rad2Deg;
            lastAngle = angle;
        }
        animator.SetFloat("IdleAngle", lastAngle);
    }
    #endregion

    #region Raycast Utils
    private static readonly RaycastHit2D[] s_Hits = new RaycastHit2D[16];

    private ContactFilter2D BuildVisionFilter()
    {
        ContactFilter2D filter = new ContactFilter2D();
        filter.useLayerMask = true;
        filter.SetLayerMask(obstructionMask | playerMask);
        filter.useTriggers = triggersBlockVision;
        return filter;
    }

    private static bool IsLayerInMask(int layer, LayerMask mask)
    {
        return (mask.value & (1 << layer)) != 0;
    }
    #endregion

    private static Vector2 SnapToCardinal(Vector2 v)
    {
        if (v.sqrMagnitude < 1e-6f) return Vector2.zero;
        if (Mathf.Abs(v.x) > Mathf.Abs(v.y))
        {
            return new Vector2(Mathf.Sign(v.x), 0f);
        }
        else
        {
            return new Vector2(0f, Mathf.Sign(v.y));
        }
    }
}
