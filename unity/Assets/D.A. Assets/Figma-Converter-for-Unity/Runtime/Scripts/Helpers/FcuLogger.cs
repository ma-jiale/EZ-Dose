using System.Collections;

namespace DA_Assets.FCU
{
    public class FcuLogger
    {
        public static void Debug(object log, FcuDebugSettingsFlags logType)
        {
            if (!FcuDebugSettings.Settings.HasFlag(logType))
                return;

            if (logType == FcuDebugSettingsFlags.LogError)
            {
                UnityEngine.Debug.LogError(log);
            }
            else
            {
                UnityEngine.Debug.Log(log);
            }
        }

        public static bool WriteLogBeforeEqual(ICollection list1, ICollection list2, FcuLocKey locKey, int count1, int count2, ref int tempCount)
        {
            if (list1.Count != list2.Count)
            {
                if (tempCount != list1.Count)
                {
                    tempCount = list1.Count;
                    UnityEngine.Debug.Log(locKey.Localize(count1, count2));
                }

                return true;
            }

            if (tempCount != list2.Count)
            {
                UnityEngine.Debug.Log(locKey.Localize(count1, count2));
            }

            return false;
        }

        public static bool WriteLogBeforeEqual(ref int count1, ref int count2, string log, ref int tempCount)
        {
            if (count1 != count2)
            {
                if (tempCount != count1)
                {
                    tempCount = count1;
                    UnityEngine.Debug.Log(log);
                }

                return true;
            }

            if (tempCount != count2)
            {
                UnityEngine.Debug.Log(log);
            }

            return false;
        }

        public static bool WriteLogBeforeEqual(ICollection list1, ICollection list2, FcuLocKey locKey, ref int tempCount)
        {
            if (list1.Count != list2.Count)
            {
                if (tempCount != list1.Count)
                {
                    tempCount = list1.Count;
                    UnityEngine.Debug.Log(locKey.Localize(list1.Count, list2.Count));
                }

                return true;
            }

            if (tempCount != list2.Count)
            {
                UnityEngine.Debug.Log(locKey.Localize(list1.Count, list2.Count));
            }

            return false;
        }
    }
}