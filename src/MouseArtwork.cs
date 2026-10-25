using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;
using System.IO;
using System.Reflection;
using System.Runtime.InteropServices;

namespace AmBattery {
 public static class MouseArtwork {
  static readonly Bitmap image=Load();
  static Icon icon;
  public static Icon Icon {get{if(icon==null){using(var bitmap=CreateBitmap(64)){IntPtr handle=bitmap.GetHicon();try{icon=(Icon)System.Drawing.Icon.FromHandle(handle).Clone();}finally{Native.DestroyIcon(handle);}}}return icon;}}
  static Bitmap Load(){
   using(var stream=Assembly.GetExecutingAssembly().GetManifestResourceStream("AmBattery.MouseArtwork")){
    if(stream==null)throw new InvalidOperationException("缺少官网鼠标图片资源，请重新构建程序。");
    #if ASSET_BUILD
    using(var source=Image.FromStream(stream))using(var full=new Bitmap(source.Width,source.Height,PixelFormat.Format32bppArgb)){
     using(var graphics=Graphics.FromImage(full))graphics.DrawImageUnscaled(source,0,0);
     var bits=full.LockBits(new Rectangle(0,0,full.Width,full.Height),ImageLockMode.ReadOnly,PixelFormat.Format32bppArgb);
     int left=full.Width,top=full.Height,right=-1,bottom=-1;
     try{var bytes=new byte[Math.Abs(bits.Stride)*bits.Height];Marshal.Copy(bits.Scan0,bytes,0,bytes.Length);for(int y=0;y<full.Height;y++)for(int x=0;x<full.Width;x++){int row=bits.Stride>=0?y*bits.Stride:(full.Height-1-y)*-bits.Stride;if(bytes[row+x*4+3]<4)continue;left=Math.Min(left,x);right=Math.Max(right,x);top=Math.Min(top,y);bottom=Math.Max(bottom,y);}}
     finally{full.UnlockBits(bits);}
     if(right<left||bottom<top)throw new InvalidDataException("官网鼠标图片没有可见内容。");
     var crop=Rectangle.FromLTRB(Math.Max(0,left-1),Math.Max(0,top-1),Math.Min(full.Width,right+2),Math.Min(full.Height,bottom+2));
     return full.Clone(crop,PixelFormat.Format32bppPArgb);
    }
    #else
    using(var source=Image.FromStream(stream)){var result=new Bitmap(source.Width,source.Height,PixelFormat.Format32bppPArgb);using(var graphics=Graphics.FromImage(result))graphics.DrawImageUnscaled(source,0,0);return result;}
    #endif
   }
  }
  public static void SaveTrimmed(string path){image.Save(path,ImageFormat.Png);}
  public static void Draw(Graphics graphics,RectangleF area){
   float scale=Math.Min(area.Width/image.Width,area.Height/image.Height);float width=image.Width*scale,height=image.Height*scale;
   var destination=new RectangleF(area.Left+(area.Width-width)/2,area.Top+(area.Height-height)/2,width,height);
   var state=graphics.Save();try{graphics.InterpolationMode=InterpolationMode.HighQualityBicubic;graphics.PixelOffsetMode=PixelOffsetMode.HighQuality;graphics.CompositingQuality=CompositingQuality.HighQuality;graphics.DrawImage(image,destination,new RectangleF(0,0,image.Width,image.Height),GraphicsUnit.Pixel);}finally{graphics.Restore(state);}
  }
  public static Bitmap CreateBitmap(int size){var result=new Bitmap(size,size,PixelFormat.Format32bppPArgb);using(var graphics=Graphics.FromImage(result)){graphics.Clear(Color.Transparent);Draw(graphics,new RectangleF(1,1,size-2,size-2));}return result;}
 }
}
