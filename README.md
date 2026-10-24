# 屏幕识字（Screen OCR）

一个 Windows 小工具：框选屏幕任意区域，用系统自带的 OCR 识别文字并自动复制。
无需安装 Python、Tesseract 或任何第三方 OCR 引擎，单个 EXE 即可运行。

## 使用

1. 双击 `屏幕识字.exe`。
2. 点击“框选屏幕并识别”，或按 `Ctrl+Alt+O`。
3. 拖动鼠标框选文字，松开后自动识别并复制。
4. `Esc` / 鼠标右键取消。

详细说明见 [使用说明.txt](docs/使用说明.txt)。

## 实现

- C# / .NET Framework 4.8 / WinForms
- `Windows.Media.Ocr`（Windows 10/11 自带 OCR）
- 截图通过 GDI+ `CopyFromScreen`
- 框选遮罩与橡皮筋选区为自绘无边框窗口
- OCR 前处理：放大、灰度、二值化（深色背景自动反色）
- 中文 OCR 结果清理：去掉汉字间空格、规整金额/编号中的标点

主要文件：

| 文件 | 作用 |
| --- | --- |
| `src/MainForm.cs` | 主窗口、快捷键、截图、结果与复制 |
| `src/SelectionForm.cs` | 全屏框选遮罩 |
| `src/OcrService.cs` | Windows OCR 调用与图片预处理 |
| `src/Theme.cs` | 颜色、字体和圆角绘制 |
| `src/Program.cs` | 入口、单实例 |
| `src/app.manifest` | Per-Monitor V2 DPI、普通权限 |

## 构建

需要 Windows 10/11 自带的 .NET Framework 4.x 编译器（`csc.exe`）。
首次构建会从 NuGet 下载 Windows SDK 引用元数据（约 4 MB），之后使用缓存。

```powershell
powershell -NoProfile -ExecutionPolicy Bypass -File .\build.ps1
```

输出：`dist\屏幕识字.exe`。

## 已知限制

- 使用 Windows 10 自带 OCR，复杂排版、艺术字、低分辨率文字的准确率有限。
- 识别语言取决于系统安装的 OCR 语言包；本工具优先使用简体中文。
- 超大面积框选会限制放大倍数，以控制识别耗时。

## 许可证

MIT License，© 2026 maxchoel。详见 [LICENSE](LICENSE)。
