using System;
using System.Drawing;
using System.IO;
using System.Runtime.InteropServices;
using System.Reflection;
using System.Windows.Forms;

namespace AmBattery {
 public class Tests {
  static int count;
  static void Assert(bool value,string message){if(!value)throw new Exception(message);count++;}
  [STAThread] public static void Main(){
   Assert(BatteryRules.Band(0)=="Red","0 red");Assert(BatteryRules.Band(29)=="Red","29 red");Assert(BatteryRules.Band(30)=="Yellow","30 yellow");Assert(BatteryRules.Band(59)=="Yellow","59 yellow");Assert(BatteryRules.Band(60)=="Green","60 green");Assert(BatteryRules.Band(100)=="Green","100 green");Assert(BatteryRules.Band(null)=="Unknown","unknown");
   var gate=new NotificationGate();Assert(!gate.Check(100,true),"full no alert");Assert(!gate.Check(30,true),"30 no alert");Assert(gate.Check(29,true),"crossing alerts");Assert(!gate.Check(28,true),"low duplicate suppressed");Assert(!gate.Check(null,true),"disconnected no alert");Assert(!gate.Check(28,true),"reconnect no duplicate");Assert(!gate.Check(30,true),"rearm");Assert(gate.Check(29,true),"next cycle alerts");
   var disabled=new NotificationGate();Assert(!disabled.Check(12,false),"disabled");Assert(disabled.Check(12,true),"enabling while low");
   var reading=new BatteryReading();Assert(HidDevice.DecodeMouse(new byte[]{0xd6,0,1,0},reading)&&reading.Mouse==0,"zero battery accepted");Assert(HidDevice.DecodeMouse(new byte[]{0xd6,1,1,45},reading)&&reading.MouseState=="充电中","charging decode");Assert(!HidDevice.DecodeMouse(new byte[]{0xd6,0,0,100},new BatteryReading()),"not present");Assert(!HidDevice.DecodeMouse(new byte[]{0xd6,0,1,255},new BatteryReading()),"invalid percentage");Assert(!HidDevice.DecodeMouse(new byte[]{0xf7,0,1,80},new BatteryReading()),"wrong response");Assert(!HidDevice.DecodeMouse(new byte[0],new BatteryReading()),"short response");
   string path=Path.Combine(AppDomain.CurrentDomain.BaseDirectory,"test-settings.json");var s=Settings.Defaults();s.Position="Left";s.Offset=47;Settings.Write(path,s);var restored=Settings.Read<Settings>(path);Assert(restored.Position=="Left"&&restored.Offset==47&&restored.Monitors.Count==s.Monitors.Count,"settings persistence");
   using(var bmp=new Bitmap(440,100))using(var g=Graphics.FromImage(bmp)){g.Clear(Palette.Background);int?[] values={100,60,59,30,29,null};for(int i=0;i<values.Length;i++)Palette.Ring(g,new RectangleF(i*72+4,17,60,60),values[i],16);bmp.Save(Path.Combine(AppDomain.CurrentDomain.BaseDirectory,"ring-render.png"));}
   Assert(Marshal.SizeOf(typeof(Native.BLENDFUNCTION))==4,"layered blend layout");Assert(Marshal.SizeOf(typeof(Native.POINT))==8,"layered point layout");
   using(var frame=TaskbarWidget.RenderFrame(78,48,1,100)){Assert(frame.GetPixel(0,0).A==1,"transparent clickable background");int solid=0;for(int x=0;x<frame.Width;x++)for(int y=0;y<frame.Height;y++)if(frame.GetPixel(x,y).A>200)solid++;Assert(solid>100,"ring and percentage are drawn");frame.Save(Path.Combine(AppDomain.CurrentDomain.BaseDirectory,"widget-frame.png"));}
   using(var menu=new ContextMenuStrip())using(var widget=new TaskbarWidget(menu)){
    Assert(!widget.Visible,"render test starts hidden");var present=typeof(TaskbarWidget).GetMethod("PresentFrame",BindingFlags.NonPublic|BindingFlags.Instance);bool drawn=(bool)present.Invoke(widget,new object[]{0,0,78,48});Assert(drawn,"per-pixel layered bitmap accepted: "+widget.LastError);Assert(!Native.IsWindowVisible(widget.Handle),"native rendering test does not show a window");
   }
   Console.WriteLine("PASS: "+count+" checks. Ring rendering saved.");
   var d=new Native.DISPLAY_DEVICE();d.Size=Marshal.SizeOf(d);Console.WriteLine("DISPLAY_DEVICE size="+d.Size+" enum="+Native.EnumDisplayDevices(null,0,ref d,0)+" name="+d.Name+" id="+d.ID);
   Native.Diagnose();
  }
 }
}
