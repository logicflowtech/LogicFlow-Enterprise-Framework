namespace LogicFlowEnterpriseFramework.Blazor.Models;

public sealed record IrpmModuleDefinition(
    string Slug,
    string Title,
    string ShortTitle,
    string Description,
    string Status);

public sealed record IrpmCompanyContext(
    string Key,
    string CompanyName,
    string RegistrationNo,
    string State,
    string Country,
    string Sector,
    string Summary);

public static class IrpmWorkspaceCatalog
{
    public static readonly IReadOnlyList<IrpmModuleDefinition> Modules =
    [
        new("company-profile", "Company Profile", "Company", "Core identity, registration, contacts, and company document workspace.", "In progress"),
        new("industrial-profile", "Industrial Profile", "Industrial", "Industry sector, business activity, products, and market positioning.", "Planned"),
        new("organization-structure", "Organization Structure", "Organization", "Entity structure, leadership arrangement, and operating ownership view.", "Planned"),
        new("financial-details", "Financial Details", "Financial", "Banking, statutory references, financial attachments, and supporting records.", "In progress"),
        new("overall-project-cost", "Overall Project Cost", "Project Cost", "Investment scope, capex composition, and project cost rollup.", "Planned"),
        new("overall-manpower", "Overall Manpower", "Manpower", "Headcount, hiring profile, local versus expatriate mix, and capability plan.", "Planned"),
        new("history-of-applications", "History of Applications", "Applications", "Application history, prior submissions, and milestone review trail.", "Planned")
    ];

    public static IrpmModuleDefinition? FindModule(string? slug)
    {
        if (string.IsNullOrWhiteSpace(slug))
        {
            return null;
        }

        return Modules.FirstOrDefault(module =>
            string.Equals(module.Slug, slug.Trim(), StringComparison.OrdinalIgnoreCase));
    }

    public static IrpmCompanyContext ResolveCompany(string? companyKey)
    {
        var normalized = string.IsNullOrWhiteSpace(companyKey)
            ? "infineon"
            : companyKey.Trim().ToLowerInvariant();

        return normalized switch
        {
            "pentamaster" => new IrpmCompanyContext(
                "pentamaster",
                "Pentamaster Technology (M) Sdn. Bhd.",
                "628943-D",
                "Penang",
                "Malaysia",
                "Automation Solutions",
                "Automation and smart manufacturing company profile used as a seeded IRPM workspace sample."),
            "inari" => new IrpmCompanyContext(
                "inari",
                "Inari Technology Sdn. Bhd.",
                "724315-V",
                "Penang",
                "Malaysia",
                "Advanced Electronics",
                "Advanced electronics and outsourced semiconductor services sample for IRPM workspace layout."),
            _ => new IrpmCompanyContext(
                "infineon",
                "Infineon Technologies (Kulim) Sdn. Bhd.",
                "679693-W",
                "Kedah",
                "Malaysia",
                "Semiconductor Manufacturing",
                "Seeded IRPM company profile focused on semiconductor manufacturing, statutory profile, and support records.")
        };
    }
}
