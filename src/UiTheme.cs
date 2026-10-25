using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Runtime.InteropServices;
using System.Windows.Forms;

namespace AmBattery {
 public static class UiTheme {
  static string mode="Dark";
  public static event EventHandler Changed;
  public static string Mode {get{return mode;}}
  public static bool Dark {get{return mode=="Dark";}}
  public static void Set(string value){value=value=="Light"?"Light":"Dark";if(mode==value)return;mode=value;if(Changed!=null)Changed(null,EventArgs.Empty);}
  public static Color Background {get{return Dark?Color.FromArgb(19,21,25):Color.FromArgb(245,246,248);}}
  public static Color Card {get{return Dark?Color.FromArgb(30,33,39):Color.White;}}
  public static Color Field {get{return Dark?Color.FromArgb(40,44,52):Color.FromArgb(245,246,249);}}
  public static Color Hover {get{return Dark?Color.FromArgb(48,53,62):Color.FromArgb(235,238,243);}}
  public static Color Text {get{return Dark?Color.FromArgb(237,240,246):Color.FromArgb(31,36,45);}}
  public static Color Muted {get{return Dark?Color.FromArgb(148,158,175):Color.FromArgb(115,125,141);}}
  public static Color Border {get{return Dark?Color.FromArgb(48,53,63):Color.FromArgb(231,234,240);}}
  public static Color Accent {get{return Dark?Color.FromArgb(85,145,255):Color.FromArgb(48,114,242);}}
  public static Color Selected {get{return Dark?Color.FromArgb(40,57,86):Color.FromArgb(232,240,255);}}
  public static Color Warning {get{return Dark?Color.FromArgb(239,186,103):Color.FromArgb(169,108,27);}}
  public static Color SwitchOff {get{return Dark?Color.FromArgb(68,74,85):Color.FromArgb(213,218,227);}}
  public static Font Font(float pixels,bool strong=false){return new Font("Microsoft YaHei UI",pixels,strong?FontStyle.Bold:FontStyle.Regular,GraphicsUnit.Pixel);}
  public static float Scale(Control control){try{return control.IsHandleCreated?Math.Max(.75f,Native.GetDpiForWindow(control.Handle)/96f):1;}catch{return 1;}}
  public static void Window(Form form){if(!form.IsHandleCreated)return;try{int corner=2,dark=Dark?1:0;DwmSetWindowAttribute(form.Handle,33,ref corner,4);DwmSetWindowAttribute(form.Handle,20,ref dark,4);}catch{}}
  [DllImport("dwmapi.dll")]static extern int DwmSetWindowAttribute(IntPtr hwnd,int attribute,ref int value,int size);
 }
 public static class UiPaint {
  public static GraphicsPath Round(RectangleF rect,float radius){var path=new GraphicsPath();float diameter=Math.Min(radius*2,Math.Min(rect.Width,rect.Height));if(diameter<1){path.AddRectangle(rect);return path;}path.AddArc(rect.Left,rect.Top,diameter,diameter,180,90);path.AddArc(rect.Right-diameter,rect.Top,diameter,diameter,270,90);path.AddArc(rect.Right-diameter,rect.Bottom-diameter,diameter,diameter,0,90);path.AddArc(rect.Left,rect.Bottom-diameter,diameter,diameter,90,90);path.CloseFigure();return path;}
  public static void Fill(Graphics graphics,RectangleF rect,float radius,Color color){using(var path=Round(rect,radius))using(var brush=new SolidBrush(color))graphics.FillPath(brush,path);}
  public static void Stroke(Graphics graphics,RectangleF rect,float radius,Color color){using(var path=Round(rect,radius))using(var pen=new Pen(color))graphics.DrawPath(pen,path);}
  public static void Text(Graphics graphics,string text,Font font,Rectangle bounds,Color color,TextFormatFlags extra=0){TextRenderer.DrawText(graphics,text,font,bounds,color,TextFormatFlags.Left|TextFormatFlags.VerticalCenter|TextFormatFlags.EndEllipsis|TextFormatFlags.NoPadding|extra);}
  public static void Chevron(Graphics g,RectangleF bounds,Color color){using(var pen=new Pen(color,1.5f)){pen.StartCap=LineCap.Round;pen.EndCap=LineCap.Round;g.DrawLines(pen,new[]{new PointF(bounds.Left,bounds.Top),new PointF(bounds.Left+bounds.Width/2,bounds.Bottom),new PointF(bounds.Right,bounds.Top)});}}
  public static void Check(Graphics g,RectangleF bounds,Color color){using(var pen=new Pen(color,1.7f)){pen.StartCap=LineCap.Round;pen.EndCap=LineCap.Round;g.DrawLines(pen,new[]{new PointF(bounds.Left,bounds.Top+bounds.Height*.5f),new PointF(bounds.Left+bounds.Width*.36f,bounds.Bottom),new PointF(bounds.Right,bounds.Top)});}}
 }
 public class UiControl:Control {
  protected bool Hovered;
  public UiControl(){SetStyle(ControlStyles.UserPaint|ControlStyles.AllPaintingInWmPaint|ControlStyles.OptimizedDoubleBuffer|ControlStyles.ResizeRedraw|ControlStyles.SupportsTransparentBackColor,true);BackColor=Color.Transparent;Font=UiTheme.Font(13);UiTheme.Changed+=OnThemeChanged;}
  protected virtual void OnThemeChanged(object sender,EventArgs e){Invalidate();}
  protected override void OnMouseEnter(EventArgs e){Hovered=true;Invalidate();base.OnMouseEnter(e);}
  protected override void OnMouseLeave(EventArgs e){Hovered=false;Invalidate();base.OnMouseLeave(e);}
  protected override void Dispose(bool disposing){if(disposing)UiTheme.Changed-=OnThemeChanged;base.Dispose(disposing);}
 }
 public sealed class UiCard:Panel {
  public int[] Dividers=new int[0];
  public UiCard(){DoubleBuffered=true;BackColor=UiTheme.Card;UiTheme.Changed+=ThemeChanged;}
  void ThemeChanged(object sender,EventArgs e){BackColor=UiTheme.Card;Invalidate(true);}
  protected override void OnPaintBackground(PaintEventArgs e){e.Graphics.Clear(Parent==null?UiTheme.Background:Parent.BackColor);e.Graphics.SmoothingMode=SmoothingMode.AntiAlias;float scale=UiTheme.Scale(this);UiPaint.Fill(e.Graphics,new RectangleF(.5f,.5f,Width-1,Height-1),14*scale,UiTheme.Card);UiPaint.Stroke(e.Graphics,new RectangleF(.5f,.5f,Width-1,Height-1),14*scale,UiTheme.Border);using(var pen=new Pen(UiTheme.Border))foreach(int y in Dividers)e.Graphics.DrawLine(pen,18*scale,y*scale,Width-18*scale,y*scale);}
  protected override void Dispose(bool disposing){if(disposing)UiTheme.Changed-=ThemeChanged;base.Dispose(disposing);}
 }
}
