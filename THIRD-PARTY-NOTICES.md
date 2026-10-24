# 第三方组件与许可证

本程序（屏幕识字 / Screen OCR）在运行和构建时使用以下第三方组件。
各组件的版权归其各自作者所有，使用需遵守对应许可证。

## PaddleOCRSharp 6.2.0

- 项目：https://github.com/raoyutian/PaddleOCRSharp
- 许可证：Apache License 2.0
- 用途：C# 封装的 PaddleOCR 调用层。

## PaddlePaddle / Paddle Inference 3.3.0

- 项目：https://github.com/PaddlePaddle/Paddle
- 许可证：Apache License 2.0
- 用途：本机推理运行库（`paddle_inference.dll` 及其依赖）。

## PP-OCRv5 模型

- 来源：PaddlePaddle / PaddleOCR 提供的 PP-OCRv5 mobile 检测与识别模型。
- 许可证：Apache License 2.0
- 用途：中英文文字检测与识别。

## Newtonsoft.Json 13.0.3

- 项目：https://github.com/JamesNK/Newtonsoft.Json
- 许可证：MIT License
- 用途：PaddleOCRSharp 的模型配置解析依赖。

## Windows.Media.Ocr

- 来源：Microsoft Windows 10/11 系统自带 OCR API（备用引擎）。
- 用途：Paddle 运行库缺失时的兜底识别。

Apache License 2.0 全文见 https://www.apache.org/licenses/LICENSE-2.0
MIT License 全文见 https://opensource.org/licenses/MIT