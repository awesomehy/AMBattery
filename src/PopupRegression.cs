using System;
using System.Drawing;
using System.Reflection;
using System.Windows.Forms;

namespace AmBattery {
 public static class PopupRegression {
  [STAThread] public static int Main(){
   try{
    using(var popup=new BatteryPopup()){
     var handle=popup.Handle;
     for(int i=0;i<5;i++){
      popup.Close();
      if(popup.IsDisposed)throw new Exception("Closing the details window disposed it; a second open will fail.");
      if(popup.Handle!=handle)throw new Exception("A normal close unexpectedly destroyed the reusable popup handle.");
      popup.Reading=new BatteryReading{Mouse=68-i,Dock=92,Timestamp=DateTime.Now};
      popup.Invalidate();
     }
     var deactivate=typeof(BatteryPopup).GetMethod("OnDeactivate",BindingFlags.NonPublic|BindingFlags.Instance);
     deactivate.Invoke(popup,new object[]{EventArgs.Empty});
     if(popup.IsDisposed||popup.Handle!=handle)throw new Exception("Focus loss destroyed the reusable popup.");
     var closing=typeof(BatteryPopup).GetMethod("OnFormClosing",BindingFlags.NonPublic|BindingFlags.Instance);
     foreach(var reason in new[]{CloseReason.ApplicationExitCall,CloseReason.WindowsShutDown}){
      var args=new FormClosingEventArgs(reason,false);closing.Invoke(popup,new object[]{args});
      if(args.Cancel)throw new Exception("The popup blocked application exit or Windows shutdown.");
     }
     var key=typeof(BatteryPopup).GetMethod("ProcessCmdKey",BindingFlags.NonPublic|BindingFlags.Instance);
     var message=Message.Create(handle,0x100,new IntPtr((int)Keys.Escape),IntPtr.Zero);
     if(!(bool)key.Invoke(popup,new object[]{message,Keys.Escape})||popup.IsDisposed)throw new Exception("Escape did not keep the popup reusable.");
     popup.AllowClose=true;popup.Close();if(!popup.IsDisposed)throw new Exception("The popup could not be disposed during application exit.");
    }
    Console.WriteLine("PASS: five close/reuse cycles, focus-loss lifecycle, reading updates, Escape dismissal, application exit and Windows shutdown.");return 0;
   }catch(Exception error){Console.WriteLine("FAIL: "+error.Message);return 1;}
  }
 }
}
