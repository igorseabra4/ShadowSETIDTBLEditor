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

namespace ShadowSETIDEditor
{
    public partial class Form1 : Form
    {
        public Form1()
        {
            InitializeComponent();

            if (File.Exists("Resources\\ShadowObjectList.ini") & File.Exists("Resources\\ShadowStageList.ini"))
            {
                ReadObjectListData("Resources\\ShadowObjectList.ini");
                ReadStageListData("Resources\\ShadowStageList.ini");
            }
            else
            {
                MessageBox.Show("Error loading external files.", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                Close();
            }
        }

        private uint Switch(uint v)
        {
            return BitConverter.ToUInt32(BitConverter.GetBytes(v).Reverse().ToArray(), 0);
        }

        public class ObjectEntry
        {
            public byte list;
            public byte type;
            public string name;
        }

        List<ObjectEntry> ObjectEntryList = new List<ObjectEntry>();

        private void ReadObjectListData(string FileName)
        {
            string[] ObjectListFile = File.ReadAllLines(FileName);

            byte List = 0;
            byte Type = 0;
            string Name = "";
            string DebugName = "";

            foreach (string i in ObjectListFile)
            {
                if (i.StartsWith("["))
                {
                    List = Convert.ToByte(i.Substring(1, 2), 16);
                    Type = Convert.ToByte(i.Substring(5, 2), 16);
                }
                else if (i.StartsWith("Object="))
                    Name = i.Split('=')[1];
                else if (i.StartsWith("Debug="))
                    DebugName = i.Split('=')[1];
                else if (i.StartsWith("EndOfFile"))
                {
                    ObjectEntryList.Add(new ObjectEntry()
                    {
                        list = List,
                        type = Type,
                        name = Name != "" ? Name : DebugName != "" ? DebugName : "Unknown/Unused"
                    });
                    break;
                }
                else if (i.Length == 0)
                {
                    ObjectEntryList.Add(new ObjectEntry()
                    {
                        list = List,
                        type = Type,
                        name = Name != "" ? Name : DebugName != "" ? DebugName : "Unknown/Unused"
                    });
                    List = 0;
                    Type = 0;
                    Name = "";
                    DebugName = "";
                }
            }
        }

        public class StageEntry
        {
            public uint flag0;
            public uint flag1;
            public string name;

            public override string ToString()
            {
                return name;
            }
        }

        private void ReadStageListData(string FileName)
        {
            string[] StageListFile = File.ReadAllLines(FileName);

            checkedListBox1.Items.Clear();
            int currentInteger = 0;
            foreach (string i in StageListFile)
            {
                if (i.StartsWith("["))
                    currentInteger = Convert.ToInt32(new string(new char[] { i[1] }));
                else
                    checkedListBox1.Items.Add(new StageEntry()
                    {
                        flag0 = currentInteger == 0 ? Convert.ToUInt32(i.Substring(0, 8), 16) : 0,
                        flag1 = currentInteger == 1 ? Convert.ToUInt32(i.Substring(0, 8), 16) : 0,
                        name = i.Substring(9)
                    });
            }
        }

        private bool srcFind = false;
        private bool tgtFind = false;
        private int srcPos, tgtPos;
        private bool programIsChangingStuff = false;
        private string currentFileName;

        private void openToolStripMenuItem_Click(object sender, EventArgs e)
        {
            OpenFileDialog openFileDialog = new OpenFileDialog()
            {
                Filter = ".bin files|*.bin"
            };
            if (openFileDialog.ShowDialog() == DialogResult.OK)
                LoadTable(openFileDialog.FileName);
        }

        private void saveToolStripMenuItem_Click(object sender, EventArgs e)
        {
            SaveTable(currentFileName);
        }

        private void saveAsToolStripMenuItem_Click(object sender, EventArgs e)
        {
            SaveFileDialog saveFileDialog = new SaveFileDialog
            {
                Filter = ".bin files|*.bin",
                FileName = currentFileName
            };
            if (saveFileDialog.ShowDialog() == DialogResult.OK)
            {
                SaveTable(saveFileDialog.FileName);
            }
        }

        private void aboutToolStripMenuItem_Click(object sender, EventArgs e)
        {
            MessageBox.Show("Shadow SET ID Table Editor release 1 by igorseabra4\nStage CPY modification by dreamsyntax", "About", MessageBoxButtons.OK, MessageBoxIcon.Information);
        }

        private void exitToolStripMenuItem_Click(object sender, EventArgs e)
        {
            Close();
        }

        public class TableEntry
        {
            public ObjectEntry Object;
            public uint values0;
            public uint values1;

            public override string ToString()
            {
                return "[" + Object.list.ToString("X2") + "] [" + Object.type.ToString("X2") + "] " + Object.name;
            }
        }

        private void LoadTable(string fileName)
        {
            currentFileName = fileName;

            BinaryReader TableReader = new BinaryReader(new FileStream(currentFileName, FileMode.Open));

            comboBox1.Items.Clear();

            TableReader.BaseStream.Position = 4;
            int amount = TableReader.ReadInt32();

            for (int i = 0; i < amount; i++)
            {
                TableEntry TemporaryEntry = new TableEntry();

                byte objType = TableReader.ReadByte();
                byte objList = TableReader.ReadByte();
                TableReader.ReadInt16();

                TemporaryEntry.values0 = Switch(TableReader.ReadUInt32());
                TemporaryEntry.values1 = Switch(TableReader.ReadUInt32());

                bool found = false;
                foreach (ObjectEntry j in ObjectEntryList)
                {
                    if (j.list == objList & j.type == objType)
                    {
                        TemporaryEntry.Object = j;
                        found = true;
                        break;
                    }
                }
                if (!found)
                {
                    TemporaryEntry.Object = new ObjectEntry()
                    {
                        type = objType,
                        list = objList,
                        name = "Unknown/Unused"
                    };
                }

                comboBox1.Items.Add(TemporaryEntry);
            }

            saveToolStripMenuItem.Enabled = true;
            saveAsToolStripMenuItem.Enabled = true;
            toolStripStatusLabel1.Text = currentFileName;

            TableReader.Close();
        }

        private void SaveTable(string fileName)
        {
            BinaryWriter TableWriter = new BinaryWriter(new FileStream(fileName, FileMode.Create));

            TableWriter.Write(0);
            TableWriter.Write(comboBox1.Items.Count);

            foreach (TableEntry i in comboBox1.Items)
            {
                TableWriter.Write(i.Object.type);
                TableWriter.Write(i.Object.list);
                TableWriter.Write((short)0);
                TableWriter.Write(Switch(i.values0));
                TableWriter.Write(Switch(i.values1));
            }

            TableWriter.Close();

            currentFileName = fileName;
            toolStripStatusLabel1.Text = currentFileName;
        }

        private void comboBox1_SelectedIndexChanged(object sender, EventArgs e)
        {
            programIsChangingStuff = true;

            for (int i = 0; i < checkedListBox1.Items.Count; i++)
            {
                checkedListBox1.SetItemChecked(i,
                    ((comboBox1.SelectedItem as TableEntry).values0 & (checkedListBox1.Items[i] as StageEntry).flag0) != 0 |
                    ((comboBox1.SelectedItem as TableEntry).values1 & (checkedListBox1.Items[i] as StageEntry).flag1) != 0);
            }

            programIsChangingStuff = false;
        }

        private void checkedListBox1_ItemCheck(object sender, ItemCheckEventArgs e)
        {
            if (!programIsChangingStuff & comboBox1.SelectedIndex > -1)
            {
                if (e.NewValue == CheckState.Checked)
                {
                    (comboBox1.SelectedItem as TableEntry).values0 = (comboBox1.SelectedItem as TableEntry).values0 | (checkedListBox1.Items[e.Index] as StageEntry).flag0;
                    (comboBox1.SelectedItem as TableEntry).values1 = (comboBox1.SelectedItem as TableEntry).values1 | (checkedListBox1.Items[e.Index] as StageEntry).flag1;
                }
                else
                {
                    (comboBox1.SelectedItem as TableEntry).values0 = (comboBox1.SelectedItem as TableEntry).values0 ^ (checkedListBox1.Items[e.Index] as StageEntry).flag0;
                    (comboBox1.SelectedItem as TableEntry).values1 = (comboBox1.SelectedItem as TableEntry).values1 ^ (checkedListBox1.Items[e.Index] as StageEntry).flag1;
                }
            }
        }

        private void cpyButton_Click(object sender, EventArgs e)
        {
            //when sourceLevelText and endLevelText match to an existing stage, set them
            string changes = "";
            for (int i = 0; i < checkedListBox1.Items.Count; i++){
                if(srcFind && tgtFind)
                {
                    MessageBox.Show("src:"+srcPos+"\ntgt:"+tgtPos, "About", MessageBoxButtons.OK, MessageBoxIcon.Information);
                    changes = "Changes Done:";
                    break;
                }
                if (sourceLevelText.Text == checkedListBox1.Items[i].ToString()){
                    srcPos = i;
                    srcFind = true;
                }
                if (targetLevelText.Text == checkedListBox1.Items[i].ToString()){
                    tgtPos = i;
                    tgtFind = true;
                }
            }
            if (changes == "Changes Done:")
            {
                //begin copy source data to target data
                for (int i = 0; i < comboBox1.Items.Count; i++)
                {
                    comboBox1.SelectedIndex = i;
                    bool srcStatus = checkedListBox1.GetItemChecked(srcPos);
                    bool tgtStatus = checkedListBox1.GetItemChecked(tgtPos);
                    if (tgtStatus != srcStatus)
                    {
                        if (checkBoxCPYMode.Checked)
                        {
                            if (srcStatus)
                            {
                                //for CPYMode enabled, only copy when srcStatus is true, do not disable false
                                changes = changes + "\nChanged: " + comboBox1.Items[i].ToString() + " from: " + tgtStatus + " to: " + srcStatus;
                                checkedListBox1.SetItemChecked(tgtPos, srcStatus);
                            }
                        }
                        else
                        {
                            //1:1 copy
                            changes = changes + "\nChanged: " + comboBox1.Items[i].ToString() + " from: " + tgtStatus + " to: " + srcStatus;
                            checkedListBox1.SetItemChecked(tgtPos, srcStatus);
                        }
                    }
                }
                MessageBox.Show(changes); //maybe output to file?
            } else if (sourceLevelText.Text == "source" || targetLevelText.Text == "target"){
                MessageBox.Show("Use CPY to copy all setid data from source stage and replace target stage's check/uncheck for all objects", "StageCopy Info", MessageBoxButtons.OK, MessageBoxIcon.Information);
                MessageBox.Show("Ensure stage name is typed exactly as shown in checkboxes!", "StageCopy Info", MessageBoxButtons.OK, MessageBoxIcon.Information);
            } else {
                MessageBox.Show("Source or Target stage names were not found, Check spelling and try again!", "StageCopy Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }

        }
    }
}
