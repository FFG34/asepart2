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
        foreach (var line in lines)
        {
            var parts = line.Trim().Split(' ');
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
                    DrawCircle(float.Parse(parts[1]));
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
                    SaveImage(lines[1]);
                    break;
                case "load":
                    LoadImage(lines[1]);
                    break;
                case "var":
                    DefineVariable(parts[1], float.Parse(parts[2]));
                    break;
                case "set":
                    SetVariable(parts[1], float.Parse(parts[2]));
                    break;
                case "if":
                    if (CheckIfCondition(parts[1]))
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

    public void ExecuteCommand(string command)
    {
        string[] lines = command.Split(' ');
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
        if (variables.ContainsKey(input))
        {
            return variables[input];
        }

        if (float.TryParse(input, out float result))
        {
            return result;
        }

        throw new ArgumentException($"Unable to parse '{input}' as a float or variable.");
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

    private void DefineVariable(string name, float value)
    {
        if (!variables.ContainsKey(name))
        {
            variables[name] = value;
        }
        else
        {
            throw new ArgumentException($"Variable '{name}' is already defined.");
        }
    }

    private void SetVariable(string name, float value)
    {
        if (variables.ContainsKey(name))
        {
            variables[name] = value;
        }
        else
        {
            throw new ArgumentException($"Variable '{name}' is not defined.");
        }
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
