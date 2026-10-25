using System;
using AmBattery;
class Probe {
 static void Main(string[] args){if(args.Length>0&&args[0]=="notifications"){int state;Console.WriteLine("SHQueryUserNotificationState HR="+Native.SHQueryUserNotificationState(out state)+" State="+state);return;}if(args.Length>0&&args[0]=="windows"){Native.Diagnose();return;}foreach(var path in HidDevice.Paths())Console.WriteLine(path);for(int i=0;i<3;i++){var r=HidDevice.Read();Console.WriteLine("Mouse="+r.Mouse+" Dock="+r.Dock+" Error="+r.Error+" Raw="+r.Raw);System.Threading.Thread.Sleep(500);}}
}
