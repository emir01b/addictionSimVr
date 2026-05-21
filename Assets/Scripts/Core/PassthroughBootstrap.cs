using UnityEngine;

namespace AlcoholSimVR.Core
{
    /// <summary>
    /// Passthrough MR — <see cref="MRRuntimeConfigurator"/> ile birlikte çalışır.
    /// </summary>
    [DefaultExecutionOrder(-199)]
    public class PassthroughBootstrap : MonoBehaviour
    {
        [SerializeField] private MRRuntimeConfigurator _configurator;

        private void Awake()
        {
            if (_configurator == null)
            {
                _configurator = GetComponent<MRRuntimeConfigurator>();
            }

            if (_configurator == null)
            {
                _configurator = gameObject.AddComponent<MRRuntimeConfigurator>();
            }

            if (GetComponent<HandTrackingSetup>() == null)
            {
                gameObject.AddComponent<HandTrackingSetup>();
            }
        }
    }
}
