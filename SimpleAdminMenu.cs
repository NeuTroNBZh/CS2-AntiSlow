using System.Reflection;
using CounterStrikeSharp.API.Core;
using Microsoft.Extensions.Logging;

namespace AntiSlowPlugin;

// Optional CS2-SimpleAdmin integration without a compile-time dependency (same approach as RetakeV4): every API method is
// resolved by exact signature before anything is registered, so an incompatible SimpleAdmin is skipped with a warning.
internal sealed class SimpleAdminMenu
{
    private const string ApiTypeName = "CS2_SimpleAdminApi.ICS2_SimpleAdminApi";
    private const string CategoryId = "antislow";

    private readonly object _api;
    private readonly ILogger _logger;
    private readonly IReadOnlyDictionary<string, MethodInfo> _methods;
    private readonly List<string> _menus = new();

    private static IReadOnlyDictionary<string, Type[]> Signatures { get; } = new Dictionary<string, Type[]>
    {
        ["RegisterMenuCategory"] = new[] { typeof(string), typeof(string), typeof(string) },
        ["RegisterMenu"] = new[] { typeof(string), typeof(string), typeof(string), typeof(Func<CCSPlayerController, object>), typeof(string), typeof(string) },
        ["UnregisterMenu"] = new[] { typeof(string), typeof(string) },
        ["CreateMenuWithBack"] = new[] { typeof(string), typeof(string), typeof(CCSPlayerController) },
        ["AddMenuOption"] = new[] { typeof(object), typeof(string), typeof(Action<CCSPlayerController>), typeof(bool), typeof(string) },
    };

    private SimpleAdminMenu(object api, ILogger logger, IReadOnlyDictionary<string, MethodInfo> methods)
    {
        _api = api;
        _logger = logger;
        _methods = methods;
    }

    public static SimpleAdminMenu? TryFind(ILogger logger)
    {
        foreach (var apiType in AppDomain.CurrentDomain.GetAssemblies().Select(a => a.GetType(ApiTypeName)).OfType<Type>())
        {
            object? api;
            try
            {
                var capability = apiType.GetField("PluginCapability", BindingFlags.Public | BindingFlags.Static)?.GetValue(null);
                api = capability?.GetType().GetMethod("Get", Type.EmptyTypes)?.Invoke(capability, null);
            }
            catch (TargetInvocationException ex)
            {
                logger.LogWarning(ex.InnerException ?? ex, "[AntiSlow] CS2-SimpleAdmin API found but not available");
                continue;
            }
            if (api is null)
            {
                continue;
            }
            var methods = Signatures.ToDictionary(s => s.Key, s => apiType.GetMethod(s.Key, s.Value));
            if (methods.FirstOrDefault(m => m.Value is null).Key is { } missing)
            {
                logger.LogWarning("[AntiSlow] CS2-SimpleAdmin API has no compatible {Method} method: no admin menu entry", missing);
                return null;
            }
            return new SimpleAdminMenu(api, logger, methods.ToDictionary(m => m.Key, m => m.Value!));
        }
        return null;
    }

    public void RegisterCategory(string title, string permission) =>
        _methods["RegisterMenuCategory"].Invoke(_api, new object?[] { CategoryId, title, permission });

    // Entries are rebuilt every time the admin opens them, so the player lists are always current.
    public void RegisterPlayerList(string menuId, string title, string permission,
        Func<IReadOnlyList<(string Label, Action<CCSPlayerController> Choose)>> entries, string emptyLabel)
    {
        Func<CCSPlayerController, object> factory = admin =>
        {
            try
            {
                var menu = CreateMenuWithBack(title, admin);
                var options = entries();
                if (options.Count == 0)
                {
                    AddOption(menu, emptyLabel, _ => { }, disabled: true);
                }
                foreach (var (label, choose) in options)
                {
                    AddOption(menu, label, choose, disabled: false);
                }
                return menu;
            }
            catch (Exception ex) when (ex is TargetInvocationException or InvalidOperationException or ArgumentException)
            {
                _logger.LogWarning(ex, "[AntiSlow] SimpleAdmin menu {Title} failed", title);
                return CreateMenuWithBack(title, admin);
            }
        };
        _methods["RegisterMenu"].Invoke(_api, new object?[] { CategoryId, menuId, title, factory, permission, null });
        _menus.Add(menuId);
    }

    public void Unregister()
    {
        foreach (var menuId in _menus)
        {
            try
            {
                _methods["UnregisterMenu"].Invoke(_api, new object?[] { CategoryId, menuId });
            }
            catch (TargetInvocationException ex)
            {
                _logger.LogWarning(ex.InnerException ?? ex, "[AntiSlow] Could not unregister SimpleAdmin menu {Menu}", menuId);
            }
        }
        _menus.Clear();
    }

    private void AddOption(object menu, string label, Action<CCSPlayerController> choose, bool disabled) =>
        _methods["AddMenuOption"].Invoke(_api, new object?[] { menu, label, choose, disabled, null });

    private object CreateMenuWithBack(string title, CCSPlayerController admin) =>
        _methods["CreateMenuWithBack"].Invoke(_api, new object?[] { title, CategoryId, admin })
        ?? throw new InvalidOperationException("SimpleAdmin returned no menu");
}
