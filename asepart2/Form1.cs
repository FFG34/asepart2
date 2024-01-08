using System;
using System.Drawing;
using System.Windows.Forms;

namespace asepart2
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
                string commandText = commandTextBox.Text.Trim();
                if (string.IsNullOrEmpty(commandText))
                {
                    MessageBox.Show("Please enter a command.", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
                else
                {
                    try
                    {
                        parser.ExecuteCommand(commandText);
                        displayArea.Invalidate(); // Refresh the canvas after executing the command
                    }
                    catch (Exception ex)
                    {
                        MessageBox.Show("Error: " + ex.Message, "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    }
                    // Removed the clear command to keep the text in the box
                }
                // Prevent the event from bubbling up to other key event handlers
                e.SuppressKeyPress = true;
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
