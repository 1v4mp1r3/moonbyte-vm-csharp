using System.Text;

namespace MoonByte.Core;

public static class Disassembler
{
    public static string Disassemble(BytecodeChunk chunk)
    {
        var builder = new StringBuilder();
        for (int i = 0; i < chunk.Instructions.Count; i++)
        {
            Instruction instruction = chunk.Instructions[i];
            builder.Append(i.ToString("0000"));
            builder.Append("  ");
            builder.Append(instruction.OpCode);
            if (instruction.Name is not null)
            {
                builder.Append(' ');
                builder.Append(instruction.Name);
            }

            if (instruction.Operand != 0 || instruction.OpCode is OpCode.Constant or OpCode.Call or OpCode.GetLocal or OpCode.SetLocal or OpCode.MakeTable)
            {
                builder.Append(' ');
                builder.Append(instruction.Operand);
                if (instruction.OpCode == OpCode.Constant)
                {
                    builder.Append(" ; ");
                    builder.Append(chunk.Constants[instruction.Operand].ToDisplayString());
                }
            }

            builder.AppendLine();
        }

        return builder.ToString();
    }
}

