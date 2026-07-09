using Ocpp.Core.Types;

namespace OcppSimulator.App.Tabs;

/// <summary>The 43 standard configuration keys. RW values are editable in place.</summary>
public sealed class ConfigurationTab : ChargePointTab
{
    private readonly DataGridView _grid = new()
    {
        Dock = DockStyle.Fill,
        AllowUserToAddRows = false,
        AllowUserToDeleteRows = false,
        RowHeadersVisible = false,
        AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill,
    };

    private bool _loading;

    public ConfigurationTab()
    {
        var keyCol = new DataGridViewTextBoxColumn { Name = "key", HeaderText = "Anahtar", ReadOnly = true, FillWeight = 140 };
        var valCol = new DataGridViewTextBoxColumn { Name = "value", HeaderText = "Değer", FillWeight = 120 };
        var roCol = new DataGridViewCheckBoxColumn { Name = "ro", HeaderText = "Salt Okunur", ReadOnly = true, FillWeight = 40 };
        _grid.Columns.AddRange(keyCol, valCol, roCol);
        _grid.CellEndEdit += OnCellEndEdit;

        var info = new Label { Dock = DockStyle.Top, Height = 28, Text = "  Değeri değiştirmek CSMS'ten gelen ChangeConfiguration ile aynı etkiyi yapar. Salt okunur anahtarlar reddedilir.", ForeColor = Color.DimGray };
        Controls.Add(_grid);
        Controls.Add(info);
    }

    private void OnCellEndEdit(object? sender, DataGridViewCellEventArgs e)
    {
        if (_loading || e.ColumnIndex != _grid.Columns["value"]!.Index) return;
        var key = _grid.Rows[e.RowIndex].Cells["key"].Value?.ToString();
        var value = _grid.Rows[e.RowIndex].Cells["value"].Value?.ToString() ?? "";
        if (key is null) return;

        var status = Cp.Configuration.Set(key, value);
        if (status != ConfigurationStatus.Accepted)
        {
            MessageBox.Show($"{key}: {status}", "Konfigürasyon", MessageBoxButtons.OK, MessageBoxIcon.Information);
            RefreshUi(); // revert to stored value
        }
    }

    public override void RefreshUi()
    {
        _loading = true;
        _grid.Rows.Clear();
        foreach (var item in Cp.Configuration.All)
        {
            int idx = _grid.Rows.Add(item.Key, item.Value, item.Readonly);
            var row = _grid.Rows[idx];
            row.Cells["value"].ReadOnly = item.Readonly;
            if (item.Readonly) row.Cells["value"].Style.BackColor = Color.FromArgb(240, 240, 240);
        }
        _loading = false;
    }
}
