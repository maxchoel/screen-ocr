using System;
using System.Drawing;
using System.Runtime.InteropServices;
using System.Windows.Forms;

class TestWindow
{
    [DllImport("user32.dll")]
    static extern bool SetProcessDpiAwarenessContext(IntPtr ctx);

    [STAThread]
    static void Main(string[] args)
    {
        try { SetProcessDpiAwarenessContext(new IntPtr(-4)); } catch { }
        Application.EnableVisualStyles();
        Application.SetCompatibleTextRenderingDefault(false);
        Application.Run(new F(args.Length > 0 && args[0] == "dark"));
    }

    class F : Form
    {
        public F(bool dark)
        {
            Color back = dark ? Color.FromArgb(30, 32, 36) : Color.White;
            Color fore = dark ? Color.White : Color.Black;
            Text = dark ? "OCR TEST WINDOW DARK" : "OCR TEST WINDOW";
            StartPosition = FormStartPosition.Manual;
            Bounds = new Rectangle(100, 100, 1000, 440);
            BackColor = back;
            TopMost = true;
            FormBorderStyle = FormBorderStyle.FixedSingle;
            MaximizeBox = false;

            Label h = new Label();
            h.Text = "屏幕文字识别测试";
            h.Font = new Font("Microsoft YaHei UI", 22, FontStyle.Bold);
            h.AutoSize = true;
            h.Location = new Point(35, 24);
            h.ForeColor = fore;
            Controls.Add(h);

            Label a = new Label();
            a.Text = "框选这段文字，程序应该自动识别。";
            a.Font = new Font("Microsoft YaHei UI", 18);
            a.AutoSize = true;
            a.Location = new Point(35, 95);
            a.ForeColor = fore;
            Controls.Add(a);

            Label b = new Label();
            b.Text = "订单号：AB-2026-1021";
            b.Font = new Font("Microsoft YaHei UI", 17);
            b.AutoSize = true;
            b.Location = new Point(35, 155);
            b.ForeColor = fore;
            Controls.Add(b);

            Label c = new Label();
            c.Text = "金额：¥1,280.50    日期：2026年10月21日";
            c.Font = new Font("Microsoft YaHei UI", 17);
            c.AutoSize = true;
            c.Location = new Point(35, 215);
            c.ForeColor = fore;
            Controls.Add(c);

            Label d = new Label();
            d.Text = "This is a screen OCR test.";
            d.Font = new Font("Segoe UI", 17);
            d.AutoSize = true;
            d.Location = new Point(35, 280);
            d.ForeColor = fore;
            Controls.Add(d);

            Label e = new Label();
            e.Text = "扫描清晰，复制方便。";
            e.Font = new Font("Microsoft YaHei UI", 14);
            e.AutoSize = true;
            e.Location = new Point(35, 340);
            e.ForeColor = fore;
            Controls.Add(e);
        }
    }
}
