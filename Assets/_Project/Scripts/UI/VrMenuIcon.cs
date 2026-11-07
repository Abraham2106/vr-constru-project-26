using UnityEngine;
using UnityEngine.UI;

namespace ProyectoVR.UI
{
    [RequireComponent(typeof(CanvasRenderer))]
    public sealed class VrMenuIcon : MaskableGraphic
    {
        public string Kind;
        protected override void OnPopulateMesh(VertexHelper mesh)
        {
            mesh.Clear();
            switch (Kind)
            {
                case "Home":
                    Line(mesh,2,14,12,23); Line(mesh,12,23,22,14);
                    Line(mesh,5,14,5,3); Line(mesh,5,3,10,3); Line(mesh,10,3,10,10);
                    Line(mesh,10,10,15,10); Line(mesh,15,10,15,3); Line(mesh,15,3,20,3); Line(mesh,20,3,20,14); break;
                case "Medir":
                    Line(mesh,3,6,18,21); Line(mesh,18,21,23,16);Line(mesh,23,16,8,1);Line(mesh,8,1,3,6);
                    for(int i=0;i<4;i++)Line(mesh,7+i*3,10+i*3,10+i*3,7+i*3);break;
                case "Pintar":
                    Line(mesh,3,16,19,16);Line(mesh,19,16,19,22);Line(mesh,19,22,3,22);Line(mesh,3,22,3,16);
                    Line(mesh,19,19,23,19);Line(mesh,23,19,23,12);Line(mesh,23,12,12,12);Line(mesh,12,12,12,2);break;
                case "Dia":
                    Circle(mesh,12,12,5,0,360);
                    for(int i=0;i<8;i++){float a=i*Mathf.PI/4;Line(mesh,12+8*Mathf.Cos(a),12+8*Mathf.Sin(a),12+11*Mathf.Cos(a),12+11*Mathf.Sin(a));}break;
                case "Noche": Circle(mesh,12,12,10,90,360);Circle(mesh,17,17,9,180,270);break;
                case "Continuar":Line(mesh,2,12,22,12);Line(mesh,15,19,22,12);Line(mesh,22,12,15,5);break;
                case "SinHerramienta":Line(mesh,4,3,21,3);Line(mesh,21,3,21,19);Line(mesh,21,19,17,23);Line(mesh,17,23,4,23);Line(mesh,4,23,4,3);Line(mesh,8,23,8,15);Line(mesh,8,15,17,15);Line(mesh,17,15,17,23);break;
                default:Circle(mesh,12,12,9,-70,210);Line(mesh,4,17,3,24);Line(mesh,4,17,11,18);break;
            }
        }
        void Circle(VertexHelper m,float x,float y,float r,float start,float end){for(int i=0;i<24;i++){float a=Mathf.Lerp(start,end,i/24f)*Mathf.Deg2Rad,b=Mathf.Lerp(start,end,(i+1)/24f)*Mathf.Deg2Rad;Line(m,x+r*Mathf.Cos(a),y+r*Mathf.Sin(a),x+r*Mathf.Cos(b),y+r*Mathf.Sin(b));}}
        void Line(VertexHelper mesh,float x,float y,float x2,float y2)
        {
            var rect=rectTransform.rect;
            Vector2 a=new Vector2(rect.xMin+x/24*rect.width,rect.yMin+y/24*rect.height);
            Vector2 b=new Vector2(rect.xMin+x2/24*rect.width,rect.yMin+y2/24*rect.height);
            Vector2 normal=new Vector2(-(b-a).y,(b-a).x).normalized*rect.width/28;
            int n=mesh.currentVertCount;
            foreach(var p in new[]{a-normal,a+normal,b+normal,b-normal}){var v=UIVertex.simpleVert;v.position=p;v.color=color;mesh.AddVert(v);}
            mesh.AddTriangle(n,n+1,n+2);mesh.AddTriangle(n,n+2,n+3);
        }
    }
}
