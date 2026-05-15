using UnityEngine;

namespace AlcoholAwareness
{
    /// <summary>
    /// Defines the available scenario types in the alcohol awareness simulation.
    /// </summary>
    public enum ScenarioType
    {
        BalanceWalk,
        ReflexTest,
        PrecisionControl,
        PrecisionCarry
    }

    /// <summary>
    /// ScriptableObject that holds all data for a single scenario.
    /// Create instances via Assets > Create > AlcoholAwareness > Scenario Data.
    /// </summary>
    [CreateAssetMenu(fileName = "NewScenario", menuName = "AlcoholAwareness/Scenario Data")]
    public class ScenarioData : ScriptableObject
    {
        [Header("Temel Bilgiler")]
        [Tooltip("Senaryo türü")]
        public ScenarioType scenarioType;

        [Tooltip("Senaryo başlığı (UI'da gösterilecek)")]
        public string scenarioName;

        [Tooltip("Senaryo ikonu")]
        public Sprite scenarioIcon;

        [Header("Bilgilendirme Sayfası")]
        [Tooltip("Senaryonun amacı")]
        [TextArea(2, 4)]
        public string purpose;

        [Tooltip("Kullanıcıdan beklenen")]
        [TextArea(2, 4)]
        public string expectation;

        [Tooltip("Alkol etkisinin neyi bozacağı")]
        [TextArea(2, 4)]
        public string alcoholEffect;

        [Tooltip("Kısa bilgilendirme metni (buton altında gösterilir)")]
        [TextArea(3, 6)]
        public string shortDescription;
    }
}
