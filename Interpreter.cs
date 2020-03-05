using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace KLanguage
{
    public class Interpreter
    {
        private Random r = new Random();
        private Dictionary<string, dynamic> variables = new Dictionary<string, dynamic>();

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
                //If(condition)[code]

                var condition = "";
                var embeddedCode = cmd.Split('[')[1].Split(']')[0].Split('/');

                int i = cmd.ToCharArray().ToList().IndexOf('(') + 1;
                var currentChar = ' ';

                while ((currentChar = cmd[i]) != ')' && i < cmd.Length)
                {
                    condition += currentChar;
                    ++i;
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

                condition = condition.Replace(" ", "");

                var name = "";
                var current = new List<float>();
                var op = ' ';

                for (int j = condition.Length - 1; j >= 0; --j)
                {
                    var x = condition[j];

                    if (x == '=' || x == '<' || x == '>' || x == '!')
                    {
                        if (current.Count == 0)
                        {
                            op = x;
                        }

                        current.Add((float)variables[name]);
                        name = "";

                        continue;
                    }

                    name += x;
                }

                current.Add((float)variables[name]);

                if (current.Count == 2)
                {
                    current.Reverse();

                    if (op == '=')
                    {
                        if (current[0] == current[1])
                            ExecuteCode(code);
                    }
                    else if (op == '<')
                    {
                        if (current[0] < current[1])
                        {
                            ExecuteCode(code);
                        }
                    }
                    else if (op == '>')
                    {
                        if (current[0] > current[1])
                            ExecuteCode(code);
                    }
                    else if (op == '!')
                    {
                        if (current[0] != current[1])
                            ExecuteCode(code);
                    }
                }
            }
            else if (cmd.Contains("Print"))
            {
                if (args.Length > 1)
                    throw new Exception("Too many arguments!");

                if (args.Length > 0)
                    Console.WriteLine(args[0]);
            }
            else if (cmd.Contains("Read"))
            {
                Console.WriteLine("Input: ");
                return Console.ReadLine();
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
                if (command.Contains("If"))
                {
                    ExecuteCommand(command);
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

                dynamic result = null;

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
                        if (instruction.Contains(']'))
                        {
                            var x = instruction.Split('!')[1].Split(']')[0];
                            result = variables[x];
                            continue;
                        }
                        else
                        {
                            var x = instruction.Split('!')[1];
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
