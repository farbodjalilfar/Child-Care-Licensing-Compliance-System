namespace ChildCareLicensing.IntegrationTests;

/// <summary>Identifiers from the demo seed data, so tests read as scenarios rather than GUIDs.</summary>
public static class TestIdentities
{
    public const string OperatorRole = "Operator";
    public const string ReviewerRole = "Reviewer";
    public const string InspectorRole = "Inspector";

    public static readonly Guid SunshineOperatorId = Guid.Parse("0a000001-0000-4000-8000-000000000001");
    public static readonly Guid MapleGroveOperatorId = Guid.Parse("0a000002-0000-4000-8000-000000000002");

    public static readonly Guid SunshineFacilityId = Guid.Parse("22222222-2222-2222-2222-222222222222");
    public static readonly Guid SunshineApplicationId = Guid.Parse("55555555-5555-5555-5555-555555555555");
}
