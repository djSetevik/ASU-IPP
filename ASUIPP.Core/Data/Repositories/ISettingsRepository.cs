using ASUIPP.Core.Models;

namespace ASUIPP.Core.Data.Repositories
{
    public interface ISettingsRepository
    {
        string Get(string key);
        void Set(string key, string value);
        AppSettings GetAppSettings();
        void SaveAppSettings(AppSettings settings);
    }
}