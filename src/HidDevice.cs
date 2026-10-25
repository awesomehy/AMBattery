using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Runtime.InteropServices;
using System.Threading;
using Microsoft.Win32.SafeHandles;

namespace AmBattery {
  public sealed class BatteryReading {
    public int? Mouse, Dock;
    public string MouseState = "状态未知", DockState = "状态未知", Error = "", Raw = "";
    public DateTime Timestamp;
    public bool Connected { get { return Mouse.HasValue || Dock.HasValue; } }
  }
  public static class HidDevice {
    static string cachedPath;
    public static bool DecodeMouse(byte[] payload,BatteryReading reading){
      if(payload==null||payload.Length<4||payload[0]!=0xd6||payload[2]==0||payload[3]>100)return false;
      reading.Mouse=payload[3];reading.MouseState=payload[1]==1?"充电中":payload[1]==2?"已充满":payload[1]==0?"使用电池":"状态未知";return true;
    }
    [StructLayout(LayoutKind.Sequential)] struct InterfaceData { public int Size; public Guid Guid; public int Flags; public IntPtr Reserved; }
    [DllImport("hid.dll")] static extern void HidD_GetHidGuid(out Guid guid);
    [DllImport("setupapi.dll", CharSet=CharSet.Unicode, SetLastError=true)] static extern IntPtr SetupDiGetClassDevs(ref Guid guid, string enumerator, IntPtr parent, uint flags);
    [DllImport("setupapi.dll", SetLastError=true)] static extern bool SetupDiEnumDeviceInterfaces(IntPtr set, IntPtr info, ref Guid guid, uint index, ref InterfaceData data);
    [DllImport("setupapi.dll", CharSet=CharSet.Unicode, SetLastError=true)] static extern bool SetupDiGetDeviceInterfaceDetail(IntPtr set, ref InterfaceData data, IntPtr detail, int size, out int required, IntPtr info);
    [DllImport("setupapi.dll")] static extern bool SetupDiDestroyDeviceInfoList(IntPtr set);
    [DllImport("kernel32.dll",CharSet=CharSet.Unicode,SetLastError=true)] static extern SafeFileHandle CreateFile(string path,uint access,uint share,IntPtr security,uint creation,uint flags,IntPtr template);
    [DllImport("hid.dll",SetLastError=true)] [return:MarshalAs(UnmanagedType.Bool)] static extern bool HidD_SetFeature(SafeFileHandle handle, byte[] report, int length);
    [DllImport("hid.dll",SetLastError=true)] [return:MarshalAs(UnmanagedType.Bool)] static extern bool HidD_GetFeature(SafeFileHandle handle, byte[] report, int length);
    static byte[] Packet(byte command, params byte[] args) {
      var packet=new byte[65];packet[1]=command;int sum=command;
      for(int i=0;i<args.Length;i++){packet[i+2]=args[i];sum+=args[i];}
      packet[8]=(byte)(255-(sum&255));return packet;
    }
    static byte[] Exchange(SafeFileHandle handle, byte[] packet, bool read) {
      if(!HidD_SetFeature(handle,packet,packet.Length))throw new Win32Exception();
      Thread.Sleep(35);if(!read)return null;
      var report=new byte[65];
      if(!HidD_GetFeature(handle,report,report.Length))throw new Win32Exception();
      var payload=new byte[64];Buffer.BlockCopy(report,1,payload,0,64);return payload;
    }
    static byte[] Status(SafeFileHandle handle){var p=new byte[65];p[1]=0xf7;return Exchange(handle,p,true);}
    public static List<string> Paths() {
      Guid guid; HidD_GetHidGuid(out guid); var list = new List<string>();
      IntPtr set=SetupDiGetClassDevs(ref guid,null,IntPtr.Zero,0x12);
      if(set==new IntPtr(-1)) throw new Win32Exception();
      try { for(uint i=0;;i++) {
        var data=new InterfaceData();data.Size=Marshal.SizeOf(data);
        if(!SetupDiEnumDeviceInterfaces(set,IntPtr.Zero,ref guid,i,ref data)) break;
        int size;SetupDiGetDeviceInterfaceDetail(set,ref data,IntPtr.Zero,0,out size,IntPtr.Zero);
        IntPtr buffer=Marshal.AllocHGlobal(size);
        try {Marshal.WriteInt32(buffer,IntPtr.Size==8?8:6);if(SetupDiGetDeviceInterfaceDetail(set,ref data,buffer,size,out size,IntPtr.Zero)) {
          string path=Marshal.PtrToStringUni(IntPtr.Add(buffer,4));
          if(path.IndexOf("vid_3151&pid_5007&mi_02",StringComparison.OrdinalIgnoreCase)>=0) list.Add(path);
        }}finally{Marshal.FreeHGlobal(buffer);}
      }} finally{SetupDiDestroyDeviceInfoList(set);}
      return list;
    }
    public static BatteryReading Read() {
      var result=new BatteryReading();result.Timestamp=DateTime.Now;
      try {
        if(cachedPath==null){var paths=Paths();if(paths.Count==0){result.Error="未找到怒喵接收器，请检查 USB 连接";return result;}cachedPath=paths[0];}
        using(var handle=CreateFile(cachedPath,0xc0000000,3,IntPtr.Zero,3,0,IntPtr.Zero)) {
          if(handle.IsInvalid) throw new Win32Exception();
          var status=Status(handle);result.Raw="status="+BitConverter.ToString(status);
          if(status[10]>0&&status[10]<=100){result.Dock=status[10];result.DockState=status[10]==100?"已充满":"充电中";}
          if(status[4]==0) {
            try {
              Exchange(handle,Packet(0xf6,5),false);
              var limit=DateTime.UtcNow.AddMilliseconds(1200);bool ready=false;
              while(DateTime.UtcNow<limit){var s=Status(handle);if(s[4]==1)break;if(s[5]==1||(s[0]==0&&s[4]==0&&s[6]==2)){ready=true;break;}Thread.Sleep(60);}
              if(ready){
                Exchange(handle,Packet(0xfe,64),false);Exchange(handle,Packet(0xd6),false);
                limit=DateTime.UtcNow.AddMilliseconds(1500);bool response=false;
                while(DateTime.UtcNow<limit){var s=Status(handle);if(s[0]==1||s[0]==0xd6){response=true;break;}Thread.Sleep(60);}
                if(response){var request=new byte[65];request[1]=0xfc;var mouse=Exchange(handle,request,true);result.Raw+=" mouse="+BitConverter.ToString(mouse);DecodeMouse(mouse,result);}
              }
            } finally {try{Exchange(handle,Packet(0xf6,0),false);}catch{}}
          }
          if(!result.Connected)result.Error="设备尚未返回有效电量";
        }
      }catch(Exception error){cachedPath=null;result.Error=error.Message;}
      return result;
    }
  }
}
