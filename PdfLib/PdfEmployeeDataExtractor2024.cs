using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text.RegularExpressions;

namespace PdfLib
{
    public enum PdfSource
    {
        None = 0,
        HOMETAX = 1,
        SEMUSARANG = 2,
        DOUZONE = 3,
        UNKNOWN = 9
    }

    /// <summary>
    /// 2024년 기준 원천징수영수증에서 직원 데이터를 추출하는 클래스입니다.
    /// </summary>
    internal class PdfEmployeeDataExtractor2024 : IPdfEmployeeDataExtractor
    {

        /// <summary>
        /// 지정된 PDF 파일에서 직원 데이터를 추출합니다.
        /// </summary>
        /// <param name="filePath">PDF 파일 경로</param>
        /// <param name="isDebug">디버깅 여부를 나타내는 플래그 (true일 경우 디버깅 정보 출력)</param>
        /// <returns>추출된 직원 데이터를 포함하는 <see cref="PdfEmployeeData"/> 객체</returns>
        /// <exception cref="Exception">
        /// 잘못된 파일이거나, 직원 정보(이름, 주민등록번호 등)가 가려지거나, 기준년도가 작년이 아닌 경우 예외가 발생합니다.
        /// </exception>
        public PdfEmployeeData ExtractPdfEmployeeData(List<List<string>> firstTableData, List<List<string>> secondTableData, PdfSource pdfSource = PdfSource.UNKNOWN, bool isDebug = false)
        {
            firstTableData = firstTableData
               .Select(row => row.Select(cell => Regex.Replace(cell, @"\s+", "")).ToList())
               .ToList();

            if (isDebug)
            {
                foreach (var row in firstTableData)
                {
                    Debug.WriteLine(string.Join("|", row));
                }
            }

            string name = "";
            string uidnum7 = "";
            int baseYear = 0;

            //var userInfoRow = firstTableData
            //    .FirstOrDefault(r => r.Any(cell => cell.Contains("⑥")) && r.Any(cell => cell.Contains("⑦")));
            var userInfoRow = firstTableData
                .FirstOrDefault(r =>
                    (r.Any(cell => cell.Contains("⑥")) || r.Any(cell => cell.Contains("(6)"))) &&
                    (r.Any(cell => cell.Contains("⑦")) || r.Any(cell => cell.Contains("(7)")))
                );


            //상황에 따라 cell이 다를 수 있으므로 row문자열을 모두 합친 후 문자열로 처리를 한다 (필수 데이터만 이런 처리를 함)
            if (userInfoRow != null)
            {
                //int startIndex = userInfoRow.FindIndex(cell => cell.Contains("⑥"));
                //int endIndex = userInfoRow.FindIndex(cell => cell.Contains("⑦"));
                int startIndex = userInfoRow.FindIndex(cell => cell.Contains("⑥") || cell.Contains("(6)"));
                int endIndex = userInfoRow.FindIndex(cell => cell.Contains("⑦") || cell.Contains("(7)"));


                //성명
                if (startIndex >= 0 && endIndex > startIndex)
                {
                    // ⑥/(6) 포함 셀부터 ⑦/(7) 포함 셀 이전까지
                    var cellsToJoin = userInfoRow.Skip(startIndex).Take(endIndex - startIndex);
                    string concated = string.Concat(cellsToJoin);
                    string[] keywords = { "⑥성명", "(6)성명" };
                    int idx = -1;
                    string keyword = "";
                    foreach (var k in keywords)
                    {
                        idx = concated.IndexOf(k);
                        if (idx >= 0)
                        {
                            keyword = k;
                            break;
                        }
                    }
                    if (idx >= 0)
                    {
                        // "성명"의 끝 위치부터 끝까지 가져오기
                        name = concated.Substring(idx + keyword.Length).Trim();
                    }
                }

                //주민번호
                if (endIndex >= 0)
                {
                    string concated = string.Concat(userInfoRow.Skip(endIndex));
                    concated = Regex.Replace(concated, @"\D", "");
                    if (concated != null && concated.Length > 13)
                    {
                        concated = concated.Substring(concated.Length - 13);
                    }
                    if (concated != null && concated.Length >= 7)
                    {
                        uidnum7 = concated.Substring(0, 7);
                    }
                }

            }

            if (!isDebug)
            {
                if (name.Length == 0 || uidnum7.Length == 0)
                {
                    throw new Exception("잘못된 파일입니다.");
                }

                if (name.Contains("*") || uidnum7.Contains("*"))
                {
                    throw new Exception("이름이나 주민번호가 가려져 직원 정보를 알 수 없습니다.");
                }
            }

            // 기준년도
            var baseYearInfoRow = firstTableData
                .FirstOrDefault(row => row.Any(cell => cell.Contains("근무기간")));

            if (baseYearInfoRow != null)
            {
                string concated = string.Concat(baseYearInfoRow);

                string keyword = "근무기간";

                int idx = concated.IndexOf(keyword);
                if (idx >= 0)
                {
                    concated = concated.Substring(idx + keyword.Length).Trim();
                    concated = Regex.Replace(concated, @"\D", "");
                    if (concated.Length >= 4)
                    {
                        string baseYearStr = concated.Substring(0, 4);
                        if (!int.TryParse(baseYearStr, out baseYear))
                        {
                            baseYear = 0;
                        }
                    }
                }
            }

            if (!isDebug)
            {
                if (baseYear == 0)
                {
                    throw new Exception("잘못된 파일입니다.");
                }

                if (baseYear != DateTime.Now.Year - 1)
                {
                    throw new Exception("작년 기준년도의 원천징수영수증이 아닙니다.");
                }
            }

            decimal totalSum = firstTableData
               .FirstOrDefault(row => row.Any(cell => (cell.Contains("16") && cell.Contains("계")) || cell == "계"))?
               .SkipWhile(cell => !(cell.Contains("16") && cell.Contains("계")) && cell != "계")
               .Skip(1)
               .Select(cell => decimal.TryParse(cell.Trim(), out decimal value) ? value : 0)
               .FirstOrDefault() ?? 0;

            //1page 상단의 내역 중 비과세소득과 같은 경우 종(전)에만 값이 있고 주(현)에는 값이 없을 수 있음
            decimal untaxedTotalSum = firstTableData
                .FirstOrDefault(row => row.Any(cell => cell.Contains("비과세소득")))?
                .SkipWhile(cell => !cell.Contains("비과세소득"))
                .Skip(1)
                .Select(cell => decimal.TryParse(cell.Trim(), out decimal value) ? value : 0)
                .FirstOrDefault() ?? 0;

            //1page 아래 내용은 항목은 컬럼으로 주/종을 가리지 않으므로 빈값은 모두 skip
            decimal previousTaxPaid = firstTableData
               .FirstOrDefault(row => row.Any(cell => cell.Contains("주(현)근무지")))?
               .SkipWhile(cell => !cell.Contains("주(현)근무지"))
               .Skip(1)
               .SkipWhile(cell => string.IsNullOrWhiteSpace(cell))
               .Take(3)
               .Select(cell => decimal.TryParse(cell.Trim(), out decimal value) ? value : 0)
               .Sum() ?? 0;

            //징수세액
            int deductibleTax = firstTableData
               .FirstOrDefault(row => row.Any(cell => cell.Contains("징수세액")))?
               .SkipWhile(cell => !cell.Contains("징수세액"))
               .Skip(1)
               .SkipWhile(cell => string.IsNullOrWhiteSpace(cell))
               .Take(3)
               .Select(cell => int.TryParse(cell.Trim().Replace(",", ""), out int value) ? value : 0)
               .Sum() ?? 0;

            if (isDebug)
            {
                Debug.WriteLine("------------");
                Debug.WriteLine($"name: {name}");
                Debug.WriteLine($"uidnum7: {uidnum7}");
                Debug.WriteLine($"baseYear: {baseYear}");
                Debug.WriteLine($"totalSum: {totalSum}");
                Debug.WriteLine($"untaxedTotalSum: {untaxedTotalSum}");
                Debug.WriteLine($"previousTaxPaid: {previousTaxPaid}");
                Debug.WriteLine($"deductibleTax: {deductibleTax}");
            }


            //두번째 페이지

            secondTableData = secondTableData
               .Select(row => row.Select(cell => Regex.Replace(cell, @"\s+", "")).ToList())
               .ToList();

            if (isDebug)
            {
                foreach (var row in secondTableData)
                {
                    Debug.WriteLine(string.Join("|", row));
                }
            }

            int nationalPensionRowIndex = secondTableData
                    .FindIndex(row => row.Any(cell => cell.Contains("국민연금보험료")));

            decimal nationalPension = nationalPensionRowIndex < 0 ? 0 : secondTableData[nationalPensionRowIndex - 1]
                .SkipWhile(cell => cell != "대상금액")
                .Skip(1)
                .Select(cell => decimal.TryParse(cell.Trim(), out decimal value) ? value : 0)
                .FirstOrDefault();

            int publicOfficialPensionIndex = secondTableData
                .FindIndex(row => row.Any(cell => cell.Contains("공무원")));

            decimal publicOfficialPension = secondTableData
              .Where((row, index) => index == publicOfficialPensionIndex || index == publicOfficialPensionIndex - 1)
              .SelectMany(row => row.SkipWhile(cell => cell != "대상금액")
              .Skip(1))
              .Select(cell => decimal.TryParse(cell.Trim(), out decimal value) ? value : (decimal?)null)
              .FirstOrDefault() ?? 0;

            int soldierPensionIndex = secondTableData
                .FindIndex(row => row.Any(cell => cell.Contains("군인연금")));

            decimal soldierPension = secondTableData
              .Where((row, index) => index == soldierPensionIndex || index == soldierPensionIndex - 1)
              .SelectMany(row => row.SkipWhile(cell => cell != "대상금액")
              .Skip(1))
              .Select(cell => decimal.TryParse(cell.Trim(), out decimal value) ? value : (decimal?)null)
              .FirstOrDefault() ?? 0;

            int privateSchoolPensionRowIndex = secondTableData
                .FindIndex(row => row.Any(cell => cell.Contains("사립학교")));

            decimal privateSchoolPension = secondTableData
               .Where((row, index) => index == privateSchoolPensionRowIndex || index == privateSchoolPensionRowIndex - 1)
               .SelectMany(row => row.SkipWhile(cell => cell != "대상금액")
               .Skip(1))
               .Select(cell => decimal.TryParse(cell.Trim(), out decimal value) ? value : (decimal?)null)
               .FirstOrDefault() ?? 0;

            int postalPensionRowIndex = secondTableData
                .FindIndex(row => row.Any(cell => cell.Contains("별정우체국")));

            decimal postalPension = postalPensionRowIndex < 0 ? 0 : secondTableData[postalPensionRowIndex - 1]
                .SkipWhile(cell => cell != "대상금액")
                .Skip(1)
                .Select(cell => decimal.TryParse(cell.Trim(), out decimal value) ? value : 0)
                .FirstOrDefault();

            int healthInsuranceIndex = secondTableData
                .FindIndex(row => row.Any(cell => cell.Contains("건강보험료")));

            decimal healthInsurance = healthInsuranceIndex < 0 ? 0 : secondTableData[healthInsuranceIndex]
                .SkipWhile(cell => !cell.Contains("대상금액"))
                .Skip(1)
                .Select(cell => decimal.TryParse(cell.Trim(), out decimal value) ? value : 0)
                .FirstOrDefault();

            int employmentInsuranceIndex = secondTableData
                .FindIndex(row => row.Any(cell => cell.Contains("고용보험료")));

            decimal employmentInsurance = employmentInsuranceIndex < 0 ? 0 : secondTableData[employmentInsuranceIndex - 1]
                .SkipWhile(cell => cell != "대상금액")
                .Skip(1)
                .Select(cell => decimal.TryParse(cell.Trim(), out decimal value) ? value : 0)
                .FirstOrDefault();

            int preCalculatedSalary = Convert.ToInt32(totalSum + untaxedTotalSum - previousTaxPaid -
                (nationalPension + publicOfficialPension + soldierPension + privateSchoolPension + postalPension + healthInsurance + employmentInsurance));

            if (isDebug)
            {
                Debug.WriteLine("------------");
                Debug.WriteLine($"nationalPension: {nationalPension}");
                Debug.WriteLine($"publicOfficialPension: {publicOfficialPension}");
                Debug.WriteLine($"soldierPension: {soldierPension}");
                Debug.WriteLine($"privateSchoolPension: {privateSchoolPension}");
                Debug.WriteLine($"postalPension: {postalPension}");
                Debug.WriteLine($"healthInsurance: {healthInsurance}");
                Debug.WriteLine($"employmentInsurance: {employmentInsurance}");
                Debug.WriteLine($"preCalculatedSalary: {preCalculatedSalary}");
            }

            return new PdfEmployeeData(name, uidnum7, baseYear, preCalculatedSalary, deductibleTax);
        }
    }

}
