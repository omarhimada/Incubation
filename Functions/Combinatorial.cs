using System;
using System.Collections.Generic;
using System.Text;

namespace Incubation.Functions {
    public static class PartialCombinatorial {
        /// <summary>
        /// Generates all possible combinations of characters of a specified length using the provided alphabet.
        /// </summary>
        /// <remarks>The method produces combinations in lexicographical order based on the order of characters in
        /// the alphabet array. The caller is responsible for converting the returned character arrays to strings if needed.
        /// The total number of combinations generated is Math.Pow(alphabet.Length, length).</remarks>
        /// <param name="alphabet">An array of characters representing the set of symbols to use for generating combinations. Cannot be null or
        /// empty.</param>
        /// <param name="length">The length of each combination to generate. Must be greater than or equal to zero.</param>
        /// <returns>An enumerable collection of character arrays, each representing a unique combination of the specified length
        /// formed from the given alphabet. The collection will be empty if the length is zero.</returns>
        public static IEnumerable<char[]> CartesianProduct(char[] alphabet, int length) {
            // Generates all strings of 'length' from alphabet. For 2 digits -> 100 combos.
            // Returns char[] (caller makes string)
            int n = alphabet.Length;
            int total = (int)Math.Pow(n, length);

            for (int i = 0; i < total; i++) {
                var chars = new char[length];
                int x = i;
                for (int pos = length - 1; pos >= 0; pos--) {
                    chars[pos] = alphabet[x % n];
                    x /= n;
                }
                yield return chars;
            }
        }
    }
}
