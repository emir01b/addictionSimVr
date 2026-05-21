using System.Reflection;
using UnityEngine;

namespace AlcoholSimVR.Utilities
{
    /// <summary>
    /// OVRHand internal alanlarına erişim (HandType SDK'da internal).
    /// </summary>
    public static class OVRHandUtility
    {
        private static readonly FieldInfo HandTypeField = typeof(OVRHand).GetField(
            "HandType",
            BindingFlags.Instance | BindingFlags.NonPublic);

        /// <summary>Sol veya sağ el tipini ayarlar.</summary>
        public static void SetHandType(OVRHand hand, OVRHand.Hand handType)
        {
            if (hand == null || HandTypeField == null)
            {
                return;
            }

            HandTypeField.SetValue(hand, handType);
        }

        /// <summary>OVRSkeleton tipini ayarlar.</summary>
        public static void SetSkeletonType(OVRSkeleton skeleton, OVRSkeleton.SkeletonType type)
        {
            if (skeleton == null)
            {
                return;
            }

            MethodInfo method = typeof(OVRSkeleton).GetMethod(
                "SetSkeletonType",
                BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);

            method?.Invoke(skeleton, new object[] { type });
        }
    }
}
