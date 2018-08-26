﻿using System;
using System.Collections.Generic;
using System.Linq;

namespace DopaScript
{
    public class Interpreter
    {
        Program _program;

        Value[] _globalVariables;
        List<Value> _heap;
        Function _currentFunction;

        public delegate Value FunctionDelegate(List<Value> values);
        Dictionary<string, FunctionDelegate> _embededFunctions;
        EmbededLibrary _embededLibrary;

        public Interpreter()
        {
            _embededFunctions = new Dictionary<string, FunctionDelegate>();
            _embededLibrary = new EmbededLibrary();
            _embededFunctions = _embededLibrary.EmbededFunctions;

            InstructionExecutors.Add(typeof(InstructionAssignment), ExecuteInstructionAssignment);
            InstructionExecutors.Add(typeof(InstructionValue), ExecuteInstructionValue);
            InstructionExecutors.Add(typeof(InstructionFunction), ExecuteInstructionFunction);
            InstructionExecutors.Add(typeof(InstructionVariableValue), ExecuteInstructionVariableValue);
            InstructionExecutors.Add(typeof(InstructionReturn), ExecuteInstructionReturn);
            InstructionExecutors.Add(typeof(InstructionOperation), ExecuteInstructionOperation);
        }

        public void AddFunction(string name, FunctionDelegate function)
        {
            _embededFunctions.Add(name, function);
        }

        public void Parse(string source)
        {
            Tokenizer tokenizer = new Tokenizer();
            var tokens = tokenizer.Tokenize(source);

            SyntaxAnalyser syntaxAnalyser = new SyntaxAnalyser();
            _program = syntaxAnalyser.Analyse(tokens);
        }

        public void Execute()
        {
            _globalVariables = new Value[_program.Variables.Count];
            for (int i = 0; i < _globalVariables.Length; i++)
            {
                _globalVariables[i] = new Value();
            }
            _heap = new List<Value>();

            foreach (Instruction instruction in _program.Instructions)
            {
                ExecuteInstruction(instruction);
            }
        }

        class InstructionResult
        {
            public InstructionResult()
            {
                Return = false;
            }

            public Value Value { get; set; }
            public bool Return { get; set; }
        }

        InstructionResult ExecuteInstruction(Instruction instruction)
        {
            Type type = instruction.GetType();
            return InstructionExecutors[type](instruction);
        }

        delegate InstructionResult ExecuteInstructionDelegate(Instruction instruction);
        Dictionary<Type, ExecuteInstructionDelegate> InstructionExecutors = new Dictionary<Type, ExecuteInstructionDelegate>();

        InstructionResult ExecuteInstructionAssignment(Instruction instruction)
        {
            InstructionAssignment instructionAssignment = instruction as InstructionAssignment;

            Variable variable = GetVariableByName(instructionAssignment.VariableName);
            Value value = ExecuteInstruction(instructionAssignment.Instruction).Value;

            Value variableValue = GetVariableValue(variable);
            CopyValue(ref variableValue, value);

            return null;
        }

        InstructionResult ExecuteInstructionValue(Instruction instruction)
        {
            InstructionValue instructionValue = instruction as InstructionValue;

            return new InstructionResult()
            {
                Value = instructionValue.Value
            };
        }

        InstructionResult ExecuteInstructionFunction(Instruction instruction)
        {
            InstructionResult result = new InstructionResult();

            InstructionFunction instructionFunction = instruction as InstructionFunction;

            List<Value> values = new List<Value>();
            foreach (var parameter in instructionFunction.Parameters)
            {
                Value value = CopyValue(ExecuteInstruction(parameter).Value);
                values.Add(value);
            }

            if (_embededFunctions.ContainsKey(instructionFunction.FunctionName))
            {
                result.Value = _embededFunctions[instructionFunction.FunctionName](values);
            }
            else
            {
                Function previousFunction = _currentFunction;
                _currentFunction = _program.Functions.First(f => f.Name == instructionFunction.FunctionName);

                _heap.AddRange(values);
                int numberOfValueToAdd = _currentFunction.Variables.Count - _currentFunction.ParametersCount;
                for (int i = 0;i < numberOfValueToAdd;i++)
                {
                    _heap.Add(new Value());
                }

                foreach (var functionInstruction in _currentFunction.Instructions)
                {
                    InstructionResult r = ExecuteInstruction(functionInstruction);
                    if(r != null && r.Return)
                    {
                        result.Value = r.Value;
                    }
                }

                _currentFunction = previousFunction;
            }

            _heap.RemoveRange(_heap.Count - instructionFunction.Parameters.Count, instructionFunction.Parameters.Count);

            return result;
        }

        InstructionResult ExecuteInstructionVariableValue(Instruction instruction)
        {
            InstructionVariableValue instructionVariableValue = instruction as InstructionVariableValue;

            Variable variable = GetVariableByName(instructionVariableValue.VariableName);
            Value value = GetVariableValue(variable);

            return new InstructionResult()
            {
                Value = value
            };
        }

        InstructionResult ExecuteInstructionReturn(Instruction instruction)
        {
            InstructionReturn instructionVariableValue = instruction as InstructionReturn;

            Value value = null;
            if (instructionVariableValue.ValueInstruction != null)
            {
                value = ExecuteInstruction(instructionVariableValue.ValueInstruction).Value;
            }

            return new InstructionResult()
            {
                Value = value,
                Return = true
            };
        }

        InstructionResult ExecuteInstructionOperation(Instruction instruction)
        {
            InstructionOperation instructionOperation = instruction as InstructionOperation;

            List<Value> values = new List<Value>();
            foreach(Instruction valueInstruction in instructionOperation.ValuesInstructions)
            {
                InstructionResult r = ExecuteInstruction(valueInstruction);
                values.Add(r.Value);
            }

            Value leftValue = CopyValue(values.First());
            for(int i = 0;i < instructionOperation.Operators.Count;i++)
            {
                InstructionOperation.OperatorType ope = instructionOperation.Operators[i];
                Value rightValue = values[i + 1];
                switch (ope)
                {
                    case InstructionOperation.OperatorType.Addition:
                        leftValue.NumericValue = leftValue.NumericValue + rightValue.NumericValue;
                        break;
                    case InstructionOperation.OperatorType.Substraction:
                        leftValue.NumericValue = leftValue.NumericValue - rightValue.NumericValue;
                        break;
                    case InstructionOperation.OperatorType.Multiplication:
                        leftValue.NumericValue = leftValue.NumericValue * rightValue.NumericValue;
                        break;
                    case InstructionOperation.OperatorType.Division:
                        leftValue.NumericValue = leftValue.NumericValue / rightValue.NumericValue;
                        break;
                    case InstructionOperation.OperatorType.Modulo:
                        leftValue.NumericValue = leftValue.NumericValue % rightValue.NumericValue;
                        break;
                    case InstructionOperation.OperatorType.Or:
                        leftValue.BoolValue = leftValue.BoolValue || rightValue.BoolValue;
                        break;
                    case InstructionOperation.OperatorType.And:
                        leftValue.BoolValue = leftValue.BoolValue && rightValue.BoolValue;
                        break;
                    case InstructionOperation.OperatorType.TestEqual:
                        switch (leftValue.Type)
                        {
                            case Value.DataType.String:
                                leftValue.BoolValue = leftValue.StringValue == rightValue.StringValue;
                                break;
                            case Value.DataType.Numeric:
                                leftValue.BoolValue = leftValue.NumericValue == rightValue.NumericValue;
                                break;
                            case Value.DataType.Boolean:
                                leftValue.BoolValue = leftValue.BoolValue == rightValue.BoolValue;
                                break;
                        }    
                        leftValue.Type = Value.DataType.Boolean;
                        break;
                    case InstructionOperation.OperatorType.TestNotEqual:
                        switch (leftValue.Type)
                        {
                            case Value.DataType.String:
                                leftValue.BoolValue = leftValue.StringValue != rightValue.StringValue;
                                break;
                            case Value.DataType.Numeric:
                                leftValue.BoolValue = leftValue.NumericValue != rightValue.NumericValue;
                                break;
                            case Value.DataType.Boolean:
                                leftValue.BoolValue = leftValue.BoolValue != rightValue.BoolValue;
                                break;
                        }
                        leftValue.Type = Value.DataType.Boolean;
                        break;
                    case InstructionOperation.OperatorType.GreaterThan:
                        leftValue.BoolValue = leftValue.NumericValue > rightValue.NumericValue;
                        leftValue.Type = Value.DataType.Boolean;
                        break;
                    case InstructionOperation.OperatorType.LessThan:
                        leftValue.BoolValue = leftValue.NumericValue < rightValue.NumericValue;
                        leftValue.Type = Value.DataType.Boolean;
                        break;
                    case InstructionOperation.OperatorType.GreaterThanOrEqual:
                        leftValue.BoolValue = leftValue.NumericValue >= rightValue.NumericValue;
                        leftValue.Type = Value.DataType.Boolean;
                        break;
                    case InstructionOperation.OperatorType.LessThanOrEqual:
                        leftValue.BoolValue = leftValue.NumericValue <= rightValue.NumericValue;
                        leftValue.Type = Value.DataType.Boolean;
                        break;
                }
            }

            return new InstructionResult()
            {
                Value = leftValue,
                Return = true
            };
        }

        Value GetVariableValue(Variable variable)
        {
            if (variable.Global)
            {
                return _globalVariables[variable.Index];
            }
            else
            {
                return _heap[_heap.Count - _currentFunction.Variables.Count + variable.Index];
            }
        }

        Variable GetVariableByName(string name)
        {
            if(_currentFunction != null)
            {
                Variable localVariable = _currentFunction.Variables.FirstOrDefault(v => v.Name == name);
                if (localVariable != null)
                {
                    return localVariable;
                }
            }
            return _program.Variables.FirstOrDefault(v => v.Name == name);
        }

        Value CopyValue(Value value)
        {
            Value newValue = new Value();
            newValue.Type = value.Type;
            newValue.BoolValue = value.BoolValue;
            newValue.NumericValue = value.NumericValue;
            newValue.StringValue = value.StringValue;
            return newValue;
        }

        void CopyValue(ref Value destination, Value value)
        {
            destination.Type = value.Type;
            destination.BoolValue = value.BoolValue;
            destination.NumericValue = value.NumericValue;
            destination.StringValue = value.StringValue;
        }
    }
}