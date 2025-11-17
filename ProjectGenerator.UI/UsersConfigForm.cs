using ProjectGenerator.Core.Models;
using System.Windows.Forms;

namespace ProjectGenerator.UI;

public class UsersConfigForm : Form
{
    private DataGridView dgvUsers;
    private Button btnAdd;
    private Button btnRemove;
    private Button btnOK;
    private Button btnCancel;
    private List<SeedUser> _users;
    private List<SeedRole> _roles;

    public UsersConfigForm(List<SeedUser> users, List<SeedRole> roles)
    {
        _users = new List<SeedUser>(users);
        _roles = roles;
        
        // Add default admin user if empty
        if (_users.Count == 0)
        {
            _users.Add(new SeedUser
            {
                Username = "admin",
                Email = "admin@example.com",
                PhoneNumber = "09123456789",
                Password = "Admin@123",
                Roles = new List<string> { "Admin" }
            });
        }
        
        InitializeComponents();
        LoadUsers();
    }

    private void InitializeComponents()
    {
        this.Text = "تنظیم کاربران اولیه";
        this.Size = new Size(900, 500);
        this.StartPosition = FormStartPosition.CenterParent;
        this.RightToLeft = RightToLeft.Yes;
        this.RightToLeftLayout = true;
        this.FormBorderStyle = FormBorderStyle.FixedDialog;
        this.MaximizeBox = false;
        this.MinimizeBox = false;

        // DataGridView
        dgvUsers = new DataGridView
        {
            Location = new Point(20, 20),
            Size = new Size(840, 350),
            AutoGenerateColumns = false,
            AllowUserToAddRows = false,
            SelectionMode = DataGridViewSelectionMode.FullRowSelect,
            MultiSelect = false
        };

        dgvUsers.Columns.Add(new DataGridViewTextBoxColumn
        {
            HeaderText = "نام کاربری",
            DataPropertyName = "Username",
            Width = 120
        });

        dgvUsers.Columns.Add(new DataGridViewTextBoxColumn
        {
            HeaderText = "ایمیل",
            DataPropertyName = "Email",
            Width = 200
        });

        dgvUsers.Columns.Add(new DataGridViewTextBoxColumn
        {
            HeaderText = "شماره تلفن",
            DataPropertyName = "PhoneNumber",
            Width = 120
        });

        dgvUsers.Columns.Add(new DataGridViewTextBoxColumn
        {
            HeaderText = "رمز عبور",
            DataPropertyName = "Password",
            Width = 120
        });

        dgvUsers.Columns.Add(new DataGridViewTextBoxColumn
        {
            HeaderText = "نقش‌ها",
            Name = "RolesDisplay",
            Width = 240,
            ReadOnly = true
        });

        this.Controls.Add(dgvUsers);

        // Buttons
        btnAdd = new Button
        {
            Text = "افزودن",
            Location = new Point(20, 390),
            Size = new Size(100, 35)
        };
        btnAdd.Click += BtnAdd_Click;
        this.Controls.Add(btnAdd);

        btnRemove = new Button
        {
            Text = "حذف",
            Location = new Point(130, 390),
            Size = new Size(100, 35)
        };
        btnRemove.Click += BtnRemove_Click;
        this.Controls.Add(btnRemove);

        btnOK = new Button
        {
            Text = "تایید",
            Location = new Point(660, 390),
            Size = new Size(100, 35),
            DialogResult = DialogResult.OK
        };
        btnOK.Click += BtnOK_Click;
        this.Controls.Add(btnOK);

        btnCancel = new Button
        {
            Text = "انصراف",
            Location = new Point(760, 390),
            Size = new Size(100, 35),
            DialogResult = DialogResult.Cancel
        };
        this.Controls.Add(btnCancel);

        this.AcceptButton = btnOK;
        this.CancelButton = btnCancel;
    }

    private void LoadUsers()
    {
        dgvUsers.Rows.Clear();
        foreach (var user in _users)
        {
            int rowIndex = dgvUsers.Rows.Add();
            var row = dgvUsers.Rows[rowIndex];
            row.Cells["Username"].Value = user.Username;
            row.Cells["Email"].Value = user.Email;
            row.Cells["PhoneNumber"].Value = user.PhoneNumber;
            row.Cells["Password"].Value = user.Password;
            row.Cells["RolesDisplay"].Value = string.Join(", ", user.Roles);
            row.Tag = user;
        }
    }

    private void BtnAdd_Click(object? sender, EventArgs e)
    {
        using var form = new UserEditForm(_roles);
        if (form.ShowDialog() == DialogResult.OK)
        {
            var newUser = form.GetUser();
            _users.Add(newUser);
            LoadUsers();
        }
    }

    private void BtnRemove_Click(object? sender, EventArgs e)
    {
        if (dgvUsers.SelectedRows.Count > 0)
        {
            var user = dgvUsers.SelectedRows[0].Tag as SeedUser;
            if (user != null)
            {
                var result = MessageBox.Show(
                    $"آیا از حذف کاربر '{user.Username}' اطمینان دارید؟",
                    "تایید حذف",
                    MessageBoxButtons.YesNo,
                    MessageBoxIcon.Question);

                if (result == DialogResult.Yes)
                {
                    _users.Remove(user);
                    LoadUsers();
                }
            }
        }
    }

    private void BtnOK_Click(object? sender, EventArgs e)
    {
        if (_users.Count == 0)
        {
            MessageBox.Show("حداقل یک کاربر باید تعریف شود", "خطا", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            this.DialogResult = DialogResult.None;
        }
    }

    public List<SeedUser> GetUsers()
    {
        return _users;
    }
}
