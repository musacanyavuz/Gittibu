using System.Collections.Generic;
using System.Linq;
using GittiBu.Models;
using Dapper.FastCrud;
using GittiBu.Common;

namespace GittiBu.Services
{
    public class SystemSettingService : BaseService
    {
        public List<SystemSetting> GetSystemSettings() => GetConnection().Find<SystemSetting>().ToList();



        public bool Update(SystemSetting setting)
        {
            return GetConnection().Update(setting);
        }

        public SystemSetting GetSetting(Enums.SystemSettingName name)
        {
            var setting = GetSystemSettings().FirstOrDefault(p => p.Name == name);
            return setting;
        }

    }
}