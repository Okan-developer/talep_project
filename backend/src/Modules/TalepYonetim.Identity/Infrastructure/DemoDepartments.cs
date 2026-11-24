using TalepYonetim.Identity.Domain;

namespace TalepYonetim.Identity.Infrastructure;

/// <summary>The six demo departments (spec 0001, R-2). Fixed ids keep the seed idempotent.</summary>
internal static class DemoDepartments
{
    public static readonly Department[] All =
    [
        new(Guid.Parse("019aa000-0000-7000-8000-000000000001"), "İnsan Kaynakları"),
        new(Guid.Parse("019aa000-0000-7000-8000-000000000002"), "Muhasebe"),
        new(Guid.Parse("019aa000-0000-7000-8000-000000000003"), "Finans"),
        new(Guid.Parse("019aa000-0000-7000-8000-000000000004"), "Operasyon"),
        new(Guid.Parse("019aa000-0000-7000-8000-000000000005"), "BT Yazılım"),
        new(Guid.Parse("019aa000-0000-7000-8000-000000000006"), "BT Sistem"),
    ];
}
