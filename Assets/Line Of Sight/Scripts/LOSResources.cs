using UnityEngine;

namespace LOS
{
    public class Materials
    {
        #region Constants

        // Paths to material shaders

        private const string MaskShaderPath = "Shaders/LOSMask";
        private const string CombineShaderPath = "Shaders/LOSCombine";
        private const string BlurShaderPath = "Shaders/LOSBlur";
        private const string SkyBoxShaderPath = "Shaders/LOSSkyBox";

        #endregion Constants

        #region Public Properties

        public static Material Mask
        {
            get
            {
                if (m_MaskMaterial == null)
                    m_MaskMaterial = CreateMaterial(MaskShaderPath);

                return m_MaskMaterial;
            }
        }

        public static Material Combine
        {
            get
            {
                if (m_CombineMaterial == null)
                    m_CombineMaterial = CreateMaterial(CombineShaderPath);

                return m_CombineMaterial;
            }
        }

        public static Material Blur
        {
            get
            {
                if (m_BlurMaterial == null)
                    m_BlurMaterial = CreateMaterial(BlurShaderPath);

                return m_BlurMaterial;
            }
        }

        public static Material SkyBox
        {
            get
            {
                if (m_SkyBoxMaterial == null)
                    m_SkyBoxMaterial = CreateMaterial(SkyBoxShaderPath);

                return m_SkyBoxMaterial;
            }
        }

        #endregion Public Properties

        #region Private Data members

        private static Material m_MaskMaterial;
        private static Material m_CombineMaterial;
        private static Material m_BlurMaterial;
        private static Material m_SkyBoxMaterial;

        #endregion Private Data members

        /// <summary>
        /// Destroys all resources
        /// </summary>
        public static void DestroyResources()
        {
            if (m_MaskMaterial != null)
            {
                Object.DestroyImmediate(m_MaskMaterial);
                m_MaskMaterial = null;
            }
            if (m_CombineMaterial != null)
            {
                Object.DestroyImmediate(m_CombineMaterial);
                m_CombineMaterial = null;
            }
            if (m_BlurMaterial != null)
            {
                Object.DestroyImmediate(m_BlurMaterial);
                m_BlurMaterial = null;
            }
            if (m_SkyBoxMaterial != null)
            {
                Object.DestroyImmediate(m_SkyBoxMaterial);
                m_SkyBoxMaterial = null;
            }
        }

        /// <summary>
        /// Creates and returns material from shader.
        /// </summary>
        private static Material CreateMaterial(string shaderResource)
        {
            Material material = null;

            Shader shader = Resources.Load(shaderResource, typeof(Shader)) as Shader;

            if (Assert.Verify(Shaders.CheckShader(shader)))
            {
                material = new Material(shader);
                material.hideFlags = HideFlags.HideAndDontSave;
            }

            Assert.Test(material != null, "Failed to created material from shader: " + shaderResource);

            return material;
        }
    }

    public class Shaders
    {
        #region Constants

        // Paths to shaders

        private const string DepthShaderPath = "Shaders/LOSDepth";
        private const string DepthRGBAShaderPath = "Shaders/LOSDepthRGBA";

        #endregion Constants

        #region Public Properties

        public static Shader Depth
        {
            get
            {
                if (m_Depth == null)
                    m_Depth = LoadShader(DepthShaderPath);

                return m_Depth;
            }
        }

        public static Shader DepthRGBA
        {
            get
            {
                if (m_DepthRGBA == null)
                    m_DepthRGBA = LoadShader(DepthRGBAShaderPath);

                return m_DepthRGBA;
            }
        }

        #endregion Public Properties

        #region Private Data members

        private static Shader m_Depth;
        private static Shader m_DepthRGBA;

        #endregion Private Data members

        /// <summary>
        /// Destroys all resources
        /// </summary>
        public static void DestroyResources()
        {
            m_Depth = null;
            m_DepthRGBA = null;
        }

        /// <summary>
        /// Creates and returns shader.
        /// </summary>
        private static Shader LoadShader(string shaderResource)
        {
            Shader shader = Resources.Load(shaderResource, typeof(Shader)) as Shader;

            Assert.Test(shader != null, "Failed to load shader: " + shaderResource);
            Assert.Test(CheckShader(shader), "Shader not supported: " + shaderResource);

            return shader;
        }

        /// <summary>
        /// Checks if shader exists and is supported.
        /// </summary>
        public static bool CheckShader(Shader shader)
        {
            return (shader != null && shader.isSupported);
        }
    }

    public class ShaderID
    {
        #region Shader IDs

        public static int FrustumRays
        {
            get { return Shader.PropertyToID("_FrustumRays"); }
        }

        public static int FrustumOrigins
        {
            get { return Shader.PropertyToID("_FrustumOrigins"); }
        }

        public static int SourceDepthTex
        {
            get { return Shader.PropertyToID("_SourceDepthTex"); }
        }

        public static int SourceDepthCube
        {
            get { return Shader.PropertyToID("_SourceDepthCube"); }
        }

        public static int SourceWorldProj
        {
            get { return Shader.PropertyToID("_SourceWorldProj"); }
        }

        public static int SourceInfo
        {
            get { return Shader.PropertyToID("_SourceInfo"); }
        }

        public static int Settings
        {
            get { return Shader.PropertyToID("_Settings"); }
        }

        public static int Flags
        {
            get { return Shader.PropertyToID("_Flags"); }
        }

        public static int ColorMask
        {
            get { return Shader.PropertyToID("_ColorMask"); }
        }

        public static int FarPlane
        {
            get { return Shader.PropertyToID("_FarPlane"); }
        }

        public static int PreEffectTex
        {
            get { return Shader.PropertyToID("_PreEffectTex"); }
        }

        public static int MaskTex
        {
            get { return Shader.PropertyToID("_MaskTex"); }
        }

        #endregion Shader IDs
    }
}