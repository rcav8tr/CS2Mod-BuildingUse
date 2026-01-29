namespace BuildingUse
{
    // This mod's infoviews.
    // The "BU" (i.e. Building Use) prefix differentiates this enum from Game.UI.InGame.InfoviewsUISystem.Infoview.
    // It is okay that these infoview values (i.e. numbers) overlap with the game's infoview values.
    // The order of the infoviews here determines the order they are displayed.
    public enum BUInfoview
    {
        None,

        Employees,
        Visitors,
        Storage,
        Vehicles,

        Efficiency,
        Processing,
        Production,
    }
}
