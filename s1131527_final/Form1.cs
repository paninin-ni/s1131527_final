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

        private string defaultFilePath = Path.Combine(Application.StartupPath, "data.txt");

        public Form1()
        {
            InitializeComponent();
        }

        private void Form1_Load(object sender, EventArgs e)
        {
            cmbCategory.SelectedIndex = 0; 
            cmbFilterCategory.SelectedIndex = 0;

            LoadDataFromFile(defaultFilePath);
            UpdateAllUI();
        }

        private void btnAdd_Click(object sender, EventArgs e)
        {
            btnAdd.Enabled = false;
            try {
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

            RefreshDataGridView(dgvQuick, allRecords.OrderByDescending(r => r.Date).Take(10).ToList()); // 首頁只顯示最新10筆
            RefreshDataGridView(dgvHistory, allRecords); // 歷史頁顯示全部
        }

        private void RefreshDataGridView(DataGridView dgv, List<Record> records)
        {
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

        private void lblExpense_Click(object sender, EventArgs e)
        {

        }

        private void Form1_FormClosing(object sender, FormClosingEventArgs e)
        {
            if (MessageBox.Show("確定要離開記帳系統？", "確認", MessageBoxButtons.YesNo) == DialogResult.No)
                e.Cancel = true;
        }

        private void RenderCharts()
        {
            if (chartPie == null) return;

            // 1. 初始化圖表
            chartPie.Series.Clear();
            chartPie.Titles.Clear();

            // 設定大標題（自訂字型與大小）
            Title title = chartPie.Titles.Add("本月支出分類比例");
            title.Font = new Font("Microsoft JhengHei", 12, FontStyle.Bold);

            // 2. 篩選本月支出資料（排除「收入」）
            var thisMonthExpenses = allRecords
                .Where(r => r.Date.Year == DateTime.Today.Year && r.Date.Month == DateTime.Today.Month && r.Category != "收入")
                .GroupBy(r => r.Category)
                .Select(g => new { Category = g.Key, Total = g.Sum(r => r.Amount) })
                .ToList();

            // 💡 防呆機制：如果本月完全沒有記任何一筆支出，就不要畫圖，改顯示提示文字
            if (thisMonthExpenses.Count == 0)
            {
                chartPie.Titles.Add("本月尚無支出紀錄");
                return;
            }

            // 3. 建立並設定 Pie Series
            Series pieSeries = new Series("ExpensePie")
            {
                ChartType = SeriesChartType.Pie,
                Font = new Font("Microsoft JhengHei", 10, FontStyle.Regular) // 設定扇形上文字的字型
            };

            // 4. 將資料灌入圖表
            foreach (var item in thisMonthExpenses)
            {
                pieSeries.Points.AddXY(item.Category, item.Total);
            }

            // 5. 🌟 讓圓餅圖強大又美觀的關鍵設定 🌟

            // 顯示標籤文字 (預設會顯示金額數字)
            pieSeries.IsValueShownAsLabel = true;

            // 改成顯示「百分比」而非單純數字 (例如：食 45%)
            // #VALX 代表 X軸名稱(分類)，#PERCENT 代表該區塊所佔百分比
            pieSeries.Label = "#VALX #PERCENT{P1}";

            // 讓標籤字體彈出到圓餅圖外面（避免分類太多時，字體全部擠在圓餅圖裡面重疊）
            pieSeries["PieLabelStyle"] = "Outside";

            // 連接圓餅圖與外部文字的拉線顏色
            pieSeries["PieLineColor"] = "Black";

            // 把設定好的 Series 加回圖表
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
}
