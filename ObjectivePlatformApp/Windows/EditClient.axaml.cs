using Avalonia;
using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Markup.Xaml;
using Avalonia.Media;
using ObjectivePlatformApp.Data;
using ObjectivePlatformApp.Models;
using System.Text.RegularExpressions;

namespace ObjectivePlatformApp;

public partial class EditClient : UserControl
{
    private Clients _client;
    private bool _isNewClient;

    private readonly Regex _nameRegex = new Regex(@"^[�-ߨ�-��A-Za-z\-]+$");
    private readonly Regex _emailRegex = new Regex(@"^[^@\s]+@[^@\s]+\.[^@\s]+$");
    private readonly Regex _phoneRegex = new Regex(@"^\+?\d{10,15}$");

    public EditClient()
    {
        InitializeComponent();
        InitializeEvents();
    }

    public EditClient(Clients client, bool isNewClient = false) : this()
    {
        _client = client;
        _isNewClient = isNewClient;

        LoadClientData();
        ValidateAllFields();
    }

    private void InitializeEvents()
    {
        CancelButton.Click += (s, e) => NavigateBack();
        SaveButton.Click += SaveButton_Click;

        FirstNameTextBox.TextChanged += (s, e) => ValidateField(FirstNameTextBox, FirstNameError, true);
        LastNameTextBox.TextChanged += (s, e) => ValidateField(LastNameTextBox, LastNameError, true);
        MiddleNameTextBox.TextChanged += (s, e) => ValidateField(MiddleNameTextBox, MiddleNameError, false);
        EmailTextBox.TextChanged += (s, e) => ValidateEmail();
        PhoneTextBox.TextChanged += (s, e) => ValidatePhone();
    }

    private void LoadClientData()
    {
        FirstNameTextBox.Text = _client.FirstName;
        LastNameTextBox.Text = _client.LastName;
        MiddleNameTextBox.Text = _client.MiddleName;
        EmailTextBox.Text = _client.Email;
        PhoneTextBox.Text = _client.Phone;
    }

    private void SaveButton_Click(object? sender, RoutedEventArgs e)
    {
        if (!IsFormValid()) return;

        UpdateClientData();
        SaveClientToDatabase();
        var successWindow = new Window
        {
            Title = "�����������",
            Content = new TextBlock
            {
                Text = "������� �������� �������!",
                Margin = new Thickness(20),
                TextWrapping = TextWrapping.Wrap,
                FontSize = 16
            },
            SizeToContent = SizeToContent.WidthAndHeight,
            WindowStartupLocation = WindowStartupLocation.CenterOwner,
            MinWidth = 350
        };
        successWindow.ShowDialog(TopLevel.GetTopLevel(this) as Window);
        NavigateBack();
    }

    private void UpdateClientData()
    {
        _client.FirstName = FirstNameTextBox.Text?.Trim() ?? "";
        _client.LastName = LastNameTextBox.Text?.Trim() ?? "";
        _client.MiddleName = MiddleNameTextBox.Text?.Trim() ?? "";
        _client.Email = EmailTextBox.Text?.Trim() ?? "";
        _client.Phone = PhoneTextBox.Text?.Trim() ?? "";
    }

    private void SaveClientToDatabase()
    {
        using var db = new AppDbContext();
        if (_isNewClient)
            db.Clients.Add(_client);
        else
            db.Clients.Update(_client);
        db.SaveChanges();
    }

    private void NavigateBack()
    {
        var mainWindow = (MainWindow)TopLevel.GetTopLevel(this)!;
        mainWindow.Content = new ClientsWindow();
    }

    private void ValidateAllFields()
    {
        ValidateField(FirstNameTextBox, FirstNameError, true);
        ValidateField(LastNameTextBox, LastNameError, true);
        ValidateField(MiddleNameTextBox, MiddleNameError, false);
        ValidateEmail();
        ValidatePhone();
        UpdateSaveButtonState();
    }

    private void ValidateField(TextBox textBox, TextBlock errorBlock, bool isRequired)
    {
        var text = textBox.Text?.Trim() ?? "";

        if (isRequired && string.IsNullOrWhiteSpace(text))
        {
            errorBlock.Text = "��� ���� ����������� ��� ����������";
            return;
        }

        if (!string.IsNullOrWhiteSpace(text) && !_nameRegex.IsMatch(text))
        {
            errorBlock.Text = "����� ��������� ������ ����� � �����";
        }
        else
        {
            errorBlock.Text = "";
        }
    }

    private void ValidateEmail()
    {
        var text = EmailTextBox.Text?.Trim() ?? "";

        if (!string.IsNullOrWhiteSpace(text) && !_emailRegex.IsMatch(text))
        {
            EmailError.Text = "������� ���������� email (��������: example@mail.com)";
        }
        else
        {
            EmailError.Text = "";
        }

        ValidateContactInfo();
        UpdateSaveButtonState();
    }

    private void ValidatePhone()
    {
        var text = PhoneTextBox.Text?.Trim() ?? "";

        if (!string.IsNullOrWhiteSpace(text) && !_phoneRegex.IsMatch(text))
        {
            PhoneError.Text = "������� ���������� ������� (��������: +71234567890 ��� 1234567890)";
        }
        else
        {
            PhoneError.Text = "";
        }

        ValidateContactInfo();
        UpdateSaveButtonState();
    }

    private void ValidateContactInfo()
    {
        bool hasEmail = !string.IsNullOrWhiteSpace(EmailTextBox.Text);
        bool hasPhone = !string.IsNullOrWhiteSpace(PhoneTextBox.Text);

        if (!hasEmail && !hasPhone)
        {
            EmailError.Text = "���������� ������� email ��� �������";
            PhoneError.Text = "���������� ������� email ��� �������";
        }
        else if (EmailError.Text == "���������� ������� email ��� �������")
        {
            EmailError.Text = "";
        }
        else if (PhoneError.Text == "���������� ������� email ��� �������")
        {
            PhoneError.Text = "";
        }
    }

    private bool IsFormValid()
    {
        if (string.IsNullOrWhiteSpace(FirstNameTextBox.Text))
        {
            FirstNameError.Text = "��� ���� ����������� ��� ����������";
            return false;
        }

        if (string.IsNullOrWhiteSpace(LastNameTextBox.Text))
        {
            LastNameError.Text = "��� ���� ����������� ��� ����������";
            return false;
        }

        if (!_nameRegex.IsMatch(FirstNameTextBox.Text.Trim()))
        {
            FirstNameError.Text = "����� ��������� ������ ����� � �����";
            return false;
        }

        if (!_nameRegex.IsMatch(LastNameTextBox.Text.Trim()))
        {
            LastNameError.Text = "����� ��������� ������ ����� � �����";
            return false;
        }

        if (!string.IsNullOrWhiteSpace(MiddleNameTextBox.Text) &&
            !_nameRegex.IsMatch(MiddleNameTextBox.Text.Trim()))
        {
            MiddleNameError.Text = "����� ��������� ������ ����� � �����";
            return false;
        }

        if (!string.IsNullOrWhiteSpace(EmailTextBox.Text) &&
            !_emailRegex.IsMatch(EmailTextBox.Text.Trim()))
        {
            EmailError.Text = "������� ���������� email";
            return false;
        }

        if (!string.IsNullOrWhiteSpace(PhoneTextBox.Text) &&
            !_phoneRegex.IsMatch(PhoneTextBox.Text.Trim()))
        {
            PhoneError.Text = "������� ���������� �������";
            return false;
        }

        if (string.IsNullOrWhiteSpace(EmailTextBox.Text) &&
            string.IsNullOrWhiteSpace(PhoneTextBox.Text))
        {
            EmailError.Text = "���������� ������� email ��� �������";
            PhoneError.Text = "���������� ������� email ��� �������";
            return false;
        }

        return true;
    }

    private void UpdateSaveButtonState()
    {
        SaveButton.IsEnabled = IsFormValid();
    }
}