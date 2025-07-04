namespace Application.Services.Interfaces
{
    public interface IBackgroundTaskService
    {
        void RunInBackground(string taskName, Func<Task> action);
    }
}
