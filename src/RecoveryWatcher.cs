using System;
using System.Collections.Generic;
using System.IO;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Runtime.Serialization;
using System.Runtime.Serialization.Json;
using System.Text;

namespace AmBatteryRecovery {
 [DataContract]sealed class Record {
  [DataMember]public int Process;
  [DataMember]public uint State;
  [DataMember]public List<Bar> Bars;
  [DataMember]public List<Area> Areas;
 }
 [DataContract]sealed class Bar {[DataMember]public long Handle,Style;[DataMember]public string Class;}
 [DataContract]sealed class Area {[DataMember]public string Device;[DataMember]public int Left,Top,Right,Bottom;}
 [StructLayout(LayoutKind.Sequential)]struct Rect {public int Left,Top,Right,Bottom;}
 [StructLayout(LayoutKind.Sequential)]struct AppBar {public int Size;public IntPtr Window;public uint Callback,Edge;public Rect Rect;public IntPtr Param;}
 [StructLayout(LayoutKind.Sequential,CharSet=CharSet.Unicode)]struct Monitor {public int Size;public Rect Bounds,Work;public uint Flags;[MarshalAs(UnmanagedType.ByValTStr,SizeConst=32)]public string Device;}
 static class Program {
  [DllImport("kernel32.dll",SetLastError=true)]static extern IntPtr OpenProcess(uint access,bool inherit,uint process);
  [DllImport("kernel32.dll")]static extern uint WaitForSingleObject(IntPtr handle,uint milliseconds);
  [DllImport("kernel32.dll")]static extern bool CloseHandle(IntPtr handle);
  [DllImport("user32.dll")]static extern bool IsWindow(IntPtr h);
  [DllImport("user32.dll",CharSet=CharSet.Unicode)]static extern int GetClassName(IntPtr h,StringBuilder text,int length);
  [DllImport("user32.dll",EntryPoint="SetWindowLongPtrW")]static extern IntPtr SetWindowLong(IntPtr h,int index,IntPtr value);
  [DllImport("user32.dll")]static extern bool SetLayeredWindowAttributes(IntPtr h,uint key,byte alpha,uint flags);
  [DllImport("user32.dll")]static extern bool SetWindowPos(IntPtr h,IntPtr after,int x,int y,int width,int height,uint flags);
  [DllImport("user32.dll")]static extern bool RedrawWindow(IntPtr h,IntPtr rect,IntPtr region,uint flags);
  [DllImport("user32.dll")]static extern IntPtr MonitorFromRect(ref Rect rect,uint flags);
  [DllImport("user32.dll",EntryPoint="GetMonitorInfoW",CharSet=CharSet.Unicode)]static extern bool GetMonitorInfo(IntPtr h,ref Monitor monitor);
  [DllImport("user32.dll",CharSet=CharSet.Unicode)]static extern bool SystemParametersInfo(uint action,uint parameter,ref Rect rect,uint flags);
  [DllImport("shell32.dll")]static extern UIntPtr SHAppBarMessage(uint message,ref AppBar data);
  static bool Same(Rect a,Rect b){return a.Left==b.Left&&a.Right==b.Right&&a.Top==b.Top&&a.Bottom==b.Bottom;}
  static void Main(string[] args){
   try{
    string expected=Path.Combine(AppDomain.CurrentDomain.BaseDirectory,"data","taskbar-recovery.json");
    if(args.Length!=2||!string.Equals(Path.GetFullPath(args[0]),expected,StringComparison.OrdinalIgnoreCase))return;
    int parent;if(!int.TryParse(args[1],out parent)||parent<=0)return;
    var process=OpenProcess(0x100000,false,(uint)parent);
    if(process!=IntPtr.Zero){try{if(WaitForSingleObject(process,0xffffffff)!=0)return;}finally{CloseHandle(process);}}
    else if(Marshal.GetLastWin32Error()==5)return;
    Recover(expected,parent);
   }catch{}
  }
  // Keep JSON and restore-only dependencies out of the idle wait path.
  [MethodImpl(MethodImplOptions.NoInlining)]static void Recover(string expected,int parent){
   try{
    if(!File.Exists(expected))return;Record record;using(var input=File.OpenRead(expected))record=(Record)new DataContractJsonSerializer(typeof(Record)).ReadObject(input);if(record.Process!=parent)return;
    if(record.Bars!=null)foreach(var bar in record.Bars){var h=new IntPtr(bar.Handle);if(!IsWindow(h))continue;var name=new StringBuilder(64);GetClassName(h,name,64);if(name.ToString()!=bar.Class)continue;SetLayeredWindowAttributes(h,0,255,2);SetWindowLong(h,-20,new IntPtr(bar.Style));SetWindowPos(h,IntPtr.Zero,0,0,0,0,0x37);RedrawWindow(h,IntPtr.Zero,IntPtr.Zero,0x485);}
    var state=new AppBar{Size=Marshal.SizeOf(typeof(AppBar))};if(SHAppBarMessage(4,ref state).ToUInt32()!=record.State){state.Param=new IntPtr(record.State);SHAppBarMessage(10,ref state);}
    if(record.Areas!=null)foreach(var area in record.Areas){var rect=new Rect{Left=area.Left,Top=area.Top,Right=area.Right,Bottom=area.Bottom};var monitor=new Monitor{Size=Marshal.SizeOf(typeof(Monitor))};if(GetMonitorInfo(MonitorFromRect(ref rect,2),ref monitor)&&monitor.Device==area.Device&&rect.Left>=monitor.Bounds.Left&&rect.Right<=monitor.Bounds.Right&&rect.Top>=monitor.Bounds.Top&&rect.Bottom<=monitor.Bounds.Bottom&&!Same(rect,monitor.Work))SystemParametersInfo(47,0,ref rect,0);}
    File.Delete(expected);
   }catch(Exception error){try{File.AppendAllText(Path.Combine(Path.GetDirectoryName(expected),"recovery.log"),DateTime.Now.ToString("s")+" "+error.Message+Environment.NewLine);}catch{}}
  }
 }
}
