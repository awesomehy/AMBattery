using System;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;
using Microsoft.Win32;

namespace AmBattery {
 static class Program {
  [DllImport("user32.dll",CharSet=CharSet.Unicode)]static extern IntPtr FindWindow(string cls,string text);
  [DllImport("user32.dll")]static extern bool PostMessage(IntPtr hwnd,int message,IntPtr w,IntPtr l);
  [DllImport("user32.dll")]static extern uint GetWindowThreadProcessId(IntPtr hwnd,out uint process);
  [DllImport("user32.dll",SetLastError=true)]static extern bool PostThreadMessage(uint thread,int message,IntPtr w,IntPtr l);
  [STAThread] static void Main(string[] args){
   if(args.Length==2&&args[0]=="--watch"){Watch(int.Parse(args[1]));return;}
   if(args.Contains("--notification-status")){NotificationStatus.Capture().Save();return;}
   if(args.Contains("--diagnose")){var r=HidDevice.Read();Directory.CreateDirectory(Settings.DataDirectory);File.WriteAllText(Path.Combine(Settings.DataDirectory,"diagnostic.txt"),"Mouse="+r.Mouse+" ("+r.MouseState+") Dongle="+r.Dock+" ("+r.DockState+")\r\n"+r.Error+"\r\n"+r.Raw);return;}
   bool created;using(var mutex=new Mutex(true,"Local\\AmBatteryDesktop-2026",out created)){
    if(!created&&!SwitchPreviousVersion(mutex)){var hwnd=FindWindow(null,"怒喵电量与任务栏");if(hwnd!=IntPtr.Zero)PostMessage(hwnd,0x8001,IntPtr.Zero,IntPtr.Zero);return;}
    try{Native.SetProcessDpiAwarenessContext(new IntPtr(-4));}catch{}
    Application.EnableVisualStyles();Application.SetCompatibleTextRenderingDefault(false);TaskbarController.RecoverIfNeeded();
    var app=new BatteryApplication(args.Contains("--preview"),args.Contains("--settings"));
    Application.ThreadException+=delegate(object sender,ThreadExceptionEventArgs e){Log(e.Exception.ToString());app.Restore();try{MessageBox.Show(e.Exception.Message,"AM Battery");}finally{app.ExitThread();}};
    AppDomain.CurrentDomain.UnhandledException+=delegate{app.Restore();};
    Application.ApplicationExit+=delegate{app.Restore();};
    Application.Run(app);
   }
  }
  static bool SwitchPreviousVersion(Mutex mutex){
   try{
    var window=FindWindow(null,"怒喵电量与任务栏");if(window==IntPtr.Zero)return false;
    uint process;uint thread=GetWindowThreadProcessId(window,out process);var previous=Process.GetProcessById((int)process);string executable=previous.MainModule.FileName;
    if(!string.Equals(Path.GetFileName(executable),"AMBattery.exe",StringComparison.OrdinalIgnoreCase)||string.Equals(executable,Application.ExecutablePath,StringComparison.OrdinalIgnoreCase))return false;
    string oldData=Path.Combine(Path.GetDirectoryName(executable),"data"),oldSettings=Path.Combine(oldData,"settings.json"),oldRecovery=Path.Combine(oldData,"taskbar-recovery.json");
    // The new version is started by the user. Ask the previous app's own UI loop to exit.
    // Its normal ApplicationExit handler (and recovery watcher) restore taskbars first.
    if(!PostThreadMessage(thread,0x12,IntPtr.Zero,IntPtr.Zero))return false;
    bool acquired=false;try{acquired=mutex.WaitOne(5000);}catch(AbandonedMutexException){acquired=true;}if(!acquired)return false;
    if(File.Exists(oldSettings)&&!File.Exists(Settings.FilePath)){Directory.CreateDirectory(Settings.DataDirectory);File.Copy(oldSettings,Settings.FilePath);}
    DateTime deadline=DateTime.UtcNow.AddSeconds(3);while(File.Exists(oldRecovery)&&DateTime.UtcNow<deadline)Thread.Sleep(50);
    if(File.Exists(oldRecovery)){Log("Previous taskbar recovery is still pending; new taskbar control was not started.");mutex.ReleaseMutex();return false;}
    return true;
   }catch(Exception error){Log("Version switch: "+error.Message);return false;}
  }
  static void Watch(int parent){try{Process.GetProcessById(parent).WaitForExit();}catch{}try{if(File.Exists(TaskbarController.RecoveryPath)){var r=Settings.Read<TaskbarRecovery>(TaskbarController.RecoveryPath);if(r.Process==parent){TaskbarController.RestoreRecord(r);File.Delete(TaskbarController.RecoveryPath);}}}catch(Exception e){Log("Watch: "+e.Message);}}
  public static void Log(string text){try{Directory.CreateDirectory(Settings.DataDirectory);var path=Path.Combine(Settings.DataDirectory,"app.log");if(File.Exists(path)&&new FileInfo(path).Length>128000)File.WriteAllText(path,"");File.AppendAllText(path,DateTime.Now.ToString("s")+" "+text+Environment.NewLine);}catch{}}
 }
 sealed class BatteryApplication:ApplicationContext {
  Settings settings;
  readonly TaskbarController controller=new TaskbarController();
  readonly TaskbarWidget widget;
  BatteryPopup popup;
  readonly SettingsForm form;
  readonly NotifyIcon tray;
  readonly System.Windows.Forms.Timer timer;
  readonly ContextMenuStrip menu;
  BatteryReading reading=new BatteryReading();
  bool polling,exiting;
  readonly NotificationGate notificationGate=new NotificationGate();
  DateTime lastRead=DateTime.MinValue,lastPlace=DateTime.MinValue;
  string lastWidgetStatus="";
  string lastNotificationBlock="";
  bool testNotificationPending;
  DateTime testNotificationRequested;
  public BatteryApplication(bool preview,bool showSettings){
   bool first=!File.Exists(Settings.FilePath);settings=Settings.Load();UiTheme.Set(settings.Theme);
   menu=new ContextMenuStrip();menu.Items.Add("电池详情",null,delegate{OpenDetails();});menu.Items.Add("设置",null,delegate{form.ShowOn(settings.TargetScreen());});menu.Items.Add("恢复系统任务栏",null,delegate{RestoreTaskbars();});menu.Items.Add(new ToolStripSeparator());menu.Items.Add("退出",null,delegate{ExitThread();});
   menu.Renderer=new UiMenuRenderer();menu.ShowImageMargin=false;menu.Font=UiTheme.Font(13);menu.Opening+=delegate{menu.BackColor=UiTheme.Card;menu.ForeColor=UiTheme.Text;};
   tray=new NotifyIcon{Icon=MouseArtwork.Icon,Text="AM Battery",ContextMenuStrip=menu,Visible=true};tray.MouseClick+=delegate(object sender,MouseEventArgs e){if(e.Button==MouseButtons.Left)OpenDetails();};tray.BalloonTipClicked+=delegate{OpenDetails();};
   tray.BalloonTipShown+=delegate{if(!exiting&&testNotificationPending){testNotificationPending=false;form.SetNotificationTestPending(false);form.UpdateStatus("Windows 已报告显示测试通知。");Program.Log("Test notification: Windows reported displayed.");}};
   widget=new TaskbarWidget(menu);widget.OpenDetails=OpenDetails;
   form=new SettingsForm(settings);form.ApplySettings=Apply;form.RestoreTaskbars=RestoreTaskbars;form.TestNotification=delegate{Notify(true);};form.OpenDetails=OpenDetails;
   form.ThemeChanged=delegate(string mode){settings.Theme=mode;try{settings.Save();form.UpdateStatus("外观已切换为简约 · "+(mode=="Light"?"明亮":"深色")+"。");}catch(Exception error){form.UpdateStatus("外观设置保存失败："+error.Message);}};
   form.OpenNotificationSettings=delegate{try{Process.Start(new ProcessStartInfo("ms-settings:notifications"){UseShellExecute=true});}catch(Exception error){form.UpdateStatus("无法打开系统通知设置："+error.Message);}};
   var handle=form.Handle;
   timer=new System.Windows.Forms.Timer{Interval=400};timer.Tick+=delegate{controller.Tick();CheckNotificationReceipt();if(controller.VisibilityChanged||(DateTime.UtcNow-lastPlace).TotalMilliseconds>500){PlaceWidget();lastPlace=DateTime.UtcNow;}if((DateTime.UtcNow-lastRead).TotalSeconds>30&&!polling)Poll();};timer.Start();
   if(!preview&&!first)Apply(settings);
   widget.Place(settings,controller);Poll();
   if(first||showSettings||preview)form.ShowOn(settings.TargetScreen());
   Microsoft.Win32.SystemEvents.DisplaySettingsChanged+=OnDisplaysChanged;
   Microsoft.Win32.SystemEvents.UserPreferenceChanged+=OnUserPreferencesChanged;
  }
  BatteryPopup EnsurePopup(){
   if(popup==null||popup.IsDisposed){
    var created=new BatteryPopup{Reading=reading};
    created.OpenSettings=delegate{if(exiting||form.IsDisposed)return;created.Hide();form.ShowOn(settings.TargetScreen());};
    created.RefreshReading=Poll;popup=created;
   }
   return popup;
  }
  void PlaceWidget(){
   bool placed=widget.Place(settings,controller);string diagnostic=widget.Status();
   if(diagnostic!=lastWidgetStatus){lastWidgetStatus=diagnostic;try{Directory.CreateDirectory(Settings.DataDirectory);File.WriteAllText(Path.Combine(Settings.DataDirectory,"widget-status.txt"),diagnostic);}catch{}if(!placed||widget.LastError!=""){string error=widget.LastError!=""?widget.LastError:"目标屏幕的任务栏暂时不可用";Program.Log(error);form.UpdateStatus(error);}}
  }
  void OnUserPreferencesChanged(object sender,Microsoft.Win32.UserPreferenceChangedEventArgs e){Palette.ResetSystemTheme();}
  void OnDisplaysChanged(object sender,EventArgs e){try{form.BeginInvoke(new Action(delegate{if(exiting)return;Native.ResetDisplayCache();controller.Apply(settings);timer.Interval=controller.NeedsFastPolling?120:400;widget.Place(settings,controller);}));}catch{}}
  void Apply(Settings value){try{settings=value;UiTheme.Set(settings.Theme);settings.Save();controller.Apply(settings);timer.Interval=controller.NeedsFastPolling?120:400;using(var key=Registry.CurrentUser.CreateSubKey(@"Software\Microsoft\Windows\CurrentVersion\Run")){if(settings.StartWithWindows)key.SetValue("AMBattery","\""+Application.ExecutablePath+"\" --background");else key.DeleteValue("AMBattery",false);}widget.Place(settings,controller);form.UpdateStatus(controller.Status+"。显示位置："+new DisplayItem(settings.TargetScreen()));}catch(Exception e){controller.Dispose();Program.Log(e.ToString());form.UpdateStatus("未能应用："+e.Message);}}
  void RestoreTaskbars(){controller.Dispose();timer.Interval=400;settings.ManageTaskbars=false;try{settings.Save();}catch{}form.UpdateStatus("已恢复 Windows 原来的任务栏设置。");}
  void Poll(){
   if(polling||exiting)return;polling=true;lastRead=DateTime.UtcNow;
   Task.Factory.StartNew(delegate{
    var master=Process.GetProcessesByName("AM_Master");bool busy=master.Length>0;foreach(var process in master)process.Dispose();
    if(busy)return new BatteryReading{Timestamp=DateTime.Now,Error="请退出 AM_Master 后自动恢复读取"};
    return HidDevice.Read();
   }).ContinueWith(task=>{
    if(exiting||form.IsDisposed)return;
    try{form.BeginInvoke(new Action(delegate{
     if(exiting||form.IsDisposed)return;
     polling=false;int? previousMouse=reading.Mouse;reading=task.IsFaulted?new BatteryReading{Error=task.Exception.GetBaseException().Message,Timestamp=DateTime.Now}:task.Result;
     widget.Reading=reading;if(previousMouse!=reading.Mouse)PlaceWidget();if(popup!=null&&!popup.IsDisposed){popup.Reading=reading;if(popup.Visible)popup.Invalidate();}
     tray.Text="AM Battery · 鼠标 "+(reading.Mouse.HasValue?reading.Mouse+"%":"未连接")+" · Dongle "+(reading.Dock.HasValue?reading.Dock+"%":"未连接");
     NotificationStatus readiness=null;bool canNotify=settings.LowBatteryNotification;
     if(canNotify&&!notificationGate.HasNotified&&reading.Mouse.HasValue&&reading.Mouse<30){readiness=NotificationStatus.Capture();canNotify=readiness.CanInterrupt;if(!readiness.CanRequest&&lastNotificationBlock!=readiness.BlockingReason){lastNotificationBlock=readiness.BlockingReason;form.UpdateStatus(lastNotificationBlock);Program.Log(lastNotificationBlock);try{readiness.Save();}catch{}}}
     if(notificationGate.Check(reading.Mouse,canNotify)){if(!Notify(false,readiness))notificationGate.Retry();}
     if(!reading.Connected)form.UpdateStatus(reading.Error);
    }));}catch{}
   });
  }
  bool Notify(bool test,NotificationStatus readiness=null){
   if(exiting)return false;
   readiness=readiness??NotificationStatus.Capture();try{readiness.Save();}catch{}
   if(!readiness.CanRequest){form.UpdateStatus(readiness.BlockingReason);Program.Log("Notification blocked: "+readiness.PlatformSetting+"; "+readiness.BlockingReason);return false;}
   if(test){testNotificationPending=true;testNotificationRequested=DateTime.UtcNow;form.SetNotificationTestPending(true);form.UpdateStatus(readiness.InterruptionHint!=""?readiness.InterruptionHint:"已向 Windows 请求发送测试通知，正在等待显示回执…");}
   try{tray.ShowBalloonTip(7000,test?"AM Battery · 通知测试":"怒喵鼠标电量低于 30%",test?"这是通知测试。当前鼠标电量："+(reading.Mouse.HasValue?reading.Mouse+"%":"未连接"):"AM INFINITY 当前电量 "+reading.Mouse+"%，请及时更换电池或充电。",test?ToolTipIcon.Info:ToolTipIcon.Warning);Program.Log("Notification requested: "+(test?"test":"low battery")+"; permission="+readiness.PlatformSetting+"; shell="+readiness.ShellState);return true;}
   catch(Exception error){testNotificationPending=false;form.SetNotificationTestPending(false);form.UpdateStatus("系统通知发送失败："+error.Message);Program.Log("Notification failed: "+error.Message);return false;}
  }
  void CheckNotificationReceipt(){if(testNotificationPending&&(DateTime.UtcNow-testNotificationRequested).TotalSeconds>=8){testNotificationPending=false;form.SetNotificationTestPending(false);form.UpdateStatus("已请求发送，但 Windows 未返回显示确认。请查看通知中心，或检查通知与勿扰设置。");Program.Log("Test notification: no display receipt within 8 seconds.");}}
  void OpenDetails(){if(exiting)return;var details=EnsurePopup();if(details.Visible){details.Hide();return;}details.OpenAt(widget.Visible?Native.Rect(widget.Handle):new Rectangle(settings.TargetScreen().WorkingArea.Right-100,settings.TargetScreen().WorkingArea.Bottom,80,40),settings.TargetScreen());}
  public void Restore(){if(exiting)return;exiting=true;timer.Stop();controller.Dispose();widget.Hide();if(popup!=null&&!popup.IsDisposed)popup.Hide();tray.Visible=false;}
  protected override void ExitThreadCore(){Restore();Microsoft.Win32.SystemEvents.DisplaySettingsChanged-=OnDisplaysChanged;Microsoft.Win32.SystemEvents.UserPreferenceChanged-=OnUserPreferencesChanged;form.AllowClose=true;if(popup!=null){popup.AllowClose=true;popup.Dispose();}widget.Dispose();form.Dispose();tray.Dispose();menu.Dispose();timer.Dispose();base.ExitThreadCore();}
 }
}
