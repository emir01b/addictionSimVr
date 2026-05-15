using System.Collections.Generic;
using UnityEngine;
using UnityEngine.XR.Hands;

namespace AlcoholAwareness
{
    /// <summary>
    /// Controls the visibility and positioning of the hand menu.
    /// Detects when the left palm faces the user and shows the menu canvas.
    /// 
    /// Key behaviors:
    /// - Palm detection using XRHandSubsystem
    /// - Smooth position/rotation/scale animation
    /// - Suppresses menu when info panel is active
    /// - Panel distance 15cm from palm so hand doesn't overlap content
    /// </summary>
    public class HandMenuController : MonoBehaviour
    {
        [Header("Avuç İçi Algılama")]
        [Range(0.3f, 0.9f)]
        [SerializeField] float m_PalmFacingThreshold = 0.5f;

        [Header("Panel Konumlandırma")]
        [Tooltip("Panelin avuç içinden kullanıcıya doğru uzaklığı (metre).")]
        [SerializeField] float m_PanelDistanceFromPalm = 0.15f;

        [Tooltip("Panelin dünya yukarı yönünde kayması (metre).")]
        [SerializeField] float m_PanelUpOffset = 0.05f;

        [Header("Animasyon")]
        [Range(1f, 20f)]
        [SerializeField] float m_SmoothSpeed = 10f;
        [Range(1f, 20f)]
        [SerializeField] float m_ScaleAnimSpeed = 12f;

        // ── Internal ───────────────────────────────────────────────
        static readonly List<XRHandSubsystem> s_Subsystems = new List<XRHandSubsystem>();
        XRHandSubsystem m_HandSubsystem;
        Transform m_CanvasTransform;
        Camera m_MainCamera;

        bool m_IsPanelVisible;
        bool m_InfoPanelActive; // when true, menu won't auto-show
        float m_CurrentScale;
        Vector3 m_TargetPosition;
        Quaternion m_TargetRotation;
        float m_CanvasBaseScale = 0.001f;

        // UI references
        ScenarioMenuUI m_MenuUI;
        ScenarioInfoUI m_InfoUI;

        // ── Public API ─────────────────────────────────────────────

        public void SetCanvasTransform(Transform t)
        {
            m_CanvasTransform = t;
            // Store the base scale from the canvas
            if (t != null)
                m_CanvasBaseScale = t.localScale.x;
        }

        public void SetUIReferences(ScenarioMenuUI menu, ScenarioInfoUI info)
        {
            m_MenuUI = menu;
            m_InfoUI = info;
        }

        /// <summary>
        /// Called by ScenarioMenuUI when info panel opens/closes.
        /// When active, the hand menu will not auto-show.
        /// </summary>
        public void SetInfoPanelActive(bool active)
        {
            m_InfoPanelActive = active;

            if (active)
            {
                // Hide hand menu immediately
                m_IsPanelVisible = false;
                if (m_MenuUI != null) m_MenuUI.SetVisible(false);
            }
        }

        // ── Lifecycle ──────────────────────────────────────────────

        void Start()
        {
            m_MainCamera = Camera.main;
            m_CurrentScale = 0f;

            if (m_CanvasTransform != null)
                m_CanvasTransform.localScale = Vector3.zero;

            TryGetHandSubsystem();
        }

        void Update()
        {
            if (m_HandSubsystem == null || !m_HandSubsystem.running)
            {
                TryGetHandSubsystem();
                return;
            }

            if (m_MainCamera == null)
            {
                m_MainCamera = Camera.main;
                if (m_MainCamera == null) return;
            }

            bool palmFacing = CheckPalmFacingUser();
            UpdateVisibility(palmFacing);
            UpdateTransform();
            UpdateScale();
        }

        // ── Palm Detection ─────────────────────────────────────────

        bool CheckPalmFacingUser()
        {
            var leftHand = m_HandSubsystem.leftHand;
            if (!leftHand.isTracked) return false;

            var palmJoint = leftHand.GetJoint(XRHandJointID.Palm);
            if (!palmJoint.TryGetPose(out Pose palmPose)) return false;

            // palmPose.up points DORSALLY (toward back of hand).
            // Negate to get palm-surface-to-user direction.
            Vector3 palmNormal = -palmPose.up;
            Vector3 palmPos = palmPose.position;
            Vector3 toCamera = (m_MainCamera.transform.position - palmPos).normalized;

            float dot = Vector3.Dot(palmNormal, toCamera);

            if (dot > m_PalmFacingThreshold)
            {
                // Position: push toward user + lift upward
                m_TargetPosition = palmPos
                    + palmNormal * m_PanelDistanceFromPalm
                    + Vector3.up * m_PanelUpOffset;

                // Rotation: canvas -Z faces viewer
                Vector3 lookDir = m_MainCamera.transform.position - m_TargetPosition;
                if (lookDir.sqrMagnitude > 0.001f)
                    m_TargetRotation = Quaternion.LookRotation(-lookDir, Vector3.up);

                return true;
            }

            return false;
        }

        // ── Visibility ─────────────────────────────────────────────

        void UpdateVisibility(bool palmFacing)
        {
            // Don't show menu while info panel is active
            if (m_InfoPanelActive)
            {
                if (m_IsPanelVisible)
                {
                    m_IsPanelVisible = false;
                    if (m_MenuUI != null) m_MenuUI.SetVisible(false);
                }
                return;
            }

            if (palmFacing && !m_IsPanelVisible)
            {
                m_IsPanelVisible = true;
                if (m_MenuUI != null) m_MenuUI.SetVisible(true);
            }
            else if (!palmFacing && m_IsPanelVisible)
            {
                m_IsPanelVisible = false;
                if (m_MenuUI != null) m_MenuUI.SetVisible(false);
            }
        }

        // ── Transform Smoothing ────────────────────────────────────

        void UpdateTransform()
        {
            if (m_CanvasTransform == null || !m_IsPanelVisible) return;

            float t = 1f - Mathf.Exp(-m_SmoothSpeed * Time.deltaTime);
            m_CanvasTransform.position = Vector3.Lerp(m_CanvasTransform.position, m_TargetPosition, t);
            m_CanvasTransform.rotation = Quaternion.Slerp(m_CanvasTransform.rotation, m_TargetRotation, t);
        }

        void UpdateScale()
        {
            if (m_CanvasTransform == null) return;

            float target = m_IsPanelVisible ? 1f : 0f;
            m_CurrentScale = Mathf.Lerp(m_CurrentScale, target,
                1f - Mathf.Exp(-m_ScaleAnimSpeed * Time.deltaTime));

            if (Mathf.Abs(m_CurrentScale - target) < 0.01f)
                m_CurrentScale = target;

            m_CanvasTransform.localScale = Vector3.one * m_CurrentScale * m_CanvasBaseScale;
        }

        // ── Subsystem ──────────────────────────────────────────────

        void TryGetHandSubsystem()
        {
            s_Subsystems.Clear();
            SubsystemManager.GetSubsystems(s_Subsystems);

            foreach (var sub in s_Subsystems)
            {
                if (sub.running)
                {
                    m_HandSubsystem = sub;
                    return;
                }
            }

            if (s_Subsystems.Count > 0)
                m_HandSubsystem = s_Subsystems[0];
        }
    }
}
