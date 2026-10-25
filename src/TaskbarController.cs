using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Runtime.Serialization;
using System.Windows.Forms;

namespace AmBattery {
 [DataContract] public sealed class SavedTaskbar {
  [DataMember] public long Handle, Style;
  [DataMember] public string Class,Device;
  public bool Hidden;
  public DateTime LastReveal=DateTime.MinValue;
  public Rectangle Bounds;
  public string Mode;
 }
 [DataContract] public sealed class TaskbarRecovery {
  [DataMember] public int Process;
  [DataMember] public uint State;
  [DataMember] public List<SavedTaskbar> Bars=new List<SavedTaskbar>();
  [DataMember] public List<SavedArea> Areas=new List<SavedArea>();
 }
 [DataContract] public sealed class SavedArea {
  [DataMember] public string Device;
  [DataMember] public int Left,Top,Right,Bottom;
 }
 public sealed class TaskbarController:IDisposable {
  public static readonly string RecoveryPath=Path.Combine(Settings.DataDirectory,"taskbar-recovery.json");
  readonly List<SavedTaskbar> managed=new List<SavedTaskbar>();
  TaskbarRecovery recovery;
  Settings settings;
  bool watcherStarted;
  DateTime lastScan=DateTime.MinValue,lastWork=DateTime.MinValue;
  public bool Enabled {get{return recovery!=null;}}
  public bool NeedsFastPolling {get{return recovery!=null&&managed.Exists(x=>x.Mode=="Auto");}}
  public bool VisibilityChanged;
  public string Status="未接管任务栏";
  public static void RestoreRecord(TaskbarRecovery r){
   foreach(var b in r.Bars){var h=new IntPtr(b.Handle);if(Native.IsWindow(h)&&Native.Class(h)==b.Class)Show(b);}
   if(Native.BarState()!=r.State)Native.BarState(r.State);
   if(r.Areas!=null)foreach(var area in r.Areas)foreach(var screen in Screen.AllScreens)if(screen.DeviceName==area.Device){var rect=new Rectangle(area.Left,area.Top,area.Right-area.Left,area.Bottom-area.Top);if(screen.Bounds.Contains(rect)&&Native.WorkingArea(screen.Bounds)!=rect){var saved=Native.RECT.From(rect);Native.SystemParametersInfo(47,0,ref saved,0);}break;}
  }
  public static void RecoverIfNeeded(){try{if(!File.Exists(RecoveryPath))return;var r=Settings.Read<TaskbarRecovery>(RecoveryPath);try{var p=Process.GetProcessById(r.Process);if(!p.HasExited&&p.MainModule.FileName==Application.ExecutablePath)return;}catch{}RestoreRecord(r);File.Delete(RecoveryPath);}catch(Exception e){Program.Log("Recovery: "+e.Message);}}
  public void Apply(Settings value){
   Dispose();settings=value;if(!value.ManageTaskbars){Status="使用 Windows 原有任务栏设置";return;}
   recovery=new TaskbarRecovery{State=Native.BarState(),Process=Process.GetCurrentProcess().Id};
   foreach(var screen in Screen.AllScreens){var a=screen.WorkingArea;recovery.Areas.Add(new SavedArea{Device=screen.DeviceName,Left=a.Left,Top=a.Top,Right=a.Right,Bottom=a.Bottom});}
   Settings.Write(RecoveryPath,recovery);
   Native.BarState(recovery.State&~1u);
   System.Threading.Thread.Sleep(120);
   Scan();
   string watcher=Path.Combine(AppDomain.CurrentDomain.BaseDirectory,"AMBattery.Recovery.exe");
   var p=File.Exists(watcher)?new ProcessStartInfo(watcher,"\""+RecoveryPath+"\" "+Process.GetCurrentProcess().Id):new ProcessStartInfo(Application.ExecutablePath,"--watch "+Process.GetCurrentProcess().Id);
   p.UseShellExecute=false;p.CreateNoWindow=true;p.WindowStyle=ProcessWindowStyle.Hidden;
   if(!watcherStarted){Process.Start(p);watcherStarted=true;}
   Status="各屏任务栏控制已启用";
   Tick();
  }
  void Scan(){
   if(recovery==null)return;
   bool changed=false;
   foreach(var hwnd in Native.Taskbars()){
    if(managed.Exists(x=>x.Handle==hwnd.ToInt64()))continue;
    var screen=Screen.FromHandle(hwnd);var pref=settings.For(screen);
    if(pref.Mode=="System")continue;
    var b=new SavedTaskbar{Handle=hwnd.ToInt64(),Class=Native.Class(hwnd),Device=screen.DeviceName,Style=Native.GetWindowLongPtr(hwnd,-20).ToInt64(),Bounds=screen.Bounds,Mode=pref.Mode};
    managed.Add(b);recovery.Bars.Add(b);changed=true;
   }
   if(changed)Settings.Write(RecoveryPath,recovery);lastScan=DateTime.UtcNow;
  }
  static void Show(SavedTaskbar b){
   var h=new IntPtr(b.Handle);Native.SetLayeredWindowAttributes(h,0,255,2);Native.SetWindowLongPtr(h,-20,new IntPtr(b.Style));
   Native.SetWindowPos(h,IntPtr.Zero,0,0,0,0,0x37);Native.RedrawWindow(h,IntPtr.Zero,IntPtr.Zero,0x485);b.Hidden=false;
  }
  public void Tick(){
   VisibilityChanged=false;
   if(recovery==null)return;
   if((DateTime.UtcNow-lastScan).TotalSeconds>2)Scan();
   if(!NeedsFastPolling)return;
   var cursor=Cursor.Position;
   foreach(var bar in managed){
    var h=new IntPtr(bar.Handle);if(!Native.IsWindow(h))continue;
    bool auto=bar.Mode=="Auto";if(!auto)continue;
    var rect=Native.Rect(h);var bound=bar.Bounds;
    bool onBar=rect.Contains(cursor);bool edge=cursor.X>=bound.Left&&cursor.X<bound.Right&&cursor.Y>=bound.Bottom-3&&cursor.Y<bound.Bottom;
    bool show=!auto||edge||(!bar.Hidden&&onBar);
    if(show){if(auto)bar.LastReveal=DateTime.UtcNow;if(bar.Hidden){Show(bar);Native.SetWindowPos(h,new IntPtr(-1),0,0,0,0,0x13);VisibilityChanged=true;}}
    else if(!bar.Hidden&&(DateTime.UtcNow-bar.LastReveal).TotalMilliseconds>350){
     Native.SetWindowLongPtr(h,-20,new IntPtr(bar.Style|0x80000|0x20));
     if(Native.SetLayeredWindowAttributes(h,0,0,2)){bar.Hidden=true;VisibilityChanged=true;}
    }
    if(auto&&(DateTime.UtcNow-lastWork).TotalSeconds>2&&Native.WorkingArea(bound)!=bound){var r=Native.RECT.From(bound);Native.SystemParametersInfo(47,0,ref r,0);}
   }
   if((DateTime.UtcNow-lastWork).TotalSeconds>2)lastWork=DateTime.UtcNow;
  }
  public bool IsHidden(IntPtr hwnd){var b=managed.Find(x=>x.Handle==hwnd.ToInt64());return b!=null&&b.Hidden;}
  public void Dispose(){
   if(recovery==null)return;
   RestoreRecord(recovery);managed.Clear();recovery=null;Status="已恢复 Windows 任务栏设置";
   try{File.Delete(RecoveryPath);}catch{}
  }
 }
}
