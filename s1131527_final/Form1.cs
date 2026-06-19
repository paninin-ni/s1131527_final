using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace s1131527_final
{
    public partial class Form1 : Form
    {
        public Form1()
        {
            InitializeComponent();
        }

        private void dataGridView2_CellContentClick(object sender, DataGridViewCellEventArgs e)
        {
            
            if (e.ColumnIndex == dataGridView2.Columns["colDelete"].Index && e.RowIndex >= 0)
            {
                // 1. 彈出安全確認視窗（這符合專案防呆、流暢體驗的標準！）
                DialogResult result = MessageBox.Show("確定要刪除這筆記帳紀錄嗎？", "確認刪除", MessageBoxButtons.YesNo, MessageBoxIcon.Question);

                if (result == DialogResult.Yes)
                {
                    // 2. 從 DataGridView 畫面中移除這一列
                    dataGridView2.Rows.RemoveAt(e.RowIndex);

                    // 3. 提示：記得在這裡呼叫你寫的「存檔 function」，把更新後的資料寫回檔案，這樣才符合讀寫檔功能！
                    // SaveDataToFile();
                }
            }
        }

        private void Form1_Load(object sender, EventArgs e)
        {
            comboBox1.SelectedIndex = 0;
            comboBox2.SelectedIndex = 0;
        }
    }
}
