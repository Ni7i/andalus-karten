namespace AndalusKarten.Services;

/// <summary>Dünne Hülle um die Shell-Dialoge, damit ViewModels keine Page kennen müssen.</summary>
public static class Dialogs
{
    private static Page Page => Shell.Current.CurrentPage;

    public static Task AlertAsync(string title, string message) =>
        Page.DisplayAlertAsync(title, message, "OK");

    public static Task<bool> ConfirmAsync(string title, string message, string accept) =>
        Page.DisplayAlertAsync(title, message, accept, "Abbrechen");
}
