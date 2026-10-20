using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;
using System.IO;

namespace CLICKO {
    public static class IconGenerator {
        public static void GenerateIconFile(string outputPath, int size) {
            using (Bitmap bmp = CreateIconBitmap(size)) {
                SaveBitmapAsIcon(bmp, outputPath, size);
            }
        }

        public static Bitmap CreateIconBitmap(int size) {
            Bitmap bmp = new Bitmap(size, size, PixelFormat.Format32bppArgb);
            using (Graphics g = Graphics.FromImage(bmp)) {
                g.SmoothingMode = SmoothingMode.AntiAlias;
                g.InterpolationMode = InterpolationMode.HighQualityBicubic;
                g.PixelOffsetMode = PixelOffsetMode.HighQuality;
                g.Clear(Color.Transparent);

                // Modern rounded squircle background
                int r = (int)(size * 0.22f);
                Rectangle rect = new Rectangle(2, 2, size - 4, size - 4);
                using (GraphicsPath path = new GraphicsPath()) {
                    path.AddArc(rect.X, rect.Y, r * 2, r * 2, 180, 90);
                    path.AddArc(rect.Right - r * 2, rect.Y, r * 2, r * 2, 270, 90);
                    path.AddArc(rect.Right - r * 2, rect.Bottom - r * 2, r * 2, r * 2, 0, 90);
                    path.AddArc(rect.X, rect.Bottom - r * 2, r * 2, r * 2, 90, 90);
                    path.CloseFigure();

                    // Deep navy-violet gradient
                    using (LinearGradientBrush bgBrush = new LinearGradientBrush(
                        rect,
                        Color.FromArgb(20, 22, 36),
                        Color.FromArgb(10, 11, 20),
                        LinearGradientMode.ForwardDiagonal)) {
                        g.FillPath(bgBrush, path);
                    }

                    // Electric border
                    using (Pen borderPen = new Pen(Color.FromArgb(59, 130, 246), Math.Max(1.5f, size * 0.035f))) {
                        g.DrawPath(borderPen, path);
                    }
                }

                // Mouse Body
                float mw = size * 0.44f;
                float mh = size * 0.65f;
                float mx = (size - mw) / 2f;
                float my = (size - mh) / 2f + size * 0.04f;
                float mr = mw * 0.48f;

                using (GraphicsPath mousePath = new GraphicsPath()) {
                    mousePath.AddArc(mx, my, mr * 2, mr * 2, 180, 90);
                    mousePath.AddArc(mx + mw - mr * 2, my, mr * 2, mr * 2, 270, 90);
                    mousePath.AddArc(mx + mw - mr * 2, my + mh - mr * 2, mr * 2, mr * 2, 0, 90);
                    mousePath.AddArc(mx, my + mh - mr * 2, mr * 2, mr * 2, 90, 90);
                    mousePath.CloseFigure();

                    using (LinearGradientBrush mouseFill = new LinearGradientBrush(
                        new RectangleF(mx, my, mw, mh),
                        Color.FromArgb(40, 48, 74),
                        Color.FromArgb(25, 30, 48),
                        LinearGradientMode.Vertical)) {
                        g.FillPath(mouseFill, mousePath);
                    }

                    using (Pen mousePen = new Pen(Color.FromArgb(96, 165, 250), Math.Max(1.0f, size * 0.025f))) {
                        g.DrawPath(mousePen, mousePath);
                    }
                }

                // Left Click Button Highlight (Neon Cyan/Blue)
                using (GraphicsPath leftBtn = new GraphicsPath()) {
                    leftBtn.AddArc(mx + 1, my + 1, mr * 2 - 2, mr * 2 - 2, 180, 90);
                    leftBtn.AddLine(mx + mw / 2f - 1, my + 1, mx + mw / 2f - 1, my + mh * 0.42f);
                    leftBtn.AddLine(mx + mw / 2f - 1, my + mh * 0.42f, mx + 1, my + mh * 0.42f);
                    leftBtn.CloseFigure();

                    using (SolidBrush leftBrush = new SolidBrush(Color.FromArgb(210, 6, 182, 212))) {
                        g.FillPath(leftBrush, leftBtn);
                    }
                }

                // Middle scroll wheel (Vibrant Orange)
                float ww = Math.Max(3f, mw * 0.18f);
                float wh = Math.Max(6f, mh * 0.22f);
                float wx = (size - ww) / 2f;
                float wy = my + mh * 0.12f;
                using (GraphicsPath wheelPath = new GraphicsPath()) {
                    float wr = ww / 2f;
                    wheelPath.AddArc(wx, wy, wr * 2, wr * 2, 180, 180);
                    wheelPath.AddArc(wx, wy + wh - wr * 2, wr * 2, wr * 2, 0, 180);
                    wheelPath.CloseFigure();

                    using (SolidBrush wheelBrush = new SolidBrush(Color.FromArgb(249, 115, 22))) {
                        g.FillPath(wheelBrush, wheelPath);
                    }
                }

                // Center divider groove
                using (Pen divPen = new Pen(Color.FromArgb(15, 20, 32), Math.Max(1.0f, size * 0.02f))) {
                    g.DrawLine(divPen, size / 2f, my + 1, size / 2f, my + mh * 0.42f);
                    g.DrawLine(divPen, mx + 1, my + mh * 0.42f, mx + mw - 1, my + mh * 0.42f);
                }
            }
            return bmp;
        }

        public static void SaveBitmapAsIcon(Bitmap bmp, string filePath, int size) {
            using (MemoryStream ms = new MemoryStream()) {
                bmp.Save(ms, ImageFormat.Png);
                byte[] pngBytes = ms.ToArray();

                using (FileStream fs = new FileStream(filePath, FileMode.Create))
                using (BinaryWriter bw = new BinaryWriter(fs)) {
                    bw.Write((ushort)0); // reserved
                    bw.Write((ushort)1); // icon type
                    bw.Write((ushort)1); // 1 image
                    bw.Write((byte)(size >= 256 ? 0 : size)); // width
                    bw.Write((byte)(size >= 256 ? 0 : size)); // height
                    bw.Write((byte)0);    // colors
                    bw.Write((byte)0);    // reserved
                    bw.Write((ushort)1);  // planes
                    bw.Write((ushort)32); // bpp
                    bw.Write((uint)pngBytes.Length); // size
                    bw.Write((uint)22);   // offset
                    bw.Write(pngBytes);   // data
                }
            }
        }

    }
}
