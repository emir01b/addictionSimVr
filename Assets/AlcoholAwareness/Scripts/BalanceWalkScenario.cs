using UnityEngine;
using UnityEngine.XR.ARFoundation;
using System.Collections;
using System.Collections.Generic;
using TMPro;

namespace AlcoholAwareness
{
    /// <summary>
    /// Balance Walk (Denge Yürüyüşü) Scenario - Reimplemented.
    ///
    /// Flow:
    ///   1. Board (tahta) spawns on the floor at y=0, in front of the user.
    ///   2. 5-second countdown starts. During this time the user can turn
    ///      their head to reposition the board wherever they want.
    ///   3. After countdown, board locks in place and the walk begins.
    ///   4. User walks along the board. Deviation from the center line
    ///      reduces the score. Reaching the end completes the scenario.
    ///
    /// Key design decisions:
    ///   - No circular boundary. Full room is available (passthrough MR).
    ///   - ARPlaneManager is queried to detect walls so the board avoids them.
    ///   - Board sits exactly at y=0 (Quest floor level).
    /// </summary>
    public class BalanceWalkScenario : MonoBehaviour
    {
        public bool IsRunning { get; private set; }

        // ── Board Dimensions ──────────────────────────────────
        [Header("Board Settings")]
        [SerializeField] float m_BoardLength = 4.0f;
        [SerializeField] float m_BoardWidth  = 0.3f;
        [SerializeField] float m_BoardThickness = 0.02f;
        [SerializeField] float m_MaxDeviation = 0.25f;

        // ── Runtime State ─────────────────────────────────────
        GameObject m_BoardRoot;       // Parent of all scenario objects
        GameObject m_BoardMesh;       // The visible plank
        GameObject m_CountdownObj;    // Countdown text object
        TextMeshPro m_CountdownTMP;

        Transform m_Cam;
        Vector3 m_BoardCenter;
        Vector3 m_BoardForward;       // Direction from start to end
        Vector3 m_StartPos;
        Vector3 m_EndPos;

        float m_Score = 100f;
        bool  m_Completed;
        bool  m_PlacementPhase;       // True during the 5-sec placement

        ScenarioData m_Data;

        // ── Public API ────────────────────────────────────────

        public void StartScenario(ScenarioData data)
        {
            if (IsRunning) StopScenario();

            m_Data = data;
            m_Cam  = Camera.main.transform;
            IsRunning  = true;
            m_Completed = false;
            m_Score = 100f;

            Debug.Log("[BalanceWalk] Scenario started.");
            StartCoroutine(ScenarioFlow());
        }

        public void StopScenario()
        {
            IsRunning = false;
            StopAllCoroutines();

            if (m_BoardRoot != null)
                Destroy(m_BoardRoot);
            if (m_CountdownObj != null)
                Destroy(m_CountdownObj);
        }

        // ── Main Flow ─────────────────────────────────────────

        IEnumerator ScenarioFlow()
        {
            // Create root container
            m_BoardRoot = new GameObject("BalanceWalk_Root");

            // ── Phase 1: Placement (5 seconds) ───────────────
            CreateBoard();
            CreateCountdownText();

            m_PlacementPhase = true;

            for (int i = 5; i > 0; i--)
            {
                if (m_CountdownTMP != null)
                    m_CountdownTMP.text = $"Tahtayı yerleştirin\nKafanızı çevirin\n<size=14>{i}</size>";

                yield return new WaitForSeconds(1f);
            }

            m_PlacementPhase = false;

            // Lock the final position
            LockBoardPosition();

            // Show "BAŞLA!" briefly
            if (m_CountdownTMP != null)
            {
                m_CountdownTMP.text = "BAŞLA!";
                yield return new WaitForSeconds(1f);
            }
            if (m_CountdownObj != null) Destroy(m_CountdownObj);

            // ── Phase 2: Walking ─────────────────────────────
            CreateEdgeMarkers();

            while (IsRunning && !m_Completed)
            {
                EvaluateWalk();
                yield return null;
            }

            if (m_Completed)
                ShowResults();
        }

        // ── Board Creation ────────────────────────────────────

        void CreateBoard()
        {
            m_BoardMesh = GameObject.CreatePrimitive(PrimitiveType.Cube);
            m_BoardMesh.name = "WalkBoard";
            m_BoardMesh.transform.SetParent(m_BoardRoot.transform);

            // Scale: length on Z, width on X, thickness on Y
            m_BoardMesh.transform.localScale = new Vector3(
                m_BoardWidth, m_BoardThickness, m_BoardLength);

            // Remove physics collider (we don't need it)
            var col = m_BoardMesh.GetComponent<Collider>();
            if (col != null) Destroy(col);

            // Material
            var rend = m_BoardMesh.GetComponent<Renderer>();
            if (rend != null)
            {
                Material mat = CreateBoardMaterial();
                rend.material = mat;
            }

            // Initial position: in front of user, on the floor
            UpdateBoardToGaze();
        }

        Material CreateBoardMaterial()
        {
            // Try URP Lit first, then fallback
            Shader sh = Shader.Find("Universal Render Pipeline/Lit");
            if (sh == null) sh = Shader.Find("Standard");
            if (sh == null) sh = Shader.Find("Unlit/Color");

            Material mat = new Material(sh);
            // Warm wood-like color
            mat.color = new Color(0.55f, 0.35f, 0.15f, 1f);
            return mat;
        }

        /// <summary>
        /// During placement phase, board follows the user's forward gaze
        /// projected onto the floor.
        /// </summary>
        void UpdateBoardToGaze()
        {
            Vector3 forward = m_Cam.forward;
            forward.y = 0f;
            if (forward.sqrMagnitude < 0.001f) forward = Vector3.forward;
            forward.Normalize();

            Vector3 camFloor = m_Cam.position;
            camFloor.y = 0f;

            // Board starts 0.8m ahead, centered along its length
            m_BoardCenter = camFloor + forward * (0.8f + m_BoardLength * 0.5f);
            m_BoardForward = forward;

            m_StartPos = m_BoardCenter - forward * (m_BoardLength * 0.5f);
            m_EndPos   = m_BoardCenter + forward * (m_BoardLength * 0.5f);

            // Check wall collision and pull back if needed
            AdjustForWalls();

            // Apply transform — board sits exactly on the floor
            m_BoardMesh.transform.position = new Vector3(
                m_BoardCenter.x,
                m_BoardThickness * 0.5f,   // half-thickness so bottom = y:0
                m_BoardCenter.z);

            m_BoardMesh.transform.rotation = Quaternion.LookRotation(forward, Vector3.up);
        }

        void LockBoardPosition()
        {
            // Recalculate one last time
            UpdateBoardToGaze();
            Debug.Log($"[BalanceWalk] Board locked at {m_BoardCenter}, dir={m_BoardForward}");
        }

        // ── Wall Avoidance ────────────────────────────────────

        void AdjustForWalls()
        {
            // Find ARPlaneManager in the scene
            var planeMgr = FindAnyObjectByType<ARPlaneManager>();
            if (planeMgr == null) return;

            float safeMargin = 0.3f;

            foreach (var plane in planeMgr.trackables)
            {
                // Only care about vertical planes (walls)
                if (plane.alignment != UnityEngine.XR.ARSubsystems.PlaneAlignment.Vertical)
                    continue;

                Vector3 wallPos = plane.transform.position;
                Vector3 wallNormal = plane.transform.up; // ARPlane normal

                // Check if board end is too close to this wall
                float distEnd = Vector3.Dot(m_EndPos - wallPos, wallNormal);
                if (Mathf.Abs(distEnd) < safeMargin)
                {
                    // Pull the board back so it stays away from the wall
                    float pullBack = safeMargin - Mathf.Abs(distEnd);
                    Vector3 pullDir = wallNormal * Mathf.Sign(distEnd);
                    m_BoardCenter += pullDir * pullBack;
                    m_StartPos += pullDir * pullBack;
                    m_EndPos   += pullDir * pullBack;
                }

                float distStart = Vector3.Dot(m_StartPos - wallPos, wallNormal);
                if (Mathf.Abs(distStart) < safeMargin)
                {
                    float pullBack = safeMargin - Mathf.Abs(distStart);
                    Vector3 pullDir = wallNormal * Mathf.Sign(distStart);
                    m_BoardCenter += pullDir * pullBack;
                    m_StartPos += pullDir * pullBack;
                    m_EndPos   += pullDir * pullBack;
                }
            }
        }

        // ── Countdown Text ────────────────────────────────────

        void CreateCountdownText()
        {
            m_CountdownObj = new GameObject("Countdown_TMP");
            m_CountdownObj.transform.SetParent(m_BoardRoot.transform);

            m_CountdownTMP = m_CountdownObj.AddComponent<TextMeshPro>();
            m_CountdownTMP.fontSize = 5;
            m_CountdownTMP.alignment = TextAlignmentOptions.Center;
            m_CountdownTMP.color = UIFactory.AccentCyan;
            m_CountdownTMP.enableWordWrapping = true;

            var rt = m_CountdownObj.GetComponent<RectTransform>();
            rt.sizeDelta = new Vector2(2f, 1f);
        }

        // ── Edge Markers ──────────────────────────────────────

        void CreateEdgeMarkers()
        {
            // Start marker (green-cyan)
            CreateSmallMarker("StartMarker", m_StartPos, UIFactory.AccentCyan);
            // End marker (purple)
            CreateSmallMarker("EndMarker",   m_EndPos,   UIFactory.AccentPurple);

            // Side edge lines (thin cylinders along each side of the board)
            Vector3 right = Vector3.Cross(Vector3.up, m_BoardForward).normalized;
            float halfW = m_BoardWidth * 0.5f;

            CreateEdgeLine("LeftEdge",
                m_StartPos - right * halfW,
                m_EndPos   - right * halfW,
                UIFactory.AccentCyan);

            CreateEdgeLine("RightEdge",
                m_StartPos + right * halfW,
                m_EndPos   + right * halfW,
                UIFactory.AccentPurple);
        }

        void CreateSmallMarker(string name, Vector3 pos, Color color)
        {
            var marker = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            marker.name = name;
            marker.transform.SetParent(m_BoardRoot.transform);
            marker.transform.position = new Vector3(pos.x, 0.015f, pos.z);
            marker.transform.localScale = new Vector3(0.12f, 0.01f, 0.12f);
            var rend = marker.GetComponent<Renderer>();
            if (rend != null) rend.material.color = color;
            var col = marker.GetComponent<Collider>();
            if (col != null) Destroy(col);
        }

        void CreateEdgeLine(string name, Vector3 from, Vector3 to, Color color)
        {
            var obj = new GameObject(name);
            obj.transform.SetParent(m_BoardRoot.transform);
            var lr = obj.AddComponent<LineRenderer>();
            lr.positionCount = 2;
            lr.useWorldSpace = true;
            lr.startWidth = 0.015f;
            lr.endWidth   = 0.015f;

            // Slightly above ground
            from.y = 0.025f;
            to.y   = 0.025f;
            lr.SetPosition(0, from);
            lr.SetPosition(1, to);

            Shader sh = Shader.Find("Universal Render Pipeline/Unlit");
            if (sh == null) sh = Shader.Find("Sprites/Default");
            if (sh != null) lr.material = new Material(sh);

            lr.startColor = color;
            lr.endColor   = color;
        }

        // ── Update (Placement Tracking) ───────────────────────

        void Update()
        {
            if (!IsRunning) return;

            // During placement, board follows head direction
            if (m_PlacementPhase && m_BoardMesh != null)
            {
                UpdateBoardToGaze();

                // Keep countdown text above the board center, facing user
                if (m_CountdownObj != null)
                {
                    m_CountdownObj.transform.position = m_BoardCenter + Vector3.up * 1.0f;
                    Vector3 lookDir = m_Cam.position - m_CountdownObj.transform.position;
                    lookDir.y = 0;
                    if (lookDir.sqrMagnitude > 0.001f)
                        m_CountdownObj.transform.rotation = Quaternion.LookRotation(lookDir);
                }
            }
        }

        // ── Walk Evaluation ───────────────────────────────────

        void EvaluateWalk()
        {
            Vector3 playerPos = m_Cam.position;
            playerPos.y = 0f;

            // Project player position onto the board's center line
            Vector3 startToPlayer = playerPos - m_StartPos;
            float projection = Vector3.Dot(startToPlayer, m_BoardForward);

            // Lateral deviation
            Vector3 closestOnLine = m_StartPos + m_BoardForward * Mathf.Clamp(projection, 0f, m_BoardLength);
            float deviation = Vector3.Distance(playerPos, closestOnLine);

            // Score penalty when deviating
            if (deviation > m_MaxDeviation)
            {
                float penalty = (deviation - m_MaxDeviation) * 15f * Time.deltaTime;
                m_Score = Mathf.Max(0f, m_Score - penalty);

                // Visual feedback: tint board red
                SetBoardColor(Color.Lerp(new Color(0.55f, 0.35f, 0.15f), Color.red, 0.5f));
            }
            else
            {
                SetBoardColor(new Color(0.55f, 0.35f, 0.15f));
            }

            // Check completion: player reached the end
            float distToEnd = Vector3.Distance(playerPos, m_EndPos);
            if (distToEnd < 0.5f && projection > m_BoardLength * 0.8f)
            {
                m_Completed = true;
            }
        }

        void SetBoardColor(Color c)
        {
            if (m_BoardMesh == null) return;
            var rend = m_BoardMesh.GetComponent<Renderer>();
            if (rend != null) rend.material.color = c;
        }

        // ── Results ───────────────────────────────────────────

        void ShowResults()
        {
            Debug.Log($"[BalanceWalk] Completed! Score: {m_Score:F0}/100");

            // Place result panel above the end of the board
            var resultObj = new GameObject("ResultPanel");
            resultObj.transform.SetParent(m_BoardRoot.transform);
            resultObj.transform.position = m_EndPos + Vector3.up * 1.2f;

            // Face the user
            Vector3 toUser = m_Cam.position - resultObj.transform.position;
            toUser.y = 0;
            if (toUser.sqrMagnitude > 0.001f)
                resultObj.transform.rotation = Quaternion.LookRotation(toUser);

            // Canvas
            var canvas = resultObj.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.WorldSpace;
            var rt = resultObj.GetComponent<RectTransform>();
            rt.sizeDelta = new Vector2(300, 180);
            rt.localScale = Vector3.one * 0.0015f;

            UIFactory.CreatePanel("Bg", resultObj.transform,
                new Vector2(300, 180), UIFactory.PanelBackground);

            var title = UIFactory.CreateText("Title", resultObj.transform,
                "TAMAMLANDI", 20f, UIFactory.AccentCyan,
                TextAlignmentOptions.Center, FontStyles.Bold);
            title.rectTransform.anchoredPosition = new Vector2(0, 50);

            string grade = m_Score >= 80 ? "Mükemmel!" : m_Score >= 50 ? "İyi" : "Geliştirilmeli";
            var scoreText = UIFactory.CreateText("Score", resultObj.transform,
                $"Skor: {m_Score:F0}/100\n{grade}", 22f, Color.white,
                TextAlignmentOptions.Center, FontStyles.Bold);
            scoreText.rectTransform.anchoredPosition = new Vector2(0, -5);

            var info = UIFactory.CreateText("Info", resultObj.transform,
                "Menüye dönmek için avucunuza bakın", 11f,
                UIFactory.TextSub, TextAlignmentOptions.Center);
            info.rectTransform.anchoredPosition = new Vector2(0, -55);

            // Auto cleanup
            IsRunning = false;
            Destroy(m_BoardRoot, 10f);
        }
    }
}
