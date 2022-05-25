using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace playfairСipher
{
    public class PlayFairEng : SecurityAlgorithm
    {
        protected readonly Dictionary<char, int> alphabet;
        string key;

        public PlayFairEng(string key)
        {
            alphabet = new Dictionary<char, int>();
            char c = 'a';
            alphabet.Add(c, 0);

            for (int i = 1; i < 26; i++)
            {
                alphabet.Add(++c, i);
            }

            this.key = key;
        }

        #region Public Methods
        public override string Encrypt(string plainText)
        {
            return Process(plainText, Mode.Encrypt);
        }

        public override string Decrypt(string cipherText)
        {
            return Process(cipherText, Mode.Decrypt);
        }

        #endregion

        #region Private Methods

        private string Process(string message, Mode mode)
        {
            
            // Ключ:Символ
            // Значение:Позиция
            Dictionary<char, string> characterPositionsInMatrix = new Dictionary<char, string>();

            // Ключ:Символ
            // Значение:Позиция
            Dictionary<string, char> positionCharacterInMatrix = new Dictionary<string, char>();

            FillMatrix(key.Distinct().ToArray(), characterPositionsInMatrix, positionCharacterInMatrix);

            if (mode == Mode.Encrypt)
            {
                message = RepairWord(message);
            }

            string result = "";

            for (int i = 0; i < message.Length; i += 2)
            {
                string substring_of_2 = message.Substring(i, 2).ToLower(); // получение символов из текста по парам
                // получить строку и столбец каждого символа
                string rc1 = characterPositionsInMatrix[substring_of_2[0]];
                string rc2 = characterPositionsInMatrix[substring_of_2[1]];

                if (rc1[0] == rc2[0]) // та же колонка, другая строка
                {
                    int newC1 = 0, newC2 = 0;

                    switch (mode)
                    {
                        case Mode.Encrypt: // шифрование
                            newC1 = (int.Parse(rc1[1].ToString()) + 1) % 5;
                            newC2 = (int.Parse(rc2[1].ToString()) + 1) % 5;
                            break;
                        case Mode.Decrypt: // дешифрование
                            newC1 = (int.Parse(rc1[1].ToString()) - 1) % 5;
                            newC2 = (int.Parse(rc2[1].ToString()) - 1) % 5;
                            break;
                    }

                    newC1 = RepairNegative(newC1);
                    newC2 = RepairNegative(newC2);

                    result += positionCharacterInMatrix[rc1[0].ToString() + newC1.ToString()];
                    result += positionCharacterInMatrix[rc2[0].ToString() + newC2.ToString()];
                }

                else if (rc1[1] == rc2[1]) // та же колонка, другая строка
                {
                    int newR1 = 0, newR2 = 0;

                    switch (mode)
                    {
                        case Mode.Encrypt: // шифрование
                            newR1 = (int.Parse(rc1[0].ToString()) + 1) % 5;
                            newR2 = (int.Parse(rc2[0].ToString()) + 1) % 5;
                            break;
                        case Mode.Decrypt: // дешифрование
                            newR1 = (int.Parse(rc1[0].ToString()) - 1) % 5;
                            newR2 = (int.Parse(rc2[0].ToString()) - 1) % 5;
                            break;
                    }
                    newR1 = RepairNegative(newR1);
                    newR2 = RepairNegative(newR2);

                    result += positionCharacterInMatrix[newR1.ToString() + rc1[1].ToString()];
                    result += positionCharacterInMatrix[newR2.ToString() + rc2[1].ToString()];
                }

                else // разные строки и столбцы
                {
                    // 1-й символ: строка 1-го + столбец 2-го
                    // 2-ой символ: строка 2-го + столбец 1-го
                    result += positionCharacterInMatrix[rc1[0].ToString() + rc2[1].ToString()];
                    result += positionCharacterInMatrix[rc2[0].ToString() + rc1[1].ToString()];
                }
            }

            // удаляет все x из строки при дешифровании
            if (mode == Mode.Decrypt)
            {
                result = result.Replace("x", "");
            }

            return result;
        }

        private string RepairWord(string message)
        {
            string trimmed = message.Replace(" ", "");
            string result = "";

            for (int i = 0; i < trimmed.Length; i++)
            {
                result += trimmed[i];

                if (i < trimmed.Length - 1 && message[i] == message[i + 1]) // проверить, являются ли две последовательные буквы одинаковыми
                {
                    result += 'x';
                }
            }

            if (result.Length % 2 != 0) // проверить, является ли длина четной
            {
                result += 'x';
            }

            return result.ToLower();
        }

        private void FillMatrix(IList<char> key, Dictionary<char, string> characterPositionsInMatrix, Dictionary<string, char> positionCharacterInMatrix)
        {
            char[,] matrix = new char[5, 5];
            int keyPosition = 0, charPosition = 0;
            List<char> alphabetPF = alphabet.Keys.ToList();
            alphabetPF.Remove('j');

            for (int i = 0; i < 5; i++)
            {
                for (int j = 0; j < 5; j++)
                {
                    if (charPosition < key.Count)
                    {
                        matrix[i, j] = key[charPosition]; // заполнить матрицу ключом
                        alphabetPF.Remove(key[charPosition]);
                        charPosition++;
                    }

                    else // клавиша закончена... заполните остальной алфавит
                    {
                        matrix[i, j] = alphabetPF[keyPosition];
                        keyPosition++;
                    }

                    string position = i.ToString() + j.ToString();
                    // хранить позиции символов в словаре, чтобы не искать их каждый раз
                    characterPositionsInMatrix.Add(matrix[i, j], position);
                    positionCharacterInMatrix.Add(position, matrix[i, j]);
                }
            }
        }

        private int RepairNegative(int number)
        {
            if (number < 0)
            {
                number += 5;
            }

            return number;
        }

        #endregion
    }
}