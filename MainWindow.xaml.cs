using System.Runtime.InteropServices;
using Microsoft.UI.Windowing;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Input;
using MesaAyudaTI.Models;
using MesaAyudaTI.Pages;
using MesaAyudaTI.Services;
using Windows.Graphics;

namespace MesaAyudaTI;

public sealed partial class MainWindow : Window
{
    private bool _placed;

    public TicketStore Store => TicketStore.Instance;

    public MainWindow()
    {
        InitializeComponent();

        ExtendsContentIntoTitleBar = true;
        SetTitleBar(AppTitleBar);
        AppWindow.TitleBar.PreferredHeightOption = TitleBarHeightOption.Tall;
        AppWindow.SetIcon("Assets/AppIcon.ico");
        AppWindow.Title = "Mesa de ayuda TI";
        NavFrame.CacheSize = 8;

        Store.Changed += (_, _) => UpdateBadge();
        AppNavigator.NavigateRequested += SelectTag;
        Closed += (_, _) => AppNavigator.NavigateRequested -= SelectTag;
        Activated += OnFirstActivated;
    }

    private void OnFirstActivated(object sender, WindowActivatedEventArgs args)
    {
        if (_placed || args.WindowActivationState == WindowActivationState.Deactivated)
            return;

        _placed = true;
        Activated -= OnFirstActivated;

        var hwnd = WinRT.Interop.WindowNative.GetWindowHandle(this);
        var scale = GetDpiForWindow(hwnd) / 96.0;
        var width = (int)(1280 * scale);
        var height = (int)(840 * scale);
        var area = DisplayArea.GetFromWindowId(AppWindow.Id, DisplayAreaFallback.Nearest).WorkArea;
        var x = area.X + Math.Max(0, (area.Width - width) / 2);
        var y = area.Y + Math.Max(0, (area.Height - height) / 2);
        AppWindow.MoveAndResize(new RectInt32(x, y, width, height));

        if (AppWindow.Presenter is OverlappedPresenter presenter)
        {
            presenter.PreferredMinimumWidth = (int)(1024 * scale);
            presenter.PreferredMinimumHeight = (int)(680 * scale);
        }
    }

    private void NavView_Loaded(object sender, RoutedEventArgs e)
    {
        if (NavView.SettingsItem is NavigationViewItem settings)
            settings.Content = "Preferencias";

        if (NavFrame.Content is null)
            NavFrame.Navigate(typeof(PanelPage));

        UpdateBadge();
    }

    private void TitleBar_PaneToggleRequested(TitleBar sender, object args)
    {
        NavView.IsPaneOpen = !NavView.IsPaneOpen;
    }

    private void TitleBar_BackRequested(TitleBar sender, object args)
    {
        if (NavFrame.CanGoBack)
            NavFrame.GoBack();
    }

    private void NewShortcut_Invoked(KeyboardAccelerator sender, KeyboardAcceleratorInvokedEventArgs args)
    {
        args.Handled = true;
        SelectTag("nuevo");
    }

    private void NavView_SelectionChanged(NavigationView sender, NavigationViewSelectionChangedEventArgs args)
    {
        if (args.IsSettingsSelected)
        {
            NavigateTo(typeof(SettingsPage));
            return;
        }

        if (args.SelectedItem is NavigationViewItem item)
            NavigateTo(PageForTag(item.Tag as string));
    }

    private void SelectTag(string tag)
    {
        if (tag == "settings")
        {
            NavView.SelectedItem = NavView.SettingsItem;
            return;
        }

        var item = NavView.MenuItems.OfType<NavigationViewItem>().FirstOrDefault(i => (i.Tag as string) == tag);
        if (item is null)
            return;

        if (ReferenceEquals(NavView.SelectedItem, item))
            NavigateTo(PageForTag(tag));
        else
            NavView.SelectedItem = item;
    }

    private void NavigateTo(Type? pageType)
    {
        if (pageType is null)
            return;

        if (NavFrame.CurrentSourcePageType == pageType)
        {
            if (pageType == typeof(TicketsPage) && NavFrame.Content is TicketsPage tickets)
                tickets.FocusPendingTicket();
            return;
        }

        NavFrame.Navigate(pageType);
        NavFrame.BackStack.Clear();
        NavFrame.ForwardStack.Clear();
    }

    private static Type? PageForTag(string? tag) => tag switch
    {
        "panel" => typeof(PanelPage),
        "tickets" => typeof(TicketsPage),
        "nuevo" => typeof(NuevoTicketPage),
        "equipo" => typeof(EquipoPage),
        _ => null
    };

    private void UpdateBadge()
    {
        var open = Store.Tickets.Count(t => t.IsOpen);
        var critical = Store.Tickets.Count(t => t.IsOpen && t.Priority == TicketPriority.Critica);
        OpenTicketsBadge.Value = open;
        OpenTicketsBadge.Visibility = open == 0 ? Visibility.Collapsed : Visibility.Visible;

        var key = critical > 0 ? "CriticalValueInfoBadgeStyle" : "AttentionValueInfoBadgeStyle";
        if (Application.Current.Resources.ContainsKey(key) && Application.Current.Resources[key] is Style style)
            OpenTicketsBadge.Style = style;
    }

    [DllImport("user32.dll")]
    private static extern uint GetDpiForWindow(IntPtr hwnd);
}
