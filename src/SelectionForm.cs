using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Windows.Forms;

namespace ScreenOcrTool
{
    internal sealed class SelectionForm : Form
    {
        private readonly Bitmap _screenshot;
        private readonly Rectangle _screenBounds;
        private Point _dragStart;
        private Point _dragCurrent;
        private bool _dragging;
        private Rectangle _selection;

        public Rectangle SelectedRect { get; private set; }

        public SelectionForm(Bitmap screenshot, Rectangle screenBounds)
        {
            _screenshot = screenshot;
            _screenBounds = screenBounds;
            SelectedRect = Rectangle.Empty;

            AutoScaleMode = AutoScaleMode.None;
            FormBorderStyle = FormBorderStyle.None;
            ShowInTaskbar = false;
            StartPosition = FormStartPosition.Manual;
            Bounds = screenBounds;
            TopMost = true;
            KeyPreview = true;
            Cursor = Cursors.Cross;
            BackColor = Color.Black;
            DoubleBuffered = true;
            SetStyle(
                ControlStyles.OptimizedDoubleBuffer |
                ControlStyles.AllPaintingInWmPaint |
                ControlStyles.UserPaint,
                true);
            UpdateStyles();

            KeyDown += SelectionForm_KeyDown;
            MouseDown += SelectionForm_MouseDown;
            MouseMove += SelectionForm_MouseMove;
            MouseUp += SelectionForm_MouseUp;
            FormClosing += SelectionForm_FormClosing;
        }

        protected override void OnShown(EventArgs e)
        {
            base.OnShown(e);
            Activate();
            Focus();
            BringToFront();
        }

        protected override void OnPaintBackground(PaintEventArgs e)
        {
            // 整张图都在 OnPaint 中绘制，避免闪烁。
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            Graphics graphics = e.Graphics;
            graphics.PageUnit = GraphicsUnit.Pixel;

            if (_screenshot != null)
            {
                graphics.DrawImageUnscaled(_screenshot, 0, 0);
            }
            else
            {
                graphics.Clear(Color.Black);
            }

            using (SolidBrush dim = new SolidBrush(Color.FromArgb(112, 0, 0, 0)))
            {
                graphics.FillRectangle(dim, ClientRectangle);
            }

            if (_selection.Width > 0 && _selection.Height > 0)
            {
                graphics.DrawImage(
                    _screenshot,
                    _selection,
                    _selection.X,
                    _selection.Y,
                    _selection.Width,
                    _selection.Height,
                    GraphicsUnit.Pixel);

                Rectangle border = new Rectangle(
                    _selection.X - 1,
                    _selection.Y - 1,
                    _selection.Width + 1,
                    _selection.Height + 1);

                using (Pen whitePen = new Pen(Color.FromArgb(220, 255, 255, 255), 1f))
                {
                    graphics.DrawRectangle(
                        whitePen,
                        border.X - 1,
                        border.Y - 1,
                        border.Width + 1,
                        border.Height + 1);
                }

                using (Pen bluePen = new Pen(Theme.Primary, 2f))
                {
                    graphics.DrawRectangle(bluePen, border);
                }

                DrawHandles(graphics, _selection);
                DrawSizeBadge(graphics, _selection);
            }
            else if (!_dragging)
            {
                DrawGuideCrosshair(graphics);
            }

            if (!_dragging)
            {
                DrawInstructionCard(graphics);
            }
        }

        private void DrawInstructionCard(Graphics graphics)
        {
            int cardWidth = Math.Min(560, Math.Max(360, ClientSize.Width - 80));
            const int cardHeight = 86;
            Rectangle card = new Rectangle(
                (ClientSize.Width - cardWidth) / 2,
                26,
                cardWidth,
                cardHeight);

            Rectangle shadow = new Rectangle(card.X + 3, card.Y + 4, card.Width, card.Height);
            Theme.FillRounded(graphics, shadow, Color.FromArgb(55, 0, 0, 0), 14);
            Theme.DrawRounded(
                graphics,
                card,
                Color.FromArgb(222, 17, 24, 39),
                Color.FromArgb(80, 255, 255, 255),
                14,
                1f);

            Rectangle titleRect = new Rectangle(card.X + 12, card.Y + 14, card.Width - 24, 30);
            Rectangle hintRect = new Rectangle(card.X + 12, card.Y + 46, card.Width - 24, 26);

            TextRenderer.DrawText(
                graphics,
                "拖动鼠标，框选要识别的区域",
                Theme.ButtonFont,
                titleRect,
                Color.White,
                TextFormatFlags.HorizontalCenter |
                TextFormatFlags.VerticalCenter |
                TextFormatFlags.SingleLine |
                TextFormatFlags.NoPadding);

            TextRenderer.DrawText(
                graphics,
                "按 Esc 或点击鼠标右键取消",
                Theme.SmallFont,
                hintRect,
                Color.FromArgb(185, 255, 255, 255),
                TextFormatFlags.HorizontalCenter |
                TextFormatFlags.VerticalCenter |
                TextFormatFlags.SingleLine |
                TextFormatFlags.NoPadding);
        }

        private void DrawGuideCrosshair(Graphics graphics)
        {
            Point position = PointToClient(Cursor.Position);
            if (!ClientRectangle.Contains(position))
            {
                return;
            }

            using (Pen pen = new Pen(Color.FromArgb(55, 255, 255, 255), 1f))
            {
                pen.DashStyle = DashStyle.Dash;
                graphics.DrawLine(pen, position.X, 0, position.X, ClientSize.Height);
                graphics.DrawLine(pen, 0, position.Y, ClientSize.Width, position.Y);
            }
        }

        private static void DrawHandles(Graphics graphics, Rectangle selection)
        {
            const int size = 7;
            Point[] points = new Point[]
            {
                new Point(selection.Left, selection.Top),
                new Point(selection.Right, selection.Top),
                new Point(selection.Left, selection.Bottom),
                new Point(selection.Right, selection.Bottom)
            };

            foreach (Point point in points)
            {
                Rectangle handle = new Rectangle(point.X - size / 2, point.Y - size / 2, size, size);
                using (SolidBrush brush = new SolidBrush(Color.White))
                {
                    graphics.FillRectangle(brush, handle);
                }
                using (Pen pen = new Pen(Theme.Primary, 2f))
                {
                    graphics.DrawRectangle(pen, handle);
                }
            }
        }

        private void DrawSizeBadge(Graphics graphics, Rectangle selection)
        {
            string text = selection.Width.ToString() + " × " + selection.Height.ToString();
            Size measured = TextRenderer.MeasureText(
                graphics,
                text,
                Theme.SmallFont,
                new Size(int.MaxValue, int.MaxValue),
                TextFormatFlags.NoPadding | TextFormatFlags.SingleLine);

            int width = measured.Width + 18;
            int height = 26;
            int x = Math.Min(ClientSize.Width - width - 8, selection.Right - width);
            int y = selection.Bottom + 9;
            if (y + height > ClientSize.Height - 8)
            {
                y = selection.Top - height - 9;
            }
            if (y < 8)
            {
                y = selection.Bottom - height - 9;
            }
            x = Math.Max(8, x);

            Rectangle badge = new Rectangle(x, y, width, height);
            Theme.DrawRounded(graphics, badge, Color.FromArgb(220, 17, 24, 39), Color.FromArgb(50, 255, 255, 255), 8, 1f);
            TextRenderer.DrawText(
                graphics,
                text,
                Theme.SmallFont,
                badge,
                Color.White,
                TextFormatFlags.HorizontalCenter |
                TextFormatFlags.VerticalCenter |
                TextFormatFlags.SingleLine |
                TextFormatFlags.NoPadding);
        }

        private void SelectionForm_MouseDown(object sender, MouseEventArgs e)
        {
            if (e.Button == MouseButtons.Right)
            {
                DialogResult = DialogResult.Cancel;
                Close();
                return;
            }

            if (e.Button != MouseButtons.Left)
            {
                return;
            }

            _dragging = true;
            _dragStart = ClampToClient(e.Location);
            _dragCurrent = _dragStart;
            _selection = Rectangle.Empty;
            Capture = true;
            Invalidate();
        }

        private void SelectionForm_MouseMove(object sender, MouseEventArgs e)
        {
            if (!_dragging)
            {
                Invalidate();
                return;
            }

            _dragCurrent = ClampToClient(e.Location);
            _selection = NormalizeRect(_dragStart, _dragCurrent);
            Invalidate();
        }

        private void SelectionForm_MouseUp(object sender, MouseEventArgs e)
        {
            if (!_dragging || e.Button != MouseButtons.Left)
            {
                return;
            }

            _dragging = false;
            Capture = false;
            _dragCurrent = ClampToClient(e.Location);
            _selection = NormalizeRect(_dragStart, _dragCurrent);

            if (_selection.Width < 8 || _selection.Height < 8)
            {
                _selection = Rectangle.Empty;
                Invalidate();
                return;
            }

            // 确保框选范围不超出当前屏幕截图。
            Rectangle validArea = new Rectangle(0, 0, _screenBounds.Width, _screenBounds.Height);
            _selection.Intersect(validArea);
            if (_selection.Width < 8 || _selection.Height < 8)
            {
                _selection = Rectangle.Empty;
                Invalidate();
                return;
            }

            SelectedRect = _selection;
            DialogResult = DialogResult.OK;
            Close();
        }

        private void SelectionForm_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.KeyCode == Keys.Escape)
            {
                e.Handled = true;
                DialogResult = DialogResult.Cancel;
                Close();
            }
        }

        private void SelectionForm_FormClosing(object sender, FormClosingEventArgs e)
        {
            if (DialogResult == DialogResult.None)
            {
                DialogResult = DialogResult.Cancel;
            }
        }

        private Point ClampToClient(Point point)
        {
            int x = Math.Max(0, Math.Min(ClientSize.Width - 1, point.X));
            int y = Math.Max(0, Math.Min(ClientSize.Height - 1, point.Y));
            return new Point(x, y);
        }

        private static Rectangle NormalizeRect(Point first, Point second)
        {
            int left = Math.Min(first.X, second.X);
            int top = Math.Min(first.Y, second.Y);
            int width = Math.Abs(first.X - second.X);
            int height = Math.Abs(first.Y - second.Y);
            return new Rectangle(left, top, width, height);
        }
    }
}
