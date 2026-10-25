using System;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Reflection;
using System.Windows.Forms;

namespace AmBattery {
 public static class UiRegression {
  static int checks;
  static void Check(bool passed,string message){if(!passed)throw new Exception(message);checks++;}
  static object Field(object owner,string name){return owner.GetType().GetField(name,BindingFlags.Instance|BindingFlags.NonPublic).GetValue(owner);}
  static void Key(Control control,Keys key){control.GetType().GetMethod("OnKeyDown",BindingFlags.NonPublic|BindingFlags.Instance).Invoke(control,new object[]{new KeyEventArgs(key)});}
  static void Prepare(Control control){var handle=control.Handle;foreach(Control child in control.Controls)Prepare(child);control.PerformLayout();}
  static void Render(Control control,string path){Prepare(control);using(var image=new Bitmap(control.Width,control.Height)){control.DrawToBitmap(image,control.ClientRectangle);image.Save(path,ImageFormat.Png);}}
  static void Bounds(Control parent){foreach(Control child in parent.Controls){Check(child.Left>=0&&child.Top>=0&&child.Right<=parent.ClientSize.Width&&child.Bottom<=parent.ClientSize.Height,"Control outside parent: "+child.GetType().Name+" "+child.Text);Bounds(child);}}
  [STAThread]public static int Main(){
   try{
    try{Native.SetProcessDpiAwarenessContext(new IntPtr(-4));}catch{}
    Application.EnableVisualStyles();Application.SetCompatibleTextRenderingDefault(false);
    string folder=Path.Combine(AppDomain.CurrentDomain.BaseDirectory,"ui-preview");Directory.CreateDirectory(folder);
    var settings=Settings.Defaults();settings.Theme="Dark";
    using(var form=new SettingsForm(settings))using(var popup=new BatteryPopup()){
     popup.Reading=new BatteryReading{Mouse=68,Dock=92,MouseState="使用电池",DockState="充电中",Timestamp=DateTime.Now};
     Bounds(form);Bounds(popup);
     foreach(string mode in new[]{"Light","Dark"}){
      UiTheme.Set(mode);Render(form,Path.Combine(folder,"settings-"+mode.ToLower()+".png"));Render(popup,Path.Combine(folder,"popup-"+mode.ToLower()+".png"));
      Check(form.BackColor==UiTheme.Background&&popup.BackColor==UiTheme.Background,"Theme did not reach forms.");
      var select=(UiSelect)Field(form,"target");using(var options=new UiOptionList(select,38){Size=new Size(select.Width,select.Items.Count*38+12)})Render(options,Path.Combine(folder,"dropdown-"+mode.ToLower()+".png"));
     }
     var target=(UiSelect)Field(form,"target");target.SelectedIndex=0;Key(target,Keys.Down);Check(target.SelectedIndex==Math.Min(1,target.Items.Count-1),"Display selection ignored arrow key.");
     var offset=(UiStepper)Field(form,"offset");offset.Value=0;Key(offset,Keys.Left);Check(offset.Value==0,"Offset underflow.");Key(offset,Keys.Right);Check(offset.Value==1,"Offset key input failed.");offset.Value=1501;Check(offset.Value==1500,"Offset overflow.");offset.Value=12;
     var manage=(UiSwitch)Field(form,"manage");manage.Checked=true;Key(manage,Keys.Space);Check(!manage.Checked,"Switch keyboard toggle failed.");manage.Checked=true;
     Check(!(bool)Field(manage,"timer").GetType().GetProperty("Enabled").GetValue(Field(manage,"timer"),null),"Hidden switch kept an animation timer running.");
     UiTheme.Set("Light");var collect=typeof(SettingsForm).GetMethod("Collect",BindingFlags.Instance|BindingFlags.NonPublic);var value=(Settings)collect.Invoke(form,null);Check(value.Theme=="Light"&&value.Offset==12&&value.ManageTaskbars,"Settings collection lost theme or values.");
     string saved=Path.Combine(folder,"settings-roundtrip.json");Settings.Write(saved,value);Check(Settings.Read<Settings>(saved).Theme=="Light","Theme did not persist.");
     var handle=form.Handle;form.Close();Check(!form.IsDisposed&&form.Handle==handle,"Settings close destroyed reusable window.");
     var closing=typeof(SettingsForm).GetMethod("OnFormClosing",BindingFlags.Instance|BindingFlags.NonPublic);var shutDown=new FormClosingEventArgs(CloseReason.WindowsShutDown,false);closing.Invoke(form,new object[]{shutDown});Check(!shutDown.Cancel,"Settings blocked Windows shutdown.");
     Check(!form.Visible&&!popup.Visible,"Offscreen UI test displayed a window.");
    }
    using(var light=new Bitmap(Path.Combine(folder,"settings-light.png")))using(var dark=new Bitmap(Path.Combine(folder,"settings-dark.png")))using(var overview=new Bitmap(light.Width+dark.Width+72,Math.Max(light.Height,dark.Height)+48))using(var g=Graphics.FromImage(overview)){g.Clear(Color.FromArgb(222,226,233));g.DrawImageUnscaled(light,24,24);g.DrawImageUnscaled(dark,light.Width+48,24);overview.Save(Path.Combine(folder,"themes.png"),ImageFormat.Png);}
    Console.WriteLine("PASS: "+checks+" UI layout, theme, persistence and control checks. Native offscreen previews: "+folder);return 0;
   }catch(Exception error){Console.WriteLine("FAIL: "+error);return 1;}
  }
 }
}
