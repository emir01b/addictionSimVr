using UnityEngine;
using System;

namespace AlcoholAwareness
{
    /// <summary>
    /// Manages the lifecycle of scenarios.
    /// Handles starting, stopping, and monitoring scenario progress.
    /// </summary>
    public class ScenarioManager : MonoBehaviour
    {
        public static ScenarioManager Instance { get; private set; }

        private void Awake()
        {
            if (Instance == null)
                Instance = this;
            else
                Destroy(gameObject);
        }

        public void StartScenario(ScenarioData data)
        {
            Debug.Log($"[ScenarioManager] Starting scenario: {data.scenarioName}");
            
            switch (data.scenarioType)
            {
                case ScenarioType.BalanceWalk:
                    StartBalanceWalk(data);
                    break;
                case ScenarioType.ReflexTest:
                    Debug.Log("Reflex Test not implemented yet.");
                    break;
                case ScenarioType.PrecisionControl:
                    Debug.Log("Precision Control not implemented yet.");
                    break;
                case ScenarioType.PrecisionCarry:
                    Debug.Log("Precision Carry not implemented yet.");
                    break;
            }
        }

        private void StartBalanceWalk(ScenarioData data)
        {
            // Create or activate the Balance Walk scenario logic
            BalanceWalkScenario scenario = gameObject.GetComponent<BalanceWalkScenario>();
            if (scenario == null)
                scenario = gameObject.AddComponent<BalanceWalkScenario>();
            
            scenario.StartScenario(data);
        }

        public void StopCurrentScenario()
        {
            // Stop whatever is running
            var balanceWalk = GetComponent<BalanceWalkScenario>();
            if (balanceWalk != null && balanceWalk.IsRunning)
                balanceWalk.StopScenario();
        }
    }
}
