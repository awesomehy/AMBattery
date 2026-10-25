using System;
using System.Drawing;
using System.Drawing.Drawing2D;

namespace AmBattery {
 public static class TaskbarMouseArtwork {
  // Lucide Mouse, selected candidate A. Original SVG: assets/icon-candidates/a.svg.
  // ISC license and attribution: assets/licenses/lucide-LICENSE.txt.
  static readonly GraphicsPath outline=UiPaint.Round(new RectangleF(5,2,14,20),7);
  public static void Draw(Graphics graphics,RectangleF area,Color color){
   float size=Math.Min(area.Width,area.Height);var saved=graphics.Save();
   try{graphics.TranslateTransform(area.Left+(area.Width-size)/2,area.Top+(area.Height-size)/2);graphics.ScaleTransform(size/24,size/24);graphics.SmoothingMode=SmoothingMode.AntiAlias;using(var pen=new Pen(color,2)){pen.StartCap=LineCap.Round;pen.EndCap=LineCap.Round;pen.LineJoin=LineJoin.Round;graphics.DrawPath(pen,outline);graphics.DrawLine(pen,12,6,12,10);}}
   finally{graphics.Restore(saved);}
  }
 }
}
