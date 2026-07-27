using UnityEngine;

namespace LightIsEssentialForPhotosynthisis
{
    [System.Serializable]
    public struct IncomingRayData
    {
        public Transform startPoint;
        public Transform endPoint;
        public Color color;
    }

    public class IodineStarchLightInteraction : MonoBehaviour
    {
        [Header("Incoming Rays (manual placement)")]
        public IncomingRayData[] incomingRays;
        public Material lineMaterial;
        public float rayWidth = 0.04f;

        [Header("Reflection")]
        public Color reflectColor = Color.blue;
        public Transform reflectOutEndPoint;
        public float reflectWidth = 0.06f;
        [Range(0f, 1f)] public float colorMatchTolerance = 0.05f;

        [Header("Glow / Realism")]
        [Tooltip("Multiplies the base color to drive HDR emission — higher values glow brighter")]
        public float emissionIntensity = 3f;
        [Tooltip("Reflected ray can glow brighter than incoming rays for emphasis")]
        public float reflectEmissionIntensity = 5f;

        static readonly int BaseColorProperty = Shader.PropertyToID("_BaseColor");
        static readonly int EmissionColorProperty = Shader.PropertyToID("_EmissionColor");
        MaterialPropertyBlock _mpb;

        void Start()
        {
            _mpb = new MaterialPropertyBlock();

            for (int i = 0; i < incomingRays.Length; i++)
            {
                var ray = incomingRays[i];
                if (ray.startPoint == null || ray.endPoint == null) continue;

                CreateLine("IncomingRay_" + i, ray.startPoint.position, ray.endPoint.position,
                    ray.color, rayWidth, emissionIntensity);

                if (ColorsApproxEqual(ray.color, reflectColor) && reflectOutEndPoint != null)
                {
                    CreateLine("ReflectedOutgoingRay", ray.endPoint.position, reflectOutEndPoint.position,
                        ray.color, reflectWidth, reflectEmissionIntensity);
                }
            }
        }

        void CreateLine(string name, Vector3 start, Vector3 end, Color color, float width, float emissionStrength)
        {
            GameObject obj = new GameObject(name);
            obj.transform.parent = transform;

            LineRenderer lr = obj.AddComponent<LineRenderer>();
            lr.material = lineMaterial;
            lr.positionCount = 2;
            lr.startWidth = width;
            lr.endWidth = width;
            lr.SetPosition(0, start);
            lr.SetPosition(1, end);

            ApplyColor(lr, color, emissionStrength);
        }

        void ApplyColor(LineRenderer lr, Color color, float emissionStrength)
        {
            lr.GetPropertyBlock(_mpb);
            _mpb.SetColor(BaseColorProperty, color);

            // HDR emission color boosted beyond the base color for a glowing look
            Color emissionColor = color * emissionStrength;
            _mpb.SetColor(EmissionColorProperty, emissionColor);

            lr.SetPropertyBlock(_mpb);
        }

        bool ColorsApproxEqual(Color a, Color b)
        {
            return Mathf.Abs(a.r - b.r) < colorMatchTolerance &&
                   Mathf.Abs(a.g - b.g) < colorMatchTolerance &&
                   Mathf.Abs(a.b - b.b) < colorMatchTolerance;
        }
    }
}