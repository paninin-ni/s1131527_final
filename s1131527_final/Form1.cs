using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
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

            if (cmbPlanType != null && cmbPlanType.Items.Count > 0) cmbPlanType.SelectedIndex = 0;
            if (cmbPlanCategory != null && cmbPlanCategory.Items.Count > 0) cmbPlanCategory.SelectedIndex = 0;

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
            finally { btnAdd.Enabled = true; }
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

            PlanTransaction newPlan = new PlanTransaction
            {
                Type = cmbPlanType.SelectedItem.ToString(),
                Category = cmbPlanCategory.SelectedItem.ToString(),
                Amount = amount,
                Note = txtPlanNote.Text.Trim()
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
                    string type = dgvPlan.Rows[e.RowIndex].Cells[0].Value.ToString();
                    string category = dgvPlan.Rows[e.RowIndex].Cells[1].Value.ToString();
                    string amountStr = dgvPlan.Rows[e.RowIndex].Cells[2].Value.ToString().Replace("$", "").Replace(",", "");
                    int amount = int.Parse(amountStr);
                    string note = dgvPlan.Rows[e.RowIndex].Cells[3].Value.ToString();

                    var target = planTransactions.FirstOrDefault(p => p.Type == type && p.Category == category && p.Amount == amount && p.Note == note);
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
            var thisMonth = allRecords.Where(r =>
              r.Date.Year == DateTime.Today.Year &&
              r.Date.Month == DateTime.Today.Month).ToList();
            int totalIncome = thisMonth.Where(r => r.Category == "收入").Sum(r => r.Amount);
            int totalExpense = thisMonth.Where(r => r.Category != "收入").Sum(r => r.Amount);
            int balance = totalIncome - totalExpense;

            lblIncome.Text = $"+ ${totalIncome:N0}";
            lblExpense.Text = $"- ${totalExpense:N0}";
            lblBalance.Text = $"${balance:N0}";

            lblBalance.ForeColor = balance >= 0 ? System.Drawing.Color.Black : System.Drawing.Color.Red;

            RefreshDataGridView(dgvQuick, allRecords.OrderByDescending(r => r.Date).Take(10).ToList());
            RefreshDataGridView(dgvHistory, allRecords);

            RefreshPlanDataGridView();

            RenderCharts();
        }

        private void RefreshDataGridView(DataGridView dgv, List<Record> records)
        {
            if (dgv == null) return;
            dgv.Rows.Clear();
            foreach (var r in records)
            {
                if (dgv.Columns.Contains("colDelete") || dgv.Columns.Cast<DataGridViewColumn>().Any(c => c.HeaderText == "刪除"))
                {
                    dgv.Rows.Add(r.Date.ToString("yyyy/MM/dd"), r.Category, $"${r.Amount:N0}", r.Note, "❌ 刪除");
                }
                else
                {
                    dgv.Rows.Add(r.Date.ToString("yyyy/MM/dd"), r.Category, $"${r.Amount:N0}", r.Note);
                }
            }
        }

        private void RefreshPlanDataGridView()
        {
            if (dgvPlan == null) return;
            dgvPlan.Rows.Clear();
            foreach (var p in planTransactions)
            {
                dgvPlan.Rows.Add(p.Type, p.Category, $"${p.Amount:N0}", p.Note, "❌ 刪除");
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
            {
                filtered = filtered.Where(r => r.Category == selectedCategory).ToList();
            }

            if (!string.IsNullOrEmpty(searchNote))
            {
                filtered = filtered.Where(r => r.Note.ToLower().Contains(searchNote)).ToList();
            }

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
                    string dateStr = dgvHistory.Rows[e.RowIndex].Cells[0].Value.ToString();
                    string category = dgvHistory.Rows[e.RowIndex].Cells[1].Value.ToString();
                    string amountStr = dgvHistory.Rows[e.RowIndex].Cells[2].Value.ToString().Replace("$", "").Replace(",", "");
                    int amount = int.Parse(amountStr);
                    string note = dgvHistory.Rows[e.RowIndex].Cells[3].Value.ToString();

                    var target = allRecords.FirstOrDefault(r => r.Date.ToString("yyyy/MM/dd") == dateStr && r.Category == category && r.Amount == amount && r.Note == note);
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
                using (StreamWriter sw = new StreamWriter(filePath, false, System.Text.Encoding.UTF8))
                {
                    foreach (var r in allRecords)
                    {
                        sw.WriteLine($"{r.Date:yyyy-MM-dd},{r.Category},{r.Amount},{r.Note}");
                    }
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
                string[] lines = File.ReadAllLines(filePath, System.Text.Encoding.UTF8);
                foreach (string line in lines)
                {
                    if (string.IsNullOrWhiteSpace(line)) continue;
                    string[] parts = line.Split(',');
                    if (parts.Length >= 4)
                    {
                        Record r = new Record
                        {
                            Date = DateTime.Parse(parts[0]),
                            Category = parts[1],
                            Amount = int.Parse(parts[2]),
                            Note = parts[3]
                        };
                        allRecords.Add(r);
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
                    {
                        sw.WriteLine($"{p.Type},{p.Category},{p.Amount},{p.Note}");
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"計檔案劃存檔失敗：{ex.Message}", "錯誤", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        // 💡 新增：每月固定計畫檔案讀取功能
        private void LoadPlanFromFile(string filePath)
        {
            if (!File.Exists(filePath)) return;
            try
            {
                planTransactions.Clear();
                string[] lines = File.ReadAllLines(filePath, Encoding.UTF8);
                foreach (string line in lines)
                {
                    if (string.IsNullOrWhiteSpace(line)) continue;
                    string[] parts = line.Split(',');
                    if (parts.Length >= 4)
                    {
                        planTransactions.Add(new PlanTransaction
                        {
                            Type = parts[0],
                            Category = parts[1],
                            Amount = int.Parse(parts[2]),
                            Note = parts[3]
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

        private void lblExpense_Click(object sender, EventArgs e) { }

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

            Title title = chartPie.Titles.Add("本月支出分類比例");
            title.Font = new Font("Microsoft JhengHei", 12, FontStyle.Bold);

            var thisMonthExpenses = allRecords
                .Where(r => r.Date.Year == DateTime.Today.Year && r.Date.Month == DateTime.Today.Month && r.Category != "收入")
                .GroupBy(r => r.Category)
                .Select(g => new { Category = g.Key, Total = g.Sum(r => r.Amount) })
                .ToList();

            if (thisMonthExpenses.Count == 0)
            {
                chartPie.Titles.Add("本月尚無支出紀錄");
                return;
            }

            Series pieSeries = new Series("ExpensePie")
            {
                ChartType = SeriesChartType.Pie,
                Font = new Font("Microsoft JhengHei", 10, FontStyle.Regular)
            };

            foreach (var item in thisMonthExpenses)
            {
                pieSeries.Points.AddXY(item.Category, item.Total);
            }

            pieSeries.IsValueShownAsLabel = true;
            pieSeries.Label = "#VALX #PERCENT{P1}";
            pieSeries["PieLabelStyle"] = "Outside";
            pieSeries["PieLineColor"] = "Black";

            chartPie.Series.Add(pieSeries);
        }
    }

    public class Record
    {
        public DateTime Date { get; set; }
        public string Category { get; set; }
        public int Amount { get; set; }
        public string Note { get; set; }
    }

    public class PlanTransaction
    {
        public string Type { get; set; }      
        public string Category { get; set; }  
        public int Amount { get; set; }       
        public string Note { get; set; }       
    }
}