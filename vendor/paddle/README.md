# PaddleOCR 依赖

`build.ps1` 会引用并复制本目录下的文件。仓库默认忽略大体积的 `native/` 和 `inference/`，
所以从全新克隆开始构建时，需要先按下面的方式准备好这些文件。

## 需要的 NuGet 包

1. **PaddleOCRSharp 6.2.0**
   - `lib/net48/PaddleOCRSharp.dll` → `vendor/paddle/PaddleOCRSharp.dll`
   - `lib/net48/PaddleOCRSharp.xml` → `vendor/paddle/PaddleOCRSharp.xml`
   - `build/ytLib/PaddleOCR.dll` → `vendor/paddle/native/PaddleOCR.dll`
   - `build/ytLib/inference/` → `vendor/paddle/inference/`
     （至少保留 `PP-OCRv5_mobile_det_infer`、`PP-OCRv5_mobile_rec_infer`、
     `ppocr_keys.txt`、`PaddleOCR.config.json`）

2. **Paddle.Runtime.win_x64 3.3.0.1**
   - `build/win_x64/*.dll` → `vendor/paddle/native/`
     （`paddle_inference.dll`、`mklml.dll`、`opencv_world470.dll`、
     `paddle_yt_phi.dll`、`paddle_yt_common.dll`、`mkldnn.dll`、
     `libiomp5md.dll`、`tbb12.dll`、`yaml-cpp.dll`、VC 运行库等）

3. **Newtonsoft.Json 13.0.3**
   - `lib/net45/Newtonsoft.Json.dll` → `vendor/paddle/Newtonsoft.Json.dll`

## 获取方式

从 [nuget.org](https://www.nuget.org/) 搜索并下载对应 `.nupkg`，
把扩展名改成 `.zip` 后解压，按上面的路径放置即可（`Expand-Archive` 不能直接解压 `.nupkg`）。

## 目录约定

```
vendor/paddle/
  PaddleOCRSharp.dll
  PaddleOCRSharp.xml
  Newtonsoft.Json.dll
  native/*.dll
  inference/
    PP-OCRv5_mobile_det_infer/
    PP-OCRv5_mobile_rec_infer/
    ppocr_keys.txt
    PaddleOCR.config.json
    ...
```

`build.ps1` 会把 `native/*.dll` 和 `inference/` 复制到 `dist/` 下。