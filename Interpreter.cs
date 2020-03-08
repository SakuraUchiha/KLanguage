using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Windows.Forms;
using MultiParse;

namespace KLanguage
{
    public class Interpreter
    {
        private Dictionary<string, dynamic> variables = new Dictionary<string, dynamic>(); //Stores all variables defined in program

        private Random r = new Random(); //Random number generator
        private Expression parser = new Expression(); //Mathematical and logical expression parser

        //Executes Loop's code
        public void ExecuteLoop(string code, int times)
        {
            for (int i = 0; i < times; ++i)
            {
                ExecuteCode(code);
            }
        }

        //Executes command of given name and arguments
        public dynamic ExecuteCommand(string cmd, params dynamic[] args)
        {
            //Defines Loop
            if (cmd.Contains("Loop"))
            {
                //Loop[Print#!a]:n
                //Gets code to repeat as array of lines
                var embeddedCode = cmd.Split('[')[1].Split(']')[0].Split('/');
                //Gets how many times to repeat
                int times = int.Parse(cmd.Split(':')[1]);

                var code = "";

                //Converts array of lines to string
                foreach (var line in embeddedCode)
                {
                    if (line == embeddedCode.First())
                    {
                        code += line;
                        continue;
                    }
                    code += "\n" + line;
                }

                //Execute loop
                ExecuteLoop(code, times);
            }
            //Defines If
            else if (cmd.Contains("If"))
            {
                //Gets condition of If
                var condition = cmd.Split('(')[1].Split(')')[0].Replace(" ", "");
                //Gets code to execute as array of lines
                var embeddedCode = cmd.Split('[')[1].Split(']')[0].Split('/');

                var code = "";

                //Converts array of lines to string
                foreach (var line in embeddedCode)
                {
                    if (line == embeddedCode.First())
                    {
                        code += line;
                        continue;
                    }
                    code += "\n" + line;
                }

                //Checks if condition contains variables and replaces them with their values
                foreach (var name in variables.Keys)
                {
                    if (condition.Contains("var:" + name))
                        condition = condition.Replace("var:" + name, Convert.ToString(variables[name]));
                }

                bool value = Convert.ToBoolean(parser.Evaluate(condition)); //Parses condition and converts result to boolean

                //If structure
                if (value)
                {
                    ExecuteCode(code); //Executes given code
                }
                return value; //Returns result
            }
            //Defines Print function
            else if (cmd.Contains("Print"))
            {
                if (args.Length > 1)
                    throw new Exception("Too many arguments!");

                if (args.Length > 0)
                    Console.WriteLine(Convert.ToString(args[0])); //Writes given text to console
            }
            //Defines Read function
            else if (cmd.Contains("Read") && !cmd.Contains("File"))
            {
                Console.WriteLine("Input: "); 
                return Console.ReadLine(); //Reads input and returns it
            }
            //Defines expression operator
            else if (cmd.Contains("@"))
            {
                var raw = "";

                //Gets expression and stores it in string
                for (int i = 0; i < cmd.Length; ++i)
                {
                    if (cmd[i] == '@' && raw.Length == 0)
                        continue;
                    if (cmd[i] == '@' && raw.Length > 0)
                    {
                        break;
                    }
                    raw += cmd[i];
                }

                //Deletes spaces
                var expression = raw.Replace(" ", "");

                //Replaces any variables with their values
                foreach (var name in variables.Keys)
                {
                    if (expression.Contains(name))
                        expression = expression.Replace(name, Convert.ToString(variables[name]));
                }

                return parser.Evaluate(expression); //Calculates value of expression and returns it
            }
            //Defines variable logic
            else if (cmd.Contains("Variable"))
            {
                var name = args[0]; //Gets name of variable
                var type = args[1]; //Gets type of variable
                dynamic value = null; //Creates non-type variable to store value

                if (args[2] == "&") //Read operator - set value of variable to result of Read function
                {
                    string result = Convert.ToString(ExecuteCommand("Read")); //Ensures that result is string

                    //Converts input to given type of variable
                    if (type == "T")
                        value = Console.ReadLine();
                    else if (type == "N")
                        value = float.Parse(Console.ReadLine());
                    else if (type == "B")
                        value = bool.Parse(Console.ReadLine());
                }
                else if (args[2] == "$") //Random value operator - sets value of variable to random from given type's range
                {
                    if (type == "T")
                        value = (char)r.Next(0, 256); //Sets value to random char
                    else if (type == "N")
                        value = (float)r.Next(int.MinValue, int.MaxValue); //Sets value to random number
                    else if (type == "B")
                        value = (r.Next(0, 2) == 1 ? true : false); //Sets value to true/false
                }
                else if (args[2].Contains("@")) //Expression operator sets value of variable to value of expression
                {
                    if (type != "N")
                        throw new Exception("Wrong type exception!");

                    value = (float)ExecuteCommand(args[2]); //Parses value of expression to number
                }
                else if (args[2].Contains("FileRead")) //Reads from given file and sets value of variable to result
                {
                    if (type != "T")
                        throw new Exception("Wrong type exception!");

                    value = Convert.ToString(ExecuteCommand(args[2])); //Ensures the result is text
                }
                else
                {
                    //Sets value to given argument
                    if (type == "T")
                        value = args[2];
                    else if (type == "N")
                        value = float.Parse(args[2]);
                    else if (type == "B")
                        value = bool.Parse(args[2]);
                }

                //If variable already exists just returns it
                if (variables.ContainsKey(name))
                {
                    variables[name] = value;
                    return variables[name];
                }

                //Creates new variable with given arguments

                var variable = new KeyValuePair<string, dynamic>(name, value);
                variables.Add(variable.Key, variable.Value);

                return variable.Value; //Returns value
            }
            //Creates file at given path
            else if (cmd.Contains("FileCreate"))
            {
                var path = cmd.Split('(')[1].Split(')')[0];
                File.Create(path).Close();
            }
            //Reads from file at given path
            else if (cmd.Contains("FileRead"))
            {
                var path = cmd.Split('(')[1].Split(')')[0];

                return File.ReadAllText(path); //Returns result
            }
            //Writes given text to file at given path
            else if (cmd.Contains("FileWrite"))
            {
                var data = cmd.Split('(')[1].Split(')')[0].Split(';'); //Gets arguments
                var path = data[0];
                var text = data[1];

                //Allows the text to be taken from variable
                if (text.Contains("var:"))
                {
                    text = text.Split(':')[1];

                    //Replaces name of given variable with its value
                    foreach (var name in variables.Keys)
                    {
                        if (text.Contains(name))
                            text = text.Replace(name, Convert.ToString(variables[name]));
                    }
                }

                File.WriteAllText(path, text); //Writes text to file
            }

            return "No Such Command!"; //Unknown instruction
        }

        //This function executes code given in string
        public void ExecuteCode(string code)
        {
            var lines = new List<string>(); //List of lines

            string line = ""; //current line

            //Reads code line by line and saves each line to list
            using (StringReader reader = new StringReader(code))
            {
                while ((line = reader.ReadLine()) != null)
                {
                    lines.Add(line);
                }
            }

            //Iterates through lines to execute code
            foreach (var command in lines)
            {
                dynamic result = null; //Stores return value of executed command

                //Defines logic for Else structure
                if (command.Contains("Else") && !command.Contains("If"))
                {
                    if (command != lines.First() && (lines[lines.IndexOf(command) - 1].Contains("If")))
                    {
                        bool r = Convert.ToBoolean(result); //Converts result of If or Else If above to boolean

                        if (r == false)
                        {
                            var embeddedCode = command.Split('[')[1].Split(']')[0].Split('/'); //Gets code to execute
                            var c = "";

                            //Iterates through each line of code
                            foreach (var l in embeddedCode)
                            {
                                if (l == embeddedCode.First())
                                {
                                    c += l;
                                    continue;
                                }
                                c += "\n" + l;
                            }

                            //Executes code
                            ExecuteCode(c);
                            continue;
                        }
                    }
                }

                //Defines logic for Else If structure
                if (command.Contains("Else If"))
                {
                    if (command != lines.First() && lines[lines.IndexOf(command) - 1].Contains("If"))
                    {
                        bool r = Convert.ToBoolean(result); //Converts result of If or Else If above to boolean

                        if (r == false)
                        {
                            var c = command.Split('(')[1]; //Gets condition of Else If

                            string x = "If("; //Converts Else If to If to simplify code

                            for (int i = 0; i < c.Length; ++i) //Iterates through chars in condition and stores them in new If structure's condition
                                x += c[i];                      

                                result = ExecuteCommand(command); //Executes If structure and stores return value

                            continue;
                        }
                    }
                    continue;
                }

                //Defines logic of If structure
                if (command.Contains("If") && !command.Contains("Else"))
                {
                    result = ExecuteCommand(command); //Stores return value of structure
                    continue;
                }

                //Defines logic of Loop structure
                if (command.Contains("Loop"))
                {
                    ExecuteCommand(command);
                    continue;
                }

                List<string> commands = new List<string>(); //Stores all sub-commands in current line
                var cmd = command.Split('#'); //Gets each sub-command in current line

                //Iterates through each command and adds it to list
                for (int i = 0; i < cmd.Length; ++i)
                    commands.Add(cmd[i]);

                //List is reversed to implement back-executution algorithm(commands are executed from last to first)
                commands.Reverse();

                //Iterates through all sub-commands (instructions)
                foreach (var instruction in commands)
                {
                    //Defines = operator to create and change values and types of variables
                    if (instruction.Contains('='))
                    {
                        var data = instruction.Split('=');
                        var args = data[1].Split(';');

                        result = ExecuteCommand("Variable", data[0], args[0], args[1]);
                        continue;
                    }

                    //Defines ! operator to get values of variables
                    if (instruction.Contains('!'))
                    {
                        //Gets name of variable
                        var name = instruction.Split('!')[1].Split(']')[0];

                        //Checks if variable of given name exists
                        if (variables.ContainsKey(name))
                        {
                            result = variables[name]; //returns value of given variable
                            continue;
                        }
                    }

                    //If instruction is the first(the last in the line) executes and sets result to its return value
                    if (instruction == commands.First())
                    {
                        result = ExecuteCommand(instruction);
                        continue;
                    }
                
                    //Else, executes instruction with result of previous instruction as an argument
                    result = ExecuteCommand(instruction, result);
                }

                //Clears list
                commands.Clear();
            }
        }
    }
}
