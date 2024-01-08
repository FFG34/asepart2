using System;
using System.Drawing;
using System.IO;
using System.Windows.Forms;
using static System.Windows.Forms.LinkLabel;

public class CommandParser
{
    private TextBox codeTextBox;
    private PictureBox displayArea;
    private Graphics graphics;
    private Pen currentPen;
    public PointF currentPosition;
    private Bitmap currentDrawing;
    private bool fillEnabled = false;

    // Make properties public so they can be accessed by unit tests
    public PointF CurrentPosition => currentPosition;
    public Pen CurrentPen => currentPen;
    public bool FillEnabled => fillEnabled;
    private float currentLineThickness = 1f;
    private float currentRotationAngle = 0f;
    private Color currentTextColor = Color.Black;
    private Dictionary<string, float> variables = new Dictionary<string, float>();
    private bool isInsideIfBlock = false;
    private bool isInsideLoop = false;
    private List<string> loopBlock = new List<string>();
    private Dictionary<string, List<string>> methods = new Dictionary<string, List<string>>();
    private bool isInsideMethod = false;
    private string currentMethodName = "";
    private int currentLineIndex = 0;
    private readonly object graphicsLock = new object();




    private bool CheckIfCondition(string condition)
    {
        // Check if the condition contains comparison operators
        if (condition.Contains(">") || condition.Contains(">=") || condition.Contains("<") || condition.Contains("<=") || condition.Contains("==") || condition.Contains("!="))
        {
            // Split the condition into its parts (e.g., "x > 3" into "x", ">", "3")
            string[] parts = condition.Split(' ');

            // Ensure that there are three parts: left operand, operator, right operand
            if (parts.Length != 3)
            {
                throw new ArgumentException($"Invalid condition: '{condition}'");
            }

            string leftOperand = parts[0];
            string comparisonOperator = parts[1];
            string rightOperand = parts[2];

            // Retrieve the values of the left and right operands
            float leftValue = ParseFloat(leftOperand);
            float rightValue = ParseFloat(rightOperand);

            // Perform the comparison based on the operator
            switch (comparisonOperator)
            {
                case ">":
                    return leftValue > rightValue;
                case ">=":
                    return leftValue >= rightValue;
                case "<":
                    return leftValue < rightValue;
                case "<=":
                    return leftValue <= rightValue;
                case "==":
                    return leftValue == rightValue;
                case "!=":
                    return leftValue != rightValue;
                default:
                    throw new ArgumentException($"Invalid comparison operator: '{comparisonOperator}'");
            }
        }
        else
        {
            // The condition might be just a variable name, so check if it exists
            if (variables.ContainsKey(condition))
            {
                // Return true if the variable exists and its value is not equal to 0
                return variables[condition] != 0;
            }
            else
            {
                throw new ArgumentException($"Invalid condition: '{condition}'");
            }
        }
    }


    private float GetOperandValue(string operand)
    {
        // Check if the operand is a valid variable name
        if (variables.ContainsKey(operand))
        {
            // If it's a variable, return its value
            return variables[operand];
        }
        else if (float.TryParse(operand, out float value))
        {
            // If it's a numeric value, return it
            return value;
        }
        else
        {
            throw new ArgumentException($"Invalid operand: '{operand}'");
        }
    }


    public class SyntaxErrorException : Exception
    {
        public SyntaxErrorException(string message) : base(message)
        {
        }
    }



    public CommandParser(TextBox codeTextBox, PictureBox displayArea)
    {
        this.codeTextBox = codeTextBox;
        this.displayArea = displayArea;

        // Setup bitmap to draw on
        Bitmap bmp = new Bitmap(displayArea.Width, displayArea.Height);
        displayArea.Image = bmp;
        this.graphics = Graphics.FromImage(bmp);
        this.currentPen = new Pen(Color.Black);
        this.currentPosition = new PointF(0, 0);
    }

    public void ExecuteProgram(string program)
    {
        lock (graphicsLock)
        {
            var lines = codeTextBox.Text.Split(new[] { Environment.NewLine }, StringSplitOptions.RemoveEmptyEntries);
            List<string> currentMethodBody = new List<string>();
            bool skipCommands = false; // This will determine whether to skip commands inside an if block.

            foreach (var line in lines)
            {
                var parts = line.Trim().Split(' ');
                string command = parts[0].ToLower();

                // Handle the start of an if block
                if (command == "if")
                {
                    // Extract the condition and check it
                    string condition = string.Join(" ", parts.Skip(1));
                    isInsideIfBlock = true;
                    skipCommands = !CheckIfCondition(condition); // Determine whether to skip the following commands
                    continue; // Proceed to the next iteration
                }

                // Handle the end of an if block
                if (command == "endif")
                {
                    isInsideIfBlock = false;
                    skipCommands = false; // Stop skipping commands
                    continue; // Proceed to the next iteration
                }

                // If we are inside an if block and the condition is false, skip the command execution
                if (isInsideIfBlock && skipCommands)
                {
                    continue;
                }
                switch (parts[0].ToLower())
                {
                    case "moveto":
                        MoveTo(float.Parse(parts[1]), float.Parse(parts[2]));
                        break;
                    case "drawto":
                        DrawTo(float.Parse(parts[1]), float.Parse(parts[2]));
                        break;
                    case "clear":
                        Clear();
                        break;
                    case "rectangle":
                        DrawRectangle(float.Parse(parts[1]), float.Parse(parts[2]));
                        break;
                    case "circle":
                        float radius;
                        // Check if the argument is a variable, if not try parsing it as a float
                        if (!variables.TryGetValue(parts[1], out radius) && !float.TryParse(parts[1], out radius))
                        {
                            throw new ArgumentException($"Unable to parse '{parts[1]}' as a float or variable.");
                        }
                        DrawCircle(radius);
                        break;
                    case "triangle":
                        DrawTriangle(float.Parse(parts[1]), float.Parse(parts[2]), float.Parse(parts[3]), float.Parse(parts[4]), float.Parse(parts[5]), float.Parse(parts[6]));
                        break;
                    case "color":
                        SetColor(Color.FromName(parts[1]));
                        break;
                    case "reset":
                        ResetPenPosition();
                        break;
                    case "fill":
                        ToggleFill(parts[1]);
                        break;
                    case "linewidth":
                        SetLineThickness(float.Parse(parts[1]));
                        break;
                    case "rotate":
                        Rotate(float.Parse(parts[1]));
                        break;
                    case "text":
                        string textContent = string.Join(" ", parts.Skip(1));
                        DrawText(textContent);
                        break;
                    case "save":
                        SaveDrawing(string.Join(" ", parts.Skip(1)));
                        break;
                    case "load":
                        LoadDrawing(string.Join(" ", parts.Skip(1)));
                        break;
                    case "var":
                        DefineVariable(parts[1], float.Parse(parts[2]));
                        break;
                    case "set":
                        if (parts.Length < 3)
                        {
                            throw new ArgumentException("Invalid set command format. Correct format: set variableName expression");
                        }
                        // Use the entire expression after the variable name
                        string variableName = parts[1];
                        string variableExpression = string.Join(" ", parts.Skip(2));
                        SetVariable(variableName, variableExpression);
                        break;
                    case "if":
                        // Extract the condition part of the if statement correctly
                        string condition = line.Substring(line.IndexOf(' ') + 1);
                        if (CheckIfCondition(condition))
                        {
                            isInsideIfBlock = true;
                        }
                        else
                        {
                            SkipToEndIf();
                        }
                        break;
                    case "endif":
                        isInsideIfBlock = false;
                        break;
                    case "loop":
                        if (!isInsideLoop)
                        {
                            isInsideLoop = true;
                            loopBlock.Clear();
                        }
                        else
                        {
                            throw new InvalidOperationException("Nested loops are not supported.");
                        }
                        break;
                    case "endloop":
                        if (isInsideLoop)
                        {
                            RepeatLoopBlock(loopBlock);
                            isInsideLoop = false;
                        }
                        else
                        {
                            throw new InvalidOperationException("Mismatched endloop statement.");
                        }
                        break;
                    case "method":
                        if (currentMethodBody.Count == 0)
                        {
                            currentMethodBody.Clear();
                            string methodName = parts[1];
                            isInsideMethod = true;
                            currentMethodName = methodName;
                        }
                        else
                        {
                            throw new InvalidOperationException("Nested method definitions are not supported.");
                        }
                        break;
                    case "endmethod":
                        if (isInsideMethod)
                        {
                            DefineMethod(currentMethodName, currentMethodBody);
                            currentMethodBody.Clear();
                            isInsideMethod = false;
                        }
                        else
                        {
                            throw new InvalidOperationException("Mismatched endmethod statement.");
                        }
                        break;
                    default:
                        if (isInsideMethod)
                        {
                            // Add the line to the current method's body
                            currentMethodBody.Add(line);
                        }
                        else if (isInsideLoop)
                        {
                            // Store lines within the loop block
                            loopBlock.Add(line);
                        }
                        else if (IsMethodCall(parts[0]))
                        {
                            CallMethod(parts[0]);
                        }
                        else
                        {
                            ExecuteCommand(line);
                        }
                        break;
                }
            }

            // After executing a command that draws something, refresh the PictureBox
            displayArea.Invalidate();
        }
    }


    public void ExecuteCommand(string commandText)
    {
        string[] lines = commandText.Split(' ');
        switch (lines[0].ToLower())
        {
            case "moveto":
                MoveTo(ParseFloat(lines[1]), ParseFloat(lines[2]));
                break;
            case "drawto":
                DrawTo(ParseFloat(lines[1]), ParseFloat(lines[2]));
                break;
            case "clear":
                Clear();
                break;
            case "rectangle":
                DrawRectangle(ParseFloat(lines[1]), ParseFloat(lines[2]));
                break;
            case "circle":
                DrawCircle(ParseFloat(lines[1]));
                break;
            case "triangle":
                DrawTriangle(ParseFloat(lines[1]), ParseFloat(lines[2]), ParseFloat(lines[3]), ParseFloat(lines[4]), ParseFloat(lines[5]), ParseFloat(lines[6]));
                break;
            case "color":
                SetColor(Color.FromName(lines[1]));
                break;
            case "reset":
                ResetPenPosition();
                break;
            case "fill":
                ToggleFill(lines[1]);
                break;
            default:
                throw new ArgumentException($"Unknown command: {lines[0]}");
        }
        displayArea.Invalidate();
    }

    private float ParseFloat(string input)
    {
        // Trim the input to remove any leading or trailing white spaces
        input = input.Trim();
        if (string.IsNullOrEmpty(input))
        {
            throw new ArgumentException("Input string is null or empty.");
        }
        // Try parsing directly as a float
        if (float.TryParse(input, out float result))
        {
            return result;
        }

        // Handle variables
        if (variables.TryGetValue(input, out result))
        {
            return result;
        }

        // Handle expressions like "x*10"
        string[] parts = input.Split(new char[] { '*', '/', '+', '-' }, StringSplitOptions.RemoveEmptyEntries);
        if (parts.Length == 2)
        {
            float left = GetOperandValue(parts[0].Trim());
            float right = GetOperandValue(parts[1].Trim());

            // Determine the operator used in the expression
            if (input.Contains("*"))
            {
                return left * right;
            }
            // Add additional conditions to handle '/', '+', and '-' if necessary
        }

        throw new ArgumentException($"Unable to parse '{input}' as a float or variable.");
    }

    private void SaveDrawing(string fileName)
    {
        if (currentDrawing != null)
        {
            try
            {
                currentDrawing.Save(fileName + ".png"); // Save as PNG image (you can choose a different format)
            }
            catch (Exception ex)
            {
                MessageBox.Show("Error saving the drawing: " + ex.Message, "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }
    }

    private void LoadDrawing(string fileName)
    {
        try
        {
            var loadedImage = Image.FromFile(fileName);
            currentDrawing = new Bitmap(loadedImage);
            graphics = Graphics.FromImage(currentDrawing);
            displayArea.Image = currentDrawing;
        }
        catch (Exception ex)
        {
            MessageBox.Show("Error loading the image: " + ex.Message, "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
    }


    private float ParseOperand(string operand)
    {
        if (variables.ContainsKey(operand))
        {
            return variables[operand];
        }
        else if (float.TryParse(operand, out float value))
        {
            return value;
        }
        else
        {
            throw new ArgumentException($"Invalid operand: '{operand}'");
        }
    }


    public void SaveProgram(string filePath)
    {
        File.WriteAllText(filePath, codeTextBox.Text);
    }

    public void LoadProgram(string filePath)
    {
        codeTextBox.Text = File.ReadAllText(filePath);
    }

    private void SetLineThickness(float thickness)
    {
        currentLineThickness = thickness;
    }

    private bool IsMethodCall(string command)
    {
        return command.EndsWith("()");
    }


    private void DefineMethod(string methodName, List<string> methodBody)
    {
        if (!methods.ContainsKey(methodName))
        {
            methods[methodName] = methodBody;
        }
        else
        {
            throw new ArgumentException($"Method '{methodName}' is already defined.");
        }
    }

    private void CallMethod(string methodName)
    {
        methodName = methodName.TrimEnd('(', ')');
        if (methods.ContainsKey(methodName))
        {
            List<string> methodBody = methods[methodName];
            foreach (var line in methodBody)
            {
                ExecuteCommand(line);
            }
        }
        else
        {
            throw new ArgumentException($"Method '{methodName}' is not defined.");
        }
    }

    private void RepeatLoopBlock(List<string> loopBlock)
    {
        for (int i = 0; i < 1; i++)
        {
            foreach (var line in loopBlock)
            {
                // Execute the lines within the loop block.
                ExecuteCommand(line);
            }
        }
    }
    private void DrawText(string textContent)
    {
        using (Font font = new Font("Arial", 12))
        using (SolidBrush brush = new SolidBrush(currentTextColor))
        {
            PointF textPosition = new PointF(currentPosition.X, currentPosition.Y);
            graphics.DrawString(textContent, font, brush, textPosition);
        }
    }

    public void DefineVariable(string name, float value)
    {
        if (!variables.ContainsKey(name))
        {
            variables[name] = value;
        }
        else
        {
            variables[name] = value; // Update the value of an existing variable
        }
    }

    private void SetVariable(string name, string expression)
    {
        if (string.IsNullOrWhiteSpace(expression))
        {
            throw new ArgumentException($"Expression for variable '{name}' is null or empty.");
        }

        float value = ParseFloat(expression);
        variables[name] = value;
    }



    private void SkipToEndIf()
    {
        int ifCount = 1; // Track nested if statements

        while (ifCount > 0)
        {
            string line = ReadNextLine();

            if (line == null)
            {
                throw new SyntaxErrorException("Mismatched 'if' and 'endif' statements.");
            }

            // Check if the line contains "if" or "endif" to adjust the ifCount
            if (line.ToLower().Trim() == "if")
            {
                ifCount++;
            }
            else if (line.ToLower().Trim() == "endif")
            {
                ifCount--;
            }
        }
    }

    private string ReadNextLine()
    {
        string[] lines = codeTextBox.Lines;

        if (currentLineIndex < lines.Length)
        {
            string line = lines[currentLineIndex];
            currentLineIndex++;
            return line;
        }
        else
        {
            return null; // Indicates the end of the code
        }
    }
    private void Rotate(float angle)
    {
        currentRotationAngle = angle;
    }

    public void CheckSyntax()
    {
        var commands = codeTextBox.Text.Split(new[] { Environment.NewLine }, StringSplitOptions.RemoveEmptyEntries);
        foreach (var command in commands)
        {
            if (!IsValidCommand(command.Trim()))
            {
                MessageBox.Show($"Syntax error in command: {command}", "Syntax Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }
        }
        MessageBox.Show("All commands have valid syntax.", "Syntax Check", MessageBoxButtons.OK, MessageBoxIcon.Information);
    }
    private bool IsValidCommand(string command)
    {
        var lines = command.Split(' ');
        var commandType = lines[0].ToLower();

        try
        {
            switch (commandType)
            {
                case "moveto":
                case "drawto":
                    return lines.Length == 3 && lines.Skip(1).All(p => float.TryParse(p, out _));
                case "rectangle":
                    return lines.Length == 5 && lines.Skip(1).All(p => float.TryParse(p, out _));
                case "circle":
                    return lines.Length == 2 && float.TryParse(lines[1], out _);
                case "triangle":
                    return lines.Length == 7 && lines.Skip(1).All(p => float.TryParse(p, out _));
                case "color":
                    return lines.Length == 2 && Enum.IsDefined(typeof(KnownColor), lines[1]);
                case "clear":
                case "reset":
                    return lines.Length == 1;
                case "fill":
                    return lines.Length == 2 && (lines[1].ToLower() == "on" || lines[1].ToLower() == "off");
                default:
                    return false;
            }
        }
        catch
        {
            return false;
        }
    }

    private void MoveTo(float x, float y)
    {
        currentPosition = new PointF(x, y);
    }

    public void SaveImage(string filePath)
    {
        if (displayArea.Image != null)
        {
            displayArea.Image.Save(filePath);
        }
    }

    public void LoadImage(string filePath)
    {
        if (File.Exists(filePath))
        {
            displayArea.Image = Image.FromFile(filePath);
            graphics = Graphics.FromImage(displayArea.Image);
        }
    }

    private void DrawTo(float x, float y)
    {
        PointF newPosition = new PointF(x, y);
        graphics.DrawLine(currentPen, currentPosition, newPosition);
        currentPosition = newPosition;
    }

    private void Clear()
    {
        graphics.Clear(Color.White);
        currentPosition = new PointF(0, 0);
    }

    private void DrawRectangle(float width, float height)
    {
        if (fillEnabled)
            graphics.FillRectangle(currentPen.Brush, currentPosition.X, currentPosition.Y, width, height);
        else
            graphics.DrawRectangle(currentPen, currentPosition.X, currentPosition.Y, width, height);
    }

    private void DrawCircle(float radius)
    {
        if (fillEnabled)
            graphics.FillEllipse(currentPen.Brush, currentPosition.X - radius, currentPosition.Y - radius, radius * 2, radius * 2);
        else
            graphics.DrawEllipse(currentPen, currentPosition.X - radius, currentPosition.Y - radius, radius * 2, radius * 2);
    }

    private void DrawTriangle(float x1, float y1, float x2, float y2, float x3, float y3)
    {
        PointF[] points = { new PointF(x1, y1), new PointF(x2, y2), new PointF(x3, y3) };
        if (fillEnabled)
            graphics.FillPolygon(currentPen.Brush, points);
        else
            graphics.DrawPolygon(currentPen, points);
    }

    private void SetColor(Color color)
    {
        currentPen.Color = color;
    }

    private void ResetPenPosition()
    {
        currentPosition = new PointF(0, 0);
    }

    private void ToggleFill(string state)
    {
        fillEnabled = state.ToLower() == "on";
    }

    public void SetupGraphics(PaintEventArgs e)
    {
        var g = e.Graphics;
        g.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.AntiAlias;
        g.PixelOffsetMode = System.Drawing.Drawing2D.PixelOffsetMode.HighQuality;
        g.CompositingQuality = System.Drawing.Drawing2D.CompositingQuality.HighQuality;
    }

    public void Cleanup()
    {
        if (graphics != null)
            graphics.Dispose();
        if (currentPen != null)
            currentPen.Dispose();
    }

}
