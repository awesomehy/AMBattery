namespace AmBattery {
 public static class BatteryRules {
  public static string Band(int? value){return !value.HasValue?"Unknown":value<30?"Red":value<60?"Yellow":"Green";}
 }
 public sealed class NotificationGate {
  bool notified;
  public bool HasNotified {get{return notified;}}
  public void Retry(){notified=false;}
  public bool Check(int? value,bool enabled){
   if(!value.HasValue)return false;
   if(value>=30){notified=false;return false;}
   if(!enabled||notified)return false;
   notified=true;return true;
  }
 }
}
