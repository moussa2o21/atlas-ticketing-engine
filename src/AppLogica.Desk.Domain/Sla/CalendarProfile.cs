namespace AppLogica.Desk.Domain.Sla;

/// <summary>
/// Predefined calendar profiles for MENA region support.
/// Each profile defines a standard set of working days.
/// </summary>
public enum CalendarProfile
{
    /// <summary>Sun–Thu working, Fri–Sat weekend (Saudi Arabia, UAE, Qatar, Kuwait, Bahrain, Oman)</summary>
    Gcc = 0,

    /// <summary>Sun–Thu working, Fri-only weekend (Egypt)</summary>
    Egypt = 1,

    /// <summary>Mon–Fri working, Sat–Sun weekend (International/Western)</summary>
    International = 2
}
