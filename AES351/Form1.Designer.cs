namespace AES351
{
    partial class Form1
    {
        /// <summary>
        ///  Required designer variable.
        /// </summary>
        private System.ComponentModel.IContainer components = null;

        /// <summary>
        ///  Clean up any resources being used.
        /// </summary>
        /// <param name="disposing">true if managed resources should be disposed; otherwise, false.</param>
        protected override void Dispose(bool disposing)
        {
            if (disposing && (components != null))
            {
                components.Dispose();
            }
            base.Dispose(disposing);
        }

        #region Windows Form Designer generated code

        /// <summary>
        ///  Required method for Designer support - do not modify
        ///  the contents of this method with the code editor.
        /// </summary>
        private void InitializeComponent()
        {
            runButton = new Button();
            syntaxButton = new Button();
            codeTextBox = new TextBox();
            commandTextBox = new TextBox();
            displayArea = new PictureBox();
            loadButton = new Button();
            saveButton = new Button();
            ((System.ComponentModel.ISupportInitialize)displayArea).BeginInit();
            SuspendLayout();
            // 
            // runButton
            // 
            runButton.Location = new Point(294, 579);
            runButton.Name = "runButton";
            runButton.Size = new Size(75, 39);
            runButton.TabIndex = 0;
            runButton.Text = "Run";
            runButton.Click += RunButton_Click;
            // 
            // syntaxButton
            // 
            syntaxButton.Location = new Point(890, 579);
            syntaxButton.Name = "syntaxButton";
            syntaxButton.Size = new Size(75, 39);
            syntaxButton.TabIndex = 1;
            syntaxButton.Text = "Syntax";
            syntaxButton.Click += SyntaxButton_Click;
            // 
            // codeTextBox
            // 
            codeTextBox.Location = new Point(10, 10);
            codeTextBox.Multiline = true;
            codeTextBox.Name = "codeTextBox";
            codeTextBox.Size = new Size(481, 488);
            codeTextBox.TabIndex = 2;
            // 
            // commandTextBox
            // 
            commandTextBox.Location = new Point(68, 520);
            commandTextBox.Multiline = true;
            commandTextBox.Name = "commandTextBox";
            commandTextBox.Size = new Size(880, 30);
            commandTextBox.TabIndex = 3;
            commandTextBox.KeyUp += commandTextBox_KeyUp;
            // 
            // displayArea
            // 
            displayArea.BackColor = Color.Gray;
            displayArea.BorderStyle = BorderStyle.FixedSingle;
            displayArea.Location = new Point(497, 10);
            displayArea.Name = "displayArea";
            displayArea.Size = new Size(495, 488);
            displayArea.TabIndex = 4;
            displayArea.TabStop = false;
            displayArea.Paint += displayArea_Paint;
            // 
            // loadButton
            // 
            loadButton.Location = new Point(129, 579);
            loadButton.Name = "loadButton";
            loadButton.Size = new Size(75, 39);
            loadButton.TabIndex = 2;
            loadButton.Text = "Load";
            loadButton.UseVisualStyleBackColor = true;
            loadButton.Click += LoadButton_Click;
            // 
            // saveButton
            // 
            saveButton.Location = new Point(736, 579);
            saveButton.Name = "saveButton";
            saveButton.Size = new Size(75, 39);
            saveButton.TabIndex = 3;
            saveButton.Text = "Save";
            saveButton.UseVisualStyleBackColor = true;
            saveButton.Click += SaveButton_Click;
            // 
            // Form1
            // 
            AutoScaleDimensions = new SizeF(10F, 25F);
            AutoScaleMode = AutoScaleMode.Font;
            ClientSize = new Size(1004, 638);
            Controls.Add(runButton);
            Controls.Add(syntaxButton);
            Controls.Add(codeTextBox);
            Controls.Add(commandTextBox);
            Controls.Add(saveButton);
            Controls.Add(loadButton);
            Controls.Add(displayArea);
            Name = "Form1";
            Text = "Graphical Programming Language";
            Load += Form1_Load;
            ((System.ComponentModel.ISupportInitialize)displayArea).EndInit();
            ResumeLayout(false);
            PerformLayout();
        }

        private System.Windows.Forms.Button runButton;
        private System.Windows.Forms.Button syntaxButton;
        private System.Windows.Forms.Button loadButton;
        private System.Windows.Forms.Button saveButton;
        private System.Windows.Forms.TextBox codeTextBox;
        private System.Windows.Forms.TextBox commandTextBox;
        private System.Windows.Forms.PictureBox displayArea;


        #endregion
    }
}
