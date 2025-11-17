using ProjectGenerator.Core.Models;
using System.Windows.Forms;

namespace ProjectGenerator.UI;

public class UserEditForm : Form
{
    private TextBox txtUsername;
    private TextBox txtEmail;
    private TextBox txtPhoneNumber;
    private TextBox txtPassword;
    private CheckedListBox clbRoles;
    private Button btnOK;
    private Button btnCancel;
    private SeedUser _user;
    private List<SeedRole> _availableRoles;

    public UserEditForm(List<SeedRole> availableRoles, SeedUser? user = null)
    {
        _user = user ?? new SeedUser();
        _availableRoles = availableRoles;
        InitializeComponents();
        LoadUser();
    }

    private void InitializeComponents()
    {
        this.Text = string.IsNullOrEmpty(_user.Username) ? "افزودن کاربر جدید" : "ویرایش کاربر";
        this.Size = new Size(500, 450);
        this.StartPosition = FormStartPosition.CenterParent;
        this.RightToLeft = RightToLeft.Yes;
        this.RightToLeftLayout = true;
        this.FormBorderStyle = FormBorderStyle.FixedDialog;
        this.MaximizeBox = false;
        this.MinimizeBox = false;

        int yPos = 20;

        // Username
        AddLabel("نام کاربری:", yPos);
        txtUsername = AddTextBox(yPos);
        yPos += 40;

        // Email
        AddLabel("ایمیل:", yPos);
        txtEmail = AddTextBox(yPos);
        yPos += 40;

        // Phone Number
        AddLabel("شماره تلفن:", yPos);
        txtPhoneNumber = AddTextBox(yPos);
        yPos += 40;

        // Password
        AddLabel("رمز عبور:", yPos);
        txtPassword = AddTextBox(yPos);
        txtPassword.UseSystemPasswordChar = false; // Show password for seed data
        yPos += 40;

        // Roles
        var lblRoles = new Label
        {
            Text = "نقش‌ها:",
            Location = new Point(20, yPos),
            Size = new Size(100, 20)
        };
        this.Controls.Add(lblRoles);

        yPos += 30;

        clbRoles = new CheckedListBox
        {
            Location = new Point(20, yPos),
            Size = new Size(440, 180),
            CheckOnClick = true
        };
        
        foreach (var role in _availableRoles)
        {
            clbRoles.Items.Add(role.Name);
        }
        
        this.Controls.Add(clbRoles);

        yPos += 200;

        // Buttons
        btnOK = new Button
        {
            Text = "تایید",
            Location = new Point(260, yPos),
            Size = new Size(100, 35),
            DialogResult = DialogResult.OK
        };
        btnOK.Click += BtnOK_Click;
        this.Controls.Add(btnOK);

        btnCancel = new Button
        {
            Text = "انصراف",
            Location = new Point(360, yPos),
            Size = new Size(100, 35),
            DialogResult = DialogResult.Cancel
        };
        this.Controls.Add(btnCancel);

        this.AcceptButton = btnOK;
        this.CancelButton = btnCancel;
    }

    private void AddLabel(string text, int yPos)
    {
        var label = new Label
        {
            Text = text,
            Location = new Point(20, yPos),
            Size = new Size(100, 20)
        };
        this.Controls.Add(label);
    }

    private TextBox AddTextBox(int yPos)
    {
        var textBox = new TextBox
        {
            Location = new Point(130, yPos),
            Size = new Size(330, 25)
        };
        this.Controls.Add(textBox);
        return textBox;
    }

    private void LoadUser()
    {
        txtUsername.Text = _user.Username;
        txtEmail.Text = _user.Email;
        txtPhoneNumber.Text = _user.PhoneNumber;
        txtPassword.Text = _user.Password;
        
        for (int i = 0; i < clbRoles.Items.Count; i++)
        {
            var role = clbRoles.Items[i].ToString();
            if (role != null && _user.Roles.Contains(role))
            {
                clbRoles.SetItemChecked(i, true);
            }
        }
    }

    private void BtnOK_Click(object? sender, EventArgs e)
    {
        if (string.IsNullOrWhiteSpace(txtUsername.Text))
        {
            MessageBox.Show("لطفا نام کاربری را وارد کنید", "خطا", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            this.DialogResult = DialogResult.None;
            return;
        }

        if (string.IsNullOrWhiteSpace(txtEmail.Text))
        {
            MessageBox.Show("لطفا ایمیل را وارد کنید", "خطا", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            this.DialogResult = DialogResult.None;
            return;
        }

        if (string.IsNullOrWhiteSpace(txtPassword.Text))
        {
            MessageBox.Show("لطفا رمز عبور را وارد کنید", "خطا", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            this.DialogResult = DialogResult.None;
            return;
        }

        if (clbRoles.CheckedItems.Count == 0)
        {
            MessageBox.Show("لطفا حداقل یک نقش انتخاب کنید", "خطا", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            this.DialogResult = DialogResult.None;
            return;
        }

        _user.Username = txtUsername.Text.Trim();
        _user.Email = txtEmail.Text.Trim();
        _user.PhoneNumber = txtPhoneNumber.Text.Trim();
        _user.Password = txtPassword.Text.Trim();
        _user.Roles.Clear();
        
        foreach (var item in clbRoles.CheckedItems)
        {
            if (item != null)
            {
                _user.Roles.Add(item.ToString() ?? "");
            }
        }
    }

    public SeedUser GetUser()
    {
        return _user;
    }
}
