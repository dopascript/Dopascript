﻿using System;
using System.Collections.Generic;

namespace DopaScript
{
    public class Value
    {
        public enum DataType { String, Numeric, Boolean }

        public DataType Type { get; set; }

        public decimal NumericValue { get; set; }
        public bool BoolValue { get; set; }
        public string StringValue { get; set; }
    }

    class Variable
    {
        public string Name { get; set; }
        public bool Reference { get; set; }
        public bool Global { get; set; }
        public int Index { get; set; }
    }

    class Program
    {
        public List<Variable> Variables { get; set; }
        public List<Instruction> Instructions { get; set; }
        public List<Function> Functions { get; set; }

        public Program()
        {
            Variables = new List<Variable>();
            Instructions = new List<Instruction>();
            Functions = new List<Function>();
        }
    }

    class Function
    {
        public string Name { get; set; }
        public List<Variable> Variables { get; set; }
        public List<Instruction> Instructions { get; set; }
        public int ParametersCount { get; set; }

        public Function()
        {
            Variables = new List<Variable>();
            Instructions = new List<Instruction>();
            ParametersCount = 0;
        }
    }

    abstract class Instruction
    {
        public int Line { get; set; }
        public int Position { get; set; }
    }

    class InstructionAssignment : Instruction
    {
        public enum Type { Base, Addition, Substraction, Division, Multiplication }

        public string VariableName { get; set; }
        public Instruction Instruction { get; set; }
    }

    class InstructionOperation : Instruction
    {
        public enum OperatorType{ Addition, Substraction, Multiplication, Division, Modulo, Or, And,
                                  TestEqual, TestNotEqual, GreaterThan, LessThan, GreaterThanOrEqual, LessThanOrEqual}

        public List<Instruction>  ValuesInstructions { get; set; }
        public List<OperatorType> Operators { get; set; }

        public InstructionOperation()
        {
            ValuesInstructions = new List<Instruction>();
            Operators = new List<OperatorType>();
        }
    }

    class InstructionBloc : Instruction
    {
        public List<Instruction> Instructions { get; set; }

        public InstructionBloc()
        {
            Instructions = new List<Instruction>();
        }
    }

    class InstructionFunction : Instruction
    {
        public string FunctionName { get; set; }
        public List<Instruction> Parameters { get; set; }
        public Instruction BlocInstruction { get; set; }

        public InstructionFunction()
        {
            Parameters = new List<Instruction>();
        }
    }

    class InstructionCondition : Instruction
    {
        public List<Instruction> TestInstructions { get; set; }
        public List<Instruction> BlocInstructions { get; set; }

        public InstructionCondition()
        {
            TestInstructions = new List<Instruction>();
            BlocInstructions = new List<Instruction>();
        }
    }

    class InstructionWhile : Instruction
    {
        public Instruction TestInstruction { get; set; }
        public Instruction BlocInstruction { get; set; }
    }

    class InstructionFor : Instruction
    {
        public Instruction InitInstruction { get; set; }
        public Instruction TestInstruction { get; set; }
        public Instruction incrementInstruction { get; set; }
        public Instruction BlocInstruction { get; set; }
    }

    class InstructionReturn : Instruction
    {
        public Instruction ValueInstruction { get; set; }
    }

    class InstructionBreak : Instruction
    {

    }

    class InstructionValue : Instruction
    {
        public Value Value { get; set; }
    }

    class InstructionVariableValue : Instruction
    {
        public string VariableName { get; set; }
    }
}