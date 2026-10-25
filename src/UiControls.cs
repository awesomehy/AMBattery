using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Windows.Forms;

namespace AmBattery {
 public sealed class UiArtwork:UiControl {
  protected override void OnPaint(PaintEventArgs e){MouseArtwork.Draw(e.Graphics,ClientRectangle);}
 }
 public sealed class UiBrandArtwork:UiControl {
  public BrandMark Mark;
  protected override void OnPaint(PaintEventArgs e){BrandArtwork.Draw(e.Graphics,ClientRectangle,Mark);}
 }
 public sealed class UiDisplayIcon:UiControl {
  protected override void OnPaint(PaintEventArgs e){var g=e.Graphics;g.SmoothingMode=SmoothingMode.AntiAlias;float s=UiTheme.Scale(this),x=2*s,y=3*s,w=20*s,h=14*s;using(var p=new Pen(UiTheme.Muted,1.3f*s)){using(var path=UiPaint.Round(new RectangleF(x,y,w,h),2*s))g.DrawPath(p,path);g.DrawLine(p,x+w/2,y+h,x+w/2,y+h+4*s);g.DrawLine(p,x+5*s,y+h+5*s,x+w-5*s,y+h+5*s);}}
 }
 public sealed class UiButton:UiControl {
  public bool Primary,Quiet,CloseGlyph;
  public UiButton(){TabStop=true;Cursor=Cursors.Hand;AccessibleRole=AccessibleRole.PushButton;Size=new Size(100,36);}
  public void PerformClick(){if(Enabled)OnClick(EventArgs.Empty);}
  protected override void OnKeyDown(KeyEventArgs e){if(e.KeyCode==Keys.Space||e.KeyCode==Keys.Enter){PerformClick();e.Handled=true;}base.OnKeyDown(e);}
  protected override void OnEnabledChanged(EventArgs e){Invalidate();base.OnEnabledChanged(e);}
  protected override void OnPaint(PaintEventArgs e){var g=e.Graphics;g.SmoothingMode=SmoothingMode.AntiAlias;float scale=UiTheme.Scale(this);var rect=new RectangleF(.5f,.5f,Width-1,Height-1);Color fill=Primary?UiTheme.Accent:Hovered?UiTheme.Hover:UiTheme.Field;if(!Enabled)fill=UiTheme.Field;if(!Quiet||Hovered||Primary)UiPaint.Fill(g,rect,8*scale,fill);if(!Primary&&!Quiet)UiPaint.Stroke(g,rect,8*scale,UiTheme.Border);Color ink=!Enabled?UiTheme.Muted:Primary?Color.White:Quiet?UiTheme.Muted:UiTheme.Text;if(CloseGlyph){float x=Width/2f,y=Height/2f,r=4*scale;using(var pen=new Pen(ink,1.4f*scale)){g.DrawLine(pen,x-r,y-r,x+r,y+r);g.DrawLine(pen,x+r,y-r,x-r,y+r);}}else UiPaint.Text(g,Text,Font,ClientRectangle,ink,TextFormatFlags.HorizontalCenter);if(Focused&&ShowFocusCues)UiPaint.Stroke(g,new RectangleF(2,2,Width-5,Height-5),6*scale,UiTheme.Accent);}
 }
 public sealed class UiSwitch:UiControl {
  bool selected;float position;readonly Timer timer;
  public event EventHandler CheckedChanged;
  public bool Checked {get{return selected;}set{if(selected==value)return;selected=value;if(IsHandleCreated&&Visible)timer.Start();else{position=value?1:0;timer.Stop();}Invalidate();AccessibilityNotifyClients(AccessibleEvents.StateChange,-1);if(CheckedChanged!=null)CheckedChanged(this,EventArgs.Empty);}}
  public UiSwitch(){Size=new Size(50,32);TabStop=true;Cursor=Cursors.Hand;AccessibleRole=AccessibleRole.CheckButton;timer=new Timer{Interval=15};timer.Tick+=delegate{float target=selected?1:0;position+=(target-position)*.38f;if(Math.Abs(target-position)<.015f){position=target;timer.Stop();}Invalidate();};}
  protected override void OnClick(EventArgs e){if(Enabled)Checked=!Checked;base.OnClick(e);}
  protected override void OnKeyDown(KeyEventArgs e){if(e.KeyCode==Keys.Space||e.KeyCode==Keys.Enter){OnClick(EventArgs.Empty);e.Handled=true;}base.OnKeyDown(e);}
  protected override void OnPaint(PaintEventArgs e){var g=e.Graphics;g.SmoothingMode=SmoothingMode.AntiAlias;float s=UiTheme.Scale(this);var r=new RectangleF(3*s,3*s,Width-6*s,Height-6*s);Color off=UiTheme.SwitchOff,on=Color.FromArgb(52,199,89);var fill=Color.FromArgb((int)(off.R+(on.R-off.R)*position),(int)(off.G+(on.G-off.G)*position),(int)(off.B+(on.B-off.B)*position));UiPaint.Fill(g,r,r.Height/2,Enabled?fill:UiTheme.SwitchOff);float size=r.Height-4*s,x=r.Left+2*s+(r.Width-r.Height)*position;using(var shadow=new SolidBrush(Color.FromArgb(28,0,0,0)))g.FillEllipse(shadow,x,r.Top+3*s,size,size);using(var brush=new SolidBrush(Color.White))g.FillEllipse(brush,x,r.Top+2*s,size,size);if(Focused&&ShowFocusCues)UiPaint.Stroke(g,new RectangleF(.5f,.5f,Width-1,Height-1),Height/2,UiTheme.Accent);}
  protected override void Dispose(bool disposing){if(disposing)timer.Dispose();base.Dispose(disposing);}
 }
 public sealed class UiThemePicker:UiControl {
  public event EventHandler SelectionChanged;
  public UiThemePicker(){Size=new Size(278,42);TabStop=true;Cursor=Cursors.Hand;AccessibleRole=AccessibleRole.List;}
  protected override void OnMouseClick(MouseEventArgs e){string mode=e.X<Width/2?"Light":"Dark";Choose(mode);base.OnMouseClick(e);}
  void Choose(string mode){UiTheme.Set(mode);if(SelectionChanged!=null)SelectionChanged(this,EventArgs.Empty);}
  protected override void OnKeyDown(KeyEventArgs e){if(e.KeyCode==Keys.Left||e.KeyCode==Keys.Right||e.KeyCode==Keys.Space){Choose(e.KeyCode==Keys.Left?"Light":e.KeyCode==Keys.Right?"Dark":UiTheme.Dark?"Light":"Dark");e.Handled=true;}base.OnKeyDown(e);}
  protected override void OnPaint(PaintEventArgs e){var g=e.Graphics;g.SmoothingMode=SmoothingMode.AntiAlias;float s=UiTheme.Scale(this);UiPaint.Fill(g,new RectangleF(0,0,Width,Height),10*s,UiTheme.Field);float half=Width/2f;UiPaint.Fill(g,new RectangleF((UiTheme.Dark?half:0)+3*s,3*s,half-6*s,Height-6*s),8*s,UiTheme.Card);for(int i=0;i<2;i++){int x=(int)(i*half);Color ink=(i==0&&!UiTheme.Dark)||(i==1&&UiTheme.Dark)?UiTheme.Text:UiTheme.Muted;using(var pen=new Pen(ink,1.35f*s)){float cx=x+25*s,cy=Height/2f;if(i==0){g.DrawEllipse(pen,cx-4*s,cy-4*s,8*s,8*s);for(int a=0;a<8;a++){double angle=a*Math.PI/4;g.DrawLine(pen,cx+(float)Math.Cos(angle)*6*s,cy+(float)Math.Sin(angle)*6*s,cx+(float)Math.Cos(angle)*8*s,cy+(float)Math.Sin(angle)*8*s);}}else {g.DrawArc(pen,cx-6*s,cy-6*s,12*s,12*s,50,275);g.DrawArc(pen,cx-1*s,cy-8*s,10*s,12*s,95,155);}}UiPaint.Text(g,i==0?"简约 · 明亮":"简约 · 深色",Font,new Rectangle(x+(int)(41*s),0,(int)half-(int)(45*s),Height),ink);}}
 }
 public sealed class UiSelect:UiControl {
  public readonly List<object> Items=new List<object>();
  int selected=-1;ToolStripDropDown popup;
  public event EventHandler SelectedIndexChanged;
  public int SelectedIndex {get{return selected;}set{if(value<-1||value>=Items.Count)throw new ArgumentOutOfRangeException("value");if(selected==value)return;selected=value;Text=SelectedItem==null?"":SelectedItem.ToString();Invalidate();if(SelectedIndexChanged!=null)SelectedIndexChanged(this,EventArgs.Empty);}}
  public object SelectedItem {get{return selected<0||selected>=Items.Count?null:Items[selected];}set{SelectedIndex=Items.IndexOf(value);}}
  public UiSelect(){Size=new Size(160,36);TabStop=true;Cursor=Cursors.Hand;AccessibleRole=AccessibleRole.ComboBox;}
  protected override void OnClick(EventArgs e){if(Enabled){if(popup!=null&&popup.Visible)popup.Close();else OpenMenu();}base.OnClick(e);}
  protected override void OnKeyDown(KeyEventArgs e){if(e.KeyCode==Keys.Space||e.KeyCode==Keys.F4||e.KeyCode==Keys.Enter){OpenMenu();e.Handled=true;}else if(e.KeyCode==Keys.Up||e.KeyCode==Keys.Down){SelectedIndex=Math.Max(0,Math.Min(Items.Count-1,selected+(e.KeyCode==Keys.Up?-1:1)));e.Handled=true;}base.OnKeyDown(e);}
  public void OpenMenu(){if(!Enabled||Items.Count==0)return;if(popup!=null)popup.Dispose();float scale=UiTheme.Scale(this);int height=(int)(Items.Count*38*scale+12*scale);var list=new UiOptionList(this,(int)(38*scale)){Size=new Size(Width,height)};popup=new ToolStripDropDown{AutoSize=false,Size=list.Size,Padding=Padding.Empty,Margin=Padding.Empty,DropShadowEnabled=true,BackColor=UiTheme.Card};var host=new ToolStripControlHost(list){AutoSize=false,Size=list.Size,Padding=Padding.Empty,Margin=Padding.Empty};popup.Items.Add(host);popup.Closed+=delegate{Invalidate();};using(var path=UiPaint.Round(new RectangleF(0,0,Width,height),10*scale))popup.Region=new Region(path);popup.Show(this,new Point(0,Height+(int)(6*scale)));list.Focus();Invalidate();}
  internal void SelectAndClose(int index){SelectedIndex=index;if(popup!=null)popup.Close();Focus();}
  protected override void OnEnabledChanged(EventArgs e){Invalidate();base.OnEnabledChanged(e);}
  protected override void OnPaint(PaintEventArgs e){var g=e.Graphics;g.SmoothingMode=SmoothingMode.AntiAlias;float s=UiTheme.Scale(this);UiPaint.Fill(g,new RectangleF(.5f,.5f,Width-1,Height-1),8*s,Hovered&&Enabled?UiTheme.Hover:UiTheme.Field);UiPaint.Stroke(g,new RectangleF(.5f,.5f,Width-1,Height-1),8*s,(Focused||popup!=null&&popup.Visible)&&Enabled?UiTheme.Accent:UiTheme.Border);UiPaint.Text(g,Text,Font,new Rectangle((int)(12*s),0,Width-(int)(42*s),Height),Enabled?UiTheme.Text:UiTheme.Muted);UiPaint.Chevron(g,new RectangleF(Width-25*s,Height/2f-2*s,8*s,4*s),UiTheme.Muted);}
  protected override void Dispose(bool disposing){if(disposing&&popup!=null)popup.Dispose();base.Dispose(disposing);}
 }
 internal sealed class UiOptionList:UiControl {
  readonly UiSelect owner;readonly int row;int hovered;
  public UiOptionList(UiSelect owner,int row){this.owner=owner;this.row=row;hovered=owner.SelectedIndex;Font=owner.Font;TabStop=true;}
  protected override void OnMouseMove(MouseEventArgs e){int index=(e.Y-6)/row;if(index!=hovered){hovered=Math.Max(0,Math.Min(owner.Items.Count-1,index));Invalidate();}base.OnMouseMove(e);}
  protected override void OnMouseClick(MouseEventArgs e){int index=(e.Y-6)/row;if(index>=0&&index<owner.Items.Count)owner.SelectAndClose(index);base.OnMouseClick(e);}
  protected override void OnKeyDown(KeyEventArgs e){if(e.KeyCode==Keys.Up||e.KeyCode==Keys.Down){hovered=Math.Max(0,Math.Min(owner.Items.Count-1,hovered+(e.KeyCode==Keys.Up?-1:1)));Invalidate();e.Handled=true;}else if(e.KeyCode==Keys.Enter||e.KeyCode==Keys.Space){owner.SelectAndClose(hovered);e.Handled=true;}else if(e.KeyCode==Keys.Escape){FindForm();owner.SelectAndClose(owner.SelectedIndex);e.Handled=true;}base.OnKeyDown(e);}
  protected override void OnPaint(PaintEventArgs e){var g=e.Graphics;g.Clear(UiTheme.Card);g.SmoothingMode=SmoothingMode.AntiAlias;float s=UiTheme.Scale(owner);for(int i=0;i<owner.Items.Count;i++){var rect=new Rectangle(6,6+i*row,Width-12,row);if(i==hovered||i==owner.SelectedIndex)UiPaint.Fill(g,rect,6*s,i==owner.SelectedIndex?UiTheme.Selected:UiTheme.Hover);UiPaint.Text(g,owner.Items[i].ToString(),Font,new Rectangle(rect.Left+(int)(10*s),rect.Top,rect.Width-(int)(42*s),rect.Height),UiTheme.Text);if(i==owner.SelectedIndex)UiPaint.Check(g,new RectangleF(rect.Right-23*s,rect.Top+row/2f-4*s,10*s,8*s),UiTheme.Accent);}}
 }
 public sealed class UiStepper:UiControl {
  decimal value;
  public decimal Minimum,Maximum=1500;
  public decimal Value {get{return value;}set{decimal next=Math.Max(Minimum,Math.Min(Maximum,value));if(this.value==next)return;this.value=next;Invalidate();}}
  public UiStepper(){Size=new Size(142,36);TabStop=true;AccessibleRole=AccessibleRole.SpinButton;Cursor=Cursors.Hand;AccessibleName="向内偏移，单位像素";}
  protected override void OnMouseClick(MouseEventArgs e){if(Enabled)Value+=e.X<Width/2?-1:1;base.OnMouseClick(e);}
  protected override void OnMouseWheel(MouseEventArgs e){if(Enabled)Value+=e.Delta>0?1:-1;base.OnMouseWheel(e);}
  protected override void OnKeyDown(KeyEventArgs e){if(e.KeyCode==Keys.Left||e.KeyCode==Keys.Down||e.KeyCode==Keys.Right||e.KeyCode==Keys.Up){Value+=e.KeyCode==Keys.Left||e.KeyCode==Keys.Down?-1:1;e.Handled=true;}else if(e.KeyCode==Keys.Home){Value=Minimum;e.Handled=true;}else if(e.KeyCode==Keys.End){Value=Maximum;e.Handled=true;}base.OnKeyDown(e);}
  protected override void OnPaint(PaintEventArgs e){var g=e.Graphics;g.SmoothingMode=SmoothingMode.AntiAlias;float s=UiTheme.Scale(this);UiPaint.Fill(g,new RectangleF(.5f,.5f,Width-1,Height-1),8*s,UiTheme.Field);UiPaint.Stroke(g,new RectangleF(.5f,.5f,Width-1,Height-1),8*s,Focused?UiTheme.Accent:UiTheme.Border);float cy=Height/2f;using(var pen=new Pen(UiTheme.Muted,1.3f*s)){g.DrawLine(pen,12*s,cy,20*s,cy);g.DrawLine(pen,Width-20*s,cy,Width-12*s,cy);g.DrawLine(pen,Width-16*s,cy-4*s,Width-16*s,cy+4*s);}using(var font=UiTheme.Font(11*s))UiPaint.Text(g,"偏移  "+Value+" px",font,new Rectangle((int)(27*s),0,Width-(int)(54*s),Height),UiTheme.Text,TextFormatFlags.HorizontalCenter);}
 }
 public sealed class UiMenuRenderer:ToolStripProfessionalRenderer {
  protected override void OnRenderToolStripBackground(ToolStripRenderEventArgs e){e.Graphics.Clear(UiTheme.Card);}
  protected override void OnRenderToolStripBorder(ToolStripRenderEventArgs e){using(var pen=new Pen(UiTheme.Border))e.Graphics.DrawRectangle(pen,0,0,e.ToolStrip.Width-1,e.ToolStrip.Height-1);}
  protected override void OnRenderMenuItemBackground(ToolStripItemRenderEventArgs e){if(e.Item.Selected)UiPaint.Fill(e.Graphics,new Rectangle(4,1,e.Item.Width-8,e.Item.Height-2),5,UiTheme.Hover);}
  protected override void OnRenderItemText(ToolStripItemTextRenderEventArgs e){e.TextColor=e.Item.Enabled?UiTheme.Text:UiTheme.Muted;base.OnRenderItemText(e);}
  protected override void OnRenderSeparator(ToolStripSeparatorRenderEventArgs e){using(var pen=new Pen(UiTheme.Border))e.Graphics.DrawLine(pen,12,e.Item.Height/2,e.Item.Width-12,e.Item.Height/2);}
 }
}
