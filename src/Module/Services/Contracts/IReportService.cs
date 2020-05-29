namespace Module.Services.Contracts {
   public interface IReportService<T> : IReportLoadService<T>, IArrangementService, IArrangementRunService<T> { }
}
