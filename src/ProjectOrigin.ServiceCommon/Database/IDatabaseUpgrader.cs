using System.Threading.Tasks;

namespace ProjectOrigin.ServiceCommon.Database;

public interface IDatabaseUpgrader
{
    Task Upgrade();
    Task<bool> IsUpgradeRequired();
}
