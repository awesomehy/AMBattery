using System;
using System.IO;
using System.Reflection;
using System.Runtime.Serialization;
using Microsoft.Win32;

namespace AmBattery {
 [DataContract] public sealed class NotificationStatus {
  [DataMember] public DateTime Timestamp;
  [DataMember] public int? ToastEnabled, GlobalToastsEnabled;
  [DataMember] public bool PolicyDisabled;
  [DataMember] public string PlatformSetting="Unknown",ProbeError="";
  [DataMember] public int ShellState;
  public bool CanRequest {get{return BlockingReason=="";}}
  public bool CanInterrupt {get{return CanRequest&&(ShellState==0||ShellState==5);}}
  public string BlockingReason {get{
   if(PolicyDisabled||PlatformSetting=="DisabledByGroupPolicy")return "Windows 策略禁用了通知，请检查系统通知策略。";
   if(ToastEnabled==0||GlobalToastsEnabled==0)return "Windows 通知总开关已关闭。请打开“通知设置”启用通知后重试。";
   if(PlatformSetting=="DisabledForUser")return "Windows 通知平台仍处于禁用状态。请在“通知设置”中关闭再打开总开关后重试。";
   if(PlatformSetting=="DisabledForApplication")return "Windows 已禁用应用通知，请在“通知设置”中检查本工具的通知权限。";
   if(PlatformSetting=="DisabledByManifest")return "系统未允许该通知身份发送提醒，请保留通知诊断记录。";
   return "";
  }}
  public string InterruptionHint {get{
   switch(ShellState){case 1:return "系统当前处于锁屏或非活动会话，通知可能暂不弹出。";case 2:case 3:return "系统处于全屏或游戏状态，通知可能被静默；请离开全屏后再测试。";case 4:return "系统处于演示模式，通知可能被静默。";case 6:return "系统处于通知安静时段，提醒可能暂不弹出。";case 7:return "当前应用状态可能限制通知横幅。";default:return "";}
  }}
  static int? ReadDword(RegistryKey root,string path,string name){try{using(var key=root.OpenSubKey(path)){object value=key==null?null:key.GetValue(name);return value==null?(int?)null:Convert.ToInt32(value);}}catch{return null;}}
  public static NotificationStatus Capture(){
   var status=new NotificationStatus{Timestamp=DateTime.Now};
   status.ToastEnabled=ReadDword(Registry.CurrentUser,@"Software\Microsoft\Windows\CurrentVersion\PushNotifications","ToastEnabled");
   status.GlobalToastsEnabled=ReadDword(Registry.CurrentUser,@"Software\Microsoft\Windows\CurrentVersion\Notifications\Settings","NOC_GLOBAL_SETTING_TOASTS_ENABLED");
   string policy=@"Software\Policies\Microsoft\Windows\CurrentVersion\PushNotifications";
   status.PolicyDisabled=ReadDword(Registry.CurrentUser,policy,"NoToastApplicationNotification")==1||ReadDword(Registry.LocalMachine,policy,"NoToastApplicationNotification")==1;
   try{
    var manager=Type.GetType("Windows.UI.Notifications.ToastNotificationManager, Windows.UI.Notifications, ContentType=WindowsRuntime",true);
    var notifier=manager.GetMethod("CreateToastNotifier",new[]{typeof(string)}).Invoke(null,new object[]{"AmBattery.Desktop"});
    status.PlatformSetting=notifier.GetType().GetProperty("Setting").GetValue(notifier,null).ToString();
   }catch(Exception error){var failure=error.GetBaseException();if(failure.HResult==unchecked((int)0x80070490))status.PlatformSetting="LegacyTrayIdentity";else status.ProbeError=failure.Message;}
   try{int shell;if(Native.SHQueryUserNotificationState(out shell)==0)status.ShellState=shell;}catch{}
   return status;
  }
  public void Save(){Settings.Write(Path.Combine(Settings.DataDirectory,"notification-status.json"),this);}
 }
}
