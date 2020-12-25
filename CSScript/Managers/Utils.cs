using System;
using System.Collections.Generic;

namespace CSScript
{
    /// <summary>
    /// Преставляет универсальные средства для работы с данными.
    /// </summary>
    internal static class Utils
    {
        public static void DeleteDuplicates<T>(List<T> list, Func<T, T, bool> comparison)
        {
            for (int i = 0; i < list.Count - 1; i++) {
                T value1 = list[i];
                for (int j = i + 1; j < list.Count; j++) {
                    T value2 = list[j];
                    if (comparison(value1, value2)) {
                        list.RemoveAt(j--);
                    }
                }
            }
        }
    }
}
