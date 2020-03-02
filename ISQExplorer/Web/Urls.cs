using ISQExplorer.Misc;

namespace ISQExplorer.Web
{
    public static class Urls
    {
        public const string DeptSchedule = "https://bannerssb.unf.edu/nfpo-ssb/wksfwbs.p_dept_schd";

        public static string DeptSchedulePostData(int termNo, int deptId) =>
            $"pv_term={termNo}&pv_dept={deptId}&pv_ptrm=&pv_campus=&pv_sub=Submit";
        
        public static string DeptToProf(string pathName, string search) =>
            $"https://bannerssb.unf.edu/nfpo-ssb{pathName.HtmlEncode()}{search.HtmlEncode()}";

        public static string CoursePage(string courseCode) =>
            $"https://bannerssb.unf.edu/nfpo-ssb/wksfwbs.p_course_isq_grade?pv_course_id={courseCode.HtmlEncode()}";

        public static string ProfessorPage(string nNumber) =>
            $"https://bannerssb.unf.edu/nfpo-ssb/wksfwbs.p_instructor_isq_grade?pv_instructor={nNumber.HtmlEncode()}";
    }
}