using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using NUnit.Framework;
using System.Drawing;

namespace AES351
{
    

    [TestFixture]
    public class CommandParserTests
    {
        private CommandParser parser;

        [SetUp]
        public void Setup()
        {
            // Initialize the CommandParser and other setup steps if needed
            parser = new CommandParser();
        }

        [Test]
        public void TestVariables()
        {
            // Test defining and setting variables
            parser.ExecuteProgram("var x 5");
            Assert.AreEqual(5, parser.GetVariableValue("x"));

            parser.ExecuteProgram("set x 10");
            Assert.AreEqual(10, parser.GetVariableValue("x"));
        }

        [Test]
        public void TestIfStatement()
        {
            // Test if statement and endif
            parser.ExecuteProgram("var x 5");
            parser.ExecuteProgram("if x > 3");
            Assert.IsTrue(parser.IsInsideIfBlock());

            parser.ExecuteProgram("endif");
            Assert.IsFalse(parser.IsInsideIfBlock());
        }

        [Test]
        public void TestLoopCommand()
        {
            // Test loop and endloop
            parser.ExecuteProgram("loop");
            Assert.IsTrue(parser.IsInsideLoop());

            parser.ExecuteProgram("endloop");
            Assert.IsFalse(parser.IsInsideLoop());
        }

        [Test]
        public void TestSyntaxChecking()
        {
            // Test syntax checking with invalid command
            parser.ExecuteProgram("invalid_command");
            Assert.IsTrue(parser.HasSyntaxError());

            // Test syntax checking with valid command
            parser.ExecuteProgram("var x 5");
            Assert.IsFalse(parser.HasSyntaxError());
        }

        [Test]
        public void TestMethods()
        {
            // Test defining and calling methods without parameters
            parser.ExecuteProgram("method MyMethod");
            parser.ExecuteProgram("moveto 100 100");
            parser.ExecuteProgram("endmethod");
            parser.CallMethod("MyMethod");
            Assert.AreEqual(new PointF(100, 100), parser.CurrentPosition);

            // Test defining and calling methods with parameters
            parser.ExecuteProgram("method DrawSquare size");
            parser.ExecuteProgram("rectangle size size");
            parser.ExecuteProgram("endmethod");
            parser.CallMethod("DrawSquare", new List<float> { 50 });
            Assert.AreEqual(new SizeF(50, 50), parser.GetLastDrawnRectangleSize());
        }
    }

