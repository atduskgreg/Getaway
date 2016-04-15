using UnityEngine;

namespace LOS
{
    [RequireComponent(typeof(Camera))]
    [ExecuteInEditMode]
    [AddComponentMenu("Line of Sight/LOS Source Cube")]
    public class LOSSourceCube : MonoBehaviour, ILOSSource
    {
        #region Exposed Data Members

        [Tooltip("Color rendered into mask")]
        [SerializeField]
        private Color m_MaskColor = Color.white;

        [Tooltip("Intensity of color rendered into mask")]
        [Range(0f, 10f)]
        [SerializeField]
        private float m_MaskIntensity = 1.0f;

        [Tooltip("Inverts mask")]
        [SerializeField]
        private bool m_MaskInvert = false;

        [Tooltip("Resolution of cubemap render texture. Should be power of 2.")]
        [SerializeField]
        private int m_CubeMapResolution = 512;

        [Tooltip("Fade over distance amount")]
        [Range(0.0f, 1f)]
        [SerializeField]
        private float m_DistanceFade = 0.1f;

        [Tooltip("Reduces artifacts in mask, best kept as low as possible")]
        [Range(0.0f, 1f)]
        [SerializeField]
        private float m_MinVariance = 0.1f;

        #endregion Exposed Data Members

        #region Public Properties

        public Color MaskColor
        {
            get { return m_MaskColor; }
            set { m_MaskColor = value; }
        }

        public float MaskIntensity
        {
            get { return m_MaskIntensity; }
            set { m_MaskIntensity = value; }
        }

        public bool MaskInvert
        {
            get { return m_MaskInvert; }
            set { m_MaskInvert = value; }
        }

        public int CubeMapResolution
        {
            get { return LOSHelper.NearestPowerOfTwo(m_CubeMapResolution); }
            set { m_CubeMapResolution = LOSHelper.NearestPowerOfTwo(value); }
        }

        public float DistanceFade
        {
            get { return m_DistanceFade; }
            set { m_DistanceFade = Mathf.Clamp01(value); }
        }

        public float MinVariance
        {
            get { return m_MinVariance; }
            set { m_MinVariance = Mathf.Clamp01(value); }
        }

        public bool IsVisible
        {
            get { return m_IsVisibile; }
        }

        public Camera SourceCamera
        {
            get { return m_Camera; }
        }

        public Bounds CameraBounds
        {
            get { return m_CameraBounds; }
        }

        public GameObject GameObject
        {
            get { return gameObject; }
        }

        public Vector4 SourceInfo
        {
            get
            {
                Vector4 sourceInfo = Vector4.zero;

                if (m_Camera != null)
                {
                    sourceInfo = m_Camera.transform.position;
                    sourceInfo.w = m_Camera.farClipPlane;
                }

                return sourceInfo;
            }
        }

        #endregion Public Properties

        #region Private Data Members

        private Camera m_Camera;
        private Bounds m_CameraBounds;

        private bool m_IsVisibile = true;

        #endregion Private Data Members

        #region MonoBehaviour Functions

        private void Awake()
        {
            m_Camera = GetComponentInChildren<Camera>();
        }

        private void OnEnable()
        {
            Debug.LogWarning("LOS Source Cube has been deprecated, please use the 360 LOS Source High Precision prefab instead!");

            // Register with LOS manager singleton.
            LOSManager.Instance.AddLOSSourceCube(this);

            // Check if component can be enabled.
            enabled &= Assert.Verify(m_Camera != null, "Camera Component missing");

            if (enabled)
            {
                // Alert user if camera is set to orthographic
                Assert.Test(!m_Camera.orthographic, "Cameras attached to an LOS source cube can't be orthographic, changing to perspective!");

                // Initiliaze source camera
                LOSHelper.InitSourceCamera(m_Camera);

                // Initiliaze bounds.
                m_CameraBounds = CalculateSourceBounds();
            }
        }

        private void OnDisable()
        {
            // Unregister with LOS manager singleton.
            LOSManager.Instance.RemoveLOSSourceCube(this);
        }

        private void Update()
        {
            // Check if this source's transform has changed.
            if (transform.hasChanged)
            {
                // Recalculate camera bounds.
                m_CameraBounds = CalculateSourceBounds();

                // Reset transform changed flag.
                transform.hasChanged = false;
            }

            // Check if this source and it's frustum are visible to the main camera.
            m_IsVisibile = GeometryUtility.TestPlanesAABB(LOSMask.CameraFrustumPlanes, m_CameraBounds);
        }

        private void OnDrawGizmos()
        {
            // Draw gizmo in editor.
            DrawGizmo(false);
        }

        private void OnDrawGizmosSelected()
        {
            // Draw gizmo in editor.
            DrawGizmo(true);
        }

        #endregion MonoBehaviour Functions

        #region Private Functions

        /// <summary>
        /// Draws camera gizmo showing the correct frustum in editor scene.
        /// Draws sphere showing area covered by this source.
        /// </summary>
        private void DrawGizmo(bool selected)
        {
            if (m_Camera == null) return;

            Color gizmoColor = m_MaskColor;
            float sphereRadius = m_Camera.farClipPlane;

            Gizmos.matrix = transform.localToWorldMatrix;

            gizmoColor.a = selected ? 0.9f : 0.25f;
            Gizmos.color = gizmoColor;
            Gizmos.DrawWireSphere(Vector3.zero, sphereRadius);

            if (selected)
            {
                gizmoColor.a = 0.15f;
                Gizmos.color = gizmoColor;
                Gizmos.DrawSphere(Vector3.zero, sphereRadius);
            }
        }

        /// <summary>
        /// Calculate the bounds for this source.
        /// </summary>
        private Bounds CalculateSourceBounds()
        {
            float radius = m_Camera.farClipPlane;

            Bounds cubeBound = new Bounds();
            cubeBound.center = transform.position;
            cubeBound.extents = new Vector3(radius, radius, radius);

            return cubeBound;
        }

        #endregion Private Functions
    }
}