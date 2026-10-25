using System;
using System.Collections.Generic;
using System.Drawing;
using System.Runtime.InteropServices;
using System.Text;
using System.Windows.Forms;

namespace AmBattery {
 public static class Native {
  public delegate bool EnumProc(IntPtr hwnd,IntPtr param);
  [StructLayout(LayoutKind.Sequential)] public struct RECT {public int Left,Top,Right,Bottom;public Rectangle Rectangle {get{return Rectangle.FromLTRB(Left,Top,Right,Bottom);}} public static RECT From(Rectangle r){return new RECT{Left=r.Left,Top=r.Top,Right=r.Right,Bottom=r.Bottom};}}
  [StructLayout(LayoutKind.Sequential)] public struct APPBARDATA {public int Size;public IntPtr Window;public uint Callback,Edge;public RECT Rect;public IntPtr Param;}
  [StructLayout(LayoutKind.Sequential)] public struct POINT {public int X,Y;public POINT(int x,int y){X=x;Y=y;}}
  [StructLayout(LayoutKind.Sequential)] public struct SIZE {public int Width,Height;public SIZE(int w,int h){Width=w;Height=h;}}
  [StructLayout(LayoutKind.Sequential)]struct MONITORINFO {public int Size;public RECT Monitor,Work;public uint Flags;}
  [StructLayout(LayoutKind.Sequential,Pack=1)] public struct BLENDFUNCTION {public byte Operation,Flags,Alpha,Format;}
  [StructLayout(LayoutKind.Sequential,CharSet=CharSet.Unicode)] public struct DISPLAY_DEVICE {public int Size;[MarshalAs(UnmanagedType.ByValTStr,SizeConst=32)]public string Name;[MarshalAs(UnmanagedType.ByValTStr,SizeConst=128)]public string Description;public uint Flags;[MarshalAs(UnmanagedType.ByValTStr,SizeConst=128)]public string ID;[MarshalAs(UnmanagedType.ByValTStr,SizeConst=128)]public string Key;}
  [DllImport("user32.dll")] public static extern bool EnumWindows(EnumProc callback,IntPtr param);
  [DllImport("user32.dll",CharSet=CharSet.Unicode)]static extern IntPtr FindWindowEx(IntPtr parent,IntPtr after,string name,string title);
  [DllImport("user32.dll")] public static extern bool EnumChildWindows(IntPtr parent,EnumProc callback,IntPtr param);
  [DllImport("user32.dll",CharSet=CharSet.Unicode)] public static extern int GetClassName(IntPtr hwnd,StringBuilder text,int length);
  [DllImport("user32.dll")] public static extern bool GetWindowRect(IntPtr hwnd,out RECT rect);
  [DllImport("user32.dll")] public static extern bool GetClientRect(IntPtr hwnd,out RECT rect);
  [DllImport("user32.dll")] public static extern bool IsWindow(IntPtr hwnd);
  [DllImport("user32.dll")] public static extern bool IsWindowVisible(IntPtr hwnd);
  [DllImport("user32.dll")] public static extern bool IsZoomed(IntPtr hwnd);
  [DllImport("user32.dll")] public static extern IntPtr GetParent(IntPtr hwnd);
  [DllImport("user32.dll")] public static extern IntPtr GetForegroundWindow();
  [DllImport("user32.dll",EntryPoint="GetWindowLongPtrW")] public static extern IntPtr GetWindowLongPtr(IntPtr hwnd,int index);
  [DllImport("user32.dll",EntryPoint="SetWindowLongPtrW",SetLastError=true)] public static extern IntPtr SetWindowLongPtr(IntPtr hwnd,int index,IntPtr value);
  [DllImport("user32.dll",SetLastError=true)] public static extern IntPtr SetParent(IntPtr child,IntPtr parent);
  [DllImport("user32.dll",SetLastError=true)] public static extern bool SetWindowPos(IntPtr hwnd,IntPtr after,int x,int y,int width,int height,uint flags);
  [DllImport("user32.dll",SetLastError=true)] public static extern bool SetLayeredWindowAttributes(IntPtr hwnd,uint key,byte alpha,uint flags);
  [DllImport("user32.dll",SetLastError=true)] public static extern bool GetLayeredWindowAttributes(IntPtr hwnd,out uint key,out byte alpha,out uint flags);
  [DllImport("user32.dll")] public static extern bool RedrawWindow(IntPtr hwnd,IntPtr rect,IntPtr region,uint flags);
  [DllImport("user32.dll",CharSet=CharSet.Unicode)] public static extern bool EnumDisplayDevices(string device,uint index,ref DISPLAY_DEVICE info,uint flags);
  [DllImport("user32.dll",SetLastError=true)] public static extern bool SystemParametersInfo(uint action,uint parameter,ref RECT rect,uint flags);
  [DllImport("user32.dll")] public static extern IntPtr MonitorFromWindow(IntPtr hwnd,uint flags);
  [DllImport("user32.dll")]static extern IntPtr MonitorFromRect(ref RECT rect,uint flags);
  [DllImport("user32.dll",EntryPoint="GetMonitorInfoW")]static extern bool GetMonitorInfo(IntPtr monitor,ref MONITORINFO info);
  [DllImport("shell32.dll")] public static extern UIntPtr SHAppBarMessage(uint message,ref APPBARDATA data);
  [DllImport("shell32.dll")] public static extern int SHQueryUserNotificationState(out int state);
  [DllImport("user32.dll")] public static extern bool SetForegroundWindow(IntPtr hwnd);
  [DllImport("user32.dll")] public static extern bool DestroyIcon(IntPtr icon);
  [DllImport("user32.dll")] public static extern uint GetDpiForWindow(IntPtr hwnd);
  [DllImport("user32.dll")] public static extern bool SetProcessDpiAwarenessContext(IntPtr context);
  [DllImport("user32.dll")]public static extern IntPtr GetDC(IntPtr hwnd);
  [DllImport("user32.dll")]public static extern int ReleaseDC(IntPtr hwnd,IntPtr dc);
  [DllImport("gdi32.dll")]public static extern IntPtr CreateCompatibleDC(IntPtr dc);
  [DllImport("gdi32.dll")]public static extern bool DeleteDC(IntPtr dc);
  [DllImport("gdi32.dll")]public static extern IntPtr SelectObject(IntPtr dc,IntPtr item);
  [DllImport("gdi32.dll")]public static extern bool DeleteObject(IntPtr item);
  [DllImport("user32.dll",SetLastError=true)]public static extern bool UpdateLayeredWindow(IntPtr hwnd,IntPtr dstDc,ref POINT destination,ref SIZE size,IntPtr srcDc,ref POINT source,uint colorKey,ref BLENDFUNCTION blend,uint flags);
  [DllImport("user32.dll")]static extern int GetDisplayConfigBufferSizes(uint flags,out uint paths,out uint modes);
  [DllImport("user32.dll")]static extern int QueryDisplayConfig(uint flags,ref uint paths,IntPtr pathArray,ref uint modes,IntPtr modeArray,IntPtr topology);
  [DllImport("user32.dll")]static extern int DisplayConfigGetDeviceInfo(IntPtr packet);
  static readonly Dictionary<string,string> displayIds=new Dictionary<string,string>(),displayNames=new Dictionary<string,string>();
  static List<IntPtr> taskbarCache;static DateTime taskbarCacheTime=DateTime.MinValue;
  public static void ResetTaskbarCache(){taskbarCache=null;taskbarCacheTime=DateTime.MinValue;}
  public static void ResetDisplayCache(){displayIds.Clear();displayNames.Clear();ResetTaskbarCache();}
  static IntPtr DeviceRequest(int type,int size,IntPtr source,int luidOffset,int idOffset){var p=Marshal.AllocHGlobal(size);for(int i=0;i<size;i++)Marshal.WriteByte(p,i,0);Marshal.WriteInt32(p,0,type);Marshal.WriteInt32(p,4,size);Marshal.WriteInt64(p,8,Marshal.ReadInt64(source,luidOffset));Marshal.WriteInt32(p,16,Marshal.ReadInt32(source,idOffset));return p;}
  static void ReadDisplayConfig(){
   uint paths,modes;if(GetDisplayConfigBufferSizes(2,out paths,out modes)!=0||paths==0)return;
   IntPtr pathArray=Marshal.AllocHGlobal((int)paths*72),modeArray=Marshal.AllocHGlobal((int)modes*64);
   try{if(QueryDisplayConfig(2,ref paths,pathArray,ref modes,modeArray,IntPtr.Zero)!=0)return;
    for(int i=0;i<paths;i++){IntPtr path=IntPtr.Add(pathArray,i*72);IntPtr source=DeviceRequest(1,84,path,0,8),target=DeviceRequest(2,420,path,20,28);
     try{if(DisplayConfigGetDeviceInfo(source)==0&&DisplayConfigGetDeviceInfo(target)==0){string device=Marshal.PtrToStringUni(IntPtr.Add(source,20),32).TrimEnd('\0');string id=Marshal.PtrToStringUni(IntPtr.Add(target,164),128).TrimEnd('\0');string name=Marshal.PtrToStringUni(IntPtr.Add(target,36),64).TrimEnd('\0');if(id.Length>0)displayIds[device]=id;if(name.Length>0)displayNames[device]=name;}}
     finally{Marshal.FreeHGlobal(source);Marshal.FreeHGlobal(target);}
    }
   }finally{Marshal.FreeHGlobal(pathArray);Marshal.FreeHGlobal(modeArray);}
  }
  public static string Class(IntPtr hwnd){var b=new StringBuilder(256);GetClassName(hwnd,b,256);return b.ToString();}
  public static Rectangle Rect(IntPtr hwnd){RECT r;GetWindowRect(hwnd,out r);return r.Rectangle;}
  public static uint BarState(){var d=new APPBARDATA();d.Size=Marshal.SizeOf(d);return SHAppBarMessage(4,ref d).ToUInt32();}
  public static void BarState(uint value){var d=new APPBARDATA();d.Size=Marshal.SizeOf(d);d.Param=new IntPtr(value);SHAppBarMessage(10,ref d);}
  public static List<IntPtr> Taskbars(){bool valid=taskbarCache!=null&&(DateTime.UtcNow-taskbarCacheTime).TotalSeconds<5;if(valid)foreach(var h in taskbarCache)if(!IsWindow(h)){valid=false;break;}if(valid)return taskbarCache;var list=new List<IntPtr>();foreach(string name in new[]{"Shell_TrayWnd","Shell_SecondaryTrayWnd"}){IntPtr found=IntPtr.Zero;while((found=FindWindowEx(IntPtr.Zero,found,name,null))!=IntPtr.Zero)list.Add(found);}taskbarCache=list;taskbarCacheTime=DateTime.UtcNow;return list;}
  public static Rectangle WorkingArea(Rectangle bounds){var rect=RECT.From(bounds);var info=new MONITORINFO{Size=Marshal.SizeOf(typeof(MONITORINFO))};return GetMonitorInfo(MonitorFromRect(ref rect,2),ref info)?info.Work.Rectangle:Rectangle.Empty;}
  public static string DisplayId(Screen screen){string device=screen.DeviceName.TrimEnd('\0'),id;if(!displayIds.TryGetValue(device,out id)){ReadDisplayConfig();if(!displayIds.TryGetValue(device,out id)){var d=new DISPLAY_DEVICE();d.Size=Marshal.SizeOf(d);if(EnumDisplayDevices(device,0,ref d,1)&&!string.IsNullOrEmpty(d.ID))id=d.ID;else{string name;if(displayNames.TryGetValue(device,out name)){int matches=0;foreach(var n in displayNames.Values)if(n==name)matches++;id="name:"+name+(matches>1?"|"+device:"");}else id=device;}displayIds[device]=id;}}return id;}
  public static string DisplayLabel(Screen screen){string device=screen.DeviceName.TrimEnd('\0'),name;DisplayId(screen);if(!displayNames.TryGetValue(device,out name))name=device.Replace("\\\\.\\","");return (screen.Primary?"主屏":"副屏")+" · "+name+" · "+screen.Bounds.Width+" × "+screen.Bounds.Height;}
  public static void Diagnose(){var fg=GetForegroundWindow();Console.WriteLine("Foreground="+fg+" "+Class(fg)+" "+Rect(fg));foreach(var s in Screen.AllScreens)Console.WriteLine(DisplayLabel(s)+" "+s.Bounds+" ID="+DisplayId(s));foreach(var bar in Taskbars()){uint key,flags;byte alpha;bool layered=GetLayeredWindowAttributes(bar,out key,out alpha,out flags);Console.WriteLine(Class(bar)+" "+bar+" "+Rect(bar)+" visible="+IsWindowVisible(bar)+" alpha="+(layered?alpha.ToString():"n/a"));EnumChildWindows(bar,delegate(IntPtr h,IntPtr p){Console.WriteLine("  "+Class(h)+" "+h+" "+Rect(h)+" visible="+IsWindowVisible(h)+" style="+GetWindowLongPtr(h,-16).ToInt64().ToString("X")+" ex="+GetWindowLongPtr(h,-20).ToInt64().ToString("X"));return true;},IntPtr.Zero);}}
 }
}
