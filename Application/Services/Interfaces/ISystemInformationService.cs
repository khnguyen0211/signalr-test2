using Application.Models.SystemInformation;

namespace Application.Services.Interfaces
{
    public interface ISystemInformationService
    {
        public SystemInfo GetWindowsSystemInfo();
    }
}
