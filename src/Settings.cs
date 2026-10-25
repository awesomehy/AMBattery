using System;
using System.Collections.Generic;
using System.IO;
using System.Runtime.Serialization;
using System.Runtime.Serialization.Json;
using System.Windows.Forms;

namespace AmBattery {
 [DataContract] public sealed class MonitorPreference {
  [DataMember] public string Id;
  [DataMember] public string Device;
  [DataMember] public string Mode="Always";
 }
 [DataContract] public sealed class Settings {
  [DataMember] public bool ManageTaskbars=true;
  [DataMember] public bool LowBatteryNotification=true;
  [DataMember] public bool StartWithWindows=false;
  [DataMember] public string Theme="Dark";
  [DataMember] public string WidgetDisplay="";
  [DataMember] public string Position="Right";
  [DataMember] public int Offset=0;
  [DataMember] public List<MonitorPreference> Monitors=new List<MonitorPreference>();
  public static readonly string DataDirectory=Path.Combine(AppDomain.CurrentDomain.BaseDirectory,"data");
  public static readonly string FilePath=Path.Combine(DataDirectory,"settings.json");
  public static Settings Load(){try{var s=Read<Settings>(FilePath);if(s.Monitors==null)s.Monitors=new List<MonitorPreference>();if(s.Theme!="Light"&&s.Theme!="Dark")s.Theme="Dark";return s;}catch{return Defaults();}}
  public static Settings Defaults(){var s=new Settings();foreach(var screen in Screen.AllScreens){string id=Native.DisplayId(screen);s.Monitors.Add(new MonitorPreference{Id=id,Device=screen.DeviceName,Mode=screen.Primary?"Auto":"Always"});if(!screen.Primary||s.WidgetDisplay=="")s.WidgetDisplay=id;}return s;}
  public MonitorPreference For(Screen screen){string id=Native.DisplayId(screen);var p=Monitors.Find(x=>x.Id==id);if(p==null){p=new MonitorPreference{Id=id,Device=screen.DeviceName,Mode="Always"};Monitors.Add(p);}return p;}
  public Screen TargetScreen(){foreach(var screen in Screen.AllScreens)if(Native.DisplayId(screen)==WidgetDisplay)return screen;foreach(var screen in Screen.AllScreens)if(!screen.Primary)return screen;return Screen.PrimaryScreen;}
  public void Save(){Write(FilePath,this);}
  public static T Read<T>(string path){using(var f=File.OpenRead(path)){return (T)new DataContractJsonSerializer(typeof(T)).ReadObject(f);}}
  public static void Write<T>(string path,T value){Directory.CreateDirectory(Path.GetDirectoryName(path));string temp=path+".tmp";using(var f=File.Create(temp)){new DataContractJsonSerializer(typeof(T)).WriteObject(f,value);}if(File.Exists(path))File.Replace(temp,path,null);else File.Move(temp,path);}
 }
}
