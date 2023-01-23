using System.Collections.ObjectModel;
using Serilog.Events;

namespace Umbraco.Cms.Core.Logging.Viewer;

[Obsolete("Use ILogViewerService instead. Scheduled for removal in Umbraco 15.")]
public interface ILogLevelLoader
{
    /// <summary>
    ///     Get the Serilog level values of the global minimum and the UmbracoFile one from the config file.
    /// </summary>
    [Obsolete("Use ILogViewerService.GetLogLevelsFromSinks instead. Scheduled for removal in Umbraco 15.")]
    ReadOnlyDictionary<string, LogEventLevel?> GetLogLevelsFromSinks();

    /// <summary>
    ///     Get the Serilog minimum-level value from the config file.
    /// </summary>
    [Obsolete("Use ILogViewerService.GetGlobalMinLogLevel instead. Scheduled for removal in Umbraco 15.")]
    LogEventLevel? GetGlobalMinLogLevel();
}
