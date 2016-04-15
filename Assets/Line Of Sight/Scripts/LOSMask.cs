using System;
using System.Collections.Generic;
using UnityEngine;

namespace LOS
{
    public enum LOSQualityLevel { Low, Medium, High }

    [ExecuteInEditMode]
    [RequireComponent(typeof(Camera))]
    [AddComponentMenu("Line of Sight/LOS Mask")]
    public class LOSMask : MonoBehaviour
    {
        #region Exposed Data Members

        [Tooltip("Reference to the LOS Buffer Storage component used by this Mask")]
        [SerializeField]
        private LOSBufferStorage m_BufferStorage;

        [Tooltip("Level of quality")]
        [SerializeField]
        private LOSQualityLevel m_QualityLevel = LOSQualityLevel.Medium;

        [Tooltip("Global switch to disable all blur, overrides other blur settings")]
        [SerializeField]
        private bool m_DisableBlur = false;

        [Tooltip("Enable HDR rendering of this mask")]
        [SerializeField]
        private bool m_HDRMask = true;

        #endregion Exposed Data Members

        #region Public Properties

        public LOSQualityLevel QualityLevel
        {
            get { return m_QualityLevel; }
            set { m_QualityLevel = value; }
        }

        public bool DisableBlur
        {
            get { return m_DisableBlur; }
            set { m_DisableBlur = value; }
        }

        public static Plane[] CameraFrustumPlanes
        {
            get { return m_CameraFrustumPlanes; }
        }

        #endregion Public Properties

        #region Private Data Members

        private Dictionary<int, Cubemap> m_CubeMaps = new Dictionary<int, Cubemap>();

        private static Plane[] m_CameraFrustumPlanes = new Plane[6];

        private Camera m_Camera;

        #endregion Private Data Members

        #region MonoBehaviour Functions

        private void Awake()
        {
            // Get and verify camera component
            m_Camera = GetComponent<Camera>();
        }

        private void OnEnable()
        {
            // Check if this component can be enabled.
            // Disable if post processing not supported.
            enabled &= Assert.Verify(SystemInfo.supportsImageEffects, "Image effects not supported.");
            enabled &= Assert.Verify(SystemInfo.supportsRenderTextures, "Render textures not supported.");

            // Disable if buffer storage is not assigned.
            enabled &= Assert.Verify(m_BufferStorage != null, "LOS Buffer Storage property not assigned.");

            // Disable if camera component is missing.
            enabled &= Assert.Verify(m_Camera != null, "Camera component missing.");

            if (enabled)
            {
                // Make sure Frustum planes are initiliazed.
                LOSHelper.ExtractFrustumPlanes(m_CameraFrustumPlanes, m_Camera);
            }
        }

        private void OnDisable()
        {
            // Remove all cubemaps from dictionary.
            m_CubeMaps.Clear();

            // Destroy Resources.
            Materials.DestroyResources();
            Shaders.DestroyResources();
        }

        private void OnPreRender()
        {
            if (m_Camera == null) return;

            // Make sure we can acces the cameras depth buffer in our shader.
            m_Camera.depthTextureMode |= DepthTextureMode.Depth;

            // Update Mask Camera frutsum planes if needed.
            if (transform.hasChanged)
            {
                LOSHelper.ExtractFrustumPlanes(m_CameraFrustumPlanes, m_Camera);
                transform.hasChanged = false;
            }
        }

        private void OnRenderImage(RenderTexture source, RenderTexture destination)
        {
            if (m_Camera == null) return;

            // Calculate Frustum origins and rays for mask camera.
            Matrix4x4 frustumOrigins;
            Matrix4x4 frustumRays;
            LOSHelper.CalculateViewVectors(m_Camera, out frustumRays, out frustumOrigins);

            // Push parameters which are identical for all LOS sources.
            Materials.Mask.SetMatrix(ShaderID.FrustumRays, frustumRays);
            Materials.Mask.SetMatrix(ShaderID.FrustumOrigins, frustumOrigins);

            // Store original skybox.
            Material originalSkybox = RenderSettings.skybox;
            // Set-up skybox material which clears render texture to farplane depth.
            RenderSettings.skybox = Materials.SkyBox;

            // Create Mask Render Texture.
            RenderTexture maskRenderTexture = CreateMaskRenderTexture();

            // Get list with all LOS sources.
            List<LOSSource> losSources = LOSManager.Instance.LOSSources;
            // Iterate over all LOS sources.
            for (int i = 0; i < losSources.Count; i++)
            {
                if (losSources[i].IsVisible)
                    RenderSourceToMask(losSources[i], ref maskRenderTexture);
            }

            // Render all Cube Sources.
            // Get list with all LOS cube sources.
            List<LOSSourceCube> losSourcesCube = LOSManager.Instance.LOSSourcesCube;
            // Iterate over all LOS cube sources.
            for (int i = 0; i < losSourcesCube.Count; i++)
            {
                if (losSourcesCube[i].IsVisible)
                    RenderSourceCubeToMask(losSourcesCube[i], ref maskRenderTexture);
            }

            // Revert original skybox.
            RenderSettings.skybox = originalSkybox;

            // Get unmodified screen buffer.
            if (m_BufferStorage)
                Materials.Combine.SetTexture(ShaderID.PreEffectTex, m_BufferStorage.BufferTexture);

            // Set-up material.
            Materials.Combine.SetTexture(ShaderID.MaskTex, maskRenderTexture);

            // Render final effect.
            Graphics.Blit(source, destination, Materials.Combine);

            RenderTexture.ReleaseTemporary(maskRenderTexture);
        }

        #endregion MonoBehaviour Functions

        #region Private Functions

        /// <summary>
        /// Updates mask for specific LOS source.
        /// </summary>
        private void RenderSourceToMask(LOSSource losSource, ref RenderTexture maskRenderTexture)
        {
            Camera sourceCamera = losSource.SourceCamera;

            if (sourceCamera == null) return;

            // Set "skybox" material farplane.
            Materials.SkyBox.SetVector(ShaderID.FarPlane, new Vector4(sourceCamera.farClipPlane, sourceCamera.farClipPlane, sourceCamera.farClipPlane, sourceCamera.farClipPlane));

            // Source depth texture resolution.
            int sourceBufferWidth = CalculateRTSize(losSource.RenderTargetWidth, m_QualityLevel);
            int sourceBufferHeight = CalculateRTSize(losSource.RenderTargetHeight, m_QualityLevel);

            // Create temporary rendertexture for rendering linear depth.
            RenderTexture sourceBuffer = RenderTexture.GetTemporary(sourceBufferWidth, sourceBufferHeight, 16, RenderTextureFormat.RGFloat);
            sourceBuffer.filterMode = FilterMode.Trilinear;
            sourceBuffer.wrapMode = TextureWrapMode.Clamp;

            // Set camera render target.
            sourceCamera.targetTexture = sourceBuffer;

            // Render depth from source Camera.
            sourceCamera.RenderWithShader(Shaders.Depth, null);

            // Blur buffer if needed.
            if (losSource.Blur && !m_DisableBlur)
                BlurTexture(sourceBuffer, Materials.Blur, losSource.BlurIterations, losSource.BlurSize);

            //Push LOS source specific parameters.
            Materials.Mask.SetTexture(ShaderID.SourceDepthTex, sourceBuffer);
            Materials.Mask.SetMatrix(ShaderID.SourceWorldProj, sourceCamera.projectionMatrix * sourceCamera.worldToCameraMatrix);
            Materials.Mask.SetVector(ShaderID.SourceInfo, losSource.SourceInfo);
            Materials.Mask.SetVector(ShaderID.Settings, new Vector4(losSource.DistanceFade, losSource.EdgeFade, losSource.MinVariance, losSource.MaskInvert ? 1.0f : 0.0f));
            Materials.Mask.SetVector(ShaderID.Flags, new Vector4(PixelOperation.Clamp == losSource.OutOfBoundArea ? 0.0f : 1.0f, PixelOperation.Exclude == losSource.OutOfBoundArea ? -1.0f : 1.0f, 0, 0));
            Materials.Mask.SetColor(ShaderID.ColorMask, losSource.MaskColor * losSource.MaskIntensity);

            // Set Correct material pass.
            Materials.Mask.SetPass(m_Camera.orthographic ? 1 : 0);

            // Render Mask.
            IndexedGraphicsBlit(maskRenderTexture);

            // Release linear depth render texture.
            RenderTexture.ReleaseTemporary(sourceBuffer);
        }

        /// <summary>
        /// Updates mask for specific LOS cube source.
        /// </summary>
        private void RenderSourceCubeToMask(LOSSourceCube losSource, ref RenderTexture maskRenderTexture)
        {
            // Calculate cube map size.
            int cubeMapSize = CalculateRTSize(losSource.CubeMapResolution, m_QualityLevel);

            // Return if cube map size is to small.
            if (cubeMapSize <= 0) return;

            // Get cube map render texture.
            Cubemap sourceBufferCube = CreateCubeMap(cubeMapSize);

            // Get camera.
            Camera cubeCamera = losSource.SourceCamera;
            Materials.SkyBox.SetVector(ShaderID.FarPlane, new Vector4(cubeCamera.farClipPlane, cubeCamera.farClipPlane, cubeCamera.farClipPlane, cubeCamera.farClipPlane));

            // Set replacement shader for rendering.
            cubeCamera.SetReplacementShader(Shaders.DepthRGBA, null);

            // Render encoded depth to cube map.
            cubeCamera.RenderToCubemap(sourceBufferCube);

            // Reset replamcent shader.
            cubeCamera.ResetReplacementShader();

            //Push LOS cube source specific parameters.
            Materials.Mask.SetTexture(ShaderID.SourceDepthCube, sourceBufferCube);
            Materials.Mask.SetVector(ShaderID.SourceInfo, losSource.SourceInfo);
            Materials.Mask.SetVector(ShaderID.Settings, new Vector4(losSource.DistanceFade, 0, losSource.MinVariance, losSource.MaskInvert ? 1.0f : 0.0f));
            Materials.Mask.SetColor(ShaderID.ColorMask, losSource.MaskColor * losSource.MaskIntensity);

            // Set Correct material pass.
            Materials.Mask.SetPass(m_Camera.orthographic ? 3 : 2);

            //Render mask with correct pass.
            IndexedGraphicsBlit(maskRenderTexture);
        }

        /// <summary>
        /// Creates a temporary render texture used for rendering the mask
        /// </summary>
        private RenderTexture CreateMaskRenderTexture()
        {
            // Mask RenderTexture settings.
            int maskDownSampler = LOSQualityLevel.Medium > m_QualityLevel ? 1 : 0;
            int maskBufferWidth = Screen.width >> maskDownSampler;
            int maskBufferHeight = Screen.height >> maskDownSampler;

            RenderTextureFormat maskTextureFormat = m_HDRMask ? RenderTextureFormat.ARGBHalf : RenderTextureFormat.ARGB32;
            FilterMode maskFilterMode = FilterMode.Bilinear;

            // Get mask temporary render texture.
            RenderTexture maskRenderTexture = RenderTexture.GetTemporary(maskBufferWidth, maskBufferHeight, 0, maskTextureFormat);
            maskRenderTexture.filterMode = maskFilterMode;

            // Clear mask rendertexture to black.
            RenderTexture.active = maskRenderTexture;
            GL.Clear(true, true, new Color(0, 0, 0, 0));

            return maskRenderTexture;
        }

        /// <summary>
        /// Get or creates a cubemap used for rendering the LOS Source Cube components depth map
        /// </summary>
        private Cubemap CreateCubeMap(int size)
        {
            Cubemap cubemap = null;

            if (!m_CubeMaps.TryGetValue(size, out cubemap))
            {
                cubemap = new Cubemap(size, TextureFormat.ARGB32, false);
                cubemap.filterMode = FilterMode.Trilinear;

                m_CubeMaps.Add(size, cubemap);
            }

            return cubemap;
        }

        /// <summary>
        /// Blurs the render texture using a seperable blur.
        /// </summary>
        private void BlurTexture(RenderTexture source, Material blurMaterial, int iterations, float size)
        {
            // Store RT filter mode.
            FilterMode originalFilterMode = source.filterMode;
            // Set to trilinear.
            source.filterMode = FilterMode.Trilinear;

            for (int i = 0; i < iterations; ++i)
            {
                float iterationOffset = i;
                blurMaterial.SetVector(ShaderID.Settings, new Vector4(size + iterationOffset, -size - iterationOffset, 0.0f, 0.0f));

                // Create temporary buffer.
                RenderTexture buffer = RenderTexture.GetTemporary(source.width, source.height, source.depth, source.format);
                buffer.filterMode = source.filterMode;
                buffer.wrapMode = source.wrapMode;

                Graphics.Blit(source, buffer, blurMaterial, 0);
                Graphics.Blit(buffer, source, blurMaterial, 1);

                // Release temporary buffer.
                RenderTexture.ReleaseTemporary(buffer);
            }
            // Revert RT filter mode.
            source.filterMode = originalFilterMode;
        }

        /// <summary>
        /// Renders quad to full screen with index for interpolating frustum corners
        /// </summary>
        private void IndexedGraphicsBlit(RenderTexture destination)
        {
            RenderTexture.active = destination;

            GL.PushMatrix();
            GL.LoadOrtho();

            GL.Begin(GL.QUADS);

            // Bottom left corner.
            GL.MultiTexCoord2(0, 0.0f, 0.0f);
            GL.Vertex3(0.0f, 0.0f, 3.0f);

            // Bottom right corner.
            GL.MultiTexCoord2(0, 1.0f, 0.0f);
            GL.Vertex3(1.0f, 0.0f, 2.0f);

            // Top right corner.
            GL.MultiTexCoord2(0, 1.0f, 1.0f);
            GL.Vertex3(1.0f, 1.0f, 1.0f);

            // Top left corner.
            GL.MultiTexCoord2(0, 0.0f, 1.0f);
            GL.Vertex3(0.0f, 1.0f, 0.0f);

            GL.End();
            GL.PopMatrix();

            RenderTexture.active = null;
        }

        #endregion Private Functions

        #region Static Functions

        /// <summary>
        /// Calculates the size of the Render texture according to the quality setting
        /// </summary>
        private static int CalculateRTSize(int size, LOSQualityLevel level)
        {
            const int maxTextureSize = 4096;
            int finalSize = size;

            if (level > LOSQualityLevel.Medium)
            {
                finalSize *= 2;

                Assert.Test(finalSize <= maxTextureSize, "Render texture size to big, can't be larger than " + maxTextureSize);
                //Make sure size is not bigger than max texture size
                Math.Min(finalSize, maxTextureSize);
            }
            else if (level < LOSQualityLevel.Medium)
            {
                finalSize = finalSize >> 1;
            }

            return finalSize;
        }

        #endregion Static Functions
    }
}