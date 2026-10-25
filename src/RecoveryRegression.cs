using System;
using System.IO;
using System.Reflection;
using System.Runtime.Serialization.Json;

namespace AmBattery {
 public static class RecoveryRegression {
  public static int Main(){
   try{
    var assembly=Assembly.LoadFile(Path.Combine(AppDomain.CurrentDomain.BaseDirectory,"AMBattery.Recovery.exe"));
    foreach(var reference in assembly.GetReferencedAssemblies())if(reference.Name=="System.Windows.Forms"||reference.Name=="System.Drawing")throw new Exception("Recovery helper loads UI framework.");
    var original=new TaskbarRecovery{Process=123,State=1};original.Bars.Add(new SavedTaskbar{Handle=456,Style=789,Class="Shell_TrayWnd",Device="DISPLAY1"});original.Areas.Add(new SavedArea{Device="DISPLAY1",Left=-1080,Top=-204,Right=0,Bottom=1668});
    var type=assembly.GetType("AmBatteryRecovery.Record");object record;
    using(var stream=new MemoryStream()){new DataContractJsonSerializer(typeof(TaskbarRecovery)).WriteObject(stream,original);stream.Position=0;record=new DataContractJsonSerializer(type).ReadObject(stream);}
    if((int)type.GetField("Process").GetValue(record)!=123||(uint)type.GetField("State").GetValue(record)!=1)throw new Exception("Recovery record identity mismatch.");
    var bars=(System.Collections.IList)type.GetField("Bars").GetValue(record);var areas=(System.Collections.IList)type.GetField("Areas").GetValue(record);
    if(bars.Count!=1||(long)bars[0].GetType().GetField("Handle").GetValue(bars[0])!=456||(long)bars[0].GetType().GetField("Style").GetValue(bars[0])!=789)throw new Exception("Taskbar restore data lost.");
    if(areas.Count!=1||(int)areas[0].GetType().GetField("Left").GetValue(areas[0])!=-1080||(int)areas[0].GetType().GetField("Bottom").GetValue(areas[0])!=1668)throw new Exception("Monitor work area data lost.");
    Console.WriteLine("PASS: recovery helper record compatibility and absence of UI framework dependencies.");return 0;
   }catch(Exception error){Console.WriteLine("FAIL: "+error);return 1;}
  }
 }
}
