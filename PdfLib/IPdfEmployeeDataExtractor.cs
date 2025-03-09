namespace PdfLib
{
    /// <summary>
    /// 원천징수영수증 PDF 파일에서 직원 데이터를 추출하는 기능을 제공하는 인터페이스입니다.
    /// </summary>
    public interface IPdfEmployeeDataExtractor
    {
        /// <summary>
        /// 원천징수영수증 PDF 파일에서 직원 데이터를 추출합니다.
        /// </summary>
        /// <param name="filePath">원천징수영수증 PDF 파일의 경로</param>
        /// <returns>추출된 직원 데이터를 포함하는 <see cref="PdfEmployeeData"/> 객체</returns>
        PdfEmployeeData ExtractPdfEmployeeData(string filePath);
    }
}
