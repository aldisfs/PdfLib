namespace PdfLib
{
    /// <summary>
    /// 원천징수영수증 PDF에서 추출한 직원 데이터를 포함하는 클래스입니다.
    /// </summary>
    public class PdfEmployeeData
    {
        /// <summary>
        /// 직원의 이름을 나타냅니다.
        /// </summary>
        public string Name { get; }

        /// <summary>
        /// 직원의 주민등록번호 앞 7자리를 나타냅니다.
        /// </summary>
        public string Uidnum7 { get; }

        /// <summary>
        /// 기준년도를 나타냅니다.
        /// </summary>
        public int BaseYear { get; }

        /// <summary>
        /// 아직 차감징수세액이 적용 안된 계산된 급여 금액을 나타냅니다.
        /// </summary>
        public int PreCalculatedSalary { get; }

        /// <summary>
        /// 차감징수세액을 나타냅니다.
        /// </summary>
        public int DeductibleTax { get; }

        /// <summary>
        /// 직원 데이터를 초기화하는 생성자입니다.
        /// </summary>
        /// <param name="name">직원의 이름</param>
        /// <param name="uidnum7">직원의 주민등록번호 앞 7자리</param>
        /// <param name="baseYear">기준 연도</param>
        /// <param name="preCalculatedSalary"> 아직 차감징수세액이 적용 안된 계산된 급여</param>
        /// <param name="deductibleTax">차감징수세액</param>
        public PdfEmployeeData(string name, string uidnum7, int baseYear, int preCalculatedSalary, int deductibleTax)
        {
            Name = name;
            Uidnum7 = uidnum7;
            BaseYear = baseYear;
            PreCalculatedSalary = preCalculatedSalary;
            DeductibleTax = deductibleTax;
        }
    }
}
