using CSScript.Core;
using System;
using System.IO;
using System.Text;

namespace CSScript
{
    public class ConsoleScriptContext : BaseScriptContext
    {
        public override string OnReadLine(ConsoleColor color) {

            ConsoleColor prevColor = Console.ForegroundColor;
            if (color != prevColor) {
                Console.ForegroundColor = color;
            }

            // Стандартный входной поток обрабатывает только 256 символов
            int READLINE_BUFFER_SIZE = 102400;
            Stream inputStream = Console.OpenStandardInput(READLINE_BUFFER_SIZE);
            byte[] bytes = new byte[READLINE_BUFFER_SIZE];
            int outputLength = inputStream.Read(bytes, 0, READLINE_BUFFER_SIZE);
            char[] chars = Encoding.UTF7.GetChars(bytes, 0, outputLength - Environment.NewLine.Length);
            inputStream.Dispose();

            if (color != prevColor) {
                Console.ForegroundColor = prevColor;
            }

            return new string(chars);
        }

        public override void OnWrite(string value, ConsoleColor color) {
            ConsoleColor prevColor = Console.ForegroundColor;
            if (color != prevColor) {
                Console.ForegroundColor = color;
            }
            Console.Out.Write(value);
            if (color != prevColor) {
                Console.ForegroundColor = prevColor;
            }
        }

        public override void OnWriteError(string value, ConsoleColor color) {
            ConsoleColor prevColor = Console.ForegroundColor;
            if (color != prevColor) {
                Console.ForegroundColor = color;
            }
            Console.Error.Write(value);
            if (color != prevColor) {
                Console.ForegroundColor = prevColor;
            }
        }
    }
}
