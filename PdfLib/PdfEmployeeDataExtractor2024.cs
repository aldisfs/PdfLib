using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;

namespace PdfLib
{
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
        public PdfEmployeeData ExtractPdfEmployeeData(string filePath, bool isDebug = false)
        {
            List<List<string>> firstTableData = PdfManager.ImportPdfToTable(filePath, 1);

            firstTableData = firstTableData
               .Select(row => row.Select(cell => Regex.Replace(cell, @"\s+", "")).ToList())
               .ToList();

            if (isDebug)
            {
                foreach (var row in firstTableData)
                {
                    Console.WriteLine(string.Join("|", row));
                }
            }

            string name = firstTableData
                  .FirstOrDefault(row => row.Any(cell => cell.Contains("⑥") && row.Any(c => c.Contains("⑦"))))?
                  .SkipWhile(cell => !cell.Contains("⑥"))
                  .Skip(2)
                  .FirstOrDefault() ?? "";

            string uid = firstTableData
                .FirstOrDefault(row => row.Any(cell => cell.Contains("⑥") && row.Any(c => c.Contains("⑦"))))?
                .Last()
                .Substring(0, 8)
                .Replace("-", "") ?? "";

            if (!isDebug)
            {
                if (name.Length == 0 || uid.Length == 0)
                {
                    throw new Exception("잘못된 파일입니다.");
                }

                if (name.Contains("*") || uid.Contains("*"))
                {
                    throw new Exception("이름이나 주민번호가 가려져 직원 정보를 알 수 없습니다.");
                }
            }

            int baseYear = firstTableData
                .FirstOrDefault(row => row.Any(cell => cell.Contains("근무기간")))?
                .SkipWhile(cell => !cell.Contains("근무기간"))
                .Skip(1)
                .Select(cell => int.TryParse(cell.Trim().Substring(0, 4), out int value) ? value : 0)
                .FirstOrDefault() ?? 0;

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

            decimal untaxedTotalSum = firstTableData
                .FirstOrDefault(row => row.Any(cell => cell.Contains("비과세소득")))?
                .SkipWhile(cell => !cell.Contains("비과세소득"))
                .Skip(1)
                .Select(cell => decimal.TryParse(cell.Trim(), out decimal value) ? value : 0)
                .FirstOrDefault() ?? 0;

            decimal previousTaxPaid = firstTableData
               .FirstOrDefault(row => row.Any(cell => cell.Contains("주(현)근무지")))?
               .SkipWhile(cell => !cell.Contains("주(현)근무지"))
               .Skip(1)
               .Take(3)
               .Select(cell => decimal.TryParse(cell.Trim(), out decimal value) ? value : 0)
               .Sum() ?? 0;

            int deductibleTax = firstTableData
               .FirstOrDefault(row => row.Any(cell => cell.Contains("징수세액")))?
               .SkipWhile(cell => !cell.Contains("징수세액"))
               .Skip(1)
               .Take(3)
               .Select(cell => int.TryParse(cell.Trim().Replace(",", ""), out int value) ? value : 0)
               .Sum() ?? 0;

            if (isDebug)
            {
                Console.WriteLine("------------");
                Console.WriteLine($"name: {name}");
                Console.WriteLine($"uid: {uid}");
                Console.WriteLine($"baseYear: {baseYear}");
                Console.WriteLine($"totalSum: {totalSum}");
                Console.WriteLine($"untaxedTotalSum: {untaxedTotalSum}");
                Console.WriteLine($"previousTaxPaid: {previousTaxPaid}");
                Console.WriteLine($"deductibleTax: {deductibleTax}");
            }

            List<List<string>> secondTableData = PdfManager.ImportPdfToTable(filePath, 2, 3, 25);

            secondTableData = secondTableData
               .Select(row => row.Select(cell => Regex.Replace(cell, @"\s+", "")).ToList())
               .ToList();

            if (isDebug)
            {
                foreach (var row in secondTableData)
                {
                    Console.WriteLine(string.Join("|", row));
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
                .SkipWhile(cell => cell != "대상금액")
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
                Console.WriteLine("------------");
                Console.WriteLine($"nationalPension: {nationalPension}");
                Console.WriteLine($"publicOfficialPension: {publicOfficialPension}");
                Console.WriteLine($"soldierPension: {soldierPension}");
                Console.WriteLine($"privateSchoolPension: {privateSchoolPension}");
                Console.WriteLine($"postalPension: {postalPension}");
                Console.WriteLine($"healthInsurance: {healthInsurance}");
                Console.WriteLine($"employmentInsurance: {employmentInsurance}");
                Console.WriteLine($"preCalculatedSalary: {preCalculatedSalary}");
            }

            return new PdfEmployeeData(name, uid, baseYear, preCalculatedSalary, deductibleTax);
        }
    }

}
