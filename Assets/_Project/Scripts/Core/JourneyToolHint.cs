using UnityEngine;
using UnityEngine.UI;
using TMPro;
using Unity.XR.CoreUtils;
using Vivienda.Tools;
using ProyectoVR.UI;
namespace ProyectoVR {
 public class JourneyToolHint:MonoBehaviour {
  TextMeshProUGUI text;Canvas canvas;GameObject pointer;string previous;
  void Start(){
   var origin=GetComponent<XROrigin>();if(origin==null||origin.Camera==null)return;
   var go=new GameObject("Herramienta activa",typeof(RectTransform),typeof(Canvas));
   canvas=go.GetComponent<Canvas>();canvas.renderMode=RenderMode.WorldSpace;canvas.worldCamera=origin.Camera;
   var rect=go.GetComponent<RectTransform>();rect.sizeDelta=new Vector2(800,80);
   go.transform.SetParent(origin.Camera.transform,false);go.transform.localPosition=new Vector3(0,-.28f,.9f);go.transform.localScale=Vector3.one*.001f;
   var background=go.AddComponent<Image>();background.color=new Color(.025f,.05f,.075f,.86f);background.raycastTarget=false;
   text=Label("Texto",go.transform,new Vector2(800,80),Vector2.zero,23);
   var crosshair=Label("Puntero",go.transform,new Vector2(35,35),new Vector2(0,280),26);crosshair.text="+";pointer=crosshair.gameObject;
  }
  TextMeshProUGUI Label(string name,Transform parent,Vector2 size,Vector2 position,int fontSize){
   var go=new GameObject(name,typeof(RectTransform),typeof(TextMeshProUGUI));go.transform.SetParent(parent,false);
   var rect=go.GetComponent<RectTransform>();rect.sizeDelta=size;rect.anchoredPosition=position;
   var label=go.GetComponent<TextMeshProUGUI>();label.font=TMP_Settings.defaultFontAsset;label.fontSize=fontSize;label.alignment=TextAlignmentOptions.Center;label.color=Color.white;label.raycastTarget=false;return label;
  }
  void Update(){
   var host=ViviendaToolHost.Instance;if(canvas==null)return;
   canvas.gameObject.SetActive(host!=null&&host.ActiveToolId!=ViviendaToolIds.None&&!VrWorldMenuController.AnyMenuVisible);
   if(!canvas.gameObject.activeSelf)return;pointer.SetActive(DesktopPreview.IsActive);
   string value;
   if(host.ActiveToolId==ViviendaToolIds.Measure)value=host.Measure.HasPendingPoint?"Marca el punto B | Gatillo / Espacio (escritorio)":"Medir: marca el punto A | Gatillo / Espacio (escritorio)";
   else {var paint=host.WallPaint;value="Pintar: "+(paint!=null&&paint.Palette!=null?paint.Palette.GetDisplayName(paint.SelectedColorIndex):"elige un color en el menu")+" | Gatillo / Espacio (escritorio)";}
   if(value!=previous){text.text=value;previous=value;}
  }
 }
}
