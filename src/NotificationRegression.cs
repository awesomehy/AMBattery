using System;
namespace AmBattery {
 public static class NotificationRegression {
  static int count;
  static void Assert(bool value,string reason){if(!value)throw new Exception(reason);count++;}
  public static int Main(){try{
   var status=new NotificationStatus{ToastEnabled=0,PlatformSetting="Enabled",ShellState=5};Assert(!status.CanRequest,"explicit global off must block");
   status.ToastEnabled=1;status.PlatformSetting="DisabledForUser";Assert(!status.CanRequest,"native permission must override a stale registry switch");
   status.PlatformSetting="Enabled";Assert(status.CanRequest&&status.CanInterrupt,"ready to notify");
   status.PolicyDisabled=true;Assert(!status.CanRequest,"policy blocked");status.PolicyDisabled=false;
   status.PlatformSetting="DisabledByGroupPolicy";Assert(!status.CanRequest,"native policy blocked");
   status.PlatformSetting="DisabledForApplication";Assert(!status.CanRequest,"app notification blocked");
   status.PlatformSetting="DisabledByManifest";Assert(!status.CanRequest,"invalid identity blocked");
   status.PlatformSetting="Enabled";status.ShellState=3;Assert(status.CanRequest&&!status.CanInterrupt&&status.InterruptionHint!="","defer automatic alerts in fullscreen");
   var gate=new NotificationGate();Assert(!gate.Check(29,status.CanInterrupt),"suppression must not consume the reminder");status.ShellState=5;Assert(gate.Check(29,status.CanInterrupt),"deliver once fullscreen ends");Assert(!gate.Check(28,true),"avoid duplicate alerts");gate.Retry();Assert(gate.Check(28,true),"a failed send must remain retryable");
   status.GlobalToastsEnabled=0;Assert(!status.CanRequest,"alternate global switch off");
   var current=NotificationStatus.Capture();current.Save();
   Console.WriteLine("PASS: "+count+" notification permission and retry checks. Current platform="+current.PlatformSetting+", toast="+current.ToastEnabled+", shell="+current.ShellState);return 0;
  }catch(Exception error){Console.WriteLine("FAIL: "+error.Message);return 1;}}
 }
}
