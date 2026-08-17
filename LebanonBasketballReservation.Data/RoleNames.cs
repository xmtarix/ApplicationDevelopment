namespace LebanonBasketballReservation.Data;

/// <summary>
/// The application's role names in one place. A typo in an [Authorize(Roles = "...")]
/// string fails open — the attribute simply never matches — so every reference should
/// come from here rather than from a literal.
/// </summary>
public static class RoleNames
{
    public const string Admin = "Admin";
    public const string StadiumManager = "StadiumManager";
    public const string Customer = "Customer";

    public static readonly string[] All = [Admin, StadiumManager, Customer];
}
