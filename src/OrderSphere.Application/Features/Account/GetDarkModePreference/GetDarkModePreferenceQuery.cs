using OrderSphere.Domain.Abstraction;
using OrderSphere.Domain.Primitives;

namespace OrderSphere.Application.Features.Account.GetDarkModePreference;

public sealed record GetDarkModePreferenceQuery(string UserId) : IQuery<Result<bool>>;
