using System;

namespace CSScript.Core
{
    internal static class Validate
    {
        public static void IsTrue(bool condition, string message = null) {
            if (!condition) {
                throw message == null ? new Exception() : new Exception(message);
            }
        }

        public static void IsNotNull(object obj, string message = null) {
            IsTrue(obj != null, message);
        }

        public static void IsNotBlank(string value, string message = null) {
            IsTrue(!string.IsNullOrEmpty(value), message);
        }
    }
}
