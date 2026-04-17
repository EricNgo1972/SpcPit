using SPC.DAL.SQLite;

namespace SPC.DAL.SQLite.PIT;

/// <summary>Key-value persistence for <see cref="SPC.BO.PIT.PitSettings"/>.</summary>
public class PitSettings() : SettingsDataAccessBase("PitSettings");
