using System;
using System.Collections.Generic;
using System.Drawing;
using System.Windows.Forms;

namespace AmBattery {
 public sealed class DisplayItem {
  public Screen Screen;public string Id;
  public DisplayItem(Screen s){Screen=s;Id=Native.DisplayId(s);}
  public override string ToString(){string text=Native.DisplayLabel(Screen);int last=text.LastIndexOf(" · ",StringComparison.Ordinal);return last>0?text.Substring(0,last):text;}
 }
 public sealed class SettingsForm:Form {
  readonly UiSwitch manage,alerts,startup;
  readonly UiSelect target,position;
  readonly UiStepper offset;
  readonly Dictionary<string,UiSelect> modes=new Dictionary<string,UiSelect>();
  readonly Dictionary<Label,bool> labels=new Dictionary<Label,bool>();
  readonly Label status;
  readonly UiButton notificationTest;
  readonly Settings original;
  public Action<Settings> ApplySettings;
  public Action RestoreTaskbars,TestNotification,OpenDetails,OpenNotificationSettings;
  public Action<string> ThemeChanged;
  public bool AllowClose;
  public SettingsForm(Settings settings){
   original=settings;UiTheme.Set(settings.Theme);Text="怒喵电量与任务栏";Icon=MouseArtwork.Icon;StartPosition=FormStartPosition.Manual;FormBorderStyle=FormBorderStyle.None;MaximizeBox=false;MinimizeBox=false;ShowInTaskbar=true;AutoScaleDimensions=new SizeF(96,96);AutoScaleMode=AutoScaleMode.Dpi;Font=UiTheme.Font(14);BackColor=UiTheme.Background;ForeColor=UiTheme.Text;DoubleBuffered=true;ClientSize=new Size(640,788);
   LabelText(this,"AM Battery",26,11,170,22,12,true);
   var close=Button(this,"",594,7,32,30);close.CloseGlyph=true;close.Quiet=true;close.AccessibleName="关闭设置";close.Click+=delegate{Hide();};
   Controls.Add(new UiBrandArtwork{Mark=BrandMark.AngryMiao,Bounds=new Rectangle(28,52,66,52),TabStop=false,AccessibleName="Angry Miao 品牌标志"});
   Controls.Add(new UiBrandArtwork{Mark=BrandMark.NoWire,Bounds=new Rectangle(470,49,142,66),TabStop=false,AccessibleName="NO WIRE BY ANGRYMIAO 品牌标志"});
   var brand=LabelText(this,"AM BATTERY",112,48,310,34,23,false);brand.Font=new Font("Segoe UI Semibold",23,FontStyle.Regular,GraphicsUnit.Pixel);
   LabelText(this,"鼠标电量与多屏任务栏  /  "+typeof(SettingsForm).Assembly.GetName().Version.ToString(3),113,84,332,22,12,true);
   var details=Button(this,"电池详情",490,7,92,30);details.Quiet=true;details.Click+=delegate{if(OpenDetails!=null)OpenDetails();};
   LabelText(this,"外观模式",28,129,180,30,14,false);
   var picker=new UiThemePicker{Bounds=new Rectangle(338,124,278,42),AccessibleName="外观模式：简约明亮、简约深色"};Controls.Add(picker);picker.SelectionChanged+=delegate{if(ThemeChanged!=null)ThemeChanged(UiTheme.Mode);};
   var screens=Screen.AllScreens;int taskHeight=66+screens.Length*60;
   var taskbars=Card(24,184,592,taskHeight);taskbars.Dividers=new int[screens.Length];for(int i=0;i<screens.Length;i++)taskbars.Dividers[i]=66+i*60;
   LabelText(taskbars,"任务栏管理",18,12,360,24,14,false);LabelText(taskbars,"分别控制每块屏幕的显示状态",18,36,380,20,12,true);
   manage=new UiSwitch{Bounds=new Rectangle(524,17,50,32),Checked=settings.ManageTaskbars,AccessibleName="控制各屏任务栏"};taskbars.Controls.Add(manage);
   int y=66;
   foreach(var screen in screens){var pref=settings.For(screen);taskbars.Controls.Add(new UiDisplayIcon{Bounds=new Rectangle(18,y+16,27,27),TabStop=false});LabelText(taskbars,new DisplayItem(screen).ToString(),56,y+9,340,24,14,false);LabelText(taskbars,screen.Bounds.Width+" × "+screen.Bounds.Height+"  ·  "+(screen.Bounds.Height>screen.Bounds.Width?"竖向显示":"横向显示"),56,y+34,340,20,12,true);var combo=new UiSelect{Bounds=new Rectangle(420,y+13,154,36),AccessibleName=new DisplayItem(screen)+"的任务栏模式"};combo.Items.AddRange(new object[]{"始终显示","自动隐藏"});combo.SelectedIndex=pref.Mode=="Auto"?1:0;taskbars.Controls.Add(combo);modes[Native.DisplayId(screen)]=combo;y+=60;}
   var placement=Card(24,taskbars.Bottom+16,592,126);placement.Dividers=new[]{62};
   LabelText(placement,"电量显示屏幕",18,18,195,24,14,false);
   target=new UiSelect{Bounds=new Rectangle(238,12,336,38),AccessibleName="电量组件显示屏幕"};foreach(var screen in screens){var item=new DisplayItem(screen);target.Items.Add(item);if(item.Id==settings.WidgetDisplay)target.SelectedItem=item;}if(target.SelectedIndex<0)target.SelectedIndex=target.Items.Count>1?1:0;placement.Controls.Add(target);
   LabelText(placement,"栏内位置",18,82,170,24,14,false);
   position=new UiSelect{Bounds=new Rectangle(220,76,192,36),AccessibleName="任务栏内的位置"};position.Items.AddRange(new object[]{"右侧 · 时钟左边","左侧 · 开始旁边"});position.SelectedIndex=settings.Position=="Left"?1:0;placement.Controls.Add(position);
   offset=new UiStepper{Bounds=new Rectangle(430,76,144,36),Value=settings.Offset};placement.Controls.Add(offset);
   var notices=Card(24,placement.Bottom+16,592,132);notices.Dividers=new[]{66};
   LabelText(notices,"低电量提醒",18,11,300,24,14,false);LabelText(notices,"鼠标电量低于 30% 时发送通知",18,35,330,21,12,true);
   alerts=new UiSwitch{Bounds=new Rectangle(524,17,50,32),Checked=settings.LowBatteryNotification,AccessibleName="低电量通知提醒"};notices.Controls.Add(alerts);
   notificationTest=Button(notices,"测试通知",414,17,90,32);notificationTest.Click+=delegate{if(TestNotification!=null)TestNotification();};
   LabelText(notices,"开机启动",18,77,300,24,14,false);LabelText(notices,"登录 Windows 后自动运行",18,101,330,21,12,true);
   startup=new UiSwitch{Bounds=new Rectangle(524,83,50,32),Checked=settings.StartWithWindows,AccessibleName="开机启动"};notices.Controls.Add(startup);
   var notifySettings=Button(this,"通知设置",522,notices.Bottom+17,94,30);notifySettings.Quiet=true;notifySettings.Click+=delegate{if(OpenNotificationSettings!=null)OpenNotificationSettings();};
   status=LabelText(this,"主屏自动隐藏，副屏常显；电量显示在所选任务栏。",28,notices.Bottom+16,482,40,12,true);status.AutoEllipsis=false;
   var restore=Button(this,"恢复系统任务栏",24,status.Bottom+14,142,38);restore.Quiet=true;restore.Click+=delegate{manage.Checked=false;if(RestoreTaskbars!=null)RestoreTaskbars();UpdateStatus("已恢复 Windows 原来的任务栏设置。");};
   var apply=Button(this,"应用设置",384,status.Bottom+14,112,38);apply.Primary=true;apply.Click+=delegate{if(ApplySettings!=null)ApplySettings(Collect());};
   var done=Button(this,"完成",508,status.Bottom+14,108,38);done.Click+=delegate{if(ApplySettings!=null)ApplySettings(Collect());Hide();};
   ClientSize=new Size(640,done.Bottom+24);
   manage.CheckedChanged+=delegate{foreach(var combo in modes.Values)combo.Enabled=manage.Checked;};foreach(var combo in modes.Values)combo.Enabled=manage.Checked;
   UiTheme.Changed+=OnThemeChanged;OnThemeChanged(null,EventArgs.Empty);
  }
  UiCard Card(int x,int y,int width,int height){var card=new UiCard{Bounds=new Rectangle(x,y,width,height)};Controls.Add(card);return card;}
  Label LabelText(Control parent,string text,int x,int y,int width,int height,float pixels,bool muted){var label=new Label{Text=text,Bounds=new Rectangle(x,y,width,height),Font=UiTheme.Font(pixels),BackColor=Color.Transparent,ForeColor=muted?UiTheme.Muted:UiTheme.Text,TextAlign=ContentAlignment.MiddleLeft,AutoEllipsis=true};labels[label]=muted;parent.Controls.Add(label);return label;}
  static UiButton Button(Control parent,string text,int x,int y,int width,int height){var button=new UiButton{Text=text,Bounds=new Rectangle(x,y,width,height)};parent.Controls.Add(button);return button;}
  void OnThemeChanged(object sender,EventArgs e){BackColor=UiTheme.Background;ForeColor=UiTheme.Text;foreach(var pair in labels)pair.Key.ForeColor=pair.Value?UiTheme.Muted:UiTheme.Text;UiTheme.Window(this);Invalidate(true);}
  Settings Collect(){var result=new Settings{ManageTaskbars=manage.Checked,LowBatteryNotification=alerts.Checked,StartWithWindows=startup.Checked,WidgetDisplay=((DisplayItem)target.SelectedItem).Id,Position=position.SelectedIndex==1?"Left":"Right",Offset=(int)offset.Value,Theme=UiTheme.Mode};foreach(var screen in Screen.AllScreens){string id=Native.DisplayId(screen);UiSelect selected;if(modes.TryGetValue(id,out selected))result.Monitors.Add(new MonitorPreference{Id=id,Device=screen.DeviceName,Mode=selected.SelectedIndex==1?"Auto":"Always"});}foreach(var old in original.Monitors)if(!result.Monitors.Exists(x=>x.Id==old.Id))result.Monitors.Add(old);return result;}
  public void UpdateStatus(string text){status.Text=text;status.ForeColor=text.Contains("失败")||text.Contains("禁用")||text.Contains("未返回")?UiTheme.Warning:UiTheme.Muted;}
  public void SetNotificationTestPending(bool pending){notificationTest.Enabled=!pending;notificationTest.Text=pending?"等待回执…":"测试通知";}
  public void ShowOn(Screen screen){if(!Visible){Location=new Point(screen.WorkingArea.Left+Math.Max(0,(screen.WorkingArea.Width-Width)/2),screen.WorkingArea.Top+Math.Max(0,(screen.WorkingArea.Height-Height)/2));Show();}Activate();}
  protected override void OnHandleCreated(EventArgs e){base.OnHandleCreated(e);UiTheme.Window(this);}
  protected override void OnFormClosing(FormClosingEventArgs e){base.OnFormClosing(e);if(!AllowClose&&e.CloseReason==CloseReason.UserClosing){e.Cancel=true;Hide();}}
  protected override void WndProc(ref Message m){if(m.Msg==0x8001){ShowOn(original.TargetScreen());return;}if(m.Msg==0x84){base.WndProc(ref m);if(m.Result==new IntPtr(1)){long point=m.LParam.ToInt64();var client=PointToClient(new Point((short)(point&0xffff),(short)((point>>16)&0xffff)));if(client.Y<36*UiTheme.Scale(this)&&client.X<Width-48*UiTheme.Scale(this))m.Result=new IntPtr(2);}return;}base.WndProc(ref m);}
  protected override void Dispose(bool disposing){if(disposing)UiTheme.Changed-=OnThemeChanged;base.Dispose(disposing);}
 }
}
