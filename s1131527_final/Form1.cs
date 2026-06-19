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

namespace s1131527_final
{
    public partial class Form1 : Form
    {
        // 儲存所有記帳紀錄的清單
        private List<Record> allRecords = new List<Record>();

        // 預設的自動存檔路徑
        private string defaultFilePath = Path.Combine(Application.StartupPath, "data.txt");

        public Form1()
        {
            InitializeComponent();
        }

        // 視窗載入時觸發的事件
        private void Form1_Load(object sender, EventArgs e)
        {
            // 1. 初始化分類下拉選單
            cmbCategory.SelectedIndex = 0; // 預設選取第一個
            cmbFilterCategory.SelectedIndex = 0;

            // 2. 自動讀取歷史資料
            LoadDataFromFile(defaultFilePath);

            // 3. 更新畫面顯示
            UpdateAllUI();
        }

        private void btnAdd_Click(object sender, EventArgs e)
        {
            if (string.IsNullOrWhiteSpace(txtAmount.Text))
            {
                MessageBox.Show("請輸入金額！", "提示", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            if (!int.TryParse(txtAmount.Text, out int amount) || amount <= 0)
            {
                MessageBox.Show("金額必須是小於或等於 1 的正整數！", "輸入錯誤", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            // 建立新紀錄物件
            Record newRecord = new Record
            {
                Date = dtpAdd.Value.Date,
                Category = cmbCategory.SelectedItem.ToString(),
                Amount = amount,
                Note = txtNote.Text.Trim()
            };

            // 加入清單
            allRecords.Add(newRecord);

            // 自動存檔 (確保資料不遺失)
            SaveDataToFile(defaultFilePath);

            // 清空輸入欄位方便下一筆輸入
            txtAmount.Clear();
            txtNote.Clear();

            // 更新所有介面
            UpdateAllUI();
            MessageBox.Show("記帳成功！", "系統提示", MessageBoxButtons.OK, MessageBoxIcon.Information);
        }

        // 計算並更新上方儀表板（收入、支出、餘額）與表格
        private void UpdateAllUI()
        {
            int totalIncome = allRecords.Where(r => r.Category == "收入").Sum(r => r.Amount);
            int totalExpense = allRecords.Where(r => r.Category != "收入").Sum(r => r.Amount);
            int balance = totalIncome - totalExpense;

            // 更新 Label 顯示
            lblIncome.Text = $"+ ${totalIncome:N0}";
            lblExpense.Text = $"- ${totalExpense:N0}";
            lblBalance.Text = $"${balance:N0}";

            // 根據餘額正負變色
            lblBalance.ForeColor = balance >= 0 ? System.Drawing.Color.Black : System.Drawing.Color.Red;

            // 重新整理兩個 DataGridView 的資料
            RefreshDataGridView(dgvQuick, allRecords.OrderByDescending(r => r.Date).Take(10).ToList()); // 首頁只顯示最新10筆
            RefreshDataGridView(dgvHistory, allRecords); // 歷史頁顯示全部
        }

        // 將清單資料綁定/寫入至 DataGridView 的通用 Method
        private void RefreshDataGridView(DataGridView dgv, List<Record> records)
        {
            dgv.Rows.Clear();
            foreach (var r in records)
            {
                // 注意：dgvHistory 最後一欄是刪除按鈕，底下的 Row 加上對應數值
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

            // LINQ 複合條件篩選
            var filtered = allRecords.Where(r => r.Date >= startDate && r.Date <= endDate).ToList();

            if (selectedCategory != "全部")
            {
                filtered = filtered.Where(r => r.Category == selectedCategory).ToList();
            }

            if (!string.IsNullOrEmpty(searchNote))
            {
                filtered = filtered.Where(r => r.Note.ToLower().Contains(searchNote)).ToList();
            }

            // 將篩選結果灌入歷史明細表格
            RefreshDataGridView(dgvHistory, filtered);
        }

        // 「🔄 重設」按鈕點擊事件
        private void btnReset_Click(object sender, EventArgs e)
        {
            dtpStart.Value = DateTime.Today;
            dtpEnd.Value = DateTime.Today;
            cmbFilterCategory.SelectedIndex = 0;
            txtSearchNote.Clear();
            RefreshDataGridView(dgvHistory, allRecords);
        }

        // 歷史明細表格內的「❌ 刪除」按鈕點擊事件 (CellContentClick)
        private void dgvHistory_CellContentClick(object sender, DataGridViewCellEventArgs e)
        {
            // 確保點擊的是「刪除」那一整行的按鈕欄位，且不是標頭
            if (e.RowIndex >= 0 && dgvHistory.Columns[e.ColumnIndex].HeaderText == "刪除")
            {
                DialogResult dialog = MessageBox.Show("確定要刪除這筆紀錄嗎？", "刪除確認", MessageBoxButtons.YesNo, MessageBoxIcon.Question);
                if (dialog == DialogResult.Yes)
                {
                    // 1. 取得要刪除的那筆資料特徵 (實務上建議用 ID，這裡用時間/分類/金額/備註比對)
                    string dateStr = dgvHistory.Rows[e.RowIndex].Cells[0].Value.ToString();
                    string category = dgvHistory.Rows[e.RowIndex].Cells[1].Value.ToString();
                    string amountStr = dgvHistory.Rows[e.RowIndex].Cells[2].Value.ToString().Replace("$", "").Replace(",", "");
                    int amount = int.Parse(amountStr);
                    string note = dgvHistory.Rows[e.RowIndex].Cells[3].Value.ToString();

                    // 2. 從總清單尋找並移除
                    var target = allRecords.FirstOrDefault(r => r.Date.ToString("yyyy/MM/dd") == dateStr && r.Category == category && r.Amount == amount && r.Note == note);
                    if (target != null)
                    {
                        allRecords.Remove(target);
                        SaveDataToFile(defaultFilePath); // 移除後同步存檔
                        UpdateAllUI(); // 更新 UI
                        MessageBox.Show("紀錄已成功刪除！", "提示", MessageBoxButtons.OK, MessageBoxIcon.Information);
                    }
                }
            }
        }



        // 存檔 (寫檔機制)：格式採用簡單的 CSV 逗號分隔格式
        private void SaveDataToFile(string filePath)
        {
            try
            {
                using (StreamWriter sw = new StreamWriter(filePath, false, System.Text.Encoding.UTF8))
                {
                    foreach (var r in allRecords)
                    {
                        // 寫入格式：日期,分類,金額,備註
                        sw.WriteLine($"{r.Date:yyyy-MM-dd},{r.Category},{r.Amount},{r.Note}");
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"存檔失敗：{ex.Message}", "錯誤", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        // 讀檔機制
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

        // 「📥 匯入歷史帳目」按鈕
        private void btnImport_Click(object sender, EventArgs e)
        {
            OpenFileDialog openFileDialog = new OpenFileDialog();
            openFileDialog.Filter = "文字檔案 (*.txt)|*.txt|所有檔案 (*.*)|*.*";
            if (openFileDialog.ShowDialog() == DialogResult.OK)
            {
                LoadDataFromFile(openFileDialog.FileName);
                SaveDataToFile(defaultFilePath); // 同步覆蓋預設預載檔
                UpdateAllUI();
                MessageBox.Show("外部歷史帳目匯入成功！", "成功", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
        }

        // 「📤 匯出資料備份」按鈕
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

    }

    // 儲存單筆記帳資料的類別結構
    public class Record
    {
        public DateTime Date { get; set; }
        public string Category { get; set; }
        public int Amount { get; set; }
        public string Note { get; set; }
    }
}
