# 屏幕识字（Screen OCR）

一个 Windows 小工具：框选屏幕任意区域，用 PaddleOCR（PP-OCRv5）高精度识别中文/英文并自动复制。
程序完全离线运行，不上传截图或文字。

> **本仓库只放源码，不含编译好的 EXE 和大体积运行库/模型**（`.gitignore` 已排除）。
> 想直接运行请按下面「构建」一节准备依赖后编译；打包好的 Release 会附带完整 `dist`。

## 使用

1. 双击 `屏幕识字.exe`（首次识别需加载模型，约 1–2 秒）。
2. 点击“框选屏幕并识别”，或按 `Ctrl+Alt+O`。
3. 拖动鼠标框选文字，松开后自动识别并复制。
4. `Esc` / 鼠标右键取消。

详细说明见 [dist/使用说明.txt](dist/使用说明.txt)。

## 实现

- C# / .NET Framework 4.8 / WinForms，**x64**
- 主引擎：PaddleOCRSharp 6.2.0 + Paddle Inference 3.3.0（PP-OCRv5 mobile det/rec）
- 备用引擎：`Windows.Media.Ocr`（Paddle 运行库缺失时自动回退）
- 截图：GDI+ `CopyFromScreen`；框选遮罩为自绘无边框窗口
- 结果清理：按行合并文本框、去掉汉字间空格、规整标点

主要文件：

| 文件 | 作用 |
| --- | --- |
| `src/MainForm.cs` | 主窗口、快捷键、截图、结果与复制 |
| `src/SelectionForm.cs` | 全屏框选遮罩 |
| `src/OcrService.cs` | 识别入口：优先 PaddleOCR，失败回退 Windows OCR |
| `src/OcrEngine.cs` | PaddleOCRSharp 调用与文本框按行合并 |
| `src/Theme.cs` | 颜色、字体和圆角绘制 |
| `src/Program.cs` | 入口、单实例、工作目录修正 |
| `src/app.manifest` | Per-Monitor V2 DPI、普通权限 |

## 构建

需要：

- Windows 10/11 自带的 .NET Framework 4.x 编译器（`csc.exe`，x64）
- `vendor/paddle/` 下的 PaddleOCRSharp 6.2.0、Paddle.Runtime.win_x64 3.3.0、Newtonsoft.Json 13.0.3（获取方式见 `vendor/paddle/README.md`）
- 首次构建会从 NuGet 下载 Windows SDK 引用元数据（约 4 MB），之后使用缓存

```powershell
powershell -NoProfile -ExecutionPolicy Bypass -File .\build.ps1
```

输出：`dist\屏幕识字.exe`，连同运行库、模型和说明文档约 336 MB。

## 已知限制

- 首次识别要加载模型，约 1–2 秒；之后每次识别约 0.9–1.9 秒（视区域大小和 CPU）。
- 需要 64 位 Windows；`dist` 体积较大（主要是 Paddle Inference 运行库和 PP-OCRv5 模型）。
- 极端小字、艺术字或强反光仍可能出错；框选松一点、一次少框几行会更稳。
- 备用 Windows OCR 仅用于 Paddle 运行库缺失时的兜底，准确率明显较低。

## 第三方组件

- PaddleOCRSharp（Apache-2.0）
- PaddlePaddle / Paddle Inference（Apache-2.0）
- PP-OCRv5 模型（PaddlePaddle 提供，Apache-2.0）
- Newtonsoft.Json（MIT）

详见 [THIRD-PARTY-NOTICES.md](THIRD-PARTY-NOTICES.md)。

## 许可证

MIT License，© 2026 maxchoel。详见 [LICENSE](LICENSE)。