using UnityEngine;

public class LatticeDeformer : MonoBehaviour
{
    [Header("Efecto Squash & Stretch")]
    public float squashAmount = 0.55f;
    public float stretchAmount = 1.25f;
    public float recoverySpeed = 6f;
    public int bounceCount = 2;

    private Mesh mesh;
    private Vector3[] originalVerts;
    private Vector3[] deformedVerts;
    private bool isAnimating = false;
    private float elapsedTime = 0f;
    private float animDuration = 0f;

    void Start()
    {
        mesh = GetComponent<MeshFilter>().mesh;
        originalVerts = mesh.vertices;
        deformedVerts = new Vector3[originalVerts.Length];
        System.Array.Copy(originalVerts, deformedVerts, originalVerts.Length);
    }

    void Update()
    {
        if (!isAnimating) return;
        elapsedTime += Time.deltaTime;
        float t = elapsedTime / animDuration;
        float scaleY = EvaluateBounce(t, squashAmount, stretchAmount, bounceCount);
        ApplyLatticeDeform(scaleY);
        if (t >= 1f)
        {
            isAnimating = false;
            RestoreMesh();
        }
    }

    public void TriggerHit(float duration = 0.45f)
    {
        elapsedTime = 0f;
        animDuration = duration;
        isAnimating = true;
    }

    void ApplyLatticeDeform(float scaleY)
    {
        Bounds bounds = mesh.bounds;
        float centerY = bounds.center.y;
        float halfH = bounds.extents.y;
        for (int i = 0; i < originalVerts.Length; i++)
        {
            Vector3 v = originalVerts[i];
            float normalizedY = (v.y - (centerY - halfH)) / (halfH * 2f);
            float newY = (centerY - halfH) + normalizedY * halfH * 2f * scaleY;
            float compensationXZ = 1f + (1f - scaleY) * 0.4f;
            deformedVerts[i] = new Vector3(v.x * compensationXZ, newY, v.z * compensationXZ);
        }
        mesh.vertices = deformedVerts;
        mesh.RecalculateNormals();
        mesh.RecalculateBounds();
    }

    float EvaluateBounce(float t, float squash, float stretch, int bounces)
    {
        if (t < 0.2f) return Mathf.Lerp(1f, squash, t / 0.2f);
        else if (t < 0.4f) return Mathf.Lerp(squash, stretch, (t - 0.2f) / 0.2f);
        else
        {
            float phase = (t - 0.4f) / 0.6f;
            float dampedBounce = Mathf.Sin(phase * Mathf.PI * bounces) * (1f - phase);
            return 1f + dampedBounce * (stretch - 1f) * 0.4f;
        }
    }

    void RestoreMesh()
    {
        mesh.vertices = originalVerts;
        mesh.RecalculateNormals();
        mesh.RecalculateBounds();
    }

    void OnDestroy()
    {
        if (mesh != null) RestoreMesh();
    }
}