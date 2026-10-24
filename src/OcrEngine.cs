using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Text;
using PaddleOCRSharp;

namespace ScreenOcrTool
{
    /// <summary>
    /// 高精度识别引擎：PaddleOCRSharp（PP-OCRv5 中文模型）。
    /// 只有程序目录里带齐 PaddleOCR 运行库和 inference 模型时才启用。
    /// </summary>
    internal sealed class PaddleOcrEngine
    {
        private readonly object _gate = new object();
        private PaddleOCREngine _engine;

        public static bool IsAvailable()
        {
            string baseDir = AppDomain.CurrentDomain.BaseDirectory;
            if (!File.Exists(Path.Combine(baseDir, "PaddleOCRSharp.dll")))
            {
                return false;
            }
            if (!File.Exists(Path.Combine(baseDir, "PaddleOCR.dll")))
            {
                return false;
            }
            string det = Path.Combine(baseDir, "inference", "PP-OCRv5_mobile_det_infer");
            string rec = Path.Combine(baseDir, "inference", "PP-OCRv5_mobile_rec_infer");
            return HasModel(det) && HasModel(rec);
        }

        private static bool HasModel(string directory)
        {
            return File.Exists(Path.Combine(directory, "inference.json"))
                || File.Exists(Path.Combine(directory, "inference.pdmodel"));
        }

        public List<string> RecognizeLines(Bitmap source)
        {
            List<string> lines = new List<string>();
            if (source == null)
            {
                return lines;
            }

            lock (_gate)
            {
                EnsureEngine();
                OCRResult result = _engine.DetectText(source);
                if (result == null)
                {
                    return lines;
                }

                List<TextBlock> blocks = result.TextBlocks;
                if (blocks == null || blocks.Count == 0)
                {
                    AddCleaned(lines, result.Text);
                    return lines;
                }

                List<Row> rows = new List<Row>();
                for (int i = 0; i < blocks.Count; i++)
                {
                    TextBlock block = blocks[i];
                    if (block == null || string.IsNullOrEmpty(block.Text) || block.Text.Trim().Length == 0)
                    {
                        continue;
                    }

                    float left, top, right, bottom;
                    if (!TryGetBounds(block, out left, out top, out right, out bottom))
                    {
                        AddCleaned(lines, block.Text);
                        continue;
                    }

                    Row row = FindRow(rows, top, bottom);
                    if (row == null)
                    {
                        row = new Row();
                        rows.Add(row);
                    }
                    row.Blocks.Add(block);
                    if (top < row.Top) row.Top = top;
                    if (bottom > row.Bottom) row.Bottom = bottom;
                }

                rows.Sort(delegate(Row a, Row b)
                {
                    return ((a.Top + a.Bottom) / 2f).CompareTo((b.Top + b.Bottom) / 2f);
                });

                for (int i = 0; i < rows.Count; i++)
                {
                    Row row = rows[i];
                    row.Blocks.Sort(delegate(TextBlock a, TextBlock b)
                    {
                        float al, at, ar, ab, bl, bt, br, bb;
                        TryGetBounds(a, out al, out at, out ar, out ab);
                        TryGetBounds(b, out bl, out bt, out br, out bb);
                        return al.CompareTo(bl);
                    });

                    StringBuilder builder = new StringBuilder();
                    for (int j = 0; j < row.Blocks.Count; j++)
                    {
                        string piece = row.Blocks[j].Text;
                        if (piece == null)
                        {
                            continue;
                        }
                        piece = piece.Trim();
                        if (piece.Length == 0)
                        {
                            continue;
                        }
                        if (builder.Length > 0)
                        {
                            builder.Append(' ');
                        }
                        builder.Append(piece);
                    }
                    AddCleaned(lines, builder.ToString());
                }
            }

            return lines;
        }

        private void EnsureEngine()
        {
            if (_engine != null)
            {
                return;
            }

            OCRModelConfig config = OCRModelConfig.V5_CN;
            OCRParameter parameter = new OCRParameter();
            parameter.use_gpu = false;
            parameter.enable_mkldnn = true;
            parameter.cpu_math_library_num_threads = Math.Max(1, Math.Min(8, Environment.ProcessorCount));
            parameter.det = true;
            parameter.rec = true;
            parameter.cls = false;
            parameter.use_angle_cls = false;
            parameter.max_side_len = 1920;
            parameter.det_db_thresh = 0.3f;
            parameter.det_db_box_thresh = 0.4f;
            parameter.det_db_unclip_ratio = 1.5f;
            parameter.rec_batch_num = 6;
            _engine = new PaddleOCREngine(config, parameter);
        }

        private static Row FindRow(List<Row> rows, float top, float bottom)
        {
            float center = (top + bottom) / 2f;
            float height = Math.Max(1f, bottom - top);
            for (int i = 0; i < rows.Count; i++)
            {
                Row row = rows[i];
                float overlap = Math.Min(row.Bottom, bottom) - Math.Max(row.Top, top);
                float rowHeight = Math.Max(1f, row.Bottom - row.Top);
                if (overlap > Math.Min(rowHeight, height) * 0.5f)
                {
                    return row;
                }
                if (Math.Abs(((row.Top + row.Bottom) / 2f) - center) < Math.Max(rowHeight, height) * 0.5f)
                {
                    return row;
                }
            }
            return null;
        }

        private static void AddCleaned(List<string> lines, string text)
        {
            if (string.IsNullOrEmpty(text))
            {
                return;
            }
            string[] parts = text.Replace("\r\n", "\n").Replace('\r', '\n').Split('\n');
            for (int i = 0; i < parts.Length; i++)
            {
                string cleaned = OcrService.CleanLine(parts[i]);
                if (!string.IsNullOrEmpty(cleaned))
                {
                    lines.Add(cleaned);
                }
            }
        }

        private static bool TryGetBounds(TextBlock block, out float left, out float top, out float right, out float bottom)
        {
            left = 0f;
            top = 0f;
            right = 0f;
            bottom = 0f;
            List<OCRPoint> points = block.BoxPoints;
            if (points == null || points.Count == 0)
            {
                return false;
            }
            left = points[0].X;
            right = points[0].X;
            top = points[0].Y;
            bottom = points[0].Y;
            for (int i = 1; i < points.Count; i++)
            {
                if (points[i].X < left) left = points[i].X;
                if (points[i].X > right) right = points[i].X;
                if (points[i].Y < top) top = points[i].Y;
                if (points[i].Y > bottom) bottom = points[i].Y;
            }
            return right > left && bottom > top;
        }

        private sealed class Row
        {
            public readonly List<TextBlock> Blocks = new List<TextBlock>();
            public float Top = float.MaxValue;
            public float Bottom = float.MinValue;
        }
    }
}