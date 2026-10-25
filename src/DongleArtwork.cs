using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;
using System.Reflection;

namespace AmBattery {
 public static class DongleArtwork {
  static Bitmap image;
  static Bitmap Load(){using(var stream=Assembly.GetExecutingAssembly().GetManifestResourceStream("AmBattery.DongleArtwork")){if(stream==null)throw new InvalidOperationException("缺少圆形充电仓图片资源。");using(var source=Image.FromStream(stream)){var result=new Bitmap(source.Width,source.Height,PixelFormat.Format32bppPArgb);using(var graphics=Graphics.FromImage(result))graphics.DrawImageUnscaled(source,0,0);return result;}}}
  public static void Draw(Graphics graphics,RectangleF area){if(image==null)image=Load();float ratio=Math.Min(area.Width/image.Width,area.Height/image.Height);var target=new RectangleF(area.Left+(area.Width-image.Width*ratio)/2,area.Top+(area.Height-image.Height*ratio)/2,image.Width*ratio,image.Height*ratio);var saved=graphics.Save();try{graphics.InterpolationMode=InterpolationMode.HighQualityBicubic;graphics.PixelOffsetMode=PixelOffsetMode.HighQuality;graphics.DrawImage(image,target);}finally{graphics.Restore(saved);}}
 }
}
