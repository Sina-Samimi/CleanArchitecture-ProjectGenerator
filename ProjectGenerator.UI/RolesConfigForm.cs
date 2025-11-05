using ProjectGenerator.Models;
using System.Windows.Forms;

namespace ProjectGenerator.UI;

public class RolesConfigForm : Form
{
    private DataGridView dgvRoles;
    private Button btnAdd;
    private Button btnRemove;
    private Button btnOK;
    private Button btnCancel;
    private List<SeedRole> _roles;

    public RolesConfigForm(List<SeedRole> roles)
    {
        _roles = new List<SeedRole>(roles);
        
        // Add default roles if empty
        if (_roles.Count == 0)
        {
            _roles.Add(new SeedRole 
            { 
                Name = "Admin", 
                Description = "مدیر سیستم با دسترسی کامل",
                Permissions = new List<string> { "ManageUsers", "ManageRoles", "ManageSettings", "ViewReports", "ManageProducts", "ManageOrders" }
            });
            _roles.Add(new SeedRole 
            { 
                Name = "Seller", 
                Description = "فروشنده با دسترسی به مدیریت محصولات و سفارشات",
                Permissions = new List<string> { "ManageOwnProducts", "ViewOrders", "ManageOwnOrders" }
            });
            _roles.Add(new SeedRole 
            { 
                Name = "User", 
                Description = "کاربر عادی",
                Permissions = new List<string> { "ViewProducts", "PlaceOrders", "ViewOwnOrders" }
            });
        }
        
        InitializeComponents();
        LoadRoles();
    }

    private void InitializeComponents()
    {
        this.Text = "تنظیم نقش‌ها";
        this.Size = new Size(800, 500);
        this.StartPosition = FormStartPosition.CenterParent;
        this.RightToLeft = RightToLeft.Yes;
        this.RightToLeftLayout = true;
        this.FormBorderStyle = FormBorderStyle.FixedDialog;
        this.MaximizeBox = false;
        this.MinimizeBox = false;

        // DataGridView
        dgvRoles = new DataGridView
        {
            Location = new Point(20, 20),
            Size = new Size(740, 350),
            AutoGenerateColumns = false,
            AllowUserToAddRows = false,
            SelectionMode = DataGridViewSelectionMode.FullRowSelect,
            MultiSelect = false
        };

        dgvRoles.Columns.Add(new DataGridViewTextBoxColumn
        {
            HeaderText = "نام نقش",
            DataPropertyName = "Name",
            Width = 150
        });

        dgvRoles.Columns.Add(new DataGridViewTextBoxColumn
        {
            HeaderText = "توضیحات",
            DataPropertyName = "Description",
            Width = 300
        });

        dgvRoles.Columns.Add(new DataGridViewTextBoxColumn
        {
            HeaderText = "مجوزها",
            Name = "PermissionsDisplay",
            Width = 250,
            ReadOnly = true
        });

        this.Controls.Add(dgvRoles);

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
            Location = new Point(560, 390),
            Size = new Size(100, 35),
            DialogResult = DialogResult.OK
        };
        btnOK.Click += BtnOK_Click;
        this.Controls.Add(btnOK);

        btnCancel = new Button
        {
            Text = "انصراف",
            Location = new Point(660, 390),
            Size = new Size(100, 35),
            DialogResult = DialogResult.Cancel
        };
        this.Controls.Add(btnCancel);

        this.AcceptButton = btnOK;
        this.CancelButton = btnCancel;
    }

    private void LoadRoles()
    {
        dgvRoles.Rows.Clear();
        foreach (var role in _roles)
        {
            int rowIndex = dgvRoles.Rows.Add();
            var row = dgvRoles.Rows[rowIndex];
            row.Cells["Name"].Value = role.Name;
            row.Cells["Description"].Value = role.Description;
            row.Cells["PermissionsDisplay"].Value = string.Join(", ", role.Permissions);
            row.Tag = role;
        }
    }

    private void BtnAdd_Click(object? sender, EventArgs e)
    {
        using var form = new RoleEditForm();
        if (form.ShowDialog() == DialogResult.OK)
        {
            var newRole = form.GetRole();
            _roles.Add(newRole);
            LoadRoles();
        }
    }

    private void BtnRemove_Click(object? sender, EventArgs e)
    {
        if (dgvRoles.SelectedRows.Count > 0)
        {
            var role = dgvRoles.SelectedRows[0].Tag as SeedRole;
            if (role != null)
            {
                var result = MessageBox.Show(
                    $"آیا از حذف نقش '{role.Name}' اطمینان دارید؟",
                    "تایید حذف",
                    MessageBoxButtons.YesNo,
                    MessageBoxIcon.Question);

                if (result == DialogResult.Yes)
                {
                    _roles.Remove(role);
                    LoadRoles();
                }
            }
        }
    }

    private void BtnOK_Click(object? sender, EventArgs e)
    {
        if (_roles.Count == 0)
        {
            MessageBox.Show("حداقل یک نقش باید تعریف شود", "خطا", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            this.DialogResult = DialogResult.None;
        }
    }

    public List<SeedRole> GetRoles()
    {
        return _roles;
    }
}
