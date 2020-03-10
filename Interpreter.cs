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
        private Dictionary<string, dynamic> constants = new Dictionary<string, dynamic>(); //Stores all constants defined in program
        private Dictionary<string, string> functions = new Dictionary<string, string>(); //Stores all functions
        private Dictionary<string[], dynamic> methods = new Dictionary<string[], dynamic>(); //Stores all methods

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

        public void ExecuteLoop(string code, string condition)
        {
            var c = condition;

            foreach (var variable in variables)
            {
                if (c.Contains("var:" + variable.Key))
                    c = c.Replace("var:" + variable.Key, Convert.ToString(variable.Value));
            }

            bool value = Convert.ToBoolean(parser.Evaluate(c));

            while (value == true)
            {
                ExecuteCode(code);

                c = condition;

                foreach (var variable in variables)
                {
                    if (c.Contains("var:" + variable.Key))
                        c = c.Replace("var:" + variable.Key, Convert.ToString(variable.Value));
                }

                value = Convert.ToBoolean(parser.Evaluate(c));
            }
        }

        //Executes command of given name and arguments
        public dynamic ExecuteCommand(string cmd, params dynamic[] args)
        {
            //Defines Function
            if (cmd.Contains("Function"))
            {
                var name = (string)Convert.ToString(args[0]);
                var embeddedCode = ((string)Convert.ToString(args[1])).Split('[')[1].Split(']')[0].Split('/');

                var code = "";

                foreach (var line in embeddedCode)
                {
                    if (line == embeddedCode.First())
                    {
                        code += line;
                        continue;
                    }

                    code += "\n" + line;
                }

                functions.Add(name, code);
            }
            //Defines Method
            else if (cmd.Contains("Method"))
            {
                var type = (string)Convert.ToString(args[0]);
                var name = (string)Convert.ToString(args[1]);
                var embeddedCode = ((string)Convert.ToString(args[2])).Split('[')[1].Split(']')[0].Split('/');
                var returnValueRaw = ((string)Convert.ToString(args[2]));

                var c = returnValueRaw.ToCharArray();

                bool add = false;

                for (int i = 0; i < c.Length; ++i)
                {
                    var x = c[i];

                    if (x == '=' && c[i - 1] == ']')
                    {
                        add = true;
                        returnValueRaw = "";
                        continue;
                    }

                    if (add)
                    {
                        returnValueRaw += x;
                    }
                }

                dynamic returnValue = null;

                if (returnValueRaw.Contains("var:"))
                {
                    returnValue = returnValueRaw;
                }
                else
                {
                    if (type == "N")
                        returnValue = float.Parse(returnValueRaw);
                    else if (type == "T")
                        returnValue = returnValueRaw;
                    else if (type == "B")
                        returnValue = bool.Parse(returnValueRaw);
                }

                var code = "";

                foreach (var line in embeddedCode)
                {
                    if (line == embeddedCode.First())
                    {
                        code += line;
                        continue;
                    }

                    code += "\n" + line;
                }

                string[] key = new string[] { type, name, code };

                if (!methods.ContainsKey(key))
                {
                    methods.Add(key, returnValue);
                }
            }
            //Defines Loop
            else if (cmd.Contains("Loop"))
            {
                //Loop[Print#!a]:n
                //Gets code to repeat as array of lines
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
                //Gets how many times to repeat or a condition
                var timesRaw = cmd.Split(':')[1];

                if (timesRaw.Contains('('))
                {
                    int i = 2;

                    while (timesRaw.Contains("var") && i < cmd.Split(':').Length)
                    {
                        timesRaw += ":" + cmd.Split(':')[i];
                        ++i;
                    }

                    var condition = timesRaw.Split('(')[1].Split(')')[0].Replace(" ", "");

                    ExecuteLoop(code, condition);
                }
                else
                {
                    if (timesRaw.Contains("var"))
                    {
                        timesRaw = cmd.Split(':')[2];
                    }

                    foreach (var variable in variables)
                    {
                        if (timesRaw.Contains(variable.Key))
                        {
                            timesRaw = timesRaw.Replace(variable.Key, Convert.ToString(variable.Value));
                            break;
                        }
                    }

                    int times = int.Parse(timesRaw);

                    //Execute loop
                    ExecuteLoop(code, times);
                }
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

                //Replaces any variables with their values
                foreach (var name in variables.Keys)
                {
                    float x = 0;

                    if (raw.Contains("var:" + name))
                    {
                        if (float.TryParse(Convert.ToString(variables[name]), out x))
                            raw = raw.Replace("var:" + name, x + "f");

                        raw = raw.Replace("var:" + name, Convert.ToString(variables[name]));
                    }
                }

                var expression = raw.Replace(" ", "");

                return Convert.ToString(parser.Evaluate(expression)); //Calculates value of expression and returns it
            }
            //Defines variable logic
            else if (cmd.Contains("Variable"))
            {
                var name = args[0]; //Gets name of variable
                var type = args[1]; //Gets type of variable
                dynamic value = null; //Creates non-type variable to store value

                if (type == "const")
                {
                    var t = args[2];

                    if (t == "N")
                        value = double.Parse(args[3]);
                    else if (t == "T")
                        value = args[3];
                    else if (t == "B")
                        value = bool.Parse(args[3]);

                    if (constants.ContainsKey(name))
                    {
                        throw new Exception("You are either trying to create constant that already exists or change value of the constant!");
                    }

                    constants.Add(name, value);

                    return null;
                }

                if (constants.ContainsKey(name))
                    throw new Exception("You are either trying to create constant that already exists or change value of the constant!");

                if (args[2] == "&") //Read operator - set value of variable to result of Read function
                {
                    string result = Convert.ToString(ExecuteCommand("Read")); //Ensures that result is string

                    //Converts input to given type of variable
                    if (type == "T")
                        value = result;
                    else if (type == "N")
                        value = float.Parse(result);
                    else if (type == "B")
                        value = bool.Parse(result);
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

                    value = float.Parse(ExecuteCommand(args[2])); //Parses value of expression to number
                }
                else if (args[2].Contains("FileRead")) //Reads from given file and sets value of variable to result
                {
                    if (type != "T")
                        throw new Exception("Wrong type exception!");

                    value = Convert.ToString(ExecuteCommand(args[2])); //Ensures the result is text
                }
                else if (args[2].Contains("!"))
                {
                    foreach (var method in methods)
                    {
                        if (args[2].Contains(method.Key[1]) && type == method.Key[0])
                        {
                            ExecuteCode(method.Key[2]);

                            string result = Convert.ToString(method.Value);
                            string typeF = Convert.ToString(method.Key[0]);

                            if (result.Contains("var:"))
                            {
                                foreach (var var in variables)
                                {
                                    if (result.Split(':')[1].Contains(var.Key))
                                    {
                                        result = result.Split(':')[1].Replace(var.Key, Convert.ToString(var.Value));
                                        break;
                                    }
                                }
                            }

                            if (type == "N")
                                value = float.Parse(result);
                            else if (type == "T")
                                value = result;
                            else if (type == "B")
                                value = bool.Parse(result);
                        }
                    }
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

                if (command.Contains("Import"))
                {
                    var name = command.TrimStart("Import ".ToCharArray());

                    var path = String.Format("{0}/Libraries/{1}.k", Path.GetDirectoryName(Application.ExecutablePath), name);

                    if (!File.Exists(path))
                        throw new Exception("You're trying to import library that doesn't exist!");

                    var library = File.ReadAllText(path);

                    ExecuteCode(library);
                    continue;
                }

                if (command.Contains("Function"))
                {
                    var raw = command.ToCharArray();

                    var name = "";
                    var current = "";

                    var args = new List<string>();

                    for(int i = 0; i < raw.Length; ++i)
                    {
                        var x = raw[i];

                        if (x == '#' && name == "")
                        {
                            name = current;
                            current = "";
                            continue;
                        }

                        if (x == ';' && args.Count < 1)
                        {
                            args.Add(current);
                            current = "";
                            continue;
                        }

                        current += x;
                    }
                    args.Add(current);

                    ExecuteCommand(name, args.ToArray());
                    continue;
                }
                else if (command.Contains("Method"))
                {
                    var raw = command.ToCharArray();

                    var name = "";
                    var current = "";

                    var args = new List<string>();

                    for (int i = 0; i < raw.Length; ++i)
                    {
                        var x = raw[i];

                        if (x == '#' && name == "")
                        {
                            name = current;
                            current = "";
                            continue;
                        }

                        if (x == ';' && args.Count < 2)
                        {
                            args.Add(current);
                            current = "";
                            continue;
                        }

                        current += x;
                    }
                    args.Add(current);

                    ExecuteCommand(name, args.ToArray());
                    continue;
                }
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

                        if (args.Length == 2)
                            result = ExecuteCommand("Variable", data[0], args[0], args[1]);
                        else if (args.Length == 3)
                            result = ExecuteCommand("Variable", data[0], args[0], args[1], args[2]);
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
                        else if (constants.ContainsKey(name))
                        {
                            result = constants[name]; //returns value of given constant
                            continue;
                        }
                        else if (methods.Keys.ToList().Find(x => x[1] == name) != null)
                        {
                            int i = methods.ToList().FindIndex(x => x.Key[1] == name);
                            ExecuteCode(methods.ElementAt(i).Key[2]);

                            string value = Convert.ToString(methods.ElementAt(i).Value);
                            string type = Convert.ToString(methods.ElementAt(i).Key[0]);

                            if (value.Contains("var:"))
                            {
                                foreach (var variable in variables)
                                {
                                    if (value.Split(':')[1].Contains(variable.Key))
                                    {
                                        value = value.Split(':')[1].Replace(variable.Key, Convert.ToString(variable.Value));
                                        break;
                                    }
                                }
                            }

                            if (type == "N")
                                result = float.Parse(value);
                            else if (type == "T")
                                result = value;
                            else if (type == "B")
                                result = bool.Parse(value);

                            continue;
                        }
                        else if (functions.ContainsKey(name))
                        {
                            ExecuteCode(functions[name]);
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
