using System;
using System.Drawing;
using System.Windows.Forms;

namespace AES351
{
    public partial class Form1 : Form
    {
        private CommandParser parser;

        public Form1()
        {
            InitializeComponent();
            parser = new CommandParser(codeTextBox, displayArea);
            this.FormClosing += new FormClosingEventHandler(Form1_FormClosing);
            displayArea.Paint += new PaintEventHandler(displayArea_Paint);
        }

        private void Form1_Load(object sender, EventArgs e)
        {
            // Initialization that occurs when the form loads can be placed here.
        }

        private void RunButton_Click(object sender, EventArgs e)
        {
            try
            {
                string program = codeTextBox.Text;
                parser.ExecuteProgram(program);
            }
            catch (Exception ex)
            {
                MessageBox.Show("Error executing the program: " + ex.Message, "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void SyntaxButton_Click(object sender, EventArgs e)
        {
            try
            {
                parser.CheckSyntax();
            }
            catch (Exception ex)
            {
                MessageBox.Show("Syntax error: " + ex.Message, "Syntax Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void SaveButton_Click(object sender, EventArgs e)
        {
            using (SaveFileDialog saveFileDialog = new SaveFileDialog())
            {
                saveFileDialog.Filter = "Text Files (*.txt)|*.txt";
                saveFileDialog.DefaultExt = "txt";
                saveFileDialog.AddExtension = true;

                if (saveFileDialog.ShowDialog() == DialogResult.OK)
                {
                    parser.SaveProgram(saveFileDialog.FileName);
                }
            }
        }

        private void LoadButton_Click(object sender, EventArgs e)
        {
            using (OpenFileDialog openFileDialog = new OpenFileDialog())
            {
                openFileDialog.Filter = "Text Files (*.txt)|*.txt";
                openFileDialog.DefaultExt = "txt";
                openFileDialog.AddExtension = true;

                if (openFileDialog.ShowDialog() == DialogResult.OK)
                {
                    parser.LoadProgram(openFileDialog.FileName);
                }
            }
        }

        private void commandTextBox_KeyUp(object sender, KeyEventArgs e)
        {
            if (e.KeyCode == Keys.Enter)
            {
                var commandText = commandTextBox.Text;
                try
                {
                    parser.ExecuteCommand(commandText);
                    commandTextBox.Clear();
                }
                catch (Exception ex)
                {
                    MessageBox.Show("Error executing command: " + ex.Message, "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
            }
        }

        private void displayArea_Paint(object sender, PaintEventArgs e)
        {
            parser.SetupGraphics(e);
        }

        private void Form1_FormClosing(object sender, FormClosingEventArgs e)
        {
            parser.Cleanup();
        }
    }
}
