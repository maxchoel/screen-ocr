using System;
using System.Diagnostics;
using System.Drawing;
using System.Drawing.Imaging;
using System.Globalization;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace ScreenOcrTool
{
    internal sealed class MainForm : Form
    {
        private const int HotkeyId = 0x5343;
        private const int WmHotkey = 0x0312;
        private const uint ModAlt = 0x0001;
        private const uint ModControl = 0x0002;
        private const uint ModNoRepeat = 0x4000;

        private readonly OcrService _ocr;
        private readonly string _ocrError;
        private readonly string _hotkeyText = "Ctrl+Alt+O";

        private Button _captureButton;
        private TextBox _resultBox;
        private Label _emptyHint;
        private Label _countLabel;
        private Label _statusLabel;
        private Label _languageLabel;
        private CheckBox _autoCopy;
        private Button _copyButton;
        private Button _clearButton;

        private bool _busy;
        private bool _hotkeyRegistered;
        private bool _showedOcrError;

        public MainForm()
        {
            string warning = null;
            try
            {
                _ocr = OcrService.Create();
                warning = _ocr.Warning;
            }
            catch (Exception ex)
            {
                _ocr = null;
                _ocrError = ex.Message;
            }

            Text = "屏幕识字";
            ClientSize = new Size(720, 640);
            MinimumSize = new Size(600, 560);
            StartPosition = FormStartPosition.CenterScreen;
            BackColor = Theme.Background;
            ForeColor = Theme.Text;
            Font = Theme.BodyFont;
            AutoScaleMode = AutoScaleMode.Dpi;
            AutoScaleDimensions = new SizeF(96f, 96f);
            KeyPreview = true;

            try
            {
                Icon = Icon.ExtractAssociatedIcon(Application.ExecutablePath);
            }
            catch
            {
                // 没有图标也不影响使用。
            }

            BuildInterface();

            if (_ocr == null)
            {
                SetStatus("系统 OCR 不可用：" + _ocrError, Theme.Danger);
            }
            else if (!string.IsNullOrEmpty(warning))
            {
                SetStatus(warning, Theme.Danger);
            }
            else
            {
                SetStatus("准备就绪。点击按钮或按 " + _hotkeyText + " 开始框选。");
            }

            UpdateUiState();
        }

        protected override void OnHandleCreated(EventArgs e)
        {
            base.OnHandleCreated(e);
            if (!_hotkeyRegistered)
            {
                _hotkeyRegistered = RegisterHotKey(
                    Handle,
                    HotkeyId,
                    ModControl | ModAlt | ModNoRepeat,
                    (uint)Keys.O);
            }
        }

        protected override void OnHandleDestroyed(EventArgs e)
        {
            if (_hotkeyRegistered)
            {
                UnregisterHotKey(Handle, HotkeyId);
                _hotkeyRegistered = false;
            }
            base.OnHandleDestroyed(e);
        }

        protected override void WndProc(ref Message m)
        {
            if (m.Msg == WmHotkey && m.WParam.ToInt32() == HotkeyId)
            {
                if (!_busy)
                {
                    BeginCapture();
                }
                return;
            }

            base.WndProc(ref m);
        }

        protected override void OnShown(EventArgs e)
        {
            base.OnShown(e);
            Activate();

            if (!_hotkeyRegistered)
            {
                SetStatus("提示：全局快捷键被其他软件占用，仍可点击上方按钮框选。", Theme.Danger);
            }

            if (_ocr == null && !_showedOcrError)
            {
                _showedOcrError = true;
                MessageBox.Show(
                    this,
                    "无法启动文字识别。\r\n\r\n" + _ocrError +
                    "\r\n\r\n请确认系统是 Windows 10 或 Windows 11，并且已安装“中文(简体)”语言包。",
                    "屏幕识字",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Warning);
            }
        }

        private void BuildInterface()
        {
            TableLayoutPanel root = new TableLayoutPanel();
            root.Dock = DockStyle.Fill;
            root.BackColor = Theme.Background;
            root.Padding = new Padding(28, 22, 28, 16);
            root.ColumnCount = 1;
            root.RowCount = 7;
            root.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100f));
            root.RowStyles.Add(new RowStyle(SizeType.Absolute, 82f));
            root.RowStyles.Add(new RowStyle(SizeType.Absolute, 58f));
            root.RowStyles.Add(new RowStyle(SizeType.Absolute, 14f));
            root.RowStyles.Add(new RowStyle(SizeType.Absolute, 28f));
            root.RowStyles.Add(new RowStyle(SizeType.Percent, 100f));
            root.RowStyles.Add(new RowStyle(SizeType.Absolute, 52f));
            root.RowStyles.Add(new RowStyle(SizeType.Absolute, 28f));
            Controls.Add(root);

            root.Controls.Add(BuildHeader(), 0, 0);
            root.Controls.Add(BuildCaptureButton(), 0, 1);

            Panel spacer = new Panel();
            spacer.Dock = DockStyle.Fill;
            spacer.BackColor = Theme.Background;
            root.Controls.Add(spacer, 0, 2);

            root.Controls.Add(BuildResultHeader(), 0, 3);
            root.Controls.Add(BuildResultCard(), 0, 4);
            root.Controls.Add(BuildActionBar(), 0, 5);
            root.Controls.Add(BuildStatusBar(), 0, 6);
        }

        private Control BuildHeader()
        {
            Panel header = new Panel();
            header.Dock = DockStyle.Fill;
            header.BackColor = Theme.Background;

            Label title = new Label();
            title.Text = "屏幕识字";
            title.Font = Theme.TitleFont;
            title.ForeColor = Theme.Text;
            title.AutoSize = true;
            title.Location = new Point(0, 0);
            title.BackColor = Color.Transparent;
            header.Controls.Add(title);

            Label subtitle = new Label();
            subtitle.Text = "框选屏幕上的文字，自动识别并复制。所有内容只在本机处理。";
            subtitle.Font = Theme.SubtitleFont;
            subtitle.ForeColor = Theme.Muted;
            subtitle.AutoSize = true;
            subtitle.Location = new Point(2, 44);
            subtitle.BackColor = Color.Transparent;
            header.Controls.Add(subtitle);

            Label badge = new Label();
            badge.Text = "●  本地离线 · 不上传";
            badge.Font = Theme.BadgeFont;
            badge.ForeColor = Theme.Success;
            badge.AutoSize = true;
            badge.BackColor = Color.Transparent;
            badge.Anchor = AnchorStyles.Top | AnchorStyles.Right;
            badge.Location = new Point(header.ClientSize.Width - badge.PreferredWidth, 8);
            header.Controls.Add(badge);
            header.Resize += delegate
            {
                badge.Location = new Point(
                    Math.Max(0, header.ClientSize.Width - badge.PreferredWidth),
                    8);
            };

            return header;
        }

        private Control BuildCaptureButton()
        {
            _captureButton = new Button();
            _captureButton.Text = "框选屏幕并识别      " + _hotkeyText;
            _captureButton.Dock = DockStyle.Fill;
            _captureButton.Margin = new Padding(0);
            _captureButton.BackColor = Theme.Primary;
            _captureButton.ForeColor = Color.White;
            _captureButton.Font = Theme.ButtonFont;
            _captureButton.FlatStyle = FlatStyle.Flat;
            _captureButton.FlatAppearance.BorderSize = 0;
            _captureButton.FlatAppearance.MouseOverBackColor = Theme.PrimaryDark;
            _captureButton.FlatAppearance.MouseDownBackColor = Theme.PrimaryDarker;
            _captureButton.UseVisualStyleBackColor = false;
            _captureButton.Cursor = Cursors.Hand;
            _captureButton.TabStop = true;
            _captureButton.AccessibleName = "框选屏幕并识别";
            _captureButton.Click += async delegate
            {
                await StartCaptureAsync();
            };
            return _captureButton;
        }

        private Control BuildResultHeader()
        {
            Panel panel = new Panel();
            panel.Dock = DockStyle.Fill;
            panel.BackColor = Theme.Background;

            Label title = new Label();
            title.Text = "识别结果";
            title.Font = Theme.SectionFont;
            title.ForeColor = Theme.Text;
            title.AutoSize = true;
            title.Location = new Point(0, 3);
            title.BackColor = Color.Transparent;
            panel.Controls.Add(title);

            _countLabel = new Label();
            _countLabel.Text = "0 个字符";
            _countLabel.Font = Theme.SmallFont;
            _countLabel.ForeColor = Theme.Muted;
            _countLabel.AutoSize = true;
            _countLabel.Anchor = AnchorStyles.Top | AnchorStyles.Right;
            _countLabel.BackColor = Color.Transparent;
            panel.Controls.Add(_countLabel);
            panel.Resize += delegate
            {
                _countLabel.Location = new Point(
                    Math.Max(0, panel.ClientSize.Width - _countLabel.PreferredWidth),
                    6);
            };

            return panel;
        }

        private Control BuildResultCard()
        {
            BorderedPanel card = new BorderedPanel();
            card.Dock = DockStyle.Fill;
            card.Margin = new Padding(0, 0, 0, 8);
            card.Padding = new Padding(15, 12, 8, 8);
            card.BackColor = Theme.Card;
            card.BorderColor = Theme.Border;

            _resultBox = new TextBox();
            _resultBox.Dock = DockStyle.Fill;
            _resultBox.Multiline = true;
            _resultBox.ScrollBars = ScrollBars.Vertical;
            _resultBox.WordWrap = true;
            _resultBox.AcceptsReturn = true;
            _resultBox.ShortcutsEnabled = true;
            _resultBox.BorderStyle = BorderStyle.None;
            _resultBox.BackColor = Theme.Card;
            _resultBox.ForeColor = Theme.Text;
            _resultBox.Font = Theme.ResultFont;
            _resultBox.HideSelection = false;
            _resultBox.TextChanged += delegate
            {
                UpdateResultHint();
                UpdateUiState();
            };
            _resultBox.Enter += delegate
            {
                if (string.IsNullOrEmpty(_resultBox.Text))
                {
                    _emptyHint.Visible = false;
                }
            };
            _resultBox.Leave += delegate
            {
                UpdateResultHint();
            };
            card.Controls.Add(_resultBox);

            _emptyHint = new Label();
            _emptyHint.Dock = DockStyle.Fill;
            _emptyHint.Text = "识别结果会显示在这里\r\n\r\n点击上方按钮，或按 " + _hotkeyText + " 开始框选";
            _emptyHint.TextAlign = ContentAlignment.MiddleCenter;
            _emptyHint.Font = Theme.BodyFont;
            _emptyHint.ForeColor = Color.FromArgb(148, 163, 184);
            _emptyHint.BackColor = Theme.Card;
            _emptyHint.Cursor = Cursors.Hand;
            _emptyHint.Click += async delegate
            {
                await StartCaptureAsync();
            };
            card.Controls.Add(_emptyHint);
            _emptyHint.BringToFront();

            return card;
        }

        private Control BuildActionBar()
        {
            Panel bar = new Panel();
            bar.Dock = DockStyle.Fill;
            bar.BackColor = Theme.Background;

            _autoCopy = new CheckBox();
            _autoCopy.Text = "识别后自动复制";
            _autoCopy.Dock = DockStyle.Left;
            _autoCopy.Width = 190;
            _autoCopy.Font = Theme.BodyFont;
            _autoCopy.ForeColor = Theme.Text;
            _autoCopy.BackColor = Theme.Background;
            _autoCopy.Checked = true;
            _autoCopy.TextAlign = ContentAlignment.MiddleLeft;
            _autoCopy.Cursor = Cursors.Hand;
            bar.Controls.Add(_autoCopy);

            FlowLayoutPanel buttons = new FlowLayoutPanel();
            buttons.Dock = DockStyle.Right;
            buttons.Width = 220;
            buttons.FlowDirection = FlowDirection.RightToLeft;
            buttons.WrapContents = false;
            buttons.Padding = new Padding(0, 8, 0, 0);
            buttons.BackColor = Theme.Background;

            _copyButton = CreateSecondaryButton("复制结果", 104);
            _copyButton.Click += delegate
            {
                CopyResultToClipboard(true);
            };
            buttons.Controls.Add(_copyButton);

            _clearButton = CreateSecondaryButton("清空", 74);
            _clearButton.Click += delegate
            {
                _resultBox.Clear();
                _countLabel.Text = "0 个字符";
                SetStatus("已清空识别结果。");
                _emptyHint.Visible = true;
                _emptyHint.BringToFront();
            };
            buttons.Controls.Add(_clearButton);

            bar.Controls.Add(buttons);
            return bar;
        }

        private Control BuildStatusBar()
        {
            Panel bar = new Panel();
            bar.Dock = DockStyle.Fill;
            bar.BackColor = Theme.Background;

            _languageLabel = new Label();
            _languageLabel.Text = _ocr == null ? "OCR 不可用" : "识别语言：" + _ocr.DisplayLanguage;
            _languageLabel.Font = Theme.SmallFont;
            _languageLabel.ForeColor = Theme.Muted;
            _languageLabel.AutoSize = true;
            _languageLabel.Anchor = AnchorStyles.Top | AnchorStyles.Right;
            _languageLabel.BackColor = Theme.Background;
            bar.Controls.Add(_languageLabel);

            _statusLabel = new Label();
            _statusLabel.Text = "正在准备…";
            _statusLabel.Font = Theme.SmallFont;
            _statusLabel.ForeColor = Theme.Muted;
            _statusLabel.AutoEllipsis = true;
            _statusLabel.AutoSize = false;
            _statusLabel.Dock = DockStyle.Fill;
            _statusLabel.TextAlign = ContentAlignment.MiddleLeft;
            _statusLabel.BackColor = Theme.Background;
            bar.Controls.Add(_statusLabel);

            bar.Resize += delegate
            {
                _languageLabel.Location = new Point(
                    Math.Max(0, bar.ClientSize.Width - _languageLabel.PreferredWidth),
                    5);
                _statusLabel.Padding = new Padding(0, 0, _languageLabel.PreferredWidth + 12, 0);
            };

            return bar;
        }

        private static Button CreateSecondaryButton(string text, int width)
        {
            Button button = new Button();
            button.Text = text;
            button.Width = width;
            button.Height = 34;
            button.Margin = new Padding(6, 0, 0, 0);
            button.FlatStyle = FlatStyle.Flat;
            button.FlatAppearance.BorderSize = 1;
            button.FlatAppearance.BorderColor = Theme.Border;
            button.FlatAppearance.MouseOverBackColor = Color.FromArgb(239, 246, 255);
            button.FlatAppearance.MouseDownBackColor = Color.FromArgb(219, 234, 254);
            button.BackColor = Theme.Card;
            button.ForeColor = Theme.Text;
            button.Font = Theme.BodyFont;
            button.Cursor = Cursors.Hand;
            button.UseVisualStyleBackColor = false;
            return button;
        }

        private async void BeginCapture()
        {
            await StartCaptureAsync();
        }

        private async Task StartCaptureAsync()
        {
            if (_busy)
            {
                return;
            }
            if (_ocr == null)
            {
                MessageBox.Show(
                    this,
                    "文字识别暂时不可用：\r\n" + _ocrError,
                    "屏幕识字",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Warning);
                return;
            }

            _busy = true;
            UpdateUiState();

            FormWindowState previousState = WindowState;
            Bitmap screenshot = null;
            bool windowRestored = false;

            try
            {
                Screen targetScreen = Screen.FromPoint(Cursor.Position);
                SetStatus("准备截屏…");
                Hide();
                await Task.Delay(220);

                screenshot = CaptureScreen(targetScreen.Bounds);
                Rectangle selectedRectangle = Rectangle.Empty;

                using (SelectionForm picker = new SelectionForm(screenshot, targetScreen.Bounds))
                {
                    DialogResult pickerResult = picker.ShowDialog();
                    if (pickerResult != DialogResult.OK ||
                        picker.SelectedRect.Width < 8 ||
                        picker.SelectedRect.Height < 8)
                    {
                        RestoreWindow(previousState);
                        windowRestored = true;
                        SetStatus("已取消框选。");
                        return;
                    }

                    selectedRectangle = picker.SelectedRect;
                }

                RestoreWindow(previousState);
                windowRestored = true;

                _resultBox.Clear();
                _emptyHint.Text = "正在识别，请稍候…";
                _emptyHint.Visible = true;
                _emptyHint.BringToFront();
                SetStatus("正在识别文字…");

                // 稍微向外多取一圈，避免用户贴边框选时切掉第一个字。
                Rectangle recognitionRectangle = selectedRectangle;
                recognitionRectangle.Inflate(16, 16);
                recognitionRectangle.Intersect(new Rectangle(0, 0, screenshot.Width, screenshot.Height));

                using (Bitmap cropped = screenshot.Clone(recognitionRectangle, PixelFormat.Format32bppArgb))
                {
                    Stopwatch watch = Stopwatch.StartNew();
                    string text = await _ocr.RecognizeAsync(cropped);
                    watch.Stop();
                    ShowRecognitionResult(text, watch.Elapsed);
                }
            }
            catch (Exception ex)
            {
                if (!windowRestored)
                {
                    RestoreWindow(previousState);
                }
                SetStatus("识别出错，请重试。", Theme.Danger);
                MessageBox.Show(
                    this,
                    "识别过程中出现问题：\r\n\r\n" + ex.Message,
                    "屏幕识字",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Error);
            }
            finally
            {
                if (screenshot != null)
                {
                    screenshot.Dispose();
                }
                _busy = false;
                UpdateUiState();
            }
        }

        private void ShowRecognitionResult(string text, TimeSpan elapsed)
        {
            if (text == null)
            {
                text = string.Empty;
            }

            _resultBox.Text = text;
            _resultBox.SelectionStart = 0;

            int characterCount = text.Replace("\r", string.Empty).Replace("\n", string.Empty).Length;
            _countLabel.Text = characterCount.ToString(CultureInfo.InvariantCulture) + " 个字符";

            if (string.IsNullOrEmpty(text))
            {
                _emptyHint.Text = "没有识别到文字\r\n\r\n试试框选得更准确一些，或把页面放大后再识别";
                _emptyHint.Visible = true;
                _emptyHint.BringToFront();
                SetStatus("没有识别到文字，请调整框选范围后重试。", Theme.Danger);
                return;
            }

            _emptyHint.Visible = false;
            bool copied = false;
            if (_autoCopy.Checked)
            {
                copied = CopyResultToClipboard(false);
            }

            string status = "识别完成 · 用时 " +
                            elapsed.TotalSeconds.ToString("0.0", CultureInfo.InvariantCulture) +
                            " 秒";
            if (copied)
            {
                status += " · 已复制到剪贴板";
            }
            SetStatus(status, Theme.Success);
            Activate();
            _resultBox.Focus();
        }

        private bool CopyResultToClipboard(bool updateStatus)
        {
            string text = _resultBox.Text;
            if (string.IsNullOrEmpty(text))
            {
                if (updateStatus)
                {
                    SetStatus("还没有可复制的内容。", Theme.Danger);
                }
                return false;
            }

            for (int attempt = 0; attempt < 3; attempt++)
            {
                try
                {
                    Clipboard.SetText(text);
                    if (updateStatus)
                    {
                        SetStatus("已复制 " + text.Length + " 个字符到剪贴板。", Theme.Success);
                    }
                    return true;
                }
                catch (ExternalException)
                {
                    if (attempt < 2)
                    {
                        System.Threading.Thread.Sleep(60);
                    }
                }
            }

            if (updateStatus)
            {
                SetStatus("复制失败，剪贴板可能正被其他程序占用。", Theme.Danger);
            }
            return false;
        }

        private void UpdateResultHint()
        {
            if (string.IsNullOrEmpty(_resultBox.Text))
            {
                if (_busy)
                {
                    _emptyHint.Text = "正在识别，请稍候…";
                }
                else
                {
                    _emptyHint.Text = "识别结果会显示在这里\r\n\r\n点击上方按钮，或按 " + _hotkeyText + " 开始框选";
                }
                _emptyHint.Visible = !_resultBox.Focused;
                if (_emptyHint.Visible)
                {
                    _emptyHint.BringToFront();
                }
            }
            else
            {
                _emptyHint.Visible = false;
            }
        }

        private void UpdateUiState()
        {
            bool hasResult = _resultBox != null && !string.IsNullOrEmpty(_resultBox.Text);
            if (_captureButton != null)
            {
                _captureButton.Enabled = !_busy && _ocr != null;
            }
            if (_copyButton != null)
            {
                _copyButton.Enabled = hasResult;
            }
            if (_clearButton != null)
            {
                _clearButton.Enabled = hasResult;
            }
            if (_autoCopy != null)
            {
                _autoCopy.Enabled = !_busy;
            }
        }

        private void SetStatus(string text)
        {
            SetStatus(text, Theme.Muted);
        }

        private void SetStatus(string text, Color color)
        {
            if (_statusLabel == null)
            {
                return;
            }
            _statusLabel.Text = text;
            _statusLabel.ForeColor = color;
        }

        private void RestoreWindow(FormWindowState previousState)
        {
            if (!Visible)
            {
                Show();
            }

            if (previousState == FormWindowState.Minimized)
            {
                WindowState = FormWindowState.Normal;
            }
            else
            {
                WindowState = previousState;
            }

            Activate();
            BringToFront();
        }

        private static Bitmap CaptureScreen(Rectangle bounds)
        {
            Bitmap screenshot = new Bitmap(bounds.Width, bounds.Height, PixelFormat.Format32bppArgb);
            using (Graphics graphics = Graphics.FromImage(screenshot))
            {
                graphics.CopyFromScreen(
                    bounds.Left,
                    bounds.Top,
                    0,
                    0,
                    bounds.Size,
                    CopyPixelOperation.SourceCopy);
            }
            return screenshot;
        }

        [DllImport("user32.dll", SetLastError = true)]
        private static extern bool RegisterHotKey(IntPtr hWnd, int id, uint fsModifiers, uint vk);

        [DllImport("user32.dll", SetLastError = true)]
        private static extern bool UnregisterHotKey(IntPtr hWnd, int id);
    }
}
