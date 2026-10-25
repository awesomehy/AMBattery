using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;
using System.IO;

namespace AmBattery {
 public static class AssetBuilder {
  public static void Main(){
   MouseArtwork.SaveTrimmed("assets/am-infinity-mouse-ui.png");
   using(var source=Image.FromFile("assets/am-infinity-dongle-review.png")){
    float ratio=Math.Min(1,128f/Math.Max(source.Width,source.Height));
    using(var optimized=new Bitmap(Math.Max(1,(int)Math.Round(source.Width*ratio)),Math.Max(1,(int)Math.Round(source.Height*ratio)),PixelFormat.Format32bppPArgb)){
     using(var graphics=Graphics.FromImage(optimized)){graphics.Clear(Color.Transparent);graphics.InterpolationMode=InterpolationMode.HighQualityBicubic;graphics.PixelOffsetMode=PixelOffsetMode.HighQuality;graphics.DrawImage(source,new Rectangle(0,0,optimized.Width,optimized.Height));}
     optimized.Save("assets/am-infinity-dongle-ui.png",ImageFormat.Png);
    }
   }
   int[] sizes={16,24,32,48,64,128,256};var payloads=new List<byte[]>();
   foreach(int size in sizes)using(var bitmap=MouseArtwork.CreateBitmap(size))using(var memory=new MemoryStream()){bitmap.Save(memory,ImageFormat.Png);payloads.Add(memory.ToArray());}
   using(var writer=new BinaryWriter(File.Create("assets/am-battery.ico"))){writer.Write((ushort)0);writer.Write((ushort)1);writer.Write((ushort)sizes.Length);int offset=6+16*sizes.Length;for(int i=0;i<sizes.Length;i++){writer.Write((byte)(sizes[i]==256?0:sizes[i]));writer.Write((byte)(sizes[i]==256?0:sizes[i]));writer.Write((byte)0);writer.Write((byte)0);writer.Write((ushort)1);writer.Write((ushort)32);writer.Write(payloads[i].Length);writer.Write(offset);offset+=payloads[i].Length;}foreach(var payload in payloads)writer.Write(payload);}
   using(var preview=new Bitmap(390,195,PixelFormat.Format32bppArgb))using(var graphics=Graphics.FromImage(preview))using(var frame=TaskbarWidget.RenderFrame(78,48,1,68)){
    graphics.Clear(Color.FromArgb(32,32,32));using(var font=new Font("Segoe UI",12,FontStyle.Regular,GraphicsUnit.Pixel))using(var brush=new SolidBrush(Color.FromArgb(190,198,192))){graphics.DrawString("100%",font,brush,20,15);graphics.DrawString("3x preview",font,brush,135,15);}
    graphics.DrawImageUnscaled(frame,20,48);graphics.InterpolationMode=InterpolationMode.NearestNeighbor;graphics.PixelOffsetMode=PixelOffsetMode.Half;graphics.DrawImage(frame,new Rectangle(135,38,234,144),0,0,78,48,GraphicsUnit.Pixel);preview.Save("build/official-mouse-preview.png",ImageFormat.Png);
   }
   Console.WriteLine("Official mouse artwork and application icon prepared.");
   using(var preview=new Bitmap(450,212,PixelFormat.Format32bppArgb))using(var graphics=Graphics.FromImage(preview)){
    bool[] modes={true,false};float[] scales={1,1.5f,2};
    for(int row=0;row<modes.Length;row++){bool light=modes[row];using(var background=new SolidBrush(light?Color.FromArgb(236,239,244):Color.FromArgb(32,35,41)))graphics.FillRectangle(background,0,row*106,450,106);for(int column=0;column<scales.Length;column++){float scale=scales[column];using(var frame=TaskbarWidget.RenderFrame((int)(78*scale),(int)(48*scale),scale,68,light))graphics.DrawImageUnscaled(frame,10+column*140,row*106+(106-frame.Height)/2);}}
    preview.Save("build/taskbar-mouse-symbol-preview.png",ImageFormat.Png);
   }
  }
 }
}
