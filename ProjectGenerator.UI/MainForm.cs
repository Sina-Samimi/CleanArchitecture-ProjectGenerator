using ProjectGenerator.Core.Models;
using Newtonsoft.Json;
using System.Windows.Forms;
using ProjectGenerator.Core.Generators;

namespace ProjectGenerator.UI;

public partial class MainForm : Form
{
    private TextBox txtProjectName;
    private TextBox txtOutputPath;
    private TextBox txtNamespace;
    private CheckBox chkIncludeWebSite;
    private CheckBox chkIncludeTests;
    private CheckBox chkGenerateSeedData;
    
    // Feature checkboxes
    private CheckBox chkUserManagement;
    private CheckBox chkSellerPanel;
    private CheckBox chkProductCatalog;
    private CheckBox chkShoppingCart;
    private CheckBox chkInvoicing;
    private CheckBox chkBlogSystem;
    
    private Button btnBrowse;
    private Button btnConfigureRoles;
    private Button btnConfigureUsers;
    private Button btnGenerate;
    private Button btnLoadConfig;
    private Button btnSaveConfig;
    
    private GroupBox grpBasicSettings;
    private GroupBox grpFeatures;
    private GroupBox grpTheme;
    private GroupBox grpActions;
    
    private Label lblStatus;
    private ProgressBar progressBar;
    
    // Theme controls
    private TextBox txtSiteName;
    private TextBox txtPrimaryColor;
    private TextBox txtSecondaryColor;
    private TextBox txtFontFamily;
    
    private ProjectConfig _config;

    public MainForm()
    {
        _config = new ProjectConfig();
        InitializeComponents();
        SetupEventHandlers();
    }

    private void InitializeComponents()
    {
        this.Text = "تولید کننده پروژه Clean Architecture";
        this.Size = new Size(800, 900);
        this.StartPosition = FormStartPosition.CenterScreen;
        this.RightToLeft = RightToLeft.Yes;
        this.RightToLeftLayout = true;

        int yPos = 20;
        int leftMargin = 20;

        // Basic Settings Group
        grpBasicSettings = new GroupBox
        {
            Text = "تنظیمات پایه",
            Location = new Point(leftMargin, yPos),
            Size = new Size(740, 150)
        };
        this.Controls.Add(grpBasicSettings);

        // Project Name
        AddLabel(grpBasicSettings, "نام پروژه:", 20, 30);
        txtProjectName = AddTextBox(grpBasicSettings, 150, 25, 550);

        // Output Path
        AddLabel(grpBasicSettings, "مسیر خروجی:", 20, 60);
        txtOutputPath = AddTextBox(grpBasicSettings, 150, 55, 450);
        btnBrowse = new Button
        {
            Text = "...",
            Location = new Point(610, 55),
            Size = new Size(40, 25)
        };
        grpBasicSettings.Controls.Add(btnBrowse);

        // Namespace
        AddLabel(grpBasicSettings, "Namespace:", 20, 90);
        txtNamespace = AddTextBox(grpBasicSettings, 150, 85, 550);

        // Basic Options
        chkIncludeWebSite = AddCheckBox(grpBasicSettings, "شامل لایه WebSite", 150, 115, true);
        chkIncludeTests = AddCheckBox(grpBasicSettings, "شامل پروژه Test", 350, 115, true);
        chkGenerateSeedData = AddCheckBox(grpBasicSettings, "تولید داده اولیه", 550, 115, true);

        yPos += 170;

        // Features Group
        grpFeatures = new GroupBox
        {
            Text = "امکانات پروژه",
            Location = new Point(leftMargin, yPos),
            Size = new Size(740, 120)
        };
        this.Controls.Add(grpFeatures);

        chkUserManagement = AddCheckBox(grpFeatures, "مدیریت کاربران", 50, 30, true);
        chkSellerPanel = AddCheckBox(grpFeatures, "پنل فروشنده", 250, 30, true);
        chkProductCatalog = AddCheckBox(grpFeatures, "کاتالوگ محصولات", 450, 30, true);
        
        chkShoppingCart = AddCheckBox(grpFeatures, "سبد خرید", 50, 60, true);
        chkInvoicing = AddCheckBox(grpFeatures, "صدور فاکتور", 250, 60, true);
        chkBlogSystem = AddCheckBox(grpFeatures, "سیستم بلاگ", 450, 60, true);

        yPos += 140;

        // Theme Settings Group
        grpTheme = new GroupBox
        {
            Text = "تنظیمات تم و طراحی",
            Location = new Point(leftMargin, yPos),
            Size = new Size(740, 150),
            Visible = chkIncludeWebSite.Checked
        };
        this.Controls.Add(grpTheme);

        AddLabel(grpTheme, "نام سایت:", 20, 30);
        txtSiteName = AddTextBox(grpTheme, 150, 25, 550);

        AddLabel(grpTheme, "رنگ اصلی:", 20, 60);
        txtPrimaryColor = AddTextBox(grpTheme, 150, 55, 200);
        txtPrimaryColor.Text = "#007bff";

        AddLabel(grpTheme, "رنگ ثانویه:", 370, 60);
        txtSecondaryColor = AddTextBox(grpTheme, 500, 55, 200);
        txtSecondaryColor.Text = "#6c757d";

        AddLabel(grpTheme, "فونت:", 20, 90);
        txtFontFamily = AddTextBox(grpTheme, 150, 85, 550);
        txtFontFamily.Text = "Vazirmatn, Tahoma, Arial, sans-serif";

        yPos += 160;

        // Actions Group
        grpActions = new GroupBox
        {
            Text = "عملیات",
            Location = new Point(leftMargin, yPos),
            Size = new Size(740, 100)
        };
        this.Controls.Add(grpActions);

        btnConfigureRoles = new Button
        {
            Text = "تنظیم نقش‌ها",
            Location = new Point(50, 30),
            Size = new Size(150, 35)
        };
        grpActions.Controls.Add(btnConfigureRoles);

        btnConfigureUsers = new Button
        {
            Text = "تنظیم کاربران اولیه",
            Location = new Point(220, 30),
            Size = new Size(150, 35)
        };
        grpActions.Controls.Add(btnConfigureUsers);

        btnLoadConfig = new Button
        {
            Text = "بارگذاری تنظیمات",
            Location = new Point(390, 30),
            Size = new Size(150, 35)
        };
        grpActions.Controls.Add(btnLoadConfig);

        btnSaveConfig = new Button
        {
            Text = "ذخیره تنظیمات",
            Location = new Point(560, 30),
            Size = new Size(150, 35)
        };
        grpActions.Controls.Add(btnSaveConfig);

        yPos += 120;

        // Generate Button
        btnGenerate = new Button
        {
            Text = "تولید پروژه",
            Location = new Point(leftMargin, yPos),
            Size = new Size(740, 50),
            BackColor = Color.FromArgb(0, 120, 215),
            ForeColor = Color.White,
            Font = new Font("Segoe UI", 12, FontStyle.Bold)
        };
        this.Controls.Add(btnGenerate);

        yPos += 70;

        // Progress Bar
        progressBar = new ProgressBar
        {
            Location = new Point(leftMargin, yPos),
            Size = new Size(740, 25),
            Visible = false
        };
        this.Controls.Add(progressBar);

        yPos += 35;

        // Status Label
        lblStatus = new Label
        {
            Location = new Point(leftMargin, yPos),
            Size = new Size(740, 30),
            TextAlign = ContentAlignment.MiddleCenter,
            Font = new Font("Segoe UI", 10)
        };
        this.Controls.Add(lblStatus);
    }

    private Label AddLabel(Control parent, string text, int x, int y)
    {
        var label = new Label
        {
            Text = text,
            Location = new Point(x, y),
            Size = new Size(120, 20),
            TextAlign = ContentAlignment.MiddleRight
        };
        parent.Controls.Add(label);
        return label;
    }

    private TextBox AddTextBox(Control parent, int x, int y, int width)
    {
        var textBox = new TextBox
        {
            Location = new Point(x, y),
            Size = new Size(width, 25)
        };
        parent.Controls.Add(textBox);
        return textBox;
    }

    private CheckBox AddCheckBox(Control parent, string text, int x, int y, bool isChecked)
    {
        var checkBox = new CheckBox
        {
            Text = text,
            Location = new Point(x, y),
            Size = new Size(180, 20),
            Checked = isChecked
        };
        parent.Controls.Add(checkBox);
        return checkBox;
    }

    private void SetupEventHandlers()
    {
        btnBrowse.Click += BtnBrowse_Click;
        btnConfigureRoles.Click += BtnConfigureRoles_Click;
        btnConfigureUsers.Click += BtnConfigureUsers_Click;
        btnLoadConfig.Click += BtnLoadConfig_Click;
        btnSaveConfig.Click += BtnSaveConfig_Click;
        btnGenerate.Click += BtnGenerate_Click;
        
        txtProjectName.TextChanged += TxtProjectName_TextChanged;
        chkGenerateSeedData.CheckedChanged += ChkGenerateSeedData_CheckedChanged;
        chkIncludeWebSite.CheckedChanged += ChkIncludeWebSite_CheckedChanged;
    }

    private void TxtProjectName_TextChanged(object? sender, EventArgs e)
    {
        txtNamespace.Text = txtProjectName.Text;
    }

    private void ChkGenerateSeedData_CheckedChanged(object? sender, EventArgs e)
    {
        btnConfigureRoles.Enabled = chkGenerateSeedData.Checked;
        btnConfigureUsers.Enabled = chkGenerateSeedData.Checked;
    }

    private void ChkIncludeWebSite_CheckedChanged(object? sender, EventArgs e)
    {
        grpTheme.Visible = chkIncludeWebSite.Checked;
    }

    private void BtnBrowse_Click(object? sender, EventArgs e)
    {
        using var dialog = new FolderBrowserDialog();
        if (dialog.ShowDialog() == DialogResult.OK)
        {
            txtOutputPath.Text = dialog.SelectedPath;
        }
    }

    private void BtnConfigureRoles_Click(object? sender, EventArgs e)
    {
        using var rolesForm = new RolesConfigForm(_config.Options.SeedRoles);
        if (rolesForm.ShowDialog() == DialogResult.OK)
        {
            _config.Options.SeedRoles = rolesForm.GetRoles();
        }
    }

    private void BtnConfigureUsers_Click(object? sender, EventArgs e)
    {
        using var usersForm = new UsersConfigForm(_config.Options.SeedUsers, _config.Options.SeedRoles);
        if (usersForm.ShowDialog() == DialogResult.OK)
        {
            _config.Options.SeedUsers = usersForm.GetUsers();
        }
    }

    private void BtnLoadConfig_Click(object? sender, EventArgs e)
    {
        using var dialog = new OpenFileDialog
        {
            Filter = "JSON Files (*.json)|*.json|All Files (*.*)|*.*",
            Title = "بارگذاری فایل تنظیمات"
        };

        if (dialog.ShowDialog() == DialogResult.OK)
        {
            try
            {
                var json = File.ReadAllText(dialog.FileName);
                _config = JsonConvert.DeserializeObject<ProjectConfig>(json) ?? new ProjectConfig();
                LoadConfigToUI();
                lblStatus.Text = "تنظیمات با موفقیت بارگذاری شد";
                lblStatus.ForeColor = Color.Green;
            }
            catch (Exception ex)
            {
                MessageBox.Show($"خطا در بارگذاری فایل: {ex.Message}", "خطا", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }
    }

    private void BtnSaveConfig_Click(object? sender, EventArgs e)
    {
        using var dialog = new SaveFileDialog
        {
            Filter = "JSON Files (*.json)|*.json",
            Title = "ذخیره فایل تنظیمات",
            FileName = "project-config.json"
        };

        if (dialog.ShowDialog() == DialogResult.OK)
        {
            try
            {
                SaveUIToConfig();
                var json = JsonConvert.SerializeObject(_config, Formatting.Indented);
                File.WriteAllText(dialog.FileName, json);
                lblStatus.Text = "تنظیمات با موفقیت ذخیره شد";
                lblStatus.ForeColor = Color.Green;
            }
            catch (Exception ex)
            {
                MessageBox.Show($"خطا در ذخیره فایل: {ex.Message}", "خطا", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }
    }

    private async void BtnGenerate_Click(object? sender, EventArgs e)
    {
        if (!ValidateInput())
            return;

        SaveUIToConfig();

        btnGenerate.Enabled = false;
        progressBar.Visible = true;
        progressBar.Style = ProgressBarStyle.Marquee;
        lblStatus.Text = "در حال تولید پروژه...";
        lblStatus.ForeColor = Color.Blue;

        try
        {
            await Task.Run(() =>
            {
                var generator = new SolutionGenerator(_config);
                generator.Generate();
            });

            progressBar.Visible = false;
            lblStatus.Text = "پروژه با موفقیت تولید شد!";
            lblStatus.ForeColor = Color.Green;

            var result = MessageBox.Show(
                $"پروژه در مسیر زیر ایجاد شد:\n{_config.OutputPath}\n\nآیا می‌خواهید پوشه را باز کنید؟",
                "موفقیت",
                MessageBoxButtons.YesNo,
                MessageBoxIcon.Information);

            if (result == DialogResult.Yes)
            {
                System.Diagnostics.Process.Start("explorer.exe", _config.OutputPath);
            }
        }
        catch (Exception ex)
        {
            progressBar.Visible = false;
            lblStatus.Text = "خطا در تولید پروژه";
            lblStatus.ForeColor = Color.Red;
            MessageBox.Show($"خطا: {ex.Message}", "خطا", MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
        finally
        {
            btnGenerate.Enabled = true;
        }
    }

    private bool ValidateInput()
    {
        if (string.IsNullOrWhiteSpace(txtProjectName.Text))
        {
            MessageBox.Show("لطفا نام پروژه را وارد کنید", "خطا", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            txtProjectName.Focus();
            return false;
        }

        if (string.IsNullOrWhiteSpace(txtOutputPath.Text))
        {
            MessageBox.Show("لطفا مسیر خروجی را انتخاب کنید", "خطا", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            btnBrowse.Focus();
            return false;
        }

        return true;
    }

    private void SaveUIToConfig()
    {
        _config.ProjectName = txtProjectName.Text.Trim();
        _config.OutputPath = Path.Combine(txtOutputPath.Text.Trim(), _config.ProjectName);
        _config.Namespace = string.IsNullOrWhiteSpace(txtNamespace.Text) 
            ? _config.ProjectName 
            : txtNamespace.Text.Trim();
        
        _config.Options.IncludeWebSite = chkIncludeWebSite.Checked;
        _config.Options.IncludeTests = chkIncludeTests.Checked;
        _config.Options.GenerateInitialSeedData = chkGenerateSeedData.Checked;
        
        _config.Options.Features.UserManagement = chkUserManagement.Checked;
        _config.Options.Features.SellerPanel = chkSellerPanel.Checked;
        _config.Options.Features.ProductCatalog = chkProductCatalog.Checked;
        _config.Options.Features.ShoppingCart = chkShoppingCart.Checked;
        _config.Options.Features.Invoicing = chkInvoicing.Checked;
        _config.Options.Features.BlogSystem = chkBlogSystem.Checked;

        // Theme settings
        if (chkIncludeWebSite.Checked)
        {
            _config.Theme.SiteName = string.IsNullOrWhiteSpace(txtSiteName.Text) 
                ? _config.Theme.SiteName 
                : txtSiteName.Text.Trim();
            _config.Theme.PrimaryColor = string.IsNullOrWhiteSpace(txtPrimaryColor.Text) 
                ? _config.Theme.PrimaryColor 
                : txtPrimaryColor.Text.Trim();
            _config.Theme.SecondaryColor = string.IsNullOrWhiteSpace(txtSecondaryColor.Text) 
                ? _config.Theme.SecondaryColor 
                : txtSecondaryColor.Text.Trim();
            _config.Theme.FontFamily = string.IsNullOrWhiteSpace(txtFontFamily.Text) 
                ? _config.Theme.FontFamily 
                : txtFontFamily.Text.Trim();
        }
    }

    private void LoadConfigToUI()
    {
        txtProjectName.Text = _config.ProjectName;
        txtOutputPath.Text = Path.GetDirectoryName(_config.OutputPath) ?? "";
        txtNamespace.Text = _config.Namespace;
        
        chkIncludeWebSite.Checked = _config.Options.IncludeWebSite;
        chkIncludeTests.Checked = _config.Options.IncludeTests;
        chkGenerateSeedData.Checked = _config.Options.GenerateInitialSeedData;
        
        chkUserManagement.Checked = _config.Options.Features.UserManagement;
        chkSellerPanel.Checked = _config.Options.Features.SellerPanel;
        chkProductCatalog.Checked = _config.Options.Features.ProductCatalog;
        chkShoppingCart.Checked = _config.Options.Features.ShoppingCart;
        chkInvoicing.Checked = _config.Options.Features.Invoicing;
        chkBlogSystem.Checked = _config.Options.Features.BlogSystem;

        // Theme settings
        txtSiteName.Text = _config.Theme.SiteName;
        txtPrimaryColor.Text = _config.Theme.PrimaryColor;
        txtSecondaryColor.Text = _config.Theme.SecondaryColor;
        txtFontFamily.Text = _config.Theme.FontFamily;
        grpTheme.Visible = chkIncludeWebSite.Checked;
    }
}
