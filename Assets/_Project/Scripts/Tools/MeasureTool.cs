using System.Collections.Generic;
using TMPro;
using UnityEngine;

namespace Vivienda.Tools
{
    public class MeasureTool : MonoBehaviour, IViviendaTool
    {
        [Header("Visual")]
        [SerializeField] Material lineMaterial;
        [SerializeField] float lineWidth = 0.008f;
        [SerializeField] Color lineColor = new(1f, 0.85f, 0.1f);
        [SerializeField] float labelScale = 0.024f;
        [Tooltip("Material TMP con shader Overlay: el numero se ve por encima de paredes y muebles.")]
        [SerializeField] Material labelMaterial;

        readonly List<MeasureSegment> segments = new();
        Transform segmentRoot;
        Vector3? pendingA;
        bool active;

        public string ToolId => ViviendaToolIds.Measure;
        public bool IsActive => active;
        public int SegmentCount => segments.Count;
        public bool HasPendingPoint => pendingA.HasValue;
        GameObject pendingMarker;
        void OnDestroy(){if(segmentRoot!=null)Destroy(segmentRoot.gameObject);if(pendingMarker!=null)Destroy(pendingMarker);}

        public void SetActive(bool value)
        {
            active = value;
            if (!active) {pendingA = null;if(pendingMarker!=null)Destroy(pendingMarker);}
        }

        public void OnTriggerDown(ToolRaycastHit hit)
        {
            if (!active || !hit.HasHit)
                return;

            if (pendingA == null)
            {
                pendingA = hit.Point;
                pendingMarker=GameObject.CreatePrimitive(PrimitiveType.Sphere);pendingMarker.name="Punto A";pendingMarker.GetComponent<Collider>().enabled=false;pendingMarker.transform.position=hit.Point;pendingMarker.transform.localScale=Vector3.one*.045f;if(lineMaterial!=null)pendingMarker.GetComponent<Renderer>().sharedMaterial=lineMaterial;
                return;
            }

            if(pendingMarker!=null)Destroy(pendingMarker);
            CreateSegment(pendingA.Value, hit.Point);
            pendingA = null;
        }

        public void OnSecondaryDown()
        {
            UndoLast();
        }

        public void UndoLast()
        {
            if(pendingA.HasValue){pendingA=null;if(pendingMarker!=null)Destroy(pendingMarker);return;}
            if (segments.Count == 0)
            {
                pendingA = null;
                return;
            }

            var last = segments[segments.Count - 1];
            segments.RemoveAt(segments.Count - 1);
            if (last != null)
                Destroy(last.gameObject);
        }

        public void ClearAll()
        {
            pendingA = null;
            if(pendingMarker!=null)Destroy(pendingMarker);
            for (var i = segments.Count - 1; i >= 0; i--)
            {
                if (segments[i] != null)
                    Destroy(segments[i].gameObject);
            }
            segments.Clear();
        }

        void CreateSegment(Vector3 a, Vector3 b)
        {
            EnsureRoot();
            var go = new GameObject($"Measure_{segments.Count + 1:D2}");
            go.transform.SetParent(segmentRoot, false);

            var segment = go.AddComponent<MeasureSegment>();
            segment.Build(a, b, lineWidth, lineColor, lineMaterial, labelScale, labelMaterial);
            segments.Add(segment);
        }

        void EnsureRoot()
        {
            if (segmentRoot != null)
                return;
            var root = new GameObject("MeasureSegments");
            root.transform.SetParent(null, false);
            segmentRoot = root.transform;
        }
    }

    public class MeasureSegment : MonoBehaviour
    {
        LineRenderer line;
        TextMeshPro label;

        public float DistanceMeters { get; private set; }

        public static string Format(float meters) => meters < 1f ? $"{meters * 100f:F0} cm" : $"{meters:F2} m";

        public void Build(Vector3 a, Vector3 b, float width, Color color, Material mat, float labelScale, Material labelMaterial = null)
        {
            DistanceMeters = Vector3.Distance(a, b);

            line = gameObject.AddComponent<LineRenderer>();
            line.positionCount = 2;
            line.SetPosition(0, a);
            line.SetPosition(1, b);
            line.startWidth = width;
            line.endWidth = width;
            line.numCapVertices = 4;
            line.useWorldSpace = true;
            line.material = mat != null ? mat : new Material(Shader.Find("Sprites/Default"));
            line.startColor = color;
            line.endColor = color;

            foreach (var end in new[] { a, b })
            {
                var dot = GameObject.CreatePrimitive(PrimitiveType.Sphere);
                dot.name = "Extremo";
                Destroy(dot.GetComponent<Collider>());
                dot.transform.SetParent(transform, false);
                dot.transform.position = end;
                dot.transform.localScale = Vector3.one * 0.025f;
                if (mat != null) dot.GetComponent<Renderer>().sharedMaterial = mat;
            }

            var labelGo = new GameObject("Label");
            labelGo.transform.SetParent(transform, false);
            labelGo.transform.position = (a + b) * 0.5f;
            labelGo.transform.localScale = Vector3.one * labelScale;

            label = labelGo.AddComponent<TextMeshPro>();
            if (labelMaterial != null) label.fontSharedMaterial = labelMaterial;
            label.alignment = TextAlignmentOptions.Center;
            label.fontSize = 30;
            label.fontStyle = FontStyles.Bold;
            label.textWrappingMode = TextWrappingModes.NoWrap;
            label.rectTransform.sizeDelta = new Vector2(220, 60);
            label.color = Color.white;
            label.text = Format(DistanceMeters);
            labelGo.AddComponent<MeasureLabel>().Init((a + b) * 0.5f);
        }
    }

    /// <summary>
    /// Mantiene el numero legible: mira a la camara, conserva el mismo tamano aparente a cualquier distancia
    /// y se separa de la linea hacia el usuario para no quedar dentro de la pared.
    /// </summary>
    public class MeasureLabel : MonoBehaviour
    {
        const float HeightPerMeter = 0.05f;    // altura del texto por metro de distancia (~2.9 grados)
        const float TextUnitHeight = 3.35f;    // altura del texto TMP (fontSize 30) a escala 1
        const float MinScale = 0.008f, MaxScale = 0.1f;

        Vector3 _anchor;
        Camera _camera;

        public void Init(Vector3 anchor) { _anchor = anchor; LateUpdate(); }

        void LateUpdate()
        {
            if (_camera == null) _camera = Camera.main;
            if (_camera == null) return;
            var eye = _camera.transform.position;
            var toEye = eye - _anchor;
            float distance = toEye.magnitude;
            if (distance < 0.01f) return;
            var dir = toEye / distance;
            transform.position = _anchor + dir * Mathf.Min(0.12f, distance * 0.25f) + Vector3.up * 0.04f;
            transform.rotation = Quaternion.LookRotation(-dir, Vector3.up);
            float scale = Mathf.Clamp(distance * HeightPerMeter / TextUnitHeight, MinScale, MaxScale);
            transform.localScale = Vector3.one * scale;
        }
    }
}
