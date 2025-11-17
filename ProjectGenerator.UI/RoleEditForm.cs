using ProjectGenerator.Core.Models;
using System.Windows.Forms;

namespace ProjectGenerator.UI;

public class RoleEditForm : Form
{
    private TextBox txtName;
    private TextBox txtDescription;
    private CheckedListBox clbPermissions;
    private Button btnOK;
    private Button btnCancel;
    private SeedRole _role;

    private readonly string[] _availablePermissions = new[]
    {
        "ManageUsers",
        "ManageRoles",
        "ManageSettings",
        "ViewReports",
        "ManageProducts",
        "ManageOwnProducts",
        "ViewProducts",
        "ManageOrders",
        "ManageOwnOrders",
        "ViewOrders",
        "ViewOwnOrders",
        "PlaceOrders",
        "ManageBlog",
        "ViewBlog",
        "ManageCategories",
        "ViewCategories"
    };

    public RoleEditForm(SeedRole? role = null)
    {
        _role = role ?? new SeedRole();
        InitializeComponents();
        LoadRole();
    }

    private void InitializeComponents()
    {
        this.Text = string.IsNullOrEmpty(_role.Name) ? "افزودن نقش جدید" : "ویرایش نقش";
        this.Size = new Size(500, 500);
        this.StartPosition = FormStartPosition.CenterParent;
        this.RightToLeft = RightToLeft.Yes;
        this.RightToLeftLayout = true;
        this.FormBorderStyle = FormBorderStyle.FixedDialog;
        this.MaximizeBox = false;
        this.MinimizeBox = false;

        int yPos = 20;

        // Name
        var lblName = new Label
        {
            Text = "نام نقش:",
            Location = new Point(20, yPos),
            Size = new Size(100, 20)
        };
        this.Controls.Add(lblName);

        txtName = new TextBox
        {
            Location = new Point(130, yPos),
            Size = new Size(330, 25)
        };
        this.Controls.Add(txtName);

        yPos += 40;

        // Description
        var lblDescription = new Label
        {
            Text = "توضیحات:",
            Location = new Point(20, yPos),
            Size = new Size(100, 20)
        };
        this.Controls.Add(lblDescription);

        txtDescription = new TextBox
        {
            Location = new Point(130, yPos),
            Size = new Size(330, 25)
        };
        this.Controls.Add(txtDescription);

        yPos += 40;

        // Permissions
        var lblPermissions = new Label
        {
            Text = "مجوزها:",
            Location = new Point(20, yPos),
            Size = new Size(100, 20)
        };
        this.Controls.Add(lblPermissions);

        yPos += 30;

        clbPermissions = new CheckedListBox
        {
            Location = new Point(20, yPos),
            Size = new Size(440, 280),
            CheckOnClick = true
        };
        
        foreach (var permission in _availablePermissions)
        {
            clbPermissions.Items.Add(permission);
        }
        
        this.Controls.Add(clbPermissions);

        yPos += 300;

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

    private void LoadRole()
    {
        txtName.Text = _role.Name;
        txtDescription.Text = _role.Description;
        
        for (int i = 0; i < clbPermissions.Items.Count; i++)
        {
            var permission = clbPermissions.Items[i].ToString();
            if (permission != null && _role.Permissions.Contains(permission))
            {
                clbPermissions.SetItemChecked(i, true);
            }
        }
    }

    private void BtnOK_Click(object? sender, EventArgs e)
    {
        if (string.IsNullOrWhiteSpace(txtName.Text))
        {
            MessageBox.Show("لطفا نام نقش را وارد کنید", "خطا", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            this.DialogResult = DialogResult.None;
            return;
        }

        _role.Name = txtName.Text.Trim();
        _role.Description = txtDescription.Text.Trim();
        _role.Permissions.Clear();
        
        foreach (var item in clbPermissions.CheckedItems)
        {
            if (item != null)
            {
                _role.Permissions.Add(item.ToString() ?? "");
            }
        }
    }

    public SeedRole GetRole()
    {
        return _role;
    }
}
