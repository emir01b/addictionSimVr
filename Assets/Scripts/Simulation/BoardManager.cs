using System.Collections;
using UnityEngine;

namespace AlcoholSimVR.Simulation
{
    public class BoardManager : MonoBehaviour
    {
        [Header("Prefab")]
        [SerializeField] private GameObject _beamBoardPrefab;

        [Header("Boyut")]
        [SerializeField] private float _boardLength = 10f;          // max 10 m
        [SerializeField] private float _boardWidth = 0.20f;
        [SerializeField] private float _boardHeight = 0.03f;

        [Header("Konum")]
        [SerializeField] private float _fallbackFloorY = 0f;
        [SerializeField] private float _wallBufferMeters = 1.0f;    // 1 m tampon (ön‑arka)
        [SerializeField] private float _minBoardLength = 2f;        // en az 2 m

        [Header("Yanal Kayma")]
        [SerializeField] private float _swayRange = 0.30f;
        [SerializeField] private float _swayIntervalMin = 0.5f;
        [SerializeField] private float _swayIntervalMax = 2.0f;
        [SerializeField] private float _swayMoveSpeed = 0.4f;

        [Header("Etki Kademesi")]
        [SerializeField] private AlcoholEffectLevel _effectLevel = AlcoholEffectLevel.Medium;

        [Header("Referanslar")]
        [SerializeField] private Core.SessionTracker _sessionTracker;
        [SerializeField] private Transform _floorReference;

        private const float PrefabDefaultWidth  = 0.20f;
        private const float PrefabDefaultHeight = 0.03f;
        private const float PrefabDefaultLength = 3f;

        private GameObject _spawnedBoard;
        private Vector3 _boardCenterPos;
        private Vector3 _boardRightDir;
        private Coroutine _swayCoroutine;
        private float _effectiveLength;          // spawn sırasında belirlenen gerçek uzunluk

        public float ActiveBoardWidth => _boardWidth;

        public void SetEffectLevel(AlcoholEffectLevel level)
        {
            _effectLevel = level;
        }

        /// <summary>
        /// Kafadan aşağı dikme tahtaya değiyor mu?
        /// isPastEnds: kullanıcı tahtanın ön/arka ucunu geçti mi?
        /// </summary>
        public bool IsPlumbLineOnBoard(Vector3 headPos, out bool isPastEnds)
        {
            isPastEnds = false;
            if (_spawnedBoard == null) return false;

            Vector3 boardPos = _spawnedBoard.transform.position;
            Vector3 fwd = _spawnedBoard.transform.forward;
            Vector3 right = _spawnedBoard.transform.right;

            Vector3 offset = new Vector3(headPos.x, boardPos.y, headPos.z) - boardPos;
            float along = Vector3.Dot(offset, fwd);
            float across = Vector3.Dot(offset, right);

            float halfLen = _effectiveLength * 0.5f;
            float halfWid = _boardWidth * 0.5f;

            if (Mathf.Abs(along) > halfLen)
            {
                isPastEnds = true;
                return false;
            }

            return Mathf.Abs(across) <= halfWid;
        }

        public void SpawnBoard()
        {
            DespawnBoard();
            if (_beamBoardPrefab == null) { Debug.LogWarning("[BoardManager] Prefab yok."); return; }

            Transform cam = ResolveCameraTransform();
            float floorY = ResolveFloorY(cam);

            Vector3 spawnPos;
            Quaternion spawnRot;

            // default uzunluk, mümkünse kısaltılır
            _effectiveLength = _boardLength;

            if (TryPlaceInPlayArea(floorY, out spawnPos, out spawnRot, out _effectiveLength))
                Debug.Log($"[BoardManager] Play area merkezine yerleştirildi. Uzunluk={_effectiveLength:F1}m");
            else if (TryPlaceByRaycast(cam, floorY, out spawnPos, out spawnRot, out _effectiveLength))
                Debug.Log($"[BoardManager] Raycast ile ortalandı. Uzunluk={_effectiveLength:F1}m");
            else
            {
                Vector3 fwd = HorizontalForward(cam);
                spawnPos = cam.position + fwd * 1.0f;
                spawnRot = Quaternion.LookRotation(fwd, Vector3.up);
            }

            spawnPos.y = floorY + _boardHeight * 0.5f;
            _spawnedBoard = Instantiate(_beamBoardPrefab, spawnPos, spawnRot);
            ScaleBoard(_spawnedBoard.transform, _effectiveLength);
            _boardCenterPos = spawnPos;
            _boardRightDir = spawnRot * Vector3.right;

            foreach (var trigger in _spawnedBoard.GetComponentsInChildren<BeamWalkTrigger>(true))
                trigger.Setup(_sessionTracker, trigger.name.Contains("Side"));

            StartSway();
        }

        public void DespawnBoard()
        {
            StopSway();
            if (_spawnedBoard != null) { Destroy(_spawnedBoard); _spawnedBoard = null; }
        }

        // ── Yanal Kayma ──
        public void StartSway()
        {
            StopSway();
            if (_spawnedBoard != null)
                _swayCoroutine = StartCoroutine(SwayLoopRoutine());
        }

        public void StopSway()
        {
            if (_swayCoroutine != null) { StopCoroutine(_swayCoroutine); _swayCoroutine = null; }
        }

        private IEnumerator SwayLoopRoutine()
        {
            while (_spawnedBoard != null)
            {
                yield return new WaitForSeconds(Random.Range(GetSwayIntervalMin(), GetSwayIntervalMax()));
                if (_spawnedBoard == null) yield break;

                float swayRange = GetSwayRange();
                float targetOffset = Random.Range(-swayRange, swayRange);
                Vector3 targetPos = _boardCenterPos + _boardRightDir * targetOffset;
                yield return StartCoroutine(SlideBoardRoutine(targetPos));
            }
        }

        private IEnumerator SlideBoardRoutine(Vector3 targetPos)
        {
            if (_spawnedBoard == null) yield break;
            Vector3 startPos = _spawnedBoard.transform.position;
            float dist = Vector3.Distance(startPos, targetPos);
            if (dist < 0.001f) yield break;

            float duration = dist / Mathf.Max(0.01f, GetSwayMoveSpeed());
            float elapsed = 0f;
            while (elapsed < duration && _spawnedBoard != null)
            {
                elapsed += Time.deltaTime;
                float t = Mathf.SmoothStep(0f, 1f, Mathf.Clamp01(elapsed / duration));
                _spawnedBoard.transform.position = Vector3.Lerp(startPos, targetPos, t);
                yield return null;
            }
            if (_spawnedBoard != null)
                _spawnedBoard.transform.position = targetPos;
        }

        // ── Zemin ──
        private float GetSwayRange()
        {
            return _swayRange * GetSwayRangeMultiplier();
        }

        private float GetSwayIntervalMin()
        {
            return Mathf.Max(0.1f, _swayIntervalMin * GetSwayIntervalMultiplier());
        }

        private float GetSwayIntervalMax()
        {
            return Mathf.Max(GetSwayIntervalMin(), _swayIntervalMax * GetSwayIntervalMultiplier());
        }

        private float GetSwayMoveSpeed()
        {
            return _swayMoveSpeed * GetSwaySpeedMultiplier();
        }

        private float GetSwayRangeMultiplier()
        {
            return _effectLevel switch
            {
                AlcoholEffectLevel.Low => 0.45f,
                AlcoholEffectLevel.High => 1.65f,
                _ => 1f
            };
        }

        private float GetSwayIntervalMultiplier()
        {
            return _effectLevel switch
            {
                AlcoholEffectLevel.Low => 1.45f,
                AlcoholEffectLevel.High => 0.62f,
                _ => 1f
            };
        }

        private float GetSwaySpeedMultiplier()
        {
            return _effectLevel switch
            {
                AlcoholEffectLevel.Low => 0.65f,
                AlcoholEffectLevel.High => 1.35f,
                _ => 1f
            };
        }

        private float ResolveFloorY(Transform cam)
        {
            if (_floorReference != null) return _floorReference.position.y;

            try
            {
                var pts = OVRManager.boundary.GetGeometry(OVRBoundary.BoundaryType.PlayArea);
                if (pts != null && pts.Length > 0)
                {
                    float y = pts[0].y;
                    var rig = FindAnyObjectByType<OVRCameraRig>();
                    if (rig != null) y = rig.transform.TransformPoint(new Vector3(0, y, 0)).y;
                    return y;
                }
            }
            catch { }

            var cr = FindAnyObjectByType<OVRCameraRig>();
            if (cr != null && cam.position.y - cr.transform.position.y > 0.3f)
                return cr.transform.position.y;

            if (Physics.Raycast(cam.position, Vector3.down, out RaycastHit hit, 5f))
                return hit.point.y;

            return cam.position.y - 1.6f;
        }

        // ── Yerleştirme ──
        private bool TryPlaceInPlayArea(float floorY, out Vector3 pos, out Quaternion rot, out float length)
        {
            pos = Vector3.zero; rot = Quaternion.identity; length = _boardLength;
            Vector3[] points;
            try { points = OVRManager.boundary.GetGeometry(OVRBoundary.BoundaryType.PlayArea); }
            catch { return false; }
            if (points == null || points.Length < 4) return false;

            Vector3 center = Vector3.zero;
            foreach (var p in points) center += p;
            center /= points.Length;

            Vector3 s1 = points[1] - points[0], s2 = points[2] - points[1];
            Vector3 longSide = s1.magnitude > s2.magnitude ? s1 : s2;

            var rig = FindAnyObjectByType<OVRCameraRig>();
            if (rig != null) { center = rig.transform.TransformPoint(center); longSide = rig.transform.TransformDirection(longSide); }

            Vector3 fwd = longSide; fwd.y = 0;
            if (fwd.sqrMagnitude < 0.001f) return false;

            // Ön‑arka sınırlama: sınırlardan en az 1 m uzak
            float available = longSide.magnitude - _wallBufferMeters * 2f;
            // Board uzunluğunu kullanılabilir alana göre ayarla, ancak en az _minBoardLength
            length = Mathf.Clamp(available, _minBoardLength, _boardLength);

            // Board merkezini, kullanılabilir alan içinde 1 m tamponu koruyacak şekilde kaydır
            float extraOffset = (available - length) * 0.5f; // pozitif ise forward yönünde
            Vector3 offsetVec = fwd.normalized * extraOffset;
            pos = center + offsetVec; pos.y = floorY;
            rot = Quaternion.LookRotation(fwd.normalized, Vector3.up);
            return true;
        }

        private bool TryPlaceByRaycast(Transform cam, float floorY, out Vector3 pos, out Quaternion rot, out float length)
        {
            pos = Vector3.zero; rot = Quaternion.identity; length = _boardLength;
            Vector3 origin = new Vector3(cam.position.x, floorY + 0.15f, cam.position.z);
            Vector3 fwd = HorizontalForward(cam);
            float dF = MeasureDist(origin, fwd), dB = MeasureDist(origin, -fwd);
            if (dF >= 19f && dB >= 19f) return false;

            // Ön‑arka sınırlama: sınırlardan en az 1 m uzak
            float corridor = dF + dB;
            float available = corridor - _wallBufferMeters * 2f;
            length = Mathf.Clamp(available, _minBoardLength, _boardLength);

            // Board merkezini, kullanılabilir corridor içinde 1 m tamponu koruyacak şekilde kaydır
            float extraOffset = (available - length) * 0.5f;
            float totalOffset = (dF - dB) * 0.5f + extraOffset;
            pos = origin + fwd * totalOffset; pos.y = floorY;
            rot = Quaternion.LookRotation(fwd, Vector3.up);
            return true;
        }

        private float MeasureDist(Vector3 o, Vector3 d) => Physics.Raycast(o, d, out RaycastHit h, 20f) ? h.distance : 20f;

        // ── Yardımcılar ──
        private void ScaleBoard(Transform root, float length)
        {
            if (root == null) return;
            root.localScale = new Vector3(
                _boardWidth / PrefabDefaultWidth,
                _boardHeight / PrefabDefaultHeight,
                length / PrefabDefaultLength);
        }

        private static Vector3 HorizontalForward(Transform t)
        {
            Vector3 f = t.forward; f.y = 0;
            return f.sqrMagnitude < 0.001f ? Vector3.forward : f.normalized;
        }

        private Transform ResolveCameraTransform()
        {
            var rig = FindAnyObjectByType<OVRCameraRig>();
            if (rig != null && rig.centerEyeAnchor != null) return rig.centerEyeAnchor;
            return Camera.main != null ? Camera.main.transform : transform;
        }
    }
}
