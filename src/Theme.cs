using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Windows.Forms;

namespace ScreenOcrTool
{
    internal static class Theme
    {
        public static readonly Color Background = Color.FromArgb(245, 247, 251);
        public static readonly Color Card = Color.White;
        public static readonly Color Primary = Color.FromArgb(37, 99, 235);
        public static readonly Color PrimaryDark = Color.FromArgb(29, 78, 216);
        public static readonly Color PrimaryDarker = Color.FromArgb(30, 64, 175);
        public static readonly Color Text = Color.FromArgb(31, 41, 55);
        public static readonly Color Muted = Color.FromArgb(107, 114, 128);
        public static readonly Color Border = Color.FromArgb(226, 232, 240);
        public static readonly Color Success = Color.FromArgb(5, 150, 105);
        public static readonly Color Danger = Color.FromArgb(220, 38, 38);

        public static readonly Font TitleFont = CreateFont(22f, FontStyle.Bold);
        public static readonly Font SubtitleFont = CreateFont(10f, FontStyle.Regular);
        public static readonly Font ButtonFont = CreateFont(13f, FontStyle.Bold);
        public static readonly Font SectionFont = CreateFont(10.5f, FontStyle.Bold);
        public static readonly Font BodyFont = CreateFont(10.5f, FontStyle.Regular);
        public static readonly Font ResultFont = CreateFont(11.5f, FontStyle.Regular);
        public static readonly Font SmallFont = CreateFont(9f, FontStyle.Regular);
        public static readonly Font BadgeFont = CreateFont(9f, FontStyle.Regular);

        private static Font CreateFont(float size, FontStyle style)
        {
            try
            {
                return new Font("Microsoft YaHei UI", size, style, GraphicsUnit.Point);
            }
            catch
            {
                return new Font(FontFamily.GenericSansSerif, size, style, GraphicsUnit.Point);
            }
        }

        public static GraphicsPath RoundedRect(Rectangle bounds, int radius)
        {
            int diameter = Math.Max(1, radius * 2);
            GraphicsPath path = new GraphicsPath();
            if (radius <= 0)
            {
                path.AddRectangle(bounds);
                path.CloseFigure();
                return path;
            }

            path.AddArc(bounds.Left, bounds.Top, diameter, diameter, 180, 90);
            path.AddArc(bounds.Right - diameter, bounds.Top, diameter, diameter, 270, 90);
            path.AddArc(bounds.Right - diameter, bounds.Bottom - diameter, diameter, diameter, 0, 90);
            path.AddArc(bounds.Left, bounds.Bottom - diameter, diameter, diameter, 90, 90);
            path.CloseFigure();
            return path;
        }

        public static void FillRounded(Graphics g, Rectangle bounds, Color color, int radius)
        {
            using (GraphicsPath path = RoundedRect(bounds, radius))
            using (SolidBrush brush = new SolidBrush(color))
            {
                g.FillPath(brush, path);
            }
        }

        public static void DrawRounded(Graphics g, Rectangle bounds, Color fill, Color border, int radius, float borderWidth)
        {
            using (GraphicsPath path = RoundedRect(bounds, radius))
            {
                using (SolidBrush brush = new SolidBrush(fill))
                {
                    g.FillPath(brush, path);
                }
                if (borderWidth > 0)
                {
                    using (Pen pen = new Pen(border, borderWidth))
                    {
                        g.DrawPath(pen, path);
                    }
                }
            }
        }
    }

    internal sealed class BorderedPanel : Panel
    {
        public Color BorderColor { get; set; }

        public BorderedPanel()
        {
            BorderColor = Theme.Border;
            SetStyle(ControlStyles.OptimizedDoubleBuffer | ControlStyles.AllPaintingInWmPaint | ControlStyles.UserPaint, true);
            UpdateStyles();
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            base.OnPaint(e);
            using (Pen pen = new Pen(BorderColor, 1f))
            {
                e.Graphics.DrawRectangle(pen, 0, 0, Width - 1, Height - 1);
            }
        }
    }
}
