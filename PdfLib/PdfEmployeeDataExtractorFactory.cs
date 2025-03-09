namespace PdfLib
{
    /// <summary>
    /// 직원 데이터 추출기 객체를 생성하는 팩토리 클래스입니다.
    /// </summary>
    public static class PdfEmployeeDataExtractorFactory
    {
        /// <summary>
        /// 지정된 연도에 맞는 직원 데이터 추출기 객체를 반환합니다.
        /// </summary>
        /// <param name="year">직원 데이터 추출 대상 연도</param>
        /// <returns>해당 연도의 <see cref="IPdfEmployeeDataExtractor"/> 구현 객체</returns>
        public static IPdfEmployeeDataExtractor GetExtractor(int year)
        {
            switch (year)
            {
                case 2024:
                    return new PdfEmployeeDataExtractor2024();

                default:
                    return new PdfEmployeeDataExtractor2024();
            }
        }
    }
}
