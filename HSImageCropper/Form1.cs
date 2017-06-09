using System;
using System.IO;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Drawing.Drawing2D;

namespace HSImageCropper
{
    public partial class Form1 : Form
    {
        enum MODE
        {
            MODE_AVATAR,
            MODE_SQUAD,
            MODE_COUNT
        }

        enum KeyModifier
        {
            None = 0,
            Alt = 1,
            Control = 2,
            Shift = 4,
            WinKey = 8
        }

        enum HOTKEYID
        {
            HOTKEY_AVATAR = 0,
            HOTKEY_SQUAD  = 1,
            HOTKEY_SAVE   = 2,
            HOTKEY_COUNT
        }

        class InfoStruct
        {
            public InfoStruct()
            {
                _name = "";
                _X = 0;
                _Y = 0;
                _squadX = 0;
                _squadY = 0;
            }
            public String _name;
            public int _X;
            public int _Y;

            public int _squadX;
            public int _squadY;

            public PictureBox previewPictureBox;
              
            public void setAvatarCood(int x, int y)
            {
                _X = x;
                _Y = y;
            }

            public void setSquadCood(int x, int y)
            {
                _squadX = x;
                _squadY = y;
            }

            public void setAvatarCood(Point p)
            {
                setAvatarCood(p.X, p.Y);
            }

            public void setSquadCood(Point p)
            {
                setSquadCood(p.X, p.Y);
            }


            public String formatCoordinate(bool isSquad = false)
            {
                if (isSquad == true)
                {
                    return "(" + _squadX + "," + _squadY + ")";
                }
                return "(" + _X + "," + _Y + ")";
            }
        }

        private PictureBox previewPictureBox = null;
        private MODE curMode = MODE.MODE_AVATAR;
        //private List<String> resImages = new List<string>(100);
        private String folderPath = "";
        private List<InfoStruct> resInfo = new List<InfoStruct>(100);
        private Point cursorPosition;

        private PictureBox hintView = new PictureBox();

        [System.Runtime.InteropServices.DllImport("user32.dll")]
        private static extern bool RegisterHotKey(IntPtr hWnd, int id, int fsModifiers, int vk);
        [System.Runtime.InteropServices.DllImport("user32.dll")]
        private static extern bool UnregisterHotKey(IntPtr hWnd, int id);

        public Form1()
        {
            InitializeComponent();
            initListViewHeader();
            Init();
        }

        private void registerHotKey()
        {

         }

        private void Init()
        {
            CoordHint.Visible = false;
            radioButton1.Checked = true;
            hintView.BackColor = Color.FromArgb(200,200,200,200);
            //BorderStyle
            hintView.Enabled = false;
            
            //hintView.Paint += new System.Windows.Forms.PaintEventHandler(this.hintview_Paint);

            int avatarID = (int)HOTKEYID.HOTKEY_AVATAR;
            RegisterHotKey(this.Handle, avatarID, (int)KeyModifier.None, Keys.Q.GetHashCode());
            int squadID = (int)HOTKEYID.HOTKEY_SQUAD;
            RegisterHotKey(this.Handle, squadID, (int)KeyModifier.None, Keys.W.GetHashCode());
            int savaID = (int)HOTKEYID.HOTKEY_SAVE;
            RegisterHotKey(this.Handle, savaID, (int)KeyModifier.Control, Keys.S.GetHashCode());

            pictureBox.Enabled = false;

        }

        protected override void WndProc(ref Message m)
        {
            base.WndProc(ref m);

            if (m.Msg == 0x0312)
            {
                /* Note that the three lines below are not needed if you only want to register one hotkey.
                 * The below lines are useful in case you want to register multiple keys, which you can use a switch with the id as argument, or if you want to know which key/modifier was pressed for some particular reason. */

                Keys key = (Keys)(((int)m.LParam >> 16) & 0xFFFF);                  // The key of the hotkey that was pressed.
                KeyModifier modifier = (KeyModifier)((int)m.LParam & 0xFFFF);       // The modifier of the hotkey that was pressed.
                int id = m.WParam.ToInt32();                                        // The id of the hotkey that was pressed.


                if (id == (int)HOTKEYID.HOTKEY_AVATAR)
                {
                    radioButton1.Checked = true;
                }
                else if (id == (int)HOTKEYID.HOTKEY_SQUAD)
                {
                    radioButton2.Checked = true;
                }
                else if (id == (int)HOTKEYID.HOTKEY_SAVE)
                {
                    saveData();
                }
            }
        }

        private void saveData()
        {
            string luatable = "local resPosInfo = {\n";
            foreach(InfoStruct data in resInfo)
            {
                string item = String.Format("\t{0}={{avatar={{ x={1},y={2} }},squad={{ x={3},y={4} }}}},\n", System.IO.Path.GetFileNameWithoutExtension(data._name),data._X,data._Y,data._squadX,data._squadY);
                luatable += item;
                //Console.WriteLine(item);
            }
            luatable += "}\nreturn resPosInfo";

            SaveFileDialog saveFileDialog1 = new SaveFileDialog();
            saveFileDialog1.InitialDirectory = "D:";
            saveFileDialog1.Filter = "Lua 文件 (*.lua)|*.lua|All files(*.*)|*>**";
            saveFileDialog1.FilterIndex = 0;
            saveFileDialog1.RestoreDirectory = true;
            DialogResult dr = saveFileDialog1.ShowDialog();
            if (dr == DialogResult.OK && saveFileDialog1.FileName.Length > 0)
            {
                FileStream fs = new FileStream(saveFileDialog1.FileName, FileMode.Append);
                StreamWriter streamWriter = new StreamWriter(fs, Encoding.UTF8);
                streamWriter.Write(luatable + "\r\n");
                streamWriter.Flush();
                streamWriter.Close();
                fs.Close();
                //richTextBox1.SaveFile(saveFileDialog1.FileName, RichTextBoxStreamType.PlainText);
                MessageBox.Show("存储文件成功！");
            }

            //Console.WriteLine(luatable);
        }

        private void textBox1_TextChanged(object sender, EventArgs e)
        {

        }

        private void hintview_Paint(object sender, System.Windows.Forms.PaintEventArgs e)
        {
            //Console.WriteLine("Paint");
            ControlPaint.DrawBorder(e.Graphics, new Rectangle(new Point(0,0),hintView.Size), Color.Ivory, ButtonBorderStyle.Dashed);
        }

        private void button1_Click(object sender, EventArgs e)
        {
            resInfo.Clear();
            listView1.Items.Clear();
            FolderBrowserDialog dialog = new FolderBrowserDialog();
            dialog.Description = "请选择文件路径";
            if (dialog.ShowDialog() == DialogResult.OK)
            {
                folderPath = dialog.SelectedPath;
                this.pathtextBox.Text = folderPath;
                traverseFolder(folderPath);
            }
        }

        private void traverseFolder(String path)
        {
            DirectoryInfo info = new DirectoryInfo(path);
            foreach(FileInfo file in info.GetFiles())
            {
                String lowwer = file.FullName.ToLower();
                if (lowwer.EndsWith(".png")||lowwer.EndsWith(".jpg"))
                {
                    string fileName = System.IO.Path.GetFileName(file.FullName);
                    InfoStruct data = new InfoStruct();
                    data._name = fileName;
                    resInfo.Add(data);
                }
            }
            initListView();
            if (resInfo.Count > 0)
            {
                listView1.Items[0].Selected = true;
                listView1.Items[0].Focused = true;
                pictureBox.Enabled = true;
            }
            else
            {
                pictureBox.Enabled = false;
            }
        }

        private void initListViewHeader()
        {
            this.listView1.Columns.Add("文件名", 175, HorizontalAlignment.Left);
            this.listView1.Columns.Add("头像数据点", 96, HorizontalAlignment.Right);
            this.listView1.Columns.Add("部队数据点", 96, HorizontalAlignment.Right);
            //listView1.ColumnWidthChanging += (e, sender) =>
            //{
            //    ColumnWidthChangingEventArgs arg = (ColumnWidthChangingEventArgs)sender;
            //    arg.Cancel = true;
            //    arg.NewWidth = listView1.Columns[arg.ColumnIndex].Width;
            //};
        }



        
        private void initListView()
        {

            this.listView1.BeginUpdate();   //数据更新，UI暂时挂起，直到EndUpdate绘制控件，可以有效避免闪烁并大大提高加载速度

            
            foreach(var data in resInfo)
            {
                ListViewItem lvi = new ListViewItem();
                lvi.Text = data._name;
                lvi.SubItems.Add(data.formatCoordinate());
                lvi.SubItems.Add(data.formatCoordinate(true));
                this.listView1.Items.Add(lvi);
            }

            this.listView1.EndUpdate();  //结束数据处理，UI界面一次性绘制。
        }

        private void loadImage(int idx)
        {

            String name = this.listView1.Items[idx].SubItems[0].Text;
            if (name != null && name != "")
            {
                try
                {
                    Image img = Image.FromFile(Path.Combine(@folderPath, @name));
                    this.pictureBox.Image = img;
                }
                catch(Exception e)
                {
                    MessageBox.Show(e.Message, "噢，遭了");
                }

            }
            //listView1.Items[idx].
        }

        private void listView1_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (listView1.SelectedIndices != null && listView1.SelectedIndices.Count > 0)
            {
                Console.WriteLine(listView1.SelectedItems[0].Index);
                loadImage(listView1.SelectedItems[0].Index);
            }
        }

        private void pictureBox_Click(object sender, EventArgs e)
        {
            int index = listView1.FocusedItem.Index;
            InfoStruct d = resInfo[index];

            if (curMode == MODE.MODE_AVATAR)
            {
                d.setAvatarCood(cursorPosition);
                this.listView1.FocusedItem.SubItems[1].Text = d.formatCoordinate();
            }
            else
            {
                d.setSquadCood(cursorPosition);
                this.listView1.FocusedItem.SubItems[2].Text = d.formatCoordinate(true);
            }
            resInfo[index] = d;
        }

        private void radioButton1_CheckedChanged(object sender, EventArgs e)
        {
            curMode = MODE.MODE_AVATAR;
            if (this.radioButton1.Checked==true)
            {
                hintView.Size = new Size(120, 150);
            }
        }

        private void radioButton2_CheckedChanged(object sender, EventArgs e)
        {
            curMode = MODE.MODE_SQUAD;
            if (radioButton2.Checked == true)
            {
                hintView.Size = new Size(120, 80);
            }
        }

        private void pictureBox_MouseEnter(object sender, EventArgs e)
        {
            CoordHint.Visible = true;
            pictureBox.Controls.Add(hintView);
        }   

        private void pictureBox_MouseLeave(object sender, EventArgs e)
        {
            CoordHint.Visible = false;
            pictureBox.Controls.Remove(hintView);   
        }

        private void pictureBox_MouseMove(object sender, MouseEventArgs e)
        {
            Point p = e.Location;
            string X = p.X.ToString();
            string Y = p.Y.ToString();
            cursorPosition = p;
            CoordHint.Text = "(" + X + "," + Y + ")";    
            hintView.Location = new Point(p.X, p.Y);
        }
    

        private void pictureBox_MouseDown(object sender, MouseEventArgs e)
        {

        }

        private void saveButton_Click(object sender, EventArgs e)
        {
            if(listView1.Items.Count == 0)
            {
                MessageBox.Show("你应该已经醉了", "没数据", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }
            saveData();
        }

        private void listView1_ItemMouseHover(object sender, ListViewItemMouseHoverEventArgs e)
        {
            //e.Item.BackColor = Color.Red;
            String name = e.Item.SubItems[0].Text;
            if (previewPictureBox==null)
            {
                previewPictureBox = new PictureBox();
                previewPictureBox.SizeMode = PictureBoxSizeMode.Zoom;
                previewPictureBox.BackColor = Color.Transparent;
                previewPictureBox.Size = new Size(200, 200);
                //previewPictureBox.BackColor = Color.Transparent;
               
            }
            Image img = Image.FromFile(Path.Combine(@folderPath, @name));
            //previewPictureBox.Size = img.Size;
            previewPictureBox.Location = new Point(this.Size.Width - previewPictureBox.Size.Width, 0);
            previewPictureBox.Image = img;
            this.Controls.Add(previewPictureBox);
            previewPictureBox.BringToFront();

        }

        private void listView1_MouseLeave(object sender, EventArgs e)
        {
            if (previewPictureBox!=null) {
                previewPictureBox.Parent.Controls.Remove(previewPictureBox);
                previewPictureBox = null;
            }
        }
    }
}
