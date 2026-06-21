using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using System.Windows.Forms.DataVisualization.Charting;

namespace s1131527_final
{
    public partial class Form1 : Form
    {
        private List<Record> allRecords = new List<Record>();
        private List<PlanTransaction> planTransactions = new List<PlanTransaction>();
        private string defaultFilePath = Path.Combine(Application.StartupPath, "data.txt");
        private string planFilePath = Path.Combine(Application.StartupPath, "plan_data.txt");

        public Form1()
        {
            InitializeComponent();
        }

        private void Form1_Load(object sender, EventArgs e)
        {
            cmbCategory.SelectedIndex = 0;
            cmbFilterCategory.SelectedIndex = 0;
            cmbPlanType.SelectedIndex = 0;
            cmbPlanCategory.SelectedIndex = 0;

            nudPlanDay.Minimum = 1;
            nudPlanDay.Maximum = 31;
            nudPlanDay.Value = 1;

            dgvQuick.RowHeadersVisible = false;
            dgvHistory.RowHeadersVisible = false;
            if (dgvPlan != null) dgvPlan.RowHeadersVisible = false;

            LoadDataFromFile(defaultFilePath);
            LoadPlanFromFile(planFilePath);
            UpdateAllUI();
        }

        private void btnAdd_Click(object sender, EventArgs e)
        {
            btnAdd.Enabled = false;
            try
            {
                if (string.IsNullOrWhiteSpace(txtAmount.Text))
                {
                    MessageBox.Show("請輸入金額！", "提示", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    return;
                }

                if (!int.TryParse(txtAmount.Text, out int amount) || amount <= 0)
                {
                    MessageBox.Show("金額必須是大於 0 的正整數！", "輸入錯誤", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    return;
                }

                Record newRecord = new Record
                {
                    Date = dtpAdd.Value.Date,
                    Category = cmbCategory.SelectedItem.ToString(),
                    Amount = amount,
                    Note = txtNote.Text.Trim()
                };

                allRecords.Add(newRecord);
                SaveDataToFile(defaultFilePath);
                txtAmount.Clear();
                txtNote.Clear();
                UpdateAllUI();
                MessageBox.Show("記帳成功！", "系統提示", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
            finally
            {
                btnAdd.Enabled = true;
            }
        }

        private void btnPlanAdd_Click(object sender, EventArgs e)
        {
            if (cmbPlanType.SelectedItem == null || cmbPlanCategory.SelectedItem == null)
            {
                MessageBox.Show("請選擇交易類型與分類！", "提示", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            if (string.IsNullOrWhiteSpace(txtPlanAmount.Text))
            {
                MessageBox.Show("請輸入計畫金額！", "提示", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            if (!int.TryParse(txtPlanAmount.Text, out int amount) || amount <= 0)
            {
                MessageBox.Show("計畫金額必須是大於 0 的正整數！", "輸入錯誤", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            if (dtpPlanStart.Value.Date > dtpPlanEnd.Value.Date)
            {
                MessageBox.Show("開始日期不能晚於結束日期！", "輸入錯誤", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            PlanTransaction newPlan = new PlanTransaction
            {
                Type = cmbPlanType.SelectedItem.ToString(),
                Category = cmbPlanCategory.SelectedItem.ToString(),
                Amount = amount,
                Note = txtPlanNote.Text.Trim(),
                Day = (int)nudPlanDay.Value,
                StartDate = dtpPlanStart.Value.Date,
                EndDate = dtpPlanEnd.Value.Date
            };

            planTransactions.Add(newPlan);
            SavePlanToFile(planFilePath);
            txtPlanAmount.Clear();
            txtPlanNote.Clear();
            UpdateAllUI();
            MessageBox.Show("每月固定計畫新增成功！", "系統提示", MessageBoxButtons.OK, MessageBoxIcon.Information);
        }

        private void dgvPlan_CellContentClick(object sender, DataGridViewCellEventArgs e)
        {
            if (e.RowIndex >= 0 && dgvPlan.Columns[e.ColumnIndex].HeaderText == "刪除")
            {
                DialogResult dialog = MessageBox.Show("確定要刪除這筆每月固定計畫嗎？", "刪除確認", MessageBoxButtons.YesNo, MessageBoxIcon.Question);
                if (dialog == DialogResult.Yes)
                {
                    Guid targetId = (Guid)dgvPlan.Rows[e.RowIndex].Tag;
                    var target = planTransactions.FirstOrDefault(p => p.Id == targetId);
                    if (target != null)
                    {
                        planTransactions.Remove(target);
                        SavePlanToFile(planFilePath);
                        UpdateAllUI();
                        MessageBox.Show("固定計畫已成功刪除！", "提示", MessageBoxButtons.OK, MessageBoxIcon.Information);
                    }
                }
            }
        }

        private void UpdateAllUI()
        {
            DateTime targetMonthStart = new DateTime(DateTime.Today.Year, DateTime.Today.Month, 1);
            DateTime targetMonthEnd = targetMonthStart.AddMonths(1).AddDays(-1);

            var thisMonthRecords = allRecords.Where(r =>
                r.Date.Year == DateTime.Today.Year &&
                r.Date.Month == DateTime.Today.Month).ToList();

            int totalIncome = thisMonthRecords.Where(r => r.Category == "收入").Sum(r => r.Amount);
            int totalExpense = thisMonthRecords.Where(r => r.Category != "收入").Sum(r => r.Amount);

            foreach (var plan in planTransactions)
            {
                if (plan.StartDate <= targetMonthEnd && plan.EndDate >= targetMonthStart)
                {
                    int dayInPlan = Math.Min(plan.Day, DateTime.DaysInMonth(DateTime.Today.Year, DateTime.Today.Month));
                    DateTime actionDate = new DateTime(DateTime.Today.Year, DateTime.Today.Month, dayInPlan);

                    if (actionDate >= plan.StartDate && actionDate <= plan.EndDate)
                    {
                        if (plan.Type == "固定收入")
                            totalIncome += plan.Amount;
                        else
                            totalExpense += plan.Amount;
                    }
                }
            }

            int balance = totalIncome - totalExpense;

            lblIncome.Text = $"+ ${totalIncome:N0}";
            lblExpense.Text = $"- ${totalExpense:N0}";
            lblBalance.Text = $"${balance:N0}";
            lblBalance.ForeColor = balance >= 0 ? Color.Black : Color.Red;

            RefreshDataGridView(dgvQuick, allRecords.OrderByDescending(r => r.Date).Take(10).ToList());
            RefreshDataGridView(dgvHistory, allRecords);
            RefreshPlanDataGridView();
            RenderCharts();
        }

        private void RefreshDataGridView(DataGridView dgv, List<Record> records)
        {
            if (dgv == null) return;
            dgv.Rows.Clear();
            bool hasDeleteCol = dgv.Columns.Cast<DataGridViewColumn>().Any(c => c.HeaderText == "刪除");
            foreach (var r in records)
            {
                int rowIndex = hasDeleteCol
                    ? dgv.Rows.Add(r.Date.ToString("yyyy/MM/dd"), r.Category, $"${r.Amount:N0}", r.Note, "刪除")
                    : dgv.Rows.Add(r.Date.ToString("yyyy/MM/dd"), r.Category, $"${r.Amount:N0}", r.Note);
                dgv.Rows[rowIndex].Tag = r.Id;
            }
        }

        private void RefreshPlanDataGridView()
        {
            if(dgvPlan == null) return;
            dgvPlan.Rows.Clear();
            foreach (var p in planTransactions)
            {
                int rowIndex = dgvPlan.Rows.Add(
                    p.Type,
                    p.Category,
                    $"${p.Amount:N0}",
                    p.Note,
                    "刪除");
                dgvPlan.Rows[rowIndex].Tag = p.Id;
            }
        }

        private void btnFilter_Click(object sender, EventArgs e)
        {
            DateTime startDate = dtpStart.Value.Date;
            DateTime endDate = dtpEnd.Value.Date;
            string selectedCategory = cmbFilterCategory.SelectedItem.ToString();
            string searchNote = txtSearchNote.Text.Trim().ToLower();

            var filtered = allRecords.Where(r => r.Date >= startDate && r.Date <= endDate).ToList();

            if (selectedCategory != "全部")
                filtered = filtered.Where(r => r.Category == selectedCategory).ToList();

            if (!string.IsNullOrEmpty(searchNote))
                filtered = filtered.Where(r => r.Note.ToLower().Contains(searchNote)).ToList();

            RefreshDataGridView(dgvHistory, filtered);
        }

        private void btnReset_Click(object sender, EventArgs e)
        {
            dtpStart.Value = DateTime.Today;
            dtpEnd.Value = DateTime.Today;
            cmbFilterCategory.SelectedIndex = 0;
            txtSearchNote.Clear();
            RefreshDataGridView(dgvHistory, allRecords);
        }

        private void dgvHistory_CellContentClick(object sender, DataGridViewCellEventArgs e)
        {
            if (e.RowIndex >= 0 && dgvHistory.Columns[e.ColumnIndex].HeaderText == "刪除")
            {
                DialogResult dialog = MessageBox.Show("確定要刪除這筆紀錄嗎？", "刪除確認", MessageBoxButtons.YesNo, MessageBoxIcon.Question);
                if (dialog == DialogResult.Yes)
                {
                    Guid targetId = (Guid)dgvHistory.Rows[e.RowIndex].Tag;
                    var target = allRecords.FirstOrDefault(r => r.Id == targetId);
                    if (target != null)
                    {
                        allRecords.Remove(target);
                        SaveDataToFile(defaultFilePath);
                        UpdateAllUI();
                        MessageBox.Show("紀錄已成功刪除！", "提示", MessageBoxButtons.OK, MessageBoxIcon.Information);
                    }
                }
            }
        }

        private void SaveDataToFile(string filePath)
        {
            try
            {
                using (StreamWriter sw = new StreamWriter(filePath, false, Encoding.UTF8))
                {
                    foreach (var r in allRecords)
                        sw.WriteLine($"{r.Id},{r.Date:yyyy-MM-dd},{r.Category},{r.Amount},{r.Note}");
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"存檔失敗：{ex.Message}", "錯誤", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void LoadDataFromFile(string filePath)
        {
            if (!File.Exists(filePath)) return;
            try
            {
                allRecords.Clear();
                foreach (string line in File.ReadAllLines(filePath, Encoding.UTF8))
                {
                    if (string.IsNullOrWhiteSpace(line)) continue;
                    string[] parts = line.Split(',');
                    if (parts.Length >= 5)
                    {
                        allRecords.Add(new Record
                        {
                            Id = Guid.TryParse(parts[0], out Guid id) ? id : Guid.NewGuid(),
                            Date = DateTime.Parse(parts[1]),
                            Category = parts[2],
                            Amount = int.Parse(parts[3]),
                            Note = string.Join(",", parts.Skip(4))
                        });
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"讀取檔案失敗：{ex.Message}", "錯誤", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void SavePlanToFile(string filePath)
        {
            try
            {
                using (StreamWriter sw = new StreamWriter(filePath, false, Encoding.UTF8))
                {
                    foreach (var p in planTransactions)
                        sw.WriteLine($"{p.Id},{p.Type},{p.Category},{p.Amount},{p.Day},{p.StartDate:yyyy-MM-dd},{p.EndDate:yyyy-MM-dd},{p.Note}");
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"計劃存檔失敗：{ex.Message}", "錯誤", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void LoadPlanFromFile(string filePath)
        {
            if (!File.Exists(filePath)) return;
            try
            {
                planTransactions.Clear();
                foreach (string line in File.ReadAllLines(filePath, Encoding.UTF8))
                {
                    if (string.IsNullOrWhiteSpace(line)) continue;
                    string[] parts = line.Split(',');
                    if (parts.Length >= 8)
                    {
                        planTransactions.Add(new PlanTransaction
                        {
                            Id = Guid.TryParse(parts[0], out Guid id) ? id : Guid.NewGuid(),
                            Type = parts[1],
                            Category = parts[2],
                            Amount = int.Parse(parts[3]),
                            Day = int.Parse(parts[4]),
                            StartDate = DateTime.Parse(parts[5]),
                            EndDate = DateTime.Parse(parts[6]),
                            Note = string.Join(",", parts.Skip(7))
                        });
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"讀取計畫檔案失敗：{ex.Message}", "錯誤", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void btnImport_Click(object sender, EventArgs e)
        {
            OpenFileDialog openFileDialog = new OpenFileDialog();
            openFileDialog.Filter = "文字檔案 (*.txt)|*.txt|所有檔案 (*.*)|*.*";
            if (openFileDialog.ShowDialog() == DialogResult.OK)
            {
                LoadDataFromFile(openFileDialog.FileName);
                SaveDataToFile(defaultFilePath);
                UpdateAllUI();
                MessageBox.Show("外部歷史帳目匯入成功！", "成功", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
        }

        private void btnExport_Click(object sender, EventArgs e)
        {
            SaveFileDialog saveFileDialog = new SaveFileDialog();
            saveFileDialog.Filter = "文字檔案 (*.txt)|*.txt";
            saveFileDialog.FileName = $"記帳備份_{DateTime.Now:yyyyMMdd}";
            if (saveFileDialog.ShowDialog() == DialogResult.OK)
            {
                SaveDataToFile(saveFileDialog.FileName);
                MessageBox.Show("資料成功匯出至指定路徑！", "成功", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
        }

        private void Form1_FormClosing(object sender, FormClosingEventArgs e)
        {
            if (MessageBox.Show("確定要離開記帳系統？", "確認", MessageBoxButtons.YesNo) == DialogResult.No)
                e.Cancel = true;
        }

        private void RenderCharts()
        {
            if (chartPie == null) return;

            chartPie.Series.Clear();
            chartPie.Titles.Clear();

            var title = chartPie.Titles.Add("本月支出分類比例");
            title.Font = new Font("Microsoft JhengHei", 12, FontStyle.Bold);

            DateTime targetMonthStart = new DateTime(DateTime.Today.Year, DateTime.Today.Month, 1);
            DateTime targetMonthEnd = targetMonthStart.AddMonths(1).AddDays(-1);

            var categoryTotals = allRecords
                .Where(r => r.Date.Year == DateTime.Today.Year &&
                            r.Date.Month == DateTime.Today.Month &&
                            r.Category != "收入")
                .GroupBy(r => r.Category)
                .ToDictionary(g => g.Key, g => g.Sum(r => r.Amount));

            foreach (var plan in planTransactions)
            {
                if (plan.Type == "固定支出" && plan.StartDate <= targetMonthEnd && plan.EndDate >= targetMonthStart)
                {
                    int dayInPlan = Math.Min(plan.Day, DateTime.DaysInMonth(DateTime.Today.Year, DateTime.Today.Month));
                    DateTime actionDate = new DateTime(DateTime.Today.Year, DateTime.Today.Month, dayInPlan);

                    if (actionDate >= plan.StartDate && actionDate <= plan.EndDate)
                    {
                        if (categoryTotals.ContainsKey(plan.Category))
                            categoryTotals[plan.Category] += plan.Amount;
                        else
                            categoryTotals[plan.Category] = plan.Amount;
                    }
                }
            }

            if (categoryTotals.Count == 0)
            {
                chartPie.Titles.Add("本月尚無支出紀錄");
                return;
            }

            Series pieSeries = new Series("ExpensePie")
            {
                ChartType = SeriesChartType.Pie,
                Font = new Font("Microsoft JhengHei", 10, FontStyle.Regular),
                IsValueShownAsLabel = true,
                Label = "#VALX #PERCENT{P1}"
            };
            pieSeries["PieLabelStyle"] = "Outside";
            pieSeries["PieLineColor"] = "Black";

            foreach (var item in categoryTotals)
                pieSeries.Points.AddXY(item.Key, item.Value);

            chartPie.Series.Add(pieSeries);
        }
    }

    public class Record
    {
        public Guid Id { get; set; } = Guid.NewGuid();
        public DateTime Date { get; set; }
        public string Category { get; set; }
        public int Amount { get; set; }
        public string Note { get; set; }
    }

    public class PlanTransaction
    {
        public Guid Id { get; set; } = Guid.NewGuid();
        public string Type { get; set; }
        public string Category { get; set; }
        public int Amount { get; set; }
        public string Note { get; set; }
        public int Day { get; set; }
        public DateTime StartDate { get; set; }
        public DateTime EndDate { get; set; }
    }
}