using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using MesaAyudaTI.Models;
using MesaAyudaTI.Services;

namespace MesaAyudaTI.Pages;

public sealed partial class NuevoTicketPage : Page
{
    public NuevoTicketPage()
    {
        InitializeComponent();
        DepartmentBox.ItemsSource = Catalog.Departments.ToList();
        CategoryBox.ItemsSource = Catalog.CategoryChoices();
        CategoryBox.SelectedIndex = 0;

        var priorities = Catalog.PriorityChoices();
        PriorityBox.ItemsSource = priorities;
        PriorityBox.SelectedItem = priorities.First(option => option.Value == TicketPriority.Media);
    }

    private void Create_Click(object sender, RoutedEventArgs e)
    {
        var title = TitleBox.Text.Trim();
        var requester = RequesterBox.Text.Trim();
        var department = DepartmentBox.Text.Trim();
        var description = DescriptionBox.Text.Trim();

        if (title.Length < 4 || requester.Length < 2 || department.Length < 2 || description.Length < 10)
        {
            FormInfo.Severity = InfoBarSeverity.Error;
            FormInfo.Title = "Faltan datos";
            FormInfo.Message = "Indica solicitante, área, un título claro y una descripción de al menos una frase.";
            FormInfo.IsOpen = true;
            return;
        }

        if (CategoryBox.SelectedItem is not LabeledOption<TicketCategory> { Value: TicketCategory category })
            return;
        if (PriorityBox.SelectedItem is not LabeledOption<TicketPriority> { Value: TicketPriority priority })
            return;

        var ticket = TicketStore.Instance.Create(
            title,
            description,
            requester,
            department,
            AssetBox.Text,
            category,
            priority);

        ClearForm();
        TicketStore.Instance.RequestSelection(ticket.Id);
        AppNavigator.Go("tickets");
    }

    private void Clear_Click(object sender, RoutedEventArgs e) => ClearForm();

    private void ClearForm()
    {
        TitleBox.Text = "";
        RequesterBox.Text = "";
        DepartmentBox.Text = "";
        AssetBox.Text = "";
        DescriptionBox.Text = "";
        CategoryBox.SelectedIndex = 0;
        if (PriorityBox.ItemsSource is IEnumerable<LabeledOption<TicketPriority>> priorities)
            PriorityBox.SelectedItem = priorities.First(option => option.Value == TicketPriority.Media);
        FormInfo.IsOpen = false;
    }
}
