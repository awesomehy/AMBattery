using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;
using System.Reflection;

namespace AmBattery {
 public enum BrandMark {AngryMiao,NoWire}
 public static class BrandArtwork {
  static readonly Bitmap[] originals=new Bitmap[2],light=new Bitmap[2];
  static Bitmap Image(BrandMark mark){int index=(int)mark;if(originals[index]==null){string key="AmBattery.Brand."+(mark==BrandMark.AngryMiao?"AngryMiao":"NoWire");using(var stream=Assembly.GetExecutingAssembly().GetManifestResourceStream(key)){if(stream==null)throw new InvalidOperationException("缺少品牌图片资源。");using(var source=System.Drawing.Image.FromStream(stream))originals[index]=new Bitmap(source);}}if(UiTheme.Dark)return originals[index];if(light[index]==null){var source=originals[index];var image=new Bitmap(source.Width,source.Height,PixelFormat.Format32bppPArgb);using(var graphics=Graphics.FromImage(image))using(var attributes=new ImageAttributes()){var color=Color.FromArgb(36,44,57);attributes.SetColorMatrix(new ColorMatrix(new[]{new float[]{0,0,0,0,0},new float[]{0,0,0,0,0},new float[]{0,0,0,0,0},new float[]{0,0,0,1,0},new[]{color.R/255f,color.G/255f,color.B/255f,0f,1f}}));graphics.DrawImage(source,new Rectangle(0,0,image.Width,image.Height),0,0,source.Width,source.Height,GraphicsUnit.Pixel,attributes);}light[index]=image;}return light[index];}
  public static void Draw(Graphics graphics,RectangleF area,BrandMark mark){var image=Image(mark);float ratio=Math.Min(area.Width/image.Width,area.Height/image.Height);var target=new RectangleF(area.Left+(area.Width-image.Width*ratio)/2,area.Top+(area.Height-image.Height*ratio)/2,image.Width*ratio,image.Height*ratio);var saved=graphics.Save();try{graphics.InterpolationMode=InterpolationMode.HighQualityBicubic;graphics.PixelOffsetMode=PixelOffsetMode.HighQuality;graphics.DrawImage(image,target);}finally{graphics.Restore(saved);}}
 }
}
