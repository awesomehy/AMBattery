using System;
using System.Reflection;
namespace AmBattery {
 public static class NotificationProbe {
  public static void Main(){
   foreach(var assembly in new[]{"Windows.UI.Notifications","Windows"})try{
    var manager=Type.GetType("Windows.UI.Notifications.ToastNotificationManager, "+assembly+", ContentType=WindowsRuntime",true);
    var notifier=manager.GetMethod("CreateToastNotifier",new[]{typeof(string)}).Invoke(null,new object[]{"AmBattery.Desktop"});
    Console.WriteLine(assembly+" Setting="+notifier.GetType().GetProperty("Setting").GetValue(notifier,null));
   }catch(Exception e){Console.WriteLine(assembly+" Error="+e.GetBaseException().Message);}
  }
 }
}
