using Fusion;
using UnityEngine;

/// <summary>
/// Blob shadow dibujada con GL (sin Quad, sin material, sin shader issues).
/// Funciona en cualquier versión de Unity y URP.
/// SETUP: solo adjuntar al objeto raíz del player. Nada más.
/// </summary>
public class BlobShadow : NetworkBehaviour
{
    [Header("Raycast")]
    [SerializeField] private LayerMask groundLayers = ~0;
    [SerializeField] private float maxRayDistance = 20f;
    [SerializeField] private float groundOffset = 0.03f;

    [Header("Tamaño")]
    [SerializeField] private float baseRadius = 0.45f;   // Radio en el suelo
    [SerializeField] private float minRadius  = 0.08f;   // Radio en el punto más alto

    [Header("Transparencia")]
    [SerializeField] private float baseAlpha = 0.45f;    // Opacidad en el suelo
    [SerializeField] private float minAlpha  = 0.0f;     // Opacidad en altura máxima

    [Header("Calidad del círculo")]
    [SerializeField] private int rings     = 6;          // Anillos concéntricos del degradado
    [SerializeField] private int segments  = 32;         // Segmentos por anillo (más = más redondo)

    // Estado calculado cada frame en LateUpdate, consumido en OnRenderObject
    private bool  _visible;
    private Vector3 _groundPoint;
    private Vector3 _groundNormal;
    private float _radius;
    private float _alpha;

    private Material _glMat;

    public override void Spawned()
    {
        if (!HasInputAuthority)
        {
            enabled = false;
            return;
        }

        // Material mínimo para GL — solo necesita ZWrite off y blending
        _glMat = new Material(Shader.Find("Hidden/Internal-Colored"));
        _glMat.hideFlags = HideFlags.HideAndDontSave;
        _glMat.SetInt("_SrcBlend",  (int)UnityEngine.Rendering.BlendMode.SrcAlpha);
        _glMat.SetInt("_DstBlend",  (int)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
        _glMat.SetInt("_Cull",      (int)UnityEngine.Rendering.CullMode.Off);
        _glMat.SetInt("_ZWrite",    0);
        _glMat.SetInt("_ZTest",     (int)UnityEngine.Rendering.CompareFunction.LessEqual);
    }

    private void LateUpdate()
    {
        Vector3 origin = transform.position + Vector3.up * 0.1f;

        if (Physics.Raycast(origin, Vector3.down, out RaycastHit hit, maxRayDistance, groundLayers))
        {
            float height = hit.distance;
            float t      = Mathf.Clamp01(height / maxRayDistance);

            _visible      = true;
            _groundPoint  = hit.point + hit.normal * groundOffset;
            _groundNormal = hit.normal;
            _radius       = Mathf.Lerp(baseRadius, minRadius,  Mathf.Pow(t, 0.5f)); // cae rápido
            _alpha        = Mathf.Lerp(baseAlpha,  minAlpha,   t * t);               // cae aún más rápido
        }
        else
        {
            _visible = false;
        }
    }

    // OnRenderObject se llama después de que la cámara renderiza la escena
    private void OnRenderObject()
    {
        if (!_visible || _glMat == null) return;

        // Base ortonormal del plano del suelo (para dibujar el círculo alineado con la normal)
        Vector3 up      = _groundNormal.normalized;
        Vector3 right   = Vector3.Cross(up, Vector3.forward);
        if (right.sqrMagnitude < 0.001f)
            right = Vector3.Cross(up, Vector3.right);
        right.Normalize();
        Vector3 forward = Vector3.Cross(right, up).normalized;

        _glMat.SetPass(0);
        GL.PushMatrix();
        GL.MultMatrix(Matrix4x4.identity);

        GL.Begin(GL.TRIANGLES);

        float angleStep = 2f * Mathf.PI / segments;

        for (int ring = 0; ring < rings; ring++)
        {
            // Fracción normalizada de cada anillo (0 = centro, 1 = borde)
            float t0 = (float) ring      / rings;
            float t1 = (float)(ring + 1) / rings;

            // Alpha con degradado suave tipo gaussiano
            float a0 = _alpha * Mathf.Pow(1f - t0, 2f);
            float a1 = _alpha * Mathf.Pow(1f - t1, 2f);

            float r0 = _radius * t0;
            float r1 = _radius * t1;

            for (int i = 0; i < segments; i++)
            {
                float angle0 = i       * angleStep;
                float angle1 = (i + 1) * angleStep;

                Vector3 dir0 = Mathf.Cos(angle0) * right + Mathf.Sin(angle0) * forward;
                Vector3 dir1 = Mathf.Cos(angle1) * right + Mathf.Sin(angle1) * forward;

                Vector3 p00 = _groundPoint + dir0 * r0;
                Vector3 p10 = _groundPoint + dir0 * r1;
                Vector3 p01 = _groundPoint + dir1 * r0;
                Vector3 p11 = _groundPoint + dir1 * r1;

                // Triángulo 1
                GL.Color(new Color(0f, 0f, 0f, a0)); GL.Vertex(p00);
                GL.Color(new Color(0f, 0f, 0f, a1)); GL.Vertex(p10);
                GL.Color(new Color(0f, 0f, 0f, a1)); GL.Vertex(p11);

                // Triángulo 2
                GL.Color(new Color(0f, 0f, 0f, a0)); GL.Vertex(p00);
                GL.Color(new Color(0f, 0f, 0f, a1)); GL.Vertex(p11);
                GL.Color(new Color(0f, 0f, 0f, a0)); GL.Vertex(p01);
            }
        }

        GL.End();
        GL.PopMatrix();
    }

    private void OnDestroy()
    {
        if (_glMat != null)
            Destroy(_glMat);
    }
}