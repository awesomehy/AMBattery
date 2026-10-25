using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;
using System.Runtime.InteropServices;
using System.Windows.Forms;
using Microsoft.Win32;

namespace AmBattery {
 public static class Palette {
  static DateTime checkedSystemTheme=DateTime.MinValue;static bool systemLight;
  public static bool Light {get{return !UiTheme.Dark;}}
  public static bool SystemLight {get{if((DateTime.UtcNow-checkedSystemTheme).TotalSeconds>30){try{systemLight=Convert.ToInt32(Registry.GetValue(@"HKEY_CURRENT_USER\Software\Microsoft\Windows\CurrentVersion\Themes\Personalize","SystemUsesLightTheme",0))==1;}catch{}checkedSystemTheme=DateTime.UtcNow;}return systemLight;}}
  public static void ResetSystemTheme(){checkedSystemTheme=DateTime.MinValue;}
  public static Color Background {get{return UiTheme.Background;}}
  public static Color Foreground {get{return UiTheme.Text;}}
  public static Color Muted {get{return UiTheme.Muted;}}
  public static Color Track {get{return UiTheme.Border;}}
  public static Color Level(int? value){var band=BatteryRules.Band(value);return band=="Unknown"?Muted:band=="Red"?Color.FromArgb(231,91,78):band=="Yellow"?Color.FromArgb(226,187,49):Color.FromArgb(80,207,139);}
  public static Color SystemInk(bool light){return light?Color.FromArgb(31,36,45):Color.FromArgb(238,244,239);}
  public static void Ring(Graphics g,RectangleF area,int? value,float fontSize,bool systemSurface=false,bool? systemLight=null){
   g.SmoothingMode=SmoothingMode.AntiAlias;float line=Math.Max(2,area.Width/15);area.Inflate(-line/2-1,-line/2-1);
   bool light=systemLight??SystemLight;Color trackColor=systemSurface?(light?Color.FromArgb(211,216,221):Color.FromArgb(73,80,82)):Track,ink=systemSurface?SystemInk(light):Foreground;
   using(var track=new Pen(trackColor,line))g.DrawEllipse(track,area);
   if(value.HasValue&&value>0)using(var pen=new Pen(Level(value),line)){pen.StartCap=LineCap.Round;pen.EndCap=LineCap.Round;if(value==100)g.DrawEllipse(pen,area);else g.DrawArc(pen,area,-90,360f*value.Value/100);}
   using(var font=new Font("Segoe UI",fontSize,FontStyle.Regular,GraphicsUnit.Pixel))using(var brush=new SolidBrush(ink))using(var sf=new StringFormat{Alignment=StringAlignment.Center,LineAlignment=StringAlignment.Center})g.DrawString(value.HasValue?value+"%":"—",font,brush,area,sf);
  }
 }
 public sealed class TaskbarWidget:Form {
  public BatteryReading Reading=new BatteryReading();
  public Action OpenDetails;
  public IntPtr Host;
  public string LastError="";
  float scale=1;
  string presentedFrame="";
  IntPtr notificationArea;
  DateTime lastAreaScan=DateTime.MinValue;
  Rectangle lastPlacement=Rectangle.Empty;
  public TaskbarWidget(ContextMenuStrip menu){Text="AM Battery Taskbar";FormBorderStyle=FormBorderStyle.None;ShowInTaskbar=false;StartPosition=FormStartPosition.Manual;DoubleBuffered=true;ContextMenuStrip=menu;Cursor=Cursors.Hand;SetStyle(ControlStyles.Selectable,false);}
  protected override bool ShowWithoutActivation{get{return true;}}
  protected override CreateParams CreateParams {get{var c=base.CreateParams;c.ExStyle|=0x08080080;return c;}}
  public bool Place(Settings settings,TaskbarController controller){
   var target=settings.TargetScreen();IntPtr host=IntPtr.Zero;
   foreach(var bar in Native.Taskbars())if(Screen.FromHandle(bar).DeviceName==target.DeviceName){host=bar;break;}
   if(host==IntPtr.Zero){Hide();return false;}
   float dpi=Native.GetDpiForWindow(host);scale=Math.Max(.75f,dpi/96f);
   if(Host!=host){Host=host;var unused=Handle;Native.SetWindowLongPtr(Handle,-8,host);presentedFrame="";notificationArea=IntPtr.Zero;lastAreaScan=DateTime.MinValue;lastPlacement=Rectangle.Empty;}
   var rect=Native.Rect(host);int height=rect.Height,width=(int)(78*scale);
   int reserve=(int)(104*scale);
   if((DateTime.UtcNow-lastAreaScan).TotalSeconds>5){notificationArea=IntPtr.Zero;Native.EnumChildWindows(host,delegate(IntPtr child,IntPtr p){if(Native.Class(child)=="TrayNotifyWnd")notificationArea=child;return true;},IntPtr.Zero);lastAreaScan=DateTime.UtcNow;}
   if(notificationArea!=IntPtr.Zero&&Native.IsWindow(notificationArea))reserve=Native.Rect(notificationArea).Width;
   int x=settings.Position=="Left"?(int)(62*scale)+settings.Offset:rect.Width-reserve-width-settings.Offset;
   x=Math.Max(0,Math.Min(rect.Width-width,x));
   bool hidden=controller.IsHidden(host)||!Native.IsWindowVisible(host)||rect.Top>=target.Bounds.Bottom-3;
   var fg=Native.GetForegroundWindow();var foregroundClass=Native.Class(fg);var foregroundRect=Native.Rect(fg);
   if(fg!=IntPtr.Zero&&fg!=host&&!Native.IsZoomed(fg)&&foregroundClass!="Progman"&&foregroundClass!="WorkerW"&&foregroundRect.Contains(target.Bounds))hidden=true;
   if(hidden){Hide();return true;}
   int screenX=rect.Left+x,screenY=rect.Top;
   var placement=new Rectangle(screenX,screenY,width,height);if(lastPlacement!=placement||!Visible){Native.SetWindowPos(Handle,new IntPtr(-1),screenX,screenY,width,height,0x10|0x40);if(!Visible)Show();lastPlacement=placement;}
   if(Native.GetParent(Handle)!=host)Native.SetWindowLongPtr(Handle,-8,host);
   string key=width+":"+height+":"+scale+":"+Reading.Mouse+":"+Palette.SystemLight;
   if(key!=presentedFrame&&PresentFrame(screenX,screenY,width,height)){presentedFrame=key;LastError="";}
   return Native.GetParent(Handle)==host;
  }
  public string Status(){return "Renderer=OwnedLayered\r\nVisible="+Visible+"\r\nNativeVisible="+(IsHandleCreated&&Native.IsWindowVisible(Handle))+"\r\nHost="+Host+"\r\nBounds="+(IsHandleCreated?Native.Rect(Handle).ToString():"not created")+"\r\nMouse="+Reading.Mouse+"\r\nError="+LastError;}
  public static Bitmap RenderFrame(int width,int height,float scale,int? value,bool? systemLight=null){
   var bitmap=new Bitmap(width,height,PixelFormat.Format32bppPArgb);
   using(var graphics=Graphics.FromImage(bitmap)){
    // Alpha 1 keeps the whole compact button clickable while blending with the taskbar.
    graphics.Clear(Color.FromArgb(1,0,0,0));graphics.SmoothingMode=SmoothingMode.AntiAlias;graphics.TextRenderingHint=System.Drawing.Text.TextRenderingHint.AntiAliasGridFit;
    float size=Math.Min(38*scale,height-6),left=width-size-6*scale;
    bool light=systemLight??Palette.SystemLight;
    TaskbarMouseArtwork.Draw(graphics,new RectangleF(2*scale,(height-26*scale)/2,30*scale,26*scale),Palette.SystemInk(light));
    Palette.Ring(graphics,new RectangleF(left,(height-size)/2,size,size),value,10.5f*scale,true,light);
   }
   return bitmap;
  }
  bool PresentFrame(int x,int y,int width,int height){
   using(var bitmap=RenderFrame(width,height,scale,Reading.Mouse)){
    IntPtr screenDc=Native.GetDC(IntPtr.Zero),memoryDc=Native.CreateCompatibleDC(screenDc),dib=IntPtr.Zero,previous=IntPtr.Zero;
    try{
     dib=bitmap.GetHbitmap(Color.FromArgb(0));previous=Native.SelectObject(memoryDc,dib);
     var point=new Native.POINT(x,y);var source=new Native.POINT(0,0);var size=new Native.SIZE(width,height);var blend=new Native.BLENDFUNCTION{Alpha=255,Format=1};
     if(!Native.UpdateLayeredWindow(Handle,screenDc,ref point,ref size,memoryDc,ref source,0,ref blend,2)){LastError="电量组件绘制失败（"+Marshal.GetLastWin32Error()+"）";return false;}
     return true;
    }finally{if(previous!=IntPtr.Zero)Native.SelectObject(memoryDc,previous);if(dib!=IntPtr.Zero)Native.DeleteObject(dib);Native.DeleteDC(memoryDc);Native.ReleaseDC(IntPtr.Zero,screenDc);}
   }
  }
  protected override void OnMouseClick(MouseEventArgs e){base.OnMouseClick(e);if(e.Button==MouseButtons.Left&&OpenDetails!=null)OpenDetails();}
  protected override void OnPaint(PaintEventArgs e){}
 }
 public sealed class BatteryPopup:Form {
  public BatteryReading Reading=new BatteryReading();
  public Action OpenSettings,RefreshReading;
  public bool AllowClose;
  readonly UiButton settingsButton,refreshButton;
  public BatteryPopup(){Text="AM Battery · 电池详情";Icon=MouseArtwork.Icon;FormBorderStyle=FormBorderStyle.None;ShowInTaskbar=false;StartPosition=FormStartPosition.Manual;AutoScaleDimensions=new SizeF(96,96);AutoScaleMode=AutoScaleMode.Dpi;ClientSize=new Size(360,344);DoubleBuffered=true;BackColor=Palette.Background;ForeColor=Palette.Foreground;Font=UiTheme.Font(13);TopMost=true;
   settingsButton=new UiButton{Text="设置",Bounds=new Rectangle(278,299,64,32)};settingsButton.Click+=delegate{if(OpenSettings!=null)OpenSettings();};Controls.Add(settingsButton);
   refreshButton=new UiButton{Text="刷新",Quiet=true,Bounds=new Rectangle(208,299,64,32)};refreshButton.Click+=delegate{if(RefreshReading!=null)RefreshReading();};Controls.Add(refreshButton);
   UiTheme.Changed+=ThemeChanged;
  }
  void ThemeChanged(object sender,EventArgs e){BackColor=UiTheme.Background;ForeColor=UiTheme.Text;UiTheme.Window(this);Invalidate(true);}
  protected override void OnHandleCreated(EventArgs e){base.OnHandleCreated(e);UiTheme.Window(this);}
  protected override void OnDeactivate(EventArgs e){base.OnDeactivate(e);if(!IsDisposed&&!Disposing)Hide();}
  protected override void OnFormClosing(FormClosingEventArgs e){base.OnFormClosing(e);if(!AllowClose&&e.CloseReason==CloseReason.UserClosing){e.Cancel=true;Hide();}}
  protected override bool ProcessCmdKey(ref Message message,Keys key){if(key==Keys.Escape){Hide();return true;}return base.ProcessCmdKey(ref message,key);}
  public void OpenAt(Rectangle anchor,Screen screen){int x=Math.Max(screen.Bounds.Left+8,Math.Min(screen.Bounds.Right-Width-8,anchor.Right-Width));int y=anchor.Top-Height-10;if(y<screen.Bounds.Top)y=screen.Bounds.Top+8;Location=new Point(x,y);Show();Native.SetForegroundWindow(Handle);Invalidate();}
  protected override void OnPaint(PaintEventArgs e){base.OnPaint(e);var g=e.Graphics;g.SmoothingMode=SmoothingMode.AntiAlias;g.TextRenderingHint=System.Drawing.Text.TextRenderingHint.AntiAliasGridFit;float s=ClientSize.Width/360f;
   BrandArtwork.Draw(g,new RectangleF(19*s,16*s,50*s,38*s),BrandMark.AngryMiao);BrandArtwork.Draw(g,new RectangleF(236*s,10*s,104*s,49*s),BrandMark.NoWire);
   using(var title=new Font("Segoe UI Semibold",15*s,FontStyle.Regular,GraphicsUnit.Pixel))using(var ink=new SolidBrush(UiTheme.Text))g.DrawString("AM BATTERY",title,ink,86*s,17*s);
   using(var caption=UiTheme.Font(11*s))using(var muted=new SolidBrush(UiTheme.Muted))g.DrawString("设备电量",caption,muted,87*s,41*s);
   DrawDevice(g,s,72,"AM INFINITY","鼠标 · 2.4 GHz",Reading.Mouse,Reading.Mouse.HasValue?Reading.MouseState:"未连接",true);
   DrawDevice(g,s,180,"Dongle","接收器充电仓 · USB",Reading.Dock,Reading.Dock.HasValue?Reading.DockState:"未检测到电池",false);
   using(var brush=new SolidBrush(UiTheme.Muted))using(var f=UiTheme.Font(11*s))g.DrawString(Reading.Timestamp==default(DateTime)?"正在读取电量…":Reading.Connected?"更新于 "+Reading.Timestamp.ToString("HH:mm:ss"):Reading.Error,f,brush,new RectangleF(20*s,307*s,180*s,30*s));
  }
  void DrawDevice(Graphics g,float s,int y,string name,string caption,int? value,string state,bool mouse){UiPaint.Fill(g,new RectangleF(16*s,y*s,328*s,98*s),12*s,UiTheme.Card);if(mouse)MouseArtwork.Draw(g,new RectangleF(29*s,(y+19)*s,27*s,53*s));else DongleArtwork.Draw(g,new RectangleF(22*s,(y+27)*s,42*s,42*s));
   using(var ink=new SolidBrush(UiTheme.Text))using(var muted=new SolidBrush(UiTheme.Muted))using(var title=UiTheme.Font(14*s))using(var small=UiTheme.Font(11*s)){g.DrawString(name,title,ink,70*s,(y+16)*s);g.DrawString(caption,small,muted,70*s,(y+42)*s);using(var stateInk=new SolidBrush(state=="充电中"||state=="已充满"?Palette.Level(80):UiTheme.Muted))g.DrawString(state,small,stateInk,70*s,(y+66)*s);}Palette.Ring(g,new RectangleF(266*s,(y+18)*s,62*s,62*s),value,16*s);}
  protected override void Dispose(bool disposing){if(disposing)UiTheme.Changed-=ThemeChanged;base.Dispose(disposing);}
 }
}
