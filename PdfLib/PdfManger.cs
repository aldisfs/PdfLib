using System;
using System.Collections.Generic;
using System.Linq;
using UglyToad.PdfPig;

namespace PdfLib
{
    /// <summary>
    /// PDF 파일을 읽어 텍스트를 테이블 형태로 변환하는 클래스입니다.
    /// </summary>
    public class PdfManager
    {
        /// <summary>
        /// PDF 파일에서 텍스트를 읽어 표 형식으로 변환합니다.
        /// </summary>
        /// <param name="pdfPath">PDF 파일의 경로</param>
        /// <param name="pageNum">분석할 페이지 번호 (기본값: 1)</param>
        /// <param name="rowThreshold">행을 구분하는 Y 좌표 차이 임계값 (기본값: 5)</param>
        /// <param name="cellThreshold">셀을 병합할 X 좌표 차이 임계값 (기본값: 30)</param>
        /// <returns>PDF에서 추출된 테이블 데이터 (각 행이 리스트로 구성됨)</returns>
        public static List<List<string>> ImportPdfToTable(string pdfPath, int pageNum = 1, int rowThreshold = 5, int cellThreshold = 30)
        {
            using (var document = PdfDocument.Open(pdfPath))
            {
                var words = document.GetPage(pageNum).GetWords()
                    .Select(w => new { w.Text, X = w.BoundingBox.Left, Y = w.BoundingBox.Bottom })
                    .OrderByDescending(w => w.Y)
                    .ToList();

                var tableRows = new List<List<(string text, double x)>>();
                var currentRow = new List<(string, double)>();
                double lastY = double.MaxValue;

                foreach (var word in words)
                {
                    if (Math.Abs(word.Y - lastY) > rowThreshold && currentRow.Count > 0)
                    {
                        tableRows.Add(new List<(string, double)>(currentRow));
                        currentRow.Clear();
                    }
                    currentRow.Add((word.Text, word.X));
                    lastY = word.Y;
                }

                if (currentRow.Count > 0)
                {
                    tableRows.Add(currentRow);
                }

                var structuredTable = new List<List<string>>();

                foreach (var row in tableRows)
                {
                    row.Sort((a, b) => a.x.CompareTo(b.x));

                    var mergedRow = new List<string>();
                    string currentCell = row[0].text;
                    double lastX = row[0].x;

                    for (int i = 1; i < row.Count; i++)
                    {
                        if (Math.Abs(row[i].x - lastX) < cellThreshold)
                        {
                            currentCell += " " + row[i].text;
                        }
                        else
                        {
                            mergedRow.Add(currentCell);
                            currentCell = row[i].text;
                        }
                        lastX = row[i].x;
                    }
                    mergedRow.Add(currentCell);
                    structuredTable.Add(mergedRow);
                }

                return structuredTable;
            }
        }
    }
}
