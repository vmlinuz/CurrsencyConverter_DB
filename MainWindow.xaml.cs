using System.Globalization;
//This library is used for Regular Expression
using System.Text.RegularExpressions;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using CurrencyConverter_Static.Data;
using Microsoft.EntityFrameworkCore;

namespace CurrencyConverter_Static
{
    public partial class MainWindow : Window
    {
        public IDbContextFactory<CurrencyConverterDbContext> DbContextFactory { get; }
        private int? _editingCurrencyId;

        public MainWindow(IDbContextFactory<CurrencyConverterDbContext> dbContextFactory)
        {
            InitializeComponent();
            DbContextFactory = dbContextFactory;
            Loaded += MainWindow_Loaded;

            //ClearControls method is used to clear all control values
            ClearControls();

        }

        private async void MainWindow_Loaded(object sender, RoutedEventArgs e)
        {
            try
            {
                await LoadCurrencyDataAsync();
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message, "Database Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private async Task LoadCurrencyDataAsync()
        {
            await using var dbContext = await DbContextFactory.CreateDbContextAsync();
            var currencies = await dbContext.CurrencyMasters
                .AsNoTracking()
                .OrderBy(currency => currency.Id)
                .ToListAsync();

            dgvCurrency.ItemsSource = currencies;

            var converterCurrencies = currencies
                .Where(currency => currency.Amount is > 0 && !string.IsNullOrWhiteSpace(currency.CurrencyName))
                .ToList();

            cmbFromCurrency.ItemsSource = converterCurrencies;
            cmbFromCurrency.DisplayMemberPath = nameof(CurrencyMaster.CurrencyName);
            cmbToCurrency.ItemsSource = converterCurrencies;
            cmbToCurrency.DisplayMemberPath = nameof(CurrencyMaster.CurrencyName);
        }

        private async void btnSave_Click(object sender, RoutedEventArgs e)
        {
            if (!TryReadCurrencyMaster(out var amount, out var currencyName))
                return;

            try
            {
                await using var dbContext = await DbContextFactory.CreateDbContextAsync();
                CurrencyMaster currency;

                if (_editingCurrencyId is int id)
                {
                    var existingCurrency = await dbContext.CurrencyMasters.FindAsync(id);
                    if (existingCurrency is null)
                    {
                        MessageBox.Show("The selected currency no longer exists.", "Database Error", MessageBoxButton.OK, MessageBoxImage.Error);
                        await LoadCurrencyDataAsync();
                        ClearMasterForm();
                        return;
                    }

                    currency = existingCurrency;
                }
                else
                {
                    currency = new CurrencyMaster();
                    dbContext.CurrencyMasters.Add(currency);
                }

                currency.Amount = amount;
                currency.CurrencyName = currencyName;

                await dbContext.SaveChangesAsync();
                await LoadCurrencyDataAsync();
                ClearMasterForm();
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message, "Database Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void btnCancel_Click(object sender, RoutedEventArgs e)
        {
            ClearMasterForm();
        }

        private async void dgvCurrency_SelectedCellsChanged(object sender, SelectedCellsChangedEventArgs e)
        {
            if (e.AddedCells.Count == 0 || e.AddedCells[0].Item is not CurrencyMaster currency)
                return;

            switch (e.AddedCells[0].Column.DisplayIndex)
            {
                case 0:
                    _editingCurrencyId = currency.Id;
                    txtAmount.Text = currency.Amount?.ToString(CultureInfo.InvariantCulture) ?? string.Empty;
                    txtCurrencyName.Text = currency.CurrencyName ?? string.Empty;
                    btnSave.Content = "Update";
                    break;
                case 1:
                    await DeleteCurrencyAsync(currency);
                    break;
            }
        }

        private bool TryReadCurrencyMaster(out double amount, out string currencyName)
        {
            currencyName = txtCurrencyName.Text.Trim();
            if (!TryParseAmount(txtAmount.Text, out amount) || amount <= 0)
            {
                MessageBox.Show("Enter a valid amount greater than zero.", "Information", MessageBoxButton.OK, MessageBoxImage.Information);
                txtAmount.Focus();
                return false;
            }

            if (string.IsNullOrWhiteSpace(currencyName))
            {
                MessageBox.Show("Please enter a currency name.", "Information", MessageBoxButton.OK, MessageBoxImage.Information);
                txtCurrencyName.Focus();
                return false;
            }

            return true;
        }

        private static bool TryParseAmount(string text, out double amount)
        {
            return double.TryParse(
                text.Trim().Replace(',', '.'),
                NumberStyles.AllowDecimalPoint,
                CultureInfo.InvariantCulture,
                out amount);
        }

        private async Task DeleteCurrencyAsync(CurrencyMaster currency)
        {
            if (MessageBox.Show(
                    $"Delete {currency.CurrencyName ?? "this currency"}?",
                    "Confirm Delete",
                    MessageBoxButton.YesNo,
                    MessageBoxImage.Question) != MessageBoxResult.Yes)
            {
                return;
            }

            try
            {
                await using var dbContext = await DbContextFactory.CreateDbContextAsync();
                var existingCurrency = await dbContext.CurrencyMasters.FindAsync(currency.Id);
                if (existingCurrency is null)
                {
                    await LoadCurrencyDataAsync();
                    return;
                }

                dbContext.CurrencyMasters.Remove(existingCurrency);
                await dbContext.SaveChangesAsync();
                await LoadCurrencyDataAsync();
                ClearMasterForm();
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message, "Database Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void ClearMasterForm()
        {
            _editingCurrencyId = null;
            txtAmount.Clear();
            txtCurrencyName.Clear();
            btnSave.Content = "Save";
            dgvCurrency.SelectedItem = null;
            dgvCurrency.UnselectAllCells();
            txtAmount.Focus();
        }

        #region Button Click Event

        //Convert the button click event
        private void Convert_Click(object sender, RoutedEventArgs e)
        {
            if (!TryParseAmount(txtCurrency.Text, out var amount))
            {
                MessageBox.Show("Please enter a valid amount.", "Information", MessageBoxButton.OK, MessageBoxImage.Information);
                txtCurrency.Focus();
                return;
            }

            if (cmbFromCurrency.SelectedItem is not CurrencyMaster fromCurrency)
            {
                MessageBox.Show("Please Select Currency From", "Information", MessageBoxButton.OK, MessageBoxImage.Information);
                cmbFromCurrency.Focus();
                return;
            }

            if (cmbToCurrency.SelectedItem is not CurrencyMaster toCurrency)
            {
                MessageBox.Show("Please Select Currency To", "Information", MessageBoxButton.OK, MessageBoxImage.Information);
                cmbToCurrency.Focus();
                return;
            }

            var fromRate = fromCurrency.Amount.GetValueOrDefault();
            var toRate = toCurrency.Amount.GetValueOrDefault();
            if (fromRate <= 0 || toRate <= 0)
            {
                MessageBox.Show("Selected currencies must have positive rates.", "Information", MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }

            var convertedAmount = amount * fromRate / toRate;
            lblCurrency.Content = $"{toCurrency.CurrencyName} {convertedAmount:N3}";
        }

        //Clear Button click event
        private void Clear_Click(object sender, RoutedEventArgs e)
        {
            //ClearControls method is used to clear all controls value
            ClearControls();
        }
        #endregion

        #region Extra Events

        //ClearControls method is used to clear all controls value
        private void ClearControls()
        {
            txtCurrency.Text = string.Empty;
            cmbFromCurrency.SelectedIndex = -1;
            cmbToCurrency.SelectedIndex = -1;
            lblCurrency.Content = "";
            txtCurrency.Focus();
        }

        private void NumberValidationTextBox(object sender, TextCompositionEventArgs e)
        {
            if (sender is not TextBox textBox)
                return;

            string proposedText = textBox.Text
                .Remove(textBox.SelectionStart, textBox.SelectionLength)
                .Insert(textBox.SelectionStart, e.Text);

            e.Handled = !MyRegex().IsMatch(proposedText);
        }

        [GeneratedRegex(@"^\d*(,\d*)?$")]
        private static partial Regex MyRegex();
        #endregion
    }
}
