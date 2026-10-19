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
        [SerializeField] float labelScale = 0.004f;

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
            segment.Build(a, b, lineWidth, lineColor, lineMaterial, labelScale);
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

        public void Build(Vector3 a, Vector3 b, float width, Color color, Material mat, float labelScale)
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

            var labelGo = new GameObject("Label");
            labelGo.transform.SetParent(transform, false);
            labelGo.transform.position = (a + b) * 0.5f + Vector3.up * 0.05f;
            labelGo.transform.localScale = Vector3.one * labelScale;
            labelGo.AddComponent<BillboardToCamera>();

            label = labelGo.AddComponent<TextMeshPro>();
            label.alignment = TextAlignmentOptions.Center;
            label.fontSize = 30;
            label.rectTransform.sizeDelta=new Vector2(220,60);
            label.color = Color.white;
            label.text = $"{DistanceMeters:F2} m";
        }
    }
}
