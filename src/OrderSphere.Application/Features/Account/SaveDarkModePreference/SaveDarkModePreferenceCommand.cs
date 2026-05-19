using OrderSphere.Domain.Abstraction;
using OrderSphere.Domain.Primitives;

namespace OrderSphere.Application.Features.Account.SaveDarkModePreference;

public sealed record SaveDarkModePreferenceCommand(string UserId, bool PrefersDarkMode) : ICommand<Result>;
