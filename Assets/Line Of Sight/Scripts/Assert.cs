using UnityEngine;

namespace LOS
{
    public class Assert
    {
        [System.Diagnostics.Conditional("UNITY_EDITOR")]
        public static void Test(bool comparison, string message)
        {
            if (!comparison)
            {
                Debug.LogWarning(message);
                Debug.Break();
            }
        }

        [System.Diagnostics.Conditional("UNITY_EDITOR")]
        public static void Test(bool comparison)
        {
            if (!comparison)
            {
                Debug.LogWarning("Assertion failed");
                Debug.Break();
            }
        }

        public static bool Verify(bool comparison, string message)
        {
            Test(comparison, "Verify Failed: " + message);

            return comparison;
        }

        public static bool Verify(bool comparison)
        {
            Test(comparison, "Verify Failed!");

            return comparison;
        }
    }
}