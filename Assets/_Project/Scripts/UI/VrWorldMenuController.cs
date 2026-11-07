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
  GameObject mainPanel,palettePanel,reopenHint;
  TextMeshProUGUI status,controls;
  TMP_FontAsset menuFont;
  string lastDestination="";
  ProyectoVR.DayNightController environment;
  public static VrWorldMenuController Instance {get;private set;}
  public static bool AnyMenuVisible => Instance!=null&&Instance.IsVisible;
  public bool IsVisible => m_Canvas!=null&&m_Canvas.gameObject.activeSelf;
  public int ColorButtonCount {get;private set;}
  void Awake(){Instance=this;BuildMenu();}
  IEnumerator Start(){yield return null;SetVisible(!m_StartHidden);}
  void OnDestroy(){if(reopenHint!=null)Destroy(reopenHint);if(Instance==this)Instance=null;}
  RectTransform Rect(string name,Transform parent,Vector2 size,Vector2 position){var go=new GameObject(name,typeof(RectTransform));var r=go.GetComponent<RectTransform>();r.SetParent(parent,false);r.anchorMin=r.anchorMax=r.pivot=new Vector2(.5f,.5f);r.sizeDelta=size;r.anchoredPosition=position;return r;}
  TextMeshProUGUI Text(string name,Transform parent,string text,int size,Vector2 dimensions,Vector2 position,Color color){var r=Rect(name,parent,dimensions,position);var t=r.gameObject.AddComponent<TextMeshProUGUI>();if(menuFont==null)menuFont=Resources.Load<TMP_FontAsset>("Vivienda UI SDF");t.font=menuFont!=null?menuFont:TMP_Settings.defaultFontAsset;t.text=text;t.fontSize=size;t.color=color;t.alignment=TextAlignmentOptions.Center;t.raycastTarget=false;return t;}
  Button MakeButton(string name,Transform parent,string label,Vector2 position,Vector2 size,UnityAction action,Color? tint=null){
   var r=Rect(name,parent,size,position);var im=r.gameObject.AddComponent<VrGlassSurface>();im.color=tint??new Color(.25f,.30f,.39f,.92f);
   var button=r.gameObject.AddComponent<Button>();var colors=button.colors;colors.highlightedColor=new Color(1.35f,1.45f,1.55f,1);colors.selectedColor=Color.white;colors.pressedColor=new Color(.65f,.85f,1,1);colors.fadeDuration=.12f;button.colors=colors;button.targetGraphic=im;button.onClick.AddListener(action);r.gameObject.AddComponent<VrGlassMotion>();
   var c=im.color;var textColor=(c.r*.3f+c.g*.59f+c.b*.11f)>.6f?new Color(.05f,.08f,.12f):Color.white;
   Text("Label",r,label,25,size-new Vector2(24,10),Vector2.zero,textColor);return button;
  }
  void Icon(Transform parent,string kind,Vector2 position,float size){var r=Rect("Icon",parent,new Vector2(size,size),position);var icon=r.gameObject.AddComponent<VrMenuIcon>();icon.Kind=kind;icon.raycastTarget=false;icon.color=new Color(.88f,.94f,1);}
  void Chip(string label,Vector2 position,float width){var r=Rect(label,mainPanel.transform,new Vector2(width,33),position);var surface=r.gameObject.AddComponent<VrGlassSurface>();surface.Radius=17;surface.color=new Color(.58f,.65f,.73f,.22f);surface.raycastTarget=false;Text("Label",r,label,17,new Vector2(width,30),Vector2.zero,new Color(.8f,.86f,.93f));}
  void Tray(string name,Vector2 position,Vector2 size){var r=Rect(name,mainPanel.transform,size,position);var surface=r.gameObject.AddComponent<VrGlassSurface>();surface.Radius=30;surface.color=new Color(.025f,.04f,.06f,.6f);surface.raycastTarget=false;}
  void DecorateButtons(){
   foreach(var button in mainPanel.GetComponentsInChildren<Button>()){
    string n=button.name;var surface=button.GetComponent<VrGlassSurface>();
    if(n=="Exterior"||n=="Piso1"||n=="Piso2"||n=="Dia"||n=="Noche"){surface.color=new Color(1,1,1,.02f);surface.Border=1.5f;}
    if(n=="Medir"||n=="Pintar"){
     var tile=Rect("IconTile",button.transform,new Vector2(58,58),new Vector2(-188,0));var glass=tile.gameObject.AddComponent<VrGlassSurface>();glass.color=new Color(.7f,.8f,1,.25f);glass.Radius=18;glass.raycastTarget=false;Icon(tile,n,Vector2.zero,30);
     var label=button.GetComponentInChildren<TextMeshProUGUI>();label.alignment=TextAlignmentOptions.Left;label.rectTransform.sizeDelta=new Vector2(310,76);label.rectTransform.anchoredPosition=new Vector2(24,0);
    }
    else if(n=="Exterior"||n=="Dia"||n=="Noche"){Icon(button.transform,n=="Exterior"?"Home":n,new Vector2(n=="Exterior"?-100:-65,0),25);}
    else if(n=="SinHerramienta"||n=="Deshacer"||n=="Restaurar") {Icon(button.transform,n,new Vector2(-128,0),23);var label=button.GetComponentInChildren<TextMeshProUGUI>();label.fontSize=22;label.rectTransform.anchoredPosition=new Vector2(12,0);label.rectTransform.sizeDelta=new Vector2(260,50);}
    else if(n=="Continuar")Icon(button.transform,n,new Vector2(195,0),27);
   }
  }
  void ShowControls(int mode){
   if(controls!=null)controls.text=mode==0?"Y / Men\u00fa   Panel   |   Grip   Puertas   |   Gatillo   Seleccionar":mode==1?"M / Esc   Men\u00fa   |   WASD   Mover   |   E / Espacio   Interactuar   |   Clic der.   Mirar":"Esc   Men\u00fa   |   WASD   Caminar   |   G / T   Agarre / gatillo";
   for(int i=0;i<3;i++){var t=mainPanel.transform.Find("Ayuda_"+i);if(t!=null){var g=t.GetComponent<VrGlassSurface>();g.Selected=i==mode;g.color=i==mode?Color.white:new Color(1,1,1,.04f);g.SetVerticesDirty();}}
  }
  void Update(){
   if(!IsVisible||mainPanel==null)return;
   foreach(string n in StateButtons){
    var t=mainPanel.transform.Find(n);if(t==null)continue;var g=t.GetComponent<VrGlassSurface>();
    bool selected=n==lastDestination||(environment!=null&&(n=="Dia"&&!environment.IsNight||n=="Noche"&&environment.IsNight));
    string tool=ViviendaToolHost.Instance?.ActiveToolId;
    bool activeTool=n=="Medir"&&tool==ViviendaToolIds.Measure||n=="Pintar"&&tool==ViviendaToolIds.WallPaint;
    bool isTool=n=="Medir"||n=="Pintar";
    var tint=selected||activeTool?Color.white:isTool?new Color(.35f,.40f,.48f,.8f):new Color(1,1,1,.02f);
    if(g.Selected!=selected||g.Accent!=activeTool||g.color!=tint){g.Selected=selected;g.Accent=activeTool;g.color=tint;g.SetVerticesDirty();}
   }
  }
  static readonly string[] StateButtons={"Exterior","Piso1","Piso2","Dia","Noche","Medir","Pintar"};
  void SectionLabel(string label,Transform parent,float y){
   var text=Text(label,parent,label.ToUpperInvariant(),17,new Vector2(950,28),new Vector2(0,y),new Color(.66f,.76f,.86f));
   text.alignment=TextAlignmentOptions.Left;text.characterSpacing=2;
  }
  public void BuildMenu(){
   if(m_Canvas!=null){m_Canvas.gameObject.SetActive(false);Destroy(m_Canvas.gameObject);}
   var root=Rect("Menu Journey",transform,new Vector2(1080,860),Vector2.zero);root.localScale=Vector3.one*.0012f;
   m_Canvas=root.gameObject.AddComponent<Canvas>();m_Canvas.renderMode=RenderMode.WorldSpace;m_Canvas.worldCamera=FindAnyObjectByType<XROrigin>()?.Camera;m_Canvas.sortingOrder=100;
   root.gameObject.AddComponent<GraphicRaycaster>();root.gameObject.AddComponent<TrackedDeviceGraphicRaycaster>();
   var bg=root.gameObject.AddComponent<VrGlassSurface>();bg.Radius=46;bg.Border=2;bg.color=new Color(.105f,.125f,.17f,.94f);root.gameObject.AddComponent<VrGlassMotion>().IsCard=true;
   mainPanel=Rect("Inicio",root,new Vector2(1080,860),Vector2.zero).gameObject;
   var logo=Rect("Logo",mainPanel.transform,new Vector2(70,70),new Vector2(-440,332));
   var logoSurface=logo.gameObject.AddComponent<VrGlassSurface>();logoSurface.Accent=true;logoSurface.Radius=21;logoSurface.raycastTarget=false;
   Icon(logo,"Home",Vector2.zero,36);
   var title=Text("Title",mainPanel.transform,"<b>Vivienda</b> <color=#BDCDDF>| Recorrido VR</color>",36,new Vector2(830,58),new Vector2(28,348),Color.white);
   title.alignment=TextAlignmentOptions.Left;
   Chip("Dos apartamentos",new Vector2(-270,299),190);
   Chip("Escala real",new Vector2(-103,299),120);
   Tray("Ubicacion",new Vector2(0,171),new Vector2(970,84));
   Tray("Ambiente",new Vector2(0,59),new Vector2(970,80));
   SectionLabel("Ubicaci\u00f3n",mainPanel.transform,224);
   MakeButton("Exterior",mainPanel.transform,"Exterior",new Vector2(-324,171),new Vector2(310,72),TeleportExterior);
   MakeButton("Piso1",mainPanel.transform,"1   Primer piso",new Vector2(0,171),new Vector2(310,72),TeleportPiso1);
   MakeButton("Piso2",mainPanel.transform,"2   Segundo piso",new Vector2(324,171),new Vector2(310,72),TeleportPiso2);
   SectionLabel("Ambiente",mainPanel.transform,110);
   MakeButton("Dia",mainPanel.transform,"D\u00eda",new Vector2(-244,59),new Vector2(472,68),SetDayMode);
   MakeButton("Noche",mainPanel.transform,"Noche",new Vector2(244,59),new Vector2(472,68),SetNightMode);
   SectionLabel("Herramientas",mainPanel.transform,-2);
   MakeButton("Medir",mainPanel.transform,"Medir distancia\n<size=18><color=#BDCDDF>Apunta a dos puntos</color></size>",new Vector2(-244,-64),new Vector2(472,88),ToggleMeasureTool);
   MakeButton("Pintar",mainPanel.transform,"Pintar paredes\n<size=18><color=#BDCDDF>Elige un acabado</color></size>",new Vector2(244,-64),new Vector2(472,88),OpenWallPalette);
   MakeButton("SinHerramienta",mainPanel.transform,"Guardar herramienta",new Vector2(-324,-153),new Vector2(310,60),()=>{PlayClick();ViviendaToolHost.Instance?.DeactivateAll();});
   MakeButton("Deshacer",mainPanel.transform,"Deshacer medida",new Vector2(0,-153),new Vector2(310,60),()=>{PlayClick();ViviendaToolHost.Instance?.Measure?.UndoLast();});
   var restore=MakeButton("Restaurar",mainPanel.transform,"Restaurar paredes",new Vector2(324,-153),new Vector2(310,60),()=>{PlayClick();ViviendaToolHost.Instance?.WallPaint?.RestoreAllPainted();});
   restore.GetComponentInChildren<TextMeshProUGUI>().color=new Color(1,.76f,.75f);
   status=Text("Estado",mainPanel.transform,"Arrastra desde la cabecera o el borde para mover el panel.",20,new Vector2(960,52),new Vector2(0,-215),new Color(.70f,.80f,.88f));
   var resume=MakeButton("Continuar",mainPanel.transform,"Continuar recorrido",new Vector2(0,-279),new Vector2(960,72),Close,Color.white);
   var surface=resume.GetComponent<VrGlassSurface>();surface.Accent=true;surface.Radius=36;surface.SetVerticesDirty();
   resume.GetComponentInChildren<TextMeshProUGUI>().color=Color.white;
   Tray("Ayuda",new Vector2(-300,-349),new Vector2(360,46));
   string[] modes={"Visor","Escritorio","Simulador"};
   for(int i=0;i<3;i++){int mode=i;var help=MakeButton("Ayuda_"+i,mainPanel.transform,modes[i],new Vector2(-420+i*120,-349),new Vector2(116,40),()=>ShowControls(mode));help.GetComponentInChildren<TextMeshProUGUI>().fontSize=18;}
   controls=Text("Controles",mainPanel.transform,"",18,new Vector2(960,48),new Vector2(0,-397),new Color(.78f,.84f,.90f));
   controls.alignment=TextAlignmentOptions.Left;ShowControls(2);
   DecorateButtons();
   environment=FindAnyObjectByType<ProyectoVR.DayNightController>();
   palettePanel=Rect("Paleta",root,new Vector2(1080,860),Vector2.zero).gameObject;
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
   var follow=GetComponent<VrMenuFollowHead>();if(follow!=null)follow.enabled=false;
   if(reopenHint==null){
    var head=FindAnyObjectByType<XROrigin>()?.Camera;
    if(head!=null){var hint=Rect("Ayuda abrir menu",head.transform,new Vector2(1000,60),Vector2.zero);hint.localPosition=new Vector3(0,-.35f,1);hint.localRotation=Quaternion.identity;hint.localScale=Vector3.one*.0006f;
     var hintCanvas=hint.gameObject.AddComponent<Canvas>();hintCanvas.renderMode=RenderMode.WorldSpace;hintCanvas.worldCamera=head;hintCanvas.sortingOrder=101;
     var surface=hint.gameObject.AddComponent<VrGlassSurface>();surface.color=new Color(.09f,.12f,.17f,.90f);surface.Radius=28;surface.raycastTarget=false;
     string command=ProyectoVR.DesktopPreview.IsSimulatorActive?"Esc":ProyectoVR.DesktopPreview.IsActive?"M / Esc":"Y / Men\u00fa";
     Text("Recordatorio",hint,"Abrir men\u00fa: "+command,25,new Vector2(960,54),Vector2.zero,Color.white);reopenHint=hint.gameObject;}
   }
   if(reopenHint!=null)reopenHint.SetActive(!visible);
   if(visible){var camera=FindAnyObjectByType<XROrigin>()?.Camera;if(camera!=null){var forward=Vector3.ProjectOnPlane(camera.transform.forward,Vector3.up);if(forward.sqrMagnitude<.0001f)forward=Vector3.forward;forward.Normalize();transform.SetPositionAndRotation(camera.transform.position+forward*1.25f-Vector3.up*.08f,Quaternion.LookRotation(forward,Vector3.up));}if(mainPanel!=null)mainPanel.SetActive(true);if(palettePanel!=null)palettePanel.SetActive(false);}
   m_Canvas.gameObject.SetActive(visible);ViviendaToolHost.Instance?.SuppressInput();
  }
  void Teleport(TeleportDestinationId id){lastDestination=id.ToString();PlayClick();m_Teleport?.TeleportTo(id);SetVisible(false);}
  public void TeleportExterior()=>Teleport(TeleportDestinationId.Exterior);
  public void TeleportPiso1()=>Teleport(TeleportDestinationId.Piso1);
  public void TeleportPiso2()=>Teleport(TeleportDestinationId.Piso2);
  public void SetDayMode(){PlayClick();EnvironmentCycleEvents.RequestNightMode(false);if(status!=null)status.text="Transici\u00f3n al d\u00eda";}
  public void SetNightMode(){PlayClick();EnvironmentCycleEvents.RequestNightMode(true);if(status!=null)status.text="Transici\u00f3n a la noche. Las luces de cada habitaci\u00f3n se controlan con sus interruptores.";}
  public void ToggleMeasureTool(){PlayClick();ViviendaToolHost.Instance?.ActivateTool(ViviendaToolIds.Measure);SetVisible(false);}
  public void OpenWallPalette(){PlayClick();mainPanel.SetActive(false);palettePanel.SetActive(true);}
  public void SelectColor(int index){PlayClick();var host=ViviendaToolHost.Instance;host?.ActivateTool(ViviendaToolIds.WallPaint);host?.WallPaint?.SelectColorIndex(index);SetVisible(false);}
  void PlayClick(){m_ClickSound?.PlayClick();ViviendaToolHost.Instance?.SuppressInput();}
  public void WireButton(Button button,UnityAction action){if(button!=null&&action!=null)button.onClick.AddListener(action);}
 }
}
