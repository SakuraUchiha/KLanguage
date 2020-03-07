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
        private Dictionary<string, dynamic> variables = new Dictionary<string, dynamic>();

        private Random r = new Random();
        private Expression parser = new Expression();

        public void ExecuteLoop(string code, int times)
        {
            for (int i = 0; i < times; ++i)
            {
                ExecuteCode(code);
            }
        }

        public dynamic ExecuteCommand(string cmd, params dynamic[] args)
        {
            if (cmd.Contains("Loop"))
            {
                //Loop[Print#!a]:n
                var embeddedCode = cmd.Split('[')[1].Split(']')[0].Split('/');
                int times = int.Parse(cmd.Split(':')[1]);

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

                ExecuteLoop(code, times);
            }
            else if (cmd.Contains("If"))
            {
                var condition = cmd.Split('(')[1].Split(')')[0].Replace(" ", "");
                var embeddedCode = cmd.Split('[')[1].Split(']')[0].Split('/');

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

                foreach (var name in variables.Keys)
                {
                    if (condition.Contains("var:" + name))
                        condition = condition.Replace("var:" + name, Convert.ToString(variables[name]));
                }

                bool value = Convert.ToBoolean(parser.Evaluate(condition));

                if (value)
                {
                    ExecuteCode(code);
                }
                return value;
            }
            else if (cmd.Contains("Print"))
            {
                if (args.Length > 1)
                    throw new Exception("Too many arguments!");

                if (args.Length > 0)
                    Console.WriteLine(Convert.ToString(args[0]));
            }
            else if (cmd.Contains("Read") && !cmd.Contains("File"))
            {
                Console.WriteLine("Input: ");
                return Console.ReadLine();
            }
            else if (cmd.Contains("@"))
            {
                var raw = "";

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

                var expression = raw.Replace(" ", "");

                foreach (var name in variables.Keys)
                {
                    if (expression.Contains(name))
                        expression = expression.Replace(name, Convert.ToString(variables[name]));
                }

                return parser.Evaluate(expression);
            }
            else if (cmd.Contains("Variable"))
            {
                var name = args[0];
                var type = args[1];
                dynamic value = null;

                if (args[2] == "&")
                {
                    Console.WriteLine("Input:");

                    if (type == "T")
                        value = Console.ReadLine();
                    else if (type == "N")
                        value = float.Parse(Console.ReadLine());
                    else if (type == "B")
                        value = bool.Parse(Console.ReadLine());
                }
                else if (args[2] == "$")
                {
                    if (type == "T")
                        value = (char)r.Next(-255, 256);
                    else if (type == "N")
                        value = (float)r.Next(int.MinValue, int.MaxValue);
                    else if (type == "B")
                        value = r.Next(0, 2) == 1 ? true : false;
                }
                else if (args[2].Contains("@"))
                {
                    if (type != "N")
                        throw new Exception("Wrong type exception!");

                    value = (float)ExecuteCommand(args[2]);
                }
                else if (args[2].Contains("FileRead"))
                {
                    if (type != "T")
                        throw new Exception("Wrong type exception!");

                    value = Convert.ToString(ExecuteCommand(args[2]));
                }
                else
                {
                    if (type == "T")
                        value = args[2];
                    else if (type == "N")
                        value = float.Parse(args[2]);
                    else if (type == "B")
                        value = bool.Parse(args[2]);
                }

                if (variables.ContainsKey(name))
                {
                    variables[name] = value;
                    return variables[name];
                }

                var variable = new KeyValuePair<string, dynamic>(name, value);
                variables.Add(variable.Key, variable.Value);

                return variable.Value;
            }
            else if (cmd.Contains("FileCreate"))
            {
                var path = cmd.Split('(')[1].Split(')')[0];
                File.Create(path).Close();
            }
            else if (cmd.Contains("FileRead"))
            {
                var path = cmd.Split('(')[1].Split(')')[0];

                return File.ReadAllText(path);
            }
            else if (cmd.Contains("FileWrite"))
            {
                var data = cmd.Split('(')[1].Split(')')[0].Split(';');
                var path = data[0];
                var text = data[1];

                if (text.Contains("var:"))
                {
                    text = text.Split(':')[1];

                    foreach (var name in variables.Keys)
                    {
                        if (text.Contains(name))
                            text = text.Replace(name, Convert.ToString(variables[name]));
                    }
                }

                File.WriteAllText(path, text);
            }

            return "No Such Command!";
        }

        public void ExecuteCode(string code)
        {
            var lines = new List<string>();

            string line = "";

            using (StringReader reader = new StringReader(code))
            {
                while ((line = reader.ReadLine()) != null)
                {
                    lines.Add(line);
                }
            }

            foreach (var command in lines)
            {
                dynamic result = null;

                if (command.Contains("Else") && !command.Contains("If"))
                {
                    if (command != lines.First() && (lines[lines.IndexOf(command) - 1].Contains("If")))
                    {
                        bool r = Convert.ToBoolean(result);

                        if (r == false)
                        {
                            var embeddedCode = command.Split('[')[1].Split(']')[0].Split('/');
                            var c = "";

                            foreach (var l in embeddedCode)
                            {
                                if (l == embeddedCode.First())
                                {
                                    c += l;
                                    continue;
                                }
                                c += "\n" + l;
                            }

                            ExecuteCode(c);
                            continue;
                        }
                    }
                }

                if (command.Contains("Else If"))
                {
                    if (command != lines.First() && lines[lines.IndexOf(command) - 1].Contains("If"))
                    {
                        bool r = Convert.ToBoolean(result);

                        if (r == false)
                        {
                            var c = command.Split('(')[1];

                            string x = "If(";

                            for (int i = 0; i < c.Length; ++i)
                                x += c[i];

                            result = ExecuteCommand(command);

                            continue;
                        }
                    }
                    continue;
                }

                if (command.Contains("If") && !command.Contains("Else"))
                {
                    result = ExecuteCommand(command);
                    continue;
                }

                if (command.Contains("Loop"))
                {
                    ExecuteCommand(command);
                    continue;
                }

                List<string> commands = new List<string>();
                var cmd = command.Split('#');

                for (int i = 0; i < cmd.Length; ++i)
                    commands.Add(cmd[i]);

                commands.Reverse();

                foreach (var instruction in commands)
                {
                    if (instruction.Contains('='))
                    {
                        var data = instruction.Split('=');
                        var args = data[1].Split(';');

                        result = ExecuteCommand("Variable", data[0], args[0], args[1]);
                        continue;
                    }

                    if (instruction.Contains('!'))
                    {
                        var x = instruction.Split('!')[1].Split(']')[0];

                        if (variables.ContainsKey(x))
                        {
                            result = variables[x];
                            continue;
                        }
                    }

                    if (instruction == commands.First())
                    {
                        result = ExecuteCommand(instruction);
                        continue;
                    }
                
                    result = ExecuteCommand(instruction, result);
                }

                commands.Clear();
            }
        }
    }
}
