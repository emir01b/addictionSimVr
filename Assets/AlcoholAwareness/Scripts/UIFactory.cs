using UnityEngine;
using UnityEngine.UI;

using UnityEngine.XR.Interaction.Toolkit.UI;
using TMPro;

namespace AlcoholAwareness
{
    /// <summary>
    /// Factory class for creating UI elements programmatically in world space.
    /// All UI is built at runtime to avoid direct scene file manipulation.
    /// </summary>
    public static class UIFactory
    {
        // ── Color Palette (Modern Dark / Glassmorphism) ────────────
        public static readonly Color PanelBackground = new Color(0.05f, 0.05f, 0.12f, 0.85f);
        public static readonly Color AccentCyan      = new Color(0.0f, 0.95f, 1.0f, 1.0f);
        public static readonly Color AccentPurple    = new Color(0.6f, 0.2f, 1.0f, 1.0f);
        public static readonly Color TextMain        = new Color(1.00f, 1.00f, 1.00f, 1.00f);
        public static readonly Color TextSub         = new Color(0.70f, 0.75f, 0.85f, 1.00f);
        public static readonly Color ButtonNormal    = new Color(0.15f, 0.15f, 0.25f, 0.90f);
        public static readonly Color ButtonHover     = new Color(0.0f, 0.95f, 1.0f, 0.15f);
        public static readonly Color ButtonStart     = new Color(0.0f, 0.60f, 1.0f, 1.0f);

        private static Sprite s_RoundedSprite;
        private static Sprite s_CircleSprite;

        // ── Canvas Creation ────────────────────────────────────────
        public static Canvas CreateWorldSpaceCanvas(string canvasName, Transform parent, Vector2 sizeDelta, float scale = 0.001f)
        {
            var go = new GameObject(canvasName);
            go.transform.SetParent(parent, false);

            var canvas = go.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.WorldSpace;

            var scaler = go.AddComponent<CanvasScaler>();
            scaler.dynamicPixelsPerUnit = 25f;

            go.AddComponent<TrackedDeviceGraphicRaycaster>();

            var rectTransform = go.GetComponent<RectTransform>();
            rectTransform.sizeDelta = sizeDelta;
            rectTransform.localScale = Vector3.one * scale;
            rectTransform.localPosition = Vector3.zero;

            return canvas;
        }

        // ── Panel Creation ─────────────────────────────────────────
        public static RectTransform CreatePanel(string panelName, Transform parent, Vector2 sizeDelta, Color bgColor)
        {
            var go = new GameObject(panelName);
            go.transform.SetParent(parent, false);

            var rect = go.AddComponent<RectTransform>();
            rect.sizeDelta = sizeDelta;
            rect.anchorMin = new Vector2(0.5f, 0.5f);
            rect.anchorMax = new Vector2(0.5f, 0.5f);
            rect.pivot = new Vector2(0.5f, 0.5f);

            var img = go.AddComponent<Image>();
            img.sprite = GetRoundedSprite();
            img.type = Image.Type.Sliced;
            img.color = bgColor;
            img.raycastTarget = false;

            return rect;
        }

        // ── Border / Outline ───────────────────────────────────────
        public static void AddOutline(GameObject target, Color outlineColor, Vector2 thickness)
        {
            var outline = target.AddComponent<Outline>();
            outline.effectColor = outlineColor;
            outline.effectDistance = thickness;
        }

        // ── Text Creation ──────────────────────────────────────────
        public static TextMeshProUGUI CreateText(
            string objName,
            Transform parent,
            string text,
            float fontSize,
            Color color,
            TextAlignmentOptions alignment = TextAlignmentOptions.Center,
            FontStyles fontStyle = FontStyles.Normal)
        {
            var go = new GameObject(objName);
            go.transform.SetParent(parent, false);

            var rect = go.AddComponent<RectTransform>();
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;

            var tmp = go.AddComponent<TextMeshProUGUI>();
            tmp.text = text;
            tmp.fontSize = fontSize;
            tmp.color = color;
            tmp.alignment = alignment;
            tmp.fontStyle = fontStyle;
            tmp.enableWordWrapping = true;
            tmp.overflowMode = TextOverflowModes.Ellipsis;
            tmp.raycastTarget = false;

            return tmp;
        }

        // ── Button Creation ────────────────────────────────────────
        public static Button CreateButton(
            string btnName,
            Transform parent,
            Vector2 sizeDelta,
            Color normalColor,
            System.Action onClick = null)
        {
            var go = new GameObject(btnName);
            go.transform.SetParent(parent, false);

            var rect = go.AddComponent<RectTransform>();
            rect.sizeDelta = sizeDelta;
            rect.anchorMin = new Vector2(0.5f, 0.5f);
            rect.anchorMax = new Vector2(0.5f, 0.5f);
            rect.pivot = new Vector2(0.5f, 0.5f);

            var img = go.AddComponent<Image>();
            img.sprite = GetRoundedSprite();
            img.type = Image.Type.Sliced;
            img.color = normalColor;
            img.raycastTarget = true;

            var btn = go.AddComponent<Button>();
            var colors = btn.colors;
            colors.normalColor = Color.white;
            colors.highlightedColor = new Color(1.2f, 1.2f, 1.2f, 1f);
            colors.pressedColor = new Color(0.7f, 0.7f, 0.7f, 1f);
            colors.selectedColor = Color.white;
            colors.fadeDuration = 0.05f;
            btn.colors = colors;

            if (onClick != null)
                btn.onClick.AddListener(() => onClick());

            var collider = go.AddComponent<BoxCollider>();
            collider.size = new Vector3(sizeDelta.x, sizeDelta.y, 10f);

            AddLayoutElement(go, preferredWidth: sizeDelta.x, preferredHeight: sizeDelta.y);
            go.AddComponent<UIHoverFeedback>();

            return btn;
        }

        // ── Icon (Image) Creation ──────────────────────────────────
        public static Image CreateIcon(string objName, Transform parent, Sprite sprite, Vector2 sizeDelta)
        {
            var go = new GameObject(objName);
            go.transform.SetParent(parent, false);

            var rect = go.AddComponent<RectTransform>();
            rect.sizeDelta = sizeDelta;
            rect.anchorMin = new Vector2(0.5f, 0.5f);
            rect.anchorMax = new Vector2(0.5f, 0.5f);
            rect.pivot = new Vector2(0.5f, 0.5f);

            var img = go.AddComponent<Image>();
            img.sprite = sprite;
            img.preserveAspect = true;
            img.raycastTarget = false;
            img.color = AccentCyan;

            return img;
        }

        // ── Layout Helpers ─────────────────────────────────────────
        public static GridLayoutGroup AddGridLayout(GameObject parent, Vector2 cellSize, Vector2 spacing, RectOffset padding, int constraintCount = 2)
        {
            var grid = parent.AddComponent<GridLayoutGroup>();
            grid.cellSize = cellSize;
            grid.spacing = spacing;
            grid.padding = padding;
            grid.constraint = GridLayoutGroup.Constraint.FixedColumnCount;
            grid.constraintCount = constraintCount;
            grid.childAlignment = TextAnchor.MiddleCenter;
            return grid;
        }

        public static VerticalLayoutGroup AddVerticalLayout(GameObject parent, RectOffset padding, float spacing, TextAnchor childAlignment = TextAnchor.UpperCenter)
        {
            var layout = parent.AddComponent<VerticalLayoutGroup>();
            layout.padding = padding;
            layout.spacing = spacing;
            layout.childAlignment = childAlignment;
            layout.childControlWidth = true;
            layout.childControlHeight = true;
            layout.childForceExpandWidth = true;
            layout.childForceExpandHeight = false;
            return layout;
        }

        public static HorizontalLayoutGroup AddHorizontalLayout(GameObject parent, RectOffset padding, float spacing, TextAnchor childAlignment = TextAnchor.MiddleCenter)
        {
            var layout = parent.AddComponent<HorizontalLayoutGroup>();
            layout.padding = padding;
            layout.spacing = spacing;
            layout.childAlignment = childAlignment;
            layout.childControlWidth = true;
            layout.childControlHeight = true;
            layout.childForceExpandWidth = false;
            layout.childForceExpandHeight = false;
            return layout;
        }

        public static LayoutElement AddLayoutElement(GameObject target, float preferredWidth = -1f, float preferredHeight = -1f, float minWidth = -1f, float minHeight = -1f)
        {
            var le = target.AddComponent<LayoutElement>();
            le.preferredWidth = preferredWidth;
            le.preferredHeight = preferredHeight;
            le.minWidth = minWidth;
            le.minHeight = minHeight;
            return le;
        }

        // ── Interaction ────────────────────────────────────────────
        public static void MakeMovable(GameObject target, Vector2 size)
        {
            var rb = target.GetComponent<Rigidbody>();
            if (rb == null) rb = target.AddComponent<Rigidbody>();
            rb.isKinematic = true;
            rb.useGravity = false;

            var col = target.GetComponent<BoxCollider>();
            if (col == null) col = target.AddComponent<BoxCollider>();
            col.size = new Vector3(size.x, size.y, 10f);
            col.isTrigger = true;

            var grab = target.GetComponent<UnityEngine.XR.Interaction.Toolkit.Interactables.XRGrabInteractable>();
            if (grab == null) grab = target.AddComponent<UnityEngine.XR.Interaction.Toolkit.Interactables.XRGrabInteractable>();
            
            grab.movementType = UnityEngine.XR.Interaction.Toolkit.Interactables.XRBaseInteractable.MovementType.Instantaneous;
            grab.attachEaseInTime = 0.1f;
            grab.throwOnDetach = false;
            grab.retainTransformParent = true;
        }

        // ── Procedural Assets ─────────────────────────────────────
        public static Sprite GetRoundedSprite()
        {
            if (s_RoundedSprite != null) return s_RoundedSprite;
            int res = 128;
            int radius = 32;
            Texture2D tex = new Texture2D(res, res, TextureFormat.RGBA32, false);
            for (int y = 0; y < res; y++)
            {
                for (int x = 0; x < res; x++)
                {
                    bool inside = true;
                    if (x < radius && y < radius && Vector2.Distance(new Vector2(x, y), new Vector2(radius, radius)) > radius) inside = false;
                    else if (x > res - radius && y < radius && Vector2.Distance(new Vector2(x, y), new Vector2(res - radius, radius)) > radius) inside = false;
                    else if (x < radius && y > res - radius && Vector2.Distance(new Vector2(x, y), new Vector2(radius, res - radius)) > radius) inside = false;
                    else if (x > res - radius && y > res - radius && Vector2.Distance(new Vector2(x, y), new Vector2(res - radius, res - radius)) > radius) inside = false;
                    tex.SetPixel(x, y, inside ? Color.white : new Color(1, 1, 1, 0));
                }
            }
            tex.Apply();
            s_RoundedSprite = Sprite.Create(tex, new Rect(0, 0, res, res), new Vector2(0.5f, 0.5f), 100, 0, SpriteMeshType.FullRect, new Vector4(radius, radius, radius, radius));
            return s_RoundedSprite;
        }

        public static Sprite GetCircleSprite()
        {
            if (s_CircleSprite != null) return s_CircleSprite;
            int res = 128;
            float center = res / 2f;
            Texture2D tex = new Texture2D(res, res, TextureFormat.RGBA32, false);
            for (int y = 0; y < res; y++)
            {
                for (int x = 0; x < res; x++)
                {
                    float dist = Vector2.Distance(new Vector2(x, y), new Vector2(center, center));
                    tex.SetPixel(x, y, dist <= center ? Color.white : new Color(1, 1, 1, 0));
                }
            }
            tex.Apply();
            s_CircleSprite = Sprite.Create(tex, new Rect(0, 0, res, res), new Vector2(0.5f, 0.5f));
            return s_CircleSprite;
        }
    }

    public class UIHoverFeedback : MonoBehaviour, UnityEngine.EventSystems.IPointerEnterHandler, UnityEngine.EventSystems.IPointerExitHandler
    {
        Vector3 m_OriginalScale;
        void Start() => m_OriginalScale = transform.localScale;
        public void OnPointerEnter(UnityEngine.EventSystems.PointerEventData eventData) => transform.localScale = m_OriginalScale * 1.05f;
        public void OnPointerExit(UnityEngine.EventSystems.PointerEventData eventData) => transform.localScale = m_OriginalScale;
    }
}
