using ClosedXML.Excel;
using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Windows.Forms;
using static TrOCR.Helper.BaiduOcrHelper;  // 引用百度单元格类
using static TrOCR.Helper.TencentOcrHelper; // 引用腾讯单元格类

namespace TrOCR.Helper
{
    //导出的excel表格样式和剪贴板粘贴到excel的表格样式不同
    public static class ExcelHelper
    {
        // 重载方法，用于处理百度的 CellInfo
        public static void ExportToExcel(List<CellInfo> bodyCells, List<string> headerTexts, List<string> footerTexts)
        {
            // 将百度的 CellInfo 转换为通用的 TableCell
            var genericCells = bodyCells.Select(c => new TableCell
            {
                Text = c.Words,
                Row = c.RowStart,
                Col = c.ColStart,
                // 注意百度的 end 是不包含的边界，所以跨度是 end - start
                RowSpan = c.RowEnd - c.RowStart,
                ColSpan = c.ColEnd - c.ColStart
            }).ToList();

            GenerateExcelFile(genericCells, headerTexts, footerTexts);
        }

        // 重载方法，用于处理腾讯的 TableCell
        public static void ExportToExcel(List<TableCell> bodyCells, List<string> headerTexts, List<string> footerTexts)
        {
            GenerateExcelFile(bodyCells, headerTexts, footerTexts);
        }

        // 核心的 Excel 文件生成逻辑
        private static void GenerateExcelFile(List<TableCell> cells, List<string> headerTexts, List<string> footerTexts)
        {
            if (cells == null || !cells.Any())
            {
                MessageBox.Show("没有可供导出的表格数据。", "信息", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            using (var sfd = new SaveFileDialog())
            {
                sfd.Filter = "Excel 文件|*.xlsx";
                sfd.ValidateNames = true;
                sfd.FileName = $"TrOCR_表格_{DateTime.Now:yyyyMMdd_HHmmss}.xlsx";

                if (sfd.ShowDialog() == DialogResult.OK)
                {
                    try
                    {
                        using (var workbook = new XLWorkbook())
                        {
                            var worksheet = workbook.Worksheets.Add("识别结果");
                            int currentRow = 1;
                            int columnCount = cells.Max(c => c.Col + c.ColSpan);

                            // --- 写入表头 ---
                            if (headerTexts != null && headerTexts.Any())
                            {
                                foreach (var header in headerTexts)
                                {
                                    var cell = worksheet.Cell(currentRow, 1);
                                    cell.Value = header;
                                    worksheet.Range(currentRow, 1, currentRow, columnCount).Merge();
                                    cell.Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
                                    cell.Style.Font.Bold = true;
                                    currentRow++;
                                }
                                // 在表头和表格主体之间留一个空行
                                // currentRow++;
                            }

                            // --- ★★★ 手动写入和合并单元格 ★★★ ---
                            foreach (var cellInfo in cells)
                            {
                                // ClosedXML 的单元格坐标从 1 开始
                                int startRow = currentRow + cellInfo.Row;
                                int startCol = 1 + cellInfo.Col;

                                var cell = worksheet.Cell(startRow, startCol);
                                cell.Value = cellInfo.Text;

                                // ↓↓↓↓↓↓ 强制开启自动换行 ↓↓↓↓↓↓
                                cell.Style.Alignment.WrapText = true;
                                // ↑↑↑↑↑↑  ↑↑↑↑↑↑

                                // 如果需要合并
                                if (cellInfo.RowSpan > 1 || cellInfo.ColSpan > 1)
                                {
                                    int endRow = startRow + cellInfo.RowSpan - 1;
                                    int endCol = startCol + cellInfo.ColSpan - 1;
                                    worksheet.Range(startRow, startCol, endRow, endCol).Merge();
                                }
                            }

                            int bodyRowCount = cells.Max(c => c.Row + c.RowSpan);
                            currentRow += bodyRowCount;

                            // --- 写入表尾，使用方案一 ---
                            if (footerTexts != null && footerTexts.Any())
                            {
                                // currentRow++; // 在表格和表尾之间留一个空行
                                int footerCellCount = footerTexts.Count;
                                if (footerCellCount > 0)
                                {
                                    // 计算每部分大致应占的列数
                                    int colsPerFooter = columnCount / footerCellCount;
                                    int extraCols = columnCount % footerCellCount;
                                    int currentColumn = 1;

                                    for (int i = 0; i < footerCellCount; i++)
                                    {
                                        var cell = worksheet.Cell(currentRow, currentColumn);
                                        cell.Value = footerTexts[i];
                                        cell.Style.Font.Italic = true;

                                        // 计算当前单元格要合并的列数
                                        int currentColspan = colsPerFooter + (i < extraCols ? 1 : 0);

                                        // 确保最后一个单元格能填满剩余所有空间
                                        if (i == footerCellCount - 1)
                                        {
                                            currentColspan = columnCount - currentColumn + 1;
                                        }

                                        if (currentColspan > 1)
                                        {
                                            worksheet.Range(currentRow, currentColumn, currentRow, currentColumn + currentColspan - 1).Merge();
                                        }

                                        currentColumn += currentColspan;
                                    }
                                    // 因为只占用一行，所以最后 currentRow++ 一次即可
                                    currentRow++;
                                }
                            }

                            worksheet.Columns().AdjustToContents();
                            workbook.SaveAs(sfd.FileName);
                            MessageBox.Show($"表格已成功导出到:\n{sfd.FileName}", "导出成功", MessageBoxButtons.OK, MessageBoxIcon.Information);
                        }
                    }
                    catch (Exception ex)
                    {
                        MessageBox.Show($"导出 Excel 文件时发生错误: {ex.Message}", "错误", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    }
                }
            }
        }
        //原始实现，用于参考，此方法不支持跨行跨列的合并单元格
        public static void ExportToExcel(DataTable tableData, List<string> headerTexts, List<string> footerTexts)
        {
            if (tableData == null || tableData.Rows.Count == 0)
            {
                MessageBox.Show("没有可供导出的表格数据。", "信息", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            using (var sfd = new SaveFileDialog())
            {
                sfd.Filter = "Excel 文件|*.xlsx";
                sfd.ValidateNames = true;
                sfd.FileName = $"TrOCR_表格_{DateTime.Now:yyyyMMdd_HHmmss}.xlsx";

                if (sfd.ShowDialog() == DialogResult.OK)
                {
                    try
                    {
                        using (var workbook = new XLWorkbook())
                        {
                            var worksheet = workbook.Worksheets.Add("识别结果");
                            int currentRow = 1;
                            int columnCount = tableData.Columns.Count > 0 ? tableData.Columns.Count : 1;

                            // --- 新增：写入表头 ---
                            if (headerTexts != null && headerTexts.Count > 0)
                            {
                                foreach (var header in headerTexts)
                                {
                                    var cell = worksheet.Cell(currentRow, 1);
                                    cell.Value = header;
                                    // 合并单元格，让表头居中显示
                                    worksheet.Range(currentRow, 1, currentRow, columnCount).Merge();
                                    cell.Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
                                    cell.Style.Font.Bold = true;
                                    currentRow++;
                                }
                                // 在表头和表格主体之间留一个空行
                                // currentRow++;
                            }

                            // --- 写入表格主体数据 ---
                            if (tableData.Rows.Count > 0)
                            {
                                worksheet.Cell(currentRow, 1).InsertTable(tableData);
                                currentRow += tableData.Rows.Count + 1; // +1 是因为InsertTable会自带一个表头行
                            }

                            // --- 方案零：写入表尾 ---
                            // if (footerTexts != null && footerTexts.Count > 0)
                            // {
                            //     // 在表格主体和表尾之间留一个空行
                            //     // currentRow++;
                            //     foreach (var footer in footerTexts)
                            //     {
                            //         var cell = worksheet.Cell(currentRow, 1);
                            //         cell.Value = footer;
                            //         worksheet.Range(currentRow, 1, currentRow, columnCount).Merge();
                            //         cell.Style.Font.Italic = true;
                            //         currentRow++;
                            //     }
                            // }
                            // 方案一：--- ★★★★★ 优化后的写入表尾逻辑 ★★★★★ ---
                            if (footerTexts != null && footerTexts.Any())
                            {
                                // currentRow++; // 在表格和表尾之间留一个空行

                                int footerCellCount = footerTexts.Count;
                                if (footerCellCount > 0)
                                {
                                    // 计算每部分大致应占的列数
                                    int colsPerFooter = columnCount / footerCellCount;
                                    int extraCols = columnCount % footerCellCount;
                                    int currentColumn = 1;

                                    for (int i = 0; i < footerCellCount; i++)
                                    {
                                        var cell = worksheet.Cell(currentRow, currentColumn);
                                        cell.Value = footerTexts[i];
                                        cell.Style.Font.Italic = true;

                                        // 计算当前单元格要合并的列数
                                        int currentColspan = colsPerFooter + (i < extraCols ? 1 : 0);

                                        // 确保最后一个单元格能填满剩余所有空间
                                        if (i == footerCellCount - 1)
                                        {
                                            currentColspan = columnCount - currentColumn + 1;
                                        }

                                        if (currentColspan > 1)
                                        {
                                            worksheet.Range(currentRow, currentColumn, currentRow, currentColumn + currentColspan - 1).Merge();
                                        }

                                        currentColumn += currentColspan;
                                    }
                                    // 因为只占用一行，所以最后 currentRow++ 一次即可
                                    currentRow++;
                                }
                            }

                            //方案二： --- ↓↓↓↓↓↓ 修改表尾写入逻辑 ↓↓↓↓↓↓ ---
                            // if (footerTexts != null && footerTexts.Count > 0)
                            // {
                            //     // 在表格主体和表尾之间留一个空行
                            //     currentRow++; 

                            //     // 在同一行内，从左到右依次写入所有表尾文本
                            //     for (int i = 0; i < footerTexts.Count; i++)
                            //     {
                            //         // i是从0开始的，所以列号是 i + 1
                            //         var cell = worksheet.Cell(currentRow, i + 1); 
                            //         cell.Value = footerTexts[i];
                            //         cell.Style.Font.Italic = true;
                            //     }
                            // }
                            // --- ↑↑↑↑↑↑ 修改结束 ↑↑↑↑↑↑ ---

                            // 自动调整列宽
                            worksheet.Columns().AdjustToContents();

                            workbook.SaveAs(sfd.FileName);
                            MessageBox.Show($"表格已成功导出到:\n{sfd.FileName}", "导出成功", MessageBoxButtons.OK, MessageBoxIcon.Information);
                        }
                    }
                    catch (Exception ex)
                    {
                        MessageBox.Show($"导出 Excel 文件时发生错误: {ex.Message}", "错误", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    }
                }
            }
        }
    }
}