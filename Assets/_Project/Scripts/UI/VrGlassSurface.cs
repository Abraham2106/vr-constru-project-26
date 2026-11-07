using UnityEngine;
using UnityEngine.UI;

namespace ProyectoVR.UI
{
    // Rounded glass treatment using UI geometry only: stereo-safe, no framebuffer copies.
    [RequireComponent(typeof(CanvasRenderer))]
    public sealed class VrGlassSurface : MaskableGraphic
    {
        public float Radius = 24f;
        public float Border = 1.5f;
        public bool Accent;
        public bool Selected;
        public Vector2 ShinePoint;
        public float ShineStrength;

        static Material sheenMaterial;
        protected override void OnEnable()
        {
            base.OnEnable();
            if(sheenMaterial==null)
            {
                var shader=Resources.Load<Shader>("VrGlassSheen");
                if(shader!=null)sheenMaterial=new Material(shader){hideFlags=HideFlags.HideAndDontSave};
            }
            if(sheenMaterial!=null)material=sheenMaterial;
            if(canvas!=null)canvas.additionalShaderChannels|=AdditionalCanvasShaderChannels.TexCoord1;
        }

        protected override void OnPopulateMesh(VertexHelper mesh)
        {
            mesh.Clear();
            var rect = rectTransform.rect;
            float radius = Mathf.Min(Radius, Mathf.Min(rect.width, rect.height) * .5f);
            const int steps = 10;
            const int count = 4 * (steps + 1);
            Add(mesh, rect.center, Fill(.5f) * color);
            for (int ring = 0; ring < 2; ring++)
            {
                float inset = ring == 0 ? 0 : Border;
                float r = Mathf.Max(0, radius - inset);
                for (int corner = 0; corner < 4; corner++)
                {
                    Vector2 center = new Vector2(corner == 0 || corner == 3 ? rect.xMax - radius : rect.xMin + radius,
                        corner < 2 ? rect.yMax - radius : rect.yMin + radius);
                    for (int step = 0; step <= steps; step++)
                    {
                        float angle = (corner * 90f + step * 90f / steps) * Mathf.Deg2Rad;
                        Vector2 p = center + new Vector2(Mathf.Cos(angle), Mathf.Sin(angle)) * r;
                        float t = Mathf.InverseLerp(rect.yMin, rect.yMax, p.y);
                        float gradient = Accent ? Mathf.InverseLerp(rect.xMin, rect.xMax, p.x) : t;
                        Add(mesh, p, ring == 0 ? new Color(.85f, .93f, 1f, Mathf.Lerp(.12f, Selected ? .35f : .18f, t) * color.a) : Fill(gradient) * color);
                    }
                }
            }
            for (int i = 0; i < count; i++)
            {
                int next = (i + 1) % count;
                mesh.AddTriangle(0, 1 + count + i, 1 + count + next);
                mesh.AddTriangle(1 + i, 1 + next, 1 + count + next);
                mesh.AddTriangle(1 + i, 1 + count + next, 1 + count + i);
            }
        }

        Color Fill(float t)
        {
            if (Selected) return Color.Lerp(new Color(.32f,.37f,.43f,.92f),new Color(.44f,.49f,.55f,.92f),t);
            return Accent ? Color.Lerp(new Color(.22f, .43f, 1f), new Color(.10f, .78f, .85f), t)
                : Color.Lerp(new Color(.62f, .69f, .80f, .86f), Color.white, t);
        }

        void Add(VertexHelper mesh, Vector2 position, Color tint)
        {
            var vertex = UIVertex.simpleVert;
            vertex.position = position;
            vertex.color = tint;
            var rect=rectTransform.rect;
            vertex.uv0=new Vector2(Mathf.InverseLerp(rect.xMin,rect.xMax,position.x),Mathf.InverseLerp(rect.yMin,rect.yMax,position.y));
            vertex.uv1=new Vector4(.5f+ShinePoint.x*.4f,.5f+ShinePoint.y*.35f,ShineStrength,0);
            mesh.AddVert(vertex);
        }
    }
}
