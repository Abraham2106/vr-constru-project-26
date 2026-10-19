using System.Collections;
using ProyectoVR.Core;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Events;
using UnityEngine.XR.Interaction.Toolkit.UI;
using TMPro;
using Vivienda.Tools;
using Unity.XR.CoreUtils;

namespace ProyectoVR.UI
{
 public class VrWorldMenuController : MonoBehaviour
 {
  [SerializeField] Canvas m_Canvas;
  [SerializeField] XrLocomotionTeleportService m_Teleport;
  [SerializeField] UiClickSound m_ClickSound;
  [SerializeField] bool m_StartHidden;
  [SerializeField] ArchitecturalPaintPalette m_Palette;
  GameObject mainPanel,palettePanel;
  TextMeshProUGUI status;
  public static VrWorldMenuController Instance {get;private set;}
  public static bool AnyMenuVisible => Instance!=null&&Instance.IsVisible;
  public bool IsVisible => m_Canvas!=null&&m_Canvas.gameObject.activeSelf;
  public int ColorButtonCount {get;private set;}
  void Awake(){Instance=this;BuildMenu();}
  IEnumerator Start(){yield return null;SetVisible(!m_StartHidden);}
  void OnDestroy(){if(Instance==this)Instance=null;}
  RectTransform Rect(string name,Transform parent,Vector2 size,Vector2 position){var go=new GameObject(name,typeof(RectTransform));var r=go.GetComponent<RectTransform>();r.SetParent(parent,false);r.anchorMin=r.anchorMax=r.pivot=new Vector2(.5f,.5f);r.sizeDelta=size;r.anchoredPosition=position;return r;}
  TextMeshProUGUI Text(string name,Transform parent,string text,int size,Vector2 dimensions,Vector2 position,Color color){var r=Rect(name,parent,dimensions,position);var t=r.gameObject.AddComponent<TextMeshProUGUI>();t.font=TMP_Settings.defaultFontAsset;t.text=text;t.fontSize=size;t.color=color;t.alignment=TextAlignmentOptions.Center;t.raycastTarget=false;return t;}
  Button MakeButton(string name,Transform parent,string label,Vector2 position,Vector2 size,UnityAction action,Color? tint=null){
   var r=Rect(name,parent,size,position);var im=r.gameObject.AddComponent<Image>();im.color=tint??new Color(.13f,.23f,.32f,1);
   var button=r.gameObject.AddComponent<Button>();var colors=button.colors;colors.highlightedColor=new Color(.63f,.86f,1);colors.selectedColor=Color.white;colors.pressedColor=new Color(.35f,.7f,.8f);button.colors=colors;button.targetGraphic=im;button.onClick.AddListener(action);
   var c=im.color;var textColor=(c.r*.3f+c.g*.59f+c.b*.11f)>.6f?new Color(.05f,.08f,.12f):Color.white;
   Text("Label",r,label,28,size-new Vector2(16,10),Vector2.zero,textColor);return button;
  }
  public void BuildMenu(){
   if(m_Canvas!=null)m_Canvas.gameObject.SetActive(false);
   var root=Rect("Menu Journey",transform,new Vector2(1080,760),Vector2.zero);root.localScale=Vector3.one*.0012f;
   m_Canvas=root.gameObject.AddComponent<Canvas>();m_Canvas.renderMode=RenderMode.WorldSpace;m_Canvas.worldCamera=FindAnyObjectByType<XROrigin>()?.Camera;m_Canvas.sortingOrder=100;
   root.gameObject.AddComponent<GraphicRaycaster>();root.gameObject.AddComponent<TrackedDeviceGraphicRaycaster>();
   var bg=root.gameObject.AddComponent<Image>();bg.color=new Color(.035f,.065f,.10f,.98f);
   mainPanel=Rect("Inicio",root,new Vector2(1080,760),Vector2.zero).gameObject;
   Text("Title",mainPanel.transform,"VIVIENDA | RECORRIDO VR",40,new Vector2(1000,70),new Vector2(0,320),Color.white);
   Text("Subtitle",mainPanel.transform,"Dos apartamentos | Escala real",22,new Vector2(1000,45),new Vector2(0,267),new Color(.62f,.8f,.87f));
   MakeButton("Exterior",mainPanel.transform,"Exterior",new Vector2(-340,180),new Vector2(300,80),TeleportExterior);
   MakeButton("Piso1",mainPanel.transform,"Primer piso",new Vector2(0,180),new Vector2(300,80),TeleportPiso1);
   MakeButton("Piso2",mainPanel.transform,"Segundo piso",new Vector2(340,180),new Vector2(300,80),TeleportPiso2);
   MakeButton("Dia",mainPanel.transform,"Dia",new Vector2(-250,75),new Vector2(460,70),SetDayMode);
   MakeButton("Noche",mainPanel.transform,"Noche",new Vector2(250,75),new Vector2(460,70),SetNightMode);
   var measureButton=MakeButton("Medir",mainPanel.transform,"Medir distancia",new Vector2(-250,-25),new Vector2(460,85),ToggleMeasureTool);
   var measureLabel=measureButton.GetComponentInChildren<TextMeshProUGUI>();measureLabel.rectTransform.anchoredPosition=new Vector2(35,0);measureLabel.rectTransform.sizeDelta=new Vector2(350,70);
   var ruler=Rect("Regla",measureButton.transform,new Vector2(58,24),new Vector2(-175,0));ruler.gameObject.AddComponent<Image>().color=new Color(1,.85f,.2f);ruler.GetComponent<Image>().raycastTarget=false;
   for(int tick=0;tick<6;tick++){var mark=Rect("Marca",ruler,new Vector2(2,tick%2==0?14:8),new Vector2(-23+tick*9,5));var image=mark.gameObject.AddComponent<Image>();image.color=new Color(.08f,.12f,.16f);image.raycastTarget=false;}
   MakeButton("Pintar",mainPanel.transform,"Pintar paredes",new Vector2(250,-25),new Vector2(460,85),OpenWallPalette);
   MakeButton("SinHerramienta",mainPanel.transform,"Guardar herramienta",new Vector2(-340,-125),new Vector2(300,65),()=>{PlayClick();ViviendaToolHost.Instance?.DeactivateAll();});
   MakeButton("Deshacer",mainPanel.transform,"Deshacer medida",new Vector2(0,-125),new Vector2(300,65),()=>{PlayClick();ViviendaToolHost.Instance?.Measure?.UndoLast();});
   MakeButton("Restaurar",mainPanel.transform,"Restaurar paredes",new Vector2(340,-125),new Vector2(300,65),()=>{PlayClick();ViviendaToolHost.Instance?.WallPaint?.RestoreAllPainted();});
   status=Text("Estado",mainPanel.transform,"Acercate a las manijas o apunta con el mando.",23,new Vector2(1000,75),new Vector2(0,-215),new Color(.7f,.86f,.9f));
   MakeButton("Continuar",mainPanel.transform,"Continuar recorrido",new Vector2(0,-285),new Vector2(900,70),Close,new Color(.08f,.45f,.49f));
   Text("Controles",mainPanel.transform,"Y/Menu: panel | Grip: puertas | Gatillo: botones\nPC: M, WASD, E y Espacio | Mantener clic derecho: mirar",19,new Vector2(1040,55),new Vector2(0,-350),new Color(.7f,.8f,.84f));
   palettePanel=Rect("Paleta",root,new Vector2(1080,760),Vector2.zero).gameObject;
   Text("Title",palettePanel.transform,"ELIGE UN COLOR",38,new Vector2(1000,80),new Vector2(0,315),Color.white);
   Text("Help",palettePanel.transform,"Elige un color. Apunta a una pared y pulsa Gatillo / Espacio.",23,new Vector2(1000,75),new Vector2(0,250),Color.white);
   var palette=m_Palette!=null?m_Palette:ViviendaToolHost.Instance?.WallPaint?.Palette;
   if(palette==null)palette=Resources.Load<ArchitecturalPaintPalette>("ArchitecturalPaintPalette");
   ColorButtonCount=palette!=null?palette.Count:0;
   for(int i=0;i<ColorButtonCount;i++){int index=i;var col=palette.GetColor(i);MakeButton("Color_"+i,palettePanel.transform,palette.GetDisplayName(i),new Vector2(-375+(i%4)*250,140-(i/4)*115),new Vector2(230,95),()=>SelectColor(index),col);}
   MakeButton("Volver",palettePanel.transform,"Volver",new Vector2(-250,-285),new Vector2(460,75),()=>{PlayClick();palettePanel.SetActive(false);mainPanel.SetActive(true);});
   MakeButton("Cancelar",palettePanel.transform,"Cancelar herramienta",new Vector2(250,-285),new Vector2(460,75),()=>{ViviendaToolHost.Instance?.DeactivateAll();Close();});
   palettePanel.SetActive(false);
  }
  public void ToggleVisible(){SetVisible(!IsVisible);}
  public void Close(){PlayClick();SetVisible(false);}
  public void SetVisible(bool visible){
   if(m_Canvas==null)return;
   if(visible){var camera=FindAnyObjectByType<XROrigin>()?.Camera;if(camera!=null){var forward=Vector3.ProjectOnPlane(camera.transform.forward,Vector3.up);if(forward.sqrMagnitude<.0001f)forward=Vector3.forward;forward.Normalize();transform.SetPositionAndRotation(camera.transform.position+forward*1.25f-Vector3.up*.08f,Quaternion.LookRotation(forward,Vector3.up));}if(mainPanel!=null)mainPanel.SetActive(true);if(palettePanel!=null)palettePanel.SetActive(false);}
   m_Canvas.gameObject.SetActive(visible);ViviendaToolHost.Instance?.SuppressInput();
  }
  void Teleport(TeleportDestinationId id){PlayClick();m_Teleport?.TeleportTo(id);SetVisible(false);}
  public void TeleportExterior()=>Teleport(TeleportDestinationId.Exterior);
  public void TeleportPiso1()=>Teleport(TeleportDestinationId.Piso1);
  public void TeleportPiso2()=>Teleport(TeleportDestinationId.Piso2);
  public void SetDayMode(){PlayClick();EnvironmentCycleEvents.RequestNightMode(false);if(status!=null)status.text="Transicion al dia";}
  public void SetNightMode(){PlayClick();EnvironmentCycleEvents.RequestNightMode(true);if(status!=null)status.text="Transicion a la noche. Las luces de cada habitacion se controlan con sus interruptores.";}
  public void ToggleMeasureTool(){PlayClick();ViviendaToolHost.Instance?.ActivateTool(ViviendaToolIds.Measure);SetVisible(false);}
  public void OpenWallPalette(){PlayClick();mainPanel.SetActive(false);palettePanel.SetActive(true);}
  public void SelectColor(int index){PlayClick();var host=ViviendaToolHost.Instance;host?.ActivateTool(ViviendaToolIds.WallPaint);host?.WallPaint?.SelectColorIndex(index);SetVisible(false);}
  void PlayClick(){m_ClickSound?.PlayClick();ViviendaToolHost.Instance?.SuppressInput();}
  public void WireButton(Button button,UnityAction action){if(button!=null&&action!=null)button.onClick.AddListener(action);}
 }
}
