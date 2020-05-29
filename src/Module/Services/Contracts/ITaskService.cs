namespace Module.Services.Contracts {
   public interface ITaskService<T> : ITaskLoadService<T>, IArrangementService, IArrangementRunService<T> { }

}
