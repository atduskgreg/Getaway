using System.Collections.Generic;

namespace LOS
{
    public class LOSManager
    {
        #region Singleton

        private static LOSManager m_Instance;

        private LOSManager()
        {
        }

        public static LOSManager Instance
        {
            get
            {
                if (m_Instance == null)
                {
                    m_Instance = new LOSManager();
                }
                return m_Instance;
            }
        }

        #endregion Singleton

        #region Private Data Members

        private List<LOSSource> m_LOSSources = new List<LOSSource>();
        private List<LOSSourceCube> m_LOSSourcesCube = new List<LOSSourceCube>();

        #endregion Private Data Members

        #region Public Properties

        /// <summary>
        /// Returns list containing all LOSSources in the current scene.
        /// </summary>
        public List<LOSSource> LOSSources
        {
            get { return m_LOSSources; }
        }

        /// <summary>
        /// Returns list containing all LOSSourceCubes in the current scene.
        /// </summary>
        public List<LOSSourceCube> LOSSourcesCube
        {
            get { return m_LOSSourcesCube; }
        }

        /// <summary>
        /// Return numbers of cameras that are currently visible and rendering.
        /// </summary>
        public int ActiveCameraCount
        {
            get
            {
                int visibleSourceCount = 0;

                foreach (ILOSSource source in m_LOSSources)
                {
                    if (source.IsVisible)
                        ++visibleSourceCount;
                }

                foreach (ILOSSource source in m_LOSSourcesCube)
                {
                    if (source.IsVisible)
                        ++visibleSourceCount;
                }

                return visibleSourceCount;
            }
        }

        /// <summary>
        /// Returns total number of cameras in the scene.
        /// </summary>
        public int CameraCount
        {
            get { return m_LOSSources.Count + m_LOSSourcesCube.Count; }
        }

        #endregion Public Properties

        #region Public Functions

        public void AddLOSSource(LOSSource source)
        {
            Assert.Test(!m_LOSSources.Contains(source), "LOSSource already in list, can't add");

            m_LOSSources.Add(source);
        }

        public void AddLOSSourceCube(LOSSourceCube sourceCube)
        {
            Assert.Test(!m_LOSSourcesCube.Contains(sourceCube), "LOS Source Cube already in list, can't add");

            m_LOSSourcesCube.Add(sourceCube);
        }

        public void RemoveLOSSource(LOSSource source)
        {
            Assert.Test(m_LOSSources.Contains(source), "LOSSource not found in list, can't remove");

            m_LOSSources.Remove(source);
        }

        public void RemoveLOSSourceCube(LOSSourceCube sourceCube)
        {
            Assert.Test(m_LOSSourcesCube.Contains(sourceCube), "LOS Source Cube not found in list, can't remove");

            m_LOSSourcesCube.Remove(sourceCube);
        }

        #endregion Public Functions
    }
}