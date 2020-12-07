namespace Service.ErrorReport
{
    public interface IErrorReportGenerator
    {
        string CreateReport(string fileId);
    }
}
